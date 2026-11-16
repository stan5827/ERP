using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class PurchaseRequisitionsController : BaseController
{
    private readonly IPurchaseRequisitionService _prService;
    private readonly ApprovalWorkflowService _approvalService;
    private readonly IServiceProvider _serviceProvider;

    public PurchaseRequisitionsController(IPurchaseRequisitionService prService, ApprovalWorkflowService approvalService, IServiceProvider serviceProvider)
    {
        _prService = prService;
        _approvalService = approvalService;
        _serviceProvider = serviceProvider;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PurchaseRequisitionDto>>>> GetPurchaseRequisitions([FromQuery] string? status, [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();
        var (items, totalCount) = await _prService.GetPurchaseRequisitionsAsync(status, searchTerm, page, pageSize);
        return Ok(items, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PurchaseRequisitionDto>>> GetPurchaseRequisition(Guid id)
    {
        var dto = await _prService.GetPurchaseRequisitionAsync(id);
        if (dto == null) return NotFound(ApiHelper.Fail("PR not found"));
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseRequisitionDto>>> CreatePurchaseRequisition([FromBody] CreatePurchaseRequisitionRequest req)
    {
        var dto = await _prService.CreatePurchaseRequisitionAsync(req);
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdatePurchaseRequisition(Guid id, [FromBody] UpdatePurchaseRequisitionRequest req)
    {
        try
        {
            await _prService.UpdatePurchaseRequisitionAsync(id, req);
            return Ok("PR updated");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("PR not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdatePrStatusRequest req)
    {
        try
        {
            await _prService.UpdateStatusAsync(id, req.Status);
            return Ok("Status updated");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("PR not found"));
        }
    }

    [HttpPatch("{id}/lock")]
    public async Task<ActionResult<ApiResponse>> ToggleLock(Guid id)
    {
        try
        {
            await _prService.ToggleLockAsync(id);
            return Ok("PR locked/unlocked");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("PR not found"));
        }
    }

    [HttpPost("{id}/submit")]
    public async Task<ActionResult<ApiResponse>> SubmitPR(Guid id)
    {
        try
        {
            await _prService.SubmitPRAsync(id, _approvalService);
            return Ok("PR submitted for approval");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("PR not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> ApprovePR(Guid id, [FromBody] ApprovalActionRequest req)
    {
        try
        {
            await _prService.ApprovePRAsync(id, req, _approvalService);
            return Ok("PR approved");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("PR not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ApiResponse>> RejectPR(Guid id, [FromBody] ApprovalActionRequest req)
    {
        try
        {
            await _prService.RejectPRAsync(id, req);
            return Ok("PR rejected");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("PR not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/return-for-edit")]
    public async Task<ActionResult<ApiResponse>> ReturnPRForEdit(Guid id, [FromBody] ApprovalActionRequest req)
    {
        try
        {
            await _prService.ReturnPRForEditAsync(id, req);
            return Ok("PR returned for edits");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("PR not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }

    [HttpGet("{id}/approval-chain")]
    public async Task<ActionResult<ApiResponse<ApprovalChainResult>>> GetApprovalChain(Guid id)
    {
        try
        {
            var chain = await _prService.GetApprovalChainAsync(id, _approvalService);
            return Ok(chain);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("PR not found"));
        }
    }

    [HttpGet("{id}/approval-history")]
    public async Task<ActionResult<ApiResponse<List<ApprovalHistoryDto>>>> GetApprovalHistory(Guid id)
    {
        var history = await _prService.GetApprovalHistoryAsync(id);
        return Ok(history);
    }

    [HttpPost("{id}/convert-to-po")]
    public async Task<ActionResult<ApiResponse>> ConvertToPO(Guid id, [FromBody] ConvertToPoRequest req)
    {
        try
        {
            var result = await _prService.ConvertToPOAsync(id, req);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiHelper.Fail("PR not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }

    [HttpGet("budget-check")]
    public async Task<ActionResult<ApiResponse<BudgetOverrunCheck>>> CheckBudget([FromQuery] string costCenter, [FromQuery] decimal amount)
    {
        using var scope = _serviceProvider.CreateScope();
        var budgetService = scope.ServiceProvider.GetRequiredService<IBudgetControlService>();
        var result = await budgetService.CheckOverrunAsync(costCenter, amount);
        return Ok(result);
    }
}

