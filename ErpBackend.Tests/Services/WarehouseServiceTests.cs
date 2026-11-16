using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class WarehouseServiceTests
{
    [Fact]
    public async Task CreateWarehouseAsync_CreatesWarehouseWithSubmittedDetails()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var service = new WarehouseService(db);

        var (warehouse, status) = await service.CreateWarehouseAsync(new CreateWarehouseRequestDto
        {
            WarehouseCode = " WH-01 ",
            WarehouseName = " 原料倉 ",
            Address = "一樓",
            Type = "normal",
            IsActive = true
        });

        Assert.Equal("ok", status);
        Assert.NotNull(warehouse);
        Assert.Equal("WH-01", warehouse.WarehouseCode);
        Assert.Equal("原料倉", warehouse.WarehouseName);
        Assert.Equal("一樓", warehouse.Address);
        Assert.Equal("normal", warehouse.Type);
        Assert.True(warehouse.IsActive);
        Assert.Equal(1, await db.Warehouses.CountAsync());
    }

    [Fact]
    public async Task CreateWarehouseAsync_RejectsDuplicateCode()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        db.Warehouses.Add(new Warehouse
        {
            WarehouseCode = "WH-01",
            WarehouseName = "原料倉"
        });
        await db.SaveChangesAsync();
        var service = new WarehouseService(db);

        var (warehouse, status) = await service.CreateWarehouseAsync(new CreateWarehouseRequestDto
        {
            WarehouseCode = "WH-01",
            WarehouseName = "重複倉庫"
        });

        Assert.Null(warehouse);
        Assert.Equal("duplicate", status);
        Assert.Equal(1, await db.Warehouses.CountAsync());
    }

    [Fact]
    public async Task GetWarehousesAsync_ExcludesInactiveUnlessRequested()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        db.Warehouses.AddRange(
            new Warehouse { WarehouseCode = "WH-01", WarehouseName = "啟用倉" },
            new Warehouse { WarehouseCode = "WH-02", WarehouseName = "停用倉", IsActive = false });
        await db.SaveChangesAsync();
        var service = new WarehouseService(db);

        var activeWarehouses = await service.GetWarehousesAsync();
        var allWarehouses = await service.GetWarehousesAsync(includeInactive: true);

        Assert.Single(activeWarehouses);
        Assert.Equal(2, allWarehouses.Count);
        Assert.False(allWarehouses.Single(item => item.WarehouseCode == "WH-02").IsActive);
    }
}
