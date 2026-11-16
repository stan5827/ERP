using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class CreateFqcInspectionLotRequestDto
{
    public Guid WorkOrderId { get; set; }
    [Required, MaxLength(50)] public string LotNumber { get; set; } = null!;
    [DataType(DataType.Date)] public DateTime? ExpiryDate { get; set; }
    [Range(typeof(decimal), "0.01", "999999999")] public decimal SampledQty { get; set; }
    [Range(typeof(decimal), "0", "999999999")] public decimal DefectiveQty { get; set; }
    public bool CriticalDefectFound { get; set; }
    [MaxLength(50)] public string? DefectCode { get; set; }
    [Required, MaxLength(100)] public string InspectorName { get; set; } = null!;
    [MaxLength(1000)] public string? Notes { get; set; }
}

public class FqcWorkOrderOptionDto
{
    public Guid Id { get; set; }
    public string WoNumber { get; set; } = null!;
    public Guid ProductSkuId { get; set; }
    public string? ProductSkuNumber { get; set; }
    public string? ProductSkuName { get; set; }
    public string Status { get; set; } = null!;
}

public class FqcFinishedLotOptionDto
{
    public string LotNumber { get; set; } = null!;
    public decimal ProducedQty { get; set; }
}

public class FqcWarehouseOptionDto
{
    public Guid Id { get; set; }
    public string WarehouseCode { get; set; } = null!;
    public string WarehouseName { get; set; } = null!;
}

public class FqcBinLocationOptionDto
{
    public Guid Id { get; set; }
    public string LocationCode { get; set; } = null!;
}

public class DecideFqcInspectionLotRequestDto
{
    [Required] public string InspectionStatus { get; set; } = null!;
    [Range(typeof(decimal), "0", "999999999")] public decimal ApprovedQty { get; set; }
    public Guid WarehouseId { get; set; }
    [Required, MaxLength(100)] public string DecisionBy { get; set; } = null!;
    [MaxLength(1000)] public string? Notes { get; set; }
}

public class FqcInspectionLotDto
{
    public Guid Id { get; set; }
    public string InspectionNumber { get; set; } = null!;
    public Guid WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public Guid ProductSkuId { get; set; }
    public string? ProductSkuNumber { get; set; }
    public string LotNumber { get; set; } = null!;
    public DateTime? ExpiryDate { get; set; }
    public decimal ProducedQty { get; set; }
    public decimal SampledQty { get; set; }
    public decimal DefectiveQty { get; set; }
    public decimal ApprovedQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal AvailableReceiptQty => Math.Max(0, ApprovedQty - ReceivedQty);
    public bool CriticalDefectFound { get; set; }
    public string InspectionStatus { get; set; } = null!;
    public string DispositionStatus { get; set; } = null!;
    public string? DefectCode { get; set; }
    public string InspectorName { get; set; } = null!;
    public DateTime InspectedAt { get; set; }
    public string? DecisionBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public decimal QuarantinedQty { get; set; }
    public Guid? ReworkWorkOrderId { get; set; }
    public Guid? ReworkSourceInspectionLotId { get; set; }
    public string? WorkOrderType { get; set; }
    public string? WorkOrderStatus { get; set; }
    public string? DispositionBy { get; set; }
    public DateTime? DispositionAt { get; set; }
    public string? Notes { get; set; }
    public FqcScrapNoteDto? ScrapNote { get; set; }
}

public class CreateProductionReceiptRequestDto
{
    public Guid FqcInspectionLotId { get; set; }
    public decimal Quantity { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid BinLocationId { get; set; }
    [MaxLength(100)] public string? ReceivedBy { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}

public class CreateFqcScrapNoteRequestDto
{
    [Required, RegularExpression("^(01|02|03|04|05)$")]
    public string ReasonCode { get; set; } = null!;
    [Required, MaxLength(500)] public string Reason { get; set; } = null!;
}

public class FqcScrapNoteDto
{
    public Guid Id { get; set; }
    public string ScrapNumber { get; set; } = null!;
    public Guid FqcInspectionLotId { get; set; }
    public string? InspectionNumber { get; set; }
    public string? LotNumber { get; set; }
    public decimal Quantity { get; set; }
    public string Status { get; set; } = null!;
    public string? ReasonCode { get; set; }
    public string? Reason { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public string? RequestedByName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<FqcScrapApprovalEventDto> ApprovalEvents { get; set; } = [];
}

public class FqcScrapApprovalEventDto
{
    public Guid Id { get; set; }
    public string Stage { get; set; } = null!;
    public string Decision { get; set; } = null!;
    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = null!;
    public string? Comments { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class DecideFqcScrapReviewRequestDto
{
    [Required] public bool Approve { get; set; }
    [MaxLength(1000)] public string? Comments { get; set; }
}

public class ProductionReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = null!;
    public Guid WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public Guid FqcInspectionLotId { get; set; }
    public string? FqcInspectionNumber { get; set; }
    public string? LotNumber { get; set; }
    public Guid SkuId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid BinLocationId { get; set; }
    public DateTime ReceivedAt { get; set; }
}
