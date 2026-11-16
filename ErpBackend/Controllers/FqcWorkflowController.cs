using Microsoft.AspNetCore.Authorization;
using ErpBackend.DTOs;
using ErpBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

[Route("api/erp/quality/fqc-inspections")]
public class FqcWorkflowController : BaseController
{
    private readonly IFqcWorkflowService _service;

    public FqcWorkflowController(IFqcWorkflowService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FqcInspectionLotDto>>>> GetInspectionLots(
        [FromQuery] Guid? workOrderId,
        CancellationToken cancellationToken)
        => Ok(await _service.GetInspectionLotsAsync(workOrderId, cancellationToken));

    [HttpPost]
    [Authorize]
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
            var result = await _service.CreateScrapNoteAsync(id, request.Reason, actor, cancellationToken);
            return Ok(result, "待會計核定報廢單已建立；核定前不會異動庫存或產生會計傳票");
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
}
