using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class EquipmentServiceTests
{
    [Fact]
    public async Task GetEquipmentAsync_ReturnsActiveEquipmentAndAppliesStatusFilter()
    {
        using var db = CreateInMemoryDbContext();
        db.Equipments.AddRange(
            new Equipment { EquipmentCode = "EQ-001", EquipmentName = "Press", Status = "active" },
            new Equipment { EquipmentCode = "EQ-002", EquipmentName = "Lathe", Status = "repair" },
            new Equipment { EquipmentCode = "EQ-003", EquipmentName = "Retired", IsActive = false, Status = "inactive" });
        await db.SaveChangesAsync();
        var service = new EquipmentService(db);

        var active = await service.GetEquipmentAsync("active", null, CancellationToken.None);
        var all = await service.GetEquipmentAsync(null, null, CancellationToken.None);

        Assert.Single(active);
        Assert.Equal("EQ-001", active[0].EquipmentCode);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task CreateEquipmentAsync_CreatesActiveEquipmentAndReturnsDto()
    {
        using var db = CreateInMemoryDbContext();
        var service = new EquipmentService(db);
        var request = new CreateEquipmentRequestDto
        {
            EquipmentCode = " EQ-100 ",
            EquipmentName = "Injection Machine",
            EquipmentType = "machine",
            PurchaseDate = new DateTime(2026, 10, 1)
        };

        var (equipment, status) = await service.CreateEquipmentAsync(request, CancellationToken.None);

        Assert.Equal("ok", status);
        Assert.NotNull(equipment);
        Assert.Equal("EQ-100", equipment.EquipmentCode);
        Assert.Equal("active", equipment.Status);
        Assert.Equal(DateTimeKind.Utc, equipment.PurchaseDate?.Kind);
        Assert.True(await db.Equipments.AnyAsync(item => item.Id == equipment.Id && item.IsActive));
    }

    [Fact]
    public async Task CreateEquipmentAsync_RejectsDuplicateEquipmentCode()
    {
        using var db = CreateInMemoryDbContext();
        db.Equipments.Add(new Equipment { EquipmentCode = "EQ-100", EquipmentName = "Existing" });
        await db.SaveChangesAsync();
        var service = new EquipmentService(db);

        var (equipment, status) = await service.CreateEquipmentAsync(
            new CreateEquipmentRequestDto { EquipmentCode = "EQ-100", EquipmentName = "Duplicate" },
            CancellationToken.None);

        Assert.Null(equipment);
        Assert.Equal("duplicate", status);
        Assert.Equal(1, await db.Equipments.CountAsync());
    }

    private static ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }
}
