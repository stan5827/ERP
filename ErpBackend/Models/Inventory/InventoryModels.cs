using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.MES;
using ErpBackend.Models.QA;

namespace ErpBackend.Models.Inventory;

public class Warehouse
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string WarehouseCode { get; set; } = null!;
    [Required, MaxLength(100)] public string WarehouseName { get; set; } = null!;
    [MaxLength(300)] public string? Address { get; set; }
    [MaxLength(30)] public string Type { get; set; } = "normal";
    public bool IsActive { get; set; } = true;

    public ICollection<BinLocation> BinLocations { get; set; } = new List<BinLocation>();
}

public class BinLocation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid WarehouseId { get; set; }
    [Required, MaxLength(30)] public string LocationCode { get; set; } = null!;
    [MaxLength(20)] public string? Zone { get; set; }
    [MaxLength(20)] public string? Aisle { get; set; }
    [MaxLength(20)] public string? Rack { get; set; }
    [MaxLength(20)] public string? Level { get; set; }
    [MaxLength(20)] public string? Position { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? MaxWeight { get; set; }
    [MaxLength(10)] public string? GridX { get; set; }
    [MaxLength(10)] public string? GridY { get; set; }
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(WarehouseId))] public Warehouse Warehouse { get; set; } = null!;
}

public class Sku
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string SkuNumber { get; set; } = null!;
    [Required, MaxLength(200)] public string SkuName { get; set; } = null!;
    [MaxLength(500)] public string? Specification { get; set; }
    [MaxLength(50)] public string? Barcode { get; set; }
    [MaxLength(20)] public string? PurchaseUnit { get; set; }
    [MaxLength(20)] public string? PurchaseUomCode { get; set; }
    [MaxLength(30)] public string? Category { get; set; }
    [Column(TypeName = "decimal(10,4)")] public decimal ConversionRate { get; set; } = 1;
    public bool LotControlled { get; set; }
    public bool SerialControlled { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? Length { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? Width { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? Height { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal? Weight { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public Guid? DefaultBinLocationId { get; set; }
    public Guid? DefaultSupplierId { get; set; }
    [MaxLength(10)] public string? Unit { get; set; }
    [MaxLength(10)] public string? AbcClassification { get; set; }
    [MaxLength(20)] public string SourceType { get; set; } = "purchased";
    [Column(TypeName = "decimal(12,2)")] public decimal SafetyStock { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal ReorderPoint { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal MaxStock { get; set; }
    public int LeadTimeDays { get; set; } = 7;
    [Column(TypeName = "decimal(14,4)")] public decimal Moq { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal PurchasePrice { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal StandardCost { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "active";
    public int LowLevelCode { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Qty { get; set; }
    public int? UnitsPerCarton { get; set; }
    public int? CartonsPerPallet { get; set; }
    [MaxLength(100)] public string? HumidityRequirement { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(20)] public string? BaseUomCode { get; set; }
    [MaxLength(20)] public string? SalesUomCode { get; set; }
    [MaxLength(20)] public string? ProductionUomCode { get; set; }
    [Column(TypeName = "decimal(18,6)")] public decimal? PurchaseToBaseFactor { get; set; } = 1;
    [Column(TypeName = "decimal(18,6)")] public decimal? SalesToBaseFactor { get; set; } = 1;
    [Column(TypeName = "decimal(18,6)")] public decimal? ProductionToBaseFactor { get; set; } = 1;

    public Guid? BaseUomId { get; set; }
    public Guid? PurchaseUomId { get; set; }
    public Guid? SalesUomId { get; set; }
    public Guid? ProductionUomId { get; set; }
    [ForeignKey(nameof(BaseUomId))] public Uom? BaseUom { get; set; }
    [ForeignKey(nameof(PurchaseUomId))] public Uom? PurchaseUom { get; set; }
    [ForeignKey(nameof(SalesUomId))] public Uom? SalesUom { get; set; }
    [ForeignKey(nameof(ProductionUomId))] public Uom? ProductionUom { get; set; }
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<InventoryLot> Lots { get; set; } = new List<InventoryLot>();
    public ICollection<SerialNumber> SerialNumbers { get; set; } = new List<SerialNumber>();
    
    // 替代料導航屬性
    public ICollection<SubstituteItem> SubstituteItems { get; set; } = new List<SubstituteItem>();
    public ICollection<SubstituteItem> UsedAsSubstitute { get; set; } = new List<SubstituteItem>();
    public ICollection<SubstituteUsageLog> OriginalSubstituteUsageLogs { get; set; } = new List<SubstituteUsageLog>();
    public ICollection<SubstituteUsageLog> SubstituteUsageLogs { get; set; } = new List<SubstituteUsageLog>();

    [MaxLength(20)] public string ItemType { get; set; } = "purchased";
    [MaxLength(50)] public string? Model { get; set; }
    [MaxLength(100)] public string? Brand { get; set; }
    public bool IsSellable { get; set; } = true;
    public bool IsPurchasable { get; set; } = true;
    public bool IsManufacturable { get; set; } = false;
    public bool IsOutsourced { get; set; } = false;
    [MaxLength(20)] public string CostMethod { get; set; } = "standard";
    [Column(TypeName = "decimal(14,4)")] public decimal SuggestedPrice { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal BasePrice { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal? PriceA { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal? PriceB { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal? PriceC { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal? PriceD { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal? PriceE { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    public bool IsBomTop { get; set; } = false;
    public bool IsBomComponent { get; set; } = false;
    public bool IsPhantom { get; set; } = false;
    public bool IsMto { get; set; } = false;
    public bool IsMts { get; set; } = true;
    public bool IsCustom { get; set; } = false;
    
    // MPS 整合欄位
    public bool IsMpsManaged { get; set; } = false;
    [MaxLength(20)] public string PlanningStrategy { get; set; } = "MTS"; // MTS, MTO, MIXED
    public Guid? DefaultBomId { get; set; }
    public Guid? DefaultRoutingId { get; set; }
    [MaxLength(20)] public string MakeOrBuy { get; set; } = "buy";
    [MaxLength(50)] public string? SupplierItemCode { get; set; }
    public int? SupplierLeadTime { get; set; }
    [MaxLength(50)] public string? DrawingNo { get; set; }
    [MaxLength(20)] public string? DrawingRevision { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    [MaxLength(500)] public string? QualitySpec { get; set; }
    [MaxLength(20)] public string? ProductionDept { get; set; }
    [MaxLength(50)] public string? MachineCode { get; set; }
    [MaxLength(50)] public string? MoldCode { get; set; }
    [MaxLength(20)] public string? CostUnit { get; set; }
    public Guid? CostUomId { get; set; }
    [ForeignKey(nameof(CostUomId))] public Uom? CostUom { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal LatestPurchaseCost { get; set; }
    [MaxLength(20)] public string? TaxCode { get; set; }
    [MaxLength(10)] public string? TaxRate { get; set; }
    public bool IsSubstitute { get; set; }
    public bool IsAlternate { get; set; }
    public bool IqcRequired { get; set; } = true;
    public bool OqcRequired { get; set; }
    [MaxLength(30)] public string? Color { get; set; }
    [MaxLength(50)] public string? PackingMethod { get; set; }
    public int? MinPackingQty { get; set; }
    public bool IsStocked { get; set; } = true;
    [MaxLength(20)] public string? MaterialType { get; set; }
    [MaxLength(50)] public string? DesignOwner { get; set; }
// 新增欄位 - 基本屬性
    public int? ShelfLifeDays { get; set; }

// 新增欄位 - 庫存管理
    [Column(TypeName = "decimal(12,2)")] public decimal AvgDailyConsumption { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? PurchasePriceCeiling { get; set; }

    // 新增欄位 - 銷售定價
    [Column(TypeName = "decimal(14,4)")] public decimal? FloorPrice { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? DiscountRate { get; set; }
    [MaxLength(50)] public string? CommissionAccount { get; set; }
    [MaxLength(50)] public string? RelatedSkuId { get; set; }
    [MaxLength(50)] public string? CustomerItemCode { get; set; }

    // 新增欄位 - 生產製造
    [MaxLength(20)] public string? BomType { get; set; }
    [MaxLength(50)] public string? RoutingId { get; set; }
    [MaxLength(50)] public string? SubstituteSkuId { get; set; }

    // 新增欄位 - 委外管理
    public Guid? OutsourceSupplierId { get; set; }
    [MaxLength(50)] public string? ProcessingFeeAccount { get; set; }
    public Guid? OutsourceWarehouseId { get; set; }
    public bool IsSupplierProvided { get; set; }

    // Navigation properties
    [ForeignKey(nameof(DefaultWarehouseId))] public Warehouse? DefaultWarehouse { get; set; }
    [ForeignKey(nameof(DefaultSupplierId))] public Procurement.Supplier? DefaultSupplier { get; set; }
    [ForeignKey(nameof(DefaultBinLocationId))] public BinLocation? DefaultBinLocation { get; set; }
}

public class InventoryTransaction
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string TrxId { get; set; } = null!;
    [Required] public Guid SkuId { get; set; }
    [Required, MaxLength(50)] public string Type { get; set; } = null!;
    [Column(TypeName = "decimal(14,2)")] public decimal QtyChange { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal QtyBefore { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal QtyAfter { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [MaxLength(50)] public string? RelatedDocType { get; set; }
    public Guid? RelatedDocId { get; set; }
    public Guid? ReversalOfTransactionId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? BinLocationId { get; set; }
    public Guid? HandlerId { get; set; }
    public Guid? ApprovedBy { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal? UnitCost { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? TotalCost { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SkuId))] public Sku Sku { get; set; } = null!;
    [ForeignKey(nameof(WarehouseId))] public Warehouse? Warehouse { get; set; }
    [ForeignKey(nameof(BinLocationId))] public BinLocation? BinLocation { get; set; }
}

public class ProductionReceipt
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ReceiptNumber { get; set; } = null!;
    [Required] public Guid WorkOrderId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "posted";
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string? ReceivedBy { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder WorkOrder { get; set; } = null!;
    public ICollection<ProductionReceiptItem> Items { get; set; } = new List<ProductionReceiptItem>();
}

public class ProductionReceiptItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid ProductionReceiptId { get; set; }
    [Required] public Guid FqcInspectionLotId { get; set; }
    [Required] public Guid SkuId { get; set; }
    [Required] public Guid WarehouseId { get; set; }
    [Required] public Guid BinLocationId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal UnitCost { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalCost { get; set; }

    [ForeignKey(nameof(ProductionReceiptId))] public ProductionReceipt ProductionReceipt { get; set; } = null!;
    [ForeignKey(nameof(FqcInspectionLotId))] public FqcInspectionLot FqcInspectionLot { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Sku Sku { get; set; } = null!;
    [ForeignKey(nameof(WarehouseId))] public Warehouse Warehouse { get; set; } = null!;
    [ForeignKey(nameof(BinLocationId))] public BinLocation BinLocation { get; set; } = null!;
}

public class InventoryLot
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid SkuId { get; set; }
    [Required, MaxLength(50)] public string LotNumber { get; set; } = null!;
    [Column(TypeName = "decimal(14,2)")] public decimal Qty { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? BinLocationId { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "active";

    [ForeignKey(nameof(SkuId))] public Sku Sku { get; set; } = null!;
}

public class SerialNumber
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string SerialNo { get; set; } = null!;
    [Required] public Guid SkuId { get; set; }
    [MaxLength(50)] public string? SkuNumber { get; set; }
    [MaxLength(200)] public string? SkuName { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [MaxLength(100)] public string? Warehouse { get; set; }
    [MaxLength(50)] public string? Location { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "in_stock";
    public DateTime? ManufactureDate { get; set; }
    public DateTime? WarrantyExpiry { get; set; }

    [ForeignKey(nameof(SkuId))] public Sku Sku { get; set; } = null!;
}

public class MaterialIssue
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string IssueNumber { get; set; } = null!;
    [Required] public Guid WorkOrderId { get; set; }
    public Guid? ReversalOfIssueId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? IssueWarehouseId { get; set; }
    [MaxLength(20)] public string? BomVersion { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public Guid? IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ReversalOfIssueId))] public MaterialIssue? OriginalIssue { get; set; }
    [ForeignKey(nameof(WorkOrderId))] public WorkOrder? WorkOrder { get; set; }
    [ForeignKey(nameof(IssueWarehouseId))] public Warehouse? IssueWarehouse { get; set; }
    public ICollection<MaterialIssueItem> Items { get; set; } = new List<MaterialIssueItem>();
    public ICollection<MaterialIssue> Reversals { get; set; } = new List<MaterialIssue>();
}

public class MaterialIssueItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid IssueId { get; set; }
    [Required] public Guid SkuId { get; set; }
    public Guid? WorkOrderMaterialId { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [MaxLength(20)] public string? Unit { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal RequiredQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal IssuedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ShortageQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ClosedShortageQty { get; set; }
    [MaxLength(500)] public string? ShortageClosureReason { get; set; }
    public Guid? BinLocationId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";

    [ForeignKey(nameof(IssueId))] public MaterialIssue Issue { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Sku Sku { get; set; } = null!;
    [ForeignKey(nameof(WorkOrderMaterialId))] public WorkOrderMaterial? WorkOrderMaterial { get; set; }
    public ICollection<MaterialIssueItemAllocation> Allocations { get; set; } = new List<MaterialIssueItemAllocation>();
}

public class MaterialIssueItemAllocation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid IssueItemId { get; set; }
    [Required] public Guid WarehouseId { get; set; }
    public Guid? BinLocationId { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal IssuedQty { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal UnitCost { get; set; }
    public Guid? InventoryTransactionId { get; set; }

    [ForeignKey(nameof(IssueItemId))] public MaterialIssueItem IssueItem { get; set; } = null!;
    [ForeignKey(nameof(WarehouseId))] public Warehouse Warehouse { get; set; } = null!;
    [ForeignKey(nameof(BinLocationId))] public BinLocation? BinLocation { get; set; }
    [ForeignKey(nameof(InventoryTransactionId))] public InventoryTransaction? InventoryTransaction { get; set; }
}

public class MaterialReturn
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ReturnNumber { get; set; } = null!;
    [Required] public Guid WorkOrderId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ReturnWarehouseId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public Guid? ReturnedBy { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MaterialReturnItem> Items { get; set; } = new List<MaterialReturnItem>();
}

public class MaterialReturnItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid ReturnId { get; set; }
    [Required] public Guid SkuId { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ReturnQty { get; set; }
    [MaxLength(500)] public string? Reason { get; set; }

    [ForeignKey(nameof(ReturnId))] public MaterialReturn Return { get; set; } = null!;
}

public class MaterialSupplement
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string SupplementNumber { get; set; } = null!;
    [Required] public Guid WorkOrderId { get; set; }
    public Guid? DepartmentId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    [MaxLength(500)] public string? Reason { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MaterialSupplementItem> Items { get; set; } = new List<MaterialSupplementItem>();
}

public class MaterialSupplementItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid SupplementId { get; set; }
    [Required] public Guid SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BomStandardQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ActualConsumedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal SupplementQty { get; set; }

    [ForeignKey(nameof(SupplementId))] public MaterialSupplement Supplement { get; set; } = null!;
}

public class SubcontractOrder
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string SubcontractNumber { get; set; } = null!;
    [Required] public Guid WorkOrderId { get; set; }
    [Required] public Guid SupplierId { get; set; }
    public Guid? OperationId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public DateTime? IssuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SubcontractItem> Items { get; set; } = new List<SubcontractItem>();
}

public class SubcontractItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid SubcontractOrderId { get; set; }
    [Required] public Guid SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal IssuedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ReceivedQty { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }

    [ForeignKey(nameof(SubcontractOrderId))] public SubcontractOrder SubcontractOrder { get; set; } = null!;
}

public class TransferOrder
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string TransferNumber { get; set; } = null!;
    [Required] public Guid SourceWarehouseId { get; set; }
    [Required] public Guid TargetWarehouseId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public Guid? ApprovedBy { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TransferOrderItem> Items { get; set; } = new List<TransferOrderItem>();
}

public class TransferOrderItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid TransferOrderId { get; set; }
    [Required] public Guid SkuId { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TransferQty { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";

    [ForeignKey(nameof(TransferOrderId))] public TransferOrder TransferOrder { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Sku? Sku { get; set; }
}

public class CompletionOrder
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string CompletionNumber { get; set; } = null!;
    [Required] public Guid WorkOrderId { get; set; }
    [Required] public Guid WarehouseId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    [MaxLength(20)] public string QaStatus { get; set; } = "pending";
    public Guid? ReceivedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CompletionOrderItem> Items { get; set; } = new List<CompletionOrderItem>();
}

public class CompletionOrderItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid CompletionOrderId { get; set; }
    [Required] public Guid SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ReceivedQty { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? BinLocationId { get; set; }

    [ForeignKey(nameof(CompletionOrderId))] public CompletionOrder CompletionOrder { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Sku? Sku { get; set; }
}

public class DemandForecast
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid SkuId { get; set; }
    [MaxLength(7)] public string ForecastMonth { get; set; } = null!;
    [Column(TypeName = "decimal(14,2)")] public decimal ForecastQty { get; set; }
    [MaxLength(50)] public string Algorithm { get; set; } = "moving_average";
    [Column(TypeName = "decimal(5,2)")] public decimal? Accuracy { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? ActualQty { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AutoReplenishment
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal CurrentQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Rop { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal SuggestedQty { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public Guid? RelatedPrId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ExpiryAlert
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid LotId { get; set; }
    [Required] public Guid SkuId { get; set; }
    [MaxLength(20)] public string AlertLevel { get; set; } = "notice";
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool Acknowledged { get; set; }
    public Guid? AcknowledgedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StocktakeOrder
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string StocktakeNumber { get; set; } = null!;
    public Guid? WarehouseId { get; set; }
    [MaxLength(30)] public string StocktakeType { get; set; } = "cycle";
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<StocktakeOrderItem> Items { get; set; } = new List<StocktakeOrderItem>();
}

public class StocktakeOrderItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid StocktakeOrderId { get; set; }
    [Required] public Guid SkuId { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal SystemQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ActualQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Difference { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? DifferenceAmount { get; set; }
    [MaxLength(500)] public string? Reason { get; set; }

    [ForeignKey(nameof(StocktakeOrderId))] public StocktakeOrder StocktakeOrder { get; set; } = null!;
}

public class AsrsTask
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string TaskNumber { get; set; } = null!;
    [Required, MaxLength(30)] public string TaskType { get; set; } = null!;
    [MaxLength(100)] public string? SourceLocation { get; set; }
    [MaxLength(100)] public string? TargetLocation { get; set; }
    [MaxLength(50)] public string? Lpn { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    [MaxLength(50)] public string? ErrorCode { get; set; }
    public DateTime? ErpSentAt { get; set; }
    public DateTime? AsrsCompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
