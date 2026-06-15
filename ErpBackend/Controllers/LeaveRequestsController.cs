using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Controllers;

public class LeaveRequestsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public LeaveRequestsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<LeaveRequestDto>>>> GetLeaveRequests(
        [FromQuery] string? employeeId,
        [FromQuery] string? status,
        [FromQuery] string? leaveType,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.LeaveRequests
            .Include(l => l.Employee)
            .AsQueryable();

        if (!string.IsNullOrEmpty(employeeId) && Guid.TryParse(employeeId, out var empGuid))
            query = query.Where(l => l.EmployeeId == empGuid);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);

        if (!string.IsNullOrEmpty(leaveType))
            query = query.Where(l => l.LeaveType == leaveType);

        if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            query = query.Where(l => l.StartDate >= start);

        if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            query = query.Where(l => l.EndDate <= end);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.ApplicationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(l => new LeaveRequestDto
        {
            Id = l.Id,
            ApplicationNo = l.ApplicationNo,
            EmployeeId = l.EmployeeId,
            EmployeeName = l.Employee?.ChineseName,
            LeaveType = l.LeaveType,
            ApplicationDate = l.ApplicationDate,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            StartTime = l.StartTime?.ToString(),
            EndTime = l.EndTime?.ToString(),
            TotalHours = l.TotalHours,
            Reason = l.Reason,
            Status = l.Status,
            ApproverId = l.ApproverId,
            ApprovedAt = l.ApprovedAt,
            RejectReason = l.RejectReason,
            Remarks = l.Remarks,
            CreatedAt = l.CreatedAt
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> GetLeaveRequest(Guid id)
    {
        var l = await _db.LeaveRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (l == null) return NotFound(ApiHelper.Fail("Leave request not found"));

        var dto = new LeaveRequestDto
        {
            Id = l.Id,
            ApplicationNo = l.ApplicationNo,
            EmployeeId = l.EmployeeId,
            EmployeeName = l.Employee?.ChineseName,
            LeaveType = l.LeaveType,
            ApplicationDate = l.ApplicationDate,
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            StartTime = l.StartTime?.ToString(),
            EndTime = l.EndTime?.ToString(),
            TotalHours = l.TotalHours,
            Reason = l.Reason,
            AttachmentUrl = l.AttachmentUrl,
            IncludesSpecialPeriod = l.IncludesSpecialPeriod,
            ShiftConflictFlag = l.ShiftConflictFlag,
            AutoDeductPayFlag = l.AutoDeductPayFlag,
            ImpactOtOrBonus = l.ImpactOtOrBonus,
            ApprovalLevel = l.ApprovalLevel,
            Status = l.Status,
            ApproverId = l.ApproverId,
            ApprovedAt = l.ApprovedAt,
            RejectReason = l.RejectReason,
            Remarks = l.Remarks,
            CreatedAt = l.CreatedAt
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LeaveRequestDto>>> CreateLeaveRequest([FromBody] CreateLeaveRequestRequest req)
    {
        var empId = req.EmployeeId;
        if (!Guid.TryParse(empId, out var empGuid))
            return BadRequest(ApiHelper.Fail("Invalid employee ID"));

        var totalHours = (decimal)(req.EndDate - req.StartDate).TotalHours;
        if (req.EndTime.HasValue && req.StartTime.HasValue)
            totalHours += (decimal)(req.EndTime.Value - req.StartTime.Value).TotalHours;

        var leave = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            ApplicationNo = $"LR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
            EmployeeId = empGuid,
            LeaveType = req.LeaveType,
            ApplicationDate = DateTime.UtcNow,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            TotalHours = totalHours,
            Reason = req.Reason,
            AttachmentUrl = req.AttachmentUrl,
            IncludesSpecialPeriod = req.IncludesSpecialPeriod,
            ImpactOtOrBonus = req.ImpactOtOrBonus,
            Status = "pending"
        };

        _db.LeaveRequests.Add(leave);
        await _db.SaveChangesAsync();

        var dto = new LeaveRequestDto
        {
            Id = leave.Id,
            ApplicationNo = leave.ApplicationNo,
            EmployeeId = leave.EmployeeId,
            LeaveType = leave.LeaveType,
            ApplicationDate = leave.ApplicationDate,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            TotalHours = leave.TotalHours,
            Reason = leave.Reason,
            Status = leave.Status,
            CreatedAt = leave.CreatedAt
        };

        return Ok(dto);
    }

    [HttpPatch("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> ApproveLeaveRequest(Guid id, [FromBody] ApproveLeaveRequestRequest req)
    {
        var leave = await _db.LeaveRequests.FindAsync(id);
        if (leave == null) return NotFound(ApiHelper.Fail("Leave request not found"));

        leave.Status = "approved";
        leave.ApproverId = req.ApproverId;
        leave.ApprovedAt = DateTime.UtcNow;
        leave.Remarks = req.Remarks;

        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Leave request approved");
    }

    [HttpPatch("{id}/reject")]
    public async Task<ActionResult<ApiResponse>> RejectLeaveRequest(Guid id, [FromBody] RejectLeaveRequestRequest req)
    {
        var leave = await _db.LeaveRequests.FindAsync(id);
        if (leave == null) return NotFound(ApiHelper.Fail("Leave request not found"));

        leave.Status = "rejected";
        leave.ApproverId = req.ApproverId;
        leave.ApprovedAt = DateTime.UtcNow;
        leave.RejectReason = req.RejectReason;

        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Leave request rejected");
    }

    [HttpGet("leave-types")]
    public ActionResult<ApiResponse<List<string>>> GetLeaveTypes()
    {
        var types = new List<string>
        {
            "annual",     // 特休
            "sick",      // 病假
            "personal",  // 事假
            "maternal",  // 產假
            "paternal",   // 陪產假
            "bereavement", // 喪假
            "marriage",   // 婚假
            "public",     // 公假
            "unpaid"     // 無薪假
        };
        return Ok(types);
    }
}

