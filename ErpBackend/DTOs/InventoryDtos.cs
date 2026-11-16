using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class CreateWarehouseRequestDto
{
    public string WarehouseCode { get; set; } = null!;
    public string WarehouseName { get; set; } = null!;
    public string? Address { get; set; }
    public string Type { get; set; } = "normal";
    public bool IsActive { get; set; } = true;
}

public class UpdateWarehouseRequestDto
{
    public string? WarehouseCode { get; set; }
    public string? WarehouseName { get; set; }
    public string? Address { get; set; }
    public string? Type { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateBinLocationRequestDto
{
    public Guid WarehouseId { get; set; }
    public string LocationCode { get; set; } = null!;
    public string? Zone { get; set; }
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Level { get; set; }
    public string? Position { get; set; }
}

public class UpdateBinLocationRequestDto
{
    public Guid WarehouseId { get; set; }
    public string LocationCode { get; set; } = null!;
    public string? Zone { get; set; }
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Level { get; set; }
    public string? Position { get; set; }
}

public class InventoryAdjustRequest
{
    public string SkuCode { get; set; } = null!;
    public string WarehouseCode { get; set; } = null!;
    public string? BinCode { get; set; }
    public decimal QtyChange { get; set; }
    public string? LotNumber { get; set; }
    public string? Notes { get; set; }
}

public class InventoryStocktakeRequestDto
{
    [Required, MaxLength(50)] public string SkuCode { get; set; } = null!;
    [Required, MaxLength(20)] public string WarehouseCode { get; set; } = null!;
    [Required, MaxLength(50)] public string LotNumber { get; set; } = null!;
    [Required, DataType(DataType.Date)] public DateTime? ExpiryDate { get; set; }
    [Range(typeof(decimal), "0", "999999999")] public decimal CountedQty { get; set; }
    [Range(typeof(decimal), "0", "999999999")] public decimal ExpectedSystemQty { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}

public class InventoryStocktakeBalanceDto
{
    public string SkuCode { get; set; } = null!;
    public string SkuName { get; set; } = null!;
    public string WarehouseCode { get; set; } = null!;
    public string LotNumber { get; set; } = null!;
    public DateTime? ExpiryDate { get; set; }
    public decimal SystemQty { get; set; }
}

public class InventoryStocktakeResultDto
{
    public string? TrxId { get; set; }
    public string SkuCode { get; set; } = null!;
    public string WarehouseCode { get; set; } = null!;
    public string LotNumber { get; set; } = null!;
    public decimal QtyBefore { get; set; }
    public decimal CountedQty { get; set; }
    public decimal QtyChange { get; set; }
    public decimal QtyAfter { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class InventoryTransactionRequest
{
    public string SkuCode { get; set; } = null!;
    public string WarehouseCode { get; set; } = null!;
    public string? BinCode { get; set; }
    public string Type { get; set; } = null!;
    public decimal Qty { get; set; }
    public string? LotNumber { get; set; }
    public string? RelatedDocType { get; set; }
    public Guid? RelatedDocId { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }
}

public class TransferOrderItemCreateDto
{
    public Guid SkuId { get; set; }
    public string? LotNumber { get; set; }
    public decimal TransferQty { get; set; }
}

public class CreateTransferOrderDto
{
    public string TransferNumber { get; set; } = null!;
    public Guid SourceWarehouseId { get; set; }
    public Guid TargetWarehouseId { get; set; }
    public List<TransferOrderItemCreateDto> Items { get; set; } = new();
}

public class CompletionOrderItemCreateDto
{
    public Guid SkuId { get; set; }
    public string? LotNumber { get; set; }
    public decimal ReceivedQty { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? BinLocationId { get; set; }
}

public class CreateCompletionOrderDto
{
    public string CompletionNumber { get; set; } = null!;
    public Guid WorkOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public List<CompletionOrderItemCreateDto> Items { get; set; } = new();
}
