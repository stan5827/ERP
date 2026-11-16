using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;
using ErpBackend.Models.MES;

namespace ErpBackend.Controllers;

[Route("api/erp/[controller]")]
public class KpisController : BaseController
{
    private readonly IKpiService _kpiService;

    public KpisController(IKpiService kpiService)
    {
        _kpiService = kpiService;
    }

    // Keep the singular route for clients that still use the original endpoint.
    [HttpGet("dashboard")]
    [HttpGet("/api/erp/kpi/dashboard")]
    public async Task<ActionResult<ApiResponse<KpiDashboardDto>>> Dashboard(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var dto = await _kpiService.GetDashboardAsync(fromDate, toDate);
        return Ok(dto);
    }

    // GET /api/erp/kpis/definitions
    [HttpGet("definitions")]
    public async Task<ActionResult<ApiResponse<List<KpiDefinitionDto>>>> GetDefinitions()
    {
        var list = await _kpiService.GetKpiDefinitionsAsync();
        var dtos = list.Select(d => new KpiDefinitionDto
        {
            Id = d.Id,
            KpiCode = d.KpiCode,
            KpiName = d.KpiName,
            Description = d.Description,
            Category = d.Category,
            Formula = d.Formula,
            Unit = d.Unit,
            TargetValue = d.TargetValue,
            WarningThreshold = d.WarningThreshold,
            CriticalThreshold = d.CriticalThreshold,
            CalculationFrequency = d.CalculationFrequency,
            IsActive = d.IsActive,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();
        return Ok(dtos);
    }

    // GET /api/erp/kpis/definitions/{id}
    [HttpGet("definitions/{id:guid}")]
    public async Task<ActionResult<ApiResponse<KpiDefinitionDto>>> GetDefinition(Guid id)
    {
        var list = await _kpiService.GetKpiDefinitionsAsync();
        var d = list.FirstOrDefault(x => x.Id == id);
        if (d == null) return NotFound(ApiHelper.Fail<KpiDefinitionDto>("KPI definition not found"));
        return Ok(new KpiDefinitionDto
        {
            Id = d.Id,
            KpiCode = d.KpiCode,
            KpiName = d.KpiName,
            Description = d.Description,
            Category = d.Category,
            Formula = d.Formula,
            Unit = d.Unit,
            TargetValue = d.TargetValue,
            WarningThreshold = d.WarningThreshold,
            CriticalThreshold = d.CriticalThreshold,
            CalculationFrequency = d.CalculationFrequency,
            IsActive = d.IsActive,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        });
    }

    // POST /api/erp/kpis/definitions
    [HttpPost("definitions")]
    public async Task<ActionResult<ApiResponse<KpiDefinitionDto>>> CreateDefinition([FromBody] KpiDefinitionUpsertDto req)
    {
        var definition = new KpiDefinition
        {
            KpiCode = req.KpiCode,
            KpiName = req.KpiName,
            Description = req.Description,
            Category = req.Category ?? "efficiency",
            Formula = req.Formula,
            Unit = req.Unit ?? "%",
            TargetValue = req.TargetValue,
            WarningThreshold = req.WarningThreshold,
            CriticalThreshold = req.CriticalThreshold,
            CalculationFrequency = req.CalculationFrequency ?? "daily",
            IsActive = req.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _kpiService.CreateKpiDefinitionAsync(definition);
        return Ok(new KpiDefinitionDto
        {
            Id = created.Id,
            KpiCode = created.KpiCode,
            KpiName = created.KpiName,
            Description = created.Description,
            Category = created.Category,
            Formula = created.Formula,
            Unit = created.Unit,
            TargetValue = created.TargetValue,
            WarningThreshold = created.WarningThreshold,
            CriticalThreshold = created.CriticalThreshold,
            CalculationFrequency = created.CalculationFrequency,
            IsActive = created.IsActive,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        }, "KPI definition created");
    }

    // GET /api/erp/kpis/oee?workCenterId=...&date=...&shift=...
    [HttpGet("oee")]
    public async Task<ActionResult<ApiResponse<OeeResultDto>>> Oee(
        [FromQuery] Guid workCenterId,
        [FromQuery] DateTime date,
        [FromQuery] int? shift)
    {
        var result = await _kpiService.CalculateOeeAsync(workCenterId, date, shift);
        return Ok(result);
    }

    // GET /api/erp/kpis/otd?fromDate=...&toDate=...
    [HttpGet("otd")]
    public async Task<ActionResult<ApiResponse<OtdResultDto>>> Otd(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var result = await _kpiService.CalculateOtdAsync(fromDate, toDate);
        return Ok(result);
    }

    // GET /api/erp/kpis/wip
    [HttpGet("wip")]
    public async Task<ActionResult<ApiResponse<WipResultDto>>> Wip()
    {
        var result = await _kpiService.CalculateWipAsync();
        return Ok(result);
    }

    // GET /api/erp/kpis/trend?kpiCode=...&fromDate=...&toDate=...&periodType=day
    [HttpGet("trend")]
    public async Task<ActionResult<ApiResponse<List<KpiTrendDto>>>> Trend(
        [FromQuery] string kpiCode,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] string periodType = "day")
    {
        var list = await _kpiService.GetKpiTrendAsync(kpiCode, fromDate, toDate, periodType);
        return Ok(list);
    }

    // GET /api/erp/kpis/ping
    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "KpisController is operational." });
}
