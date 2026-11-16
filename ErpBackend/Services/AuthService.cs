using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.System;
using BCryptNet = BCrypt.Net.BCrypt;

namespace ErpBackend.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<(bool Success, string? Error)> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
    Task<bool> ResetUserPasswordAsync(string username, string newPassword, Guid resetByUserId);
    Task<bool> RecoverAdminPasswordAsync(string username, string newPassword, string operatorIdentity);
}

public class AuthService : IAuthService
{
    private const int DefaultMaxFailedLoginAttempts = 5;
    private const int DefaultLockoutDurationMinutes = 15;
    private readonly ErpGeneralDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(ErpGeneralDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null)
            return null;

        if (string.Equals(_db.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
        {
            await _db.Database.SqlQuery<Guid>(
                    $"""SELECT "Id" AS "Value" FROM "Users" WHERE "Id" = {user.Id} FOR UPDATE""")
                .SingleAsync();
            await _db.Entry(user).ReloadAsync();
            if (!user.IsActive)
                return null;
        }

        var hasActiveAdminRole = user.UserRoles.Any(userRole =>
            userRole.Role.IsActive && userRole.Role.RoleName == "Admin");
        if (user.EmployeeId is Guid employeeId)
        {
            var employeeStatus = await _db.Employees
                .AsNoTracking()
                .Where(employee => employee.Id == employeeId)
                .Select(employee => employee.Status)
                .SingleOrDefaultAsync();
            if (employeeStatus == null
                || !string.Equals(employeeStatus, "active", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(employeeStatus, "leave", StringComparison.OrdinalIgnoreCase))
                return null;
        }
        else if (!hasActiveAdminRole)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var maxFailedAttempts = GetPositiveSetting("Auth:Lockout:MaxFailedAttempts", DefaultMaxFailedLoginAttempts);
        var lockoutDurationMinutes = GetPositiveSetting("Auth:Lockout:DurationMinutes", DefaultLockoutDurationMinutes);
        var latestResetEvent = await GetLatestLoginResetEventAsync(user.Id);
        if (latestResetEvent?.Action == "LOGIN_LOCKOUT_STARTED"
            && latestResetEvent.CreatedAt.AddMinutes(lockoutDurationMinutes) > now)
            return null;

        var failedAttemptsStart = latestResetEvent != null
            && latestResetEvent.CreatedAt > now.AddMinutes(-lockoutDurationMinutes)
                ? latestResetEvent.CreatedAt
                : now.AddMinutes(-lockoutDurationMinutes);
        var recentFailedAttempts = await GetConsecutiveFailedLoginCountAsync(
            user.Id,
            failedAttemptsStart,
            latestResetEvent != null);

        if (!BCryptNet.Verify(request.Password, user.PasswordHash))
        {
            AddLoginAuditEvent(user.Id, user.Id, "LOGIN_FAILED", "Invalid credentials.", now);
            if (recentFailedAttempts + 1 >= maxFailedAttempts)
                AddLoginAuditEvent(user.Id, user.Id, "LOGIN_LOCKOUT_STARTED", "Account locked after repeated failed login attempts.", now);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return null;
        }

        var activeRoleIds = user.UserRoles
            .Where(userRole => userRole.Role.IsActive)
            .Select(userRole => userRole.RoleId)
            .ToList();
        var roles = user.UserRoles
            .Where(userRole => userRole.Role.IsActive)
            .Select(userRole => userRole.Role.RoleName)
            .ToList();
        var permissions = await _db.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => activeRoleIds.Contains(rp.RoleId) && rp.Permission.IsActive)
            .Select(rp => rp.Permission.PermissionCode)
            .Distinct().ToListAsync();

        var token = GenerateJwtToken(user.Id, user.EmployeeId, user.Username, user.SecurityVersion, roles, permissions);

        var employeeName = user.EmployeeId.HasValue
            ? await _db.Employees
                .Where(employee => employee.Id == user.EmployeeId.Value && employee.Status == "active")
                .Select(employee => employee.ChineseName)
                .FirstOrDefaultAsync()
            : null;

        user.LastLoginAt = now;
        AddLoginAuditEvent(user.Id, user.Id, "LOGIN_SUCCESS", "User authenticated successfully.", now);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new LoginResponse
        {
            UserId = user.Id,
            EmployeeId = user.EmployeeId,
            EmployeeName = employeeName,
            Username = user.Username,
            Email = user.Email,
            Token = token,
            ExpiresAt = now.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"] ?? "480")),
            Roles = roles,
            Permissions = permissions,
            MustChangePassword = user.MustChangePassword
        };
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found");
        
        // 密碼歷史檢查 - 防止重複使用最近 12 組密碼
        var passwordHistory = await _db.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(12)
            .ToListAsync();
        
        foreach (var history in passwordHistory)
        {
            if (BCryptNet.Verify(newPassword, history.PasswordHash))
                return (false, "Cannot reuse any of your last 12 passwords");
        }
        
        if (!BCryptNet.Verify(oldPassword, user.PasswordHash))
            return (false, "Invalid old password");

        // 記錄密碼歷史
        _db.PasswordHistories.Add(new PasswordHistory
        {
            UserId = userId,
            PasswordHash = user.PasswordHash,
            CreatedAt = DateTime.UtcNow
        });
        
        // 清理舊的密碼歷史（保留最近 12 組）
        var oldHistories = await _db.PasswordHistories
            .Where(ph => ph.UserId == userId)
            .OrderByDescending(ph => ph.CreatedAt)
            .Skip(12)
            .ToListAsync();
        _db.PasswordHistories.RemoveRange(oldHistories);
        
        user.PasswordHash = BCryptNet.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.PasswordHistoryCount = passwordHistory.Count + 1;
        user.SecurityVersion = checked(user.SecurityVersion + 1);
        
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private string ResolveJwtKey()
    {
        // 環境變數優先（與 Program.cs 一致），避免 appsettings.json 的 ${...} placeholder 未展開
        var envKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        if (!string.IsNullOrWhiteSpace(envKey) && !envKey.StartsWith("${"))
            return envKey;

        var configKey = _config["Jwt:Key"];
        if (!string.IsNullOrWhiteSpace(configKey) && !configKey.StartsWith("${"))
            return configKey;

        throw new InvalidOperationException("JWT_SECRET_KEY is not configured. Please set the JWT_SECRET_KEY environment variable (at least 32 characters).");
    }

    private string GenerateJwtToken(
        Guid userId,
        Guid? employeeId,
        string username,
        long securityVersion,
        List<string> roles,
        List<string> permissions)
    {
        var jwtKey = ResolveJwtKey();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new("security_version", securityVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        };
        if (employeeId.HasValue)
            claims.Add(new Claim("employee_id", employeeId.Value.ToString()));
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("Permission", p)));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"] ?? "480")),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> ResetUserPasswordAsync(string username, string newPassword, Guid resetByUserId)
    {
        if (resetByUserId == Guid.Empty)
            return false;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return false;

        return await ResetPasswordAsync(
            user,
            newPassword,
            resetByUserId,
            "USER_PASSWORD_RESET",
            "Password reset by an authorized administrator.",
            resetByUserId);
    }

    public async Task<bool> RecoverAdminPasswordAsync(string username, string newPassword, string operatorIdentity)
    {
        if (string.IsNullOrWhiteSpace(operatorIdentity)
            || operatorIdentity.Length > 80
            || operatorIdentity.Any(char.IsControl))
            return false;

        var user = await _db.Users
            .Include(account => account.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(account => account.Username == username);
        if (user == null
            || !user.IsActive
            || !user.UserRoles.Any(userRole =>
                userRole.Role.IsActive && userRole.Role.RoleName == "Admin"))
            return false;

        if (string.IsNullOrWhiteSpace(newPassword)
            || newPassword.Length < 16
            || !System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"[A-Za-z]")
            || !System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"[0-9]"))
            return false;

        return await ResetPasswordAsync(
            user,
            newPassword,
            null,
            "USER_PASSWORD_RECOVERY",
            $"Offline administrator recovery. Operator: {operatorIdentity}",
            null);
    }

    private async Task<bool> ResetPasswordAsync(
        User user,
        string newPassword,
        Guid? resetByUserId,
        string auditAction,
        string auditDetails,
        Guid? loginEventActorId)
    {
        if (string.IsNullOrWhiteSpace(newPassword)
            || newPassword.Length < 8
            || !System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"[A-Za-z]")
            || !System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"[0-9]"))
            return false;

        var passwordHistory = await _db.PasswordHistories
            .Where(history => history.UserId == user.Id)
            .OrderByDescending(history => history.CreatedAt)
            .Take(12)
            .ToListAsync();
        if (BCryptNet.Verify(newPassword, user.PasswordHash)
            || passwordHistory.Any(history => BCryptNet.Verify(newPassword, history.PasswordHash)))
            return false;

        _db.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
            PasswordHash = user.PasswordHash,
            CreatedAt = DateTime.UtcNow
        });
        var expiredHistory = await _db.PasswordHistories
            .Where(history => history.UserId == user.Id)
            .OrderByDescending(history => history.CreatedAt)
            .Skip(12)
            .ToListAsync();
        _db.PasswordHistories.RemoveRange(expiredHistory);

        user.PasswordHash = BCryptNet.HashPassword(newPassword);
        user.MustChangePassword = true;
        user.UpdatedAt = DateTime.UtcNow;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.PasswordHistoryCount = passwordHistory.Count + 1;
        user.SecurityVersion = checked(user.SecurityVersion + 1);
        _db.AuditLogs.Add(new AuditLog
        {
            Action = auditAction,
            EntityType = nameof(User),
            EntityId = user.Id,
            UserId = resetByUserId,
            TableName = "Users",
            Details = auditDetails
        });
        _db.AuditLogs.Add(new AuditLog
        {
            Action = "LOGIN_LOCKOUT_RELEASED",
            EntityType = nameof(User),
            EntityId = user.Id,
            UserId = loginEventActorId,
            TableName = "Users",
            Details = "Lockout cleared during password recovery.",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<AuditLog?> GetLatestLoginResetEventAsync(Guid userId)
    {
        return await _db.AuditLogs
            .AsNoTracking()
            .Where(log => log.EntityId == userId
                && (log.Action == "LOGIN_SUCCESS"
                    || log.Action == "LOGIN_LOCKOUT_STARTED"
                    || log.Action == "LOGIN_LOCKOUT_RELEASED"))
            .OrderByDescending(log => log.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private async Task<int> GetConsecutiveFailedLoginCountAsync(Guid userId, DateTime windowStart, bool hasResetEvent)
    {
        var query = _db.AuditLogs
            .AsNoTracking()
            .Where(log => log.EntityId == userId && log.Action == "LOGIN_FAILED");

        query = hasResetEvent
            ? query.Where(log => log.CreatedAt > windowStart)
            : query.Where(log => log.CreatedAt >= windowStart);

        var recentFailedAttempts = await query.CountAsync();
        return recentFailedAttempts;
    }

    private void AddLoginAuditEvent(Guid userId, Guid affectedUserId, string action, string details, DateTime createdAt)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityType = nameof(User),
            EntityId = affectedUserId,
            UserId = userId,
            TableName = "Users",
            Details = details,
            CreatedAt = createdAt
        });
    }

    private int GetPositiveSetting(string key, int defaultValue)
    {
        var configuredValue = _config[key];
        if (configuredValue == null)
            return defaultValue;

        if (!int.TryParse(configuredValue, out var value) || value <= 0)
            throw new InvalidOperationException($"Configuration value '{key}' must be a positive integer.");

        return value;
    }
}
