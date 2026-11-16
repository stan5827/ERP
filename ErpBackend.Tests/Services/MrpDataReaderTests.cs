using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.HR;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.Sales;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class MrpDataReaderTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static Sku NewSku(string code, decimal qty, decimal reorderPoint, string status = "active", int lowLevel = 0) =>
        new() { SkuNumber = code, SkuName = code + "-Name", Status = status, IsStocked = true, LowLevelCode = lowLevel, Qty = qty, ReorderPoint = reorderPoint };

    [Fact]
    public async Task PreloadDemandDataAsync_PopulatesSkuQtyAndReorderPoint()
    {
        using var db = CreateInMemoryDbContext();
        var skuA = NewSku("S-A", 100, 20);
        var skuB = NewSku("S-B", 50, 10);
        var skuC = NewSku("S-C", 5, 0);
        db.Skus.AddRange(skuA, skuB, skuC);
        await db.SaveChangesAsync();

        var reader = new MrpDataReader(db, NullLogger<MrpDataReader>.Instance);

        var cache = await reader.PreloadDemandDataAsync(new() { skuA.Id, skuB.Id }, DateTime.Today, DateTime.Today.AddDays(30), true);

        Assert.Equal(100, cache.SkuQtys[skuA.Id]);
        Assert.Equal(20, cache.SkuReorderPoints[skuA.Id]);
        Assert.Equal(50, cache.SkuQtys[skuB.Id]);
        Assert.Equal(10, cache.SkuReorderPoints[skuB.Id]);
        Assert.DoesNotContain(skuC.Id, cache.SkuQtys.Keys);
        Assert.Empty(cache.SalesOrderItems);
    }

    [Fact]
    public async Task GetSkusForPlanningAsync_ReturnsOnlyActiveStockedOrderedByLowLevel()
    {
        using var db = CreateInMemoryDbContext();
        db.Skus.AddRange(
            new Sku { SkuNumber = "S-A", SkuName = "A", Status = "active", IsStocked = true, LowLevelCode = 2 },
            new Sku { SkuNumber = "S-B", SkuName = "B", Status = "inactive", IsStocked = true, LowLevelCode = 0 },
            new Sku { SkuNumber = "S-C", SkuName = "C", Status = "active", IsStocked = false, LowLevelCode = 0 });
        await db.SaveChangesAsync();

        var reader = new MrpDataReader(db, NullLogger<MrpDataReader>.Instance);

        var result = await reader.GetSkusForPlanningAsync();

        Assert.Single(result);
        Assert.Equal("S-A", result[0].SkuNumber);
    }

    [Fact]
    public async Task GetLatestRunAsync_ReturnsMostRecent()
    {
        using var db = CreateInMemoryDbContext();
        var older = new MrpRun { RunNumber = "MRP-1", StartedAt = DateTime.Today.AddDays(-2) };
        var latest = new MrpRun { RunNumber = "MRP-2", StartedAt = DateTime.Today };
        db.MrpRuns.AddRange(older, latest);
        await db.SaveChangesAsync();

        var reader = new MrpDataReader(db, NullLogger<MrpDataReader>.Instance);

        var result = await reader.GetLatestRunAsync();

        Assert.NotNull(result);
        Assert.Equal("MRP-2", result!.RunNumber);
    }

    [Fact]
    public async Task GetRunsAsync_PaginatesByStartedAtDescending()
    {
        using var db = CreateInMemoryDbContext();
        db.MrpRuns.AddRange(
            new MrpRun { RunNumber = "MRP-1", StartedAt = DateTime.Today.AddDays(-3) },
            new MrpRun { RunNumber = "MRP-2", StartedAt = DateTime.Today.AddDays(-2) },
            new MrpRun { RunNumber = "MRP-3", StartedAt = DateTime.Today.AddDays(-1) });
        await db.SaveChangesAsync();

        var reader = new MrpDataReader(db, NullLogger<MrpDataReader>.Instance);

        var result = await reader.GetRunsAsync(page: 1, pageSize: 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("MRP-3", result[0].RunNumber);
        Assert.Equal("MRP-2", result[1].RunNumber);
    }

    [Fact]
    public async Task GetDemandSourcesAsync_ReturnsSalesOrderDemands_ForSku()
    {
        using var db = CreateInMemoryDbContext();
        var sku1 = NewSku("S-1", 100, 20); // Qty > ReorderPoint so safety-stock branch stays quiet
        var sku2 = NewSku("S-2", 100, 20);
        var so1 = new SalesOrder { OrderNumber = "SO-1", Status = "released" };
        var soi1 = new SalesOrderItem { SalesOrderId = so1.Id, SkuId = sku1.Id, ProductId = Guid.NewGuid(), Quantity = 10, DeliveredQty = 2 };
        var so2 = new SalesOrder { OrderNumber = "SO-2", Status = "released" };
        var soi2 = new SalesOrderItem { SalesOrderId = so2.Id, SkuId = sku2.Id, ProductId = Guid.NewGuid(), Quantity = 5, DeliveredQty = 0 };
        db.Skus.AddRange(sku1, sku2);
        db.SalesOrders.AddRange(so1, so2);
        db.SalesOrderItems.AddRange(soi1, soi2);
        await db.SaveChangesAsync();

        var reader = new MrpDataReader(db, NullLogger<MrpDataReader>.Instance);

        var sources = await reader.GetDemandSourcesAsync(sku1.Id, DateTime.Today, DateTime.Today.AddDays(30), includeBacklog: true);

        var soSources = sources.Where(s => s.RequirementType == "sales_order").ToList();
        Assert.Single(soSources);
        Assert.Equal("SO-1", soSources[0].SourceDocumentNumber);
        Assert.Equal(8, soSources[0].RequiredQty); // 10 - 2
    }

    [Fact]
    public async Task GetScheduledReceiptsAsync_ReturnsConfirmedPendingReceipts()
    {
        using var db = CreateInMemoryDbContext();
        var sku = NewSku("S-1", 100, 20);
        var po = new PurchaseOrder { PoNumber = "PO-1", Status = "confirmed", ExpectedDeliveryDate = DateTime.Today.AddDays(5) };
        var inRange = new PurchaseOrderItem { PurchaseOrderId = po.Id, SkuId = sku.Id, Quantity = 10, ReceivedQty = 3 };
        var fullyReceived = new PurchaseOrderItem { PurchaseOrderId = po.Id, SkuId = sku.Id, Quantity = 4, ReceivedQty = 4 };
        var outOfRange = new PurchaseOrderItem { PurchaseOrderId = po.Id, SkuId = sku.Id, Quantity = 7, ReceivedQty = 1, };
        // out-of-range delivery date
        var po2 = new PurchaseOrder { PoNumber = "PO-2", Status = "confirmed", ExpectedDeliveryDate = DateTime.Today.AddDays(40) };
        var late = new PurchaseOrderItem { PurchaseOrderId = po2.Id, SkuId = sku.Id, Quantity = 7, ReceivedQty = 1 };
        db.Skus.Add(sku);
        db.PurchaseOrders.AddRange(po, po2);
        db.PurchaseOrderItems.AddRange(inRange, fullyReceived, late);
        await db.SaveChangesAsync();

        var reader = new MrpDataReader(db, NullLogger<MrpDataReader>.Instance);

        var receipts = await reader.GetScheduledReceiptsAsync(sku.Id, DateTime.Today, DateTime.Today.AddDays(30));

        Assert.Single(receipts);
        Assert.Equal(10, receipts[0].ReceiptQty);
        Assert.Equal(3, receipts[0].ReceivedQty);
    }
}
