using System.Data;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.QA;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public sealed class SupplierPortalCapaService(ErpGeneralDbContext db)
    : ISupplierPortalCapaService
{
    private static readonly HashSet<string> ClosedComplaintStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "closed", "cancelled", "canceled" };

    private static readonly HashSet<string> EditableCapaStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "open", "in_progress", "revision_requested" };

    public async Task<IReadOnlyList<SupplierPortalComplaintSummaryDto>> GetComplaintsAsync(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        return await db.SupplierComplaints
            .AsNoTracking()
            .Where(complaint => complaint.SupplierId == supplierId)
            .OrderByDescending(complaint => complaint.CreatedAt)
            .Take(100)
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
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var complaint = await FindComplaintAsync(complaintId, supplierId, cancellationToken);
        return complaint is null ? null : ToDetail(complaint);
    }

    public Task<SupplierPortalCapaResponseSavedDto> SaveDraftAsync(
        Guid complaintId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        SupplierPortalCapaResponseRequestDto request,
        CancellationToken cancellationToken) =>
        SaveResponseAsync(
            complaintId,
            supplierId,
            supplierUserId,
            supplierRole,
            request,
            submit: false,
            cancellationToken);

    public Task<SupplierPortalCapaResponseSavedDto> SubmitResponseAsync(
        Guid complaintId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        SupplierPortalCapaResponseRequestDto request,
        CancellationToken cancellationToken) =>
        SaveResponseAsync(
            complaintId,
            supplierId,
            supplierUserId,
            supplierRole,
            request,
            submit: true,
            cancellationToken);

    private async Task<SupplierPortalCapaResponseSavedDto> SaveResponseAsync(
        Guid complaintId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        SupplierPortalCapaResponseRequestDto request,
        bool submit,
        CancellationToken cancellationToken)
    {
        EnsureCanRespond(supplierRole);
        ValidateRequest(request);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var complaint = await FindComplaintAsync(complaintId, supplierId, cancellationToken)
            ?? throw new KeyNotFoundException("客訴不存在。");

        if (ClosedComplaintStatuses.Contains(complaint.Status))
            throw new InvalidOperationException("此客訴已結案或取消，無法再回覆。");

        var capa = complaint.Capa;
        if (capa is null)
        {
            capa = new SupplierCapa
            {
                CapaNumber = CreateCapaNumber(),
                ComplaintId = complaint.Id,
                Complaint = complaint,
                Status = "open"
            };
            complaint.Capa = capa;
            db.SupplierCapas.Add(capa);
        }

        if (!EditableCapaStatuses.Contains(capa.Status))
            throw new InvalidOperationException("此 CAPA 已提交或正在審查，需由內部品質人員退回後才能修改。");

        ApplyRequest(capa, request);
        if (submit)
        {
            if (HasMissingDiscipline(capa))
                throw new ArgumentException("提交前必須完整填寫 D1 至 D8 矯正預防措施。");
            var unavailableAttachments = await db.SupplierCapaAttachments.AnyAsync(
                attachment => attachment.SupplierCapaId == capa.Id
                    && attachment.RevokedAt == null
                    && attachment.ScanStatus != "clean",
                cancellationToken);
            if (unavailableAttachments)
                throw new InvalidOperationException("所有有效附件必須完成 ClamAV 掃描且結果為安全後，才能提交 CAPA。");
            capa.Status = "submitted";
            capa.SupplierRespondedAt = DateTime.UtcNow;
        }
        else
        {
            capa.Status = "in_progress";
        }

        var revisions = await db.SupplierCapaResponseRevisions
            .Where(revision => revision.SupplierCapaId == capa.Id)
            .OrderByDescending(revision => revision.RevisionNumber)
            .Take(1)
            .Select(revision => revision.RevisionNumber)
            .ToListAsync(cancellationToken);
        var revisionNumber = revisions.Count == 0 ? 1 : revisions[0] + 1;
        var action = submit ? "submitted" : "draft_saved";
        var now = DateTime.UtcNow;
        var revision = CreateRevision(capa, supplierUserId, revisionNumber, action, now);
        db.SupplierCapaResponseRevisions.Add(revision);
        if (submit)
        {
            var cleanUnassignedAttachments = await db.SupplierCapaAttachments
                .Where(attachment => attachment.SupplierCapaId == capa.Id
                    && attachment.RevokedAt == null
                    && attachment.ScanStatus == "clean"
                    && attachment.SupplierCapaResponseRevisionId == null)
                .ToListAsync(cancellationToken);
            foreach (var attachment in cleanUnassignedAttachments)
                attachment.ResponseRevision = revision;
        }
        db.Entry(capa).Property<Guid?>("ModifiedBy").CurrentValue = supplierUserId;
        db.Entry(capa).Property<DateTime?>("ModifiedAt").CurrentValue = now;
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return new SupplierPortalCapaResponseSavedDto(
            complaint.Id,
            capa.Id,
            capa.CapaNumber,
            capa.Status,
            revisionNumber,
            action,
            ToUtcOffset(now));
    }

    private Task<SupplierComplaint?> FindComplaintAsync(
        Guid complaintId,
        Guid supplierId,
        CancellationToken cancellationToken) =>
        db.SupplierComplaints
            .Include(complaint => complaint.Capa)
                .ThenInclude(capa => capa!.ResponseRevisions)
            .SingleOrDefaultAsync(
                complaint => complaint.Id == complaintId && complaint.SupplierId == supplierId,
                cancellationToken);

    private static SupplierPortalComplaintDetailDto ToDetail(SupplierComplaint complaint)
    {
        var capa = complaint.Capa;
        var revisions = capa?.ResponseRevisions
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

    private static SupplierCapaResponseRevision CreateRevision(
        SupplierCapa capa,
        Guid supplierUserId,
        int revisionNumber,
        string action,
        DateTime createdAt) =>
        new()
        {
            SupplierCapaId = capa.Id,
            SupplierUserId = supplierUserId,
            RevisionNumber = revisionNumber,
            Action = action,
            D1_Team = capa.D1_Team,
            D2_ProblemDescription = capa.D2_ProblemDescription,
            D3_InterimContainment = capa.D3_InterimContainment,
            D4_RootCause = capa.D4_RootCause,
            D5_PermanentAction = capa.D5_PermanentAction,
            D6_Verification = capa.D6_Verification,
            D7_Prevention = capa.D7_Prevention,
            D8_Closure = capa.D8_Closure,
            CreatedAt = createdAt
        };

    private static void ApplyRequest(
        SupplierCapa capa,
        SupplierPortalCapaResponseRequestDto request)
    {
        capa.D1_Team = ApplyIfPresent(capa.D1_Team, request.D1Team);
        capa.D2_ProblemDescription = ApplyIfPresent(capa.D2_ProblemDescription, request.D2ProblemDescription);
        capa.D3_InterimContainment = ApplyIfPresent(capa.D3_InterimContainment, request.D3InterimContainment);
        capa.D4_RootCause = ApplyIfPresent(capa.D4_RootCause, request.D4RootCause);
        capa.D5_PermanentAction = ApplyIfPresent(capa.D5_PermanentAction, request.D5PermanentAction);
        capa.D6_Verification = ApplyIfPresent(capa.D6_Verification, request.D6Verification);
        capa.D7_Prevention = ApplyIfPresent(capa.D7_Prevention, request.D7Prevention);
        capa.D8_Closure = ApplyIfPresent(capa.D8_Closure, request.D8Closure);
    }

    private static string? ApplyIfPresent(string? existingValue, string? incomingValue) =>
        incomingValue is null ? existingValue : NormalizeOptional(incomingValue);

    private static bool HasMissingDiscipline(SupplierCapa capa) =>
        new[]
        {
            capa.D1_Team,
            capa.D2_ProblemDescription,
            capa.D3_InterimContainment,
            capa.D4_RootCause,
            capa.D5_PermanentAction,
            capa.D6_Verification,
            capa.D7_Prevention,
            capa.D8_Closure
        }.Any(string.IsNullOrWhiteSpace);

    private static void ValidateRequest(SupplierPortalCapaResponseRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (new[]
            {
                request.D1Team,
                request.D2ProblemDescription,
                request.D3InterimContainment,
                request.D4RootCause,
                request.D5PermanentAction,
                request.D6Verification,
                request.D7Prevention,
                request.D8Closure
            }.All(string.IsNullOrWhiteSpace))
            throw new ArgumentException("至少需提供一項 D1–D8 回覆內容。");

        if (request.D1Team?.Length > 500
            || request.D2ProblemDescription?.Length > 8000
            || request.D3InterimContainment?.Length > 8000
            || request.D4RootCause?.Length > 8000
            || request.D5PermanentAction?.Length > 8000
            || request.D6Verification?.Length > 8000
            || request.D7Prevention?.Length > 8000
            || request.D8Closure?.Length > 8000)
            throw new ArgumentException("CAPA 回覆內容超過允許長度。");
    }

    private static void EnsureCanRespond(string supplierRole)
    {
        if (!string.Equals(supplierRole, "sales", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("目前供應商入口角色沒有回覆客訴或 CAPA 的權限。");
    }

    private static string CreateCapaNumber() =>
        $"CAPA-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..30];

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
