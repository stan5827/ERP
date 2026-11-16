using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Procurement;

namespace ErpBackend.IntegrationTests;

public class AccountsPayableControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();
    private Guid _supplierId;
    private Guid _poId;

    public AccountsPayableControllerIntegrationTests()
    {
        SeedApData();
    }

    private void SeedApData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.SupplierDebitNoteApprovals.RemoveRange(db.SupplierDebitNoteApprovals);
        db.SupplierDebitNotes.RemoveRange(db.SupplierDebitNotes);
        db.PaymentRecords.RemoveRange(db.PaymentRecords);
        db.PaymentBatches.RemoveRange(db.PaymentBatches);
        db.AccountsPayables.RemoveRange(db.AccountsPayables);
        db.PurchaseOrderItems.RemoveRange(db.PurchaseOrderItems);
        db.PurchaseOrders.RemoveRange(db.PurchaseOrders);
        db.Suppliers.RemoveRange(db.Suppliers);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var supplier = new Supplier
        {
            SupplierCode = "SUP-001",
            SupplierName = "測試供應商",
            Currency = "TWD",
            Status = "ACTIVE",
            ApprovalStatus = "APPROVED",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Suppliers.Add(supplier);
        db.SaveChanges();
        _supplierId = supplier.Id;

        var po = new PurchaseOrder
        {
            PoNumber = "PO-2026-001",
            SupplierId = supplier.Id,
            PoDate = DateTime.UtcNow.AddDays(-30),
            Currency = "TWD",
            ExchangeRate = 1,
            Subtotal = 100000,
            TaxAmount = 5000,
            TotalAmount = 105000,
            Status = "confirmed",
            ReceivedAmount = 0,
            DueDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.PurchaseOrders.Add(po);
        db.SaveChanges();
        _poId = po.Id;
        db.AccountsPayables.Add(new AccountsPayable
        {
            ApNumber = "AP-2026-001",
            SupplierId = supplier.Id,
            PurchaseOrderId = po.Id,
            Currency = "TWD",
            ExchangeRate = 1m,
            InvoiceAmount = 100000m,
            TaxAmount = 5000m,
            TotalAmount = 105000m,
            BaseInvoiceAmount = 100000m,
            BaseTaxAmount = 5000m,
            BaseTotalAmount = 105000m
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GetApSummary_ReturnsSupplierSummary()
    {
        var response = await _client.GetAsync("/api/erp/accounts-payable");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Single(data.EnumerateArray());
        Assert.Equal("SUP-001", data[0].GetProperty("supplierCode").GetString());
        Assert.Equal(105000m, data[0].GetProperty("totalPayable").GetDecimal());
    }

    [Fact]
    public async Task GetAgingReport_ReturnsAgingData()
    {
        var response = await _client.GetAsync("/api/erp/accounts-payable/aging");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Single(data.EnumerateArray());
    }

    [Fact]
    public async Task GetApBySupplier_ReturnsPoDetails()
    {
        var response = await _client.GetAsync($"/api/erp/accounts-payable/by-supplier/{_supplierId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Single(data.EnumerateArray());
        Assert.Equal("PO-2026-001", data[0].GetProperty("poNumber").GetString());
    }

    [Fact]
    public async Task ApQueries_UsePostedPaymentsAndApDueDate()
    {
        using (var db = _factory.CreateDbContext())
        {
            var ap = await db.AccountsPayables.SingleAsync();
            var purchaseOrder = await db.PurchaseOrders.SingleAsync();
            var today = DateTime.UtcNow.Date;
            ap.SupplierInvoiceDate = today.AddDays(-12);
            ap.DueDate = today.AddDays(-40);
            purchaseOrder.ReceivedAmount = 90000m;

            var batchStatuses = new[] { "posted", "pending", "cancelled", "void" };
            var amounts = new[] { 20000m, 50000m, 10000m, 3000m };
            for (var index = 0; index < batchStatuses.Length; index++)
            {
                var batch = new PaymentBatch
                {
                    BatchNumber = $"PAY-QUERY-{index}",
                    PaymentDate = today,
                    PaymentMethod = "wire",
                    TotalAmount = amounts[index],
                    Status = batchStatuses[index],
                    Records =
                    [
                        new PaymentRecord
                        {
                            AccountsPayableId = ap.Id,
                            PaidAmount = amounts[index],
                            DiscountAmount = index == 0 ? 5000m : 0m
                        }
                    ]
                };
                db.PaymentBatches.Add(batch);
            }

            await db.SaveChangesAsync();
        }

        var summaryResponse = await _client.GetAsync(
            $"/api/erp/accounts-payable?startDate={DateTime.UtcNow.Date.AddDays(-20):yyyy-MM-dd}&endDate={DateTime.UtcNow.Date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        using var summaryDoc = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        var summary = summaryDoc.RootElement.GetProperty("data")[0];
        Assert.Equal(80000m, summary.GetProperty("totalPayable").GetDecimal());
        Assert.Equal(1, summary.GetProperty("count").GetInt32());

        var detailsResponse = await _client.GetAsync(
            $"/api/erp/accounts-payable/by-supplier/{_supplierId}");
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
        using var detailsDoc = JsonDocument.Parse(await detailsResponse.Content.ReadAsStringAsync());
        var detail = detailsDoc.RootElement.GetProperty("data")[0];
        Assert.Equal(20000m, detail.GetProperty("paidAmount").GetDecimal());
        Assert.Equal(5000m, detail.GetProperty("discountAmount").GetDecimal());
        Assert.Equal(80000m, detail.GetProperty("outstanding").GetDecimal());

        var agingResponse = await _client.GetAsync("/api/erp/accounts-payable/aging");
        Assert.Equal(HttpStatusCode.OK, agingResponse.StatusCode);
        using var agingDoc = JsonDocument.Parse(await agingResponse.Content.ReadAsStringAsync());
        var aging = agingDoc.RootElement.GetProperty("data")[0];
        Assert.Equal(0m, aging.GetProperty("current").GetDecimal());
        Assert.Equal(0m, aging.GetProperty("days30").GetDecimal());
        Assert.Equal(80000m, aging.GetProperty("days60").GetDecimal());
        Assert.Equal(0m, aging.GetProperty("days90").GetDecimal());
        Assert.Equal(0m, aging.GetProperty("over90").GetDecimal());
    }

    [Fact]
    public async Task RecordApPayment_ValidRequest_ReturnsOk()
    {
        var request = new
        {
            PurchaseOrderId = _poId,
            PaymentAmount = 50000
        };

        var response = await _client.PostAsJsonAsync("/api/erp/accounts-payable/payment", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Contains("Payment recorded", root.GetProperty("message").GetString()!);

        using var verifyDb = _factory.CreateDbContext();
        var updatedPo = await verifyDb.PurchaseOrders.FindAsync(_poId);
        Assert.Equal(50000, updatedPo!.ReceivedAmount);
    }

    [Fact]
    public async Task RecordApPayment_UnknownPo_ReturnsNotFound()
    {
        var request = new
        {
            PurchaseOrderId = Guid.NewGuid(),
            PaymentAmount = 10000
        };

        var response = await _client.PostAsJsonAsync("/api/erp/accounts-payable/payment", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
