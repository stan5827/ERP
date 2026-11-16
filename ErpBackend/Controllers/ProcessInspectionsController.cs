using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/quality")]
public class ProcessInspectionsController : BaseController
{
    private readonly IProcessInspectionService _service;

    public ProcessInspectionsController(IProcessInspectionService service) => _service = service;

    [HttpGet("inspection-templates")]
    public async Task<ActionResult<ApiResponse<List<InspectionTemplateDto>>>> GetTemplates(
        [FromQuery] Guid? operationId, [FromQuery] Guid? productSkuId)
        => Ok(await _service.GetTemplatesAsync(operationId, productSkuId));

    [HttpGet("inspection-templates/{id:guid}")]
    public async Task<ActionResult<ApiResponse<InspectionTemplateDto>>> GetTemplate(Guid id)
    {
        var template = await _service.GetTemplateAsync(id);
        return template == null
            ? NotFound(ApiHelper.FailOf<InspectionTemplateDto>("找不到檢驗範本"))
            : Ok(template);
    }

    [HttpPost("inspection-templates")]
    public async Task<ActionResult<ApiResponse<InspectionTemplateDto>>> CreateTemplate(
        [FromBody] SaveInspectionTemplateRequestDto request)
    {
        var (template, status) = await _service.CreateTemplateAsync(request);
        if (status == "ok") return Ok(template!, "Created");
        return TemplateError(status);
    }

    [HttpPut("inspection-templates/{id:guid}")]
    public async Task<ActionResult<ApiResponse<InspectionTemplateDto>>> UpdateTemplate(
        Guid id, [FromBody] SaveInspectionTemplateRequestDto request)
    {
        var (template, status) = await _service.UpdateTemplateAsync(id, request);
        if (status == "ok") return Ok(template!, "Updated");
        if (status == "not_found")
            return NotFound(ApiHelper.FailOf<InspectionTemplateDto>("找不到檢驗範本"));
        return TemplateError(status);
    }

    [HttpGet("work-orders/{workOrderId:guid}/inspection-operations")]
    public async Task<ActionResult<ApiResponse<List<WorkOrderInspectionOperationDto>>>> GetWorkOrderOperations(Guid workOrderId)
    {
        var operations = await _service.GetWorkOrderOperationsAsync(workOrderId);
        return operations == null
            ? NotFound(ApiHelper.FailOf<List<WorkOrderInspectionOperationDto>>("找不到工單"))
            : Ok(operations);
    }

    [HttpGet("process-inspections")]
    public async Task<ActionResult<ApiResponse<List<ProcessInspectionReportDto>>>> GetReports(
        [FromQuery] Guid? workOrderId, [FromQuery] string? lotNumber)
        => Ok(await _service.GetReportsAsync(workOrderId, lotNumber));

    [HttpPost("process-inspections")]
    public async Task<ActionResult<ApiResponse<ProcessInspectionReportDto>>> CreateReport(
        [FromBody] CreateProcessInspectionRequestDto request)
    {
        var (report, status) = await _service.CreateReportAsync(request);
        if (status == "ok") return Ok(report!, "Created");
        if (status is "work_order_not_found" or "operation_not_found" or "template_not_found")
            return NotFound(ApiHelper.FailOf<ProcessInspectionReportDto>(InspectionErrorMessage(status)));
        return BadRequest(ApiHelper.FailOf<ProcessInspectionReportDto>(InspectionErrorMessage(status)));
    }

    private ActionResult<ApiResponse<InspectionTemplateDto>> TemplateError(string status) =>
        BadRequest(ApiHelper.FailOf<InspectionTemplateDto>(status switch
        {
            "duplicate" => "範本代碼與版本已存在",
            "active_template_exists" => "此工藝與產品已有啟用中的檢驗範本，請先停用舊範本",
            "operation_not_found" => "找不到指定工藝",
            "sku_not_found" => "找不到指定產品",
            _ => "檢驗範本資料不完整或格式不正確"
        }));

    private static string InspectionErrorMessage(string status) => status switch
    {
        "work_order_not_found" => "找不到工單",
        "operation_not_found" => "此工單找不到指定工藝",
        "template_not_found" => "檢驗範本不存在或已停用",
        "template_mismatch" => "檢驗範本不適用於此工單工藝或產品",
        "required_item_missing" => "尚有必填檢驗項目未填寫",
        "invalid_numeric_value" => "數值型檢驗項目必須輸入有效數字",
        "invalid_choice" => "選項型檢驗項目的輸入不在範本允許選項中",
        "invalid_result" => "檢驗判定格式不正確",
        _ => "檢驗資料不完整或格式不正確"
    };
}
