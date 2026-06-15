using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.Asset;

public class AssetCategory
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string CategoryCode { get; set; } = null!;
    [Required, MaxLength(100)] public string CategoryName { get; set; } = null!;
    [MaxLength(30)] public string DepreciationMethod { get; set; } = "straight_line";
    public int DefaultUsefulLife { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal DefaultResidualRate { get; set; }
}

public class AssetLocation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string LocationCode { get; set; } = null!;
    [Required, MaxLength(100)] public string LocationName { get; set; } = null!;
    [MaxLength(300)] public string? Address { get; set; }
}

public class Asset
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string AssetNumber { get; set; } = null!;
    [Required, MaxLength(200)] public string AssetName { get; set; } = null!;
    [MaxLength(500)] public string? Specification { get; set; }
    [MaxLength(100)] public string? Manufacturer { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? CustodianDeptId { get; set; }
    public Guid? CustodianEmployeeId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal AcquisitionCost { get; set; }
    public DateTime? AcquisitionDate { get; set; }
    public DateTime? CommissionDate { get; set; }
    public int UsefulLife { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ResidualValue { get; set; }
    [MaxLength(30)] public string DepreciationMethod { get; set; } = "straight_line";
    public Guid? PurchaseOrderId { get; set; }
    [MaxLength(50)] public string? InvoiceNumber { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CategoryId))] public AssetCategory Category { get; set; } = null!;
    public ICollection<AssetDepreciation> Depreciations { get; set; } = new List<AssetDepreciation>();
    public ICollection<AssetMaintenance> Maintenances { get; set; } = new List<AssetMaintenance>();
}

public class AssetDepreciation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssetId { get; set; }
    [MaxLength(7)] public string Period { get; set; } = null!;
    [Column(TypeName = "decimal(14,2)")] public decimal DepreciationAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal AccumulatedDepreciation { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BookValue { get; set; }
    public Guid? JournalEntryId { get; set; }

    [ForeignKey(nameof(AssetId))] public Asset Asset { get; set; } = null!;
}

public class AssetTransfer
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssetId { get; set; }
    [MaxLength(30)] public string TransferType { get; set; } = "department";
    public Guid? FromDeptId { get; set; }
    public Guid? ToDeptId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public Guid? FromCustodianId { get; set; }
    public Guid? ToCustodianId { get; set; }
    public DateTime TransferDate { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AssetMaintenance
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssetId { get; set; }
    [MaxLength(50)] public string MaintenanceType { get; set; } = null!;
    [MaxLength(1000)] public string? Description { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PartsCost { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal LaborCost { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ExternalCost { get; set; }
    public bool IsCapitalized { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    [MaxLength(50)] public string? SerialNumber { get; set; }

    [ForeignKey(nameof(AssetId))] public Asset Asset { get; set; } = null!;
}

public class AssetDisposal
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssetId { get; set; }
    [MaxLength(50)] public string DisposalReason { get; set; } = null!;
    [Column(TypeName = "decimal(14,2)")] public decimal AccumulatedDepreciation { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BookValue { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DisposalIncome { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DisposalGainLoss { get; set; }
    public DateTime DisposalDate { get; set; }
    [MaxLength(100)] public string? EnvironmentalCertNo { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AssetStocktake
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string StocktakeNumber { get; set; } = null!;
    public DateTime StocktakeDate { get; set; }
    [MaxLength(30)] public string StocktakeType { get; set; } = "annual";
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public Guid? CreatedBy { get; set; }

    public ICollection<AssetStocktakeItem> Items { get; set; } = new List<AssetStocktakeItem>();
}

public class AssetStocktakeItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssetStocktakeId { get; set; }
    public Guid AssetId { get; set; }
    [MaxLength(30)] public string? SystemStatus { get; set; }
    [MaxLength(30)] public string? ActualStatus { get; set; }
    public bool LabelVerified { get; set; }
    public bool BarcodeScanned { get; set; }
    public bool CustodianConfirmed { get; set; }

    [ForeignKey(nameof(AssetStocktakeId))] public AssetStocktake AssetStocktake { get; set; } = null!;
}
