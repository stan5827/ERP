using System.Data;
using System.Text.Json;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.System;
using ErpBackend.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using static ErpBackend.Services.SupplierApSubmissionSupport.SupplierApSubmissionQuerySupport;
using static ErpBackend.Services.SupplierApSubmissionSupport.SupplierApSubmissionWorkflowRules;

namespace ErpBackend.Services;

public sealed class SupplierApSubmissionService(ErpGeneralDbContext db)
    : ISupplierApSubmissionService
{
    public async Task<SupplierApSubmissionPageDto> GetSupplierSubmissionsAsync(
            Guid supplierId,
            string? status,
            DateOnly? invoiceDateFrom,
            DateOnly? invoiceDateTo,
            string? submissionNumber,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
    {
        var query = FilterSubmissions(
            db.SupplierApSubmissions.AsNoTracking().Where(item => item.SupplierId == supplierId),
            status,
            invoiceDateFrom,
            invoiceDateTo,
            submissionNumber);
        return await ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierApEligibleSourceDto>> GetEligibleSourcesAsync(
        Guid supplierId,
        Guid? purchaseOrderId,
        CancellationToken cancellationToken)
    {
        var orders = await db.PurchaseOrders
            .AsNoTracking()
            .Where(order => order.SupplierId == supplierId
                && EligiblePurchaseOrderStatuses.Contains(order.Status)
                && !order.IsLocked
                && (!purchaseOrderId.HasValue || order.Id == purchaseOrderId.Value))
            .Include(order => order.Items)
                .ThenInclude(item => item.Sku)
            .ToListAsync(cancellationToken);
        if (orders.Count == 0)
            return [];

        var orderIds = orders.Select(order => order.Id).ToArray();
        var receipts = await db.GoodsReceipts
            .AsNoTracking()
            .Where(receipt => receipt.SupplierId == supplierId
                && receipt.PurchaseOrderId.HasValue
                && orderIds.Contains(receipt.PurchaseOrderId.Value)
                && EligibleReceiptStatuses.Contains(receipt.Status)
                && EligibleIqcStatuses.Contains(receipt.IqcStatus))
            .Include(receipt => receipt.Items)
                .ThenInclude(item => item.Sku)
            .ToListAsync(cancellationToken);

        var result = new List<SupplierApEligibleSourceDto>();
        foreach (var receipt in receipts)
        {
            if (receipt.Items.GroupBy(item => item.SkuId).Any(group => group.Count() > 1))
                continue;
            var order = orders.Single(item => item.Id == receipt.PurchaseOrderId);
            var reserved = await GetInvoicedQuantitiesAsync(db, receipt.Id, cancellationToken);
            var lines = receipt.Items
                .Where(item => item.AcceptedQty > 0)
                .Select(item =>
                {
                    var matchingOrderItems = order.Items.Where(poItem => poItem.SkuId == item.SkuId).ToArray();
                    var poItem = matchingOrderItems.Length == 1 ? matchingOrderItems[0] : null;
                    reserved.TryGetValue(item.Id, out var previouslyInvoiced);
                    return new SupplierApEligibleSourceLineDto(
                        item.Id,
                        poItem?.Id,
                        item.SkuId,
                        item.Sku?.SkuNumber,
                        item.Sku?.SkuName,
                        item.AcceptedQty,
                        previouslyInvoiced,
                        Math.Max(0m, item.AcceptedQty - previouslyInvoiced),
                        poItem?.UnitPrice ?? item.ForeignCurrencyPrice,
                        item.PoUnit ?? string.Empty);
                })
                .Where(item => item.AvailableQuantity > 0)
                .ToArray();
            if (lines.Length > 0)
            {
                result.Add(new SupplierApEligibleSourceDto(
                    order.Id,
                    order.PoNumber,
                    receipt.Id,
                    receipt.GrNumber,
                    order.Currency,
                    lines));
            }
        }

        return result;
    }

    public async Task<SupplierApSubmissionDetailDto?> GetSupplierSubmissionAsync(
        Guid submissionId,
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var submission = await LoadDetailAsync(db, submissionId, cancellationToken);
        return submission is null || submission.SupplierId != supplierId
            ? null
            : ToDetail(submission, includeReviewer: false);
    }

    public async Task<SupplierApSubmissionResultDto> CreateDraftAsync(
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        string idempotencyKey,
        SupplierApSubmissionCreateRequestDto request,
        CancellationToken cancellationToken)
    {
        EnsureCanSubmit(supplierRole);
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            throw new ArgumentException("Idempotency-Key 必須為 1 至 100 個字元。");
        ValidateRequest(request);
        var normalizedKey = idempotencyKey.Trim();
        var requestHash = ComputeHash(request);
        var existing = await db.SupplierApSubmissions
            .Include(item => item.Revisions)
            .SingleOrDefaultAsync(
                item => item.SupplierId == supplierId && item.IdempotencyKey == normalizedKey,
                cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.IdempotencyRequestHash, requestHash, StringComparison.Ordinal))
                throw new InvalidOperationException("相同 Idempotency-Key 不可重用於不同請款內容。");
            return ToResult(existing, CurrentRevision(existing));
        }

        var draft = new SupplierApSubmissionDraftRequestDto(
            request.SupplierInvoiceNumber,
            request.SupplierInvoiceDate,
            request.Currency,
            request.InvoiceAmount,
            request.TaxAmount,
            request.TotalAmount,
            request.PurchaseOrderId,
            request.GoodsReceiptId,
            request.Lines);
        var sources = await LoadAndValidateSourcesAsync(db, supplierId, draft, cancellationToken);
        var invoiceKey = NormalizeInvoiceNumber(request.SupplierInvoiceNumber);
        if (await IsInvoiceAlreadyUsedAsync(supplierId, invoiceKey, cancellationToken))
            throw new InvalidOperationException("此供應商發票已存在請款申請或正式應付帳款。");

        var now = DateTime.UtcNow;
        var submission = new SupplierApSubmission
        {
            SubmissionNumber = $"APS-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..30],
            SupplierId = supplierId,
            SubmittedBySupplierUserId = supplierUserId,
            Status = "draft",
            CurrentRevisionNumber = 1,
            IdempotencyKey = normalizedKey,
            IdempotencyRequestHash = requestHash,
            SupplierInvoiceNumberKey = invoiceKey,
            CreatedAt = now,
            UpdatedAt = now
        };
        var revision = CreateRevision(submission, supplierUserId, 1, draft, sources, now);
        submission.Revisions.Add(revision);
        db.SupplierApSubmissions.Add(submission);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsSubmissionUniqueConflict(exception))
        {
            throw new InvalidOperationException("請款編號、冪等鍵或同供應商發票號碼已存在。", exception);
        }
        return ToResult(submission, revision);
    }

    public async Task<SupplierApSubmissionResultDto> UpdateDraftAsync(
        Guid submissionId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        int expectedVersion,
        SupplierApSubmissionDraftRequestDto request,
        CancellationToken cancellationToken)
    {
        EnsureCanSubmit(supplierRole);
        ValidateRequest(request);
        var submission = await LoadTrackedSubmissionAsync(db, submissionId, supplierId, cancellationToken)
            ?? throw new KeyNotFoundException("請款申請不存在。");
        EnsureVersion(submission, expectedVersion);
        if (submission.Status is not ("draft" or "returned"))
            throw new InvalidOperationException("目前狀態不可修改請款草稿。");

        var sources = await LoadAndValidateSourcesAsync(db, supplierId, request, cancellationToken);
        var invoiceKey = NormalizeInvoiceNumber(request.SupplierInvoiceNumber);
        if (await IsInvoiceAlreadyUsedAsync(
                supplierId,
                invoiceKey,
                cancellationToken,
                submissionId))
            throw new InvalidOperationException("此供應商發票已存在請款申請或正式應付帳款。");

        var currentRevision = CurrentRevision(submission);
        if (currentRevision.SubmittedAt.HasValue)
        {
            currentRevision = CreateRevision(
                submission,
                supplierUserId,
                submission.CurrentRevisionNumber + 1,
                request,
                sources,
                DateTime.UtcNow);
            submission.Revisions.Add(currentRevision);
            submission.CurrentRevisionNumber++;
        }
        else
        {
            ApplyRequest(currentRevision, request, sources);
        }

        submission.SupplierInvoiceNumberKey = invoiceKey;
        submission.UpdatedAt = DateTime.UtcNow;
        submission.Version++;
        await SaveWithConcurrencyAsync(cancellationToken);
        return ToResult(submission, currentRevision);
    }

    public async Task<SupplierApSubmissionResultDto> SubmitAsync(
        Guid submissionId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        EnsureCanSubmit(supplierRole);
        var submission = await LoadTrackedSubmissionAsync(db, submissionId, supplierId, cancellationToken)
            ?? throw new KeyNotFoundException("請款申請不存在。");
        if (submission.Status == "submitted")
        {
            var submittedRevision = CurrentRevision(submission);
            return ToResult(submission, submittedRevision);
        }
        EnsureVersion(submission, expectedVersion);
        if (submission.Status is not ("draft" or "returned"))
            throw new InvalidOperationException("目前狀態不可提交請款。");

        var revision = CurrentRevision(submission);
        var request = ToRequest(revision);
        var sources = await LoadAndValidateSourcesAsync(db, supplierId, request, cancellationToken);
        ValidateInvoiceUniqueness(submission);
        if (await IsInvoiceAlreadyUsedAsync(
                supplierId,
                submission.SupplierInvoiceNumberKey!,
                cancellationToken,
                submissionId))
            throw new InvalidOperationException("此供應商發票已存在正式應付帳款。");
        var attachments = await db.SupplierApSubmissionAttachments
            .Where(item => item.SubmissionId == submissionId
                && item.RevisionId == revision.Id
                && item.RevokedAt == null)
            .ToListAsync(cancellationToken);
        if (attachments.Count == 0)
            throw new SupplierApSubmissionPolicyException("提交前必須上傳至少一份請款憑證。");
        if (attachments.Any(item => item.ScanStatus != "clean"))
            throw new SupplierApSubmissionPolicyException("所有有效附件完成安全掃描前不可提交。");

        var discrepancies = EvaluateMatch(revision, sources);
        revision.SubmittedAt = DateTime.UtcNow;
        revision.ContentHash = ComputeHash(request);
        submission.Status = "submitted";
        submission.SubmittedBySupplierUserId = supplierUserId;
        submission.UpdatedAt = DateTime.UtcNow;
        submission.Version++;
        await SaveWithConcurrencyAsync(cancellationToken);
        return ToResult(submission, revision, discrepancies);
    }

    public async Task<SupplierApSubmissionResultDto> CancelAsync(
        Guid submissionId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        int expectedVersion,
        string? reason,
        CancellationToken cancellationToken)
    {
        EnsureCanSubmit(supplierRole);
        if (reason?.Length > 2000)
            throw new ArgumentException("取消原因不可超過 2000 個字元。");
        var submission = await LoadTrackedSubmissionAsync(db, submissionId, supplierId, cancellationToken)
            ?? throw new KeyNotFoundException("請款申請不存在。");
        EnsureVersion(submission, expectedVersion);
        if (submission.Status is not ("draft" or "returned"))
            throw new InvalidOperationException("目前狀態不可取消請款申請。");

        var revision = CurrentRevision(submission);
        submission.Status = "cancelled";
        submission.UpdatedAt = DateTime.UtcNow;
        submission.Version++;
        db.AuditLogs.Add(new AuditLog
        {
            Action = "supplier_ap_submission.cancelled",
            EntityType = nameof(SupplierApSubmission),
            EntityId = submission.Id,
            Details = JsonSerializer.Serialize(new
            {
                supplierUserId,
                version = submission.Version,
                reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()
            }),
            CreatedAt = submission.UpdatedAt
        });
        await SaveWithConcurrencyAsync(cancellationToken);
        return ToResult(submission, revision);
    }

    public async Task<SupplierApSubmissionPageDto> GetEmployeeSubmissionsAsync(
            string? status,
            Guid? supplierId,
            DateOnly? invoiceDateFrom,
            DateOnly? invoiceDateTo,
            string? submissionNumber,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
    {
        var query = db.SupplierApSubmissions.AsNoTracking().AsQueryable();
        if (supplierId.HasValue)
            query = query.Where(item => item.SupplierId == supplierId.Value);
        query = FilterSubmissions(query, status, invoiceDateFrom, invoiceDateTo, submissionNumber);
        return await ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<SupplierApSubmissionDetailDto?> GetEmployeeSubmissionAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var submission = await LoadDetailAsync(db, submissionId, cancellationToken);
        return submission is null ? null : ToDetail(submission, includeReviewer: true);
    }

    public async Task<SupplierApSubmissionResultDto> ReviewAsync(
        Guid submissionId,
        Guid reviewerUserId,
        string action,
        int expectedVersion,
        string reason,
        CancellationToken cancellationToken)
    {
        if (action is not ("return" or "reject" or "approve"))
            throw new ArgumentException("不支援的審核動作。");
        if (action != "approve" && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("退回或拒絕時必須填寫原因。");

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var submission = await LoadTrackedSubmissionAsync(db, submissionId, null, cancellationToken)
            ?? throw new KeyNotFoundException("請款申請不存在。");

        if (action == "approve" && submission.Status == "approved"
            && submission.GeneratedAccountsPayable is not null)
        {
            var approved = CurrentRevision(submission);
            return ToResult(submission, approved);
        }

        EnsureVersion(submission, expectedVersion);
        if (submission.Status != "submitted")
            throw new InvalidOperationException("只有待審案件可以執行審核。");

        var revision = CurrentRevision(submission);
        var now = DateTime.UtcNow;
        var review = new SupplierApSubmissionReview
        {
            SubmissionId = submission.Id,
            RevisionId = revision.Id,
            ReviewerUserId = reviewerUserId,
            Action = action switch
            {
                "return" => "returned",
                "reject" => "rejected",
                _ => "approved"
            },
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedAt = now
        };

        IReadOnlyList<string> discrepancies = [];
        if (action == "approve")
        {
            var sources = await LoadAndValidateSourcesAsync(
                db,
                submission.SupplierId,
                ToRequest(revision),
                cancellationToken,
                submission.Id);
            if (await IsInvoiceAlreadyUsedAsync(
                    submission.SupplierId,
                    submission.SupplierInvoiceNumberKey!,
                    cancellationToken,
                    submission.Id))
                throw new InvalidOperationException("此供應商發票已存在正式應付帳款。");
            discrepancies = EvaluateMatch(revision, sources);
            review.MatchResult = discrepancies.Count == 0 ? "matched" : "variance";
            review.MatchRuleVersion = "ap-submission-v1";
            review.MatchSnapshot = JsonSerializer.Serialize(new
            {
                ruleVersion = review.MatchRuleVersion,
                evaluatedAt = now,
                discrepancies
            });
            if (discrepancies.Count > 0)
                throw new SupplierApSubmissionPolicyException(
                    "三方核對存在未解差異，請退回或拒絕；差異容差政策尚未啟用。");

            var attachmentsClean = await db.SupplierApSubmissionAttachments.AnyAsync(
                item => item.SubmissionId == submissionId
                    && item.RevisionId == revision.Id
                    && item.RevokedAt == null
                    && item.ScanStatus == "clean",
                cancellationToken);
            if (!attachmentsClean)
                throw new SupplierApSubmissionPolicyException("至少需要一份掃描通過的有效請款憑證。");

            var order = await db.PurchaseOrders.SingleAsync(
                item => item.Id == revision.PurchaseOrderId,
                cancellationToken);
            var payable = CreateAccountsPayable(submission, revision, order, now);
            db.AccountsPayables.Add(payable);
            review.AccountsPayableId = payable.Id;
            submission.GeneratedAccountsPayable = payable;
            submission.Status = "approved";
        }
        else
        {
            submission.Status = action == "return" ? "returned" : "rejected";
        }

        submission.Version++;
        submission.UpdatedAt = now;
        db.SupplierApSubmissionReviews.Add(review);
        await SaveWithConcurrencyAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return ToResult(submission, revision, discrepancies);
    }

    private async Task SaveWithConcurrencyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InvalidOperationException("請款內容已被其他使用者更新，請重新載入後再試。", exception);
        }
        catch (DbUpdateException exception) when (IsSubmissionUniqueConflict(exception))
        {
            throw new InvalidOperationException("此供應商已存在相同發票號碼或冪等識別的請款申請。", exception);
        }
    }

    private async Task<bool> IsInvoiceAlreadyUsedAsync(
        Guid supplierId,
        string invoiceKey,
        CancellationToken cancellationToken,
        Guid? excludedSubmissionId = null)
    {
        if (await db.SupplierApSubmissions.AnyAsync(
                item => item.SupplierId == supplierId
                    && (!excludedSubmissionId.HasValue || item.Id != excludedSubmissionId.Value)
                    && item.SupplierInvoiceNumberKey == invoiceKey,
                cancellationToken))
            return true;

        var existingInvoices = await db.AccountsPayables
            .AsNoTracking()
            .Where(item => item.SupplierId == supplierId && item.SupplierInvoiceNumber != null)
            .Select(item => item.SupplierInvoiceNumber!)
            .ToListAsync(cancellationToken);
        return existingInvoices.Any(invoice =>
            string.Equals(NormalizeInvoiceNumber(invoice), invoiceKey, StringComparison.Ordinal));
    }

}
