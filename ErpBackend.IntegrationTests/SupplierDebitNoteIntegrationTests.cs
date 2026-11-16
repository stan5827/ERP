using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.QA;
using ErpBackend.Services;

namespace ErpBackend.IntegrationTests;

public class SupplierDebitNoteIntegrationTests
{
    [Fact]
    public async Task VersionedEndpoint_RequiresAuthentication()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");

        var response = await client.GetAsync($"/api/v1/erp/procurement/debit-notes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReturnNote_PostingSplitsInventoryAndConsumedCostAndReversesInputTax()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedRejectedLot(db, remainingQty: 4m);
        SeedAccountingMappings(db);
        var service = CreateService(db);
        var note = await service.CreateAsync(
            new CreateSupplierDebitNoteRequest
            {
                AccountsPayableId = source.Ap.Id,
                IqcReportId = source.Iqc.Id,
                ReturnedQty = 2m,
                Reason = "來料不良退供應商"
            },
            "supplier-return-001",
            Guid.NewGuid(),
            default);

        Assert.Equal(20m, note.NetAmount);
        Assert.Equal(2m, note.TaxAmount);
        Assert.Equal(22m, note.TotalAmount);
        Assert.Equal(8m, note.InventoryAllocation);
        Assert.Equal(12m, note.CogsAllocation);
        Assert.Equal(30m, note.BaseNetAmount);
        Assert.Equal(3m, note.BaseTaxAmount);
        Assert.Equal(33m, note.BaseTotalAmount);
        Assert.Equal(12m, note.BaseInventoryAllocation);
        Assert.Equal(18m, note.BaseCogsAllocation);

        db.ChangeTracker.Clear();
        var approved = await service.ApproveAsync(
            note.Id,
            new ApproveSupplierDebitNoteRequest("DepartmentManager", null),
            Guid.NewGuid(),
            ["DepartmentManager"],
            default);
        Assert.Equal("Approved", approved.Status);
        db.ChangeTracker.Clear();

        var posted = await service.PostAsync(note.Id, Guid.NewGuid(), ["Finance"], default);

        Assert.Equal("Posted", posted.Status);
        var voucher = await db.AccountingVouchers.Include(item => item.Entries)
            .SingleAsync(item => item.ReferenceType == "SupplierDebitNote" && item.ReferenceId == note.Id);
        var accounts = await db.GLAccountMappings.ToDictionaryAsync(item => item.MappingKey, item => item.GLAccountId);
        Assert.Equal(33m, voucher.Entries.Single(item => item.GLAccountId == accounts["accounts_payable"]).DebitAmount);
        Assert.Equal(12m, voucher.Entries.Single(item => item.GLAccountId == accounts["purchase_allowance_inventory"]).CreditAmount);
        Assert.Equal(18m, voucher.Entries.Single(item => item.GLAccountId == accounts["purchase_allowance_cogs"]).CreditAmount);
        Assert.Equal(3m, voucher.Entries.Single(item => item.GLAccountId == accounts["input_tax"]).CreditAmount);

        db.ChangeTracker.Clear();
        Assert.Equal(2m, (await db.InventoryLots.SingleAsync(item => item.Id == source.Lot.Id)).Qty);
        var returnTransactionQuantities = await db.InventoryTransactions
            .Where(item => item.RelatedDocType == "SupplierDebitNote" && item.RelatedDocId == note.Id)
            .Select(item => item.QtyChange)
            .ToListAsync();
        Assert.Equal(-2m, returnTransactionQuantities.Sum());
        var repeated = await service.PostAsync(note.Id, Guid.NewGuid(), ["Finance"], default);
        Assert.Equal("Posted", repeated.Status);
        Assert.Equal(1, await db.AccountingVouchers.CountAsync(
            item => item.ReferenceType == "SupplierDebitNote" && item.ReferenceId == note.Id));
    }

    [Fact]
    public async Task Create_RejectsApThatDoesNotMatchTheIqcGoodsReceipt()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedRejectedLot(db, remainingQty: 10m);
        var otherReceipt = new GoodsReceipt
        {
            GrNumber = $"GR{Guid.NewGuid():N}"[..14],
            SupplierId = source.Supplier.Id,
            WarehouseId = source.Warehouse.Id,
            GrDate = DateTime.UtcNow,
            Status = "received",
            IqcStatus = "failed"
        };
        var wrongAp = new AccountsPayable
        {
            ApNumber = $"AP{Guid.NewGuid():N}"[..14],
            SupplierId = source.Supplier.Id,
            GoodsReceiptId = otherReceipt.Id,
            InvoiceAmount = 100m,
            TaxAmount = 5m,
            TotalAmount = 105m
        };
        db.AddRange(otherReceipt, wrongAp);
        db.SaveChanges();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new CreateSupplierDebitNoteRequest
            {
                AccountsPayableId = wrongAp.Id,
                IqcReportId = source.Iqc.Id,
                ReturnedQty = 1m,
                Reason = "不應建立"
            },
            "wrong-gr-ap-001",
            Guid.NewGuid(),
            default));
    }

    [Fact]
    public async Task Create_RejectsReturnQuantityBeyondRejectedAndAvailableBalance()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedRejectedLot(db, remainingQty: 4m);
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new CreateSupplierDebitNoteRequest
            {
                AccountsPayableId = source.Ap.Id,
                IqcReportId = source.Iqc.Id,
                ReturnedQty = 5m,
                Reason = "超量退料"
            },
            "over-return-001",
            Guid.NewGuid(),
            default));
    }

    [Fact]
    public async Task Create_RejectsDebitNoteThatExceedsApOutstandingBalance()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedRejectedLot(db, remainingQty: 10m);
        var batch = new PaymentBatch
        {
            BatchNumber = $"PAY{Guid.NewGuid():N}"[..14],
            PaymentDate = DateTime.UtcNow,
            TotalAmount = 105m,
            Status = "posted"
        };
        db.PaymentRecords.Add(new PaymentRecord
        {
            PaymentBatch = batch,
            AccountsPayableId = source.Ap.Id,
            PaidAmount = 105m
        });
        db.SaveChanges();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new CreateSupplierDebitNoteRequest
            {
                AccountsPayableId = source.Ap.Id,
                IqcReportId = source.Iqc.Id,
                ReturnedQty = 1m,
                Reason = "超過應付帳款餘額"
            },
            "ap-balance-001",
            Guid.NewGuid(),
            default));
    }

    [Fact]
    public async Task Create_IdempotencyRetryReturnsExistingNoteAndRejectsChangedPayload()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedRejectedLot(db, remainingQty: 10m);
        var service = CreateService(db);
        var request = new CreateSupplierDebitNoteRequest
        {
            AccountsPayableId = source.Ap.Id,
            IqcReportId = source.Iqc.Id,
            ReturnedQty = 1m,
            Reason = "重送測試"
        };
        var actorId = Guid.NewGuid();
        var first = await service.CreateAsync(request, "idempotent-return-001", actorId, default);
        var retry = await service.CreateAsync(request, "idempotent-return-001", actorId, default);

        Assert.Equal(first.Id, retry.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            new CreateSupplierDebitNoteRequest
            {
                AccountsPayableId = source.Ap.Id,
                IqcReportId = source.Iqc.Id,
                ReturnedQty = 2m,
                Reason = request.Reason
            },
            "idempotent-return-001",
            actorId,
            default));
    }

    [Fact]
    public async Task Approve_LargeAmountRequiresDistinctCfoAndGeneralManager()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedRejectedLot(db, remainingQty: 10m);
        source.ReceiptItem.LocalCurrencyPrice = 30_000m;
        source.Ap.InvoiceAmount = 100_000m;
        source.Ap.TaxAmount = 10_000m;
        source.Ap.TotalAmount = 110_000m;
        source.Ap.BaseInvoiceAmount = 150_000m;
        source.Ap.BaseTaxAmount = 15_000m;
        source.Ap.BaseTotalAmount = 165_000m;
        db.SaveChanges();

        var service = CreateService(db);
        var note = await service.CreateAsync(
            new CreateSupplierDebitNoteRequest
            {
                AccountsPayableId = source.Ap.Id,
                IqcReportId = source.Iqc.Id,
                ReturnedQty = 2m,
                Reason = "高額簽核測試"
            },
            "large-return-001",
            Guid.NewGuid(),
            default);
        Assert.True(note.BaseTotalAmount > 50_000m);

        var firstApproverId = Guid.NewGuid();
        var pending = await service.ApproveAsync(
            note.Id,
            new ApproveSupplierDebitNoteRequest("CFO", null),
            firstApproverId,
            ["CFO"],
            default);
        Assert.Equal("PendingApproval", pending.Status);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(
            note.Id,
            new ApproveSupplierDebitNoteRequest("GeneralManager", null),
            firstApproverId,
            ["GeneralManager"],
            default));

        var approved = await service.ApproveAsync(
            note.Id,
            new ApproveSupplierDebitNoteRequest("GeneralManager", null),
            Guid.NewGuid(),
            ["GeneralManager"],
            default);
        Assert.Equal("Approved", approved.Status);
    }

    [Fact]
    public async Task Post_RejectsClosedAccountingPeriodWithoutChangingInventory()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var source = SeedRejectedLot(db, remainingQty: 4m);
        SeedAccountingMappings(db);
        var service = CreateService(db);
        var note = await service.CreateAsync(
            new CreateSupplierDebitNoteRequest
            {
                AccountsPayableId = source.Ap.Id,
                IqcReportId = source.Iqc.Id,
                ReturnedQty = 2m,
                Reason = "關帳期間退料測試"
            },
            "closed-period-001",
            Guid.NewGuid(),
            default);
        db.ChangeTracker.Clear();
        await service.ApproveAsync(
            note.Id,
            new ApproveSupplierDebitNoteRequest("DepartmentManager", null),
            Guid.NewGuid(),
            ["DepartmentManager"],
            default);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.AccountingPeriods.Add(new AccountingPeriod
        {
            PeriodCode = $"CLOSED-{Guid.NewGuid():N}"[..20],
            StartDate = today,
            EndDate = today,
            IsClosed = true,
            ClosedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PostAsync(note.Id, Guid.NewGuid(), ["Finance"], default));

        db.ChangeTracker.Clear();
        Assert.Equal(4m, (await db.InventoryLots.SingleAsync(item => item.Id == source.Lot.Id)).Qty);
        Assert.Equal("Approved", (await db.SupplierDebitNotes.SingleAsync(item => item.Id == note.Id)).Status);
        Assert.Empty(await db.AccountingVouchers
            .Where(item => item.ReferenceType == "SupplierDebitNote" && item.ReferenceId == note.Id)
            .ToListAsync());
    }

    private static SupplierDebitNoteService CreateService(ErpGeneralDbContext db) =>
        new(db, new AccountingService(db), Options.Create(new AccountingPolicyOptions()));

    private static (Supplier Supplier, Warehouse Warehouse, BinLocation Bin, Sku Sku,
        GoodsReceipt Receipt, GoodsReceiptItem ReceiptItem, IqcReport Iqc,
        InventoryLot Lot, AccountsPayable Ap) SeedRejectedLot(ErpGeneralDbContext db, decimal remainingQty)
    {
        var supplier = new Supplier
        {
            SupplierCode = $"SUP{Guid.NewGuid():N}"[..12],
            SupplierName = "退料測試供應商"
        };
        var warehouse = new Warehouse
        {
            WarehouseCode = $"WH{Guid.NewGuid():N}"[..12],
            WarehouseName = "IQC隔離倉"
        };
        var bin = new BinLocation
        {
            Warehouse = warehouse,
            LocationCode = $"IQC-{Guid.NewGuid():N}"[..16]
        };
        var sku = new Sku
        {
            SkuNumber = $"SKU{Guid.NewGuid():N}"[..16],
            SkuName = "不良測試物料",
            Qty = remainingQty,
            LotControlled = true
        };
        var receipt = new GoodsReceipt
        {
            GrNumber = $"GR{Guid.NewGuid():N}"[..14],
            Supplier = supplier,
            Warehouse = warehouse,
            GrDate = DateTime.UtcNow,
            Status = "received",
            IqcStatus = "failed"
        };
        var lotNumber = $"LOT{Guid.NewGuid():N}"[..16];
        var receiptItem = new GoodsReceiptItem
        {
            GoodsReceipt = receipt,
            Sku = sku,
            ReceivedQty = 10m,
            InventoryQty = 10m,
            AcceptedQty = 0m,
            RejectedQty = 10m,
            BinLocation = bin,
            LotNumber = lotNumber,
            Currency = "USD",
            ExchangeRate = 1.5m,
            ForeignCurrencyPrice = 10m,
            LocalCurrencyPrice = 15m,
            ForeignCurrencyAmount = 100m,
            LocalCurrencyAmount = 150m,
            IqcResult = "fail"
        };
        receipt.Items.Add(receiptItem);
        var iqc = new IqcReport
        {
            ReportNumber = $"IQC{Guid.NewGuid():N}"[..16],
            GoodsReceipt = receipt,
            GoodsReceiptItem = receiptItem,
            Supplier = supplier,
            Sku = sku,
            ReceivedQty = 10m,
            InspectionDate = DateTime.UtcNow,
            Result = "fail",
            RejectNumber = 1,
            LotNumber = lotNumber
        };
        var lot = new InventoryLot
        {
            Sku = sku,
            LotNumber = lotNumber,
            Qty = remainingQty,
            WarehouseId = warehouse.Id,
            BinLocationId = bin.Id,
            Status = "rejected"
        };
        var ap = new AccountsPayable
        {
            ApNumber = $"AP{Guid.NewGuid():N}"[..14],
            SupplierId = supplier.Id,
            GoodsReceiptId = receipt.Id,
            Currency = "USD",
            ExchangeRate = 1.5m,
            InvoiceAmount = 100m,
            TaxAmount = 10m,
            TotalAmount = 110m,
            BaseInvoiceAmount = 150m,
            BaseTaxAmount = 15m,
            BaseTotalAmount = 165m,
            VerificationStatus = "approved"
        };

        db.AddRange(supplier, warehouse, bin, sku, receipt, iqc, lot, ap);
        db.InventoryTransactions.AddRange(
            new InventoryTransaction
            {
                TrxId = $"GR-{Guid.NewGuid():N}"[..30],
                Sku = sku,
                Type = "purchase_receipt",
                QtyChange = 10m,
                QtyBefore = 0m,
                QtyAfter = 10m,
                LotNumber = lotNumber,
                RelatedDocType = "GoodsReceipt",
                RelatedDocId = receipt.Id,
                WarehouseId = warehouse.Id,
                BinLocationId = bin.Id,
                UnitCost = 15m,
                TotalCost = 150m
            },
            new InventoryTransaction
            {
                TrxId = $"ISS-{Guid.NewGuid():N}"[..30],
                Sku = sku,
                Type = "material_issue",
                QtyChange = remainingQty - 10m,
                QtyBefore = 10m,
                QtyAfter = remainingQty,
                LotNumber = lotNumber,
                WarehouseId = warehouse.Id,
                BinLocationId = bin.Id,
                UnitCost = 15m,
                TotalCost = (remainingQty - 10m) * 15m
            });
        db.SaveChanges();
        return (supplier, warehouse, bin, sku, receipt, receiptItem, iqc, lot, ap);
    }

    private static void SeedAccountingMappings(ErpGeneralDbContext db)
    {
        var mappings = new[]
        {
            ("accounts_payable", "2101", "應付帳款", "Liability"),
            ("input_tax", "1202", "進項稅額", "Asset"),
            ("purchase_allowance_inventory", "1201", "採購折讓-存貨", "Asset"),
            ("purchase_allowance_cogs", "5101", "採購折讓-銷貨成本", "Expense")
        };
        foreach (var (key, code, name, type) in mappings)
        {
            var account = new GLAccount { AccountCode = code, AccountName = name, AccountType = type };
            db.GLAccounts.Add(account);
            db.GLAccountMappings.Add(new GLAccountMapping { MappingKey = key, GLAccount = account });
        }
        db.SaveChanges();
    }
}
