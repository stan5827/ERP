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
    Task<UserDto?> RegisterAsync(CreateUserRequest request);
    Task<(bool Success, string? Error)> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, bool skipOldPassword = false);
    Task<bool> ResetUserPasswordAsync(string username, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(ErpGeneralDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null || !BCryptNet.Verify(request.Password, user.PasswordHash))
            return null;

        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        var permissions = await _db.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => user.UserRoles.Select(ur => ur.RoleId).Contains(rp.RoleId))
            .Select(rp => rp.Permission.PermissionCode)
            .Distinct().ToListAsync();

        var token = GenerateJwtToken(user.Id, user.Username, roles, permissions);

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new LoginResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"] ?? "480")),
            Roles = roles,
            Permissions = permissions,
            MustChangePassword = user.MustChangePassword
        };
    }

    public async Task<UserDto?> RegisterAsync(CreateUserRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return null;

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCryptNet.HashPassword(request.Password),
            Email = request.Email,
            EmployeeId = request.EmployeeId
        };

        _db.Users.Add(user);

        foreach (var roleId in request.RoleIds)
        {
            if (await _db.Roles.AnyAsync(r => r.Id == roleId))
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        await _db.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            EmployeeId = user.EmployeeId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = await _db.UserRoles.Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.RoleName).ToListAsync()
        };
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, bool skipOldPassword = false)
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
        
        if (!skipOldPassword)
        {
            if (!BCryptNet.Verify(oldPassword, user.PasswordHash))
                return (false, "Invalid old password");
        }

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
        
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private string GenerateJwtToken(Guid userId, string username, List<string> roles, List<string> permissions)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? _config["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT_SECRET_KEY is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? throw new InvalidOperationException("JWT key not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
        };
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

    public async Task<bool> ResetUserPasswordAsync(string username, string newPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return false;

        user.PasswordHash = BCryptNet.HashPassword(newPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync();
        return true;
    }
}
