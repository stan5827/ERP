using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class RccpResourceServiceTests
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
    public async Task ListResourcesAsync_ReturnsAllResourcesWithWorkCenters()
    {
        using var db = CreateInMemoryDbContext();
        var wc = new WorkCenter { Id = Guid.NewGuid(), WorkCenterCode = "WC-001", WorkCenterName = "Assembly", Capacity = 100, Efficiency = 90, IsActive = true };
        db.WorkCenters.Add(wc);
        db.RccpResources.AddRange(
            new RccpResource { Id = Guid.NewGuid(), ResourceCode = "RES-001", ResourceName = "Machine A", CapacityPerBucket = 40, WorkCenterId = wc.Id },
            new RccpResource { Id = Guid.NewGuid(), ResourceCode = "RES-002", ResourceName = "Machine B", CapacityPerBucket = 30 });
        await db.SaveChangesAsync();

        var service = new RccpResourceService(db);
        var result = await service.ListResourcesAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.ResourceCode == "RES-001" && r.WorkCenter != null && r.WorkCenter.WorkCenterCode == "WC-001");
        Assert.Contains(result, r => r.ResourceCode == "RES-002" && r.WorkCenter == null);
    }

    [Fact]
    public async Task CreateResourceAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var service = new RccpResourceService(db);
        var model = new RccpResource { ResourceCode = "RES-001", ResourceName = "Machine A", CapacityPerBucket = 40 };

        var (resource, status) = await service.CreateResourceAsync(model);

        Assert.Equal("ok", status);
        Assert.NotNull(resource);
        Assert.Equal("RES-001", resource!.ResourceCode);
        Assert.NotEqual(Guid.Empty, resource.Id);
    }

    [Fact]
    public async Task CreateResourceAsync_ReturnsDuplicate_WhenCodeExists()
    {
        using var db = CreateInMemoryDbContext();
        db.RccpResources.Add(new RccpResource { Id = Guid.NewGuid(), ResourceCode = "RES-001", ResourceName = "Machine A", CapacityPerBucket = 40 });
        await db.SaveChangesAsync();

        var service = new RccpResourceService(db);
        var model = new RccpResource { ResourceCode = "RES-001", ResourceName = "Machine B", CapacityPerBucket = 50 };

        var (resource, status) = await service.CreateResourceAsync(model);

        Assert.Equal("duplicate", status);
        Assert.Null(resource);
        Assert.Single(db.RccpResources);
    }

    [Fact]
    public async Task CreateResourceAsync_ReturnsInvalid_WhenModelNull()
    {
        using var db = CreateInMemoryDbContext();
        var service = new RccpResourceService(db);

        var (resource, status) = await service.CreateResourceAsync(null!);

        Assert.Equal("invalid", status);
        Assert.Null(resource);
    }

    [Fact]
    public async Task UpdateResourceAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var id = Guid.NewGuid();
        db.RccpResources.Add(new RccpResource { Id = id, ResourceCode = "RES-001", ResourceName = "Machine A", CapacityPerBucket = 40 });
        await db.SaveChangesAsync();

        var service = new RccpResourceService(db);
        var model = new RccpResource { Id = id, ResourceCode = "RES-001", ResourceName = "Machine A Updated", CapacityPerBucket = 50 };

        var (resource, status) = await service.UpdateResourceAsync(id, model);

        Assert.Equal("ok", status);
        Assert.NotNull(resource);
        Assert.Equal("Machine A Updated", resource!.ResourceName);
        Assert.Equal(50, resource.CapacityPerBucket);
    }

    [Fact]
    public async Task UpdateResourceAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new RccpResourceService(db);
        var id = Guid.NewGuid();
        var model = new RccpResource { Id = id, ResourceCode = "RES-001", ResourceName = "Machine A" };

        var (resource, status) = await service.UpdateResourceAsync(id, model);

        Assert.Equal("not_found", status);
        Assert.Null(resource);
    }

    [Fact]
    public async Task UpdateResourceAsync_ReturnsInvalid_WhenIdMismatch()
    {
        using var db = CreateInMemoryDbContext();
        var service = new RccpResourceService(db);
        var model = new RccpResource { Id = Guid.NewGuid(), ResourceCode = "RES-001", ResourceName = "Machine A" };

        var (resource, status) = await service.UpdateResourceAsync(Guid.NewGuid(), model);

        Assert.Equal("invalid", status);
        Assert.Null(resource);
    }

    [Fact]
    public async Task GetItemResourcesAsync_ReturnsResourcesForSku()
    {
        using var db = CreateInMemoryDbContext();
        var skuId = Guid.NewGuid();
        var resId = Guid.NewGuid();
        db.RccpResources.Add(new RccpResource { Id = resId, ResourceCode = "RES-001", ResourceName = "Machine A", CapacityPerBucket = 40 });
        db.RccpItemResources.AddRange(
            new RccpItemResource { Id = Guid.NewGuid(), SkuId = skuId, ResourceId = resId, UsagePerUnit = 0.5m },
            new RccpItemResource { Id = Guid.NewGuid(), SkuId = Guid.NewGuid(), ResourceId = resId, UsagePerUnit = 0.3m });
        await db.SaveChangesAsync();

        var service = new RccpResourceService(db);
        var result = await service.GetItemResourcesAsync(skuId);

        Assert.Single(result);
        Assert.Equal(0.5m, result[0].UsagePerUnit);
        Assert.Equal("RES-001", result[0].Resource!.ResourceCode);
    }

    [Fact]
    public async Task UpdateItemResourcesAsync_ReturnsSuccess_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var skuId = Guid.NewGuid();
        db.RccpItemResources.Add(new RccpItemResource { Id = Guid.NewGuid(), SkuId = skuId, ResourceId = Guid.NewGuid(), UsagePerUnit = 0.5m });
        await db.SaveChangesAsync();

        var service = new RccpResourceService(db);
        var items = new List<RccpItemResource>
        {
            new() { SkuId = skuId, ResourceId = Guid.NewGuid(), UsagePerUnit = 0.8m },
            new() { SkuId = skuId, ResourceId = Guid.NewGuid(), UsagePerUnit = 0.2m }
        };

        var (success, message) = await service.UpdateItemResourcesAsync(items);

        Assert.True(success);
        Assert.Equal("Saved successfully", message);
        var saved = await db.RccpItemResources.Where(r => r.SkuId == skuId).ToListAsync();
        Assert.Equal(2, saved.Count);
    }

    [Fact]
    public async Task UpdateItemResourcesAsync_ReturnsFailure_WhenItemsEmpty()
    {
        using var db = CreateInMemoryDbContext();
        var service = new RccpResourceService(db);

        var (success, message) = await service.UpdateItemResourcesAsync(new List<RccpItemResource>());

        Assert.False(success);
        Assert.Equal("No items provided", message);
    }

    [Fact]
    public async Task UpdateItemResourcesAsync_ReturnsFailure_WhenItemsNull()
    {
        using var db = CreateInMemoryDbContext();
        var service = new RccpResourceService(db);

        var (success, message) = await service.UpdateItemResourcesAsync(null!);

        Assert.False(success);
        Assert.Equal("No items provided", message);
    }
}
