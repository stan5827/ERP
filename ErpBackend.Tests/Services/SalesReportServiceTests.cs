using System.Text.Json;
using ErpBackend.Data;
using ErpBackend.Models.Sales;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public class SalesReportServiceTests
{
    [Fact]
    public async Task GetOrderToCashAsync_IncludesTheEntireEndDateAndUsesTheSameRangeForSummary()
    {
        await using var db = CreateDbContext();
        var customer = new Customer { CustomerCode = "C-001", CustomerName = "Customer" };
        db.Customers.Add(customer);
        db.SalesOrders.AddRange(
            CreateOrder(customer, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), 100m),
            CreateOrder(customer, new DateTime(2026, 7, 31, 23, 59, 59, DateTimeKind.Utc), 200m),
            CreateOrder(customer, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), 300m));
        await db.SaveChangesAsync();

        var result = await new SalesReportService(db).GetOrderToCashAsync(
            new DateTime(2026, 7, 1),
            new DateTime(2026, 7, 31));
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));

        Assert.Equal(2, document.RootElement.GetProperty("rows").GetArrayLength());
        var summary = document.RootElement.GetProperty("summary");
        Assert.Equal(2, summary.GetProperty("totalOrders").GetInt32());
        Assert.Equal(300m, summary.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(DateTimeKind.Utc, summary.GetProperty("startDate").GetDateTime().Kind);
        Assert.Equal(DateTimeKind.Utc, summary.GetProperty("endDate").GetDateTime().Kind);
    }

    [Fact]
    public async Task GetOrderToCashAsync_IncludeAllIgnoresTheDateRange()
    {
        await using var db = CreateDbContext();
        var customer = new Customer { CustomerCode = "C-002", CustomerName = "Customer" };
        db.Customers.Add(customer);
        db.SalesOrders.AddRange(
            CreateOrder(customer, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), 100m),
            CreateOrder(customer, new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc), 200m));
        await db.SaveChangesAsync();

        var result = await new SalesReportService(db).GetOrderToCashAsync(
            new DateTime(2026, 7, 1),
            new DateTime(2026, 7, 2),
            includeAll: true);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));

        Assert.Equal(2, document.RootElement.GetProperty("rows").GetArrayLength());
        Assert.Equal(2, document.RootElement.GetProperty("summary").GetProperty("totalOrders").GetInt32());
    }

    private static ErpGeneralDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SalesOrder CreateOrder(Customer customer, DateTime orderDate, decimal totalAmount) =>
        new()
        {
            OrderNumber = $"SO-{Guid.NewGuid():N}",
            CustomerId = customer.Id,
            Customer = customer,
            OrderDate = orderDate,
            TotalAmount = totalAmount,
            Status = "pending"
        };
}
