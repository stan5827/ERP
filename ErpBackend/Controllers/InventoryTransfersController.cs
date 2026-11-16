using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/inventory-transfers")]
[Route("api/erp/InventoryTransfers")]
public class InventoryTransfersController : BaseController
{
    private readonly IInventoryTransferService _transferService;

    public InventoryTransfersController(IInventoryTransferService transferService)
    {
        _transferService = transferService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetTransferOrders()
    {
        var items = await _transferService.GetTransferOrdersAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> GetTransferOrder(Guid id)
    {
        var dto = await _transferService.GetTransferOrderAsync(id);
        if (dto == null) return NotFound(ApiHelper.Fail<object>("找不到指定調撥單"));
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateTransferOrder([FromBody] CreateTransferOrderDto model)
    {
        var (dto, status) = await _transferService.CreateTransferOrderAsync(model, GetUserId());
        return status switch
        {
            "ok" => Ok(dto, "Created"),
            "invalid" => BadRequest(ApiHelper.Fail<object>("請求參數不正確")),
            "duplicate" => BadRequest(ApiHelper.Fail<object>("調撥單編號已存在")),
            "not_found" => NotFound(ApiHelper.Fail<object>("倉庫不存在")),
            _ => BadRequest(ApiHelper.Fail<object>("建立調撥單失敗"))
        };
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveTransferOrder(Guid id)
    {
        var (dto, status) = await _transferService.ApproveTransferOrderAsync(id, GetUserId());
        return status switch
        {
            "ok" => Ok(dto, "Approved"),
            "not_found" => NotFound(ApiHelper.Fail<object>("找不到指定調撥單")),
            "invalid" => BadRequest(ApiHelper.Fail<object>("僅可核准狀態為 draft 的調撥單")),
            _ => BadRequest(ApiHelper.Fail<object>("核准調撥單失敗"))
        };
    }

    [HttpPost("{id}/execute")]
    public async Task<ActionResult<ApiResponse<object>>> ExecuteTransferOrder(Guid id)
    {
        var (dto, status) = await _transferService.ExecuteTransferOrderAsync(id, GetUserId());
        return status switch
        {
            "ok" => Ok(dto, "Executed"),
            "not_found" => NotFound(ApiHelper.Fail<object>("找不到指定調撥單")),
            "invalid" => BadRequest(ApiHelper.Fail<object>("僅可執行狀態為 approved 的調撥單")),
            "insufficient_stock" => BadRequest(ApiHelper.Fail<object>("來源倉庫庫存不足，無法執行調撥")),
            _ => BadRequest(ApiHelper.Fail<object>("執行調撥單失敗"))
        };
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> CancelTransferOrder(Guid id)
    {
        var (ok, status) = await _transferService.CancelTransferOrderAsync(id);
        return status switch
        {
            "ok" => Ok<object>("Cancelled"),
            "not_found" => NotFound(ApiHelper.Fail<object>("找不到指定調撥單")),
            "invalid" => BadRequest(ApiHelper.Fail<object>("已完成或已取消的調撥單無法刪除")),
            _ => BadRequest(ApiHelper.Fail<object>("取消調撥單失敗"))
        };
    }
}
