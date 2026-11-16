using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Sales;
using ErpBackend.Services;

namespace ErpBackend.IntegrationTests;

public class SalesCreditNoteIntegrationTests
{
    [Fact]
    public async Task VersionedEndpoint_RequiresAuthentication()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");

        var response = await client.GetAsync($"/api/v1/erp/sales/credit-notes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateAndPost_PaidInvoiceCreditsExcessToCustomerAdvances()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var (invoice, delivery) = SeedInvoice(db, paidAmount: 105m);
        SeedAccountingMappings(db);
        var service = CreateService(db);
        var creator = Guid.NewGuid();
        var request = new CreateSalesCreditNoteRequest
        {
            SalesInvoiceId = invoice.Id,
            DeliveryOrderId = delivery.Id,
            NetAmount = 100m,
            Reason = "價格調整"
        };

        var note = await service.CreateAsync(request, "paid-invoice-credit-001", creator, default);
        Assert.Equal(5m, note.TaxAmount);
        Assert.Equal(105m, note.TotalAmount);
        Assert.Equal("PendingApproval", note.Status);

        var retried = await service.CreateAsync(request, "paid-invoice-credit-001", creator, default);
        Assert.Equal(note.Id, retried.Id);

        db.ChangeTracker.Clear();
        var approved = await service.ApproveAsync(
            note.Id,
            new ApproveSalesCreditNoteRequest("DepartmentManager", "核准"),
            Guid.NewGuid(),
            ["DepartmentManager"],
            default);
        Assert.Equal("Approved", approved.Status);

        var posted = await service.PostAsync(
            note.Id,
            Guid.NewGuid(),
            ["Finance"],
            default);
        Assert.Equal("Posted", posted.Status);
        Assert.Equal(0m, posted.ReceivableAmount);
        Assert.Equal(105m, posted.CustomerAdvanceAmount);

        var voucher = await db.AccountingVouchers
            .Include(item => item.Entries)
            .SingleAsync(item => item.ReferenceType == "SalesCreditNote" && item.ReferenceId == note.Id);
        var mappingIds = await db.GLAccountMappings.ToDictionaryAsync(mapping => mapping.MappingKey, mapping => mapping.GLAccountId);
        Assert.Equal(100m, voucher.Entries.Single(entry => entry.GLAccountId == mappingIds["sales_discount"]).DebitAmount);
        Assert.Equal(5m, voucher.Entries.Single(entry => entry.GLAccountId == mappingIds["output_tax"]).DebitAmount);
        Assert.Equal(105m, voucher.Entries.Single(entry => entry.GLAccountId == mappingIds["customer_advances"]).CreditAmount);
        Assert.DoesNotContain(voucher.Entries, entry => entry.GLAccountId == mappingIds["accounts_receivable"]);
        Assert.Equal(0m, (await db.SalesInvoices.SingleAsync(item => item.Id == invoice.Id)).DiscountedAmount);
        var postedNote = await db.SalesCreditNotes.SingleAsync(item => item.Id == note.Id);
        postedNote.Reason = "不應允許修改";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task PartiallyPaidInvoice_CreditNoteSplitsRemainingArAndCustomerAdvance()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var (invoice, delivery) = SeedInvoice(db, paidAmount: 80m);
        SeedAccountingMappings(db);
        var service = CreateService(db);
        var note = await service.CreateAsync(
            new CreateSalesCreditNoteRequest
            {
                SalesInvoiceId = invoice.Id,
                DeliveryOrderId = delivery.Id,
                NetAmount = 100m,
                Reason = "部分收款折讓"
            },
            "partial-paid-credit-001",
            Guid.NewGuid(),
            default);
        db.ChangeTracker.Clear();

        var approved = await service.ApproveAsync(
            note.Id,
            new ApproveSalesCreditNoteRequest("DepartmentManager", null),
            Guid.NewGuid(),
            ["DepartmentManager"],
            default);
        Assert.Equal("Approved", approved.Status);
        db.ChangeTracker.Clear();

        var posted = await service.PostAsync(note.Id, Guid.NewGuid(), ["Finance"], default);

        Assert.Equal(25m, posted.ReceivableAmount);
        Assert.Equal(80m, posted.CustomerAdvanceAmount);
        Assert.Equal(25m, (await db.SalesInvoices.SingleAsync(item => item.Id == invoice.Id)).DiscountedAmount);
    }

    [Fact]
    public async Task PostingIntoClosedPeriod_RollsBackVoucherAndInvoiceBalance()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var (invoice, delivery) = SeedInvoice(db, paidAmount: 0m);
        SeedAccountingMappings(db);
        var service = CreateService(db);
        var note = await service.CreateAsync(
            new CreateSalesCreditNoteRequest
            {
                SalesInvoiceId = invoice.Id,
                DeliveryOrderId = delivery.Id,
                NetAmount = 40m,
                Reason = "關帳期間測試"
            },
            "closed-period-credit-001",
            Guid.NewGuid(),
            default);
        db.ChangeTracker.Clear();
        await service.ApproveAsync(
            note.Id,
            new ApproveSalesCreditNoteRequest("DepartmentManager", null),
            Guid.NewGuid(),
            ["DepartmentManager"],
            default);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.AccountingPeriods.Add(new AccountingPeriod
        {
            PeriodCode = $"CLOSED-{Guid.NewGuid():N}"[..18],
            StartDate = today,
            EndDate = today,
            IsClosed = true
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PostAsync(note.Id, Guid.NewGuid(), ["Finance"], default));

        db.ChangeTracker.Clear();
        Assert.Equal("Approved", (await db.SalesCreditNotes.SingleAsync(item => item.Id == note.Id)).Status);
        Assert.Equal(0, await db.AccountingVouchers.CountAsync(
            item => item.ReferenceType == "SalesCreditNote" && item.ReferenceId == note.Id));
        Assert.Equal(0m, (await db.SalesInvoices.SingleAsync(item => item.Id == invoice.Id)).DiscountedAmount);
    }

    [Fact]
    public async Task LargeCreditNote_RequiresDistinctCfoAndGeneralManagerApprovals()
    {
        using var factory = new CustomWebApplicationFactory();
        using var db = factory.CreateDbContext();
        var (invoice, delivery) = SeedInvoice(db, paidAmount: 0m, exchangeRate: 1_000m);
        var service = CreateService(db);
        var creator = Guid.NewGuid();
        var note = await service.CreateAsync(
            new CreateSalesCreditNoteRequest
            {
                SalesInvoiceId = invoice.Id,
                DeliveryOrderId = delivery.Id,
                NetAmount = 60m,
                Reason = "超過門檻測試"
            },
            "large-credit-001",
            creator,
            default);

        var cfoId = Guid.NewGuid();
        var awaitingSecondApproval = await service.ApproveAsync(
            note.Id,
            new ApproveSalesCreditNoteRequest("CFO", null),
            cfoId,
            ["CFO"],
            default);
        Assert.Equal("PendingApproval", awaitingSecondApproval.Status);

        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(
            note.Id,
            new ApproveSalesCreditNoteRequest("GeneralManager", null),
            cfoId,
            ["GeneralManager"],
            default));

        db.ChangeTracker.Clear();
        var approved = await service.ApproveAsync(
            note.Id,
            new ApproveSalesCreditNoteRequest("GeneralManager", null),
            Guid.NewGuid(),
            ["GeneralManager"],
            default);
        Assert.Equal("Approved", approved.Status);
    }

    private static SalesCreditNoteService CreateService(ErpGeneralDbContext db) =>
        new(db, new AccountingService(db), Options.Create(new AccountingPolicyOptions()));

    private static (SalesInvoice Invoice, DeliveryOrder Delivery) SeedInvoice(
        ErpGeneralDbContext db,
        decimal paidAmount,
        decimal exchangeRate = 1m)
    {
        var customer = new Customer { CustomerCode = $"C{Guid.NewGuid():N}"[..10], CustomerName = "折讓測試客戶" };
        var order = new SalesOrder
        {
            OrderNumber = $"SO{Guid.NewGuid():N}"[..12],
            Customer = customer,
            OrderDate = DateTime.UtcNow
        };
        var delivery = new DeliveryOrder
        {
            DeliveryNumber = $"DO{Guid.NewGuid():N}"[..12],
            SalesOrder = order,
            DeliveryDate = DateTime.UtcNow,
            Status = "completed"
        };
        var invoice = new SalesInvoice
        {
            InvoiceNumber = $"INV{Guid.NewGuid():N}"[..14],
            InvoiceDate = DateTime.UtcNow,
            Customer = customer,
            SalesOrderId = order.Id,
            DeliveryOrderId = delivery.Id,
            Currency = "USD",
            ExchangeRate = exchangeRate,
            SalesAmount = 100m,
            TaxAmount = 5m,
            TotalAmount = 105m,
            BaseSalesAmount = 100m * exchangeRate,
            BaseTaxAmount = 5m * exchangeRate,
            BaseTotalAmount = 105m * exchangeRate,
            PaidAmount = paidAmount,
            Status = "approved"
        };
        db.AddRange(customer, order, delivery, invoice);
        db.SaveChanges();
        return (invoice, delivery);
    }

    private static void SeedAccountingMappings(ErpGeneralDbContext db)
    {
        var mappings = new[]
        {
            ("sales_discount", "5101", "銷貨折讓", "Revenue"),
            ("accounts_receivable", "1131", "應收帳款", "Asset"),
            ("output_tax", "2201", "銷項稅額", "Liability"),
            ("customer_advances", "2141", "客戶預收款", "Liability")
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
