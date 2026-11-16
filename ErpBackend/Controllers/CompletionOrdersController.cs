using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/completion-orders")]
[Route("api/erp/CompletionOrders")]
public class CompletionOrdersController : BaseController
{
    private readonly ICompletionOrderService _completionService;

    public CompletionOrdersController(ICompletionOrderService completionService)
    {
        _completionService = completionService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetCompletionOrders()
    {
        var items = await _completionService.GetCompletionOrdersAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> GetCompletionOrder(Guid id)
    {
        var dto = await _completionService.GetCompletionOrderAsync(id);
        if (dto == null) return NotFound(ApiHelper.Fail<object>("找不到指定完工入庫單"));
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> CreateCompletionOrder([FromBody] CreateCompletionOrderDto model)
    {
        var (dto, status) = await _completionService.CreateCompletionOrderAsync(model, GetUserId());
        return status switch
        {
            "ok" => Ok(dto, "Created"),
            "invalid" => BadRequest(ApiHelper.Fail<object>("請求參數不正確")),
            "duplicate" => BadRequest(ApiHelper.Fail<object>("完工入庫單編號已存在")),
            "not_found" => NotFound(ApiHelper.Fail<object>("倉庫不存在")),
            _ => BadRequest(ApiHelper.Fail<object>("建立完工入庫單失敗"))
        };
    }

    [HttpPost("{id}/complete")]
    public async Task<ActionResult<ApiResponse<object>>> CompleteOrder(Guid id)
    {
        var (dto, status) = await _completionService.CompleteOrderAsync(id, GetUserId());
        return status switch
        {
            "ok" => Ok(dto, "Completed"),
            "not_found" => NotFound(ApiHelper.Fail<object>("找不到指定完工入庫單")),
            "invalid" => BadRequest(ApiHelper.Fail<object>("僅可完成狀態為 draft 的完工入庫單")),
            _ => BadRequest(ApiHelper.Fail<object>("完工入庫失敗"))
        };
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> CancelCompletionOrder(Guid id)
    {
        var (ok, status) = await _completionService.CancelCompletionOrderAsync(id);
        return status switch
        {
            "ok" => Ok<object>("Cancelled"),
            "not_found" => NotFound(ApiHelper.Fail<object>("找不到指定完工入庫單")),
            "invalid" => BadRequest(ApiHelper.Fail<object>("已完成或已取消的完工入庫單無法刪除")),
            _ => BadRequest(ApiHelper.Fail<object>("取消完工入庫單失敗"))
        };
    }

    [HttpGet("workorders/completable")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetCompletableWorkOrders()
    {
        var items = await _completionService.GetCompletableWorkOrdersAsync();
        return Ok(items);
    }
}
