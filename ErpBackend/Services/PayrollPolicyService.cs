using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public sealed class PayrollPolicyService : IPayrollPolicyService
{
    private readonly ErpGeneralDbContext _db;

    public PayrollPolicyService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PayrollPolicyVersionDto>> GetPoliciesAsync(CancellationToken cancellationToken)
    {
        var policies = await _db.PayrollPolicyVersions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        policies = policies.OrderByDescending(policy => policy.EffectiveFrom)
            .ThenByDescending(policy => policy.CreatedAt)
            .ToList();
        return await MapPoliciesAsync(policies, cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollPolicyVersionDto>> GetPendingFinanceReviewAsync(CancellationToken cancellationToken)
    {
        var policies = await _db.PayrollPolicyVersions
            .AsNoTracking()
            .Where(policy => policy.Status == "pending_finance")
            .ToListAsync(cancellationToken);
        policies = policies.OrderBy(policy => policy.SubmittedAt).ToList();
        return await MapPoliciesAsync(policies, cancellationToken);
    }

    public async Task<PayrollPolicyVersionDto?> GetPolicyAsync(Guid id, CancellationToken cancellationToken)
    {
        var policy = await _db.PayrollPolicyVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (policy == null)
            return null;

        var policies = await MapPoliciesAsync([policy], cancellationToken);
        return policies[0];
    }

    public async Task<PayrollPolicyOperationResultDto> CreateDraftAsync(
        SavePayrollPolicyRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty)
            return new PayrollPolicyOperationResultDto(null, "invalid_actor");
        var validationError = ValidateRequest(request);
        if (validationError != null)
            return new PayrollPolicyOperationResultDto(null, validationError);

        var duplicateVersion = await _db.PayrollPolicyVersions
            .AnyAsync(policy => policy.VersionCode.ToUpper() == request.VersionCode.Trim().ToUpper(), cancellationToken);
        if (duplicateVersion)
            return new PayrollPolicyOperationResultDto(null, "version_code_already_exists");

        var now = DateTimeOffset.UtcNow;
        var policy = new PayrollPolicyVersion
        {
            VersionCode = request.VersionCode.Trim(),
            EffectiveFrom = request.EffectiveFrom,
            ApprovalDocumentReference = request.ApprovalDocumentReference.Trim(),
            PolicyJson = request.PolicyJson,
            CreatedByUserId = actorUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.PayrollPolicyVersions.Add(policy);
        AddAuditEvent(policy.Id, "Created", actorUserId, $"Version {policy.VersionCode} draft created");
        await _db.SaveChangesAsync(cancellationToken);

        return new PayrollPolicyOperationResultDto(await GetPolicyAsync(policy.Id, cancellationToken), null);
    }

    public async Task<PayrollPolicyOperationResultDto> UpdateDraftAsync(
        Guid id,
        SavePayrollPolicyRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty)
            return new PayrollPolicyOperationResultDto(null, "invalid_actor");
        var policy = await _db.PayrollPolicyVersions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (policy == null)
            return new PayrollPolicyOperationResultDto(null, "not_found");
        if (policy.Status != "draft")
            return new PayrollPolicyOperationResultDto(null, "policy_is_immutable");

        var validationError = ValidateRequest(request);
        if (validationError != null)
            return new PayrollPolicyOperationResultDto(null, validationError);

        var duplicateVersion = await _db.PayrollPolicyVersions
            .AnyAsync(
                item => item.Id != id && item.VersionCode.ToUpper() == request.VersionCode.Trim().ToUpper(),
                cancellationToken);
        if (duplicateVersion)
            return new PayrollPolicyOperationResultDto(null, "version_code_already_exists");

        policy.VersionCode = request.VersionCode.Trim();
        policy.EffectiveFrom = request.EffectiveFrom;
        policy.ApprovalDocumentReference = request.ApprovalDocumentReference.Trim();
        policy.PolicyJson = request.PolicyJson;
        policy.UpdatedAt = DateTimeOffset.UtcNow;
        AddAuditEvent(policy.Id, "Updated", actorUserId, $"Version {policy.VersionCode} draft updated");
        await _db.SaveChangesAsync(cancellationToken);

        return new PayrollPolicyOperationResultDto(await GetPolicyAsync(policy.Id, cancellationToken), null);
    }

    public async Task<PayrollPolicyOperationResultDto> SubmitAsync(
        Guid id,
        Guid actorUserId,
        bool confirmedOfficialSources,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty)
            return new PayrollPolicyOperationResultDto(null, "invalid_actor");
        var policy = await _db.PayrollPolicyVersions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (policy == null)
            return new PayrollPolicyOperationResultDto(null, "not_found");
        if (policy.Status != "draft")
            return new PayrollPolicyOperationResultDto(null, "invalid_state");

        var missingFields = PayrollPolicyPayloadValidator.GetMissingFields(policy.PolicyJson);
        if (missingFields.Count > 0)
            return new PayrollPolicyOperationResultDto(null, "policy_incomplete", missingFields);
        if (!confirmedOfficialSources)
            return new PayrollPolicyOperationResultDto(null, "official_sources_not_confirmed");

        var now = DateTimeOffset.UtcNow;
        policy.Status = "pending_finance";
        policy.SubmittedByUserId = actorUserId;
        policy.SubmittedAt = now;
        policy.UpdatedAt = now;
        AddAuditEvent(
            policy.Id,
            "SubmittedForFinanceReview",
            actorUserId,
            $"Version {policy.VersionCode} submitted; HR confirmed official source documents");
        await _db.SaveChangesAsync(cancellationToken);

        return new PayrollPolicyOperationResultDto(await GetPolicyAsync(policy.Id, cancellationToken), null);
    }

    public async Task<PayrollPolicyOperationResultDto> ApproveAsync(
        Guid id,
        Guid actorUserId,
        bool confirmedOfficialSources,
        string? comment,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty)
            return new PayrollPolicyOperationResultDto(null, "invalid_actor");
        var policy = await _db.PayrollPolicyVersions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (policy == null)
            return new PayrollPolicyOperationResultDto(null, "not_found");
        if (policy.Status != "pending_finance")
            return new PayrollPolicyOperationResultDto(null, "invalid_state");
        if (IsSameActorAsMakerOrHrReviewer(policy, actorUserId))
            return new PayrollPolicyOperationResultDto(null, "reviewer_must_be_independent");
        if (!confirmedOfficialSources)
            return new PayrollPolicyOperationResultDto(null, "official_sources_not_confirmed");
        if (string.IsNullOrWhiteSpace(comment))
            return new PayrollPolicyOperationResultDto(null, "review_comment_required");

        var now = DateTimeOffset.UtcNow;
        policy.Status = "approved";
        policy.FinanceReviewerUserId = actorUserId;
        policy.FinanceReviewedAt = now;
        policy.ReviewComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        policy.UpdatedAt = now;
        AddAuditEvent(
            policy.Id,
            "FinanceApproved",
            actorUserId,
            $"Finance confirmed official source documents. {policy.ReviewComment}");
        await _db.SaveChangesAsync(cancellationToken);

        return new PayrollPolicyOperationResultDto(await GetPolicyAsync(policy.Id, cancellationToken), null);
    }

    public async Task<PayrollPolicyOperationResultDto> RejectAsync(
        Guid id,
        Guid actorUserId,
        string comment,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty)
            return new PayrollPolicyOperationResultDto(null, "invalid_actor");
        var policy = await _db.PayrollPolicyVersions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (policy == null)
            return new PayrollPolicyOperationResultDto(null, "not_found");
        if (policy.Status != "pending_finance")
            return new PayrollPolicyOperationResultDto(null, "invalid_state");
        if (IsSameActorAsMakerOrHrReviewer(policy, actorUserId))
            return new PayrollPolicyOperationResultDto(null, "reviewer_must_be_independent");
        if (string.IsNullOrWhiteSpace(comment))
            return new PayrollPolicyOperationResultDto(null, "review_comment_required");

        var now = DateTimeOffset.UtcNow;
        policy.Status = "rejected";
        policy.FinanceReviewerUserId = actorUserId;
        policy.FinanceReviewedAt = now;
        policy.ReviewComment = comment.Trim();
        policy.UpdatedAt = now;
        AddAuditEvent(policy.Id, "FinanceRejected", actorUserId, policy.ReviewComment);
        await _db.SaveChangesAsync(cancellationToken);

        return new PayrollPolicyOperationResultDto(await GetPolicyAsync(policy.Id, cancellationToken), null);
    }

    private static bool IsSameActorAsMakerOrHrReviewer(PayrollPolicyVersion policy, Guid actorUserId)
        => policy.CreatedByUserId == actorUserId || policy.SubmittedByUserId == actorUserId;

    private static string? ValidateRequest(SavePayrollPolicyRequestDto request)
    {
        if (request.EffectiveFrom == default)
            return "effective_date_required";
        if (string.IsNullOrWhiteSpace(request.ApprovalDocumentReference))
            return "approval_document_reference_required";
        if (!PayrollPolicyPayloadValidator.IsValidJson(request.PolicyJson))
            return "policy_json_must_be_a_json_object";
        return null;
    }

    private void AddAuditEvent(Guid policyId, string action, Guid actorUserId, string? details)
    {
        _db.PayrollPolicyAuditEvents.Add(new PayrollPolicyAuditEvent
        {
            PayrollPolicyVersionId = policyId,
            Action = action,
            ActorUserId = actorUserId,
            OccurredAt = DateTimeOffset.UtcNow,
            Details = details
        });
    }

    private async Task<IReadOnlyList<PayrollPolicyVersionDto>> MapPoliciesAsync(
        IReadOnlyList<PayrollPolicyVersion> policies,
        CancellationToken cancellationToken)
    {
        if (policies.Count == 0)
            return [];

        var policyIds = policies.Select(policy => policy.Id).ToArray();
        var events = await _db.PayrollPolicyAuditEvents
            .AsNoTracking()
            .Where(item => policyIds.Contains(item.PayrollPolicyVersionId))
            .ToListAsync(cancellationToken);
        events = events.OrderBy(item => item.OccurredAt).ToList();
        var actorIds = policies
            .SelectMany(policy => new[] { policy.CreatedByUserId, policy.SubmittedByUserId, policy.FinanceReviewerUserId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Concat(events.Select(item => item.ActorUserId))
            .Distinct()
            .ToArray();
        var usernames = await _db.Users
            .AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.Username, cancellationToken);
        var eventsByPolicy = events.GroupBy(item => item.PayrollPolicyVersionId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        return policies.Select(policy => new PayrollPolicyVersionDto(
            policy.Id,
            policy.VersionCode,
            policy.EffectiveFrom,
            policy.ApprovalDocumentReference,
            policy.PolicyJson,
            policy.Status,
            policy.CreatedByUserId,
            ResolveUsername(usernames, policy.CreatedByUserId),
            policy.CreatedAt,
            policy.UpdatedAt,
            policy.SubmittedByUserId,
            ResolveNullableUsername(usernames, policy.SubmittedByUserId),
            policy.SubmittedAt,
            policy.FinanceReviewerUserId,
            ResolveNullableUsername(usernames, policy.FinanceReviewerUserId),
            policy.FinanceReviewedAt,
            policy.ReviewComment,
            CalculationEnabled: false,
            (eventsByPolicy.TryGetValue(policy.Id, out var policyEvents) ? policyEvents : [])
                .Select(item => new PayrollPolicyAuditEventDto(
                    item.Action,
                    item.ActorUserId,
                    ResolveUsername(usernames, item.ActorUserId),
                    item.OccurredAt,
                    item.Details))
                .ToArray()))
            .ToArray();
    }

    private static string ResolveUsername(IReadOnlyDictionary<Guid, string> usernames, Guid userId)
        => usernames.TryGetValue(userId, out var username) ? username : userId.ToString();

    private static string? ResolveNullableUsername(IReadOnlyDictionary<Guid, string> usernames, Guid? userId)
        => userId.HasValue ? ResolveUsername(usernames, userId.Value) : null;
}
