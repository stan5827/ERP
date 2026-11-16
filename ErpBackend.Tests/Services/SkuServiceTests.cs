using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public class SkuServiceTests
{
    [Fact]
    public async Task CreateSkuAsync_NormalizesUnspecifiedEffectiveDatesToUtc()
    {
        await using var db = CreateInMemoryDbContext();
        var service = new SkuService(db);

        var created = await service.CreateSkuAsync(new SkuDto
        {
            SkuNumber = "SKU-001",
            SkuName = "Test SKU",
            EffectiveFrom = new DateTime(2026, 10, 19),
            EffectiveTo = new DateTime(2026, 12, 31)
        });

        var savedSku = await db.Skus.SingleAsync(sku => sku.Id == created.Id);
        Assert.Equal(DateTimeKind.Utc, savedSku.EffectiveFrom?.Kind);
        Assert.Equal(DateTimeKind.Utc, savedSku.EffectiveTo?.Kind);
    }

    [Fact]
    public async Task UpdateSkuAsync_NormalizesUnspecifiedEffectiveDatesToUtc()
    {
        await using var db = CreateInMemoryDbContext();
        var sku = new Sku { SkuNumber = "SKU-001", SkuName = "Test SKU" };
        db.Skus.Add(sku);
        await db.SaveChangesAsync();
        var service = new SkuService(db);

        await service.UpdateSkuAsync(sku.Id, new SkuDto
        {
            SkuNumber = sku.SkuNumber,
            SkuName = sku.SkuName,
            EffectiveFrom = new DateTime(2026, 10, 19),
            EffectiveTo = new DateTime(2026, 12, 31)
        });

        Assert.Equal(DateTimeKind.Utc, sku.EffectiveFrom?.Kind);
        Assert.Equal(DateTimeKind.Utc, sku.EffectiveTo?.Kind);
    }

    [Fact]
    public async Task GetSkusAsync_IncludesProductionUomCodeAndName()
    {
        await using var db = CreateInMemoryDbContext();
        var productionUom = new Uom { UomCode = "G", UomName = "公克" };
        var sku = new Sku
        {
            SkuNumber = "SKU-001",
            SkuName = "Test SKU",
            ProductionUom = productionUom,
            ProductionUomId = productionUom.Id
        };
        db.Skus.Add(sku);
        await db.SaveChangesAsync();

        var service = new SkuService(db);
        var result = await service.GetSkusAsync(null, null, null, 1, 10);

        var returnedSku = Assert.Single(result.Items);
        Assert.Equal("G", returnedSku.ProductionUomCode);
        Assert.Equal("公克", returnedSku.ProductionUomName);
    }

    [Fact]
    public async Task CreateSkuAsync_SynchronizesUomIdsAndCodesFromSelectedUoms()
    {
        await using var db = CreateInMemoryDbContext();
        var baseUom = new Uom { UomCode = "G", UomName = "公克" };
        var purchaseUom = new Uom { UomCode = "KG", UomName = "公斤" };
        var salesUom = new Uom { UomCode = "BAG", UomName = "袋" };
        var productionUom = new Uom { UomCode = "PIECE", UomName = "片" };
        db.Uoms.AddRange(baseUom, purchaseUom, salesUom, productionUom);
        await db.SaveChangesAsync();

        var created = await new SkuService(db).CreateSkuAsync(new SkuDto
        {
            SkuNumber = "SKU-UOM",
            SkuName = "單位同步測試",
            Unit = "STALE",
            BaseUomId = baseUom.Id,
            PurchaseUomId = purchaseUom.Id,
            SalesUomId = salesUom.Id,
            ProductionUomId = productionUom.Id
        });

        var savedSku = await db.Skus.SingleAsync(sku => sku.Id == created.Id);
        Assert.Equal("G", savedSku.Unit);
        Assert.Equal("G", savedSku.BaseUomCode);
        Assert.Equal("KG", savedSku.PurchaseUomCode);
        Assert.Equal("BAG", savedSku.SalesUomCode);
        Assert.Equal("PIECE", savedSku.ProductionUomCode);
        Assert.Equal("G", created.BaseUomCode);
        Assert.Equal("公克", created.BaseUomName);
        Assert.Equal("PIECE", created.ProductionUomCode);
    }

    [Fact]
    public async Task GetSkusAsync_IncludesPurchaseUomAndFiltersPurchasableSearchResults()
    {
        await using var db = CreateInMemoryDbContext();
        var purchaseUom = new Uom { UomCode = "BOX", UomName = "箱" };
        db.Skus.AddRange(
            new Sku
            {
                SkuNumber = "MAT-001",
                SkuName = "鋼材",
                Specification = "304 不鏽鋼",
                IsPurchasable = true,
                PurchaseUom = purchaseUom,
                PurchaseUomId = purchaseUom.Id
            },
            new Sku
            {
                SkuNumber = "MAT-002",
                SkuName = "原料鋼板",
                ItemType = "material",
                SourceType = "buy",
                IsPurchasable = false
            },
            new Sku
            {
                SkuNumber = "MAT-003",
                SkuName = "非採購品",
                SourceType = "make",
                IsPurchasable = false
            });
        await db.SaveChangesAsync();

        var service = new SkuService(db);
        var result = await service.GetSkusAsync(null, null, "鋼", 1, 10, isPurchasable: true);

        Assert.Equal(2, result.Items.Count);
        var returnedSku = Assert.Single(result.Items, sku => sku.SkuNumber == "MAT-001");
        Assert.Equal("BOX", returnedSku.PurchaseUomCode);
        Assert.Equal("箱", returnedSku.PurchaseUomName);
        Assert.Contains(result.Items, sku => sku.SkuNumber == "MAT-002");
    }

    [Fact]
    public async Task GetSkusAsync_FiltersSellableSearchResultsAndIncludesSalesUom()
    {
        await using var db = CreateInMemoryDbContext();
        var salesUom = new Uom { UomCode = "EA", UomName = "個" };
        db.Skus.AddRange(
            new Sku
            {
                SkuNumber = "FG-001",
                SkuName = "可銷售產品",
                IsSellable = true,
                SalesUom = salesUom,
                SalesUomId = salesUom.Id
            },
            new Sku
            {
                SkuNumber = "FG-002",
                SkuName = "不可銷售產品",
                IsSellable = false
            });
        await db.SaveChangesAsync();

        var service = new SkuService(db);
        var result = await service.GetSkusAsync(null, null, "產品", 1, 25, isSellable: true);

        var returnedSku = Assert.Single(result.Items);
        Assert.Equal("FG-001", returnedSku.SkuNumber);
        Assert.Equal("EA", returnedSku.SalesUomCode);
        Assert.Equal("個", returnedSku.SalesUomName);
    }

    private static ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }
}
