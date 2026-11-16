using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/erp/sales-reports")]
public class SalesReportsController : BaseController
{
    private readonly ISalesReportService _reportService;

    public SalesReportsController(ISalesReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// 前端 salesReportsApi.orderToCash 呼叫: GET /api/erp/SalesReports/order-to-cash
    /// </summary>
    [HttpGet("order-to-cash")]
    public async Task<ActionResult<ApiResponse<object>>> GetOrderToCash(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] bool includeAll = false)
    {
        var data = await _reportService.GetOrderToCashAsync(startDate, endDate, includeAll);
        return Ok((object)data);
    }

    /// <summary>
    /// 前端 salesReportsApi.arAging 呼叫: GET /api/erp/SalesReports/ar-aging
    /// </summary>
    [HttpGet("ar-aging")]
    public async Task<ActionResult<ApiResponse<object>>> GetArAging()
    {
        var data = await _reportService.GetArAgingAsync();
        return Ok((object)data);
    }

    /// <summary>
    /// 前端 salesReportsApi.monthlySummary 呼叫: GET /api/erp/SalesReports/monthly-summary
    /// </summary>
    [HttpGet("monthly-summary")]
    public async Task<ActionResult<ApiResponse<object>>> GetMonthlySummary([FromQuery] int? year)
    {
        var data = await _reportService.GetMonthlySummaryAsync(year);
        return Ok((object)data);
    }

    /// <summary>
    /// 前端 salesReportsApi.creditUsage 呼叫: GET /api/erp/SalesReports/credit-usage
    /// </summary>
    [HttpGet("credit-usage")]
    public async Task<ActionResult<ApiResponse<object>>> GetCreditUsage()
    {
        var data = await _reportService.GetCreditUsageAsync();
        return Ok((object)data);
    }

    [HttpGet("monthly-periods")]
    public async Task<ActionResult<ApiResponse<List<MonthlyPeriodStatusDto>>>> GetMonthlyPeriods([FromQuery] int? year)
    {
        var data = await _reportService.GetMonthlyPeriodsAsync(year);
        return Ok(data);
    }
}
