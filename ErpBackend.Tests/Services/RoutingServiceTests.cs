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

public class RoutingServiceTests
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
    public async Task GetRoutingsAsync_FiltersByProductSkuId()
    {
        using var db = CreateInMemoryDbContext();
        var sku1 = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var sku2 = new Sku { SkuNumber = "P-002", SkuName = "Product Q" };
        db.Skus.AddRange(sku1, sku2);
        db.Routings.AddRange(
            new Routing { RoutingCode = "R001", RoutingName = "A", ProductSkuId = sku1.Id },
            new Routing { RoutingCode = "R002", RoutingName = "B", ProductSkuId = sku2.Id });
        await db.SaveChangesAsync();

        var service = new RoutingService(db);
        var result = await service.GetRoutingsAsync(sku1.Id);

        Assert.Single(result);
        Assert.Equal("R001", result[0].RoutingCode);
    }

    [Fact]
    public async Task GetRoutingsAsync_FiltersByOperationIdAndActiveRouting()
    {
        using var db = CreateInMemoryDbContext();
        var sku1 = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var sku2 = new Sku { SkuNumber = "P-002", SkuName = "Product Q" };
        var workCenter = new WorkCenter { WorkCenterCode = "WC1", WorkCenterName = "Center 1" };
        var matchingOperation = new Operation { OperationCode = "OP1", OperationName = "Injection" };
        var otherOperation = new Operation { OperationCode = "OP2", OperationName = "Packing" };
        db.Skus.AddRange(sku1, sku2);
        db.WorkCenters.Add(workCenter);
        db.Operations.AddRange(matchingOperation, otherOperation);
        var matchingRouting = new Routing { RoutingCode = "R001", RoutingName = "Active route", ProductSkuId = sku1.Id, IsActive = true };
        var inactiveRouting = new Routing { RoutingCode = "R002", RoutingName = "Inactive route", ProductSkuId = sku2.Id, IsActive = false };
        var otherRouting = new Routing { RoutingCode = "R003", RoutingName = "Other route", ProductSkuId = sku2.Id, IsActive = true };
        db.Routings.AddRange(matchingRouting, inactiveRouting, otherRouting);
        await db.SaveChangesAsync();
        db.RoutingOperations.AddRange(
            new RoutingOperation { RoutingId = matchingRouting.Id, OperationSeq = 10, OperationId = matchingOperation.Id, WorkCenterId = workCenter.Id },
            new RoutingOperation { RoutingId = inactiveRouting.Id, OperationSeq = 10, OperationId = matchingOperation.Id, WorkCenterId = workCenter.Id },
            new RoutingOperation { RoutingId = otherRouting.Id, OperationSeq = 10, OperationId = otherOperation.Id, WorkCenterId = workCenter.Id });
        await db.SaveChangesAsync();

        var service = new RoutingService(db);
        var result = await service.GetRoutingsAsync(operationId: matchingOperation.Id);

        var routing = Assert.Single(result);
        Assert.Equal("R001", routing.RoutingCode);
        Assert.Equal(sku1.Id, routing.ProductSkuId);
    }

    [Fact]
    public async Task GetRoutingsAsync_MapsOperationsWithIncludes()
    {
        using var db = CreateInMemoryDbContext();
        var sku = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var wc = new WorkCenter { WorkCenterCode = "WC1", WorkCenterName = "Center 1" };
        var op1 = new Operation { OperationCode = "OP1", OperationName = "Op One" };
        var op2 = new Operation { OperationCode = "OP2", OperationName = "Op Two" };
        db.Skus.Add(sku);
        db.WorkCenters.Add(wc);
        db.Operations.AddRange(op1, op2);
        var routing = new Routing { RoutingCode = "R001", RoutingName = "A", ProductSkuId = sku.Id };
        db.Routings.Add(routing);
        await db.SaveChangesAsync();
        db.RoutingOperations.AddRange(
            new RoutingOperation { RoutingId = routing.Id, OperationSeq = 20, OperationId = op2.Id, WorkCenterId = wc.Id },
            new RoutingOperation { RoutingId = routing.Id, OperationSeq = 10, OperationId = op1.Id, WorkCenterId = wc.Id, TransferTime = 1.5m, WaitTime = 2m });
        await db.SaveChangesAsync();

        var service = new RoutingService(db);
        var result = await service.GetRoutingsAsync(sku.Id);

        Assert.Single(result);
        Assert.Equal(2, result[0].Operations.Count);
        Assert.Equal("OP1", result[0].Operations[0].OperationCode);
        Assert.Equal(10, result[0].Operations[0].OperationSeq);
        Assert.Equal("OP2", result[0].Operations[1].OperationCode);
        Assert.Equal(20, result[0].Operations[1].OperationSeq);
        Assert.Equal("WC1", result[0].Operations[0].WorkCenterCode);
    }

    [Fact]
    public async Task GetRoutingAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var sku = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        db.Skus.Add(sku);
        var routing = new Routing { RoutingCode = "R001", RoutingName = "A", ProductSkuId = sku.Id };
        db.Routings.Add(routing);
        await db.SaveChangesAsync();

        var service = new RoutingService(db);
        var dto = await service.GetRoutingAsync(routing.Id);

        Assert.NotNull(dto);
        Assert.Equal("R001", dto!.RoutingCode);
        Assert.Equal("P-001", dto.ProductSkuNumber);
    }

    [Fact]
    public async Task GetRoutingAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new RoutingService(db);

        Assert.Null(await service.GetRoutingAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateRoutingAsync_ReturnsOk_AndPersistsOperations()
    {
        using var db = CreateInMemoryDbContext();
        var sku = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var wc = new WorkCenter { WorkCenterCode = "WC1", WorkCenterName = "Center 1" };
        var op = new Operation { OperationCode = "OP1", OperationName = "Op One" };
        db.Skus.Add(sku);
        db.WorkCenters.Add(wc);
        db.Operations.Add(op);
        await db.SaveChangesAsync();

        var service = new RoutingService(db);
        var req = new CreateRoutingRequest
        {
            RoutingCode = "R001",
            RoutingName = "A",
            ProductSkuId = sku.Id,
            IsActive = true,
            Operations = new() { new CreateRoutingOperationRequest { OperationSeq = 10, OperationId = op.Id, WorkCenterId = wc.Id } }
        };

        var (dto, status) = await service.CreateRoutingAsync(req);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Single(dto!.Operations);
        Assert.Equal(10, dto.Operations[0].OperationSeq);
        Assert.Equal(1, await db.RoutingOperations.CountAsync());
    }

    [Fact]
    public async Task UpdateRoutingAsync_ReturnsOk_AndReplacesOperations()
    {
        using var db = CreateInMemoryDbContext();
        var sku = new Sku { SkuNumber = "P-001", SkuName = "Product P" };
        var wc = new WorkCenter { WorkCenterCode = "WC1", WorkCenterName = "Center 1" };
        var op = new Operation { OperationCode = "OP1", OperationName = "Op One" };
        var routing = new Routing { RoutingCode = "R001", RoutingName = "A", ProductSkuId = sku.Id };
        db.Skus.Add(sku);
        db.WorkCenters.Add(wc);
        db.Operations.Add(op);
        db.Routings.Add(routing);
        await db.SaveChangesAsync();
        db.RoutingOperations.Add(new RoutingOperation { RoutingId = routing.Id, OperationSeq = 10, OperationId = op.Id, WorkCenterId = wc.Id, TransferTime = 0, WaitTime = 0 });
        await db.SaveChangesAsync();

        var service = new RoutingService(db);
        var req = new UpdateRoutingRequest
        {
            RoutingName = "A Updated",
            Operations = new() { new CreateRoutingOperationRequest { OperationSeq = 20, OperationId = op.Id, WorkCenterId = wc.Id } }
        };

        var (dto, status) = await service.UpdateRoutingAsync(routing.Id, req);

        Assert.Equal("ok", status);
        Assert.Equal("A Updated", dto!.RoutingName);
        Assert.Single(dto.Operations);
        Assert.Equal(20, dto.Operations[0].OperationSeq);
    }

    [Fact]
    public async Task UpdateRoutingAsync_UpdatesProductSku()
    {
        using var db = CreateInMemoryDbContext();
        var originalSku = new Sku { SkuNumber = "P-001", SkuName = "Original product" };
        var updatedSku = new Sku { SkuNumber = "P-002", SkuName = "Updated product" };
        db.Skus.AddRange(originalSku, updatedSku);
        var routing = new Routing
        {
            RoutingCode = "R001",
            RoutingName = "Routing",
            ProductSkuId = originalSku.Id
        };
        db.Routings.Add(routing);
        await db.SaveChangesAsync();

        var service = new RoutingService(db);
        var (dto, status) = await service.UpdateRoutingAsync(routing.Id, new UpdateRoutingRequest
        {
            ProductSkuId = updatedSku.Id
        });

        Assert.Equal("ok", status);
        Assert.Equal(updatedSku.Id, dto!.ProductSkuId);
        Assert.Equal("P-002", dto.ProductSkuNumber);
    }

    [Fact]
    public async Task UpdateRoutingAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new RoutingService(db);

        var (dto, status) = await service.UpdateRoutingAsync(Guid.NewGuid(), new UpdateRoutingRequest());

        Assert.Null(dto);
        Assert.Equal("not_found", status);
    }
}
