using System.Data;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.System;
using BCryptNet = BCrypt.Net.BCrypt;

namespace ErpBackend.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync(string? search = null, int page = 1, int pageSize = 20, int? totalCount = null);
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<(UserDto? dto, string status)> CreateAsync(CreateUserRequest request, Guid createdByUserId);
    Task<(bool found, string status)> UpdateAsync(Guid id, UpdateUserRequestDto request, Guid updatedByUserId);
    Task<(bool found, string status)> UpdateRolesAsync(Guid id, UpdateUserRolesRequest request, Guid updatedByUserId);
    Task<(bool found, string status)> DeleteAsync(Guid id, Guid deactivatedByUserId);
    Task<(bool found, string status)> UnlockAsync(Guid id, Guid unlockedByUserId);
}

public class UserService : IUserService
{
    private readonly ErpGeneralDbContext _db;

    public UserService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<UserDto>> GetAllAsync(string? search = null, int page = 1, int pageSize = 20, int? totalCount = null)
    {
        var query = _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Username.Contains(search) || (u.Email != null && u.Email.Contains(search)));

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return items.Select(MapToDto).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        return user == null ? null : MapToDto(user);
    }

    public async Task<(UserDto? dto, string status)> CreateAsync(CreateUserRequest request, Guid createdByUserId)
    {
        if (request == null || createdByUserId == Guid.Empty) return (null, "invalid");

        var username = request.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username))
            return (null, "invalid");
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 8)
            return (null, "invalid");

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Password, @"[A-Za-z]") ||
            !System.Text.RegularExpressions.Regex.IsMatch(request.Password, @"[0-9]"))
            return (null, "invalid");

        if (await _db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
            return (null, "duplicate");

        if (request.EmployeeId == null)
            return (null, "employee_required");

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId.Value);
        if (employee == null)
            return (null, "employee_not_found");

        if (!string.Equals(employee.Status, "active", StringComparison.OrdinalIgnoreCase))
            return (null, "employee_inactive");

        if (await _db.Users.AnyAsync(u => u.EmployeeId == request.EmployeeId))
            return (null, "employee_already_linked");

        var roleIds = request.RoleIds?.Distinct().ToList() ?? [];
        if (roleIds.Count > 0)
        {
            var existingRoleCount = await _db.Roles.CountAsync(
                role => role.IsActive && roleIds.Contains(role.Id));
            if (existingRoleCount != roleIds.Count)
                return (null, "invalid_roles");
        }

        if ((string.IsNullOrWhiteSpace(employee.NationalId)
                && string.IsNullOrWhiteSpace(employee.PassportNo)
                && string.IsNullOrWhiteSpace(employee.ResidencePermitNo))
            || employee.HireDate == null
            || employee.DepartmentId == null
            || employee.JobTitleId == null)
            return (null, "employee_profile_incomplete");

        var departmentIsActive = await _db.Departments
            .AnyAsync(department => department.Id == employee.DepartmentId && department.IsActive);
        if (!departmentIsActive)
            return (null, "employee_department_invalid");

        var jobTitleIsActive = await _db.JobTitles
            .AnyAsync(jobTitle => jobTitle.Id == employee.JobTitleId && jobTitle.IsActive);
        if (!jobTitleIsActive)
            return (null, "employee_job_title_invalid");

        var user = new User
        {
            Username = username,
            PasswordHash = BCryptNet.HashPassword(request.Password),
            Email = request.Email,
            EmployeeId = request.EmployeeId,
            MustChangePassword = true
        };

        _db.Users.Add(user);

        foreach (var roleId in roleIds)
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });

        _db.AuditLogs.Add(new AuditLog
        {
            Action = "USER_CREATED",
            EntityType = nameof(User),
            EntityId = user.Id,
            UserId = createdByUserId,
            TableName = "Users",
            Details = "Employee account created with assigned roles."
        });

        await _db.SaveChangesAsync();

        var created = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == user.Id);

        return (MapToDto(created), "ok");
    }

    public async Task<(bool found, string status)> UpdateAsync(Guid id, UpdateUserRequestDto request, Guid updatedByUserId)
    {
        if (updatedByUserId == Guid.Empty) return (false, "actor_required");
        if (request == null) return (false, "invalid_email");

        var user = await _db.Users.FirstOrDefaultAsync(account => account.Id == id);
        if (user == null) return (false, "not_found");

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (email != null && email.Length > 200) return (false, "invalid_email");

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = email;
            user.UpdatedAt = DateTime.UtcNow;
            _db.AuditLogs.Add(new AuditLog
            {
                Action = "USER_UPDATED",
                EntityType = nameof(User),
                EntityId = user.Id,
                UserId = updatedByUserId,
                TableName = "Users",
                Details = email == null ? "User email cleared." : "User email updated."
            });
            await _db.SaveChangesAsync();
        }

        return (true, "ok");
    }

    public async Task<(bool found, string status)> UpdateRolesAsync(
        Guid id,
        UpdateUserRolesRequest request,
        Guid updatedByUserId)
    {
        if (updatedByUserId == Guid.Empty) return (false, "actor_required");
        if (request == null || request.RoleIds == null) return (false, "invalid_roles");
        if (string.IsNullOrWhiteSpace(request.Reason)) return (false, "reason_required");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var roleIds = request.RoleIds.Distinct().ToList();
        var activeRoleCount = await _db.Roles.CountAsync(
            role => role.IsActive && roleIds.Contains(role.Id));
        if (activeRoleCount != roleIds.Count) return (false, "invalid_roles");

        var user = await _db.Users
            .Include(account => account.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .FirstOrDefaultAsync(account => account.Id == id);
        if (user == null) return (false, "not_found");

        var existingRoleIds = user.UserRoles.Select(userRole => userRole.RoleId).ToHashSet();
        if (existingRoleIds.SetEquals(roleIds))
        {
            await transaction.CommitAsync();
            return (true, "unchanged");
        }

        var retainsAdmin = await _db.Roles
            .Where(role => roleIds.Contains(role.Id))
            .AnyAsync(role => role.RoleName == "Admin" && role.IsActive);
        var currentlyAdmin = user.UserRoles.Any(userRole =>
            userRole.Role.IsActive && userRole.Role.RoleName == "Admin");
        if (user.IsActive && currentlyAdmin && !retainsAdmin)
        {
            var otherActiveAdmins = await _db.UserRoles
                .Where(userRole =>
                    userRole.UserId != id
                    && userRole.User.IsActive
                    && userRole.Role.IsActive
                    && userRole.Role.RoleName == "Admin")
                .Select(userRole => userRole.UserId)
                .Distinct()
                .CountAsync();
            if (otherActiveAdmins == 0) return (false, "last_admin_required");
        }

        var oldRoleNames = user.UserRoles
            .Select(userRole => userRole.Role.RoleName)
            .OrderBy(roleName => roleName, StringComparer.Ordinal)
            .ToArray();
        var newRoles = await _db.Roles
            .Where(role => roleIds.Contains(role.Id) && role.IsActive)
            .ToListAsync();

        _db.UserRoles.RemoveRange(user.UserRoles);
        foreach (var role in newRoles)
            _db.UserRoles.Add(new UserRole { UserId = id, RoleId = role.Id });

        user.SecurityVersion = checked(user.SecurityVersion + 1);
        user.UpdatedAt = DateTime.UtcNow;
        var newRoleNames = newRoles
            .Select(role => role.RoleName)
            .OrderBy(roleName => roleName, StringComparer.Ordinal);
        _db.AuditLogs.Add(new AuditLog
        {
            Action = "USER_ROLES_UPDATED",
            EntityType = nameof(User),
            EntityId = id,
            UserId = updatedByUserId,
            TableName = "Users",
            Details = $"Roles changed from [{string.Join(", ", oldRoleNames)}] to [{string.Join(", ", newRoleNames)}]. Reason: {request.Reason.Trim()}"
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, "ok");
    }

    public async Task<(bool found, string status)> DeleteAsync(Guid id, Guid deactivatedByUserId)
    {
        if (deactivatedByUserId == Guid.Empty) return (false, "actor_required");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return (false, "not_found");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.SecurityVersion = checked(user.SecurityVersion + 1);
        _db.AuditLogs.Add(new AuditLog
        {
            Action = "USER_DEACTIVATED",
            EntityType = nameof(User),
            EntityId = user.Id,
            UserId = deactivatedByUserId,
            TableName = "Users",
            Details = "Employee account deactivated."
        });
        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    public async Task<(bool found, string status)> UnlockAsync(Guid id, Guid unlockedByUserId)
    {
        if (unlockedByUserId == Guid.Empty) return (false, "actor_required");

        var userExists = await _db.Users.AnyAsync(u => u.Id == id);
        if (!userExists) return (false, "not_found");

        _db.AuditLogs.Add(new AuditLog
        {
            Action = "LOGIN_LOCKOUT_RELEASED",
            EntityType = nameof(User),
            EntityId = id,
            UserId = unlockedByUserId,
            TableName = "Users",
            Details = "Account lockout released by an authorized administrator."
        });
        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        EmployeeId = user.EmployeeId,
        IsActive = user.IsActive,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt,
        Roles = user.UserRoles.Select(ur => ur.Role?.RoleName ?? "").ToList()
    };
}
