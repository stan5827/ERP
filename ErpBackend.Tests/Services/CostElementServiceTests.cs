using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class CostElementServiceTests
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
    public async Task GetCostElementsAsync_ReturnsAll_WhenNoFilters()
    {
        using var db = CreateInMemoryDbContext();
        db.CostElements.AddRange(
            new CostElement { ElementCode = "CE002", ElementName = "Beta", DisplayOrder = 2, IsActive = true },
            new CostElement { ElementCode = "CE001", ElementName = "Alpha", DisplayOrder = 1, IsActive = true });
        await db.SaveChangesAsync();

        var service = new CostElementService(db);
        var result = await service.GetCostElementsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("CE001", result[0].ElementCode);
    }

    [Fact]
    public async Task GetCostElementsAsync_FiltersBySearch()
    {
        using var db = CreateInMemoryDbContext();
        db.CostElements.AddRange(
            new CostElement { ElementCode = "CE001", ElementName = "Alpha" },
            new CostElement { ElementCode = "CE002", ElementName = "Beta" });
        await db.SaveChangesAsync();

        var service = new CostElementService(db);
        var result = await service.GetCostElementsAsync(search: "Alpha");

        Assert.Single(result);
        Assert.Equal("CE001", result[0].ElementCode);
    }

    [Fact]
    public async Task GetCostElementsAsync_FiltersByIsActive()
    {
        using var db = CreateInMemoryDbContext();
        db.CostElements.AddRange(
            new CostElement { ElementCode = "CE001", ElementName = "Alpha", IsActive = true },
            new CostElement { ElementCode = "CE002", ElementName = "Beta", IsActive = false });
        await db.SaveChangesAsync();

        var service = new CostElementService(db);
        var result = await service.GetCostElementsAsync(isActive: true);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetCostElementAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var element = new CostElement { ElementCode = "CE001", ElementName = "Alpha", ElementType = "direct" };
        db.CostElements.Add(element);
        await db.SaveChangesAsync();

        var service = new CostElementService(db);
        var dto = await service.GetCostElementAsync(element.Id);

        Assert.NotNull(dto);
        Assert.Equal("CE001", dto!.ElementCode);
        Assert.Equal("direct", dto.ElementType);
    }

    [Fact]
    public async Task GetCostElementAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CostElementService(db);
        Assert.Null(await service.GetCostElementAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateCostElementAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CostElementService(db);
        var req = new CreateCostElementRequest { ElementCode = "CE001", ElementName = "Alpha" };

        var (dto, status) = await service.CreateCostElementAsync(req);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Equal("CE001", dto!.ElementCode);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task CreateCostElementAsync_ReturnsInvalid_WhenCodeEmpty()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CostElementService(db);
        var req = new CreateCostElementRequest { ElementCode = "", ElementName = "Alpha" };

        var (dto, status) = await service.CreateCostElementAsync(req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);
        Assert.Empty(db.CostElements);
    }

    [Fact]
    public async Task CreateCostElementAsync_ReturnsDuplicate_WhenCodeExists()
    {
        using var db = CreateInMemoryDbContext();
        db.CostElements.Add(new CostElement { ElementCode = "CE001", ElementName = "Alpha" });
        await db.SaveChangesAsync();

        var service = new CostElementService(db);
        var req = new CreateCostElementRequest { ElementCode = "CE001", ElementName = "Beta" };

        var (dto, status) = await service.CreateCostElementAsync(req);

        Assert.Null(dto);
        Assert.Equal("duplicate", status);
    }

    [Fact]
    public async Task UpdateCostElementAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var element = new CostElement { ElementCode = "CE001", ElementName = "Alpha", ElementType = "direct" };
        db.CostElements.Add(element);
        await db.SaveChangesAsync();

        var service = new CostElementService(db);
        var req = new UpdateCostElementRequest { ElementName = "Alpha Updated", ElementType = "indirect" };

        var (dto, status) = await service.UpdateCostElementAsync(element.Id, req);

        Assert.Equal("ok", status);
        Assert.Equal("Alpha Updated", dto!.ElementName);
        Assert.Equal("indirect", dto.ElementType);
    }

    [Fact]
    public async Task UpdateCostElementAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CostElementService(db);

        var (dto, status) = await service.UpdateCostElementAsync(Guid.NewGuid(), new UpdateCostElementRequest());

        Assert.Null(dto);
        Assert.Equal("not_found", status);
    }

    [Fact]
    public async Task DeleteCostElementAsync_ReturnsOk_WhenNoAssignments()
    {
        using var db = CreateInMemoryDbContext();
        var element = new CostElement { ElementCode = "CE001", ElementName = "Alpha" };
        db.CostElements.Add(element);
        await db.SaveChangesAsync();

        var service = new CostElementService(db);
        var (ok, status) = await service.DeleteCostElementAsync(element.Id);

        Assert.True(ok);
        Assert.Equal("ok", status);
        Assert.Empty(db.CostElements);
    }

    [Fact]
    public async Task DeleteCostElementAsync_ReturnsGuarded_WhenAssignmentsExist()
    {
        using var db = CreateInMemoryDbContext();
        var element = new CostElement { ElementCode = "CE001", ElementName = "Alpha" };
        db.CostElements.Add(element);
        await db.SaveChangesAsync();
        db.CostElementAssignments.Add(new CostElementAssignment { CostElementId = element.Id, CostCenterId = Guid.NewGuid() });
        await db.SaveChangesAsync();

        var service = new CostElementService(db);
        var (ok, status) = await service.DeleteCostElementAsync(element.Id);

        Assert.False(ok);
        Assert.Equal("guarded", status);
        Assert.Single(db.CostElements);
    }

    [Fact]
    public async Task DeleteCostElementAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CostElementService(db);

        var (ok, status) = await service.DeleteCostElementAsync(Guid.NewGuid());

        Assert.False(ok);
        Assert.Equal("not_found", status);
    }
}
