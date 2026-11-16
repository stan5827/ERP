using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class PlanningServiceTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ErpGeneralDbContext(options);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsOnlyReorderCandidates()
    {
        using var db = CreateInMemoryDbContext();
        db.Skus.AddRange(
            new Sku { SkuNumber = "S001", SkuName = "Product 1", Qty = 5, ReorderPoint = 10, SafetyStock = 8, MaxStock = 100, Moq = 50, Status = "active" },
            new Sku { SkuNumber = "S002", SkuName = "Product 2", Qty = 50, ReorderPoint = 10, MaxStock = 100, Status = "active" },
            new Sku { SkuNumber = "S003", SkuName = "Product 3", Qty = 1, ReorderPoint = 10, SafetyStock = 5, MaxStock = 100, Status = "inactive" }
        );
        await db.SaveChangesAsync();

        var service = new PlanningService(db);
        var result = await service.GetSuggestionsAsync();

        Assert.Single(result);
        Assert.Equal("S001", result[0].SkuCode);
        Assert.Equal(95, result[0].SuggestedQty); // MaxStock(100) - Qty(5) = 95, not Moq(50) since 95 > 50
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsEmpty_WhenNoCandidates()
    {
        using var db = CreateInMemoryDbContext();
        db.Skus.Add(new Sku { SkuNumber = "S001", SkuName = "Product 1", Qty = 50, ReorderPoint = 10, MaxStock = 100, Status = "active" });
        await db.SaveChangesAsync();

        var service = new PlanningService(db);
        var result = await service.GetSuggestionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSuggestionsAsync_UsesSafetyStockAlertReason()
    {
        using var db = CreateInMemoryDbContext();
        db.Skus.Add(new Sku { SkuNumber = "S001", SkuName = "Product 1", Qty = 5, ReorderPoint = 10, SafetyStock = 15, MaxStock = 100, Moq = 50, Status = "active" });
        await db.SaveChangesAsync();

        var service = new PlanningService(db);
        var result = await service.GetSuggestionsAsync();

        Assert.Single(result);
        Assert.Equal("SAFETY_STOCK_ALERT", result[0].Reason);
    }

    [Fact]
    public async Task GetSuggestionsAsync_UsesMoq_WhenSuggestedQtyExceedsMoq()
    {
        using var db = CreateInMemoryDbContext();
        db.Skus.Add(new Sku { SkuNumber = "S001", SkuName = "Product 1", Qty = 5, ReorderPoint = 10, SafetyStock = 5, MaxStock = 8, Moq = 50, Status = "active" });
        await db.SaveChangesAsync();

        var service = new PlanningService(db);
        var result = await service.GetSuggestionsAsync();

        Assert.Single(result);
        Assert.Equal(50, result[0].SuggestedQty); // MaxStock(8) - Qty(5) = 3, but Moq(50) > 3
    }
}
