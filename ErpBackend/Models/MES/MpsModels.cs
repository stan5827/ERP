using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.MES;

/// <summary>
/// 主生產規劃標頭 - 管理版本與沖銷規則
/// </summary>
public class MpsHeader
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(30)] 
    public string MpsNo { get; set; } = null!;
    
    [Required, MaxLength(20)]
    public string PlantCode { get; set; } = "HQ";
    
    public string PlanName { get; set; } = null!;
    
    public DateTime HorizonStart { get; set; }
    public DateTime HorizonEnd { get; set; }
    
    /// <summary>
    /// 時格類型：DAY / WEEK / MONTH / CUSTOM
    /// </summary>
    [Required, MaxLength(10)]
    public string TimeBucketType { get; set; } = "WEEK";
    
    public int VersionNo { get; set; } = 1;
    
    /// <summary>
    /// 狀態：DRAFT / APPROVED / CLOSED
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "DRAFT";
    
    /// <summary>
    /// 規劃基礎：FORECAST_ONLY / MIXED / SO_ONLY
    /// </summary>
    [MaxLength(20)]
    public string BaseOn { get; set; } = "MIXED";

    // 沖銷規則
    public int BackwardConsumptionDays { get; set; } = 7;
    public int ForwardConsumptionDays { get; set; } = 7;
    
    /// <summary>
    /// 沖銷策略：CLOSEST / BACKWARD_THEN_FORWARD
    /// </summary>
    [MaxLength(30)]
    public string ConsumptionStrategy { get; set; } = "BACKWARD_THEN_FORWARD";

    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MpsLine> Lines { get; set; } = new List<MpsLine>();
}

/// <summary>
/// MPS 明細規劃行 - 品項與時格產量
/// </summary>
public class MpsLine
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid MpsHeaderId { get; set; }
    
    [Required]
    public Guid SkuId { get; set; }
    
    // 時格區間
    public DateTime BucketStart { get; set; }
    public DateTime BucketEnd { get; set; }
    
    [MaxLength(20)]
    public string? BucketCode { get; set; } // 例如 2026W01
    
    /// <summary>
    /// 原始預測量 (或主計畫量)
    /// </summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal PlanQty { get; set; }
    
    /// <summary>
    /// 已被銷售訂單沖銷消耗的量
    /// </summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal ConsumedQty { get; set; }
    
    /// <summary>
    /// 最終生成的供給建議量 (PlanQty vs ConsumedQty 調整後)
    /// </summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal AdjustedQty { get; set; }

    public bool OverrideFlag { get; set; } = false;
    
    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(MpsHeaderId))]
    public MpsHeader Header { get; set; } = null!;
    
    [ForeignKey(nameof(SkuId))]
    public Inventory.Sku Sku { get; set; } = null!;

    public ICollection<ForecastConsumptionDetail> ConsumptionDetails { get; set; } = new List<ForecastConsumptionDetail>();
}

/// <summary>
/// 需求沖銷明細 - 追蹤 SO 消耗了哪個 MPS 時格
/// </summary>
public class ForecastConsumptionDetail
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid MpsLineId { get; set; }
    
    [Required, MaxLength(30)]
    public string SoNo { get; set; } = null!;
    
    public Guid? SoLineId { get; set; }
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal ConsumedQty { get; set; }
    
    public DateTime ConsumedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(MpsLineId))]
    public MpsLine MpsLine { get; set; } = null!;
}

/// <summary>
/// 自定義時格日曆 (擴充用)
/// </summary>
public class CalendarBucket
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(32)]
    public string CalendarId { get; set; } = null!; // e.g. 'STD', '4-4-5-FY26'
    
    [Required, MaxLength(16)]
    public string BucketCode { get; set; } = null!;
    
    [Required, MaxLength(16)]
    public string BucketType { get; set; } = null!; // WEEK / BIWEEK / PERIOD
    
    public DateTime BucketStart { get; set; }
    public DateTime BucketEnd { get; set; }
    
    public int SequenceNo { get; set; }
}

/// <summary>
/// MPS 異常警告
/// </summary>
public class MpsException
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MpsHeaderId { get; set; }
    public Guid? SkuId { get; set; }
    
    [MaxLength(20)]
    public string Severity { get; set; } = "WARNING"; // WARNING, ERROR
    
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
