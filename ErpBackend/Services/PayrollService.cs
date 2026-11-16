using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface IPayrollService
{
    Task<(List<PayrollDto> Items, int TotalCount)> GetPayrollRecordsAsync(
        string? employeeId,
        string? startDate,
        string? endDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<PayrollDto?> GetPayrollRecordAsync(Guid id);
    Task<(PayrollDto? Payroll, string Status)> CalculatePayrollAsync(CalculatePayrollRequest req);
    Task<bool> ApprovePayrollAsync(Guid id);
    Task<PayrollSummaryDto?> GetPayrollSummaryAsync(DateTime periodStart, DateTime periodEnd);
}

public class PayrollService : IPayrollService
{
    private readonly ErpGeneralDbContext _db;

    public PayrollService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<(List<PayrollDto> Items, int TotalCount)> GetPayrollRecordsAsync(
        string? employeeId,
        string? startDate,
        string? endDate,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
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

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.PeriodStart)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

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
            CreatedAt = p.CreatedAt,
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
            EmployerContributions = p.EmployerContributions,
            PaymentBankAccount = p.PaymentBankAccount,
            PayrollSlipNo = p.PayrollSlipNo,
            CostAllocation = p.CostAllocation,
            Notes = p.Notes
        }).ToList();

        return (dtos, totalCount);
    }

    public async Task<PayrollDto?> GetPayrollRecordAsync(Guid id)
    {
        var p = await _db.Payrolls
            .Include(pay => pay.Employee)
            .FirstOrDefaultAsync(pay => pay.Id == id);

        if (p == null) return null;

        return new PayrollDto
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
    }

    public Task<(PayrollDto? Payroll, string Status)> CalculatePayrollAsync(CalculatePayrollRequest req)
        => Task.FromResult<(PayrollDto?, string)>((null, "calculation_not_configured"));

    public async Task<bool> ApprovePayrollAsync(Guid id)
    {
        var payroll = await _db.Payrolls.FindAsync(id);
        if (payroll == null) return false;

        payroll.Status = "approved";
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PayrollSummaryDto?> GetPayrollSummaryAsync(DateTime periodStart, DateTime periodEnd)
    {
        var payrolls = await _db.Payrolls
            .Where(p => p.PeriodStart == periodStart && p.PeriodEnd == periodEnd && p.Status != "draft")
            .ToListAsync();

        if (payrolls == null || payrolls.Count == 0)
            return new PayrollSummaryDto
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                EmployeeCount = 0,
                TotalBasicSalary = 0,
                TotalGrossPay = 0,
                TotalDeductions = 0,
                TotalNetPay = 0,
                TotalEmployerContributions = 0
            };

        return new PayrollSummaryDto
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
    }
}
