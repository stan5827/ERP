using Microsoft.AspNetCore.Authorization;
using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

[Route("api/erp/quality/fqc-inspections")]
public class FqcWorkflowController : BaseController
{
    private readonly IFqcWorkflowService _service;

    public FqcWorkflowController(IFqcWorkflowService service) => _service = service;

    [PermissionAuthorize("quality", "view")]
    [HttpGet("work-orders")]
    public async Task<ActionResult<ApiResponse<List<FqcWorkOrderOptionDto>>>> GetWorkOrderOptions(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
        => Ok(await _service.GetWorkOrderOptionsAsync(search, cancellationToken));

    [PermissionAuthorize("quality", "view")]
    [HttpGet("work-orders/{workOrderId:guid}/default-finished-lot")]
    public async Task<ActionResult<ApiResponse<FqcFinishedLotOptionDto?>>> GetDefaultFinishedLot(
        Guid workOrderId,
        CancellationToken cancellationToken)
        => Ok(await _service.GetDefaultFinishedLotAsync(workOrderId, cancellationToken));

    [PermissionAuthorize("quality", "view")]
    [HttpGet("warehouses")]
    public async Task<ActionResult<ApiResponse<List<FqcWarehouseOptionDto>>>> GetWarehouseOptions(
        CancellationToken cancellationToken)
        => Ok(await _service.GetWarehouseOptionsAsync(cancellationToken));

    [PermissionAuthorize("quality", "view")]
    [HttpGet("warehouses/{warehouseId:guid}/bin-locations")]
    public async Task<ActionResult<ApiResponse<List<FqcBinLocationOptionDto>>>> GetBinLocationOptions(
        Guid warehouseId,
        CancellationToken cancellationToken)
        => Ok(await _service.GetBinLocationOptionsAsync(warehouseId, cancellationToken));

    [PermissionAuthorize("quality", "view")]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FqcInspectionLotDto>>>> GetInspectionLots(
        [FromQuery] Guid? workOrderId,
        CancellationToken cancellationToken)
        => Ok(await _service.GetInspectionLotsAsync(workOrderId, cancellationToken));

    [HttpPost]
    [Authorize]
    [PermissionAuthorize("quality", "create")]
    public async Task<ActionResult<ApiResponse<FqcInspectionLotDto>>> CreateInspectionLot(
        [FromBody] CreateFqcInspectionLotRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateInspectionLotAsync(request, cancellationToken);
            return Ok(result, "FQC 批次已建立，等待整批判定");
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiHelper.FailOf<FqcInspectionLotDto>(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiHelper.FailOf<FqcInspectionLotDto>(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiHelper.FailOf<FqcInspectionLotDto>(exception.Message));
        }
    }

    [HttpPost("{id:guid}/decision")]
    [Authorize]
    [PermissionAuthorize("quality", "approve")]
    public async Task<ActionResult<ApiResponse<FqcInspectionLotDto>>> DecideInspectionLot(
        Guid id,
        [FromBody] DecideFqcInspectionLotRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiHelper.FailOf<FqcInspectionLotDto>("無法識別操作人員"));
            request.DecisionBy = User.Identity?.Name ?? userId.ToString();
            var result = await _service.DecideInspectionLotAsync(id, request, cancellationToken);
            return Ok(result, "FQC 整批判定已儲存");
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiHelper.FailOf<FqcInspectionLotDto>(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiHelper.FailOf<FqcInspectionLotDto>(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiHelper.FailOf<FqcInspectionLotDto>(exception.Message));
        }
    }

    [HttpPost("{id:guid}/rework-work-order")]
    [Authorize]
    [PermissionAuthorize("quality", "approve")]
    public async Task<ActionResult<ApiResponse<FqcInspectionLotDto>>> CreateReworkWorkOrder(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiHelper.FailOf<FqcInspectionLotDto>("無法識別操作人員"));
            var actor = User.Identity?.Name ?? userId.ToString();
            var result = await _service.CreateReworkWorkOrderAsync(id, actor, cancellationToken);
            return Ok(result, "返修工單已建立；隔離庫存仍保留於隔離儲位");
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiHelper.FailOf<FqcInspectionLotDto>(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiHelper.FailOf<FqcInspectionLotDto>(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiHelper.FailOf<FqcInspectionLotDto>(exception.Message));
        }
    }

    [HttpPost("{id:guid}/scrap-note")]
    [Authorize]
    [PermissionAuthorize("quality", "create")]
    public async Task<ActionResult<ApiResponse<FqcScrapNoteDto>>> CreateScrapNote(
        Guid id,
        [FromBody] CreateFqcScrapNoteRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiHelper.FailOf<FqcScrapNoteDto>("無法識別操作人員"));
            var actor = User.Identity?.Name ?? userId.ToString();
            var result = await _service.CreateScrapNoteAsync(
                id, request.ReasonCode, request.Reason, userId, actor, cancellationToken);
            return Ok(result, "報廢申請已建立，等待品保判定；核定流程不會自動異動庫存或產生會計傳票");
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
    }

    [HttpPost("scrap-notes/{scrapRecordId:guid}/resubmit")]
    [Authorize]
    [PermissionAuthorize("quality", "create")]
    public async Task<ActionResult<ApiResponse<FqcScrapNoteDto>>> ResubmitScrapNote(
        Guid scrapRecordId,
        [FromBody] CreateFqcScrapNoteRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiHelper.FailOf<FqcScrapNoteDto>("無法識別操作人員"));
            var actor = User.Identity?.Name ?? userId.ToString();
            var result = await _service.ResubmitFqcScrapNoteAsync(
                scrapRecordId, request.ReasonCode, request.Reason, userId, actor, cancellationToken);
            return Ok(result, "報廢申請已重新送交品保簽核");
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
    }

    [HttpPost("scrap-notes/{scrapRecordId:guid}/quality-review")]
    [Authorize]
    [PermissionAuthorize("quality", "approve")]
    public async Task<ActionResult<ApiResponse<FqcScrapNoteDto>>> ReviewScrapNote(
        Guid scrapRecordId,
        [FromBody] DecideFqcScrapReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiHelper.FailOf<FqcScrapNoteDto>("無法識別操作人員"));
            var actor = User.Identity?.Name ?? userId.ToString();
            var result = await _service.DecideFqcScrapQualityAsync(
                scrapRecordId, userId, actor, request.Approve, request.Comments, cancellationToken);
            return Ok(result, request.Approve ? "品保已完成報廢判定，送交財務核定" : "報廢申請已退回申請人");
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
    }

    [HttpPost("scrap-notes/{scrapRecordId:guid}/finance-approval")]
    [Authorize]
    [PermissionAuthorize("finance", "approve")]
    public async Task<ActionResult<ApiResponse<FqcScrapNoteDto>>> ApproveScrapNote(
        Guid scrapRecordId,
        [FromBody] DecideFqcScrapReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiHelper.FailOf<FqcScrapNoteDto>("無法識別操作人員"));
            var actor = User.Identity?.Name ?? userId.ToString();
            var result = await _service.DecideFqcScrapFinanceAsync(
                scrapRecordId, userId, actor, request.Approve, request.Comments, cancellationToken);
            return Ok(result, request.Approve
                ? "財務已核定；報廢單仍待另行完成實體處理，不會自動扣庫或產生傳票"
                : "報廢申請已退回申請人");
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiHelper.FailOf<FqcScrapNoteDto>(exception.Message));
        }
    }

    [HttpPost("rework-work-orders/{workOrderId:guid}/close")]
    [Authorize]
    [PermissionAuthorize("manufacturing", "approve")]
    public async Task<ActionResult<ApiResponse>> CloseReworkWorkOrder(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiHelper.Fail("無法識別操作人員"));
            var actor = User.Identity?.Name ?? userId.ToString();
            await _service.CloseReworkWorkOrderAsync(workOrderId, actor, cancellationToken);
            return Ok("返修工單已結案；尚未處理的隔離庫存仍維持鎖定");
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiHelper.Fail(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiHelper.Fail(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiHelper.Fail(exception.Message));
        }
    }
}
