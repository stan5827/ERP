using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.Inventory;

public class Uom
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string UomCode { get; set; } = null!;
    [Required, MaxLength(50)] public string UomName { get; set; } = null!;
    [MaxLength(50)] public string? UomNameEn { get; set; }
    [Required, MaxLength(20)] public string UomType { get; set; } = "base";
    public int DecimalPlaces { get; set; } = 2;
    public bool IsActive { get; set; } = true;
    public bool AllowDecimal { get; set; } = true;
    [MaxLength(200)] public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [InverseProperty(nameof(UomConversion.FromUom))]
    public ICollection<UomConversion> FromConversions { get; set; } = new List<UomConversion>();
    [InverseProperty(nameof(UomConversion.ToUom))]
    public ICollection<UomConversion> ToConversions { get; set; } = new List<UomConversion>();
}

public class UomConversion
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SkuId { get; set; }
    [Required] public Guid FromUomId { get; set; }
    [Required] public Guid ToUomId { get; set; }
    [Column(TypeName = "decimal(18,6)")] public decimal Factor { get; set; } = 1;
    [MaxLength(1)] public string Operator { get; set; } = "*";
    [Column(TypeName = "decimal(18,6)")] public decimal? InverseFactor { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(200)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SkuId))] public Sku? Sku { get; set; }
    [ForeignKey(nameof(FromUomId))] public Uom FromUom { get; set; } = null!;
    [ForeignKey(nameof(ToUomId))] public Uom ToUom { get; set; } = null!;
}

public class UomGroup
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string GroupCode { get; set; } = null!;
    [Required, MaxLength(100)] public string GroupName { get; set; } = null!;
    [MaxLength(200)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UomGroupMember
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UomGroupId { get; set; }
    public Guid UomId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UomGroupId))] public UomGroup UomGroup { get; set; } = null!;
    [ForeignKey(nameof(UomId))] public Uom Uom { get; set; } = null!;
}
