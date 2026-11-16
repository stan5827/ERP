namespace ErpBackend.DTOs;

public sealed record SupplierApSubmissionLineRequestDto(
    Guid GoodsReceiptItemId,
    decimal InvoicedQuantity,
    decimal UnitPrice,
    decimal NetAmount,
    decimal TaxAmount,
    decimal TotalAmount);

public sealed record SupplierApSubmissionDraftRequestDto(
    string SupplierInvoiceNumber,
    DateOnly SupplierInvoiceDate,
    string Currency,
    decimal InvoiceAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    Guid PurchaseOrderId,
    Guid GoodsReceiptId,
    IReadOnlyList<SupplierApSubmissionLineRequestDto> Lines);

public sealed record SupplierApSubmissionCreateRequestDto(
    string SupplierInvoiceNumber,
    DateOnly SupplierInvoiceDate,
    string Currency,
    decimal InvoiceAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    Guid PurchaseOrderId,
    Guid GoodsReceiptId,
    IReadOnlyList<SupplierApSubmissionLineRequestDto> Lines);

public sealed record SupplierApSubmissionReviewRequestDto(
    string? Reason);

public sealed record SupplierApSubmissionCancelRequestDto(
    string? Reason);

public sealed record SupplierApSubmissionLineDto(
    Guid GoodsReceiptItemId,
    Guid? PurchaseOrderItemId,
    string? SkuNumber,
    string? SkuName,
    decimal AcceptedQuantity,
    decimal InvoicedQuantity,
    decimal UnitPrice,
    decimal NetAmount,
    decimal TaxAmount,
    decimal TotalAmount);

public sealed record SupplierApSubmissionAttachmentDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string ScanStatus,
    DateTimeOffset UploadedAt,
    DateTimeOffset? ScannedAt,
    int RevisionNumber);

public sealed record SupplierApSubmissionReviewDto(
    Guid Id,
    string Action,
    string? Reason,
    string MatchResult,
    DateTimeOffset CreatedAt,
    Guid? ReviewerUserId);

public sealed record SupplierApSubmissionSummaryDto(
    Guid Id,
    string SubmissionNumber,
    string Status,
    int CurrentRevisionNumber,
    string SupplierInvoiceNumber,
    DateOnly SupplierInvoiceDate,
    string Currency,
    decimal TotalAmount,
    string MatchResult,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt);

public sealed record SupplierApSubmissionDetailDto(
    Guid Id,
    string SubmissionNumber,
    string Status,
    int CurrentRevisionNumber,
    string SupplierInvoiceNumber,
    DateOnly SupplierInvoiceDate,
    string Currency,
    decimal InvoiceAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    Guid GoodsReceiptId,
    string GoodsReceiptNumber,
    string MatchResult,
    IReadOnlyList<string> Discrepancies,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? ApprovedAt,
    Guid? AccountsPayableId,
    string? AccountsPayableNumber,
    IReadOnlyList<SupplierApSubmissionLineDto> Lines,
    IReadOnlyList<SupplierApSubmissionAttachmentDto> Attachments,
    IReadOnlyList<SupplierApSubmissionReviewDto> Reviews);

public sealed record SupplierApSubmissionPageDto(
    IReadOnlyList<SupplierApSubmissionSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record SupplierApEligibleSourceLineDto(
    Guid GoodsReceiptItemId,
    Guid? PurchaseOrderItemId,
    Guid SkuId,
    string? SkuNumber,
    string? SkuName,
    decimal AcceptedQuantity,
    decimal PreviouslyInvoicedQuantity,
    decimal AvailableQuantity,
    decimal PurchaseOrderUnitPrice,
    string Unit);

public sealed record SupplierApEligibleSourceDto(
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    Guid GoodsReceiptId,
    string GoodsReceiptNumber,
    string Currency,
    IReadOnlyList<SupplierApEligibleSourceLineDto> Lines);

public sealed record SupplierApSubmissionResultDto(
    Guid SubmissionId,
    string SubmissionNumber,
    string Status,
    int RevisionNumber,
    int Version,
    string MatchResult,
    IReadOnlyList<string> Discrepancies,
    DateTimeOffset? SubmittedAt,
    Guid? AccountsPayableId,
    string? AccountsPayableNumber);

public sealed record SupplierApSubmissionAttachmentDownloadDto(
    SupplierApSubmissionAttachmentDto Metadata,
    Stream Content);
