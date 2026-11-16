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

public class WorkCenterServiceTests
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
    public async Task GetWorkCentersAsync_ReturnsOnlyActiveOrderedByCode()
    {
        using var db = CreateInMemoryDbContext();
        db.WorkCenters.AddRange(
            new WorkCenter { WorkCenterCode = "W002", WorkCenterName = "Beta", Capacity = 1, Efficiency = 90, IsActive = true },
            new WorkCenter { WorkCenterCode = "W001", WorkCenterName = "Alpha", Capacity = 2, Efficiency = 95, IsActive = true },
            new WorkCenter { WorkCenterCode = "W003", WorkCenterName = "Gamma", Capacity = 3, Efficiency = 80, IsActive = false });
        await db.SaveChangesAsync();

        var service = new WorkCenterService(db);
        var result = await service.GetWorkCentersAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("W001", result[0].WorkCenterCode);
    }

    [Fact]
    public async Task GetWorkCenterAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var wc = new WorkCenter { WorkCenterCode = "W001", WorkCenterName = "Alpha", Capacity = 2, Efficiency = 95, IsActive = true };
        db.WorkCenters.Add(wc);
        await db.SaveChangesAsync();

        var service = new WorkCenterService(db);
        var dto = await service.GetWorkCenterAsync(wc.Id);

        Assert.NotNull(dto);
        Assert.Equal("W001", dto!.WorkCenterCode);
    }

    [Fact]
    public async Task GetWorkCenterAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new WorkCenterService(db);
        Assert.Null(await service.GetWorkCenterAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateWorkCenterAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var service = new WorkCenterService(db);
        var req = new CreateWorkCenterRequest { WorkCenterCode = "W001", WorkCenterName = "Alpha", Capacity = 2, Efficiency = 95, IsActive = true };

        var (dto, status) = await service.CreateWorkCenterAsync(req);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Equal("W001", dto!.WorkCenterCode);
    }

    [Fact]
    public async Task CreateWorkCenterAsync_ReturnsInvalid_WhenCodeEmpty()
    {
        using var db = CreateInMemoryDbContext();
        var service = new WorkCenterService(db);
        var req = new CreateWorkCenterRequest { WorkCenterCode = "", WorkCenterName = "Alpha" };

        var (dto, status) = await service.CreateWorkCenterAsync(req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);
        Assert.Empty(db.WorkCenters);
    }

    [Fact]
    public async Task CreateWorkCenterAsync_ReturnsDuplicate_WhenCodeExists()
    {
        using var db = CreateInMemoryDbContext();
        db.WorkCenters.Add(new WorkCenter { WorkCenterCode = "W001", WorkCenterName = "Alpha" });
        await db.SaveChangesAsync();

        var service = new WorkCenterService(db);
        var req = new CreateWorkCenterRequest { WorkCenterCode = "W001", WorkCenterName = "Beta" };

        var (dto, status) = await service.CreateWorkCenterAsync(req);

        Assert.Null(dto);
        Assert.Equal("duplicate", status);
    }

    [Fact]
    public async Task UpdateWorkCenterAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var wc = new WorkCenter { WorkCenterCode = "W001", WorkCenterName = "Alpha", Capacity = 1, Efficiency = 90, IsActive = true };
        db.WorkCenters.Add(wc);
        await db.SaveChangesAsync();

        var service = new WorkCenterService(db);
        var req = new UpdateWorkCenterRequest { WorkCenterName = "Alpha Updated", Capacity = 5 };

        var (dto, status) = await service.UpdateWorkCenterAsync(wc.Id, req);

        Assert.Equal("ok", status);
        Assert.Equal("Alpha Updated", dto!.WorkCenterName);
        Assert.Equal(5, dto.Capacity);
    }

    [Fact]
    public async Task UpdateWorkCenterAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new WorkCenterService(db);

        var (dto, status) = await service.UpdateWorkCenterAsync(Guid.NewGuid(), new UpdateWorkCenterRequest());

        Assert.Null(dto);
        Assert.Equal("not_found", status);
    }
}
