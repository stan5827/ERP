using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class DeliveryOrdersController : BaseController
{
    private readonly IDeliveryOrderService _deliveryOrderService;
    private readonly IProcessInspectionService _processInspectionService;

    public DeliveryOrdersController(
        IDeliveryOrderService deliveryOrderService,
        IProcessInspectionService processInspectionService)
    {
        _deliveryOrderService = deliveryOrderService;
        _processInspectionService = processInspectionService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DeliveryOrderDto>>>> GetDeliveryOrders([FromQuery] string? status, [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var dtos = await _deliveryOrderService.GetDeliveryOrdersAsync(status, searchTerm, page, pageSize);
        return Ok(dtos, totalCount: dtos.Count);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DeliveryOrderDto>>> GetDeliveryOrder(Guid id)
    {
        var dto = await _deliveryOrderService.GetDeliveryOrderAsync(id);
        return dto == null ? NotFound(ApiHelper.Fail("Delivery order not found")) : Ok(dto);
    }

    [HttpGet("{id:guid}/inspection-report")]
    public async Task<ActionResult<ApiResponse<DeliveryInspectionReportDto>>> GetInspectionReport(Guid id)
    {
        var report = await _processInspectionService.GetDeliveryReportAsync(id);
        return report == null
            ? NotFound(ApiHelper.FailOf<DeliveryInspectionReportDto>("找不到出貨單"))
            : Ok(report);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DeliveryOrderDto>>> CreateDeliveryOrder([FromBody] CreateDeliveryOrderRequest req)
    {
        var (dto, status) = await _deliveryOrderService.CreateDeliveryOrderAsync(req);
        if (status != "ok")
            return status == "sales_order_not_found"
                ? BadRequest(ApiHelper.Fail("Sales order not found"))
                : BadRequest(ApiHelper.Fail($"Delivery order rejected: {status}"));
        return Ok(dto!);
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdateDeliveryStatusRequest req)
    {
        var (found, status) = await _deliveryOrderService.UpdateStatusAsync(id, req);
        if (!found) return NotFound(ApiHelper.Fail("Delivery order not found"));
        if (status == "guarded") return BadRequest(ApiHelper.Fail("Completed deliveries cannot be changed, and only pending/cancelled are allowed"));
        return ApiResponse.Ok("Status updated");
    }

    [HttpPatch("{id}/sign")]
    public async Task<ActionResult<ApiResponse>> SignDelivery(Guid id, [FromBody] SignDeliveryRequest req)
    {
        var (found, status) = await _deliveryOrderService.SignDeliveryAsync(id, req);
        if (!found) return NotFound(ApiHelper.Fail("Delivery order not found"));
        if (status != "ok")
        {
            var message = status.StartsWith("account_mapping_missing:", StringComparison.Ordinal)
                ? "尚未設定有效的銷貨成本與成品存貨科目映射，禁止簽收出貨"
                : status == "lot_cost_unavailable"
                    ? "指定批號缺少完整的實際入庫成本資料，禁止出貨簽收"
                    : $"Delivery signing rejected: {status}";
            return BadRequest(ApiHelper.Fail(message));
        }
        return ApiResponse.Ok("Delivery signed");
    }
}
