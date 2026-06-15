using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Controllers;

public class AttendanceRecordsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public AttendanceRecordsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

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

        var query = _db.AttendanceRecords
            .Include(a => a.Employee)
            .Include(a => a.Department)
            .AsQueryable();

        if (!string.IsNullOrEmpty(employeeId) && Guid.TryParse(employeeId, out var empGuid))
            query = query.Where(a => a.EmployeeId == empGuid);

        if (!string.IsNullOrEmpty(departmentId) && Guid.TryParse(departmentId, out var deptGuid))
            query = query.Where(a => a.DepartmentId == deptGuid);

        if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            query = query.Where(a => a.Date >= start);

        if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            query = query.Where(a => a.Date <= end);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.ClockIn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(a => new AttendanceRecordDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeName = a.Employee?.ChineseName,
            DepartmentId = a.DepartmentId,
            DepartmentName = a.Department?.DeptName,
            Date = a.Date,
            ShiftType = a.ShiftType,
            ClockIn = a.ClockIn?.ToString("HH:mm"),
            ClockOut = a.ClockOut?.ToString("HH:mm"),
            Status = a.Status,
            LateMinutes = a.LateMinutes,
            EarlyLeaveMinutes = a.EarlyLeaveMinutes,
            Ot1Hours = a.Ot1Hours,
            Ot2Hours = a.Ot2Hours,
            Ot3Hours = a.Ot3Hours,
            ApprovalStatus = a.ApprovalStatus,
            Notes = a.Notes
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AttendanceRecordDto>>> GetAttendanceRecord(Guid id)
    {
        var a = await _db.AttendanceRecords
            .Include(a => a.Employee)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (a == null) return NotFound(ApiHelper.Fail("Record not found"));

        var dto = new AttendanceRecordDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeName = a.Employee?.ChineseName,
            DepartmentId = a.DepartmentId,
            DepartmentName = a.Department?.DeptName,
            Date = a.Date,
            ShiftType = a.ShiftType,
            ClockIn = a.ClockIn?.ToString("HH:mm"),
            ClockOut = a.ClockOut?.ToString("HH:mm"),
            Status = a.Status,
            LateFlag = a.LateFlag,
            LateMinutes = a.LateMinutes,
            EarlyLeaveFlag = a.EarlyLeaveFlag,
            EarlyLeaveMinutes = a.EarlyLeaveMinutes,
            Ot1Hours = a.Ot1Hours,
            Ot2Hours = a.Ot2Hours,
            Ot3Hours = a.Ot3Hours,
            ApprovalStatus = a.ApprovalStatus,
            Notes = a.Notes
        };

        return Ok(dto);
    }

    [HttpPost("clock-in")]
    public async Task<ActionResult<ApiResponse>> ClockIn([FromBody] ClockInRequest req)
    {
        if (!Guid.TryParse(req.EmployeeId, out var empId))
            return BadRequest(ApiHelper.Fail("Invalid employee ID"));

        var now = DateTime.UtcNow;
        var record = new AttendanceRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = empId,
            Date = now.Date,
            ClockIn = now,
            Status = "present",
            LateFlag = now.Hour >= 9,
            LateMinutes = now.Hour >= 9 ? (int)(now - now.Date.AddHours(9)).TotalMinutes : 0
        };

        _db.AttendanceRecords.Add(record);
        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Clocked in");
    }

    [HttpPost("clock-out")]
    public async Task<ActionResult<ApiResponse>> ClockOut([FromBody] ClockOutRequest req)
    {
        if (!Guid.TryParse(req.EmployeeId, out var empId))
            return BadRequest(ApiHelper.Fail("Invalid employee ID"));

        var today = DateTime.UtcNow.Date;
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == empId && a.Date == today);

        if (record == null) return NotFound(ApiHelper.Fail("No clock-in record found"));

        var now = DateTime.UtcNow;
        record.ClockOut = now;
        record.EarlyLeaveFlag = now.Hour < 18;
        record.EarlyLeaveMinutes = now.Hour < 18 ? (int)(now.Date.AddHours(18) - now).TotalMinutes : 0;

        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Clocked out");
    }

    [HttpPost("batch-import")]
    public async Task<ActionResult<ApiResponse>> BatchImport([FromBody] BatchAttendanceRequest req)
    {
        foreach (var item in req.Records)
        {
            var record = new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = item.EmployeeId,
                DepartmentId = item.DepartmentId,
                Date = item.Date,
                ShiftType = item.ShiftType,
                ClockIn = item.ClockIn,
                ClockOut = item.ClockOut,
                Status = item.Status ?? "present",
                LateFlag = item.ClockIn.HasValue && item.ClockIn.Value.Hour >= 9,
                LateMinutes = item.ClockIn.HasValue && item.ClockIn.Value.Hour >= 9 ? 
                    (int)(item.ClockIn.Value - item.ClockIn.Value.Date.AddHours(9)).TotalMinutes : 0,
                ApprovalStatus = "approved"
            };
            _db.AttendanceRecords.Add(record);
        }

        await _db.SaveChangesAsync();
        return ApiResponse.Ok($"{req.Records.Count} records imported");
    }
}

