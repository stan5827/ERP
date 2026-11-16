using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Sales;
using ErpBackend.Models.Inventory;

namespace ErpBackend.IntegrationTests;

public class FinanceRevenueIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();
    private Guid _customerId;
    private Guid _skuId;
    private Guid _bankAccountId;

    public FinanceRevenueIntegrationTests()
    {
        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.CashReceipts.RemoveRange(db.CashReceipts);
        db.SalesInvoices.RemoveRange(db.SalesInvoices);
        db.Guarantees.RemoveRange(db.Guarantees);
        db.BankAccounts.RemoveRange(db.BankAccounts);
        db.Customers.RemoveRange(db.Customers);
        db.Skus.RemoveRange(db.Skus);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var customer = new Customer
        {
            CustomerCode = "CUST-FIN-001",
            CustomerName = "測試客戶",
            CreditLimit = 1000000,
            CreditStatus = "normal",
            Currency = "TWD",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        _customerId = customer.Id;

        var sku = new Sku
        {
            SkuNumber = "SKU-FIN-001",
            SkuName = "財務測試物料",
            Unit = "PCS",
            Category = "FG",
            Qty = 1000,
            SafetyStock = 10,
            ReorderPoint = 20,
            MaxStock = 5000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Skus.Add(sku);
        db.SaveChanges();
        _skuId = sku.Id;

        var bankAccount = new BankAccount
        {
            BankCode = "HNB",
            BankName = "本國銀行",
            AccountNumber = "123-456-789",
            AccountName = "ERP 主要帳戶",
            Currency = "TWD",
            CurrentBalance = 1000000,
            IsActive = true
        };
        db.BankAccounts.Add(bankAccount);
        db.SaveChanges();
        _bankAccountId = bankAccount.Id;
    }

    #region Invoices

    [Fact]
    public async Task GetInvoices_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/invoices");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateInvoice_ValidRequest_Creates()
    {
        using var db = _factory.CreateDbContext();
        var salesOrder = new SalesOrder
        {
            OrderNumber = "SO-INV-TEST-001",
            CustomerId = _customerId,
            OrderDate = DateTime.UtcNow,
            Status = "confirmed",
            Items = new List<SalesOrderItem>
            {
                new()
                {
                    ProductId = _skuId,
                    SkuId = _skuId,
                    Quantity = 10,
                    UnitPrice = 1000,
                    Subtotal = 10000,
                    LineNumber = 1
                }
            }
        };
        db.SalesOrders.Add(salesOrder);
        db.SaveChanges();

        var delivery = new DeliveryOrder
        {
            DeliveryNumber = "DEL-INV-TEST-001",
            SalesOrderId = salesOrder.Id,
            DeliveryDate = DateTime.UtcNow,
            Status = "completed",
            Items = new List<DeliveryOrderItem>
            {
                new()
                {
                    SkuId = _skuId,
                    LotNumber = "LOT-INV-001",
                    DeliveredQty = 10
                }
            }
        };
        db.DeliveryOrders.Add(delivery);
        db.SaveChanges();

        var request = new
        {
            CustomerId = _customerId,
            SalesOrderId = salesOrder.Id,
            DeliveryOrderId = delivery.Id,
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Currency = "TWD",
            TaxType = "taxable_5pct",
            Items = new[]
            {
                new
                {
                    SkuId = _skuId,
                    Quantity = 10,
                    UnitPrice = 1000,
                    DiscountRate = 0
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/erp/invoices", request);
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            await response.Content.ReadAsStringAsync());

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("draft", data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CreateInvoice_UnknownCustomer_ReturnsBadRequest()
    {
        var request = new
        {
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new { SkuId = _skuId, Quantity = 1, UnitPrice = 100 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/erp/invoices", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoice_ExistingId_ReturnsDetail()
    {
        using var db = _factory.CreateDbContext();
        var invoice = new SalesInvoice
        {
            InvoiceNumber = "INV-TEST-001",
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CustomerId = _customerId,
            Currency = "TWD",
            ExchangeRate = 1,
            TaxType = "taxable_5pct",
            SalesAmount = 10000,
            TaxAmount = 500,
            TotalAmount = 10500,
            Status = "draft",
            CreatedAt = DateTime.UtcNow
        };
        db.SalesInvoices.Add(invoice);
        db.SaveChanges();

        var response = await _client.GetAsync($"/api/erp/invoices/{invoice.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("INV-TEST-001", data.GetProperty("invoiceNumber").GetString());
    }

    [Fact]
    public async Task GetInvoice_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/erp/invoices/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApproveInvoice_WithoutConfiguredAccounts_RemainsBlocked()
    {
        using var db = _factory.CreateDbContext();
        var salesOrder = new SalesOrder
        {
            OrderNumber = "SO-APPROVE-TEST-001",
            CustomerId = _customerId,
            OrderDate = DateTime.UtcNow,
            Status = "confirmed"
        };
        db.SalesOrders.Add(salesOrder);
        db.SaveChanges();

        var delivery = new DeliveryOrder
        {
            DeliveryNumber = "DEL-APPROVE-TEST-001",
            SalesOrderId = salesOrder.Id,
            DeliveryDate = DateTime.UtcNow,
            Status = "completed"
        };
        db.DeliveryOrders.Add(delivery);
        db.SaveChanges();

        var invoice = new SalesInvoice
        {
            InvoiceNumber = "INV-APPROVE-001",
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CustomerId = _customerId,
            SalesOrderId = salesOrder.Id,
            DeliveryOrderId = delivery.Id,
            Currency = "TWD",
            ExchangeRate = 1,
            TaxType = "taxable_5pct",
            SalesAmount = 10000,
            TaxAmount = 500,
            TotalAmount = 10500,
            Status = "draft",
            CreatedAt = DateTime.UtcNow
        };
        db.SalesInvoices.Add(invoice);
        db.SaveChanges();

        var request = new { ApproverId = _customerId, ApprovedBy = _customerId, Comments = "核准" };
        var response = await _client.PatchAsJsonAsync($"/api/erp/invoices/{invoice.Id}/approve", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Required active GL account mappings", body);
        Assert.Equal("draft", db.SalesInvoices.First(saved => saved.Id == invoice.Id).Status);
    }

    [Fact]
    public async Task ApproveInvoice_AlreadyApproved_ReturnsGuarded()
    {
        using var db = _factory.CreateDbContext();
        var invoice = new SalesInvoice
        {
            InvoiceNumber = "INV-APPROVE-GUARD",
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CustomerId = _customerId,
            Currency = "TWD",
            ExchangeRate = 1,
            TaxType = "taxable_5pct",
            SalesAmount = 10000,
            TaxAmount = 500,
            TotalAmount = 10500,
            Status = "approved",
            CreatedAt = DateTime.UtcNow
        };
        db.SalesInvoices.Add(invoice);
        db.SaveChanges();

        var request = new { ApproverId = _customerId, ApprovedBy = _customerId };
        var response = await _client.PatchAsJsonAsync($"/api/erp/invoices/{invoice.Id}/approve", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task VoidInvoice_ValidRequest_Voids()
    {
        using var db = _factory.CreateDbContext();
        var invoice = new SalesInvoice
        {
            InvoiceNumber = "INV-VOID-001",
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CustomerId = _customerId,
            Currency = "TWD",
            ExchangeRate = 1,
            TaxType = "taxable_5pct",
            SalesAmount = 10000,
            TaxAmount = 500,
            TotalAmount = 10500,
            Status = "draft",
            CreatedAt = DateTime.UtcNow
        };
        db.SalesInvoices.Add(invoice);
        db.SaveChanges();

        var request = new { VoiderId = _customerId, VoidedBy = _customerId, VoidReason = "錯誤開立" };
        var response = await _client.PatchAsJsonAsync($"/api/erp/invoices/{invoice.Id}/void", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VoidInvoice_AlreadyVoided_ReturnsGuarded()
    {
        using var db = _factory.CreateDbContext();
        var invoice = new SalesInvoice
        {
            InvoiceNumber = "INV-VOID-GUARD",
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CustomerId = _customerId,
            Currency = "TWD",
            ExchangeRate = 1,
            TaxType = "taxable_5pct",
            SalesAmount = 10000,
            TaxAmount = 500,
            TotalAmount = 10500,
            Status = "voided",
            CreatedAt = DateTime.UtcNow
        };
        db.SalesInvoices.Add(invoice);
        db.SaveChanges();

        var request = new { VoiderId = _customerId, VoidedBy = _customerId };
        var response = await _client.PatchAsJsonAsync($"/api/erp/invoices/{invoice.Id}/void", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAgingReport_ReturnsReport()
    {
        var response = await _client.GetAsync("/api/erp/invoices/aging");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    #endregion

    #region Cash Receipts

    [Fact]
    public async Task GetCashReceipts_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/cash-receipts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateCashReceipt_WithoutCashAccountMapping_RemainsBlocked()
    {
        using var db = _factory.CreateDbContext();
        var invoice = new SalesInvoice
        {
            InvoiceNumber = "INV-CR-001",
            InvoiceDate = DateTime.UtcNow.AddDays(-10),
            DueDate = DateTime.UtcNow.AddDays(20),
            CustomerId = _customerId,
            Currency = "TWD",
            ExchangeRate = 1,
            TaxType = "taxable_5pct",
            SalesAmount = 10000,
            TaxAmount = 500,
            TotalAmount = 10500,
            PaidAmount = 0,
            Status = "approved",
            CreatedAt = DateTime.UtcNow
        };
        db.SalesInvoices.Add(invoice);
        db.SaveChanges();

        var request = new
        {
            ReceiptDate = DateTime.UtcNow,
            CustomerId = _customerId,
            PaymentMethod = "wire",
            ReceivedAmount = 10500,
            BankAccountId = _bankAccountId,
            Allocations = new[]
            {
                new
                {
                    SalesInvoiceId = invoice.Id,
                    AllocatedAmount = 10500
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/erp/cash-receipts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Empty(db.CashReceipts);
    }

    [Fact]
    public async Task CreateCashReceipt_NoAllocations_ReturnsInvalid()
    {
        var request = new
        {
            ReceiptDate = DateTime.UtcNow,
            CustomerId = _customerId,
            PaymentMethod = "wire",
            ReceivedAmount = 1000,
            Allocations = new List<object>()
        };

        var response = await _client.PostAsJsonAsync("/api/erp/cash-receipts", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPendingInvoices_ReturnsPending()
    {
        var response = await _client.GetAsync($"/api/erp/cash-receipts/pending-invoices/{_customerId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    #endregion

    #region Guarantees

    [Fact]
    public async Task GetGuarantees_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/finance/guarantees");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateGuarantee_ValidRequest_Creates()
    {
        var request = new
        {
            GuaranteeType = "bank",
            Beneficiary = "測試受益人",
            GuaranteeAmount = 500000,
            ApplicationDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            FeeRate = 1.5m
        };

        var response = await _client.PostAsJsonAsync("/api/erp/finance/guarantees", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("保證函已建立", body);
    }

    #endregion
}
