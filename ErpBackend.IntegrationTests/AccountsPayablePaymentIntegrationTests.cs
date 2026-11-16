using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;

namespace ErpBackend.IntegrationTests;

public class AccountsPayablePaymentIntegrationTests
{
    [Fact]
    public async Task RecordPayment_RespectsPostedDebitNoteAndUpdatesApLedger()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedAp(db);
        SeedDebitNote(db, source.Ap.Id, 30m);
        var service = new AccountsPayableService(db);

        var result = await service.RecordAccountsPayablePaymentAsync(
            new RecordAccountsPayablePaymentRequest(source.Ap.Id, 70m));

        Assert.True(result.found);
        Assert.Equal("ok", result.status);
        Assert.Equal(70m, source.PurchaseOrder.ReceivedAmount);
        Assert.Equal("paid", source.Ap.PaymentStatus);
        Assert.Equal(1, await db.PaymentBatches.CountAsync());
        Assert.Equal(1, await db.PaymentRecords.CountAsync(item => item.AccountsPayableId == source.Ap.Id));
    }

    [Fact]
    public async Task RecordPayment_RejectsPaymentThatExceedsOutstandingAfterPostedDebitNote()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedAp(db);
        SeedDebitNote(db, source.Ap.Id, 30m);
        var service = new AccountsPayableService(db);

        var result = await service.RecordAccountsPayablePaymentAsync(
            new RecordAccountsPayablePaymentRequest(source.Ap.Id, 70.01m));

        Assert.True(result.found);
        Assert.Equal("exceeds_outstanding", result.status);
        Assert.Equal(0m, source.PurchaseOrder.ReceivedAmount);
        Assert.Empty(await db.PaymentRecords.ToListAsync());
    }

    [Fact]
    public async Task RecordPayment_ExcludesCancelledBatchAndAllowsRemainingPayment()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedAp(db);
        AddPayment(db, source.Ap.Id, source.PurchaseOrder.Id, 100m, "cancelled");
        var service = new AccountsPayableService(db);

        var result = await service.RecordAccountsPayablePaymentAsync(
            new RecordAccountsPayablePaymentRequest(source.Ap.Id, 100m));

        Assert.Equal("ok", result.status);
        Assert.Equal(100m, source.PurchaseOrder.ReceivedAmount);
    }

    [Fact]
    public async Task ApQueries_UsePostedPaymentsAndActiveDebitNotes()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedAp(db);
        SeedDebitNote(db, source.Ap.Id, 20m);
        AddPayment(db, source.Ap.Id, source.PurchaseOrder.Id, 10m, "posted");
        AddPayment(db, source.Ap.Id, source.PurchaseOrder.Id, 50m, "pending");
        var service = new AccountsPayableService(db);

        var summary = await service.GetApSummaryAsync();
        var detail = Assert.Single(await service.GetApBySupplierAsync(source.Supplier.Id));
        var aging = Assert.Single(await service.GetAgingReportAsync());

        Assert.Equal(70m, Assert.Single(summary).TotalPayable);
        Assert.Equal(10m, detail.PaidAmount);
        Assert.Equal(20m, detail.DebitNoteAmount);
        Assert.Equal(70m, detail.Outstanding);
        Assert.Equal(70m, aging.Current);
    }

    [Fact]
    public async Task RecordPayment_DoesNotReserveFullyReversedDebitNote()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedAp(db);
        var originalVoucherId = SeedDebitNote(db, source.Ap.Id, 30m);
        db.AccountingVouchers.Add(new AccountingVoucher
        {
            VoucherNumber = $"VR-{Guid.NewGuid():N}"[..20],
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            ReferenceType = "AccountingVoucherReversal",
            ReferenceId = originalVoucherId,
            PostingPurpose = "reversal",
            ReversalOfVoucherId = originalVoucherId,
            Status = "Posted",
            PostedAt = DateTime.UtcNow,
            Entries =
            {
                new AccountingEntry
                {
                    GLAccountId = Guid.NewGuid(),
                    CreditAmount = 30m
                },
                new AccountingEntry
                {
                    GLAccountId = Guid.NewGuid(),
                    DebitAmount = 30m
                }
            }
        });
        await db.SaveChangesAsync();
        var service = new AccountsPayableService(db);

        var result = await service.RecordAccountsPayablePaymentAsync(
            new RecordAccountsPayablePaymentRequest(source.Ap.Id, 100m));

        Assert.Equal("ok", result.status);
    }

    [Fact]
    public async Task LegacyPoEndpoint_MapsOnlyWhenExactlyOneApExists()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedAp(db);
        var service = new AccountsPayableService(db);

        var result = await service.RecordPaymentAsync(
            new RecordApPaymentRequest { PurchaseOrderId = source.PurchaseOrder.Id, PaymentAmount = 25m });

        Assert.Equal("ok", result.status);
        Assert.Equal(25m, source.PurchaseOrder.ReceivedAmount);
        Assert.Equal(source.Ap.Id, (await db.PaymentRecords.SingleAsync()).AccountsPayableId);
    }

    [Fact]
    public async Task LegacyPoEndpoint_RejectsAmbiguousApWithoutRecordingPayment()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedAp(db);
        db.AccountsPayables.Add(new AccountsPayable
        {
            ApNumber = $"AP-{Guid.NewGuid():N}"[..20],
            SupplierId = source.Supplier.Id,
            PurchaseOrderId = source.PurchaseOrder.Id,
            Currency = "TWD",
            ExchangeRate = 1m,
            InvoiceAmount = 50m,
            TaxAmount = 0m,
            TotalAmount = 50m,
            BaseInvoiceAmount = 50m,
            BaseTaxAmount = 0m,
            BaseTotalAmount = 50m
        });
        await db.SaveChangesAsync();
        var service = new AccountsPayableService(db);

        var result = await service.RecordPaymentAsync(
            new RecordApPaymentRequest { PurchaseOrderId = source.PurchaseOrder.Id, PaymentAmount = 25m });

        Assert.Equal("ambiguous_ap", result.status);
        Assert.Equal(0m, source.PurchaseOrder.ReceivedAmount);
        Assert.Empty(await db.PaymentRecords.ToListAsync());
    }

    [Fact]
    public async Task AccountsPayablePaymentEndpoint_AllowsExplicitApWhenPurchaseOrderHasMultipleApRecords()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedAp(db);
        var secondAp = new AccountsPayable
        {
            ApNumber = $"AP-{Guid.NewGuid():N}"[..20],
            SupplierId = source.Supplier.Id,
            PurchaseOrderId = source.PurchaseOrder.Id,
            Currency = "TWD",
            ExchangeRate = 1m,
            InvoiceAmount = 50m,
            TaxAmount = 0m,
            TotalAmount = 50m,
            BaseInvoiceAmount = 50m,
            BaseTaxAmount = 0m,
            BaseTotalAmount = 50m
        };
        db.AccountsPayables.Add(secondAp);
        await db.SaveChangesAsync();
        var service = new AccountsPayableService(db);

        var result = await service.RecordAccountsPayablePaymentAsync(
            new RecordAccountsPayablePaymentRequest(secondAp.Id, 50m));

        Assert.Equal("ok", result.status);
        Assert.Equal(secondAp.Id, (await db.PaymentRecords.SingleAsync()).AccountsPayableId);
    }

    [Fact]
    public async Task AccountsPayablePaymentEndpoint_RequiresReconciliationForUnallocatedLegacyPayments()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedAp(db);
        source.PurchaseOrder.ReceivedAmount = 25m;
        db.AccountsPayables.Add(new AccountsPayable
        {
            ApNumber = $"AP-{Guid.NewGuid():N}"[..20],
            SupplierId = source.Supplier.Id,
            PurchaseOrderId = source.PurchaseOrder.Id,
            Currency = "TWD",
            ExchangeRate = 1m,
            InvoiceAmount = 50m,
            TaxAmount = 0m,
            TotalAmount = 50m,
            BaseInvoiceAmount = 50m,
            BaseTaxAmount = 0m,
            BaseTotalAmount = 50m
        });
        await db.SaveChangesAsync();
        var service = new AccountsPayableService(db);

        var result = await service.RecordAccountsPayablePaymentAsync(
            new RecordAccountsPayablePaymentRequest(source.Ap.Id, 10m));

        Assert.Equal("unallocated_legacy_balance", result.status);
        Assert.Empty(await db.PaymentRecords.ToListAsync());
    }

    [Fact]
    public async Task AccountsPayablePaymentEndpoint_RecordsPaymentAgainstRequestedAp()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedAp(db);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/erp/accounts-payable/payment/accounts-payable",
            new RecordAccountsPayablePaymentRequest(source.Ap.Id, 35m));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(await db.PaymentRecords
            .Where(item => item.AccountsPayableId == source.Ap.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task AccountsPayablePaymentEndpoint_ReturnsConflictForAmountAboveNetPayable()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedAp(db);
        SeedDebitNote(db, source.Ap.Id, 30m);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/erp/accounts-payable/payment/accounts-payable",
            new RecordAccountsPayablePaymentRequest(source.Ap.Id, 70.01m));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Empty(await db.PaymentRecords.ToListAsync());
    }

    private static (Supplier Supplier, PurchaseOrder PurchaseOrder, AccountsPayable Ap) SeedAp(
        ErpGeneralDbContext db)
    {
        var supplier = new Supplier
        {
            SupplierCode = $"SUP-{Guid.NewGuid():N}"[..14],
            SupplierName = "付款測試供應商",
            Currency = "TWD"
        };
        var purchaseOrder = new PurchaseOrder
        {
            PoNumber = $"PO-{Guid.NewGuid():N}"[..20],
            Supplier = supplier,
            PoDate = DateTime.UtcNow,
            Currency = "TWD",
            ExchangeRate = 1m,
            TotalAmount = 100m,
            Status = "confirmed",
            ReceivedAmount = 0m
        };
        var ap = new AccountsPayable
        {
            ApNumber = $"AP-{Guid.NewGuid():N}"[..20],
            SupplierId = supplier.Id,
            PurchaseOrderId = purchaseOrder.Id,
            Currency = "TWD",
            ExchangeRate = 1m,
            InvoiceAmount = 100m,
            TaxAmount = 0m,
            TotalAmount = 100m,
            BaseInvoiceAmount = 100m,
            BaseTaxAmount = 0m,
            BaseTotalAmount = 100m,
            PaymentStatus = "unpaid"
        };
        db.AddRange(supplier, purchaseOrder, ap);
        db.SaveChanges();
        return (supplier, purchaseOrder, ap);
    }

    private static Guid SeedDebitNote(ErpGeneralDbContext db, Guid apId, decimal amount)
    {
        var note = new SupplierDebitNote
        {
            DebitNoteNumber = $"SDN-{Guid.NewGuid():N}"[..20],
            IdempotencyKey = $"seed-{Guid.NewGuid():N}",
            AccountsPayableId = apId,
            GoodsReceiptId = Guid.NewGuid(),
            GoodsReceiptItemId = Guid.NewGuid(),
            IqcReportId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            SkuId = Guid.NewGuid(),
            LotNumber = "LOT-TEST",
            ReturnedQty = 1m,
            NetAmount = amount,
            TotalAmount = amount,
            BaseNetAmount = amount,
            BaseTotalAmount = amount,
            Currency = "TWD",
            ExchangeRate = 1m,
            Reason = "已過帳借項測試",
            Status = "PendingApproval",
            CreatedBy = Guid.NewGuid(),
            PostedBy = Guid.NewGuid(),
            PostedAt = DateTime.UtcNow
        };
        db.SupplierDebitNotes.Add(note);
        db.SaveChanges();
        db.Database.ExecuteSqlInterpolated(
            $"UPDATE \"SupplierDebitNotes\" SET \"Status\" = 'Posted' WHERE \"Id\" = {note.Id}");
        var voucher = new AccountingVoucher
        {
            VoucherNumber = $"V-{Guid.NewGuid():N}"[..20],
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            ReferenceType = "SupplierDebitNote",
            ReferenceId = note.Id,
            PostingPurpose = "supplier_debit_note",
            Status = "Posted",
            PostedAt = DateTime.UtcNow,
            Entries =
            {
                new AccountingEntry
                {
                    GLAccountId = Guid.NewGuid(),
                    DebitAmount = amount
                },
                new AccountingEntry
                {
                    GLAccountId = Guid.NewGuid(),
                    CreditAmount = amount
                }
            }
        };
        db.AccountingVouchers.Add(voucher);
        db.SaveChanges();
        return voucher.Id;
    }

    private static void AddPayment(
        ErpGeneralDbContext db,
        Guid apId,
        Guid poId,
        decimal amount,
        string status)
    {
        db.PaymentBatches.Add(new PaymentBatch
        {
            BatchNumber = $"PAY-{Guid.NewGuid():N}"[..20],
            PaymentDate = DateTime.UtcNow,
            TotalAmount = amount,
            Status = status,
            Records =
            {
                new PaymentRecord
                {
                    AccountsPayableId = apId,
                    PaidAmount = amount
                }
            }
        });
        var purchaseOrder = db.PurchaseOrders.Single(item => item.Id == poId);
        purchaseOrder.ReceivedAmount = (purchaseOrder.ReceivedAmount ?? 0m) + amount;
        db.SaveChanges();
    }
}
