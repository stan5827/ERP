using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using Microsoft.EntityFrameworkCore;
using static ErpBackend.Services.SupplierApSubmissionSupport.SupplierApSubmissionWorkflowRules;

namespace ErpBackend.Services.SupplierApSubmissionSupport;

internal static class SupplierApSubmissionQuerySupport
{
    internal static async Task<SupplierApSubmission?> LoadTrackedSubmissionAsync(
        ErpGeneralDbContext db,
        Guid submissionId,
        Guid? supplierId,
        CancellationToken cancellationToken)
    {
        var query = db.SupplierApSubmissions
            .Include(item => item.Revisions)
                .ThenInclude(revision => revision.Lines)
                    .ThenInclude(line => line.GoodsReceiptItem)
            .Include(item => item.GeneratedAccountsPayable);
        return await query.SingleOrDefaultAsync(
            item => item.Id == submissionId
                && (!supplierId.HasValue || item.SupplierId == supplierId.Value),
            cancellationToken);
    }

    internal static IQueryable<SupplierApSubmission> FilterSubmissions(
        IQueryable<SupplierApSubmission> query,
        string? status,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? submissionNumber)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLowerInvariant();
            query = query.Where(item => item.Status == normalizedStatus);
        }
        if (invoiceDateFrom.HasValue)
            query = query.Where(item => item.Revisions.Any(revision =>
                revision.RevisionNumber == item.CurrentRevisionNumber
                && revision.SupplierInvoiceDate >= invoiceDateFrom.Value));
        if (invoiceDateTo.HasValue)
            query = query.Where(item => item.Revisions.Any(revision =>
                revision.RevisionNumber == item.CurrentRevisionNumber
                && revision.SupplierInvoiceDate <= invoiceDateTo.Value));
        if (!string.IsNullOrWhiteSpace(submissionNumber))
        {
            var number = submissionNumber.Trim();
            query = query.Where(item => item.SubmissionNumber.Contains(number));
        }
        return query;
    }

    internal static async Task<SupplierApSubmission?> LoadDetailAsync(
        ErpGeneralDbContext db,
        Guid submissionId,
        CancellationToken cancellationToken) =>
        await db.SupplierApSubmissions
            .AsNoTracking()
            .Where(item => item.Id == submissionId)
            .Include(item => item.Revisions)
                .ThenInclude(revision => revision.PurchaseOrder)
            .Include(item => item.Revisions)
                .ThenInclude(revision => revision.GoodsReceipt)
            .Include(item => item.Revisions)
                .ThenInclude(revision => revision.Lines)
                    .ThenInclude(line => line.GoodsReceiptItem)
                        .ThenInclude(receiptItem => receiptItem.Sku)
            .Include(item => item.Revisions)
                .ThenInclude(revision => revision.Lines)
                    .ThenInclude(line => line.PurchaseOrderItem)
                        .ThenInclude(orderItem => orderItem!.Sku)
            .Include(item => item.Attachments)
            .Include(item => item.Reviews)
                .ThenInclude(review => review.Reviewer)
            .Include(item => item.GeneratedAccountsPayable)
            .SingleOrDefaultAsync(cancellationToken);

    internal static async Task<SupplierApSubmissionPageDto> ToPageAsync(
        IQueryable<SupplierApSubmission> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var submissions = await query
            .OrderByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(item => item.Revisions)
            .Include(item => item.Reviews)
            .ToListAsync(cancellationToken);
        var summaries = submissions.Select(item =>
        {
            var revision = item.Revisions.Single(rev => rev.RevisionNumber == item.CurrentRevisionNumber);
            var review = item.Reviews.OrderByDescending(entry => entry.CreatedAt).FirstOrDefault();
            return ToSummary(item, revision, review?.MatchResult ?? "pending");
        }).ToArray();
        return new SupplierApSubmissionPageDto(summaries, page, pageSize, totalCount);
    }

    internal static SupplierApSubmissionDetailDto ToDetail(
        SupplierApSubmission submission,
        bool includeReviewer)
    {
        var revision = submission.Revisions.Single(item =>
            item.RevisionNumber == submission.CurrentRevisionNumber);
        var sources = new ValidatedSources(
            revision.PurchaseOrder,
            revision.GoodsReceipt,
            revision.Lines.Select(line => new ValidatedLine(
                line.GoodsReceiptItem,
                line.PurchaseOrderItem!)).ToList());
        var discrepancies = EvaluateMatch(revision, sources);
        var matchResult = submission.Status == "approved"
            ? "matched"
            : submission.Status is "submitted" or "returned"
                ? (discrepancies.Count == 0 ? "matched" : "variance")
                : "pending";
        return new SupplierApSubmissionDetailDto(
            submission.Id,
            submission.SubmissionNumber,
            submission.Status,
            submission.CurrentRevisionNumber,
            revision.SupplierInvoiceNumber,
            revision.SupplierInvoiceDate,
            revision.Currency,
            revision.InvoiceAmount,
            revision.TaxAmount,
            revision.TotalAmount,
            revision.PurchaseOrderId,
            revision.PurchaseOrder?.PoNumber ?? string.Empty,
            revision.GoodsReceiptId,
            revision.GoodsReceipt?.GrNumber ?? string.Empty,
            matchResult,
            discrepancies,
            submission.Version,
            new DateTimeOffset(DateTime.SpecifyKind(submission.CreatedAt, DateTimeKind.Utc)),
            revision.SubmittedAt.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(revision.SubmittedAt.Value, DateTimeKind.Utc))
                : null,
            submission.Status == "approved" && submission.Reviews.Count > 0
                ? new DateTimeOffset(DateTime.SpecifyKind(
                    submission.Reviews.Max(item => item.CreatedAt),
                    DateTimeKind.Utc))
                : null,
            submission.GeneratedAccountsPayableId,
            submission.GeneratedAccountsPayable?.ApNumber,
            revision.Lines.Select(line => new SupplierApSubmissionLineDto(
                line.GoodsReceiptItemId,
                line.PurchaseOrderItemId,
                line.GoodsReceiptItem?.Sku?.SkuNumber,
                line.GoodsReceiptItem?.Sku?.SkuName,
                line.GoodsReceiptItem?.AcceptedQty ?? 0m,
                line.InvoicedQuantity,
                line.UnitPrice,
                line.NetAmount,
                line.TaxAmount,
                line.TotalAmount)).ToArray(),
            submission.Attachments
                .Where(item => item.RevokedAt is null)
                .OrderBy(item => item.UploadedAt)
                .Select(item => ToAttachmentDto(item, revision.RevisionNumber))
                .ToArray(),
            submission.Reviews
                .OrderBy(item => item.CreatedAt)
                .Select(item => new SupplierApSubmissionReviewDto(
                    item.Id,
                    item.Action,
                    item.Reason,
                    item.MatchResult,
                    new DateTimeOffset(DateTime.SpecifyKind(item.CreatedAt, DateTimeKind.Utc)),
                    includeReviewer ? item.ReviewerUserId : null))
                .ToArray());
    }

    private static SupplierApSubmissionSummaryDto ToSummary(
        SupplierApSubmission submission,
        SupplierApSubmissionRevision revision,
        string matchResult) =>
        new(
            submission.Id,
            submission.SubmissionNumber,
            submission.Status,
            submission.CurrentRevisionNumber,
            revision.SupplierInvoiceNumber,
            revision.SupplierInvoiceDate,
            revision.Currency,
            revision.TotalAmount,
            matchResult,
            submission.Version,
            new DateTimeOffset(DateTime.SpecifyKind(submission.CreatedAt, DateTimeKind.Utc)),
            revision.SubmittedAt.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(revision.SubmittedAt.Value, DateTimeKind.Utc))
                : null);

    private static SupplierApSubmissionAttachmentDto ToAttachmentDto(
        SupplierApSubmissionAttachment attachment,
        int revisionNumber) =>
        new(
            attachment.Id,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.ScanStatus,
            new DateTimeOffset(DateTime.SpecifyKind(attachment.UploadedAt, DateTimeKind.Utc)),
            attachment.ScannedAt.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(attachment.ScannedAt.Value, DateTimeKind.Utc))
                : null,
            revisionNumber);
}
