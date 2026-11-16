using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/erp/sales-orders")]
public class SalesOrdersController : BaseController
{
    private readonly ISalesOrderService _salesOrderService;

    public SalesOrdersController(ISalesOrderService salesOrderService) => _salesOrderService = salesOrderService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SalesOrderDto>>>> GetSalesOrders(
        [FromQuery] string? status,
        [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var dtos = await _salesOrderService.GetSalesOrdersAsync(status, searchTerm, page, pageSize);
        return Ok(dtos, totalCount: dtos.Count);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> GetSalesOrder(Guid id)
    {
        var dto = await _salesOrderService.GetSalesOrderAsync(id);
        return dto == null ? NotFound(ApiHelper.Fail("Error")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> CreateSalesOrder([FromBody] CreateSalesOrderRequest request)
    {
        var dto = await _salesOrderService.CreateSalesOrderAsync(request);
        return Ok(dto);
    }

    [HttpPost("{idOrNumber}/confirm")]
    public async Task<ActionResult<ApiResponse<SalesOrderConfirmationDto>>> ConfirmSalesOrder(string idOrNumber)
    {
        var (found, status, result) = await _salesOrderService.ConfirmSalesOrderAsync(idOrNumber);
        if (!found) return NotFound(ApiHelper.Fail("找不到訂單"));
        if (status == "invalid_state") return BadRequest(ApiHelper.Fail("訂單狀態必須為 pending 才能確認"));
        return Ok(result!, "訂單已確認");
    }

    [HttpPatch("{id}/manufacturing-status")]
    public async Task<ActionResult<ApiResponse>> UpdateManufacturingStatus(Guid id, [FromBody] UpdateManufacturingStatusRequest request)
    {
        var (found, status) = await _salesOrderService.UpdateManufacturingStatusAsync(id, request);
        if (!found) return NotFound(ApiHelper.Fail("找不到訂單"));
        if (status == "invalid") return BadRequest(ApiHelper.Fail("缺少製造狀態值"));
        return Ok($"製造狀態已更新為 {request.NewStatus ?? request.Status}");
    }

    [HttpPost("{id}/status")]
    public async Task<object> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var (found, error, status) = await _salesOrderService.UpdateStatusAsync(id, request);
        if (!found) return NotFound(new { error = "Error" });
        if (status == "blocked") return BadRequest(new { error });
        return Ok(new { message = $"狀態已更新為 {request.NewStatus}" });
    }
}
