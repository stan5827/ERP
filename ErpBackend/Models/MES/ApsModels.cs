using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.MES;

/// <summary>
/// 產能日曆：定義工作中心的可用產能
/// </summary>
public class CapacityCalendar
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid WorkCenterId { get; set; }
    
    [Required, MaxLength(50)]
    public string CalendarName { get; set; } = null!;
    
    /// <summary>
    /// 日曆類型：daily/weekly/monthly
    /// </summary>
    [MaxLength(20)]
    public string CalendarType { get; set; } = "daily";
    
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<CapacityCalendarEntry> Entries { get; set; } = new List<CapacityCalendarEntry>();
    
    [ForeignKey(nameof(WorkCenterId))]
    public WorkCenter WorkCenter { get; set; } = null!;
}

/// <summary>
/// 產能日曆條目：每日/每班次的產能設定
/// </summary>
public class CapacityCalendarEntry
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid CalendarId { get; set; }
    
    public DateTime Date { get; set; }
    
    /// <summary>
    /// 班次：1=早班，2=中班，3=晚班
    /// </summary>
    public int ShiftNumber { get; set; } = 1;
    
    /// <summary>
    /// 班次代碼：D=日班，N=夜班
    /// </summary>
    [MaxLength(10)]
    public string ShiftCode { get; set; } = "D";
    
    /// <summary>
    /// 可用工時（小時）
    /// </summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal AvailableHours { get; set; } = 8;
    
    /// <summary>
    /// 效率百分比
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal Efficiency { get; set; } = 100;
    
    public bool IsHoliday { get; set; } = false;
    
    [MaxLength(200)]
    public string? Reason { get; set; }
    
    [ForeignKey(nameof(CalendarId))]
    public CapacityCalendar Calendar { get; set; } = null!;
}

/// <summary>
/// 生產排程主檔
/// </summary>
public class ProductionSchedule
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(30)]
    public string ScheduleNumber { get; set; } = null!;
    
    /// <summary>
    /// 排程類型：firm/frozen/released
    /// </summary>
    [MaxLength(20)]
    public string ScheduleType { get; set; } = "firm";
    
    public DateTime ScheduleDate { get; set; }
    public DateTime HorizonStart { get; set; }
    public DateTime HorizonEnd { get; set; }
    
    /// <summary>
    /// 狀態：draft/active/completed/cancelled
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "draft";
    
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAt { get; set; }
    
    public ICollection<ScheduleTask> Tasks { get; set; } = new List<ScheduleTask>();
    public ICollection<CapacityLoad> CapacityLoads { get; set; } = new List<CapacityLoad>();
    public ICollection<ScheduleConstraint> Constraints { get; set; } = new List<ScheduleConstraint>();
}

/// <summary>
/// 排程任務（甘特圖任務）
/// </summary>
public class ScheduleTask
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid ScheduleId { get; set; }
    
    [Required] public Guid WorkOrderId { get; set; }
    
    public int OperationSeq { get; set; }
    
    [Required] public Guid WorkCenterId { get; set; }
    
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal SetupHours { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal RunHours { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal TotalHours { get; set; }
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// 狀態：planned/scheduled/released/in_progress/completed
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "planned";
    
    /// <summary>
    /// 優先級：1-100，數字越大優先級越高
    /// </summary>
    public int Priority { get; set; } = 50;
    
    /// <summary>
    /// 排序類型：normal/urgent/setup_min
    /// </summary>
    [MaxLength(20)]
    public string SequenceType { get; set; } = "normal";
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [ForeignKey(nameof(ScheduleId))]
    public ProductionSchedule Schedule { get; set; } = null!;
    
    [ForeignKey(nameof(WorkOrderId))]
    public WorkOrder WorkOrder { get; set; } = null!;
    
    [ForeignKey(nameof(WorkCenterId))]
    public WorkCenter WorkCenter { get; set; } = null!;
}

/// <summary>
/// 產能負荷分析
/// </summary>
public class CapacityLoad
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid ScheduleId { get; set; }
    
    [Required] public Guid WorkCenterId { get; set; }
    
    public DateTime LoadDate { get; set; }
    
    public int ShiftNumber { get; set; } = 1;
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal AvailableHours { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal ScheduledHours { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal LoadedHours { get; set; }
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal LoadPercentage { get; set; }
    
    /// <summary>
    /// 負荷狀態：normal/overloaded/underutilized
    /// </summary>
    [MaxLength(20)]
    public string LoadStatus { get; set; } = "normal";
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal? CapacityUtilization { get; set; }
    
    [ForeignKey(nameof(ScheduleId))]
    public ProductionSchedule Schedule { get; set; } = null!;
    
    [ForeignKey(nameof(WorkCenterId))]
    public WorkCenter WorkCenter { get; set; } = null!;
}

/// <summary>
/// 排程約束條件
/// </summary>
public class ScheduleConstraint
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid ScheduleId { get; set; }
    
    /// <summary>
    /// 約束類型：resource/material/tool/maintenance
    /// </summary>
    [MaxLength(30)]
    public string ConstraintType { get; set; } = "resource";
    
    [MaxLength(200)]
    public string Description { get; set; } = null!;
    
    public Guid? ResourceId { get; set; }
    
    [MaxLength(50)]
    public string? ResourceType { get; set; }
    
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// 嚴重程度：warning/error/critical
    /// </summary>
    [MaxLength(20)]
    public string Severity { get; set; } = "warning";
    
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
    
    [MaxLength(500)]
    public string? ResolutionNotes { get; set; }
    
    [ForeignKey(nameof(ScheduleId))]
    public ProductionSchedule Schedule { get; set; } = null!;
}

/// <summary>
/// 排程優化標準
/// </summary>
public class OptimizationCriteria
{
    /// <summary>
    /// 優化目標：minimize_setup/maximize_utilization/meet_deadlines/balance_workload
    /// </summary>
    public string PrimaryGoal { get; set; } = "meet_deadlines";
    
    public bool RespectPriority { get; set; } = true;
    public bool AllowRescheduling { get; set; } = true;
    public int MaxIterations { get; set; } = 1000;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
}