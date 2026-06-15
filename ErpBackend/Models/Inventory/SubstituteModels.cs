using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.Inventory;

/// <summary>
/// 替代料群組：定義一組可互相替代的物料
/// </summary>
public class SubstituteGroup
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(50)]
    public string GroupCode { get; set; } = null!;
    
    [MaxLength(200)]
    public string? GroupName { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 替代類型：full=完全替代，partial=部分替代
    /// </summary>
    [MaxLength(20)]
    public string SubstitutionType { get; set; } = "full";
    
    /// <summary>
    /// 是否允許自動替代
    /// </summary>
    public bool AllowAutoSubstitute { get; set; } = true;
    
    /// <summary>
    /// 是否雙向替代
    /// </summary>
    public bool IsBidirectional { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 群組內的替代料列表
    /// </summary>
    public ICollection<SubstituteItem> Items { get; set; } = new List<SubstituteItem>();
}

/// <summary>
/// 替代料明細：定義群組內每個物料的替代屬性
/// </summary>
public class SubstituteItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid GroupId { get; set; }
    
    [Required] public Guid SkuId { get; set; }
    
    /// <summary>
    /// 替代優先級：1=最高優先級（主料），2=次高，依此類推
    /// </summary>
    public int Priority { get; set; } = 1;
    
    /// <summary>
    /// 替換比例：1.0表示1:1替代，1.2表示需要1.2倍數量
    /// </summary>
    [Column(TypeName = "decimal(8,4)")]
    public decimal? ConversionRate { get; set; } = 1.0m;
    
    /// <summary>
    /// 是否為主料（Primary）
    /// </summary>
    public bool IsPrimary { get; set; } = false;
    
    /// <summary>
    /// 狀態：active/inactive/temporary
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "active";
    
    /// <summary>
    /// 生效日期
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }
    
    /// <summary>
    /// 失效日期
    /// </summary>
    public DateTime? EffectiveTo { get; set; }
    
    /// <summary>
    /// 替代條件表達式（可選）
    /// </summary>
    [MaxLength(500)]
    public string? ConditionExpression { get; set; }
    
    /// <summary>
    /// 備註
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey(nameof(GroupId))]
    public SubstituteGroup Group { get; set; } = null!;
    
    [ForeignKey(nameof(SkuId))]
    public Sku Sku { get; set; } = null!;
}

/// <summary>
/// 替代料使用紀錄：追蹤替代料的使用歷史
/// </summary>
public class SubstituteUsageLog
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid OriginalSkuId { get; set; }
    
    [Required] public Guid SubstituteSkuId { get; set; }
    
    [Required] public Guid WorkOrderId { get; set; }
    
    [Column(TypeName = "decimal(14,4)")]
    public decimal OriginalQty { get; set; }
    
    [Column(TypeName = "decimal(14,4)")]
    public decimal SubstituteQty { get; set; }
    
    /// <summary>
    /// 成本差異（正數表示替代料較貴）
    /// </summary>
    [Column(TypeName = "decimal(14,4)")]
    public decimal CostVariance { get; set; }
    
    /// <summary>
    /// 使用原因：shortage=缺料，cost_down=降本，quality=品質提升，other=其他
    /// </summary>
    [MaxLength(20)]
    public string Reason { get; set; } = "shortage";
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public Guid? ApprovedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [ForeignKey(nameof(OriginalSkuId))]
    public Sku OriginalSku { get; set; } = null!;
    
    [ForeignKey(nameof(SubstituteSkuId))]
    public Sku SubstituteSku { get; set; } = null!;
    
    [ForeignKey(nameof(WorkOrderId))]
    public MES.WorkOrder WorkOrder { get; set; } = null!;
}