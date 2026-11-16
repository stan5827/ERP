using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using ErpBackend.Models.MES;
using ErpBackend.Models.Sales;

namespace ErpBackend.Services;

public interface IAiSchedulingService
{
    Task<AiScheduleResponse> GenerateScheduleAsync(AiScheduleRequest request);
    Task<List<TurnoverRiskDto>> PredictTurnoverRisksAsync(Guid? departmentId);
    Task<ShiftUtilizationDto> GetShiftUtilizationAsync(string yearMonth);
}

public class AiSchedulingService : IAiSchedulingService
{
    private readonly ErpGeneralDbContext _db;

    public AiSchedulingService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<AiScheduleResponse> GenerateScheduleAsync(AiScheduleRequest request)
    {
var employees = await _db.Employees
    .Where(e => e.Status == "active" && (request.DepartmentId == null || e.DepartmentId == request.DepartmentId))
    .Take(request.TargetHeadcount > 0 ? request.TargetHeadcount : 50)
    .ToListAsync();

        if (!employees.Any())
            return new AiScheduleResponse { Success = false, Message = "找不到員工資料" };

        var year = int.Parse(request.YearMonth[..4]);
        var month = int.Parse(request.YearMonth[4..]);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var shiftTypes = new[] { "D", "N", "off" };
        var random = new Random(request.YearMonth.GetHashCode());
        var schedules = new List<ShiftScheduleDto>();
        var warnings = new List<string>();

        foreach (var emp in employees)
        {
            var dailySchedule = new Dictionary<string, string>();
            var weekendShifts = 0;
            var totalHours = 0m;

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                var dayOfWeek = (int)date.DayOfWeek;
                var isWeekend = dayOfWeek == 0 || dayOfWeek == 6;

                var shift = shiftTypes[random.Next(shiftTypes.Length)];
                dailySchedule[date.ToString("yyyy-MM-dd")] = shift;

                if (isWeekend && shift != "off")
                    weekendShifts++;

                if (shift == "D" || shift == "N")
                    totalHours += 8;
            }

schedules.Add(new ShiftScheduleDto
{
    EmployeeId = emp.Id,
    EmployeeName = emp.ChineseName,
    YearMonth = request.YearMonth,
    DailySchedule = dailySchedule,
    TotalShifts = dailySchedule.Count(s => s.Value != "off"),
    WeekendShifts = weekendShifts,
    AvgDailyHours = totalHours / daysInMonth
});
        }

        var totalShifts = schedules.Sum(s => s.TotalShifts);
        var maxShifts = employees.Count * daysInMonth;
        var utilization = maxShifts > 0 ? (decimal)totalShifts / maxShifts * 100 : 0;

        if (utilization < 70)
            warnings.Add("整體利用率低於70%，建議增加班次配置");

        var hasExcessiveWeekend = schedules.Any(s => s.WeekendShifts > 4);
        if (hasExcessiveWeekend)
            warnings.Add("部分員工週末上班天數過多，注意法規遵循");

        return new AiScheduleResponse
        {
            Success = true,
            Message = "AI 排班已生成",
            YearMonth = request.YearMonth,
            Schedules = schedules,
            Summary = new AiScheduleSummary
            {
                TotalEmployees = employees.Count,
                TotalShifts = totalShifts,
                WeekendShifts = schedules.Sum(s => s.WeekendShifts),
                AvgShiftsPerEmployee = employees.Count > 0 ? totalShifts / employees.Count : 0,
                UtilizationRate = utilization,
                Warnings = warnings
            }
        };
    }

    public async Task<List<TurnoverRiskDto>> PredictTurnoverRisksAsync(Guid? departmentId)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

        var employees = await _db.Employees
            .Include(e => e.AttendanceRecords)
            .Include(e => e.LeaveRequests)
            .Where(e => e.Status == "active" && (departmentId == null || e.DepartmentId == departmentId))
            .ToListAsync();

        var risks = new List<TurnoverRiskDto>();

        foreach (var emp in employees)
        {
            var factors = new List<string>();
            var riskScore = 0m;

            var recentAttendance = emp.AttendanceRecords?.Where(a => a.Date >= thirtyDaysAgo).ToList() ?? new List<AttendanceRecord>();
            var lateCount = recentAttendance.Count(a => a.LateFlag);
            if (lateCount >= 3)
            {
                riskScore += 30;
                factors.Add($"近30天遲到{lateCount}次");
            }

            var earlyLeaveCount = recentAttendance.Count(a => a.EarlyLeaveFlag);
            if (earlyLeaveCount >= 3)
            {
                riskScore += 20;
                factors.Add($"早退{earlyLeaveCount}次");
            }

            var absentCount = recentAttendance.Count(a => a.Status == "absent");
            if (absentCount >= 5)
            {
                riskScore += 25;
                factors.Add($"缺勤{absentCount}天");
            }

            var leaveRequests = emp.LeaveRequests?.Where(l => l.ApplicationDate >= ninetyDaysAgo).ToList() ?? new List<LeaveRequest>();
            var pendingLeaves = leaveRequests.Count(l => l.Status == "pending");
            if (pendingLeaves >= 2)
            {
                riskScore += 15;
                factors.Add($"待核准請假{pendingLeaves}筆");
            }

            var totalOt = recentAttendance.Sum(a => a.Ot1Hours + a.Ot2Hours + a.Ot3Hours);
            if (totalOt > 60)
            {
                riskScore += 10;
                factors.Add($"加班時數過多({totalOt}小時)");
            }

            var riskLevel = riskScore switch
            {
                >= 70 => "high",
                >= 40 => "medium",
                _ => "low"
            };

            if (riskScore > 0)
            {
            risks.Add(new TurnoverRiskDto
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.EmployeeNo,
                EmployeeName = emp.ChineseName,
                RiskScore = riskScore,
                RiskLevel = riskLevel,
                RiskFactors = factors
            });
            }
        }

        return risks.OrderByDescending(r => r.RiskScore).ToList();
    }

    public async Task<ShiftUtilizationDto> GetShiftUtilizationAsync(string yearMonth)
    {
        var year = int.Parse(yearMonth[..4]);
        var month = int.Parse(yearMonth[4..]);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var schedules = await _db.ShiftSchedules
            .Where(s => s.YearMonth == yearMonth)
            .Include(s => s.Employee)
            .ToListAsync();

        var totalSlots = schedules.Count * daysInMonth;
        var filledShifts = 0;

        foreach (var schedule in schedules)
        {
            if (!string.IsNullOrEmpty(schedule.ScheduleData))
            {
                filledShifts += schedule.ScheduleData.Split(',').Count(s => s != "off");
            }
        }

        return new ShiftUtilizationDto
        {
            YearMonth = yearMonth,
            TotalSlots = totalSlots,
            FilledSlots = filledShifts,
            UtilizationRate = totalSlots > 0 ? (decimal)filledShifts / totalSlots * 100 : 0
        };
    }
}
