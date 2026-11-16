namespace ErpBackend.DTOs;

public class MaterialIssueRequest
{
    public string WoNumber { get; set; } = null!;
    public string SkuCode { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string? LotNumber { get; set; }
}

public class MaterialIssueDto
{
    public Guid Id { get; set; }
    public string IssueNumber { get; set; } = null!;
    public Guid WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public Guid? IssueWarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public string? WarehouseName { get; set; }
    public string? BomVersion { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public Guid? ReversalOfIssueId { get; set; }
    public string? Notes { get; set; }
    public List<MaterialIssueItemDto> Items { get; set; } = new();
}

public class MaterialIssueItemDto
{
    public Guid Id { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public Guid? WorkOrderMaterialId { get; set; }
    public string? Unit { get; set; }
    public string? InventoryUnit { get; set; }
    public decimal RequiredQty { get; set; }
    public decimal IssuedQty { get; set; }
    public decimal ShortageQty { get; set; }
    public decimal ClosedShortageQty { get; set; }
    public string Status { get; set; } = null!;
    public string? ShortageClosureReason { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public Guid? DefaultBinLocationId { get; set; }
    public bool LotControlled { get; set; }
    public bool SerialControlled { get; set; }
    public decimal OnHandQty { get; set; }
    public decimal DefaultIssueQty { get; set; }
    public List<MaterialIssueAllocationDto> Allocations { get; set; } = new();
}

public class MaterialIssueAllocationDto
{
    public Guid Id { get; set; }
    public Guid WarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? BinLocationId { get; set; }
    public string? BinLocationCode { get; set; }
    public string? LotNumber { get; set; }
    public decimal IssuedQty { get; set; }
    public decimal UnitCost { get; set; }
    public Guid? InventoryTransactionId { get; set; }
}

public class PostMaterialIssueRequestDto
{
    public List<MaterialIssueAllocationRequestDto> Allocations { get; set; } = new();
}

public class MaterialIssueAllocationRequestDto
{
    public Guid IssueItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinLocationId { get; set; }
    public string? LotNumber { get; set; }
    public decimal IssuedQty { get; set; }
}

public class CloseMaterialIssueShortageRequestDto
{
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = null!;
}
