using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public sealed class SavePayrollPolicyRequestDto
{
    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$")]
    public string VersionCode { get; set; } = string.Empty;

    [Required]
    public DateOnly EffectiveFrom { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string ApprovalDocumentReference { get; set; } = string.Empty;

    [Required, StringLength(100000, MinimumLength = 2)]
    public string PolicyJson { get; set; } = "{}";
}

public sealed class ReviewPayrollPolicyRequestDto
{
    public bool ConfirmedOfficialSources { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

public sealed class SubmitPayrollPolicyRequestDto
{
    public bool ConfirmedOfficialSources { get; set; }
}

public sealed record PayrollPolicyAuditEventDto(
    string Action,
    Guid ActorUserId,
    string ActorUsername,
    DateTimeOffset OccurredAt,
    string? Details);

public sealed record PayrollPolicyVersionDto(
    Guid Id,
    string VersionCode,
    DateOnly EffectiveFrom,
    string ApprovalDocumentReference,
    string PolicyJson,
    string Status,
    Guid CreatedByUserId,
    string CreatedByUsername,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid? SubmittedByUserId,
    string? SubmittedByUsername,
    DateTimeOffset? SubmittedAt,
    Guid? FinanceReviewerUserId,
    string? FinanceReviewerUsername,
    DateTimeOffset? FinanceReviewedAt,
    string? ReviewComment,
    bool CalculationEnabled,
    IReadOnlyList<PayrollPolicyAuditEventDto> AuditTrail);

public sealed record PayrollPolicyOperationResultDto(
    PayrollPolicyVersionDto? Policy,
    string? Error,
    IReadOnlyList<string>? MissingFields = null);
