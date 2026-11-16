using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
public class PayrollController : BaseController
{
    private readonly IPayrollService _payrollService;

    public PayrollController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PayrollDto>>>> GetPayrollRecords(
        [FromQuery] string? employeeId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? status)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var (items, totalCount) = await _payrollService.GetPayrollRecordsAsync(employeeId, startDate, endDate, status, page, pageSize);
        return Ok(items, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PayrollDto>>> GetPayrollRecord(Guid id)
    {
        var dto = await _payrollService.GetPayrollRecordAsync(id);
        if (dto == null) return NotFound(ApiResponse<PayrollDto>.Fail("Payroll record not found"));
        return Ok(dto);
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<PayrollDto>>> CalculatePayroll([FromBody] CalculatePayrollRequest req)
    {
        var (dto, status) = await _payrollService.CalculatePayrollAsync(req);
        if (status == "calculation_not_configured")
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse<PayrollDto>.Fail(
                    "Payroll calculation is unavailable until approved rules and statutory tables are configured"));
        if (dto == null) return BadRequest(ApiResponse<PayrollDto>.Fail("Employee not found"));
        return Ok(dto);
    }

    [HttpPatch("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> ApprovePayroll(Guid id)
    {
        var success = await _payrollService.ApprovePayrollAsync(id);
        if (!success) return NotFound(new ApiResponse { Success = false, Message = "Payroll not found" });
        return new OkObjectResult(new ApiResponse { Success = true, Message = "Payroll approved" });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<PayrollSummaryDto>>> GetPayrollSummary([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
    {
        var summary = await _payrollService.GetPayrollSummaryAsync(periodStart, periodEnd);
        if (summary == null) return NotFound(ApiResponse<PayrollSummaryDto>.Fail("No payroll records found"));
        return Ok(summary);
    }
}
