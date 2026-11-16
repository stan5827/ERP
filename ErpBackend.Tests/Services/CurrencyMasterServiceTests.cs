using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class CurrencyMasterServiceTests
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
    public async Task GetCurrenciesAsync_OrdersBaseCurrencyFirst()
    {
        using var db = CreateInMemoryDbContext();
        db.Currencies.AddRange(
            new CurrencyMaster { CurrencyCode = "USD", CurrencyName = "US Dollar", IsBaseCurrency = false },
            new CurrencyMaster { CurrencyCode = "TWD", CurrencyName = "TWD Dollar", IsBaseCurrency = true });
        await db.SaveChangesAsync();

        var service = new CurrencyMasterService(db);
        var result = await service.GetCurrenciesAsync();

        Assert.Equal(2, result.Count);
        Assert.True(result[0].IsBaseCurrency);
        Assert.Equal("TWD", result[0].CurrencyCode);
    }

    [Fact]
    public async Task GetCurrencyAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var c = new CurrencyMaster { CurrencyCode = "USD", CurrencyName = "US Dollar" };
        db.Currencies.Add(c);
        await db.SaveChangesAsync();

        var service = new CurrencyMasterService(db);
        var dto = await service.GetCurrencyAsync(c.Id);

        Assert.NotNull(dto);
        Assert.Equal("USD", dto!.CurrencyCode);
    }

    [Fact]
    public async Task GetCurrencyAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CurrencyMasterService(db);

        Assert.Null(await service.GetCurrencyAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateCurrencyAsync_ReturnsOk_AndResetsOtherBaseCurrencies()
    {
        using var db = CreateInMemoryDbContext();
        var twd = new CurrencyMaster { CurrencyCode = "TWD", CurrencyName = "TWD Dollar", IsBaseCurrency = true };
        db.Currencies.Add(twd);
        await db.SaveChangesAsync();

        var service = new CurrencyMasterService(db);
        var req = new CurrencyDto { CurrencyCode = "USD", CurrencyName = "US Dollar", IsBaseCurrency = true, IsActive = true };

        var (dto, status) = await service.CreateCurrencyAsync(req);

        Assert.Equal("ok", status);
        Assert.True(dto!.IsBaseCurrency);
        Assert.False((await db.Currencies.FirstAsync(c => c.CurrencyCode == "TWD")).IsBaseCurrency);
        Assert.True((await db.Currencies.FirstAsync(c => c.CurrencyCode == "USD")).IsBaseCurrency);
    }

    [Fact]
    public async Task CreateCurrencyAsync_DoesNotReset_WhenNotBaseCurrency()
    {
        using var db = CreateInMemoryDbContext();
        var twd = new CurrencyMaster { CurrencyCode = "TWD", CurrencyName = "TWD Dollar", IsBaseCurrency = true };
        db.Currencies.Add(twd);
        await db.SaveChangesAsync();

        var service = new CurrencyMasterService(db);
        var req = new CurrencyDto { CurrencyCode = "USD", CurrencyName = "US Dollar", IsBaseCurrency = false, IsActive = true };

        var (dto, status) = await service.CreateCurrencyAsync(req);

        Assert.Equal("ok", status);
        Assert.False(dto!.IsBaseCurrency);
        Assert.True((await db.Currencies.FirstAsync(c => c.CurrencyCode == "TWD")).IsBaseCurrency);
    }

    [Fact]
    public async Task UpdateCurrencyAsync_ReturnsOk_AndResetsOtherBaseCurrencies()
    {
        using var db = CreateInMemoryDbContext();
        var twd = new CurrencyMaster { CurrencyCode = "TWD", CurrencyName = "TWD Dollar", IsBaseCurrency = true };
        var usd = new CurrencyMaster { CurrencyCode = "USD", CurrencyName = "US Dollar", IsBaseCurrency = false };
        db.Currencies.AddRange(twd, usd);
        await db.SaveChangesAsync();

        var service = new CurrencyMasterService(db);
        var req = new CurrencyDto { CurrencyCode = "USD", CurrencyName = "US Dollar", IsBaseCurrency = true, IsActive = true };

        var (dto, status) = await service.UpdateCurrencyAsync(usd.Id, req);

        Assert.Equal("ok", status);
        Assert.True(dto!.IsBaseCurrency);
        Assert.False((await db.Currencies.FirstAsync(c => c.Id == twd.Id)).IsBaseCurrency);
    }

    [Fact]
    public async Task UpdateCurrencyAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CurrencyMasterService(db);

        var (dto, status) = await service.UpdateCurrencyAsync(Guid.NewGuid(), new CurrencyDto { CurrencyCode = "USD" });

        Assert.Null(dto);
        Assert.Equal("not_found", status);
    }
}
