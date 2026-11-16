using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/mrp")]
public class MrpController : BaseController
{
    private readonly IMrpEngine _mrpEngine;
    private readonly IMrpProcessLinkageService _linkageService;

    public MrpController(IMrpEngine mrpEngine, IMrpProcessLinkageService linkageService)
    {
        _mrpEngine = mrpEngine;
        _linkageService = linkageService;
    }

    [HttpGet("runs")]
    public async Task<ActionResult<ApiResponse<List<MrpRun>>>> GetRuns()
    {
        var runs = await _mrpEngine.GetRunsAsync();
        return Ok(runs);
    }

    [HttpGet("runs/{id}/orders")]
    public async Task<ActionResult<ApiResponse<List<MrpPlannedOrder>>>> GetPlannedOrders(Guid id, [FromQuery] string? search)
    {
        var orders = await _mrpEngine.GetPlannedOrdersAsync(id, search);
        return Ok(orders);
    }

    [HttpPost("run")]
    public async Task<ActionResult<ApiResponse<MrpRun>>> RunMrp([FromBody] MrpRunRequest request)
    {
        var horizonStart = request.HorizonStart ?? DateTime.UtcNow;
        var horizonEnd = request.HorizonEnd ?? horizonStart.AddDays(90);
        var run = await _mrpEngine.RunMrpAsync(horizonStart, horizonEnd, request.RunType ?? "regenerative");
        return Ok(run);
    }

    [HttpPost("orders/{id}/convert")]
    public async Task<dynamic> ConvertOrder(Guid id)
    {
        var result = await _linkageService.ConvertToExecutionAsync(id);
        if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
        return Ok(new { message = result.Message, documentNumber = result.DocumentNumber });
    }

    [HttpGet("summary")]
    public async Task<dynamic> GetSummary()
    {
        var summary = await _mrpEngine.GetMpsSummaryAsync();
        return Ok(summary);
    }
}
