using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class BinLocationServiceTests
{
    [Fact]
    public async Task CreateBinLocationAsync_CreatesLocationForActiveWarehouse()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var warehouse = new Warehouse { WarehouseCode = "WH-01", WarehouseName = "原料倉" };
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();
        var service = new WarehouseService(db);

        var (location, status) = await service.CreateBinLocationAsync(new CreateBinLocationRequestDto
        {
            WarehouseId = warehouse.Id,
            LocationCode = " A-01-01 ",
            Zone = " A "
        });

        Assert.Equal("ok", status);
        Assert.NotNull(location);
        Assert.Equal("A-01-01", location.LocationCode);
        Assert.Equal("A", location.Zone);
        Assert.Equal(warehouse.Id, location.WarehouseId);
        Assert.Single(await db.BinLocations.ToListAsync());
    }

    [Fact]
    public async Task CreateBinLocationAsync_RejectsDuplicateCodeInSameWarehouse()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var warehouse = new Warehouse { WarehouseCode = "WH-01", WarehouseName = "原料倉" };
        db.Warehouses.Add(warehouse);
        db.BinLocations.Add(new BinLocation
        {
            WarehouseId = warehouse.Id,
            LocationCode = "A-01-01"
        });
        await db.SaveChangesAsync();
        var service = new WarehouseService(db);

        var (location, status) = await service.CreateBinLocationAsync(new CreateBinLocationRequestDto
        {
            WarehouseId = warehouse.Id,
            LocationCode = "A-01-01"
        });

        Assert.Null(location);
        Assert.Equal("duplicate", status);
        Assert.Single(await db.BinLocations.ToListAsync());
    }

    [Fact]
    public async Task CreateBinLocationAsync_RejectsInactiveWarehouse()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var warehouse = new Warehouse
        {
            WarehouseCode = "WH-01",
            WarehouseName = "停用倉",
            IsActive = false
        };
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync();
        var service = new WarehouseService(db);

        var (location, status) = await service.CreateBinLocationAsync(new CreateBinLocationRequestDto
        {
            WarehouseId = warehouse.Id,
            LocationCode = "A-01-01"
        });

        Assert.Null(location);
        Assert.Equal("warehouse_not_found", status);
        Assert.Empty(await db.BinLocations.ToListAsync());
    }

    [Fact]
    public async Task UpdateBinLocationAsync_UpdatesLocationDetails()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var warehouse = new Warehouse { WarehouseCode = "WH-01", WarehouseName = "原料倉" };
        var binLocation = new BinLocation
        {
            WarehouseId = warehouse.Id,
            LocationCode = "A-01-01",
            Zone = "A"
        };
        db.AddRange(warehouse, binLocation);
        await db.SaveChangesAsync();
        var service = new WarehouseService(db);

        var (updated, status) = await service.UpdateBinLocationAsync(binLocation.Id, new UpdateBinLocationRequestDto
        {
            WarehouseId = warehouse.Id,
            LocationCode = "A-01-02",
            Zone = "B",
            Aisle = "02",
            Rack = "R03",
            Level = "04",
            Position = "P05"
        });

        Assert.Equal("ok", status);
        Assert.NotNull(updated);
        Assert.Equal("A-01-02", updated.LocationCode);
        Assert.Equal("B", updated.Zone);
        Assert.Equal("02", updated.Aisle);
        Assert.Equal("R03", updated.Rack);
        Assert.Equal("04", updated.Level);
        Assert.Equal("P05", updated.Position);
    }

    [Fact]
    public async Task UpdateBinLocationAsync_RejectsDuplicateCodeInSameWarehouse()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var warehouse = new Warehouse { WarehouseCode = "WH-01", WarehouseName = "原料倉" };
        var existing = new BinLocation
        {
            WarehouseId = warehouse.Id,
            LocationCode = "A-01-01"
        };
        var target = new BinLocation
        {
            WarehouseId = warehouse.Id,
            LocationCode = "A-01-02"
        };
        db.AddRange(warehouse, existing, target);
        await db.SaveChangesAsync();
        var service = new WarehouseService(db);

        var (updated, status) = await service.UpdateBinLocationAsync(target.Id, new UpdateBinLocationRequestDto
        {
            WarehouseId = warehouse.Id,
            LocationCode = "A-01-01"
        });

        Assert.Null(updated);
        Assert.Equal("duplicate", status);
        Assert.Equal("A-01-02", (await db.BinLocations.FindAsync(target.Id))!.LocationCode);
    }
}
