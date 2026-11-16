using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public sealed record SupplierCapaReviewDto(
    Guid ReviewId,
    Guid CapaId,
    Guid ResponseRevisionId,
    int RevisionNumber,
    Guid ReviewerUserId,
    string Decision,
    string? Comments,
    DateTimeOffset CreatedAt);

public sealed record SupplierCapaReviewRequestDto
{
    [Required]
    [RegularExpression("^(approve|revision_requested)$")]
    public required string Decision { get; init; }

    [Range(1, int.MaxValue)]
    public required int RevisionNumber { get; init; }

    [MaxLength(2000)]
    public string? Comments { get; init; }
}

public sealed record SupplierCapaCloseRequestDto
{
    [Required, MaxLength(2000)]
    public required string Comments { get; init; }
}
