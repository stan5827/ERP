using Microsoft.AspNetCore.Mvc;
using ErpBackend.Services;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/aps")]
public class ApsIntegrationController : ControllerBase
{
    private readonly IApsIntegrationService _apsIntegrationService;

    public ApsIntegrationController(IApsIntegrationService apsIntegrationService)
    {
        _apsIntegrationService = apsIntegrationService;
    }

    /// <summary>
    /// 匯出 APS 引擎所需的排程基礎資料
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportData([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var result = await _apsIntegrationService.ExportDataAsync(fromDate, toDate);
        return Ok(result);
    }

    /// <summary>
    /// 接收 APS 算出的詳細排程結果並更新工單與工序
    /// </summary>
    [HttpPost("import-schedule")]
    public async Task<IActionResult> ImportSchedule([FromBody] ApsImportScheduleRequest request)
    {
        var result = await _apsIntegrationService.ImportScheduleAsync(request);

        if (result is IDictionary<string, object> dict && dict.TryGetValue("code", out var codeObj) && codeObj is int code)
        {
            return code switch
            {
                200 => Ok(result),
                400 => BadRequest(result),
                _ => StatusCode(500, result)
            };
        }

        return Ok(result);
    }
}
