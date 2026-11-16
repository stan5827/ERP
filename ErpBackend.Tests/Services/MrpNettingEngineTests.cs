using System;
using System.Collections.Generic;
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

public class MrpNettingEngineTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private sealed class NullSubstituteService : ISubstituteService
    {
        public Task<List<SubstituteItem>> GetAvailableSubstitutes(Guid skuId, decimal requiredQty, DateTime? requiredDate = null)
            => Task.FromResult<List<SubstituteItem>>(new List<SubstituteItem>());
        public Task<SubstituteItem?> FindBestSubstitute(Guid skuId, decimal requiredQty, Dictionary<string, object>? conditions = null)
            => Task.FromResult<SubstituteItem?>(null);
        public Task<bool> ValidateSubstitution(Guid originalSkuId, Guid substituteSkuId, decimal qty)
            => Task.FromResult(false);
        public Task LogSubstitution(Guid originalSkuId, Guid substituteSkuId, Guid workOrderId, decimal qty, string? reason, decimal costVariance = 0)
            => Task.CompletedTask;
        public Task<List<SubstituteUsageLog>> GetSubstitutionHistory(Guid skuId, DateTime? fromDate = null)
            => Task.FromResult<List<SubstituteUsageLog>>(new List<SubstituteUsageLog>());
        public Task<decimal> CalculateCostVariance(Guid originalSkuId, Guid substituteSkuId, decimal qty)
            => Task.FromResult(0m);
    }

    private static MrpNettingEngine CreateEngine(ErpGeneralDbContext db)
    {
        var reader = new MrpDataReader(db, NullLogger<MrpDataReader>.Instance);
        return new MrpNettingEngine(db, reader, new NullSubstituteService(), NullLogger<MrpNettingEngine>.Instance);
    }

    private static Sku NewSku(decimal qty, decimal reorderPoint, decimal safetyStock = 0, decimal moq = 0, int supplierLeadTime = 0) =>
        new() { SkuNumber = "S1", SkuName = "S1", Qty = qty, ReorderPoint = reorderPoint, SafetyStock = safetyStock, Moq = moq, SupplierLeadTime = supplierLeadTime };

    [Fact]
    public async Task CalculateRequirementsAsync_ReturnsEmpty_WhenNoDemandAndStockAboveReorderPoint()
    {
        using var db = CreateInMemoryDbContext();
        var sku = NewSku(qty: 50, reorderPoint: 20, safetyStock: 2); // 50 > 20 => no demand, no reorder
        db.Skus.Add(sku);
        await db.SaveChangesAsync();

        var engine = CreateEngine(db);

        var result = await engine.CalculateRequirementsAsync(sku.Id, DateTime.Today, DateTime.Today.AddDays(30), true);

        Assert.Empty(result);
    }

    [Fact]
    public async Task CalculateRequirementsAsync_ProducesSafetyStockDemand_CreateWo_NoSubstitution()
    {
        // Characterizes the LIVE path: Qty(5) <= ReorderPoint(10) drives the safety_stock
        // demand via the reader; substitute stubbed to null (no-substitution branch).
        using var db = CreateInMemoryDbContext();
        var sku = NewSku(qty: 5, reorderPoint: 10, safetyStock: 2, moq: 0); // moq 0 -> live path uses 1
        db.Skus.Add(sku);
        await db.SaveChangesAsync();

        var engine = CreateEngine(db);

        var result = await engine.CalculateRequirementsAsync(sku.Id, DateTime.Today, DateTime.Today.AddDays(30), true);

        var req = Assert.Single(result);
        Assert.Equal("safety_stock", req.RequirementType);
        Assert.Equal(5m, req.RequiredQty);      // ROP - Qty
        Assert.Equal(3m, req.AvailableQty);     // currentStock(5) - safetyStock(2)
        Assert.Equal(2m, req.ShortageQty);     // Max(0, 5 - 3)
        Assert.Equal(0m, req.PlannedReceiptQty); // default/unpopulated
        Assert.Equal(2m, req.NetRequirement);   // Max(moq=1, shortage=2)
        Assert.Equal("create_wo", req.ActionCode);
        Assert.Null(req.SubstituteSkuId);
        Assert.Null(req.Notes);                 // plannedReceiptQty<=0 and no substitute
    }

    [Fact]
    public async Task CalculateRequirementsWithCacheAsync_ProducesReorderPoint_CreatePr()
    {
        // Characterizes the CACHED path (Option A preserves the divergence from the live path):
        // with empty demand lists and currentStock(5) <= rop(10) => reorder_point / create_pr.
        var engine = CreateEngine(CreateInMemoryDbContext());
        var skuId = Guid.NewGuid();

        var cache = new DemandDataCache
        {
            SkuQtys = { [skuId] = 5 },
            SkuReorderPoints = { [skuId] = 10 }
        };

        var result = await engine.CalculateRequirementsWithCacheAsync(skuId, DateTime.Today, DateTime.Today.AddDays(30), true, cache);

        var req = Assert.Single(result);
        Assert.Equal("reorder_point", req.RequirementType);
        Assert.Equal("create_pr", req.ActionCode);
        Assert.Equal(15m, req.RequiredQty);   // Max(moq=1, (rop*2 - currentStock) = 20-5)
        Assert.Equal(5m, req.AvailableQty);   // cached path sets AvailableQty = currentStock (NOT currentStock-safetyStock)
        Assert.Equal(15m, req.NetRequirement);
        Assert.Equal(Guid.Empty, req.MrpRunId);
    }
}
