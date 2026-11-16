using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

/// <summary>Lists an advance shipment notice submitted by the authenticated supplier.</summary>
public sealed record SupplierPortalAsnSummaryDto(
    Guid AsnId,
    string AsnNumber,
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    DateTimeOffset ShipDate,
    DateTimeOffset? ExpectedArrival,
    string Status,
    string? Carrier,
    string? TrackingNumber,
    int ItemCount,
    DateTimeOffset CreatedAt);

/// <summary>Describes a shipped purchase-order item including its supplier lot details.</summary>
public sealed record SupplierPortalAsnItemDto(
    Guid SkuId,
    string? SkuNumber,
    string? SkuName,
    decimal ShippedQuantity,
    string? LotNumber,
    DateTimeOffset? ManufactureDate,
    DateTimeOffset? ExpiryDate);

/// <summary>Returns a shipment notice and its items to the supplier that submitted it.</summary>
public sealed record SupplierPortalAsnDetailDto(
    Guid AsnId,
    string AsnNumber,
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    DateTimeOffset ShipDate,
    DateTimeOffset? ExpectedArrival,
    string Status,
    string? Carrier,
    string? TrackingNumber,
    DateTimeOffset CreatedAt,
    IReadOnlyList<SupplierPortalAsnItemDto> Items);

/// <summary>Describes a purchase-order item with its remaining quantity available to ship.</summary>
public sealed record SupplierPortalAsnOrderItemDto(
    Guid SkuId,
    string? SkuNumber,
    string? SkuName,
    string? Unit,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal ReservedForShipmentQuantity,
    decimal AvailableToShipQuantity,
    bool LotControlled);

/// <summary>Returns an eligible purchase order and its available shipment quantities.</summary>
public sealed record SupplierPortalAsnOrderOptionsDto(
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    string Status,
    DateTimeOffset? ExpectedDeliveryDate,
    IReadOnlyList<SupplierPortalAsnOrderItemDto> Items);

/// <summary>Creates an advance shipment notice for the authenticated supplier's purchase order.</summary>
public sealed record CreateSupplierPortalAsnRequestDto
{
    [Required]
    public required Guid PurchaseOrderId { get; init; }

    [Required]
    public required DateTimeOffset ShipDate { get; init; }

    [Required]
    public required DateTimeOffset ExpectedArrival { get; init; }

    [MaxLength(100)]
    public string? Carrier { get; init; }

    [MaxLength(100)]
    public string? TrackingNumber { get; init; }

    [MaxLength(500)]
    public string? Remarks { get; init; }

    [Required, MinLength(1), MaxLength(100)]
    public required IReadOnlyList<SupplierPortalAsnItemRequestDto> Items { get; init; }
}

/// <summary>Describes one shipped SKU and its lot traceability information.</summary>
public sealed record SupplierPortalAsnItemRequestDto
{
    [Required]
    public required Guid SkuId { get; init; }

    [Range(typeof(decimal), "0.01", "999999999.99")]
    public required decimal ShippedQuantity { get; init; }

    [MaxLength(50)]
    public string? LotNumber { get; init; }

    public DateTimeOffset? ManufactureDate { get; init; }

    public DateTimeOffset? ExpiryDate { get; init; }
}

/// <summary>Confirms that an advance shipment notice was recorded.</summary>
public sealed record SupplierPortalAsnSubmittedDto(
    Guid AsnId,
    string AsnNumber,
    string Status,
    DateTimeOffset SubmittedAt);
