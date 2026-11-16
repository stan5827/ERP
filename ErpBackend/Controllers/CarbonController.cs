using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/[controller]")]
public class CarbonController : BaseController
{
    private readonly ICarbonAccountingService _carbonService;

    public CarbonController(ICarbonAccountingService carbonService)
    {
        _carbonService = carbonService;
    }

    // GET /api/erp/carbon/dashboard?year=2024
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<CarbonDashboardDto>>> Dashboard([FromQuery] int year)
    {
        var dto = await _carbonService.GetDashboardAsync(year);
        return Ok(dto);
    }

    // GET /api/erp/carbon/records?scope=scope1&year=2024
    [HttpGet("records")]
    public async Task<ActionResult<ApiResponse<List<CarbonEmissionRecordDto>>>> Records(
        [FromQuery] string? scope,
        [FromQuery] int year)
    {
        var list = await _carbonService.GetEmissionRecordsAsync(scope ?? string.Empty, year);
        return Ok(list);
    }

    // GET /api/erp/carbon/summary?year=2024
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<CarbonSummaryDto>>> Summary([FromQuery] int year)
    {
        var dto = await _carbonService.CalculateSummaryAsync(year);
        return Ok(dto);
    }

    // GET /api/erp/carbon/intensity?year=2024
    [HttpGet("intensity")]
    public async Task<ActionResult<ApiResponse<CarbonIntensityDto>>> Intensity([FromQuery] int year)
    {
        var dto = await _carbonService.CalculateIntensityAsync(year);
        return Ok(dto);
    }

    // GET /api/erp/carbon/ping
    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "CarbonController is operational." });
}
