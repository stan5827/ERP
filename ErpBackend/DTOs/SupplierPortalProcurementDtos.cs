using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

/// <summary>Lists an RFQ assigned to the authenticated supplier.</summary>
public sealed record SupplierPortalRfqSummaryDto(
    Guid RfqId,
    string RfqNumber,
    DateTimeOffset RfqDate,
    DateTimeOffset? Deadline,
    string Status,
    int ItemCount,
    int QuotedItemCount);

/// <summary>Describes one item requested in an assigned RFQ and its current supplier quote.</summary>
public sealed record SupplierPortalRfqItemDto(
    Guid SkuId,
    string? SkuNumber,
    string? SkuName,
    decimal Quantity,
    string? Unit,
    string? Specifications,
    decimal? UnitPrice,
    DateTimeOffset? PromisedDeliveryDate,
    int? LeadTimeDays,
    decimal? MinimumOrderQuantity,
    string? TradeTerms,
    string? Notes,
    DateTimeOffset? QuotedAt);

/// <summary>Returns the details of an RFQ visible to the authenticated supplier.</summary>
public sealed record SupplierPortalRfqDetailDto(
    Guid RfqId,
    string RfqNumber,
    DateTimeOffset RfqDate,
    DateTimeOffset? Deadline,
    string Status,
    string Currency,
    IReadOnlyList<SupplierPortalRfqItemDto> Items);

/// <summary>Submits or updates the authenticated supplier's quote for selected RFQ items.</summary>
public sealed record SubmitSupplierRfqQuoteRequestDto
{
    [Required, MinLength(1)]
    public required IReadOnlyList<SupplierRfqQuoteLineRequestDto> Items { get; init; }
}

/// <summary>Contains one quoted RFQ item.</summary>
public sealed record SupplierRfqQuoteLineRequestDto
{
    [Required]
    public required Guid SkuId { get; init; }

    [Range(typeof(decimal), "0.0001", "999999999.9999")]
    public required decimal UnitPrice { get; init; }

    [Range(0, 3650)]
    public int LeadTimeDays { get; init; }

    [Range(typeof(decimal), "0", "999999999.99")]
    public decimal MinimumOrderQuantity { get; init; }

    public DateTimeOffset? PromisedDeliveryDate { get; init; }

    [MaxLength(50)]
    public string? TradeTerms { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}

/// <summary>Confirms the authenticated supplier's own quote submission.</summary>
public sealed record SupplierPortalQuoteSubmittedDto(
    Guid RfqId,
    int QuotedItemCount,
    DateTimeOffset SubmittedAt);

/// <summary>Lists a purchase order issued to the authenticated supplier.</summary>
public sealed record SupplierPortalPurchaseOrderSummaryDto(
    Guid PurchaseOrderId,
    string PoNumber,
    DateTimeOffset PoDate,
    DateTimeOffset? ExpectedDeliveryDate,
    string Status,
    int Version,
    string Currency,
    decimal TotalAmount,
    int ItemCount,
    string? LatestResponseType,
    int? LatestResponseVersion,
    DateTimeOffset? LatestResponseAt);

/// <summary>Describes one purchase-order line visible to its supplier.</summary>
public sealed record SupplierPortalPurchaseOrderItemDto(
    Guid PurchaseOrderItemId,
    Guid SkuId,
    string? SkuNumber,
    string? SkuName,
    decimal Quantity,
    string? Unit,
    decimal UnitPrice,
    decimal Subtotal);

/// <summary>Describes one line-level alternative proposed by a supplier.</summary>
public sealed record SupplierPortalPurchaseOrderProposalItemDto(
    Guid PurchaseOrderItemId,
    decimal? ProposedQuantity,
    DateTimeOffset? ProposedDeliveryDate);

/// <summary>Describes one immutable supplier response to a purchase order.</summary>
public sealed record SupplierPortalPurchaseOrderResponseHistoryDto(
    Guid ResponseId,
    string ResponseType,
    string? Reason,
    int PurchaseOrderVersion,
    DateTimeOffset SubmittedAt,
    IReadOnlyList<SupplierPortalPurchaseOrderProposalItemDto> Items);

/// <summary>Returns a purchase order and its supplier-visible response history.</summary>
public sealed record SupplierPortalPurchaseOrderDetailDto(
    Guid PurchaseOrderId,
    string PoNumber,
    DateTimeOffset PoDate,
    DateTimeOffset? ExpectedDeliveryDate,
    string Status,
    int Version,
    string Currency,
    string? PaymentTerms,
    string? TradeTerms,
    string? Notes,
    decimal Subtotal,
    decimal TaxAmount,
    decimal TotalAmount,
    IReadOnlyList<SupplierPortalPurchaseOrderItemDto> Items,
    IReadOnlyList<SupplierPortalPurchaseOrderResponseHistoryDto> ResponseHistory);

/// <summary>Submits an acceptance, change request, or rejection for a purchase order.</summary>
public sealed record SubmitSupplierPurchaseOrderResponseRequestDto
{
    [Required, RegularExpression("^(accepted|change_requested|rejected)$")]
    public required string ResponseType { get; init; }

    [MaxLength(500)]
    public string? Reason { get; init; }

    public IReadOnlyList<SupplierPurchaseOrderProposalItemRequestDto> Items { get; init; } =
        Array.Empty<SupplierPurchaseOrderProposalItemRequestDto>();
}

/// <summary>Provides an optional replacement quantity or delivery date for a purchase-order line.</summary>
public sealed record SupplierPurchaseOrderProposalItemRequestDto
{
    [Required]
    public required Guid PurchaseOrderItemId { get; init; }

    [Range(typeof(decimal), "0.01", "999999999.99")]
    public decimal? ProposedQuantity { get; init; }

    public DateTimeOffset? ProposedDeliveryDate { get; init; }
}

/// <summary>Confirms that a supplier purchase-order response was recorded.</summary>
public sealed record SupplierPortalPurchaseOrderResponseSubmittedDto(
    Guid PurchaseOrderId,
    Guid ResponseId,
    string ResponseType,
    DateTimeOffset SubmittedAt);

/// <summary>Assigns an RFQ to the selected suppliers.</summary>
public sealed record AssignRfqSuppliersRequestDto
{
    [Required, MinLength(1)]
    public required IReadOnlyList<Guid> SupplierIds { get; init; }
}
