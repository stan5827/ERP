using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public sealed record SupplierPortalComplaintSummaryDto(
    Guid ComplaintId,
    string ComplaintNumber,
    Guid? PurchaseOrderId,
    Guid? SkuId,
    string? LotNumber,
    decimal DefectQuantity,
    decimal Ppm,
    string? DefectType,
    string Priority,
    string? Description,
    string Status,
    DateTimeOffset CreatedAt,
    string? CapaNumber,
    string? CapaStatus,
    DateTimeOffset? ResponseDue,
    DateTimeOffset? SupplierRespondedAt);

public sealed record SupplierPortalCapaRevisionDto(
    int RevisionNumber,
    string Action,
    string? D1Team,
    string? D2ProblemDescription,
    string? D3InterimContainment,
    string? D4RootCause,
    string? D5PermanentAction,
    string? D6Verification,
    string? D7Prevention,
    string? D8Closure,
    DateTimeOffset CreatedAt);

public sealed record SupplierPortalComplaintDetailDto(
    Guid ComplaintId,
    string ComplaintNumber,
    Guid? PurchaseOrderId,
    Guid? SkuId,
    string? LotNumber,
    decimal DefectQuantity,
    decimal Ppm,
    string? DefectType,
    string Priority,
    string? Description,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? CapaId,
    string? CapaNumber,
    string? CapaStatus,
    DateTimeOffset? ResponseDue,
    DateTimeOffset? SupplierRespondedAt,
    bool IsOverdue,
    string? D1Team,
    string? D2ProblemDescription,
    string? D3InterimContainment,
    string? D4RootCause,
    string? D5PermanentAction,
    string? D6Verification,
    string? D7Prevention,
    string? D8Closure,
    IReadOnlyList<SupplierPortalCapaRevisionDto> Revisions);

public sealed record SupplierPortalCapaResponseRequestDto
{
    [MaxLength(500)]
    public string? D1Team { get; init; }

    [MaxLength(8000)]
    public string? D2ProblemDescription { get; init; }

    [MaxLength(8000)]
    public string? D3InterimContainment { get; init; }

    [MaxLength(8000)]
    public string? D4RootCause { get; init; }

    [MaxLength(8000)]
    public string? D5PermanentAction { get; init; }

    [MaxLength(8000)]
    public string? D6Verification { get; init; }

    [MaxLength(8000)]
    public string? D7Prevention { get; init; }

    [MaxLength(8000)]
    public string? D8Closure { get; init; }
}

public sealed record SupplierPortalCapaResponseSavedDto(
    Guid ComplaintId,
    Guid CapaId,
    string CapaNumber,
    string Status,
    int RevisionNumber,
    string Action,
    DateTimeOffset RecordedAt);
