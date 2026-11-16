using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/users")]
[Authorize(Roles = "Admin")]
public class UsersController : BaseController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAll()
    {
        var page = GetPage();
        var pageSize = GetPageSize();
        var search = GetSearch();

        var totalCount = await Task.FromResult(0);
        var dtos = await _userService.GetAllAsync(search, page, pageSize);
        return Ok(dtos, totalCount: dtos.Count);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
    {
        var dto = await _userService.GetByIdAsync(id);
        return dto == null ? NotFound(ApiHelper.Fail("User not found")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiHelper.Fail("Authenticated user identity is required"));

        var (dto, status) = await _userService.CreateAsync(request, actorId);
        return status switch
        {
            "duplicate" => Conflict(ApiHelper.Fail("Username already exists")),
            "employee_required" => BadRequest(ApiHelper.Fail("An employee must be linked to the account")),
            "employee_not_found" => BadRequest(ApiHelper.Fail("Employee does not exist")),
            "employee_inactive" => BadRequest(ApiHelper.Fail("Only active employees can be linked to an account")),
            "employee_already_linked" => Conflict(ApiHelper.Fail("Employee already has an account")),
            "employee_profile_incomplete" => BadRequest(ApiHelper.Fail("Employee must have an identity document, hire date, department, and job title before account provisioning")),
            "employee_department_invalid" => BadRequest(ApiHelper.Fail("Employee must be assigned to an active department before account provisioning")),
            "employee_job_title_invalid" => BadRequest(ApiHelper.Fail("Employee must be assigned to an active job title before account provisioning")),
            "invalid_roles" => BadRequest(ApiHelper.Fail("One or more roles do not exist")),
            "invalid" => BadRequest(ApiHelper.Fail("Username and password are invalid; password must be at least 8 characters and contain both letters and numbers")),
            _ => Ok(dto!, "User created successfully", null)
        };
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult<ApiResponse>> UpdateRoles(Guid id, [FromBody] UpdateUserRolesRequest request)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiHelper.Fail("Authenticated user identity is required"));

        var (found, status) = await _userService.UpdateRolesAsync(id, request, actorId);
        if (!found)
        {
            return status switch
            {
                "not_found" => NotFound(ApiHelper.Fail("User not found")),
                "invalid_roles" => BadRequest(ApiHelper.Fail("Only active roles may be assigned")),
                "reason_required" => BadRequest(ApiHelper.Fail("A reason is required to update roles")),
                "last_admin_required" => Conflict(ApiHelper.Fail("At least one other active Admin account must remain assigned")),
                "actor_required" => Unauthorized(ApiHelper.Fail("Authenticated user identity is required")),
                _ => BadRequest(ApiHelper.Fail("Unable to update user roles"))
            };
        }

        return Ok(status == "unchanged" ? "User roles are unchanged" : "User roles updated");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiHelper.Fail("Authenticated user identity is required"));

        var (found, status) = await _userService.DeleteAsync(id, actorId);
        if (!found) return NotFound(ApiHelper.Fail("User not found"));
        return Ok("User deactivated successfully");
    }

    [HttpPost("{id:guid}/unlock")]
    public async Task<ActionResult<ApiResponse>> Unlock(Guid id)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiHelper.Fail("Authenticated user identity is required"));

        var (found, status) = await _userService.UnlockAsync(id, actorId);
        if (!found) return NotFound(ApiHelper.Fail(status == "not_found" ? "User not found" : "Unable to unlock account"));
        return Ok("Account lockout released");
    }
}
