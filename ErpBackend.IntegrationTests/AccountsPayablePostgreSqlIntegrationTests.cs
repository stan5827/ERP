using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.IntegrationTests;

public class AccountsPayablePostgreSqlIntegrationTests
{
    [PostgreSqlFact]
    public async Task ApQueries_UsePostedPaymentsAndDueDateBuckets()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ERP_TEST_POSTGRES_CONNECTION_STRING")!;
        await using var database = await PostgreSqlTestDatabase.CreateAsync(connectionString);
        Guid supplierId;
        Guid primaryApId;
        var today = DateTime.UtcNow.Date;

        await using (var setupDb = new ErpGeneralDbContext(database.Options))
        {
            var supplier = new Supplier
            {
                SupplierCode = $"AP-{Guid.NewGuid():N}"[..20],
                SupplierName = "AP PostgreSQL Integration Supplier",
                Currency = "TWD",
                ApprovalStatus = "APPROVED",
                Status = "ACTIVE"
            };
            var purchaseOrder = new PurchaseOrder
            {
                PoNumber = $"PO-{Guid.NewGuid():N}"[..20],
                Supplier = supplier,
                PoDate = today.AddDays(-50),
                Currency = "TWD",
                ExchangeRate = 1m,
                Subtotal = 200000m,
                TaxAmount = 10000m,
                TotalAmount = 210000m,
                Status = "confirmed",
                ReceivedAmount = 180000m,
                DueDate = today.AddDays(10)
            };
            var payable = CreatePayable(supplier.Id, purchaseOrder.Id, "AP-POSTGRES-001", 105000m);
            payable.SupplierInvoiceDate = today.AddDays(-12);
            payable.DueDate = today.AddDays(-40);
            var secondPayable = CreatePayable(supplier.Id, purchaseOrder.Id, "AP-POSTGRES-002", 500m);
            secondPayable.SupplierInvoiceDate = today.AddDays(-60);
            secondPayable.DueDate = today.AddDays(-100);

            setupDb.AddRange(supplier, purchaseOrder, payable, secondPayable);
            AddPayment(setupDb, payable.Id, today, 20000m, 5000m, "posted");
            AddPayment(setupDb, payable.Id, today, 50000m, 0m, "pending");
            AddPayment(setupDb, payable.Id, today, 10000m, 0m, "cancelled");
            AddPayment(setupDb, payable.Id, today, 3000m, 0m, "void");
            await setupDb.SaveChangesAsync();

            supplierId = supplier.Id;
            primaryApId = payable.Id;
        }

        await using var queryDb = new ErpGeneralDbContext(database.Options);
        var service = new AccountsPayableService(queryDb);

        var summary = await service.GetApSummaryAsync(
            supplierId.ToString(),
            today.AddDays(-20).ToString("yyyy-MM-dd"),
            today.ToString("yyyy-MM-dd"));
        var summaryItem = Assert.Single(summary);
        var detail = Assert.Single(
            await service.GetApBySupplierAsync(supplierId),
            item => item.Id == primaryApId);
        var aging = Assert.Single(await service.GetAgingReportAsync());

        Assert.Equal(80000m, summaryItem.TotalPayable);
        Assert.Equal(1, summaryItem.Count);
        Assert.Equal(20000m, detail.PaidAmount);
        Assert.Equal(5000m, detail.DiscountAmount);
        Assert.Equal(80000m, detail.Outstanding);
        Assert.Equal(500m, aging.Over90);
        Assert.Equal(80000m, aging.Days60);
        Assert.Equal(0m, aging.Current);
        Assert.Equal(0m, aging.Days30);
        Assert.Equal(0m, aging.Days90);
    }

    private static AccountsPayable CreatePayable(
        Guid supplierId,
        Guid purchaseOrderId,
        string apNumber,
        decimal totalAmount) =>
        new()
        {
            ApNumber = apNumber,
            SupplierId = supplierId,
            PurchaseOrderId = purchaseOrderId,
            Currency = "TWD",
            ExchangeRate = 1m,
            InvoiceAmount = totalAmount,
            TaxAmount = 0m,
            TotalAmount = totalAmount,
            BaseInvoiceAmount = totalAmount,
            BaseTaxAmount = 0m,
            BaseTotalAmount = totalAmount
        };

    private static void AddPayment(
        ErpGeneralDbContext db,
        Guid payableId,
        DateTime paymentDate,
        decimal paidAmount,
        decimal discountAmount,
        string status)
    {
        db.PaymentBatches.Add(new PaymentBatch
        {
            BatchNumber = $"PAY-{Guid.NewGuid():N}"[..20],
            PaymentDate = paymentDate,
            PaymentMethod = "wire",
            TotalAmount = paidAmount,
            Status = status,
            Records =
            [
                new PaymentRecord
                {
                    AccountsPayableId = payableId,
                    PaidAmount = paidAmount,
                    DiscountAmount = discountAmount
                }
            ]
        });
    }
}
