using System.Data;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.QA;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public sealed class SupplierCapaReviewService(ErpGeneralDbContext db)
    : ISupplierCapaReviewService
{
    public async Task<IReadOnlyList<SupplierPortalComplaintSummaryDto>> GetComplaintsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.SupplierComplaints.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(complaint =>
                complaint.Capa != null && complaint.Capa.Status == status);

        return await query
            .OrderByDescending(complaint => complaint.Capa == null
                ? complaint.CreatedAt
                : complaint.Capa.SupplierRespondedAt ?? complaint.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(complaint => new SupplierPortalComplaintSummaryDto(
                complaint.Id,
                complaint.ComplaintNumber,
                complaint.PurchaseOrderId,
                complaint.SkuId,
                complaint.LotNumber,
                complaint.DefectQty,
                complaint.Ppm,
                complaint.DefectType,
                complaint.Priority,
                complaint.Description,
                complaint.Status,
                ToUtcOffset(complaint.CreatedAt),
                complaint.Capa == null ? null : complaint.Capa.CapaNumber,
                complaint.Capa == null ? null : complaint.Capa.Status,
                complaint.Capa == null || !complaint.Capa.SupplierResponseDue.HasValue
                    ? null
                    : ToUtcOffset(complaint.Capa.SupplierResponseDue.Value),
                complaint.Capa == null || !complaint.Capa.SupplierRespondedAt.HasValue
                    ? null
                    : ToUtcOffset(complaint.Capa.SupplierRespondedAt.Value)))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierPortalComplaintDetailDto?> GetComplaintAsync(
        Guid complaintId,
        CancellationToken cancellationToken)
    {
        var complaint = await FindComplaintAsync(complaintId, cancellationToken);
        if (complaint is null)
            return null;

        var revisions = complaint.Capa?.ResponseRevisions
            .OrderByDescending(revision => revision.RevisionNumber)
            .Take(100)
            .Select(revision => new SupplierPortalCapaRevisionDto(
                revision.RevisionNumber,
                revision.Action,
                revision.D1_Team,
                revision.D2_ProblemDescription,
                revision.D3_InterimContainment,
                revision.D4_RootCause,
                revision.D5_PermanentAction,
                revision.D6_Verification,
                revision.D7_Prevention,
                revision.D8_Closure,
                ToUtcOffset(revision.CreatedAt)))
            .ToArray() ?? [];
        var capa = complaint.Capa;
        var isOverdue = capa?.SupplierResponseDue is DateTime dueDate
            && dueDate.ToUniversalTime() < DateTime.UtcNow
            && !capa.SupplierRespondedAt.HasValue
            && !string.Equals(capa.Status, "closed", StringComparison.OrdinalIgnoreCase);

        return new SupplierPortalComplaintDetailDto(
            complaint.Id,
            complaint.ComplaintNumber,
            complaint.PurchaseOrderId,
            complaint.SkuId,
            complaint.LotNumber,
            complaint.DefectQty,
            complaint.Ppm,
            complaint.DefectType,
            complaint.Priority,
            complaint.Description,
            complaint.Status,
            ToUtcOffset(complaint.CreatedAt),
            capa?.Id,
            capa?.CapaNumber,
            capa?.Status,
            capa?.SupplierResponseDue is DateTime responseDue ? ToUtcOffset(responseDue) : null,
            capa?.SupplierRespondedAt is DateTime respondedAt ? ToUtcOffset(respondedAt) : null,
            isOverdue,
            capa?.D1_Team,
            capa?.D2_ProblemDescription,
            capa?.D3_InterimContainment,
            capa?.D4_RootCause,
            capa?.D5_PermanentAction,
            capa?.D6_Verification,
            capa?.D7_Prevention,
            capa?.D8_Closure,
            revisions);
    }

    public async Task<IReadOnlyList<SupplierCapaReviewDto>> GetReviewsAsync(
        Guid complaintId,
        CancellationToken cancellationToken)
    {
        var capaId = await db.SupplierComplaints
            .Where(complaint => complaint.Id == complaintId)
            .Select(complaint => complaint.Capa == null ? (Guid?)null : complaint.Capa.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (capaId is null)
            throw new KeyNotFoundException("客訴或 CAPA 不存在。");

        return await db.SupplierCapaReviews
            .AsNoTracking()
            .Where(review => review.SupplierCapaId == capaId.Value)
            .OrderBy(review => review.CreatedAt)
            .Select(review => new SupplierCapaReviewDto(
                review.Id,
                review.SupplierCapaId,
                review.SupplierCapaResponseRevisionId,
                review.ResponseRevision.RevisionNumber,
                review.ReviewerUserId,
                review.Decision,
                review.Comments,
                ToUtcOffset(review.CreatedAt)))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierCapaReviewDto> StartReviewAsync(
        Guid complaintId,
        Guid reviewerUserId,
        CancellationToken cancellationToken)
    {
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var complaint = await FindComplaintAsync(complaintId, cancellationToken)
            ?? throw new KeyNotFoundException("客訴不存在。");
        var capa = complaint.Capa ?? throw new KeyNotFoundException("此客訴尚未建立 CAPA。");
        if (!string.Equals(capa.Status, "submitted", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("只有已提交的 CAPA 可以開始審查。");

        var revision = GetLatestSubmittedRevision(capa)
            ?? throw new InvalidOperationException("找不到已提交的 CAPA 版本。");
        capa.Status = "under_review";
        var review = AddReview(capa, revision, reviewerUserId, "review_started", null);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return ToDto(review);
    }

    public async Task<SupplierCapaReviewDto> ReviewAsync(
        Guid complaintId,
        Guid reviewerUserId,
        SupplierCapaReviewRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var decision = request.Decision?.Trim().ToLowerInvariant();
        if (decision is not ("approve" or "revision_requested"))
            throw new ArgumentException("審查決策只允許 approve 或 revision_requested。");
        if (decision == "revision_requested" && string.IsNullOrWhiteSpace(request.Comments))
            throw new ArgumentException("退回補正時必須填寫原因。");

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var complaint = await FindComplaintAsync(complaintId, cancellationToken)
            ?? throw new KeyNotFoundException("客訴不存在。");
        var capa = complaint.Capa ?? throw new KeyNotFoundException("此客訴尚未建立 CAPA。");
        if (!string.Equals(capa.Status, "under_review", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("CAPA 目前不在審查中。");

        var revision = GetLatestSubmittedRevision(capa)
            ?? throw new InvalidOperationException("找不到已提交的 CAPA 版本。");
        if (revision.RevisionNumber != request.RevisionNumber)
            throw new InvalidOperationException("CAPA 版本已更新，請重新載入後再審查。");

        capa.Status = decision == "approve" ? "approved" : "revision_requested";
        var review = AddReview(capa, revision, reviewerUserId, decision, NormalizeOptional(request.Comments));
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return ToDto(review);
    }

    public async Task<SupplierCapaReviewDto> CloseAsync(
        Guid complaintId,
        Guid reviewerUserId,
        SupplierCapaCloseRequestDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Comments))
            throw new ArgumentException("結案時必須填寫結案說明。");

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var complaint = await FindComplaintAsync(complaintId, cancellationToken)
            ?? throw new KeyNotFoundException("客訴不存在。");
        var capa = complaint.Capa ?? throw new KeyNotFoundException("此客訴尚未建立 CAPA。");
        if (!string.Equals(capa.Status, "approved", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("只有已核准的 CAPA 可以結案。");

        var revision = GetLatestSubmittedRevision(capa)
            ?? throw new InvalidOperationException("找不到已核准的 CAPA 版本。");
        capa.Status = "closed";
        complaint.Status = "closed";
        var review = AddReview(capa, revision, reviewerUserId, "closed", NormalizeOptional(request.Comments));
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return ToDto(review);
    }

    private Task<SupplierComplaint?> FindComplaintAsync(
        Guid complaintId,
        CancellationToken cancellationToken) =>
        db.SupplierComplaints
            .Include(complaint => complaint.Capa)
                .ThenInclude(capa => capa!.ResponseRevisions)
            .SingleOrDefaultAsync(complaint => complaint.Id == complaintId, cancellationToken);

    private static SupplierCapaResponseRevision? GetLatestSubmittedRevision(SupplierCapa capa) =>
        capa.ResponseRevisions
            .Where(revision => string.Equals(revision.Action, "submitted", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(revision => revision.RevisionNumber)
            .FirstOrDefault();

    private SupplierCapaReview AddReview(
        SupplierCapa capa,
        SupplierCapaResponseRevision revision,
        Guid reviewerUserId,
        string decision,
        string? comments)
    {
        var review = new SupplierCapaReview
        {
            SupplierCapaId = capa.Id,
            SupplierCapaResponseRevisionId = revision.Id,
            ReviewerUserId = reviewerUserId,
            Decision = decision,
            Comments = comments,
            CreatedAt = DateTime.UtcNow
        };
        db.SupplierCapaReviews.Add(review);
        db.Entry(capa).Property<Guid?>("ModifiedBy").CurrentValue = reviewerUserId;
        db.Entry(capa).Property<DateTime?>("ModifiedAt").CurrentValue = review.CreatedAt;
        return review;
    }

    private static SupplierCapaReviewDto ToDto(SupplierCapaReview review) =>
        new(
            review.Id,
            review.SupplierCapaId,
            review.SupplierCapaResponseRevisionId,
            review.ResponseRevision?.RevisionNumber
                ?? throw new InvalidOperationException("CAPA review revision is not loaded."),
            review.ReviewerUserId,
            review.Decision,
            review.Comments,
            ToUtcOffset(review.CreatedAt));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
