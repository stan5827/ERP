using System.Security.Cryptography;
using System.Text.Json;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Procurement;
using ErpBackend.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ErpBackend.Services.SupplierApSubmissionSupport;

internal static class SupplierApSubmissionWorkflowRules
{
    internal static readonly HashSet<string> EligiblePurchaseOrderStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "confirmed", "partial_received" };
    internal static readonly HashSet<string> EligibleReceiptStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "received" };
    internal static readonly HashSet<string> EligibleIqcStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "passed", "not_required" };
    private const int MaximumInvoiceNumberLength = 50;

    internal static async Task<ValidatedSources> LoadAndValidateSourcesAsync(
        ErpGeneralDbContext db,
        Guid supplierId,
        SupplierApSubmissionDraftRequestDto request,
        CancellationToken cancellationToken,
        Guid? excludedSubmissionId = null)
    {
        var order = await db.PurchaseOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.Sku)
            .SingleOrDefaultAsync(
                item => item.Id == request.PurchaseOrderId && item.SupplierId == supplierId,
                cancellationToken)
            ?? throw new KeyNotFoundException("採購單不存在或不屬於此供應商。");
        var receipt = await db.GoodsReceipts
            .Include(item => item.Items)
                .ThenInclude(item => item.Sku)
            .SingleOrDefaultAsync(
                item => item.Id == request.GoodsReceiptId
                    && item.SupplierId == supplierId
                    && item.PurchaseOrderId == request.PurchaseOrderId,
                cancellationToken)
            ?? throw new KeyNotFoundException("收貨單不存在或不屬於此供應商採購單。");
        if (!EligiblePurchaseOrderStatuses.Contains(order.Status) || order.IsLocked
            || !EligibleReceiptStatuses.Contains(receipt.Status)
            || !EligibleIqcStatuses.Contains(receipt.IqcStatus))
            throw new SupplierApSubmissionPolicyException("採購單或收貨／驗收單目前不可請款。");
        if (!string.Equals(request.Currency.Trim(), order.Currency, StringComparison.OrdinalIgnoreCase))
            throw new SupplierApSubmissionPolicyException("請款幣別必須與採購單幣別一致。");

        var receiptItems = receipt.Items.ToDictionary(item => item.Id);
        if (receipt.Items.GroupBy(item => item.SkuId).Any(group => group.Count() > 1))
            throw new SupplierApSubmissionPolicyException(
                "收貨單含重複品號，現有來源資料無法安全區分請款品項。");
        var lineIds = request.Lines.Select(line => line.GoodsReceiptItemId).ToArray();
        if (lineIds.Distinct().Count() != lineIds.Length
            || lineIds.Any(id => !receiptItems.ContainsKey(id)))
            throw new ArgumentException("請款明細包含重複或不屬於指定收貨單的品項。");
        var reserved = await GetInvoicedQuantitiesAsync(
            db,
            receipt.Id,
            cancellationToken,
            excludedSubmissionId);
        var selectedLines = new List<ValidatedLine>();
        foreach (var line in request.Lines)
        {
            var receiptItem = receiptItems[line.GoodsReceiptItemId];
            var poItems = order.Items.Where(item => item.SkuId == receiptItem.SkuId).ToArray();
            var poItem = poItems.Length == 1 ? poItems[0] : null;
            if (poItem is null)
                throw new SupplierApSubmissionPolicyException(
                    "收貨品項無法唯一對應採購單明細；請先完成來源品項對應。");
            reserved.TryGetValue(receiptItem.Id, out var alreadyInvoiced);
            if (line.InvoicedQuantity > receiptItem.AcceptedQty - alreadyInvoiced)
                throw new SupplierApSubmissionPolicyException("請款數量超過收貨允收且未請款的數量。");
            selectedLines.Add(new ValidatedLine(receiptItem, poItem));
        }
        return new ValidatedSources(order, receipt, selectedLines);
    }

    internal static async Task<Dictionary<Guid, decimal>> GetInvoicedQuantitiesAsync(
        ErpGeneralDbContext db,
        Guid receiptId,
        CancellationToken cancellationToken,
        Guid? excludedSubmissionId = null)
    {
        var approved = await (
                from apItem in db.AccountsPayableItems.AsNoTracking()
                join ap in db.AccountsPayables.AsNoTracking()
                    on apItem.AccountsPayableId equals ap.Id
                where ap.GoodsReceiptId == receiptId
                select new { apItem.SkuId, apItem.Quantity })
            .ToListAsync(cancellationToken);
        var submitted = await db.SupplierApSubmissionRevisionLines
            .AsNoTracking()
            .Where(line => line.Revision.Submission.Status == "submitted"
                && (!excludedSubmissionId.HasValue
                    || line.Revision.SubmissionId != excludedSubmissionId.Value)
                && line.Revision.RevisionNumber == line.Revision.Submission.CurrentRevisionNumber
                && line.Revision.GoodsReceiptId == receiptId)
            .Select(line => new { line.GoodsReceiptItemId, line.InvoicedQuantity })
            .ToListAsync(cancellationToken);
        var receiptItems = await db.GoodsReceiptItems
            .AsNoTracking()
            .Where(item => item.GoodsReceiptId == receiptId)
            .Select(item => new { item.Id, item.SkuId })
            .ToListAsync(cancellationToken);
        var quantities = receiptItems.ToDictionary(item => item.Id, _ => 0m);
        foreach (var item in receiptItems)
            quantities[item.Id] += approved
                .Where(apItem => apItem.SkuId == item.SkuId)
                .Sum(apItem => apItem.Quantity);
        foreach (var line in submitted)
        {
            if (quantities.ContainsKey(line.GoodsReceiptItemId))
                quantities[line.GoodsReceiptItemId] += line.InvoicedQuantity;
        }
        return quantities;
    }

    internal static SupplierApSubmissionRevision CreateRevision(
        SupplierApSubmission submission,
        Guid supplierUserId,
        int revisionNumber,
        SupplierApSubmissionDraftRequestDto request,
        ValidatedSources sources,
        DateTime now)
    {
        var revision = new SupplierApSubmissionRevision
        {
            SubmissionId = submission.Id,
            RevisionNumber = revisionNumber,
            SubmittedBySupplierUserId = supplierUserId,
            CreatedAt = now
        };
        ApplyRequest(revision, request, sources);
        return revision;
    }

    internal static void ApplyRequest(
        SupplierApSubmissionRevision revision,
        SupplierApSubmissionDraftRequestDto request,
        ValidatedSources sources)
    {
        revision.SupplierInvoiceNumber = request.SupplierInvoiceNumber.Trim();
        revision.SupplierInvoiceDate = request.SupplierInvoiceDate;
        revision.Currency = request.Currency.Trim().ToUpperInvariant();
        revision.InvoiceAmount = request.InvoiceAmount;
        revision.TaxAmount = request.TaxAmount;
        revision.TotalAmount = request.TotalAmount;
        revision.PurchaseOrderId = sources.Order.Id;
        revision.GoodsReceiptId = sources.Receipt.Id;
        revision.Lines.Clear();
        for (var index = 0; index < request.Lines.Count; index++)
        {
            var input = request.Lines[index];
            var source = sources.Lines[index];
            revision.Lines.Add(new SupplierApSubmissionRevisionLine
            {
                RevisionId = revision.Id,
                GoodsReceiptItemId = source.ReceiptItem.Id,
                PurchaseOrderItemId = source.OrderItem.Id,
                InvoicedQuantity = input.InvoicedQuantity,
                UnitPrice = input.UnitPrice,
                NetAmount = input.NetAmount,
                TaxAmount = input.TaxAmount,
                TotalAmount = input.TotalAmount
            });
        }
    }

    internal static SupplierApSubmissionDraftRequestDto ToRequest(SupplierApSubmissionRevision revision) =>
        new(
            revision.SupplierInvoiceNumber,
            revision.SupplierInvoiceDate,
            revision.Currency,
            revision.InvoiceAmount,
            revision.TaxAmount,
            revision.TotalAmount,
            revision.PurchaseOrderId,
            revision.GoodsReceiptId,
            revision.Lines.Select(line => new SupplierApSubmissionLineRequestDto(
                line.GoodsReceiptItemId,
                line.InvoicedQuantity,
                line.UnitPrice,
                line.NetAmount,
                line.TaxAmount,
                line.TotalAmount)).ToArray());

    internal static IReadOnlyList<string> EvaluateMatch(
        SupplierApSubmissionRevision revision,
        ValidatedSources sources)
    {
        var discrepancies = new List<string>();
        if (revision.InvoiceAmount != revision.Lines.Sum(line => line.NetAmount))
            discrepancies.Add("invoice_amount_mismatch");
        if (revision.TaxAmount != revision.Lines.Sum(line => line.TaxAmount))
            discrepancies.Add("tax_amount_mismatch");
        if (revision.TotalAmount != revision.Lines.Sum(line => line.TotalAmount)
            || revision.TotalAmount != revision.InvoiceAmount + revision.TaxAmount)
            discrepancies.Add("total_amount_mismatch");
        foreach (var line in revision.Lines)
        {
            var source = sources.Lines.SingleOrDefault(item =>
                item.ReceiptItem.Id == line.GoodsReceiptItemId);
            if (source is null)
            {
                discrepancies.Add($"receipt_item_missing:{line.GoodsReceiptItemId}");
                continue;
            }
            if (line.UnitPrice != source.OrderItem.UnitPrice)
                discrepancies.Add($"unit_price_mismatch:{line.GoodsReceiptItemId}");
            if (line.InvoicedQuantity <= 0
                || line.InvoicedQuantity > source.ReceiptItem.AcceptedQty)
                discrepancies.Add($"quantity_mismatch:{line.GoodsReceiptItemId}");
            if (decimal.Round(line.InvoicedQuantity * line.UnitPrice, 2) != line.NetAmount)
                discrepancies.Add($"line_net_amount_mismatch:{line.GoodsReceiptItemId}");
            if (line.TotalAmount != line.NetAmount + line.TaxAmount)
                discrepancies.Add($"line_total_mismatch:{line.GoodsReceiptItemId}");
        }
        return discrepancies.Distinct(StringComparer.Ordinal).ToArray();
    }

    internal static AccountsPayable CreateAccountsPayable(
        SupplierApSubmission submission,
        SupplierApSubmissionRevision revision,
        PurchaseOrder order,
        DateTime now)
    {
        if (order.ExchangeRate <= 0)
            throw new SupplierApSubmissionPolicyException("採購單匯率無效，無法建立正式 AP。");
        var dateUtc = DateTime.SpecifyKind(
            revision.SupplierInvoiceDate.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);
        var payable = new AccountsPayable
        {
            ApNumber = $"AP-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..30],
            SupplierId = submission.SupplierId,
            PurchaseOrderId = revision.PurchaseOrderId,
            GoodsReceiptId = revision.GoodsReceiptId,
            SupplierInvoiceNumber = revision.SupplierInvoiceNumber,
            SupplierInvoiceDate = dateUtc,
            Currency = revision.Currency,
            ExchangeRate = order.ExchangeRate,
            TaxType = order.TaxType,
            TaxCode = order.TaxCode,
            InvoiceAmount = revision.InvoiceAmount,
            TaxAmount = revision.TaxAmount,
            TotalAmount = revision.TotalAmount,
            BaseInvoiceAmount = decimal.Round(revision.InvoiceAmount * order.ExchangeRate, 2),
            BaseTaxAmount = decimal.Round(revision.TaxAmount * order.ExchangeRate, 2),
            BaseTotalAmount = decimal.Round(revision.TotalAmount * order.ExchangeRate, 2),
            DueDate = order.DueDate,
            PaymentTerms = order.PaymentTerms,
            VerificationStatus = "pending",
            PaymentStatus = "unpaid",
            ThreeWayMatchResult = "matched",
            CreatedAt = now
        };
        foreach (var line in revision.Lines)
        {
            var receiptItem = line.GoodsReceiptItem
                ?? throw new SupplierApSubmissionPolicyException("收貨品項已不存在，無法建立正式 AP。");
            payable.Items.Add(new AccountsPayableItem
            {
                AccountsPayableId = payable.Id,
                SkuId = receiptItem.SkuId,
                Quantity = line.InvoicedQuantity,
                UnitPrice = line.UnitPrice,
                Amount = line.NetAmount
            });
        }
        return payable;
    }

    internal static SupplierApSubmissionResultDto ToResult(
        SupplierApSubmission submission,
        SupplierApSubmissionRevision revision,
        IReadOnlyList<string>? discrepancies = null)
    {
        var matchResult = submission.Status == "approved"
            ? "matched"
            : discrepancies is { Count: > 0 }
                ? "variance"
                : "pending";
        return new SupplierApSubmissionResultDto(
            submission.Id,
            submission.SubmissionNumber,
            submission.Status,
            revision.RevisionNumber,
            submission.Version,
            matchResult,
            discrepancies ?? [],
            revision.SubmittedAt.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(revision.SubmittedAt.Value, DateTimeKind.Utc))
                : null,
            submission.GeneratedAccountsPayableId,
            submission.GeneratedAccountsPayable?.ApNumber);
    }

    internal static void ValidateRequest(SupplierApSubmissionCreateRequestDto request) =>
        ValidateRequest(new SupplierApSubmissionDraftRequestDto(
            request.SupplierInvoiceNumber,
            request.SupplierInvoiceDate,
            request.Currency,
            request.InvoiceAmount,
            request.TaxAmount,
            request.TotalAmount,
            request.PurchaseOrderId,
            request.GoodsReceiptId,
            request.Lines));

    internal static void ValidateRequest(SupplierApSubmissionDraftRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.SupplierInvoiceNumber)
            || request.SupplierInvoiceNumber.Trim().Length > MaximumInvoiceNumberLength)
            throw new ArgumentException("發票號碼為必填，且不可超過 50 個字元。");
        if (request.SupplierInvoiceDate == default)
            throw new ArgumentException("發票日期為必填。");
        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Trim().Length > 10)
            throw new ArgumentException("幣別為必填，且不可超過 10 個字元。");
        if (request.PurchaseOrderId == Guid.Empty || request.GoodsReceiptId == Guid.Empty)
            throw new ArgumentException("採購單與收貨單為必填。");
        if (request.InvoiceAmount <= 0 || request.TaxAmount < 0
            || request.TotalAmount <= 0 || request.TotalAmount != request.InvoiceAmount + request.TaxAmount)
            throw new ArgumentException("發票未稅金額、稅額與含稅總額不一致。");
        if (request.Lines is null || request.Lines.Count == 0)
            throw new ArgumentException("至少需要一筆請款明細。");
        if (request.Lines.Any(line => line.InvoicedQuantity <= 0
                || line.UnitPrice < 0
                || line.NetAmount < 0
                || line.TaxAmount < 0
                || line.TotalAmount != line.NetAmount + line.TaxAmount))
            throw new ArgumentException("請款明細數量或金額無效。");
        if (request.Lines.Any(line =>
                decimal.Round(line.InvoicedQuantity, 2) != line.InvoicedQuantity
                || decimal.Round(line.UnitPrice, 4) != line.UnitPrice
                || decimal.Round(line.NetAmount, 2) != line.NetAmount
                || decimal.Round(line.TaxAmount, 2) != line.TaxAmount
                || decimal.Round(line.TotalAmount, 2) != line.TotalAmount))
            throw new ArgumentException("請款數量或金額精度超出允許位數。");
        if (request.InvoiceAmount != request.Lines.Sum(line => line.NetAmount)
            || request.TaxAmount != request.Lines.Sum(line => line.TaxAmount)
            || request.TotalAmount != request.Lines.Sum(line => line.TotalAmount))
            throw new ArgumentException("表頭金額與明細加總不一致。");
    }

    internal static string NormalizeInvoiceNumber(string invoiceNumber) =>
        string.Concat(invoiceNumber.Trim().Where(character => !char.IsWhiteSpace(character)))
            .ToUpperInvariant();

    internal static string ComputeHash<T>(T value) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));

    internal static bool IsSubmissionUniqueConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: not null
        } postgres
        && (postgres.ConstraintName.Contains("SupplierInvoiceNumberKey", StringComparison.Ordinal)
            || postgres.ConstraintName.Contains("IdempotencyKey", StringComparison.Ordinal)
            || postgres.ConstraintName.Contains("SubmissionNumber", StringComparison.Ordinal));

    internal static void EnsureCanSubmit(string supplierRole)
    {
        if (!string.Equals(supplierRole, "sales", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("目前供應商入口角色沒有提交請款的權限。");
    }

    internal static void EnsureVersion(SupplierApSubmission submission, int expectedVersion)
    {
        if (expectedVersion <= 0 || submission.Version != expectedVersion)
            throw new InvalidOperationException("請款內容版本已變更，請重新載入後再試。");
    }

    internal static void ValidateInvoiceUniqueness(SupplierApSubmission submission)
    {
        var expectedKey = NormalizeInvoiceNumber(CurrentRevision(submission).SupplierInvoiceNumber);
        if (!string.Equals(submission.SupplierInvoiceNumberKey, expectedKey, StringComparison.Ordinal))
            throw new InvalidOperationException("發票號碼索引不一致，請重新儲存草稿。");
    }

    internal static SupplierApSubmissionRevision CurrentRevision(SupplierApSubmission submission) =>
        submission.Revisions.Single(revision =>
            revision.RevisionNumber == submission.CurrentRevisionNumber);

    internal sealed record ValidatedSources(
        PurchaseOrder Order,
        GoodsReceipt Receipt,
        IReadOnlyList<ValidatedLine> Lines);

    internal sealed record ValidatedLine(
        GoodsReceiptItem ReceiptItem,
        PurchaseOrderItem OrderItem);
}
