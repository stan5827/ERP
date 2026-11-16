using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.System;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface ISystemSettingsService
{
    Task<List<PermissionTreeDto>> GetPermissionTreeAsync();
    Task<PermissionTreeDto?> CreatePermissionAsync(CreatePermissionRequest request);
    Task<PermissionTreeDto?> UpdatePermissionAsync(Guid id, UpdatePermissionRequest request);
    Task<bool> DeletePermissionAsync(Guid id);
    Task<List<UserWithRolesDto>> GetUsersAsync();
    Task<List<Guid>> GetLinkedEmployeeIdsAsync();
    Task<List<RoleDto>> GetRolesAsync();
    Task<RoleDto?> CreateRoleAsync(CreateRoleRequest request);
    Task<bool> UpdateRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds);
    Task<bool> DeleteRoleAsync(Guid id);
    Task<List<ThemeDto>> GetThemesAsync();
    Task<UserThemePreferenceDto> GetUserThemeAsync(Guid id);
    Task<bool> UpdateUserThemeAsync(Guid id, UpdateThemeRequest req);
    Task<List<ModuleAccessDto>> GetRoleModulesAsync(string roleName);
    Task<List<ModuleAccessDto>> GetCurrentUserModulesAsync(Guid userId);
}

public class SystemSettingsService : ISystemSettingsService
{
    private readonly ErpGeneralDbContext _db;

    public SystemSettingsService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<List<PermissionTreeDto>> GetPermissionTreeAsync()
    {
        var permissions = await _db.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        return BuildPermissionTree(permissions, null);
    }

    public async Task<PermissionTreeDto?> CreatePermissionAsync(CreatePermissionRequest request)
    {
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            PermissionCode = request.PermissionCode,
            ModuleName = request.ModuleName,
            ActionType = request.ActionType,
            PermissionType = request.PermissionType,
            Description = request.Description,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync();

        return new PermissionTreeDto
        {
            Id = permission.Id,
            PermissionCode = permission.PermissionCode,
            ModuleName = permission.ModuleName,
            ActionType = permission.ActionType,
            PermissionType = permission.PermissionType,
            Description = permission.Description,
            SortOrder = permission.SortOrder,
            Children = new List<PermissionTreeDto>()
        };
    }

    public async Task<PermissionTreeDto?> UpdatePermissionAsync(Guid id, UpdatePermissionRequest request)
    {
        var permission = await _db.Permissions.FindAsync(id);
        if (permission == null)
            return null;

        var affectedUsers = await _db.Users
            .Where(user => _db.UserRoles.Any(userRole =>
                userRole.UserId == user.Id
                && _db.RolePermissions.Any(rolePermission =>
                    rolePermission.RoleId == userRole.RoleId && rolePermission.PermissionId == id)))
            .ToListAsync();
        foreach (var user in affectedUsers)
            user.SecurityVersion = checked(user.SecurityVersion + 1);

        permission.PermissionCode = request.PermissionCode;
        permission.ModuleName = request.ModuleName;
        permission.ActionType = request.ActionType;
        permission.PermissionType = request.PermissionType;
        permission.Description = request.Description;
        permission.ParentId = request.ParentId;
        permission.SortOrder = request.SortOrder;

        await _db.SaveChangesAsync();

        return new PermissionTreeDto
        {
            Id = permission.Id,
            PermissionCode = permission.PermissionCode,
            ModuleName = permission.ModuleName,
            ActionType = permission.ActionType,
            PermissionType = permission.PermissionType,
            Description = permission.Description,
            SortOrder = permission.SortOrder,
            Children = new List<PermissionTreeDto>()
        };
    }

    public async Task<bool> DeletePermissionAsync(Guid id)
    {
        var permission = await _db.Permissions.FindAsync(id);
        if (permission == null)
            return false;

        var affectedRoleIds = await _db.RolePermissions
            .Where(rolePermission => rolePermission.PermissionId == id)
            .Select(rolePermission => rolePermission.RoleId)
            .Distinct()
            .ToListAsync();
        var affectedUsers = await _db.Users
            .Where(user => _db.UserRoles.Any(userRole =>
                userRole.UserId == user.Id && affectedRoleIds.Contains(userRole.RoleId)))
            .ToListAsync();
        foreach (var user in affectedUsers)
            user.SecurityVersion = checked(user.SecurityVersion + 1);

        var childPermissions = await _db.Permissions.Where(p => p.ParentId == id).ToListAsync();
        foreach (var child in childPermissions)
        {
            child.ParentId = null;
        }

        var rolePerms = await _db.RolePermissions.Where(rp => rp.PermissionId == id).ToListAsync();
        _db.RolePermissions.RemoveRange(rolePerms);

        _db.Permissions.Remove(permission);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<UserWithRolesDto>> GetUsersAsync()
    {
        var users = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive)
            .ToListAsync();

        return users.Select(u => new UserWithRolesDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            EmployeeId = u.EmployeeId,
            IsActive = u.IsActive,
            Roles = u.UserRoles.Select(ur => new RoleDto
            {
                Id = ur.Role!.Id,
                RoleName = ur.Role.RoleName,
                IsActive = ur.Role.IsActive
            }).ToList()
        }).ToList();
    }

    public async Task<List<Guid>> GetLinkedEmployeeIdsAsync()
    {
        return await _db.Users
            .AsNoTracking()
            .Where(user => user.EmployeeId.HasValue)
            .Select(user => user.EmployeeId!.Value)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = await _db.Roles
            .Include(role => role.RolePermissions)
            .OrderBy(r => r.RoleName)
            .ToListAsync();

        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            RoleName = r.RoleName,
            Description = r.Description,
            IsSystem = r.IsSystem,
            IsActive = r.IsActive,
            PermissionIds = r.RolePermissions.Select(permission => permission.PermissionId).ToList()
        }).ToList();
    }

    public async Task<RoleDto?> CreateRoleAsync(CreateRoleRequest request)
    {
        var roleName = request.RoleName?.Trim();
        if (string.IsNullOrWhiteSpace(roleName)
            || roleName.Length > 50
            || string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase)
            || await _db.Roles.AnyAsync(role => role.RoleName.ToLower() == roleName.ToLower()))
            return null;
        var requestedPermissionIds = request.PermissionIds.Distinct().ToHashSet();
        var validPermissionIds = (await _db.Permissions
            .Where(permission => permission.IsActive && requestedPermissionIds.Contains(permission.Id))
            .Select(permission => permission.Id)
            .ToListAsync()).ToHashSet();
        if (requestedPermissionIds.Count != validPermissionIds.Count)
            return null;

        var role = new Role
        {
            Id = Guid.NewGuid(),
            RoleName = roleName,
            Description = request.Description?.Trim(),
            IsSystem = false
        };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        return new RoleDto
        {
            Id = role.Id,
            RoleName = role.RoleName,
            Description = role.Description,
            IsSystem = role.IsSystem,
            IsActive = role.IsActive,
            CreatedAt = role.CreatedAt
        };
    }

    public async Task<bool> UpdateRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds)
    {
        var role = await _db.Roles
            .Include(item => item.RolePermissions)
            .SingleOrDefaultAsync(item => item.Id == roleId);
        if (role == null || string.Equals(role.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
            return false;

        var permissions = await _db.Permissions
            .Where(permission => permission.IsActive)
            .Select(permission => new { permission.Id, permission.ParentId })
            .ToListAsync();
        var permissionById = permissions.ToDictionary(permission => permission.Id);
        var requestedIds = permissionIds.ToHashSet();
        if (requestedIds.Any(id => !permissionById.ContainsKey(id)))
            return false;

        foreach (var permission in permissions)
        {
            var parentId = permission.ParentId;
            while (parentId.HasValue)
            {
                if (requestedIds.Contains(parentId.Value))
                {
                    requestedIds.Add(permission.Id);
                    break;
                }
                parentId = permissionById.TryGetValue(parentId.Value, out var parent)
                    ? parent.ParentId
                    : null;
            }
        }

        var affectedUsers = await _db.Users
            .Where(user => _db.UserRoles.Any(userRole =>
                userRole.UserId == user.Id && userRole.RoleId == roleId))
            .ToListAsync();
        foreach (var user in affectedUsers)
            user.SecurityVersion = checked(user.SecurityVersion + 1);

        _db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var permissionId in requestedIds)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                PermissionId = permissionId
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null)
            return false;

        if (role.IsSystem || string.Equals(role.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
            return false;

        var affectedUsers = await _db.Users
            .Where(user => _db.UserRoles.Any(userRole =>
                userRole.UserId == user.Id && userRole.RoleId == id))
            .ToListAsync();
        foreach (var user in affectedUsers)
            user.SecurityVersion = checked(user.SecurityVersion + 1);

        var rolePerms = _db.RolePermissions.Where(rp => rp.RoleId == id);
        _db.RolePermissions.RemoveRange(rolePerms);

        var userRoles = _db.UserRoles.Where(ur => ur.RoleId == id);
        _db.UserRoles.RemoveRange(userRoles);

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();

        return true;
    }

    public Task<List<ThemeDto>> GetThemesAsync()
    {
        var themes = new List<ThemeDto>
        {
            new() { Name = "modern-dark", DisplayName = "Modern Dark", Description = "Dark theme with blue accents", IsDefault = true },
            new() { Name = "light-pro", DisplayName = "Light Pro", Description = "Professional light theme", IsDefault = false,
                Colors = new ThemeColorsDto { Primary = "#1d5cd4", Accent = "#1d5cd4", Background = "#f5f7fa", Surface = "#ffffff", Text = "#1a202c", Border = "#e2e8f0" }},
            new() { Name = "high-contrast", DisplayName = "High Contrast", Description = "High contrast theme for accessibility", IsDefault = false,
                Colors = new ThemeColorsDto { Primary = "#0066ff", Accent = "#ffff00", Background = "#000000", Surface = "#1a1a1a", Text = "#ffffff", Border = "#333333" }},
            new() { Name = "custom", DisplayName = "Custom", Description = "Custom theme", IsDefault = false }
        };

        return Task.FromResult(themes);
    }

    public async Task<UserThemePreferenceDto> GetUserThemeAsync(Guid id)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                return new UserThemePreferenceDto();
            }

            return new UserThemePreferenceDto
            {
                ThemeName = user.ThemeName ?? "modern-dark",
                CustomPrimaryColor = user.CustomPrimaryColor,
                CustomAccentColor = user.CustomAccentColor
            };
        }
        catch (Exception)
        {
            return new UserThemePreferenceDto();
        }
    }

    public async Task<bool> UpdateUserThemeAsync(Guid id, UpdateThemeRequest req)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return false;

            user.ThemeName = req.ThemeName;
            user.CustomPrimaryColor = req.CustomPrimaryColor;
            user.CustomAccentColor = req.CustomAccentColor;

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<List<ModuleAccessDto>> GetRoleModulesAsync(string roleName)
    {
        if (string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
            return BuildModuleAccess(PermissionCatalog.Modules.SelectMany(module =>
                new[] { module.Id, $"{module.Id}:page" }
                    .Concat(PermissionCatalog.Actions.Select(action =>
                        PermissionCatalog.GetActionCode(module.Id, action.Code)))));

        var permissionCodes = await _db.RolePermissions
            .Where(rolePermission => rolePermission.Role.IsActive
                && rolePermission.Role.RoleName == roleName
                && rolePermission.Permission.IsActive)
            .Select(rolePermission => rolePermission.Permission.PermissionCode)
            .ToListAsync();
        return BuildModuleAccess(permissionCodes);
    }

    public async Task<List<ModuleAccessDto>> GetCurrentUserModulesAsync(Guid userId)
    {
        var isAdmin = await _db.UserRoles
            .AnyAsync(userRole => userRole.UserId == userId
                && userRole.Role.IsActive
                && userRole.Role.RoleName.ToLower() == "admin");
        if (isAdmin)
        {
            return BuildModuleAccess(PermissionCatalog.Modules.SelectMany(module =>
                new[] { module.Id, $"{module.Id}:page" }
                    .Concat(PermissionCatalog.Actions.Select(action =>
                        PermissionCatalog.GetActionCode(module.Id, action.Code)))));
        }

        var permissionCodes = await _db.UserRoles
            .Where(userRole => userRole.UserId == userId
                && userRole.Role.IsActive)
            .SelectMany(userRole => userRole.Role.RolePermissions)
            .Where(rolePermission => rolePermission.Permission.IsActive)
            .Select(rolePermission => rolePermission.Permission.PermissionCode)
            .Distinct()
            .ToListAsync();
        return BuildModuleAccess(permissionCodes);
    }

    private static List<ModuleAccessDto> BuildModuleAccess(IEnumerable<string> permissionCodes)
    {
        var codes = permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var access = new List<ModuleAccessDto>();
        foreach (var module in PermissionCatalog.Modules)
        {
            var moduleGranted = codes.Contains(module.Id);
            var modulePageGranted = codes.Contains($"{module.Id}:page");
            var actions = module.IncludeReverse
                ? PermissionCatalog.Actions.Append(("reverse", "沖銷")).ToList()
                : PermissionCatalog.Actions;
            var moduleActions = actions
                .Where(action => moduleGranted
                    || (modulePageGranted && action.Code == "view")
                    || codes.Contains(PermissionCatalog.GetActionCode(module.Id, action.Code)))
                .Select(action => action.Code)
                .ToList();
            var resourceAccess = PermissionCatalog.Resources
                .Where(resource => resource.ModuleId == module.Id)
                .Select(resource =>
                {
                    var resourceGranted = codes.Contains(resource.ModuleId);
                    var resourcePageGranted = codes.Contains(PermissionCatalog.GetResourcePageCode(resource));
                    var resourceActions = actions
                        .Where(action => resourceGranted
                            || (resourcePageGranted && action.Code == "view")
                            || codes.Contains(PermissionCatalog.GetResourceActionCode(resource, action.Code)))
                        .Select(action => action.Code)
                        .ToList();
                    return new ModuleAccessDto
                    {
                        ModuleId = $"{resource.ModuleId}:{resource.Id}",
                        Permissions = resourceActions
                    };
                })
                .Where(resource => resource.Permissions.Count > 0)
                .ToList();
            if (resourceAccess.Count > 0 && !moduleActions.Contains("view", StringComparer.OrdinalIgnoreCase))
                moduleActions.Add("view");
            if (!moduleGranted && moduleActions.Count == 0 && resourceAccess.Count == 0)
                continue;

            access.Add(new ModuleAccessDto
            {
                ModuleId = module.Id,
                Permissions = moduleActions
            });
            access.AddRange(resourceAccess);
        }

        return access;
    }

    private List<PermissionTreeDto> BuildPermissionTree(List<Permission> allPermissions, Guid? parentId)
    {
        return allPermissions
            .Where(p => p.ParentId == parentId)
            .Select(p => new PermissionTreeDto
            {
                Id = p.Id,
                PermissionCode = p.PermissionCode,
                ModuleName = p.ModuleName,
                ActionType = p.ActionType,
                PermissionType = p.PermissionType,
                Description = p.Description,
                SortOrder = p.SortOrder,
                Children = BuildPermissionTree(allPermissions, p.Id)
            })
            .ToList();
    }
}
