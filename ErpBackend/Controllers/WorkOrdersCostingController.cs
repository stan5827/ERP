using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp")]
[Route("api/v1/erp")]
public class WorkOrdersCostingController : BaseController
{
    private readonly IWorkOrderCostingService _costingService;
    private readonly IWorkOrderCostVarianceService _varianceService;
    private readonly AccountingPolicyOptions _accountingPolicy;

    public WorkOrdersCostingController(
        IWorkOrderCostingService costingService,
        IWorkOrderCostVarianceService varianceService,
        IOptions<AccountingPolicyOptions> accountingPolicy)
    {
        _costingService = costingService;
        _varianceService = varianceService;
        _accountingPolicy = accountingPolicy.Value;
    }

    [Authorize]
    [HttpGet("work-orders-costing/summary")]
    public async Task<IActionResult> GetCostingSummary()
    {
        try
        {
            var wos = await _costingService.GetCostingSummaryAsync();
            return StatusCode(200, ApiResponse<List<WorkOrderCostSummaryDto>>.Ok(wos));
        }
        catch (Exception ex)
        {
            return StatusCode(400, ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize]
    [HttpGet("workorders/{id}/costing/analysis")]
    public async Task<IActionResult> GetCostAnalysis(Guid id)
    {
        try
        {
            var data = await _costingService.GetCostAnalysisAsync(id);
            if (data == null) return NotFound(ApiHelper.Fail("Error"));
            return StatusCode(200, ApiResponse<WorkOrderCostAnalysisDto>.Ok(data));
        }
        catch (Exception ex)
        {
            return StatusCode(400, ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize]
    [HttpGet("workorders/{id:guid}/costing/variance-allocations")]
    public async Task<IActionResult> GetVarianceAllocations(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var allocations = await _varianceService.GetAllocationsAsync(id, cancellationToken);
            return StatusCode(200, ApiResponse<List<WorkOrderCostVarianceAllocationDto>>.Ok(allocations));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize]
    [HttpPost("workorders/{id:guid}/costing/variance-allocations")]
    public async Task<IActionResult> CreateVarianceAllocation(
        Guid id,
        [FromBody] CreateWorkOrderCostVarianceAllocationRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiResponse.Fail("缺少有效的使用者識別"));
        try
        {
            var allocation = await _varianceService.CreateAsync(id, request, actorId, cancellationToken);
            return StatusCode(200, ApiResponse<WorkOrderCostVarianceAllocationDto>.Ok(allocation));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize]
    [HttpPost("workorders/{id:guid}/costing/variance-allocations/{allocationId:guid}/approvals")]
    public async Task<IActionResult> ApproveVarianceAllocation(
        Guid id,
        Guid allocationId,
        [FromBody] ApproveWorkOrderCostVarianceAllocationRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(ApiResponse.Fail("缺少有效的使用者識別"));
        try
        {
            var allocation = await _varianceService.ApproveAsync(
                id, allocationId, request, actorId, GetRoleClaims(), cancellationToken);
            return StatusCode(200, ApiResponse<WorkOrderCostVarianceAllocationDto>.Ok(allocation));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize]
    [HttpPost("workorders/{id}/costing/settle")]
    public async Task<IActionResult> SettleWorkOrderCost(Guid id)
    {
        if (!HasFinancePostingRole())
            return Forbid();
        try
        {
            var (found, status) = await _costingService.SettleWorkOrderCostAsync(id);
            if (!found) return NotFound(ApiHelper.Fail("找不到工單"));
            return status switch
            {
                "ok" => StatusCode(200, ApiResponse.Ok("成本結轉完成")),
                "already_settled" => StatusCode(200, ApiResponse.Ok("工單成本已結轉")),
                "no_actual_cost" => StatusCode(400, ApiResponse.Fail("尚未發生實際成本或尚無完工數，無法結轉")),
                "not_completed" => StatusCode(400, ApiResponse.Fail("工單尚未完工，無法結轉成本")),
                "receipt_pending" => StatusCode(400, ApiResponse.Fail("FQC 批次尚未全數放行或放行數尚未完成入庫，無法結轉成本")),
                "quality_disposition_pending" => StatusCode(400, ApiResponse.Fail("尚有 FQC 隔離品待處置，無法結轉成本")),
                "variance_approval_pending" => StatusCode(400, ApiResponse.Fail("尚有成本差異分攤待簽核，無法結轉成本")),
                "variance_exceeds_actual" => StatusCode(400, ApiResponse.Fail("核准的異常成本分攤超過工單實際成本，無法結轉")),
                _ => StatusCode(400, ApiResponse.Fail($"無法結轉工單成本：{status}"))
            };
        }
        catch (Exception ex)
        {
            return StatusCode(400, ApiResponse.Fail(ex.Message));
        }
    }

    [Authorize]
    [HttpPost("work-orders-costing/batch-settle")]
    public async Task<IActionResult> BatchSettle()
    {
        if (!HasFinancePostingRole())
            return Forbid();
        try
        {
            var msg = await _costingService.BatchSettleAsync();
            return StatusCode(200, ApiResponse.Ok(msg));
        }
        catch (Exception ex)
        {
            return StatusCode(400, ApiResponse.Fail(ex.Message));
        }
    }

    private bool HasFinancePostingRole() =>
        User.IsInRole("Admin")
        || GetRoleClaims().Contains(_accountingPolicy.FinancePostingRole, StringComparer.OrdinalIgnoreCase);

    private IReadOnlyCollection<string> GetRoleClaims() =>
        User.Claims
            .Where(claim => claim.Type == ClaimTypes.Role || claim.Type == "role")
            .Select(claim => claim.Value)
            .ToArray();
}
