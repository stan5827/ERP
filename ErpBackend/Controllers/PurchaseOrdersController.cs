using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class PurchaseOrdersController : BaseController
{
    private readonly IPurchaseOrderService _poService;

    public PurchaseOrdersController(IPurchaseOrderService poService)
    {
        _poService = poService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PurchaseOrderDto>>>> GetPurchaseOrders(
        [FromQuery] string? status,
        [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();
        var (items, totalCount) = await _poService.GetPurchaseOrdersAsync(status, searchTerm, page, pageSize);
        return Ok(items, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> GetPurchaseOrder(Guid id)
    {
        var dto = await _poService.GetPurchaseOrderAsync(id);
        if (dto == null) return NotFound(ApiHelper.Fail("Error"));
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
    {
        try
        {
            var dto = await _poService.CreatePurchaseOrderAsync(request);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiHelper.Fail(ex.Message));
        }
    }

    [HttpPost("{idOrNumber}/confirm")]
    public async Task<ActionResult<ApiResponse>> ConfirmPurchaseOrder(string idOrNumber)
    {
        try
        {
            await _poService.ConfirmPurchaseOrderAsync(idOrNumber);
            return Ok("採購單已確認");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiHelper.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }

    [HttpPost("{idOrNumber}/receive")]
    public async Task<ActionResult<ApiResponse>> ReceivePurchaseOrder(string idOrNumber, [FromBody] ReceivePurchaseOrderRequest? request)
    {
        try
        {
            var message = await _poService.ReceivePurchaseOrderAsync(idOrNumber, request);
            return Ok(message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiHelper.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeletePurchaseOrder(Guid id)
    {
        try
        {
            await _poService.DeletePurchaseOrderAsync(id);
            return Ok("採購單已刪除");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiHelper.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiHelper.Fail(ex.Message));
        }
    }
}
