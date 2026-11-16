using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Helpers;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Procurement;

namespace ErpBackend.Services;

public interface IAccountsPayableService
{
    Task<List<ApSummaryDto>> GetApSummaryAsync(string? supplierId = null, string? startDate = null, string? endDate = null);
    Task<List<ApAgingDto>> GetAgingReportAsync();
    Task<List<ApDetailDto>> GetApBySupplierAsync(Guid supplierId);
    Task<(bool found, string status)> RecordPaymentAsync(RecordApPaymentRequest req);
    Task<(bool found, string status)> RecordAccountsPayablePaymentAsync(RecordAccountsPayablePaymentRequest req);
}

public class AccountsPayableService : IAccountsPayableService
{
    private readonly ErpGeneralDbContext _db;

    public AccountsPayableService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<ApSummaryDto>> GetApSummaryAsync(
        string? supplierId = null,
        string? startDate = null,
        string? endDate = null)
    {
        var query = _db.AccountsPayables.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(supplierId) && Guid.TryParse(supplierId, out var suppGuid))
            query = query.Where(ap => ap.SupplierId == suppGuid);
        if (DateOnly.TryParse(startDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
        {
            var startDateUtc = DateTime.SpecifyKind(
                fromDate.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);
            query = query.Where(ap => ap.SupplierInvoiceDate >= startDateUtc);
        }
        if (DateOnly.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate)
            && toDate < DateOnly.MaxValue)
        {
            var endDateExclusiveUtc = DateTime.SpecifyKind(
                toDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);
            query = query.Where(ap => ap.SupplierInvoiceDate < endDateExclusiveUtc);
        }

        var payables = await query.ToListAsync();
        if (payables.Count == 0)
            return [];

        var balances = await GetBalancesAsync(payables);
        var supplierIds = balances
            .Where(balance => balance.Outstanding > 0)
            .Select(balance => balance.Payable.SupplierId)
            .Distinct()
            .ToList();
        if (supplierIds.Count == 0)
            return [];

        var suppliers = await _db.Suppliers
            .AsNoTracking()
            .Where(supplier => supplierIds.Contains(supplier.Id))
            .ToDictionaryAsync(supplier => supplier.Id);

        return balances
            .Where(balance => balance.Outstanding > 0 && suppliers.ContainsKey(balance.Payable.SupplierId))
            .GroupBy(balance => balance.Payable.SupplierId)
            .Select(group =>
            {
                var supplier = suppliers[group.Key];
                return new ApSummaryDto
                {
                    SupplierId = supplier.Id,
                    SupplierCode = supplier.SupplierCode,
                    SupplierName = supplier.SupplierName,
                    TotalPayable = group.Sum(balance => balance.Outstanding),
                    Count = group.Count()
                };
            })
            .OrderBy(item => item.SupplierName)
            .ToList();
    }

    public async Task<List<ApAgingDto>> GetAgingReportAsync()
    {
        var payables = await _db.AccountsPayables.AsNoTracking().ToListAsync();
        if (payables.Count == 0)
            return [];

        var balances = await GetBalancesAsync(payables);
        var supplierIds = balances
            .Where(balance => balance.Outstanding > 0)
            .Select(balance => balance.Payable.SupplierId)
            .Distinct()
            .ToList();
        if (supplierIds.Count == 0)
            return [];

        var suppliers = await _db.Suppliers
            .AsNoTracking()
            .Where(supplier => supplierIds.Contains(supplier.Id))
            .ToDictionaryAsync(supplier => supplier.Id);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return balances
            .Where(balance => balance.Outstanding > 0 && suppliers.ContainsKey(balance.Payable.SupplierId))
            .GroupBy(balance => balance.Payable.SupplierId)
            .Select(group =>
            {
                var aging = new ApAgingDto
                {
                    SupplierId = group.Key,
                    SupplierName = suppliers[group.Key].SupplierName
                };
                foreach (var balance in group)
                {
                    if (balance.Payable.DueDate is not DateTime dueDate)
                    {
                        aging.Current += balance.Outstanding;
                        continue;
                    }

                    var daysPastDue = today.DayNumber - DateOnly.FromDateTime(dueDate).DayNumber;
                    if (daysPastDue <= 0)
                        aging.Current += balance.Outstanding;
                    else if (daysPastDue <= 30)
                        aging.Days30 += balance.Outstanding;
                    else if (daysPastDue <= 60)
                        aging.Days60 += balance.Outstanding;
                    else if (daysPastDue <= 90)
                        aging.Days90 += balance.Outstanding;
                    else
                        aging.Over90 += balance.Outstanding;
                }

                return aging;
            })
            .OrderBy(item => item.SupplierName)
            .ToList();
    }

    public async Task<List<ApDetailDto>> GetApBySupplierAsync(Guid supplierId)
    {
        var payables = await _db.AccountsPayables
            .AsNoTracking()
            .Where(ap => ap.SupplierId == supplierId)
            .ToListAsync();
        if (payables.Count == 0)
            return [];

        var balances = await GetBalancesAsync(payables);
        var purchaseOrderIds = payables
            .Where(ap => ap.PurchaseOrderId.HasValue)
            .Select(ap => ap.PurchaseOrderId!.Value)
            .Distinct()
            .ToList();
        var purchaseOrders = await _db.PurchaseOrders
            .AsNoTracking()
            .Where(po => purchaseOrderIds.Contains(po.Id))
            .ToDictionaryAsync(po => po.Id);

        return balances
            .Select(balance =>
            {
                purchaseOrders.TryGetValue(balance.Payable.PurchaseOrderId ?? Guid.Empty, out var purchaseOrder);
                return new ApDetailDto
                {
                    Id = balance.Payable.Id,
                    ApNumber = balance.Payable.ApNumber,
                    SupplierInvoiceNumber = balance.Payable.SupplierInvoiceNumber,
                    SupplierInvoiceDate = balance.Payable.SupplierInvoiceDate,
                    Currency = balance.Payable.Currency,
                    PoNumber = purchaseOrder?.PoNumber,
                    PoDate = purchaseOrder?.PoDate
                        ?? balance.Payable.SupplierInvoiceDate
                        ?? balance.Payable.CreatedAt,
                    DueDate = balance.Payable.DueDate,
                    TotalAmount = balance.Payable.TotalAmount,
                    PaidAmount = balance.PaidAmount,
                    DiscountAmount = balance.DiscountAmount,
                    DebitNoteAmount = balance.DebitNoteAmount,
                    Outstanding = balance.Outstanding,
                    Status = balance.Outstanding <= 0
                        ? "paid"
                        : balance.PaidAmount + balance.DiscountAmount + balance.DebitNoteAmount > 0
                            ? "partial"
                            : "unpaid"
                };
            })
            .OrderBy(item => item.SupplierInvoiceDate ?? item.PoDate)
            .ThenBy(item => item.ApNumber)
            .ToList();
    }

    private async Task<List<AccountsPayableBalance>> GetBalancesAsync(
        IReadOnlyCollection<AccountsPayable> payables)
    {
        var payableIds = payables.Select(payable => payable.Id).ToArray();
        var payments = await GetPostedPaymentTotalsAsync(payableIds);
        var debitNotes = await GetActiveDebitNoteAmountsAsync(payableIds);

        return payables.Select(payable =>
        {
            payments.TryGetValue(payable.Id, out var payment);
            debitNotes.TryGetValue(payable.Id, out var debitNoteAmount);
            var paidAmount = payment.PaidAmount;
            var discountAmount = payment.DiscountAmount;
            return new AccountsPayableBalance(
                payable,
                paidAmount,
                discountAmount,
                debitNoteAmount,
                Math.Max(0m, payable.TotalAmount - paidAmount - discountAmount - debitNoteAmount));
        }).ToList();
    }

    private async Task<Dictionary<Guid, PaymentTotals>> GetPostedPaymentTotalsAsync(
        IReadOnlyCollection<Guid> payableIds)
    {
        if (payableIds.Count == 0)
            return [];

        var records = await (
            from record in _db.PaymentRecords.AsNoTracking()
            join batch in _db.PaymentBatches.AsNoTracking()
                on record.PaymentBatchId equals batch.Id
            where payableIds.Contains(record.AccountsPayableId)
                && batch.Status.ToLower() == "posted"
            select new
            {
                record.AccountsPayableId,
                record.PaidAmount,
                record.DiscountAmount
            }).ToListAsync();

        return records
            .GroupBy(record => record.AccountsPayableId)
            .ToDictionary(
                group => group.Key,
                group => new PaymentTotals(
                    group.Sum(record => record.PaidAmount),
                    group.Sum(record => record.DiscountAmount)));
    }

    private async Task<Dictionary<Guid, decimal>> GetActiveDebitNoteAmountsAsync(
        IReadOnlyCollection<Guid> payableIds)
    {
        if (payableIds.Count == 0)
            return [];

        var notes = await _db.SupplierDebitNotes
            .AsNoTracking()
            .Where(note => payableIds.Contains(note.AccountsPayableId)
                && note.Status.ToLower() == "posted")
            .Select(note => new { note.Id, note.AccountsPayableId, note.TotalAmount })
            .ToListAsync();
        if (notes.Count == 0)
            return [];

        var noteIds = notes.Select(note => note.Id).ToArray();
        var postedVouchers = await _db.AccountingVouchers
            .AsNoTracking()
            .Where(voucher => voucher.ReferenceType == "SupplierDebitNote"
                && voucher.ReferenceId.HasValue
                && noteIds.Contains(voucher.ReferenceId.Value)
                && voucher.PostingPurpose == "supplier_debit_note"
                && voucher.Status.ToLower() == "posted")
            .Select(voucher => new { voucher.Id, NoteId = voucher.ReferenceId!.Value })
            .ToListAsync();
        if (postedVouchers.Count == 0)
            return [];

        var voucherIds = postedVouchers.Select(voucher => voucher.Id).ToArray();
        var reversedVoucherIds = await _db.AccountingVouchers
            .AsNoTracking()
            .Where(voucher => voucher.ReversalOfVoucherId.HasValue
                && voucherIds.Contains(voucher.ReversalOfVoucherId.Value)
                && voucher.Status.ToLower() == "posted")
            .Select(voucher => voucher.ReversalOfVoucherId!.Value)
            .ToListAsync();
        var reversedSet = reversedVoucherIds.ToHashSet();
        var activeNoteIds = postedVouchers
            .Where(voucher => !reversedSet.Contains(voucher.Id))
            .Select(voucher => voucher.NoteId)
            .ToHashSet();

        return notes
            .Where(note => activeNoteIds.Contains(note.Id))
            .GroupBy(note => note.AccountsPayableId)
            .ToDictionary(group => group.Key, group => group.Sum(note => note.TotalAmount));
    }

    private readonly record struct PaymentTotals(decimal PaidAmount, decimal DiscountAmount);

    private sealed record AccountsPayableBalance(
        AccountsPayable Payable,
        decimal PaidAmount,
        decimal DiscountAmount,
        decimal DebitNoteAmount,
        decimal Outstanding);

    public async Task<(bool found, string status)> RecordPaymentAsync(RecordApPaymentRequest req)
    {
        if (!IsValidPaymentAmount(req.PaymentAmount))
            return (true, "invalid_amount");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        var purchaseOrder = await LockPurchaseOrderAsync(req.PurchaseOrderId);
        if (purchaseOrder is null)
            return (false, "not_found");

        var accountsPayables = await _db.AccountsPayables
            .Where(ap => ap.PurchaseOrderId == purchaseOrder.Id)
            .OrderBy(ap => ap.Id)
            .ToListAsync();
        if (accountsPayables.Count == 0)
            return (true, "no_ap_for_po");
        if (accountsPayables.Count > 1)
            return (true, "ambiguous_ap");

        var ap = await LockAccountsPayableAsync(accountsPayables[0].Id);
        if (ap is null)
            return (false, "not_found");
        if (ap.PurchaseOrderId != purchaseOrder.Id ||
            ap.SupplierId != purchaseOrder.SupplierId)
            return (true, "ap_po_mismatch");

        var status = await RecordPaymentWithinTransactionAsync(
            ap,
            purchaseOrder,
            req.PaymentAmount);
        if (status != "ok")
            return (true, status);

        if (transaction is not null)
            await transaction.CommitAsync();
        return (true, "ok");
    }

    public async Task<(bool found, string status)> RecordAccountsPayablePaymentAsync(
        RecordAccountsPayablePaymentRequest req)
    {
        if (req.AccountsPayableId == Guid.Empty || !IsValidPaymentAmount(req.PaymentAmount))
            return (true, "invalid_amount");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        var initialAp = await _db.AccountsPayables
            .AsNoTracking()
            .SingleOrDefaultAsync(ap => ap.Id == req.AccountsPayableId);
        if (initialAp is null)
            return (false, "not_found");

        var purchaseOrder = initialAp.PurchaseOrderId.HasValue
            ? await LockPurchaseOrderAsync(initialAp.PurchaseOrderId.Value)
            : null;
        var ap = await LockAccountsPayableAsync(req.AccountsPayableId);
        if (ap is null)
            return (false, "not_found");
        if (purchaseOrder is not null &&
            (ap.PurchaseOrderId != purchaseOrder.Id || ap.SupplierId != purchaseOrder.SupplierId))
            return (true, "ap_po_mismatch");

        var status = await RecordPaymentWithinTransactionAsync(ap, purchaseOrder, req.PaymentAmount);
        if (status != "ok")
            return (true, status);

        if (transaction is not null)
            await transaction.CommitAsync();
        return (true, "ok");
    }

    private async Task<string> RecordPaymentWithinTransactionAsync(
        AccountsPayable ap,
        PurchaseOrder? purchaseOrder,
        decimal paymentAmount)
    {
        if (ap.TotalAmount <= 0)
            return "invalid_ap_amount";

        var paymentsForAp = await GetPaymentAmountForApAsync(ap.Id);
        var legacyPaymentReservation = 0m;
        if (purchaseOrder is not null)
        {
            var legacyBalance = await GetUnlinkedLegacyPaymentAmountAsync(
                purchaseOrder.Id,
                purchaseOrder.ReceivedAmount ?? 0m);
            if (legacyBalance.apCount > 1 && legacyBalance.amount > 0)
                return "unallocated_legacy_balance";
            legacyPaymentReservation = legacyBalance.amount;
        }
        var debitNoteAmount = await GetActiveDebitNoteAmountAsync(ap.Id);
        var outstanding = ap.TotalAmount - paymentsForAp - legacyPaymentReservation - debitNoteAmount;
        if (paymentAmount > outstanding)
            return "exceeds_outstanding";

        var now = DateTime.UtcNow;
        var batch = new PaymentBatch
        {
            BatchNumber = DocumentNumberGenerator.Generate("PAY"),
            PaymentDate = now,
            PaymentMethod = "wire",
            TotalAmount = paymentAmount,
            Status = "posted",
            CreatedAt = now
        };
        batch.Records.Add(new PaymentRecord
        {
            AccountsPayableId = ap.Id,
            PaidAmount = paymentAmount
        });
        _db.PaymentBatches.Add(batch);

        var totalPaidForAp = paymentsForAp + paymentAmount + legacyPaymentReservation;
        var balanceAfterPayment = ap.TotalAmount - totalPaidForAp - debitNoteAmount;
        ap.PaymentStatus = balanceAfterPayment <= 0 ? "paid" : "partial";
        if (purchaseOrder is not null)
        {
            var activePaymentsForPurchaseOrder =
                await GetActivePaymentAmountForPurchaseOrderAsync(purchaseOrder.Id);
            purchaseOrder.ReceivedAmount = legacyPaymentReservation +
                                           activePaymentsForPurchaseOrder +
                                           paymentAmount;
            if (purchaseOrder.ReceivedAmount >= purchaseOrder.TotalAmount)
                purchaseOrder.Status = "paid";
        }

        await _db.SaveChangesAsync();
        return "ok";
    }

    private async Task<decimal> GetPaymentAmountForApAsync(Guid apId)
    {
        var amounts = await (
            from record in _db.PaymentRecords
            join batch in _db.PaymentBatches on record.PaymentBatchId equals batch.Id
            where record.AccountsPayableId == apId &&
                  batch.Status.ToLower() == "posted"
            select new { record.PaidAmount, record.DiscountAmount }
        ).ToListAsync();
        return amounts.Sum(item => item.PaidAmount + item.DiscountAmount);
    }

    private async Task<(int apCount, decimal amount)> GetUnlinkedLegacyPaymentAmountAsync(
        Guid purchaseOrderId,
        decimal legacyPaidAmount)
    {
        var apIds = await _db.AccountsPayables
            .Where(ap => ap.PurchaseOrderId == purchaseOrderId)
            .Select(ap => ap.Id)
            .ToListAsync();
        if (apIds.Count == 0)
            return (0, legacyPaidAmount);

        var amounts = await (
            from record in _db.PaymentRecords
            join batch in _db.PaymentBatches on record.PaymentBatchId equals batch.Id
            where apIds.Contains(record.AccountsPayableId)
            select new { record.PaidAmount, record.DiscountAmount }
        )
            .ToListAsync();
        var recordedPayments = amounts.Sum(item => item.PaidAmount + item.DiscountAmount);
        return (apIds.Count, Math.Max(0m, legacyPaidAmount - recordedPayments));
    }

    private async Task<decimal> GetActivePaymentAmountForPurchaseOrderAsync(Guid purchaseOrderId)
    {
        var amounts = await (
            from record in _db.PaymentRecords
            join batch in _db.PaymentBatches on record.PaymentBatchId equals batch.Id
            join ap in _db.AccountsPayables on record.AccountsPayableId equals ap.Id
            where ap.PurchaseOrderId == purchaseOrderId &&
                  batch.Status.ToLower() == "posted"
            select new { record.PaidAmount, record.DiscountAmount }
        ).ToListAsync();
        return amounts.Sum(item => item.PaidAmount + item.DiscountAmount);
    }

    private async Task<decimal> GetActiveDebitNoteAmountAsync(Guid apId)
    {
        var notes = await _db.SupplierDebitNotes
            .Where(note => note.AccountsPayableId == apId &&
                           note.Status.ToLower() == "posted")
            .Select(note => new { note.Id, note.TotalAmount })
            .ToListAsync();
        if (notes.Count == 0)
            return 0m;

        var noteIds = notes.Select(note => note.Id).ToList();
        var postedVouchers = await _db.AccountingVouchers
            .Where(voucher => voucher.ReferenceType == "SupplierDebitNote" &&
                              voucher.ReferenceId.HasValue &&
                              noteIds.Contains(voucher.ReferenceId.Value) &&
                              voucher.PostingPurpose == "supplier_debit_note" &&
                              voucher.Status.ToLower() == "posted")
            .Select(voucher => new { voucher.Id, ReferenceId = voucher.ReferenceId!.Value })
            .ToListAsync();
        if (postedVouchers.Count == 0)
            return 0m;

        var voucherIds = postedVouchers.Select(voucher => voucher.Id).ToList();
        var reversedVoucherIds = await _db.AccountingVouchers
            .Where(voucher => voucher.ReversalOfVoucherId.HasValue &&
                              voucherIds.Contains(voucher.ReversalOfVoucherId.Value) &&
                              voucher.Status.ToLower() == "posted")
            .Select(voucher => voucher.ReversalOfVoucherId!.Value)
            .ToListAsync();
        var postedNoteIds = postedVouchers
            .Select(voucher => voucher.ReferenceId)
            .ToHashSet();
        var reversedNoteIds = postedVouchers
            .Where(voucher => reversedVoucherIds.Contains(voucher.Id))
            .Select(voucher => voucher.ReferenceId)
            .ToHashSet();
        return notes
            .Where(note => postedNoteIds.Contains(note.Id) && !reversedNoteIds.Contains(note.Id))
            .Sum(note => note.TotalAmount);
    }

    private async Task<PurchaseOrder?> LockPurchaseOrderAsync(Guid purchaseOrderId)
    {
        if (_db.Database.IsRelational() &&
            _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var purchaseOrder = await _db.PurchaseOrders
                .FromSqlInterpolated($"SELECT * FROM \"PurchaseOrders\" WHERE \"Id\" = {purchaseOrderId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (purchaseOrder is not null)
                await _db.Entry(purchaseOrder).ReloadAsync();
            return purchaseOrder;
        }

        return await _db.PurchaseOrders.SingleOrDefaultAsync(po => po.Id == purchaseOrderId);
    }

    private async Task<AccountsPayable?> LockAccountsPayableAsync(Guid apId)
    {
        if (_db.Database.IsRelational() &&
            _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var ap = await _db.AccountsPayables
                .FromSqlInterpolated($"SELECT * FROM \"AccountsPayables\" WHERE \"Id\" = {apId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (ap is not null)
                await _db.Entry(ap).ReloadAsync();
            return ap;
        }

        return await _db.AccountsPayables.SingleOrDefaultAsync(item => item.Id == apId);
    }

    private static bool IsValidPaymentAmount(decimal amount) =>
        amount > 0 && decimal.Round(amount, 2, MidpointRounding.AwayFromZero) == amount;
}
