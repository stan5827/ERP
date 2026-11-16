using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class DashboardDto
{
    public List<KpiCardDto> KpiCards { get; set; } = new();
    public List<AlertDto> Alerts { get; set; } = new();
    public List<WorkOrderSummaryDto> RecentWorkOrders { get; set; } = new();
    public List<ProductionTrendDto> ProductionTrend { get; set; } = new();
    public List<AttendanceTrendDto> AttendanceTrend { get; set; } = new();
    public int TodayAttendanceCount { get; set; }
    public int PendingApprovals { get; set; }
    public int TotalEmployees { get; set; }
    public int ActiveAlerts { get; set; }
    public SidebarBadgeDto SidebarBadges { get; set; } = new();
}

public class KpiCardDto
{
    public string Title { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string Color { get; set; } = null!;
}

public class AlertDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Severity { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class WorkOrderSummaryDto
{
    public Guid Id { get; set; }
    public string WoNumber { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal PlannedQty { get; set; }
    public decimal CompletedQty { get; set; }
    public string Status { get; set; } = null!;
    public string? Priority { get; set; }
    public DateTime? DueDate { get; set; }
}

public class SidebarBadgeDto
{
    public int SalesCount { get; set; }
    public int ProcurementCount { get; set; }
    public int HrCount { get; set; }
    public int InventoryCount { get; set; }
    public int ManufacturingCount { get; set; }
    public int QualityCount { get; set; }
}

public class ProductionTrendDto
{
    public string Date { get; set; } = null!;
    public decimal Planned { get; set; }
    public decimal Actual { get; set; }
    public decimal Efficiency { get; set; }
}

public class AttendanceTrendDto
{
    public string Date { get; set; } = null!;
    public int Present { get; set; }
    public int Late { get; set; }
    public int Absent { get; set; }
    public int Leave { get; set; }
}

public class AttendanceRecordDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime Date { get; set; }
    public string? ShiftType { get; set; }
    public string? ClockIn { get; set; }
    public string? ClockOut { get; set; }
    public string Status { get; set; } = null!;
    public bool LateFlag { get; set; }
    public int LateMinutes { get; set; }
    public bool EarlyLeaveFlag { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public decimal Ot1Hours { get; set; }
    public decimal Ot2Hours { get; set; }
    public decimal Ot3Hours { get; set; }
    public string? ApprovalStatus { get; set; }
    public string? Notes { get; set; }
}

public class ClockInRequest
{
    public string EmployeeId { get; set; } = null!;
}

public class ClockOutRequest
{
    public string EmployeeId { get; set; } = null!;
}

public class BatchAttendanceRequest
{
    public List<BatchAttendanceItem> Records { get; set; } = new();
}

public class BatchAttendanceItem
{
    public Guid EmployeeId { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime Date { get; set; }
    public string? ShiftType { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public string? Status { get; set; }
}
