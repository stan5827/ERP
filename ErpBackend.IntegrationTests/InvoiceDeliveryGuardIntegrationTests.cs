using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Sales;

namespace ErpBackend.IntegrationTests;

public class InvoiceDeliveryGuardIntegrationTests
{
    private static readonly CustomWebApplicationFactory Factory = new();

    [Fact]
    public async Task CreateInvoice_WithoutCompletedDelivery_IsRejected()
    {
        Guid customerId;
        Guid skuId;
        using (var db = Factory.CreateDbContext())
        {
            db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
            db.SalesInvoices.RemoveRange(db.SalesInvoices);
            db.Customers.RemoveRange(db.Customers);
            db.Skus.RemoveRange(db.Skus);
            db.SaveChanges();
            db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

            var customer = new Customer
            {
                CustomerCode = "CUST-DELIVERY-GUARD",
                CustomerName = "Delivery Guard Test Customer",
                Currency = "TWD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var sku = new Sku
            {
                SkuNumber = "SKU-DELIVERY-GUARD",
                SkuName = "Delivery Guard Test Product",
                Unit = "PCS",
                Category = "FG",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Customers.Add(customer);
            db.Skus.Add(sku);
            db.SaveChanges();
            customerId = customer.Id;
            skuId = sku.Id;
        }

        using var client = Factory.CreateClient();
        var request = new
        {
            CustomerId = customerId,
            Items = new[]
            {
                new { SkuId = skuId, Quantity = 1m, UnitPrice = 100m }
            }
        };

        var response = await client.PostAsJsonAsync("/api/erp/invoices", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invoice rejected: invalid_invoice_items",
            await response.Content.ReadAsStringAsync());
        using var verifyDb = Factory.CreateDbContext();
        Assert.Empty(verifyDb.SalesInvoices);
    }
}
