using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Controllers;

public class PayrollController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public PayrollController(ErpGeneralDbContext db)
    {
        _db = db;
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

        var query = _db.Payrolls
            .Include(p => p.Employee)
            .AsQueryable();

        if (!string.IsNullOrEmpty(employeeId) && Guid.TryParse(employeeId, out var empGuid))
            query = query.Where(p => p.EmployeeId == empGuid);

        if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            query = query.Where(p => p.PeriodStart >= start);

        if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            query = query.Where(p => p.PeriodEnd <= end);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.PeriodStart)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(p => new PayrollDto
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EmployeeName = p.Employee?.ChineseName,
            PeriodStart = p.PeriodStart,
            PeriodEnd = p.PeriodEnd,
            BasicSalary = p.BasicSalary,
            GrossPay = p.GrossPay,
            TotalDeductions = p.TotalDeductions,
            NetPay = p.NetPay,
            Status = p.Status,
            PaymentDate = p.PaymentDate,
            CreatedAt = p.CreatedAt
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PayrollDto>>> GetPayrollRecord(Guid id)
    {
        var p = await _db.Payrolls
            .Include(pay => pay.Employee)
            .FirstOrDefaultAsync(pay => pay.Id == id);

        if (p == null) return NotFound(ApiHelper.Fail("Payroll record not found"));

        var dto = new PayrollDto
        {
            Id = p.Id,
            EmployeeId = p.EmployeeId,
            EmployeeName = p.Employee?.ChineseName,
            PeriodStart = p.PeriodStart,
            PeriodEnd = p.PeriodEnd,
            BasicSalary = p.BasicSalary,
            InsuranceBase = p.InsuranceBase,
            PensionBase = p.PensionBase,
            ScheduledDays = p.ScheduledDays,
            ActualWorkingDays = p.ActualWorkingDays,
            TotalLateMinutes = p.TotalLateMinutes,
            TotalEarlyMinutes = p.TotalEarlyMinutes,
            AnomalyDays = p.AnomalyDays,
            TotalOt1Hours = p.TotalOt1Hours,
            TotalOt2Hours = p.TotalOt2Hours,
            TotalOt3Hours = p.TotalOt3Hours,
            Ot1Pay = p.Ot1Pay,
            Ot2Pay = p.Ot2Pay,
            Ot3Pay = p.Ot3Pay,
            TotalLeaveHours = p.TotalLeaveHours,
            AnnualLeaveHours = p.AnnualLeaveHours,
            PersonalLeaveHours = p.PersonalLeaveHours,
            SickLeaveHours = p.SickLeaveHours,
            FullAttendanceFlag = p.FullAttendanceFlag,
            LateDeduction = p.LateDeduction,
            EarlyDeduction = p.EarlyDeduction,
            AnomalyPenalty = p.AnomalyPenalty,
            OtherDeductions = p.OtherDeductions,
            OvertimePay = p.OvertimePay,
            Bonus = p.Bonus,
            AttendanceBonus = p.AttendanceBonus,
            LaborInsuranceDeduction = p.LaborInsuranceDeduction,
            HealthInsuranceDeduction = p.HealthInsuranceDeduction,
            PensionDeduction = p.PensionDeduction,
            WithholdingTax = p.WithholdingTax,
            GrossPay = p.GrossPay,
            TotalDeductions = p.TotalDeductions,
            EmployerContributions = p.EmployerContributions,
            NetPay = p.NetPay,
            Status = p.Status,
            PaymentDate = p.PaymentDate,
            PaymentBankAccount = p.PaymentBankAccount,
            PayrollSlipNo = p.PayrollSlipNo,
            CostAllocation = p.CostAllocation,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt
        };

        return Ok(dto);
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<PayrollDto>>> CalculatePayroll([FromBody] CalculatePayrollRequest req)
    {
        var employee = await _db.Employees.FindAsync(req.EmployeeId);
        if (employee == null) return BadRequest(ApiHelper.Fail("Employee not found"));

        var attendanceQuery = _db.AttendanceRecords
            .Where(a => a.EmployeeId == req.EmployeeId && a.Date >= req.PeriodStart && a.Date <= req.PeriodEnd);

        var attendance = await attendanceQuery.ToListAsync();
        var totalLateMinutes = attendance.Sum(a => a.LateMinutes);
        var totalEarlyMinutes = attendance.Sum(a => a.EarlyLeaveMinutes);
        var actualDays = attendance.Count(a => a.Status == "present");

        var leaveQuery = _db.LeaveRequests
            .Where(l => l.EmployeeId == req.EmployeeId && l.StartDate >= req.PeriodStart && l.EndDate <= req.PeriodEnd && l.Status == "approved");

        var leaves = await leaveQuery.ToListAsync();
        var leaveHours = leaves.Sum(l => l.TotalHours);

        var basicSalary = 0m;

        var grossPay = basicSalary;

        var payroll = new Payroll
        {
            Id = Guid.NewGuid(),
            EmployeeId = req.EmployeeId,
            PeriodStart = req.PeriodStart,
            PeriodEnd = req.PeriodEnd,
            BasicSalary = basicSalary,
            InsuranceBase = basicSalary,
            PensionBase = basicSalary,
            ScheduledDays = 22,
            ActualWorkingDays = actualDays,
            TotalLateMinutes = totalLateMinutes,
            TotalEarlyMinutes = totalEarlyMinutes,
            AnomalyDays = attendance.Count(a => a.LateFlag || a.EarlyLeaveFlag || a.ClockingAnomalyFlag),
            TotalLeaveHours = leaveHours,
            Status = "calculated",
            CalculatedAt = DateTime.UtcNow
        };

        _db.Payrolls.Add(payroll);
        await _db.SaveChangesAsync();

        var dto = new PayrollDto
        {
            Id = payroll.Id,
            EmployeeId = payroll.EmployeeId,
            EmployeeName = employee.ChineseName,
            PeriodStart = payroll.PeriodStart,
            PeriodEnd = payroll.PeriodEnd,
            BasicSalary = payroll.BasicSalary,
            GrossPay = payroll.GrossPay,
            TotalDeductions = payroll.TotalDeductions,
            NetPay = payroll.NetPay,
            Status = payroll.Status,
            CreatedAt = payroll.CreatedAt
        };

        return Ok(dto);
    }

    [HttpPatch("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> ApprovePayroll(Guid id)
    {
        var payroll = await _db.Payrolls.FindAsync(id);
        if (payroll == null) return NotFound(ApiHelper.Fail("Payroll not found"));

        payroll.Status = "approved";
        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Payroll approved");
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<PayrollSummaryDto>>> GetPayrollSummary([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
    {
        var payrolls = await _db.Payrolls
            .Where(p => p.PeriodStart == periodStart && p.PeriodEnd == periodEnd && p.Status != "draft")
            .ToListAsync();

        var summary = new PayrollSummaryDto
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            EmployeeCount = payrolls.Count,
            TotalBasicSalary = payrolls.Sum(p => p.BasicSalary),
            TotalGrossPay = payrolls.Sum(p => p.GrossPay),
            TotalDeductions = payrolls.Sum(p => p.TotalDeductions),
            TotalNetPay = payrolls.Sum(p => p.NetPay),
            TotalEmployerContributions = payrolls.Sum(p => p.EmployerContributions)
        };

        return Ok(summary);
    }
}

