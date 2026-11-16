using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
[Route("api/erp/wipreports")]
public class WipReportsController : BaseController
{
    private readonly IWipReportService _wipReportService;

    public WipReportsController(IWipReportService wipReportService) => _wipReportService = wipReportService;

    [Authorize]
    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "WipReportsController restored." });

    [PermissionAuthorize("manufacturing", "execute")]
    [HttpPost("submit")]
    public async Task<ActionResult<ApiResponse<WipReportResultDto>>> ReportCompletion(
        [FromBody] WipReportRequest request,
        CancellationToken cancellationToken)
    {
        if (GetUserId() == Guid.Empty)
            return Unauthorized(ApiHelper.Fail("請先登入系統"));

        var (result, status) = await _wipReportService.ReportCompletionAsync(
            request,
            GetUserId(),
            User.IsInRole("Admin") || User.IsInRole("Production Supervisor"),
            cancellationToken);
        if (status == "not_found") return NotFound(ApiHelper.Fail("找不到指定工單"));
        if (status == "operation_not_found") return BadRequest(ApiHelper.Fail("指定工序不屬於此工單"));
        if (status == "work_order_closed") return BadRequest(ApiHelper.Fail("工單已完工或結案，無法繼續報工"));
        if (status == "invalid_quantities") return BadRequest(ApiHelper.Fail("良品、不良品與報廢數量合計必須大於零，且不可為負數"));
        if (status == "invalid_hours") return BadRequest(ApiHelper.Fail("工時不可為負數"));
        if (status == "invalid_time_range") return BadRequest(ApiHelper.Fail("個人報工必須是一段不超過 24 小時的連續工時"));
        if (status == "quantity_exceeds_plan") return BadRequest(ApiHelper.Fail("本次回報數量超過該工序剩餘計畫數量（含待審報工）"));
        if (status == "fqc_lot_locked") return Conflict(ApiHelper.Fail("該成品批號已建立 FQC 批次，不能再追加最後工序報工"));
        if (status == "defect_reason_required") return BadRequest(ApiHelper.Fail("有不良或報廢數量時，請填寫不良原因代碼"));
        if (status == "invalid_equipment_hours") return BadRequest(ApiHelper.Fail("設備工時須大於零、不可重複指定設備"));
        if (status == "equipment_not_available") return BadRequest(ApiHelper.Fail("所選設備不存在、已停用或目前不可使用"));
        if (status == "equipment_required_for_machine_hours") return BadRequest(ApiHelper.Fail("機台工時必須記錄在獨立的設備使用紀錄中並指定設備"));
        if (status == "equipment_usage_must_be_recorded_separately") return BadRequest(ApiHelper.Fail("設備工時請改由獨立的設備使用紀錄登錄"));
        if (status == "employee_mapping_required") return BadRequest(ApiHelper.Fail("目前帳號尚未綁定在職員工，請聯絡管理員完成帳號與員工對應"));
        if (status == "proxy_not_allowed") return Forbid();
        if (status == "employee_not_available") return BadRequest(ApiHelper.Fail("作業員不存在或已離職"));
        if (status == "invalid_report_type") return BadRequest(ApiHelper.Fail("報工類型只能是個人報工或班組共同報工"));
        if (status == "invalid_shift") return BadRequest(ApiHelper.Fail("請選擇有效班別"));
        if (status == "invalid_team_members") return BadRequest(ApiHelper.Fail("班組成員不可重複"));
        if (status == "invalid_team_member_hours") return BadRequest(ApiHelper.Fail("班組報工須逐一登錄每位成員的人工工時，且工時合計須等於本筆總人工工時"));
        if (status == "team_members_required") return BadRequest(ApiHelper.Fail("班組共同報工至少需要兩位成員，並指定負責人"));
        if (status == "reporter_must_be_team_member") return BadRequest(ApiHelper.Fail("登入的報工人員必須列為班組成員"));
        if (status == "employee_time_overlap") return Conflict(ApiHelper.Fail("報工人員的工作時段與另一筆未退回報工重疊"));
        return Ok(result!, "報工已送出，待主管核准後計入生產進度");
    }

    [HttpPost("{id:guid}/resubmit")]
    public async Task<ActionResult<ApiResponse<WipReportResultDto>>> CorrectAndResubmit(
        Guid id,
        [FromBody] WipReportRequest request,
        CancellationToken cancellationToken)
    {
        if (GetUserId() == Guid.Empty)
            return Unauthorized(ApiHelper.Fail("請先登入系統"));
        var (result, status) = await _wipReportService.CorrectAndResubmitAsync(
            id,
            request,
            GetUserId(),
            cancellationToken);
        if (status == "report_not_found") return NotFound(ApiHelper.Fail("找不到報工紀錄"));
        if (status == "report_not_owned") return Forbid();
        if (status == "report_not_returned") return Conflict(ApiHelper.Fail("只有已退回的報工可以修正後重新送出"));
        if (status == "report_type_immutable" || status == "report_scope_immutable")
            return BadRequest(ApiHelper.Fail("重新送出時不可變更報工類型、工單或工序"));
        if (status == "operation_not_found") return BadRequest(ApiHelper.Fail("報工對應的工序不存在"));
        if (status == "invalid_quantities") return BadRequest(ApiHelper.Fail("良品、不良品與報廢數量合計必須大於零，且不可為負數"));
        if (status == "invalid_hours") return BadRequest(ApiHelper.Fail("工時不可為負數"));
        if (status == "invalid_time_range") return BadRequest(ApiHelper.Fail("個人報工必須是一段不超過 24 小時的連續工時"));
        if (status == "quantity_exceeds_plan") return Conflict(ApiHelper.Fail("修正後數量超過該工序剩餘計畫數量"));
        if (status == "fqc_lot_locked") return Conflict(ApiHelper.Fail("此成品批號已建立 FQC 批次，無法重新送出"));
        if (status == "defect_reason_required") return BadRequest(ApiHelper.Fail("有不良或報廢數量時，請填寫不良原因代碼"));
        if (status == "invalid_shift") return BadRequest(ApiHelper.Fail("請選擇有效班別"));
        if (status == "invalid_team_members") return BadRequest(ApiHelper.Fail("班組成員不可重複，且須保留負責人"));
        if (status == "invalid_team_member_hours") return BadRequest(ApiHelper.Fail("班組報工須逐一登錄每位成員的人工工時，且工時合計須等於本筆總人工工時"));
        if (status == "reporter_must_be_team_member") return BadRequest(ApiHelper.Fail("登入的報工人員必須列為班組成員"));
        if (status == "employee_not_available") return BadRequest(ApiHelper.Fail("班組成員不存在或已離職"));
        if (status == "employee_time_overlap") return Conflict(ApiHelper.Fail("報工人員的工作時段與另一筆未退回報工重疊"));
        if (status == "equipment_usage_must_be_recorded_separately")
            return BadRequest(ApiHelper.Fail("設備工時請改由獨立的設備使用紀錄登錄"));
        return Ok(result!, "修正後報工已重新送出，待主管核准後計入生產進度");
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WipReportQueueItemDto>>>> GetMyReports(
        CancellationToken cancellationToken)
    {
        var reports = await _wipReportService.GetReportsAsync(
            GetUserId(),
            null,
            pendingOnly: false,
            cancellationToken);
        return Ok(reports);
    }

    [Authorize]
    [PermissionAuthorize("manufacturing", "approve")]
    [HttpGet("review-queue")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WipReportQueueItemDto>>>> GetReviewQueue(
        CancellationToken cancellationToken)
    {
        var reports = await _wipReportService.GetReportsAsync(
            GetUserId(),
            null,
            pendingOnly: true,
            cancellationToken);
        return Ok(reports);
    }

    [Authorize]
    [PermissionAuthorize("manufacturing", "approve")]
    [HttpPost("{id:guid}/approve")]
    public Task<ActionResult<ApiResponse<WipReportResultDto>>> Approve(
        Guid id,
        [FromBody] WipReportReviewRequestDto request,
        CancellationToken cancellationToken) =>
        ReviewAsync(id, request, approve: true, cancellationToken);

    [Authorize]
    [PermissionAuthorize("manufacturing", "approve")]
    [HttpPost("{id:guid}/return")]
    public Task<ActionResult<ApiResponse<WipReportResultDto>>> ReturnForCorrection(
        Guid id,
        [FromBody] WipReportReviewRequestDto request,
        CancellationToken cancellationToken) =>
        ReviewAsync(id, request, approve: false, cancellationToken);

    [Authorize]
    [HttpPost("equipment-usage")]
    public async Task<ActionResult<ApiResponse<EquipmentUsageRecordDto>>> CreateEquipmentUsage(
        [FromBody] EquipmentUsageRequestDto request,
        CancellationToken cancellationToken)
    {
        if (GetUserId() == Guid.Empty)
            return Unauthorized(ApiHelper.Fail("請先登入系統"));
        var (result, status) = await _wipReportService.CreateEquipmentUsageAsync(
            request,
            GetUserId(),
            cancellationToken);
        if (status == "not_found") return NotFound(ApiHelper.Fail("找不到指定工單"));
        if (status == "operation_not_found") return BadRequest(ApiHelper.Fail("指定工序不屬於此工單"));
        if (status == "equipment_not_available") return BadRequest(ApiHelper.Fail("設備不存在、已停用或目前不可使用"));
        if (status == "equipment_time_overlap") return Conflict(ApiHelper.Fail("此設備時段已有使用紀錄，請確認是否重複登錄"));
        if (status == "employee_mapping_required") return BadRequest(ApiHelper.Fail("目前帳號尚未綁定在職員工，請聯絡管理員"));
        if (status == "invalid_equipment_usage") return BadRequest(ApiHelper.Fail("設備工時必須大於零，且結束時間須晚於開始時間"));
        if (status == "invalid_shift") return BadRequest(ApiHelper.Fail("請選擇有效班別"));
        return CreatedAtAction(
            nameof(GetEquipmentUsage),
            new { workOrderId = request.WorkOrderId },
            ApiResponse<EquipmentUsageRecordDto>.Ok(result!, "設備使用紀錄已建立"));
    }

    [Authorize]
    [HttpGet("work-orders/{workOrderId:guid}/equipment-usage")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentUsageRecordDto>>>> GetEquipmentUsage(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        var usages = await _wipReportService.GetEquipmentUsageAsync(workOrderId, cancellationToken);
        return Ok(usages);
    }

    private async Task<ActionResult<ApiResponse<WipReportResultDto>>> ReviewAsync(
        Guid id,
        WipReportReviewRequestDto request,
        bool approve,
        CancellationToken cancellationToken)
    {
        var (result, status) = await _wipReportService.ReviewAsync(
            id,
            GetUserId(),
            approve,
            request.Comments,
            cancellationToken);
        if (status == "report_not_found") return NotFound(ApiHelper.Fail("找不到報工紀錄"));
        if (status == "report_not_pending") return Conflict(ApiHelper.Fail("此報工已處理，無法重複審核"));
        if (status == "cannot_review_own_report") return Forbid();
        if (status == "review_reason_required") return BadRequest(ApiHelper.Fail("退回報工時必須填寫原因"));
        if (status == "operation_not_found") return BadRequest(ApiHelper.Fail("報工對應的工序不存在"));
        if (status == "quantity_exceeds_plan") return Conflict(ApiHelper.Fail("核准後會超過工序計畫數量，請先處理其他待審報工"));
        if (status == "fqc_lot_locked") return Conflict(ApiHelper.Fail("此成品批號已建立 FQC 批次，無法核准追加報工"));
        return Ok(result!, approve ? "報工已核准並計入生產進度" : "報工已退回，原紀錄保留");
    }

}
