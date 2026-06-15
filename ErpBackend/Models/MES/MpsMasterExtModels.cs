using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Models.MES;

/// <summary>
/// 幣別主檔
/// </summary>
public class CurrencyMaster
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(10)]
    public string CurrencyCode { get; set; } = null!; // e.g. TWD, USD, EUR
    
    [Required, MaxLength(100)]
    public string CurrencyName { get; set; } = null!;
    
    [MaxLength(10)]
    public string Symbol { get; set; } = "$";
    
    public int DecimalPlaces { get; set; } = 2;
    
    public bool IsBaseCurrency { get; set; } = false;
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
}

/// <summary>
/// 匯率表
/// </summary>
public class ExchangeRate
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(10)]
    public string FromCurrency { get; set; } = null!;
    
    [Required, MaxLength(10)]
    public string ToCurrency { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,6)")]
    public decimal Rate { get; set; } = 1;
    
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ValidTo { get; set; }
    
    [MaxLength(20)]
    public string RateType { get; set; } = "SPOT"; // e.g. SPOT, BUDGET, AVERAGE
    
    [MaxLength(50)]
    public string Source { get; set; } = "MANUAL"; // e.g. MANUAL, ECB, CENTRAL_BANK
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// RCCP 關鍵資源 - 粗估產能
/// </summary>
public class RccpResource
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(30)]
    public string ResourceCode { get; set; } = null!;
    
    [Required, MaxLength(100)]
    public string ResourceName { get; set; } = null!;
    
    /// <summary>
    /// 資源類型：MACHINE, LABOR, SUPPLIER, TOOL
    /// </summary>
    [MaxLength(20)]
    public string ResourceType { get; set; } = "MACHINE";
    
    public Guid? WorkCenterId { get; set; } // 可隱射到具體的生產線
    
    /// <summary>
    /// 每個時格(通常是週)提供的可用總量 (例如總工時)
    /// </summary>
    [Column(TypeName = "decimal(14,2)")]
    public decimal CapacityPerBucket { get; set; }
    
    /// <summary>
    /// 產能單位：HOURS, UNITS, M2, etc.
    /// </summary>
    [MaxLength(20)]
    public string Uom { get; set; } = "HOURS";
    
    [Column(TypeName = "decimal(14,4)")]
    public decimal Efficiency { get; set; } = 1.0m;

    [Column(TypeName = "decimal(14,4)")]
    public decimal Utilization { get; set; } = 0.9m; // 預期利用率 (Buffer)
    
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
    
    [ForeignKey(nameof(WorkCenterId))]
    public WorkCenter? WorkCenter { get; set; }
}

/// <summary>
/// 品項資源清單 (Capacity Bill)
/// </summary>
public class RccpItemResource
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid SkuId { get; set; }
    
    [Required]
    public Guid ResourceId { get; set; }
    
    /// <summary>
    /// 單位產量所需的資源量 (例如：0.5 小時/每單位)
    /// </summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal UsagePerUnit { get; set; }
    
    [ForeignKey(nameof(SkuId))]
    public Sku Sku { get; set; } = null!;
    
    [ForeignKey(nameof(ResourceId))]
    public RccpResource Resource { get; set; } = null!;
}
