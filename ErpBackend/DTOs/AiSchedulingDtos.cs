using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class AiScheduleRequest
{
    public string YearMonth { get; set; } = null!;
    public Guid? DepartmentId { get; set; }
    public int TargetHeadcount { get; set; }
}

public class AiScheduleResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public string YearMonth { get; set; } = null!;
    public List<ShiftScheduleDto> Schedules { get; set; } = new();
    public AiScheduleSummary Summary { get; set; } = new();
}

public class AiScheduleSummary
{
    public int TotalEmployees { get; set; }
    public int TotalShifts { get; set; }
    public int WeekendShifts { get; set; }
    public decimal AvgShiftsPerEmployee { get; set; }
    public decimal UtilizationRate { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class ShiftScheduleDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public string YearMonth { get; set; } = null!;
    public Dictionary<string, string> DailySchedule { get; set; } = new();
    public int TotalShifts { get; set; }
    public int WeekendShifts { get; set; }
    public decimal AvgDailyHours { get; set; }
}

public class TurnoverRiskDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = null!;
    public string EmployeeName { get; set; } = null!;
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = null!;
    public List<string> RiskFactors { get; set; } = new();
}

public class ShiftUtilizationDto
{
    public string YearMonth { get; set; } = null!;
    public int TotalSlots { get; set; }
    public int FilledSlots { get; set; }
    public decimal UtilizationRate { get; set; }
    public Dictionary<string, decimal> ByDepartment { get; set; } = new();
}
