using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class BomServiceTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static void SetProductionUom(ErpGeneralDbContext db, Sku sku, string code, decimal factor = 1m)
    {
        var uom = new Uom { UomCode = code, UomName = code };
        db.Uoms.Add(uom);
        sku.ProductionUomId = uom.Id;
        sku.ProductionUomCode = code;
        sku.ProductionToBaseFactor = factor;
    }

    [Fact]
    public async Task GetBomsAsync_ReturnsEmpty_WhenNoData()
    {
        using var db = CreateInMemoryDbContext();
        var service = new BomService(db);

        var result = await service.GetBomsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetBomsAsync_FiltersByProductSkuId()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var other = new Sku { SkuNumber = "P-002", SkuName = "Product Q" };
        db.Skus.AddRange(prod, other);
        db.Boms.AddRange(
            new Bom { BomCode = "B001", ProductSkuId = prod.Id },
            new Bom { BomCode = "B002", ProductSkuId = other.Id });
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var result = await service.GetBomsAsync(prod.Id);

        Assert.Single(result);
        Assert.Equal("B001", result[0].BomCode);
        Assert.Equal("P-001", result[0].ProductSkuNumber);
        Assert.Equal("Product P", result[0].ProductSkuName);
    }

    [Fact]
    public async Task GetBomAsync_ReturnsDtoWithChildSkuInfo_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var child = new Sku { SkuNumber = "C-001", SkuName = "Child C" };
        db.Skus.AddRange(prod, child);
        var bom = new Bom { BomCode = "B001", ProductSkuId = prod.Id };
        db.Boms.Add(bom);
        await db.SaveChangesAsync();
        db.BomItems.Add(new BomItem { BomId = bom.Id, ChildSkuId = child.Id, Quantity = 2, Unit = "ea" });
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var dto = await service.GetBomAsync(bom.Id);

        Assert.NotNull(dto);
        Assert.Equal("P-001", dto!.ProductSkuNumber);
        Assert.Equal("Product P", dto.ProductSkuName);
        Assert.Single(dto.Items);
        Assert.Equal("C-001", dto.Items[0].ChildSkuNumber);
        Assert.Equal("Child C", dto.Items[0].ChildSkuName);
    }

    [Fact]
    public async Task GetBomAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new BomService(db);

        var dto = await service.GetBomAsync(Guid.NewGuid());

        Assert.Null(dto);
    }

    [Fact]
    public async Task CreateBomAsync_ReturnsOk_AndMapsChildSkuNames_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var child = new Sku { SkuNumber = "C-001", SkuName = "Child C" };
        SetProductionUom(db, child, "G");
        db.Skus.AddRange(prod, child);
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var req = new CreateBomRequest
        {
            BomCode = "B001",
            ProductSkuId = prod.Id,
            Items = new() { new CreateBomItemRequest { ChildSkuId = child.Id, Quantity = 2, Unit = "KG" } }
        };

        var (dto, status) = await service.CreateBomAsync(req);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Equal("B001", dto!.BomCode);
        Assert.Single(dto.Items);
        Assert.Equal("C-001", dto.Items[0].ChildSkuNumber);
        Assert.Equal("Child C", dto.Items[0].ChildSkuName);
        Assert.Equal("G", dto.Items[0].Unit);
    }

    [Fact]
    public async Task CreateBomAsync_NormalizesUnspecifiedEffectiveDateToUtc()
    {
        using var db = CreateInMemoryDbContext();
        var product = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        db.Skus.Add(product);
        await db.SaveChangesAsync();
        var service = new BomService(db);

        var (_, status) = await service.CreateBomAsync(new CreateBomRequest
        {
            BomCode = "B001",
            ProductSkuId = product.Id,
            EffectiveDate = new DateTime(2026, 10, 19)
        });

        Assert.Equal("ok", status);
        Assert.Equal(DateTimeKind.Utc, (await db.Boms.SingleAsync()).EffectiveDate.Kind);
    }

    [Fact]
    public async Task CreateBomAsync_ReturnsInvalid_WhenBomCodeEmpty()
    {
        using var db = CreateInMemoryDbContext();
        db.Skus.Add(new Sku { SkuNumber = "P-001", SkuName = "Product P" });
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var req = new CreateBomRequest { BomCode = "", ProductSkuId = db.Skus.First().Id };

        var (dto, status) = await service.CreateBomAsync(req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);
        Assert.Empty(db.Boms);
    }

    [Fact]
    public async Task CreateBomAsync_ReturnsInvalid_WhenProductSkuMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new BomService(db);
        var req = new CreateBomRequest { BomCode = "B001", ProductSkuId = Guid.NewGuid() };

        var (dto, status) = await service.CreateBomAsync(req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);
    }

    [Fact]
    public async Task CreateBomAsync_ReturnsDuplicate_WhenCodeExists()
    {
        using var db = CreateInMemoryDbContext();
        db.Skus.Add(new Sku { SkuNumber = "P-001", SkuName = "Product P" });
        db.Boms.Add(new Bom { BomCode = "B001" });
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var req = new CreateBomRequest { BomCode = "B001", ProductSkuId = db.Skus.First().Id };

        var (dto, status) = await service.CreateBomAsync(req);

        Assert.Null(dto);
        Assert.Equal("duplicate", status);
    }

    [Fact]
    public async Task CreateBomAsync_ReturnsInvalid_WhenChildEqualsProduct()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        SetProductionUom(db, prod, "ea");
        db.Skus.Add(prod);
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var req = new CreateBomRequest
        {
            BomCode = "B001",
            ProductSkuId = prod.Id,
            Items = new() { new CreateBomItemRequest { ChildSkuId = prod.Id, Quantity = 1, Unit = "ea" } }
        };

        var (dto, status) = await service.CreateBomAsync(req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);
        Assert.Empty(db.Boms);
    }

    [Fact]
    public async Task CreateBomAsync_ReturnsInvalid_WhenQuantityNotPositive()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var child = new Sku { SkuNumber = "C-001", SkuName = "Child C" };
        SetProductionUom(db, child, "ea");
        db.Skus.AddRange(prod, child);
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var req = new CreateBomRequest
        {
            BomCode = "B001",
            ProductSkuId = prod.Id,
            Items = new() { new CreateBomItemRequest { ChildSkuId = child.Id, Quantity = 0, Unit = "ea" } }
        };

        var (dto, status) = await service.CreateBomAsync(req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);
    }

    [Fact]
    public async Task CreateBomAsync_UsesProductionUnit_AndRejectsMissingProductionUnit()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var child = new Sku { SkuNumber = "C-001", SkuName = "Child C" };
        db.Skus.AddRange(prod, child);
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var req = new CreateBomRequest
        {
            BomCode = "B001",
            ProductSkuId = prod.Id,
            Items = new() { new CreateBomItemRequest { ChildSkuId = child.Id, Quantity = 2, Unit = null! } }
        };

        var (dto, status) = await service.CreateBomAsync(req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);

        SetProductionUom(db, child, "G");
        await db.SaveChangesAsync();
        (dto, status) = await service.CreateBomAsync(req);

        Assert.Equal("ok", status);
        Assert.Equal("G", dto!.Items[0].Unit);
    }

    [Fact]
    public async Task CreateBomAsync_ReturnsInvalid_WhenDuplicateChildSkuInItems()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var child = new Sku { SkuNumber = "C-001", SkuName = "Child C" };
        SetProductionUom(db, child, "ea");
        db.Skus.AddRange(prod, child);
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var req = new CreateBomRequest
        {
            BomCode = "B001",
            ProductSkuId = prod.Id,
            Items = new()
            {
                new CreateBomItemRequest { ChildSkuId = child.Id, Quantity = 2, Unit = "ea" },
                new CreateBomItemRequest { ChildSkuId = child.Id, Quantity = 1, Unit = "ea" }
            }
        };

        var (dto, status) = await service.CreateBomAsync(req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);
    }

    [Fact]
    public async Task UpdateBomAsync_ReturnsOk_AndReplacesItems_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var childA = new Sku { SkuNumber = "C-001", SkuName = "Child A" };
        var childB = new Sku { SkuNumber = "C-002", SkuName = "Child B" };
        SetProductionUom(db, childB, "pcs");
        db.Skus.AddRange(prod, childA, childB);
        var bom = new Bom { BomCode = "B001", ProductSkuId = prod.Id };
        db.Boms.Add(bom);
        await db.SaveChangesAsync();
        db.BomItems.Add(new BomItem { BomId = bom.Id, ChildSkuId = childA.Id, Quantity = 2, Unit = "ea" });
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var req = new UpdateBomRequest { Items = new() { new CreateBomItemRequest { ChildSkuId = childB.Id, Quantity = 3, Unit = "ea" } } };

        var (dto, status) = await service.UpdateBomAsync(bom.Id, req);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Single(dto!.Items);
        Assert.Equal(childB.Id, dto.Items[0].ChildSkuId);
    }

    [Fact]
    public async Task UpdateBomAsync_NormalizesUnspecifiedEffectiveDateToUtc()
    {
        using var db = CreateInMemoryDbContext();
        var bom = new Bom { BomCode = "B001", ProductSkuId = Guid.NewGuid() };
        db.Boms.Add(bom);
        await db.SaveChangesAsync();
        var service = new BomService(db);

        var (_, status) = await service.UpdateBomAsync(bom.Id, new UpdateBomRequest
        {
            EffectiveDate = new DateTime(2026, 10, 19)
        });

        Assert.Equal("ok", status);
        Assert.Equal(DateTimeKind.Utc, (await db.Boms.SingleAsync()).EffectiveDate.Kind);
    }

    [Fact]
    public async Task UpdateBomAsync_ReturnsNotFound_WhenIdMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new BomService(db);

        var (dto, status) = await service.UpdateBomAsync(Guid.NewGuid(), new UpdateBomRequest());

        Assert.Null(dto);
        Assert.Equal("not_found", status);
    }

    [Fact]
    public async Task UpdateBomAsync_ReturnsInvalid_WhenSelfReferenceItem()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        db.Skus.Add(prod);
        var bom = new Bom { BomCode = "B001", ProductSkuId = prod.Id };
        db.Boms.Add(bom);
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var req = new UpdateBomRequest { Items = new() { new CreateBomItemRequest { ChildSkuId = prod.Id, Quantity = 1, Unit = "ea" } } };

        var (dto, status) = await service.UpdateBomAsync(bom.Id, req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);
    }

    [Fact]
    public async Task DeleteBomAsync_ReturnsOk_AndRemovesBom_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        db.Skus.Add(prod);
        var bom = new Bom { BomCode = "B001", ProductSkuId = prod.Id };
        db.Boms.Add(bom);
        await db.SaveChangesAsync();
        db.BomItems.Add(new BomItem { BomId = bom.Id, ChildSkuId = prod.Id, Quantity = 1, Unit = "ea" });
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var (ok, status) = await service.DeleteBomAsync(bom.Id);

        Assert.True(ok);
        Assert.Equal("ok", status);
        Assert.Empty(db.Boms);
        Assert.Empty(db.BomItems);
    }

    [Fact]
    public async Task DeleteBomAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new BomService(db);

        var (ok, status) = await service.DeleteBomAsync(Guid.NewGuid());

        Assert.False(ok);
        Assert.Equal("not_found", status);
    }

    [Fact]
    public async Task SetBomStatusAsync_ReturnsOk_WhenActivated()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        db.Skus.Add(prod);
        var bom = new Bom { BomCode = "B001", ProductSkuId = prod.Id, Status = "draft" };
        db.Boms.Add(bom);
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var (ok, status) = await service.SetBomStatusAsync(bom.Id, "active");

        Assert.True(ok);
        Assert.Equal("ok", status);
        Assert.Equal("active", (await db.Boms.FirstAsync(b => b.Id == bom.Id)).Status);
    }

    [Fact]
    public async Task SetBomStatusAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new BomService(db);

        var (ok, status) = await service.SetBomStatusAsync(Guid.NewGuid(), "active");

        Assert.False(ok);
        Assert.Equal("not_found", status);
    }

    [Fact]
    public async Task GetCostRollupAsync_ReturnsTotals_WhenBomExists()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var child = new Sku { SkuNumber = "C-001", SkuName = "Child C", SourceType = "self_made", StandardCost = 100m };
        SetProductionUom(db, child, "ea");
        db.Skus.AddRange(prod, child);
        var bom = new Bom { BomCode = "B001", ProductSkuId = prod.Id };
        db.Boms.Add(bom);
        await db.SaveChangesAsync();
        db.BomItems.Add(new BomItem { BomId = bom.Id, ChildSkuId = child.Id, Quantity = 2, Unit = "ea" });
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var dto = await service.GetCostRollupAsync(bom.Id);

        Assert.NotNull(dto);
        Assert.Equal(200m, dto!.MaterialCost);
        Assert.Equal(40m, dto.LaborCost);
        Assert.Equal(20m, dto.OverheadCost);
        Assert.Equal(260m, dto.TotalCost);
    }

    [Fact]
    public async Task ApplyStandardCostRollupAsync_UpdatesSkuAndAuditsApproval()
    {
        using var db = CreateInMemoryDbContext();
        var product = new Sku
        {
            SkuNumber = "P-001",
            SkuName = "Product P",
            StandardCost = 12m,
            LatestPurchaseCost = 25m
        };
        var child = new Sku
        {
            SkuNumber = "C-001",
            SkuName = "Child C",
            SourceType = "self_made",
            StandardCost = 100m
        };
        SetProductionUom(db, child, "ea");
        db.Skus.AddRange(product, child);
        var bom = new Bom
        {
            BomCode = "B001",
            ProductSkuId = product.Id,
            Status = "active",
            EffectiveDate = DateTime.UtcNow.AddDays(-1)
        };
        db.Boms.Add(bom);
        await db.SaveChangesAsync();
        db.BomItems.Add(new BomItem
        {
            BomId = bom.Id,
            ChildSkuId = child.Id,
            Quantity = 2m,
            Unit = "ea"
        });
        await db.SaveChangesAsync();

        var approverId = Guid.NewGuid();
        var (result, status) = await new BomService(db).ApplyStandardCostRollupAsync(
            bom.Id,
            260m,
            approverId,
            CancellationToken.None);

        Assert.Equal("ok", status);
        Assert.NotNull(result);
        Assert.Equal(12m, result!.PreviousStandardCost);
        Assert.Equal(260m, result.AppliedStandardCost);
        Assert.Equal(260m, product.StandardCost);
        Assert.Equal(25m, product.LatestPurchaseCost);
        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal("approve_bom_standard_cost", audit.Action);
        Assert.Equal(bom.Id, audit.EntityId);
        Assert.Equal(approverId, audit.UserId);
    }

    [Fact]
    public async Task ApplyStandardCostRollupAsync_RejectsStalePreview()
    {
        using var db = CreateInMemoryDbContext();
        var product = new Sku { SkuNumber = "P-001", SkuName = "Product P", StandardCost = 12m };
        var child = new Sku { SkuNumber = "C-001", SkuName = "Child C", StandardCost = 100m };
        SetProductionUom(db, child, "ea");
        db.Skus.AddRange(product, child);
        var bom = new Bom
        {
            BomCode = "B001",
            ProductSkuId = product.Id,
            Status = "active",
            EffectiveDate = DateTime.UtcNow.AddDays(-1)
        };
        db.Boms.Add(bom);
        await db.SaveChangesAsync();
        db.BomItems.Add(new BomItem
        {
            BomId = bom.Id,
            ChildSkuId = child.Id,
            Quantity = 2m,
            Unit = "ea"
        });
        await db.SaveChangesAsync();
        child.StandardCost = 110m;
        await db.SaveChangesAsync();

        var (result, status) = await new BomService(db).ApplyStandardCostRollupAsync(
            bom.Id,
            200m,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal("stale_cost", status);
        Assert.Equal(12m, product.StandardCost);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task GetCostRollupAsync_ConvertsProductionQuantityToBaseUnit()
    {
        using var db = CreateInMemoryDbContext();
        var product = new Sku { SkuNumber = "P-001", SkuName = "Product" };
        var component = new Sku
        {
            SkuNumber = "C-001",
            SkuName = "Component",
            Unit = "KG",
            StandardCost = 100m
        };
        SetProductionUom(db, component, "G", 0.001m);
        db.Skus.AddRange(product, component);
        var bom = new Bom { BomCode = "B001", ProductSkuId = product.Id };
        db.Boms.Add(bom);
        await db.SaveChangesAsync();
        db.BomItems.Add(new BomItem
        {
            BomId = bom.Id,
            ChildSkuId = component.Id,
            Quantity = 1000m,
            Unit = "G"
        });
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var result = await service.GetCostRollupAsync(bom.Id);

        Assert.NotNull(result);
        Assert.Equal(100m, result!.MaterialCost);
    }

    [Fact]
    public async Task GetCostRollupAsync_UsesLatestPurchaseCostForPurchasedComponent()
    {
        using var db = CreateInMemoryDbContext();
        var product = new Sku { SkuNumber = "P-001", SkuName = "Product" };
        var component = new Sku
        {
            SkuNumber = "C-001",
            SkuName = "Purchased component",
            SourceType = "buy",
            LatestPurchaseCost = 150m,
            StandardCost = 100m
        };
        SetProductionUom(db, component, "ea");
        db.Skus.AddRange(product, component);
        var bom = new Bom { BomCode = "B001", ProductSkuId = product.Id };
        db.Boms.Add(bom);
        await db.SaveChangesAsync();
        db.BomItems.Add(new BomItem { BomId = bom.Id, ChildSkuId = component.Id, Quantity = 2m, Unit = "ea" });
        await db.SaveChangesAsync();

        var result = await new BomService(db).GetCostRollupAsync(bom.Id);

        Assert.NotNull(result);
        Assert.Equal(300m, result!.MaterialCost);
        Assert.Equal(300m, result.TotalCost);
    }

    [Fact]
    public async Task GetCostRollupAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new BomService(db);

        var dto = await service.GetCostRollupAsync(Guid.NewGuid());

        Assert.Null(dto);
    }

    [Fact]
    public async Task GetBomTreeAsync_BuildsTreeAndBreaksCycle()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P", SourceType = "self_made" };
        var child = new Sku { SkuNumber = "C-001", SkuName = "Child C", SourceType = "self_made" };
        db.Skus.AddRange(prod, child);
        var parentBom = new Bom { BomCode = "B001", ProductSkuId = prod.Id, Status = "active" };
        var childBom = new Bom { BomCode = "B002", ProductSkuId = child.Id, Status = "active" };
        db.Boms.AddRange(parentBom, childBom);
        await db.SaveChangesAsync();
        db.BomItems.Add(new BomItem { BomId = parentBom.Id, ChildSkuId = child.Id, Quantity = 2, Unit = "ea" });
        db.BomItems.Add(new BomItem { BomId = childBom.Id, ChildSkuId = prod.Id, Quantity = 1, Unit = "ea" }); // cycle back to root
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var root = await service.GetBomTreeAsync(prod.Id);

        Assert.NotNull(root);
        Assert.Equal(prod.Id, root!.SkuId);
        Assert.Single(root.Children);
        Assert.Equal(child.Id, root.Children[0].SkuId);
        Assert.Empty(root.Children[0].Children);
    }

    [Fact]
    public async Task GetLatestActiveRevisionAsync_ReturnsMaxVersion_WhenMultipleActive()
    {
        using var db = CreateInMemoryDbContext();
        var prod = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        db.Skus.Add(prod);
        db.Boms.AddRange(
            new Bom { BomCode = "B001", ProductSkuId = prod.Id, Version = "1.0", Status = "active", EffectiveDate = DateTime.UtcNow.AddDays(-2) },
            new Bom { BomCode = "B002", ProductSkuId = prod.Id, Version = "2.0", Status = "active", EffectiveDate = DateTime.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync();

        var service = new BomService(db);
        var dto = await service.GetLatestActiveRevisionAsync(prod.Id);

        Assert.NotNull(dto);
        Assert.Equal("2.0", dto!.Version);
    }
}
