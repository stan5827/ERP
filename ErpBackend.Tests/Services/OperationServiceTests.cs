using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class OperationServiceTests
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
    public async Task GetOperationsAsync_ReturnsOnlyActiveOrderedByCode()
    {
        using var db = CreateInMemoryDbContext();
        db.Operations.AddRange(
            new Operation { OperationCode = "OP002", OperationName = "Beta", IsActive = true },
            new Operation { OperationCode = "OP001", OperationName = "Alpha", IsActive = true },
            new Operation { OperationCode = "OP003", OperationName = "Gamma", IsActive = false });
        await db.SaveChangesAsync();

        var service = new OperationService(db);
        var result = await service.GetOperationsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("OP001", result[0].OperationCode);
    }

    [Fact]
    public async Task GetOperationAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var op = new Operation { OperationCode = "OP001", OperationName = "Alpha", SetupTime = 1.5m, RunTime = 2.0m, CostPerHour = 50m, IsOutsourced = false, IsActive = true };
        db.Operations.Add(op);
        await db.SaveChangesAsync();

        var service = new OperationService(db);
        var dto = await service.GetOperationAsync(op.Id);

        Assert.NotNull(dto);
        Assert.Equal("OP001", dto!.OperationCode);
        Assert.Equal(1.5m, dto.SetupTime);
    }

    [Fact]
    public async Task GetOperationAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new OperationService(db);
        Assert.Null(await service.GetOperationAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateOperationAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var service = new OperationService(db);
        var req = new CreateOperationRequest { OperationCode = "OP001", OperationName = "Alpha", SetupTime = 1.5m, RunTime = 2.0m, CostPerHour = 50m, IsOutsourced = false, IsActive = true };

        var (dto, status) = await service.CreateOperationAsync(req);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Equal("OP001", dto!.OperationCode);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task CreateOperationAsync_ReturnsInvalid_WhenCodeEmpty()
    {
        using var db = CreateInMemoryDbContext();
        var service = new OperationService(db);
        var req = new CreateOperationRequest { OperationCode = "", OperationName = "Alpha" };

        var (dto, status) = await service.CreateOperationAsync(req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);
        Assert.Empty(db.Operations);
    }

    [Fact]
    public async Task CreateOperationAsync_ReturnsDuplicate_WhenCodeExists()
    {
        using var db = CreateInMemoryDbContext();
        db.Operations.Add(new Operation { OperationCode = "OP001", OperationName = "Alpha" });
        await db.SaveChangesAsync();

        var service = new OperationService(db);
        var req = new CreateOperationRequest { OperationCode = "OP001", OperationName = "Beta" };

        var (dto, status) = await service.CreateOperationAsync(req);

        Assert.Null(dto);
        Assert.Equal("duplicate", status);
    }

    [Fact]
    public async Task UpdateOperationAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var op = new Operation { OperationCode = "OP001", OperationName = "Alpha", SetupTime = 1.0m, IsActive = true };
        db.Operations.Add(op);
        await db.SaveChangesAsync();

        var service = new OperationService(db);
        var req = new UpdateOperationRequest { OperationName = "Alpha Updated", SetupTime = 2.0m };

        var (dto, status) = await service.UpdateOperationAsync(op.Id, req);

        Assert.Equal("ok", status);
        Assert.Equal("Alpha Updated", dto!.OperationName);
        Assert.Equal(2.0m, dto.SetupTime);
    }

    [Fact]
    public async Task UpdateOperationAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new OperationService(db);

        var (dto, status) = await service.UpdateOperationAsync(Guid.NewGuid(), new UpdateOperationRequest());

        Assert.Null(dto);
        Assert.Equal("not_found", status);
    }

    [Fact]
    public async Task DeleteOperationAsync_ReturnsOk_WhenNotUsed()
    {
        using var db = CreateInMemoryDbContext();
        var op = new Operation { OperationCode = "OP001", OperationName = "Alpha" };
        db.Operations.Add(op);
        await db.SaveChangesAsync();

        var service = new OperationService(db);
        var (ok, status) = await service.DeleteOperationAsync(op.Id);

        Assert.True(ok);
        Assert.Equal("ok", status);
        Assert.Empty(db.Operations);
    }

    [Fact]
    public async Task DeleteOperationAsync_ReturnsGuarded_WhenUsedInRouting()
    {
        using var db = CreateInMemoryDbContext();
        var op = new Operation { OperationCode = "OP001", OperationName = "Alpha" };
        db.Operations.Add(op);
        await db.SaveChangesAsync();
        db.RoutingOperations.Add(new RoutingOperation { OperationId = op.Id, RoutingId = Guid.NewGuid() });
        await db.SaveChangesAsync();

        var service = new OperationService(db);
        var (ok, status) = await service.DeleteOperationAsync(op.Id);

        Assert.False(ok);
        Assert.Equal("guarded", status);
        Assert.Single(db.Operations);
    }

    [Fact]
    public async Task DeleteOperationAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new OperationService(db);

        var (ok, status) = await service.DeleteOperationAsync(Guid.NewGuid());

        Assert.False(ok);
        Assert.Equal("not_found", status);
    }
}
