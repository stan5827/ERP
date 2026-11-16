using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboardData()
    {
        try
        {
            var data = await _dashboardService.GetDashboardDataAsync();
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<DashboardDto>.Fail($"Dashboard error: {ex.Message}"));
        }
    }

    [HttpGet("sidebar-badges")]
    public async Task<ActionResult<ApiResponse<SidebarBadgeDto>>> GetSidebarBadges()
    {
        try
        {
            var badges = await _dashboardService.GetSidebarBadgesAsync();
            return Ok(badges);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<SidebarBadgeDto>.Fail($"Sidebar badges error: {ex.Message}"));
        }
    }
}
