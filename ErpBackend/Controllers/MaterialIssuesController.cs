using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class MaterialIssuesController : BaseController
{
    private readonly IMaterialIssueService _materialIssueService;

    public MaterialIssuesController(IMaterialIssueService materialIssueService) => _materialIssueService = materialIssueService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MaterialIssueDto>>>> GetIssues(
        [FromQuery] Guid? workOrderId,
        [FromQuery] string? status)
        => Ok(await _materialIssueService.GetIssuesAsync(workOrderId, status));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MaterialIssueDto>>> GetIssue(Guid id)
    {
        var issue = await _materialIssueService.GetIssueAsync(id);
        return issue == null
            ? NotFound(ApiResponse.Fail("領料單不存在"))
            : Ok(issue);
    }

    [HttpPost("work-orders/{workOrderId:guid}/draft")]
    public async Task<ActionResult<ApiResponse<MaterialIssueDto>>> CreateDraft(Guid workOrderId)
    {
        var (dto, status) = await _materialIssueService.CreateDraftAsync(workOrderId);
        return status switch
        {
            "ok" => Ok(dto!),
            "work_order_not_found" => NotFound(ApiResponse.Fail("工單不存在")),
            "work_order_closed" => BadRequest(ApiResponse.Fail("已完工或結案工單不能建立領料單")),
            "work_order_has_no_materials" => BadRequest(ApiResponse.Fail("工單尚未展開用料，無法建立領料單")),
            "no_open_material_demand" => BadRequest(ApiResponse.Fail("工單用料已領足或短少已結案，沒有待領需求")),
            "work_order_material_sku_not_found" => BadRequest(ApiResponse.Fail("工單用料包含不存在的品號")),
            _ => BadRequest(ApiResponse.Fail(status))
        };
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<ApiResponse<MaterialIssueDto>>> PostIssue(
        Guid id,
        [FromBody] PostMaterialIssueRequestDto request)
    {
        var (dto, status) = await _materialIssueService.PostAsync(id, request, GetUserId());
        if (status == "ok")
            return Ok(dto!, "領料單已過帳");
        return MapIssueError(status);
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/close-shortage")]
    public async Task<ActionResult<ApiResponse<MaterialIssueDto>>> CloseShortage(
        Guid id,
        Guid itemId,
        [FromBody] CloseMaterialIssueShortageRequestDto request)
    {
        var (dto, status) = await _materialIssueService.CloseShortageAsync(id, itemId, request);
        if (status == "ok")
            return Ok(dto!, "欠料已結案");
        return MapIssueError(status);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<MaterialIssueDto>>> CancelDraft(Guid id)
    {
        var (dto, status) = await _materialIssueService.CancelDraftAsync(id);
        if (status == "ok")
            return Ok(dto!, "草稿已取消");
        return MapIssueError(status);
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<ApiResponse<MaterialIssueDto>>> ReverseIssue(Guid id)
    {
        var (dto, status) = await _materialIssueService.ReverseAsync(id, GetUserId());
        if (status == "ok")
            return Ok(dto!, "已建立領料沖銷單");
        return MapIssueError(status);
    }

    /// <summary>
    /// 提交工單領料 (Submit Material Issue)
    /// 扣減庫存並同步更新實際成本
    /// </summary>
    [HttpPost("submit")]
    public async Task<ActionResult<ApiResponse<object>>> SubmitIssue([FromBody] MaterialIssueRequest request)
    {
        var (result, status) = await _materialIssueService.SubmitIssueAsync(request);

        if (status == "ok")
            return Ok(result!);
        return MapIssueError(status);
    }

    private ActionResult MapIssueError(string status)
    {
        var message = status switch
        {
            "not_found" => "領料單不存在",
            "not_found_wo" => "工單不存在",
            "not_found_sku" => "領料品號不存在",
            "default_warehouse_required" => "此品號未設定預設倉庫，請從領料單指定倉庫",
            "no_open_material_demand" => "工單沒有此品號的待領需求",
            "not_draft" => "只有草稿狀態的領料單可以過帳或修改",
            "not_closable" => "只有部分發料的領料單可以結案欠料",
            "not_reversible" => "此狀態的領料單不可沖銷",
            "already_reversed" => "此領料單已沖銷，不可重複沖銷",
            "nothing_to_reverse" => "領料單沒有已過帳數量可沖銷",
            "invalid_quantity" => "實發數量必須大於零",
            "invalid_issue_item" => "領料明細不屬於此單據",
            "issued_exceeds_demand" => "實發數量不可超過本次應發數量",
            "invalid_closure" => "短少結案數量與原因皆為必填",
            "closure_exceeds_shortage" => "結案數量不可超過未領數量",
            "item_not_found" => "領料明細不存在",
            "work_order_material_not_found" => "找不到對應的工單用料快照",
            "draft_has_posted_allocations" => "已有出庫分配的草稿不可取消",
            _ when status.StartsWith("insufficient_stock:", StringComparison.Ordinal)
                => $"品號 {status["insufficient_stock:".Length..]} 可用庫存不足，未進行扣帳",
            _ when status.StartsWith("location_stock_insufficient:", StringComparison.Ordinal)
                => $"品號 {status["location_stock_insufficient:".Length..]} 在指定倉庫、儲位或批號的庫存不足",
            _ when status.StartsWith("warehouse_not_found:", StringComparison.Ordinal)
                => $"品號 {status["warehouse_not_found:".Length..]} 指定的倉庫不存在或已停用",
            _ when status.StartsWith("bin_location_invalid:", StringComparison.Ordinal)
                => $"品號 {status["bin_location_invalid:".Length..]} 的儲位與倉庫不符或已停用",
            _ when status.StartsWith("lot_required:", StringComparison.Ordinal)
                => $"品號 {status["lot_required:".Length..]} 為批號管制品，必須指定批號",
            _ when status.StartsWith("serial_controlled_not_supported:", StringComparison.Ordinal)
                => $"品號 {status["serial_controlled_not_supported:".Length..]} 為序號管制品，請先使用序號領料流程",
            _ => status
        };
        return status is "not_found" or "not_found_wo" or "not_found_sku"
            ? NotFound(ApiResponse.Fail(message))
            : BadRequest(ApiResponse.Fail(message));
    }
}
