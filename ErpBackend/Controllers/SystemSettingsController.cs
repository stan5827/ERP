using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/system-settings")]
[Route("api/erp/systemsettings")]
public class SystemSettingsController : BaseController
{
    private readonly ISystemSettingsService _settingsService;

    public SystemSettingsController(ISystemSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("ping")]
    public ActionResult<ApiResponse<string>> Ping() => Ok<string>("pong");

    [HttpGet("permissions/tree")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<PermissionTreeDto>>>> GetPermissionTree()
    {
        var data = await _settingsService.GetPermissionTreeAsync();
        return Ok(data);
    }

    [HttpPost("permissions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PermissionTreeDto>>> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        var data = await _settingsService.CreatePermissionAsync(request);
        if (data == null)
            return BadRequest(ApiResponse<PermissionTreeDto>.Fail("Failed to create permission"));
        return Ok(data);
    }

    [HttpPut("permissions/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PermissionTreeDto>>> UpdatePermission(Guid id, [FromBody] UpdatePermissionRequest request)
    {
        var data = await _settingsService.UpdatePermissionAsync(id, request);
        if (data == null)
            return NotFound(ApiResponse<PermissionTreeDto>.Fail("Permission not found"));
        return Ok(data);
    }

    [HttpDelete("permissions/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePermission(Guid id)
    {
        var success = await _settingsService.DeletePermissionAsync(id);
        if (!success)
            return new NotFoundObjectResult(new ApiResponse { Success = false, Message = "Permission not found" });
        return new OkObjectResult(new ApiResponse { Success = true, Message = "Permission deleted successfully" });
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<UserWithRolesDto>>>> GetUsers()
    {
        var data = await _settingsService.GetUsersAsync();
        return Ok(data);
    }

    [HttpGet("users/linked-employee-ids")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<Guid>>>> GetLinkedEmployeeIds()
    {
        var data = await _settingsService.GetLinkedEmployeeIdsAsync();
        return Ok(data);
    }

    [HttpGet("roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles()
    {
        var data = await _settingsService.GetRolesAsync();
        return Ok(data);
    }

    [HttpPost("roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleRequest request)
    {
        var role = await _settingsService.CreateRoleAsync(request);
        if (role == null)
            return Conflict(ApiResponse<RoleDto>.Fail("Role name is required, must be unique, and cannot exceed 50 characters"));

        if (request.PermissionIds.Count > 0
            && !await _settingsService.UpdateRolePermissionsAsync(role.Id, request.PermissionIds))
            return new BadRequestObjectResult(ApiResponse<RoleDto>.Fail("One or more permissions do not exist"));

        return Ok(role);
    }

    [HttpPut("roles/{id:guid}/permissions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRolePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest request)
    {
        var success = await _settingsService.UpdateRolePermissionsAsync(id, request.PermissionIds);
        if (!success)
            return BadRequest(new ApiResponse { Success = false, Message = "Role or permission does not exist, or the Admin role cannot be changed" });
        return new OkObjectResult(new ApiResponse { Success = true, Message = "Role permissions updated successfully" });
    }

    [HttpDelete("roles/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
        var success = await _settingsService.DeleteRoleAsync(id);
        if (!success)
            return new NotFoundObjectResult(new ApiResponse { Success = false, Message = "Role not found or is system role" });
        return new OkObjectResult(new ApiResponse { Success = true, Message = "Role deleted successfully" });
    }

    [HttpGet("themes")]
    public async Task<ActionResult<ApiResponse<List<ThemeDto>>>> GetThemes()
    {
        var data = await _settingsService.GetThemesAsync();
        return Ok(data);
    }

    [HttpGet("users/{id}/theme")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserThemePreferenceDto>>> GetUserTheme(Guid id)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();
        if (userId != id && !User.IsInRole("Admin"))
            return Forbid();

        var data = await _settingsService.GetUserThemeAsync(id);
        return Ok(data);
    }

    [HttpPut("users/{id}/theme")]
    [Authorize]
    public async Task<IActionResult> UpdateUserTheme(Guid id, [FromBody] UpdateThemeRequest req)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized();
        if (userId != id && !User.IsInRole("Admin"))
            return Forbid();

        var success = await _settingsService.UpdateUserThemeAsync(id, req);
        if (!success)
            return new NotFoundObjectResult(new ApiResponse { Success = false, Message = "User not found" });
        return new OkObjectResult(new ApiResponse { Success = true, Message = "Theme updated successfully" });
    }

    [HttpGet("role-modules/{roleName}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<List<ModuleAccessDto>>>> GetRoleModules(string roleName)
    {
        var data = await _settingsService.GetRoleModulesAsync(roleName);
        return Ok(data);
    }

    [HttpGet("permissions/me")]
    [Authorize(Policy = "AuthenticatedAccount")]
    public async Task<ActionResult<ApiResponse<List<ModuleAccessDto>>>> GetCurrentUserModules()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
            return Unauthorized(ApiResponse<List<ModuleAccessDto>>.Fail("Authenticated user identity is required"));
        var data = await _settingsService.GetCurrentUserModulesAsync(userId);
        return Ok(data);
    }
}
