using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.MES;

/// <summary>
/// MRP運行記錄
/// </summary>
public class MrpRun
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(30)]
    public string RunNumber { get; set; } = null!;
    
    /// <summary>
    /// 運行類型：regenerative=全量運行，net_change=增量運行
    /// </summary>
    [MaxLength(20)]
    public string RunType { get; set; } = "regenerative";
    
    public DateTime PlanningHorizonStart { get; set; }
    public DateTime PlanningHorizonEnd { get; set; }
    
    [MaxLength(20)]
    public string Status { get; set; } = "running"; // running/completed/failed
    
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    public int TotalItemsProcessed { get; set; }
    public int PlannedOrdersGenerated { get; set; }
    public int PurchaseRequisitionsGenerated { get; set; }
    
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    public Guid? TriggeredBy { get; set; }
    
    public ICollection<MrpRequirement> Requirements { get; set; } = new List<MrpRequirement>();
    public ICollection<MrpPlannedOrder> PlannedOrders { get; set; } = new List<MrpPlannedOrder>();
}

/// <summary>
/// MRP需求明細
/// </summary>
public class MrpRequirement
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid MrpRunId { get; set; }
    
    [Required] public Guid SkuId { get; set; }
    
    /// <summary>
    /// 需求類型：sales_order/work_order/forecast/safety_stock
    /// </summary>
    [MaxLength(30)]
    public string RequirementType { get; set; } = "sales_order";
    
    public Guid? SourceDocumentId { get; set; }
    
    [MaxLength(30)]
    public string? SourceDocumentNumber { get; set; }
    
    public DateTime RequiredDate { get; set; }
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal RequiredQty { get; set; }
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal AvailableQty { get; set; }
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal ShortageQty { get; set; }
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal PlannedReceiptQty { get; set; }
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal NetRequirement { get; set; }
    
    /// <summary>
    /// 建議動作：none/create_wo/create_pr/substitute
    /// </summary>
    [MaxLength(20)]
    public string ActionCode { get; set; } = "none";
    
    public Guid? SubstituteSkuId { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    [ForeignKey(nameof(MrpRunId))] public MrpRun MrpRun { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Inventory.Sku Sku { get; set; } = null!;
}

/// <summary>
/// MRP計畫訂單
/// </summary>
public class MrpPlannedOrder
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid MrpRunId { get; set; }
    
    [Required] public Guid SkuId { get; set; }
    
    /// <summary>
    /// 訂單類型：work_order/purchase_order
    /// </summary>
    [MaxLength(20)]
    public string OrderType { get; set; } = "work_order";
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal OrderQty { get; set; }
    
    public DateTime PlannedStartDate { get; set; }
    public DateTime PlannedEndDate { get; set; }
    
    public Guid? SourceRequirementId { get; set; }
    
    /// <summary>
    /// 狀態：suggested/confirmed/released
    /// </summary>
    [MaxLength(30)]
    public string Status { get; set; } = "suggested";
    
    public Guid? ConvertedDocumentId { get; set; }
    
    [MaxLength(30)]
    public string? ConvertedDocumentNumber { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey(nameof(MrpRunId))] public MrpRun MrpRun { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Inventory.Sku Sku { get; set; } = null!;
}

/// <summary>
/// 安全庫存設定
/// </summary>
public class SafetyStockLevel
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid SkuId { get; set; }
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal SafetyStockQty { get; set; }
    
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    public Guid? SetBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey(nameof(SkuId))] public Inventory.Sku Sku { get; set; } = null!;
}

/// <summary>
/// 需求預測（擴展現有模型）
/// </summary>
public class DemandForecastExt
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid SkuId { get; set; }
    
    [MaxLength(7)]
    public string ForecastMonth { get; set; } = null!; // YYYY-MM
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal ForecastQty { get; set; }
    
    [MaxLength(50)]
    public string Algorithm { get; set; } = "moving_average";
    
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Accuracy { get; set; }
    
    [Column(TypeName = "decimal(14,2)")]
    public decimal? ActualQty { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey(nameof(SkuId))] public Inventory.Sku Sku { get; set; } = null!;
}

/// <summary>
/// 需求來源（內部使用）
/// </summary>
public class DemandSource
{
    public string RequirementType { get; set; } = null!;
    public Guid? SourceDocumentId { get; set; }
    public string? SourceDocumentNumber { get; set; }
    public DateTime RequiredDate { get; set; }
    public decimal RequiredQty { get; set; }
}