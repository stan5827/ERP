using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.System;

namespace ErpBackend.Controllers;

public class SystemSettingsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public SystemSettingsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet("ping")]
    public ActionResult<ApiResponse<string>> Ping() => Ok<string>("pong");

    [HttpGet("permissions/tree")]
    public async Task<ActionResult<ApiResponse<List<PermissionTreeDto>>>> GetPermissionTree()
    {
        var permissions = await _db.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var tree = BuildPermissionTree(permissions, null);
        return Ok(tree);
    }

    [HttpPost("permissions")]
    public async Task<ActionResult<ApiResponse<PermissionTreeDto>>> CreatePermission([FromBody] CreatePermissionRequest request)
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

        return Ok(new PermissionTreeDto
        {
            Id = permission.Id,
            PermissionCode = permission.PermissionCode,
            ModuleName = permission.ModuleName,
            ActionType = permission.ActionType,
            PermissionType = permission.PermissionType,
            Description = permission.Description,
            SortOrder = permission.SortOrder,
            Children = new List<PermissionTreeDto>()
        });
    }

    [HttpPut("permissions/{id}")]
    public async Task<ActionResult<ApiResponse<PermissionTreeDto>>> UpdatePermission(Guid id, [FromBody] UpdatePermissionRequest request)
    {
        var permission = await _db.Permissions.FindAsync(id);
        if (permission == null)
            return NotFound(ApiHelper.Fail("找不到權限"));

        permission.PermissionCode = request.PermissionCode;
        permission.ModuleName = request.ModuleName;
        permission.ActionType = request.ActionType;
        permission.PermissionType = request.PermissionType;
        permission.Description = request.Description;
        permission.ParentId = request.ParentId;
        permission.SortOrder = request.SortOrder;

        await _db.SaveChangesAsync();

        return Ok(new PermissionTreeDto
        {
            Id = permission.Id,
            PermissionCode = permission.PermissionCode,
            ModuleName = permission.ModuleName,
            ActionType = permission.ActionType,
            PermissionType = permission.PermissionType,
            Description = permission.Description,
            SortOrder = permission.SortOrder,
            Children = new List<PermissionTreeDto>()
        });
    }

    [HttpDelete("permissions/{id}")]
    public async Task<ActionResult<ApiResponse>> DeletePermission(Guid id)
    {
        var permission = await _db.Permissions.FindAsync(id);
        if (permission == null)
            return NotFound(ApiHelper.Fail("找不到權限"));

        var childPermissions = await _db.Permissions.Where(p => p.ParentId == id).ToListAsync();
        foreach (var child in childPermissions)
        {
            child.ParentId = null;
        }

        var rolePerms = await _db.RolePermissions.Where(rp => rp.PermissionId == id).ToListAsync();
        _db.RolePermissions.RemoveRange(rolePerms);

        _db.Permissions.Remove(permission);
        await _db.SaveChangesAsync();

        return ApiResponse.Ok(new object(), "權限已刪除");
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

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<List<UserWithRolesDto>>>> GetUsers()
    {
        var users = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.IsActive)
            .OrderBy(u => u.Username)
            .Select(u => new UserWithRolesDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                Roles = u.UserRoles.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    RoleName = ur.Role.RoleName,
                    Description = ur.Role.Description
                }).ToList()
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("users/{id}/roles")]
    public async Task<ActionResult<ApiResponse>> UpdateUserRoles(Guid id, [FromBody] UpdateUserRolesRequest request)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound(ApiHelper.Fail("找不到使用者"));

        var existingRoles = _db.UserRoles.Where(ur => ur.UserId == id);
        _db.UserRoles.RemoveRange(existingRoles);

        foreach (var roleId in request.RoleIds)
        {
            _db.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = id,
                RoleId = roleId
            });
        }

        await _db.SaveChangesAsync();
        return ApiResponse.Ok(new object(), "使用者角色已更新");
    }

    [HttpGet("roles")]
    public async Task<ActionResult<ApiResponse<List<RoleWithPermissionsDto>>>> GetRoles()
    {
        var roles = await _db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.RoleName)
            .Select(r => new RoleWithPermissionsDto
            {
                Id = r.Id,
                RoleName = r.RoleName,
                Description = r.Description,
                IsSystem = r.IsSystem,
                CreatedAt = r.CreatedAt,
                PermissionIds = r.RolePermissions.Select(rp => rp.PermissionId).ToList()
            })
            .ToListAsync();

        return Ok(roles);
    }

    [HttpPut("roles/{id}/permissions")]
    public async Task<ActionResult<ApiResponse>> UpdateRolePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest request)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            return NotFound(ApiHelper.Fail("找不到角色"));

        var existingPerms = _db.RolePermissions.Where(rp => rp.RoleId == id);
        _db.RolePermissions.RemoveRange(existingPerms);

        foreach (var permId in request.PermissionIds)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = id,
                PermissionId = permId
            });
        }

        await _db.SaveChangesAsync();
        return ApiResponse.Ok(new object(), "角色權限已更新");
    }

    [HttpPost("roles")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            RoleName = request.RoleName,
            Description = request.Description,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        return Ok(new RoleDto
        {
            Id = role.Id,
            RoleName = role.RoleName,
            Description = role.Description
        });
    }

    [HttpDelete("roles/{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteRole(Guid id)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id);
        if (role == null)
            return NotFound(ApiHelper.Fail("找不到角色"));

        if (role.IsSystem)
            return BadRequest(ApiHelper.Fail("系統角色無法刪除"));

        var rolePerms = _db.RolePermissions.Where(rp => rp.RoleId == id);
        _db.RolePermissions.RemoveRange(rolePerms);

        var userRoles = _db.UserRoles.Where(ur => ur.RoleId == id);
        _db.UserRoles.RemoveRange(userRoles);

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();

        return ApiResponse.Ok(new object(), "角色已刪除");
    }

    [HttpGet("themes")]
    public ActionResult<ApiResponse<List<ThemeDto>>> GetThemes()
    {
        var themes = new List<ThemeDto>
        {
            new() { Name = "modern-dark", DisplayName = "現代深色", Description = "目前使用的深色玻璃風格", IsDefault = true },
            new() { Name = "light-pro", DisplayName = "淺色專業", Description = "專業的淺色配色", IsDefault = false, 
                Colors = new ThemeColorsDto { Primary = "#1d5cd4", Accent = "#1d5cd4", Background = "#f5f7fa", Surface = "#ffffff", Text = "#1a202c", Border = "#e2e8f0" }},
            new() { Name = "high-contrast", DisplayName = "高對比", Description = "高對比度黑白配色", IsDefault = false,
                Colors = new ThemeColorsDto { Primary = "#0066ff", Accent = "#ffff00", Background = "#000000", Surface = "#1a1a1a", Text = "#ffffff", Border = "#333333" }},
            new() { Name = "custom", DisplayName = "自訂", Description = "自訂主題顏色", IsDefault = false }
        };

        return Ok(themes);
    }

    [HttpGet("users/{id}/theme")]
    public async Task<ActionResult<ApiResponse<UserThemePreferenceDto>>> GetUserTheme(Guid id)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) 
            {
                return Ok(new UserThemePreferenceDto());
            }

            return Ok(new UserThemePreferenceDto
            {
                ThemeName = user.ThemeName ?? "modern-dark",
                CustomPrimaryColor = user.CustomPrimaryColor,
                CustomAccentColor = user.CustomAccentColor
            });
        }
        catch (Exception)
        {
            return Ok(new UserThemePreferenceDto());
        }
    }

    [HttpPut("users/{id}/theme")]
    public async Task<ActionResult<ApiResponse>> UpdateUserTheme(Guid id, [FromBody] UpdateThemeRequest req)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound(ApiHelper.Fail("使用者不存在"));

            user.ThemeName = req.ThemeName;
            user.CustomPrimaryColor = req.CustomPrimaryColor;
            user.CustomAccentColor = req.CustomAccentColor;

            await _db.SaveChangesAsync();
            return ApiResponse.Ok(new object(), "主題更新成功");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiHelper.Fail($"主題更新失敗: {ex.Message}"));
        }
    }
    
    [HttpGet("role-modules/{roleName}")]
    public ActionResult<ApiResponse<List<ModuleAccessDto>>> GetRoleModules(string roleName)
    {
        var access = new List<ModuleAccessDto>
        {
            new() { ModuleId = "dashboard", Permissions = new List<string> { "read" } },
            new() { ModuleId = "sales", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "procurement", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "inventory", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "manufacturing", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "quality", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "hr", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "finance", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "systemsettings", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "engineering", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "mps", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "rccp", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "mrp", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "uom", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "org", Permissions = new List<string> { "read", "write" } },
            new() { ModuleId = "actual_costing", Permissions = new List<string> { "read", "write" } }
        };
        
        return Ok(access);
    }
}
