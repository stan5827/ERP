using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
[Route("api/erp/ess")]
public class EmployeeEssController : BaseController
{
    private readonly IPayrollService _payrollService;
    private readonly IAttendanceRecordService _attendanceRecordService;
    private readonly ILeaveRequestService _leaveRequestService;

    public EmployeeEssController(
        IPayrollService payrollService,
        IAttendanceRecordService attendanceRecordService,
        ILeaveRequestService leaveRequestService)
    {
        _payrollService = payrollService;
        _attendanceRecordService = attendanceRecordService;
        _leaveRequestService = leaveRequestService;
    }

    [HttpGet("payroll")]
    public async Task<ActionResult<ApiResponse<List<EmployeeEssPayrollDto>>>> GetPayroll(CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(ApiHelper.Fail<List<EmployeeEssPayrollDto>>("Employee identity is required."));

        var (items, totalCount) = await _payrollService.GetPayrollRecordsAsync(
            employeeId.ToString(), null, null, "paid", GetPage(), GetPageSize(), cancellationToken);
        var personalPayroll = items.Select(item => new EmployeeEssPayrollDto
        {
            Id = item.Id,
            PeriodStart = item.PeriodStart,
            PeriodEnd = item.PeriodEnd,
            BasicSalary = item.BasicSalary,
            OvertimePay = item.OvertimePay,
            Bonus = item.Bonus,
            AttendanceBonus = item.AttendanceBonus,
            GrossPay = item.GrossPay,
            LaborInsuranceDeduction = item.LaborInsuranceDeduction,
            HealthInsuranceDeduction = item.HealthInsuranceDeduction,
            PensionDeduction = item.PensionDeduction,
            WithholdingTax = item.WithholdingTax,
            TotalDeductions = item.TotalDeductions,
            NetPay = item.NetPay,
            Status = item.Status,
            PaymentDate = item.PaymentDate
        }).ToList();

        return Ok(personalPayroll, totalCount: totalCount);
    }

    [HttpGet("attendance")]
    public async Task<ActionResult<ApiResponse<List<AttendanceRecordDto>>>> GetAttendance(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(ApiHelper.Fail<List<AttendanceRecordDto>>("Employee identity is required."));

        var records = await _attendanceRecordService.GetAttendanceRecordsAsync(
            employeeId.ToString(), startDate: startDate, endDate: endDate,
            page: GetPage(), pageSize: GetPageSize(), cancellationToken: cancellationToken);
        return Ok(records);
    }

    [HttpPost("attendance/clock-in")]
    public async Task<ActionResult<ApiResponse>> ClockIn(
        [FromBody] EmployeeEssClockRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(ApiHelper.Fail("Employee identity is required."));
        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            return BadRequest(ApiHelper.Fail("Valid location coordinates are required"));

        var status = await _attendanceRecordService.ClockInForEmployeeAsync(
            employeeId, request.Latitude!.Value, request.Longitude!.Value, cancellationToken);
        return ClockResult(status, "Clocked in");
    }

    [HttpPost("attendance/clock-out")]
    public async Task<ActionResult<ApiResponse>> ClockOut(
        [FromBody] EmployeeEssClockRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(ApiHelper.Fail("Employee identity is required."));
        if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            return BadRequest(ApiHelper.Fail("Valid location coordinates are required"));

        var status = await _attendanceRecordService.ClockOutForEmployeeAsync(
            employeeId, request.Latitude!.Value, request.Longitude!.Value, cancellationToken);
        return ClockResult(status, "Clocked out");
    }

    [HttpGet("leave-requests")]
    public async Task<ActionResult<ApiResponse<List<LeaveRequestDto>>>> GetLeaveRequests(CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(ApiHelper.Fail<List<LeaveRequestDto>>("Employee identity is required."));

        var requests = await _leaveRequestService.GetAllAsync(
            employeeId: employeeId, page: GetPage(), pageSize: GetPageSize(), cancellationToken: cancellationToken);
        return Ok(requests);
    }

    [HttpGet("leave-requests/{id:guid}")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> GetLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(ApiHelper.Fail<LeaveRequestDto>("Employee identity is required."));

        var request = await _leaveRequestService.GetByIdAsync(id, cancellationToken);
        if (request is null || request.EmployeeId != employeeId)
            return NotFound(ApiHelper.Fail<LeaveRequestDto>("Leave request not found"));

        return Ok(request);
    }

    [HttpPost("leave-requests")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> CreateLeaveRequest(
        [FromBody] EmployeeEssCreateLeaveRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(ApiHelper.FailOf<LeaveRequestDto>("Employee identity is required."));
        if (!request.StartDate.HasValue || !request.EndDate.HasValue || request.EndDate < request.StartDate)
            return BadRequest(ApiHelper.FailOf<LeaveRequestDto>("A valid leave date range is required"));

        var (dto, status) = await _leaveRequestService.CreateAsync(
            new CreateLeaveRequestRequest
            {
                EmployeeId = employeeId.ToString(),
                LeaveType = request.LeaveType,
                StartDate = request.StartDate.Value,
                EndDate = request.EndDate.Value,
                Reason = request.Reason
            },
            cancellationToken);

        if (status == "schedule_not_configured")
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiHelper.FailOf<LeaveRequestDto>(
                    "Leave requests are unavailable until work schedules and holidays are configured"));
        if (status == "not_found")
            return NotFound(ApiHelper.FailOf<LeaveRequestDto>("Employee not found"));
        if (status != "ok" || dto is null)
            return BadRequest(ApiHelper.FailOf<LeaveRequestDto>("Invalid leave request"));

        return CreatedAtAction(
            nameof(GetLeaveRequest),
            new { id = dto.Id },
            ApiResponse<LeaveRequestDto>.Ok(dto, "Leave request created"));
    }

    [HttpGet("leave-balances")]
    public async Task<ActionResult<ApiResponse<List<EmployeeEssLeaveBalanceDto>>>> GetLeaveBalances(
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        if (!TryGetEmployeeId(out var employeeId))
            return Unauthorized(ApiHelper.Fail<List<EmployeeEssLeaveBalanceDto>>("Employee identity is required."));

        var balances = await _leaveRequestService.GetLeaveBalancesAsync(
            employeeId, year ?? DateTime.UtcNow.Year, cancellationToken);
        return Ok(balances);
    }

    private ActionResult<ApiResponse> ClockResult(string status, string successMessage)
    {
        return status switch
        {
            "ok" => Ok(successMessage),
            "employee_not_found" => NotFound(ApiHelper.Fail("Employee not found")),
            "invalid_location" => BadRequest(ApiHelper.Fail("Valid location coordinates are required")),
            "geofence_not_configured" => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiHelper.Fail("Attendance geofence is not configured on the server")),
            "timezone_invalid" => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiHelper.Fail("Attendance time zone configuration is invalid")),
            "outside_geofence" => StatusCode(
                StatusCodes.Status403Forbidden,
                ApiHelper.Fail("You must be within the configured company attendance area")),
            "already_clocked_in" => Conflict(ApiHelper.Fail("An attendance clock-in already exists for today")),
            "already_clocked_out" => Conflict(ApiHelper.Fail("An attendance clock-out already exists for today")),
            "not_found" => NotFound(ApiHelper.Fail("No clock-in record found for today")),
            _ => StatusCode(StatusCodes.Status500InternalServerError, ApiHelper.Fail("Attendance punch failed"))
        };
    }

    private bool TryGetEmployeeId(out Guid employeeId)
        => Guid.TryParse(User.FindFirst("employee_id")?.Value, out employeeId);
}
