using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.MES;

/// <summary>
/// KPI指標定義
/// </summary>
public class KpiDefinition
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(50)]
    public string KpiCode { get; set; } = null!;
    
    [Required, MaxLength(100)]
    public string KpiName { get; set; } = null!;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 指標類別：efficiency/quality/delivery/cost
    /// </summary>
    [MaxLength(20)]
    public string Category { get; set; } = "efficiency";
    
    /// <summary>
    /// 計算公式（表達式）
    /// </summary>
    [MaxLength(1000)]
    public string? Formula { get; set; }
    
    /// <summary>
    /// 單位：%/hours/pcs/amount
    /// </summary>
    [MaxLength(20)]
    public string Unit { get; set; } = "%";
    
    /// <summary>
    /// 目標值
    /// </summary>
    [Column(TypeName = "decimal(14,4)")]
    public decimal? TargetValue { get; set; }
    
    /// <summary>
    /// 警告閾值（低於此值觸發警告）
    /// </summary>
    [Column(TypeName = "decimal(14,4)")]
    public decimal? WarningThreshold { get; set; }
    
    /// <summary>
    /// 嚴重閾值（低於此值觸發嚴重警告）
    /// </summary>
    [Column(TypeName = "decimal(14,4)")]
    public decimal? CriticalThreshold { get; set; }
    
    /// <summary>
    /// 計算頻率：hourly/daily/weekly/monthly
    /// </summary>
    [MaxLength(20)]
    public string CalculationFrequency { get; set; } = "daily";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<KpiValue> Values { get; set; } = new List<KpiValue>();
}

/// <summary>
/// KPI指標值（時間序列數據）
/// </summary>
public class KpiValue
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid KpiDefinitionId { get; set; }
    
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    /// <summary>
    /// 時間維度：hour/day/week/month
    /// </summary>
    [MaxLength(20)]
    public string PeriodType { get; set; } = "day";
    
    [Column(TypeName = "decimal(14,4)")]
    public decimal Value { get; set; }
    
    [Column(TypeName = "decimal(14,4)")]
    public decimal? TargetValue { get; set; }
    
    /// <summary>
    /// 達成率
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? AchievementRate { get; set; }
    
    /// <summary>
    /// 狀態：normal/warning/critical
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "normal";
    
    /// <summary>
    /// 原始數據JSON（可選，用於詳細分析）
    /// </summary>
    public string? RawData { get; set; }
    
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey(nameof(KpiDefinitionId))]
    public KpiDefinition KpiDefinition { get; set; } = null!;
}

/// <summary>
/// OEE（設備綜合效率）計算所需的設備運行數據
/// </summary>
public class MachineOperationRecord
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid WorkCenterId { get; set; }
    
    public Guid? WorkOrderId { get; set; }
    
    public DateTime RecordDate { get; set; }
    
    /// <summary>
    /// 班次：1=早班，2=中班，3=晚班
    /// </summary>
    public int ShiftNumber { get; set; } = 1;
    
    /// <summary>
    /// 計劃運行時間（分鐘）
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal PlannedRunTime { get; set; }
    
    /// <summary>
    /// 實際運行時間（分鐘）
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal ActualRunTime { get; set; }
    
    /// <summary>
    /// 停機時間（分鐘）
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal Downtime { get; set; }
    
    /// <summary>
    /// 換線時間（分鐘）
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal SetupTime { get; set; }
    
    /// <summary>
    /// 等待時間（分鐘）
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal WaitTime { get; set; }
    
    /// <summary>
    /// 理論產出數量（標準產量）
    /// </summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal TheoreticalOutput { get; set; }
    
    /// <summary>
    /// 實際產出數量
    /// </summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal ActualOutput { get; set; }
    
    /// <summary>
    /// 良品數量
    /// </summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal GoodOutput { get; set; }
    
    /// <summary>
    /// 不良品數量
    /// </summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal DefectOutput { get; set; }
    
    /// <summary>
    /// 停機原因代碼
    /// </summary>
    [MaxLength(20)]
    public string? DowntimeReasonCode { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey(nameof(WorkCenterId))]
    public WorkCenter WorkCenter { get; set; } = null!;
    
    [ForeignKey(nameof(WorkOrderId))]
    public WorkOrder? WorkOrder { get; set; }
}

/// <summary>
/// 報廢記錄（擴展現有模型）
/// </summary>
public class ScrapRecordExt
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid WorkOrderId { get; set; }
    
    [Required] public Guid SkuId { get; set; }
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal ScrapQty { get; set; }
    
    /// <summary>
    /// 報廢原因代碼
    /// </summary>
    [MaxLength(20)]
    public string? ScrapReasonCode { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    [MaxLength(30)]
    public string? DispositionCode { get; set; }
    
    /// <summary>
    /// 報廢成本
    /// </summary>
    [Column(TypeName = "decimal(14,4)")]
    public decimal CostAmount { get; set; }
    
    /// <summary>
    /// 責任部門
    /// </summary>
    [MaxLength(50)]
    public string? ResponsibleDept { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey(nameof(WorkOrderId))]
    public WorkOrder WorkOrder { get; set; } = null!;
    
    [ForeignKey(nameof(SkuId))]
    public Inventory.Sku Sku { get; set; } = null!;
}