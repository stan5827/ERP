using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/[controller]")]
public class BudgetsController : BaseController
{
    private readonly IBudgetControlService _budgetService;

    public BudgetsController(IBudgetControlService budgetService)
    {
        _budgetService = budgetService;
    }

    // GET /api/erp/budgets/overrun?costCenterCode=...&amount=...
    [HttpGet("overrun")]
    public async Task<ActionResult<ApiResponse<BudgetOverrunCheck>>> CheckOverrun(
        [FromQuery] string costCenterCode,
        [FromQuery] decimal amount)
    {
        var result = await _budgetService.CheckOverrunAsync(costCenterCode, amount);
        return Ok(result);
    }

    // GET /api/erp/budgets/alerts
    [HttpGet("alerts")]
    public async Task<ActionResult<ApiResponse<List<BudgetAlertDto>>>> GetAlerts()
    {
        var alerts = await _budgetService.GetActiveAlertsAsync();
        return Ok(alerts);
    }

    // GET /api/erp/budgets/ping
    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "BudgetsController is operational." });
}
