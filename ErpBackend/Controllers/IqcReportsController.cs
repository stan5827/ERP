using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/quality/[controller]")]
public class IqcReportsController : BaseController
{
    private readonly IIqcReportService _iqcReportService;

    public IqcReportsController(IIqcReportService iqcReportService)
    {
        _iqcReportService = iqcReportService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<IqcReportDto>>>> GetReports([FromQuery] string? result)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var (items, totalCount) = await _iqcReportService.GetReportsAsync(result, page, pageSize);

        return Ok(items, totalCount: totalCount);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IqcInspectionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IqcInspectionResponseDto>>> GetInspectionAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var inspection = await _iqcReportService.GetInspectionAsync(id, cancellationToken);
        return inspection is null
            ? NotFound(ApiResponse.Fail("找不到 IQC 檢驗單"))
            : Ok(inspection);
    }

    [Authorize]
    [HttpPost("{id:guid}/decision")]
    [ProducesResponseType(typeof(ApiResponse<IqcInspectionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<IqcInspectionResponseDto>>> CompleteInspectionAsync(
        Guid id,
        [FromBody] CompleteIqcInspectionRequestDto request,
        CancellationToken cancellationToken)
    {
        var inspectorId = GetUserId();
        if (inspectorId == Guid.Empty)
            return Unauthorized(ApiResponse.Fail("無法識別檢驗人員"));

        try
        {
            var result = await _iqcReportService.CompleteInspectionAsync(
                id, request, inspectorId, cancellationToken);
            return Ok(result, "IQC 檢驗結果已記錄");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Fail(ex.Message));
        }
    }
}