using Microsoft.AspNetCore.Authorization;
using ErpBackend.DTOs;
using ErpBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

[Route("api/erp/manufacturing/production-receipts")]
public class ProductionReceiptsController : BaseController
{
    private readonly IFqcWorkflowService _service;

    public ProductionReceiptsController(IFqcWorkflowService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProductionReceiptDto>>>> GetProductionReceipts(
        [FromQuery] Guid? workOrderId,
        CancellationToken cancellationToken)
        => Ok(await _service.GetProductionReceiptsAsync(workOrderId, cancellationToken));

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ProductionReceiptDto>>> CreateProductionReceipt(
        [FromBody] CreateProductionReceiptRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized(ApiHelper.FailOf<ProductionReceiptDto>("無法識別操作人員"));
            request.ReceivedBy = User.Identity?.Name ?? userId.ToString();
            var receipt = await _service.CreateProductionReceiptAsync(request, cancellationToken);
            return Ok(receipt, "製造完工入庫已過帳");
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiHelper.FailOf<ProductionReceiptDto>(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiHelper.FailOf<ProductionReceiptDto>(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiHelper.FailOf<ProductionReceiptDto>(exception.Message));
        }
    }
}
