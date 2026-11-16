using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class UomServiceTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    [Fact]
    public async Task GetUomsAsync_ReturnsEmpty_WhenNoData()
    {
        using var db = CreateInMemoryDbContext();
        var service = new UomService(db);

        var result = await service.GetUomsAsync(null, null, null);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUomsAsync_ReturnsAllActiveUomsOrdered()
    {
        using var db = CreateInMemoryDbContext();
        db.Uoms.AddRange(
            new Uom { UomCode = "EA", UomName = "Piece", UomType = "base", DisplayOrder = 2, IsActive = true },
            new Uom { UomCode = "KG", UomName = "Kilogram", UomType = "base", DisplayOrder = 1, IsActive = true },
            new Uom { UomCode = "BX", UomName = "Box", UomType = "length", DisplayOrder = 0, IsActive = false });
        await db.SaveChangesAsync();

        var service = new UomService(db);

        var result = await service.GetUomsAsync(null, null, null);

        Assert.Equal(3, result.Count);
        // Ordered by DisplayOrder then UomCode
        Assert.Equal("BX", result[0].UomCode);
        Assert.Equal("KG", result[1].UomCode);
        Assert.Equal("EA", result[2].UomCode);
    }

    [Fact]
    public async Task GetUomsAsync_FiltersByUomType()
    {
        using var db = CreateInMemoryDbContext();
        db.Uoms.AddRange(
            new Uom { UomCode = "EA", UomName = "Piece", UomType = "base", IsActive = true },
            new Uom { UomCode = "BX", UomName = "Box", UomType = "length", IsActive = true });
        await db.SaveChangesAsync();

        var service = new UomService(db);

        var result = await service.GetUomsAsync("length", null, null);

        Assert.Single(result);
        Assert.Equal("BX", result[0].UomCode);
    }

    [Fact]
    public async Task GetUomsAsync_FiltersByIsActive()
    {
        using var db = CreateInMemoryDbContext();
        db.Uoms.AddRange(
            new Uom { UomCode = "EA", UomName = "Piece", UomType = "base", IsActive = true },
            new Uom { UomCode = "BX", UomName = "Box", UomType = "length", IsActive = false });
        await db.SaveChangesAsync();

        var service = new UomService(db);

        var active = await service.GetUomsAsync(null, true, null);
        var inactive = await service.GetUomsAsync(null, false, null);

        Assert.Single(active);
        Assert.Equal("EA", active[0].UomCode);
        Assert.Single(inactive);
        Assert.Equal("BX", inactive[0].UomCode);
    }

    [Fact]
    public async Task GetUomsAsync_FiltersBySearchTerm()
    {
        using var db = CreateInMemoryDbContext();
        db.Uoms.AddRange(
            new Uom { UomCode = "EA", UomName = "Piece", UomType = "base", IsActive = true },
            new Uom { UomCode = "KG", UomName = "Kilogram", UomType = "base", IsActive = true });
        await db.SaveChangesAsync();

        var service = new UomService(db);

        var result = await service.GetUomsAsync(null, null, "Kilo");

        Assert.Single(result);
        Assert.Equal("KG", result[0].UomCode);
    }

    [Fact]
    public async Task GetUomsAsync_MapsAllDtoFields()
    {
        using var db = CreateInMemoryDbContext();
        db.Uoms.Add(new Uom
        {
            UomCode = "EA",
            UomName = "Piece",
            UomNameEn = "Piece",
            UomType = "base",
            DecimalPlaces = 0,
            IsActive = true,
            AllowDecimal = false,
            Description = "Each",
            DisplayOrder = 1
        });
        await db.SaveChangesAsync();

        var service = new UomService(db);

        var result = await service.GetUomsAsync(null, null, null);

        var dto = result[0];
        Assert.Equal("EA", dto.UomCode);
        Assert.Equal("Piece", dto.UomName);
        Assert.Equal("Piece", dto.UomNameEn);
        Assert.Equal("base", dto.UomType);
        Assert.Equal(0, dto.DecimalPlaces);
        Assert.True(dto.IsActive);
        Assert.False(dto.AllowDecimal);
        Assert.Equal("Each", dto.Description);
        Assert.Equal(1, dto.DisplayOrder);
    }

    [Fact]
    public async Task GetUomAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var uom = new Uom { UomCode = "EA", UomName = "Piece", UomType = "base", IsActive = true };
        db.Uoms.Add(uom);
        await db.SaveChangesAsync();

        var service = new UomService(db);

        var result = await service.GetUomAsync(uom.Id);

        Assert.NotNull(result);
        Assert.Equal("EA", result!.UomCode);
        Assert.Equal("Piece", result.UomName);
    }

    [Fact]
    public async Task GetUomAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new UomService(db);

        var result = await service.GetUomAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
