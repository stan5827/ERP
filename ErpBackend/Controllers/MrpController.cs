using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Services;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/mrp")]
public class MrpController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    private readonly IMrpEngine _mrpEngine;
    private readonly IMrpProcessLinkageService _linkageService;

    public MrpController(ErpGeneralDbContext db, IMrpEngine mrpEngine, IMrpProcessLinkageService linkageService)
    {
        _db = db;
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
        var query = _db.MrpPlannedOrders
            .Include(o => o.Sku)
            .Where(o => o.MrpRunId == id);
        
        // 修復 FE-001: Server-side 搜尋
        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(o => 
                o.Sku!.SkuNumber.ToLower().Contains(searchLower) ||
                o.Sku!.SkuName.ToLower().Contains(searchLower));
        }
        
        var orders = await query
            .OrderBy(o => o.PlannedStartDate)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpPost("run")]
    public async Task<ActionResult<ApiResponse<MrpRun>>> RunMrp([FromBody] MrpRunRequest request)
    {
        var horizonStart = ToUtc(request.HorizonStart ?? DateTime.UtcNow);
        var horizonEnd = ToUtc(request.HorizonEnd ?? horizonStart.AddDays(90));
        
        var run = await _mrpEngine.RunMrpAsync(horizonStart, horizonEnd, request.RunType ?? "regenerative");
        return Ok(run);
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    [HttpPost("orders/{id}/convert")]
    public async Task<dynamic> ConvertOrder(Guid id)
    {
        var result = await _linkageService.ConvertToExecutionAsync(id);
        if (!result.Success) return BadRequest(ApiResponse.Fail(result.Message));
        
        return Ok(new { 
            message = result.Message, 
            documentNumber = result.DocumentNumber 
        });
    }

    [HttpGet("summary")]
    public async Task<dynamic> GetSummary()
    {
        var latestRun = await _mrpEngine.GetLatestRunAsync();
        if (latestRun == null) return Ok(new { hasRuns = false });

        var plannedOrders = await _db.MrpPlannedOrders
            .Where(o => o.MrpRunId == latestRun.Id)
            .GroupBy(o => o.OrderType)
            .Select(g => new { Type = g.Key, Count = g.Count(), Qty = g.Sum(x => x.OrderQty) })
            .ToListAsync();

        return Ok(new {
            hasRuns = true,
            latestRunNo = latestRun.RunNumber,
            completedAt = latestRun.CompletedAt,
            plannedOrders
        });
    }
}

public class MrpRunRequest
{
    public DateTime? HorizonStart { get; set; }
    public DateTime? HorizonEnd { get; set; }
    public string? RunType { get; set; }
}
