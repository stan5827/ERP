using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
public class AttendanceRecordsController : BaseController
{
    private readonly IAttendanceRecordService _attendanceService;

    public AttendanceRecordsController(IAttendanceRecordService attendanceService) => _attendanceService = attendanceService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AttendanceRecordDto>>>> GetAttendanceRecords(
        [FromQuery] string? employeeId,
        [FromQuery] string? departmentId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? status)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var dtos = await _attendanceService.GetAttendanceRecordsAsync(
            employeeId, departmentId, startDate, endDate, status, page, pageSize);
        return Ok(dtos, totalCount: dtos.Count);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AttendanceRecordDto>>> GetAttendanceRecord(Guid id)
    {
        var dto = await _attendanceService.GetAttendanceRecordAsync(id);
        return dto == null ? NotFound(ApiHelper.Fail("Record not found")) : Ok(dto);
    }

    [HttpPost("clock-in")]
    public async Task<ActionResult<ApiResponse>> ClockIn([FromBody] ClockInRequest req)
    {
        var status = await _attendanceService.ClockInAsync(req);
        if (status == "invalid") return BadRequest(ApiHelper.Fail("Invalid employee ID"));
        return ApiResponse.Ok("Clocked in");
    }

    [HttpPost("clock-out")]
    public async Task<ActionResult<ApiResponse>> ClockOut([FromBody] ClockOutRequest req)
    {
        var (found, status) = await _attendanceService.ClockOutAsync(req);
        if (status == "invalid") return BadRequest(ApiHelper.Fail("Invalid employee ID"));
        if (!found) return NotFound(ApiHelper.Fail("No clock-in record found"));
        return ApiResponse.Ok("Clocked out");
    }

    [HttpPost("batch-import")]
    public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchAttendanceRequest req)
    {
        var count = await _attendanceService.BatchImportAsync(req);
        return ApiResponse.Ok($"{count} records imported");
    }
}
