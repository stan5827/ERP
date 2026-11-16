using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class PlannedOrderEngineTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static PlannedOrderEngine CreateEngine(ErpGeneralDbContext db) =>
        new(db, NullLogger<PlannedOrderEngine>.Instance);

    private static Sku NewSku(string code, decimal moq = 0, string makeOrBuy = "make", int leadTime = 7) =>
        new() { SkuNumber = code, SkuName = code + "-Name", Moq = moq, MakeOrBuy = makeOrBuy, SupplierLeadTime = leadTime };

    [Fact]
    public void CalculateOrderQuantity_ReturnsNetRequirement_WhenNoMoq()
    {
        var engine = CreateEngine(CreateInMemoryDbContext());
        var sku = NewSku("S1", moq: 0);
        Assert.Equal(10, engine.CalculateOrderQuantity(10, sku));
    }

    [Fact]
    public void CalculateOrderQuantity_RoundsUpToMoq_WhenBelowMoq()
    {
        var engine = CreateEngine(CreateInMemoryDbContext());
        var sku = NewSku("S1", moq: 20);
        Assert.Equal(20, engine.CalculateOrderQuantity(5, sku));
    }

    [Fact]
    public void CalculateOrderQuantity_ReturnsNetRequirement_WhenAboveMoq()
    {
        var engine = CreateEngine(CreateInMemoryDbContext());
        var sku = NewSku("S1", moq: 10);
        Assert.Equal(25, engine.CalculateOrderQuantity(25, sku));
    }

    [Fact]
    public void CalculateOrderQuantity_ReturnsNetRequirement_WhenSkuNull()
    {
        var engine = CreateEngine(CreateInMemoryDbContext());
        Assert.Equal(7, engine.CalculateOrderQuantity(7, null));
    }

    [Fact]
    public async Task CalculateProductionLeadTimeAsync_Returns7_WhenNoRouting()
    {
        var engine = CreateEngine(CreateInMemoryDbContext());
        var result = await engine.CalculateProductionLeadTimeAsync(Guid.NewGuid(), 2);
        Assert.Equal(7, result);
    }

    [Fact]
    public async Task CalculateProductionLeadTimeAsync_SumsSetupAndRunTime()
    {
        using var db = CreateInMemoryDbContext();
        var op = new Operation { OperationCode = "OP1", OperationName = "Assemble", SetupTime = 8, RunTime = 0.5m };
        var routing = new Routing
        {
            RoutingCode = "R1",
            RoutingName = "R1",
            ProductSkuId = Guid.NewGuid(),
            IsActive = true,
            Operations = { new RoutingOperation { OperationSeq = 1, Operation = op } }
        };
        db.Routings.Add(routing);
        await db.SaveChangesAsync();

        var engine = CreateEngine(db);
        var result = await engine.CalculateProductionLeadTimeAsync(routing.ProductSkuId, 2);

        // setup 8 + run 0.5 * qty 2 = 9 hours => ceil(9/8) = 2 days
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task ExplodeBomAsync_AddsDependentRequirement_ForBomItems()
    {
        using var db = CreateInMemoryDbContext();
        var parentSku = NewSku("P");
        var childSku = NewSku("C");
        var bom = new Bom
        {
            BomCode = "BOM-P",
            ProductSkuId = parentSku.Id,
            Status = "active",
            Items = { new BomItem { ChildSkuId = childSku.Id, Quantity = 3, Unit = "EA" } }
        };
        db.Skus.AddRange(parentSku, childSku);
        db.Boms.Add(bom);
        await db.SaveChangesAsync();

        var engine = CreateEngine(db);
        var plannedOrder = new MrpPlannedOrder { SkuId = parentSku.Id, OrderQty = 2 };
        var visited = new HashSet<Guid> { parentSku.Id };

        await engine.ExplodeBomAsync(plannedOrder, Guid.NewGuid(), 0, visited);
        await db.SaveChangesAsync(); // engine must NOT SaveChanges itself

        var req = await db.MrpRequirements.FirstOrDefaultAsync(r => r.SkuId == childSku.Id);
        Assert.NotNull(req);
        Assert.Equal("dependent", req!.RequirementType);
        Assert.Equal(6, req.RequiredQty); // item.Quantity(3) * parentOrder.OrderQty(2)
        Assert.Equal(plannedOrder.Id, req.SourceDocumentId);
        Assert.True(req.Notes?.Contains("層級") == true);
    }

    [Fact]
    public async Task ExplodeBomAsync_SkipsCycle_WhenChildAlreadyVisited()
    {
        using var db = CreateInMemoryDbContext();
        var parentSku = NewSku("P");
        var childSku = NewSku("C");
        var bom = new Bom
        {
            BomCode = "BOM-P",
            ProductSkuId = parentSku.Id,
            Status = "active",
            Items = { new BomItem { ChildSkuId = childSku.Id, Quantity = 3, Unit = "EA" } }
        };
        db.Skus.AddRange(parentSku, childSku);
        db.Boms.Add(bom);
        await db.SaveChangesAsync();

        var engine = CreateEngine(db);
        var plannedOrder = new MrpPlannedOrder { SkuId = parentSku.Id, OrderQty = 2 };
        var visited = new HashSet<Guid> { parentSku.Id, childSku.Id }; // child already in path => cycle guard

        await engine.ExplodeBomAsync(plannedOrder, Guid.NewGuid(), 0, visited);
        await db.SaveChangesAsync();

        Assert.Equal(0, await db.MrpRequirements.CountAsync());
    }

    [Fact]
    public async Task ExplodeBomAsync_NoOps_WhenBomNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var engine = CreateEngine(db);
        var plannedOrder = new MrpPlannedOrder { SkuId = Guid.NewGuid(), OrderQty = 2 };

        await engine.ExplodeBomAsync(plannedOrder, Guid.NewGuid(), 0, new HashSet<Guid>());

        Assert.Equal(0, await db.MrpRequirements.CountAsync());
    }
}
