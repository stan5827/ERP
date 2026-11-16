using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class SkuDto
{
    public Guid Id { get; set; }
    public string SkuNumber { get; set; } = null!;
    public string SkuName { get; set; } = null!;
    public string? Category { get; set; }
    public string? Unit { get; set; }
    public Guid? BaseUomId { get; set; }
    public string? BaseUomCode { get; set; }
    public string? BaseUomName { get; set; }
    public string? Barcode { get; set; }
    public bool LotControlled { get; set; }
    public bool SerialControlled { get; set; }
    public string? AbcClassification { get; set; }
    public decimal StandardCost { get; set; }
    public string? SourceType { get; set; }
    public bool IsActive { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal? AvgDailyConsumption { get; set; }
    public decimal? PurchasePriceCeiling { get; set; }
    public decimal? FloorPrice { get; set; }
    public decimal? DiscountRate { get; set; }
    public string? CommissionAccount { get; set; }
    public string? RelatedSkuId { get; set; }
    public string? CustomerItemCode { get; set; }
    public string? Specification { get; set; }
    public string? ItemType { get; set; }
    public string? Model { get; set; }
    public string? Brand { get; set; }
    public string? Status { get; set; } = "active";
    public decimal Qty { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal MaxStock { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public Guid? DefaultBinLocationId { get; set; }
    public decimal? PriceA { get; set; }
    public decimal? PriceB { get; set; }
    public decimal? PriceC { get; set; }
    public decimal? PriceD { get; set; }
    public decimal? PriceE { get; set; }
    public string? Currency { get; set; } = "TWD";
    public bool IsSellable { get; set; }
    public bool IsPurchasable { get; set; }
    public bool IsSubstitute { get; set; }
    public bool IsAlternate { get; set; }
    public bool IqcRequired { get; set; }
    public bool OqcRequired { get; set; }
    public string? Color { get; set; }
    public string? PackingMethod { get; set; }
    public int? MinPackingQty { get; set; }
    public bool IsStocked { get; set; }
    public string? MaterialType { get; set; }
    public string? DesignOwner { get; set; }
    public int? ShelfLifeDays { get; set; }
    public string? BomType { get; set; }
    public string? RoutingId { get; set; }
    public string? SubstituteSkuId { get; set; }
    public Guid? OutsourceSupplierId { get; set; }
    public string? ProcessingFeeAccount { get; set; }
    public Guid? OutsourceWarehouseId { get; set; }
    public bool IsSupplierProvided { get; set; }
    public Guid? PurchaseUomId { get; set; }
    public string? PurchaseUomCode { get; set; }
    public string? PurchaseUomName { get; set; }
    public Guid? SalesUomId { get; set; }
    public string? SalesUomCode { get; set; }
    public string? SalesUomName { get; set; }
    public Guid? ProductionUomId { get; set; }
    public string? ProductionUomCode { get; set; }
    public string? ProductionUomName { get; set; }
    public decimal? PurchaseToBaseFactor { get; set; }
    public decimal? SalesToBaseFactor { get; set; }
    public decimal? ProductionToBaseFactor { get; set; }
    public string? DefaultWarehouseName { get; set; }
    public bool IsManufacturable { get; set; }
    public bool IsOutsourced { get; set; }
    public string? CostMethod { get; set; }
    public decimal? SuggestedPrice { get; set; }
    public decimal? BasePrice { get; set; }
    public bool IsBomTop { get; set; }
    public bool IsBomComponent { get; set; }
    public bool IsPhantom { get; set; }
    public bool IsMto { get; set; }
    public bool IsMts { get; set; }
    public bool IsCustom { get; set; }
    public string? MakeOrBuy { get; set; }
    public Guid? DefaultSupplierId { get; set; }
    public string? DefaultSupplierName { get; set; }
    public string? SupplierItemCode { get; set; }
    public int? SupplierLeadTime { get; set; }
    public string? DrawingNo { get; set; }
    public string? DrawingRevision { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? QualitySpec { get; set; }
    public string? ProductionDept { get; set; }
    public string? MachineCode { get; set; }
    public string? MoldCode { get; set; }
    public Guid? DefaultBomId { get; set; }
    public Guid? DefaultRoutingId { get; set; }
    public string? CostUnit { get; set; }
    public Guid? CostUomId { get; set; }
    public decimal LatestPurchaseCost { get; set; }
    public string? TaxCode { get; set; }
    public string? TaxRate { get; set; }
}

public class EmployeeDto
{
    public Guid Id { get; set; }
    public string EmployeeNo { get; set; } = null!;
    public string ChineseName { get; set; } = null!;
    public string? EnglishName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? JobTitleId { get; set; }
    public string? JobTitleName { get; set; }
    public Guid? GradeId { get; set; }
    public string? GradeName { get; set; }
    public string? Status { get; set; }
    public DateTime? HireDate { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class OrgUnitDto
{
    public Guid Id { get; set; }
    public string DeptCode { get; set; } = null!;
    public string DeptName { get; set; } = null!;
    public string? ParentDeptId { get; set; }
    public string? ManagerId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<OrgUnitDto> Children { get; set; } = new();
}
