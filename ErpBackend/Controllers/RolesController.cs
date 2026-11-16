using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize(Roles = "Admin")]
public class RolesController : BaseController
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService) => _roleService = roleService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RoleSummaryDto>>>> GetActiveRoles()
        => Ok(await _roleService.GetActiveRolesAsync());

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "RolesController is currently being repaired." });
}
