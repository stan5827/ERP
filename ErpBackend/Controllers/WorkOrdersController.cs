using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Helpers;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
[Route("api/erp/work-orders")]
public class WorkOrdersController : BaseController
{
    private readonly IWorkOrderService _workOrderService;

    public WorkOrdersController(IWorkOrderService workOrderService) => _workOrderService = workOrderService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WorkOrderDto>>>> GetWorkOrders([FromQuery] string? status)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var totalCount = await Task.FromResult(0);
        var items = await _workOrderService.GetAllAsync(status, page, pageSize);

        return Ok(items, totalCount: items.Count);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> GetWorkOrder(Guid id)
    {
        var dto = await _workOrderService.GetByIdAsync(id);
        return dto == null ? NotFound(ApiResponse.Fail("工單不存在")) : Ok(dto);
    }

    [HttpPatch("{id:guid}/status")]
    [HttpPatch("~/api/erp/WorkOrders/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateWorkOrderStatus(
        Guid id,
        [FromBody] UpdateWorkOrderStatusRequestDto request)
    {
        if (!string.Equals(request.Status, "released", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse.Fail("目前只支援發放工單"));

        var (found, status) = await _workOrderService.ReleaseAsync(id);
        if (!found)
            return NotFound(ApiResponse.Fail("工單不存在"));
        if (status == "invalid_transition")
            return Conflict(ApiResponse.Fail("只有計畫中或待發放的工單可以發放"));
        return Ok("工單已發放");
    }

    [HttpPatch("{id:guid}/start")]
    [HttpPatch("~/api/erp/WorkOrders/{id:guid}/start")]
    public async Task<ActionResult<ApiResponse>> StartWorkOrder(Guid id)
    {
        var (found, status) = await _workOrderService.StartAsync(id);
        if (!found)
            return NotFound(ApiResponse.Fail("工單不存在"));
        if (status == "invalid_transition")
            return Conflict(ApiResponse.Fail("只有已發放的工單可以開始生產"));
        return Ok("工單已開始生產");
    }

    [HttpPost("{id:guid}/create-supply-orders")]
    public async Task<ActionResult<ApiResponse<CreateSupplyOrdersResultDto>>> CreateSupplyOrders(Guid id)
    {
        var (found, status, result) = await _workOrderService.CreateSupplyOrdersAsync(id);
        if (!found) return NotFound(ApiResponse.Fail("工單不存在"));
        if (status == "bom_not_found")
            return BadRequest(ApiResponse.Fail("缺料品號沒有有效 BOM，無法建立子工單"));
        if (status == "bom_component_sku_not_found")
            return BadRequest(ApiResponse.Fail("BOM 包含不存在的子件品號"));
        if (status == "bom_cycle")
            return BadRequest(ApiResponse.Fail("BOM 存在循環相依，請先修正 BOM"));
        if (status == "bom_depth_exceeded")
            return BadRequest(ApiResponse.Fail("BOM 展開超過 20 層，請檢查 BOM 結構"));
        if (status.StartsWith("sku_not_purchasable_or_manufacturable:", StringComparison.Ordinal))
        {
            var skuNumber = status["sku_not_purchasable_or_manufacturable:".Length..];
            return BadRequest(ApiResponse.Fail(
                $"缺料品號 {skuNumber} 未設定可採購或可製造，且來源類型未設定為外購或自製。請確認此子件品號設定後再重試"));
        }

        return Ok(result!, "補貨工單與請購需求已建立");
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> CreateWorkOrder([FromBody] CreateWorkOrderRequest req)
    {
        var (dto, status) = await _workOrderService.CreateAsync(req);
        if (status == "not_found") return BadRequest(ApiResponse.Fail("品號不存在"));
        if (status == "bom_component_sku_not_found")
            return BadRequest(ApiResponse.Fail("BOM 包含不存在的子件品號"));
        if (status == "bom_cycle")
            return BadRequest(ApiResponse.Fail("BOM 存在循環相依，請先修正 BOM"));
        if (status == "bom_not_found")
            return BadRequest(ApiResponse.Fail("虛擬件沒有有效 BOM，請先修正 BOM"));
        if (status == "bom_depth_exceeded")
            return BadRequest(ApiResponse.Fail("BOM 展開超過 20 層，請檢查 BOM 結構"));
        return Ok(dto!, "工單已建立");
    }

    [HttpPost("from-sales-order/{salesOrderId:guid}")]
    public async Task<ActionResult<ApiResponse<List<WorkOrderDto>>>> CreateWorkOrdersFromSalesOrder(Guid salesOrderId)
    {
        var (workOrders, status) = await _workOrderService.CreateFromSalesOrderAsync(salesOrderId);
        if (status == "order_not_found")
            return NotFound(ApiHelper.Fail("銷售訂單不存在"));
        if (status == "order_not_confirmed")
            return BadRequest(ApiHelper.Fail("請先確認並核准銷售訂單，再轉發工單"));
        if (status == "no_manufacturing_items")
            return BadRequest(ApiHelper.Fail("此訂單沒有需要內製的品項"));
        if (status == "already_created")
            return Ok(workOrders, "此訂單的內製品項已轉工單，未重複建立");
        if (status.StartsWith("sku_not_found:", StringComparison.Ordinal))
            return BadRequest(ApiHelper.Fail($"訂單第 {GetLineNumber(status)} 項的品號不存在"));
        if (status.StartsWith("bom_not_found:", StringComparison.Ordinal))
            return BadRequest(ApiHelper.Fail($"訂單第 {GetLineNumber(status)} 項沒有有效 BOM，請先建立並啟用 BOM"));
        if (status.StartsWith("bom_component_sku_not_found:", StringComparison.Ordinal))
            return BadRequest(ApiHelper.Fail($"訂單第 {GetLineNumber(status)} 項的 BOM 包含不存在的子件品號"));
        if (status.StartsWith("routing_not_found:", StringComparison.Ordinal))
            return BadRequest(ApiHelper.Fail($"訂單第 {GetLineNumber(status)} 項沒有有效製程路由，請先建立並啟用含工序的路由"));
        if (status.StartsWith("routing_invalid:", StringComparison.Ordinal))
            return BadRequest(ApiHelper.Fail($"訂單第 {GetLineNumber(status)} 項的製程路由包含未啟用的工序或工作中心"));
        if (status.StartsWith("invalid_schedule:", StringComparison.Ordinal))
            return BadRequest(ApiHelper.Fail($"訂單第 {GetLineNumber(status)} 項的完工日期早於開工日期"));
        if (status.StartsWith("sales_uom_not_configured:", StringComparison.Ordinal))
            return BadRequest(ApiHelper.Fail($"訂單第 {GetLineNumber(status)} 項的品號尚未設定銷售單位"));
        if (status.StartsWith("production_uom_not_configured:", StringComparison.Ordinal))
            return BadRequest(ApiHelper.Fail($"訂單第 {GetLineNumber(status)} 項的品號尚未設定生產單位"));

        return Ok(workOrders, "工單已建立並連結至銷售訂單");
    }

    [HttpPatch("{id:guid}/explode-routing")]
    public async Task<ActionResult<ApiResponse>> ExplodeRouting(Guid id)
    {
        var (found, status, operationCount) = await _workOrderService.ExplodeRoutingAsync(id);
        if (!found) return NotFound(ApiHelper.Fail("工單不存在"));
        if (status == "already_exists")
            return Ok("此工單已有工序資料，未重複展開");
        if (status == "routing_not_found")
            return BadRequest(ApiHelper.Fail("此品號沒有啟用中的製程路由"));
        if (status == "routing_empty")
            return BadRequest(ApiHelper.Fail("製程路由尚未設定工序"));
        if (status == "routing_invalid")
            return BadRequest(ApiHelper.Fail("製程路由包含未啟用的工序或工作中心"));

        return Ok($"已建立 {operationCount} 筆工序資料");
    }

    [HttpPut("{id:guid}/operations/order")]
    public async Task<ActionResult<ApiResponse>> ReorderOperations(
        Guid id,
        [FromBody] ReorderWorkOrderOperationsRequestDto request)
    {
        var (found, status) = await _workOrderService.ReorderOperationsAsync(id, request.OperationIds);
        if (!found)
            return NotFound(ApiResponse.Fail("工單不存在"));
        if (status == "invalid_order")
            return BadRequest(ApiResponse.Fail("工序清單必須包含此工單的所有工序，且不可重複"));
        if (status == "has_reports")
            return BadRequest(ApiResponse.Fail("工單已有工序報工紀錄，為避免影響歷史資料，不能重新排序"));
        return Ok("工序順序已更新");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdateWorkOrder(Guid id, [FromBody] UpdateWorkOrderRequest req)
    {
        var (found, status) = await _workOrderService.UpdateAsync(id, req);
        if (!found) return NotFound(ApiResponse.Fail("工單不存在"));
        return Ok("工單已更新");
    }

    [HttpPost("recalculate-materials")]
    [PermissionAuthorize("manufacturing", "update")]
    public async Task<ActionResult<ApiResponse>> RecalculateMaterials(
        [FromBody] RecalculateWorkOrderMaterialsRequestDto request)
    {
        var status = await _workOrderService.RecalculateMaterialsAsync(
            request.WorkOrderIds,
            request.PlannedQty);
        if (status == "ok")
            return Ok("工單計畫數量與用料需求已重算，舊草稿已取消並建立新草稿；核對後再過帳");

        var message = status switch
        {
            "invalid_planned_qty" => "計畫生產數量必須大於零",
            "invalid_work_order_ids" => "請提供不重複且至少一筆工單編號",
            "not_found" => "指定工單不存在，未更新任何工單",
            "work_order_not_editable" => "僅計畫中、待發放或已發放且尚未開始的工單可重算",
            "work_order_has_progress" => "工單已有生產或完工數量，未重算用料",
            "material_issue_history" => "工單已有領料/退料/短少結案歷史，未重算用料",
            "draft_has_posted_allocations" => "領料草稿已有庫存分配，未重算用料",
            "bom_not_found" => "找不到工單綁定的 BOM，未更新任何工單",
            "bom_composition_changed" => "BOM 用料品項或單位與工單快照不同，需人工覆核後再處理",
            "no_open_material_demand" => "BOM 沒有可領用料需求，未更新任何工單",
            "bom_component_sku_not_found" => "BOM 含不存在的用料品號，未更新任何工單",
            "bom_cycle" => "BOM 存在循環相依，未更新任何工單",
            "bom_depth_exceeded" => "BOM 展開超過允許層數，未更新任何工單",
            _ => status
        };
        return status switch
        {
            "not_found" => NotFound(ApiResponse.Fail(message)),
            "invalid_planned_qty" or "invalid_work_order_ids" => BadRequest(ApiResponse.Fail(message)),
            _ => Conflict(ApiResponse.Fail(message))
        };
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteWorkOrder(Guid id)
    {
        var (found, status) = await _workOrderService.DeleteAsync(id);
        if (!found) return NotFound(ApiResponse.Fail("工單不存在"));
        if (status == "guarded") return BadRequest(ApiResponse.Fail("只能刪除草稿狀態的工單"));
        return Ok("工單已刪除");
    }

    private static string GetLineNumber(string status)
        => status[(status.LastIndexOf(':') + 1)..];
}
