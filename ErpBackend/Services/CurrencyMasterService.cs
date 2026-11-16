using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface ICurrencyMasterService
{
    Task<List<CurrencyDto>> GetCurrenciesAsync();
    Task<CurrencyDto?> GetCurrencyAsync(Guid id);
    Task<(CurrencyDto? dto, string status)> CreateCurrencyAsync(CurrencyDto model);
    Task<(CurrencyDto? dto, string status)> UpdateCurrencyAsync(Guid id, CurrencyDto model);
}

public class CurrencyMasterService : ICurrencyMasterService
{
    private readonly ErpGeneralDbContext _db;

    public CurrencyMasterService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<CurrencyDto>> GetCurrenciesAsync() =>
        await _db.Currencies.AsNoTracking()
            .OrderByDescending(c => c.IsBaseCurrency)
            .ThenBy(c => c.CurrencyCode)
            .Select(c => ToDto(c))
            .ToListAsync();

    public async Task<CurrencyDto?> GetCurrencyAsync(Guid id)
    {
        var c = await _db.Currencies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return c == null ? null : ToDto(c);
    }

    public async Task<(CurrencyDto? dto, string status)> CreateCurrencyAsync(CurrencyDto model)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (model.IsBaseCurrency)
                await ResetOtherBaseCurrenciesAsync(null);

            var c = new CurrencyMaster
            {
                CurrencyCode = model.CurrencyCode!,
                CurrencyName = model.CurrencyName!,
                Symbol = model.Symbol ?? "$",
                DecimalPlaces = model.DecimalPlaces,
                IsBaseCurrency = model.IsBaseCurrency,
                IsActive = model.IsActive
            };

            _db.Currencies.Add(c);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (ToDto(c), "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(CurrencyDto? dto, string status)> UpdateCurrencyAsync(Guid id, CurrencyDto model)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var c = await _db.Currencies.FirstOrDefaultAsync(c => c.Id == id);
            if (c == null) return (null, "not_found");

            if (model.IsBaseCurrency)
                await ResetOtherBaseCurrenciesAsync(c);

            c.CurrencyCode = model.CurrencyCode!;
            c.CurrencyName = model.CurrencyName!;
            c.Symbol = model.Symbol ?? "$";
            c.DecimalPlaces = model.DecimalPlaces;
            c.IsBaseCurrency = model.IsBaseCurrency;
            c.IsActive = model.IsActive;
            c.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (ToDto(c), "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task ResetOtherBaseCurrenciesAsync(CurrencyMaster? except)
    {
        var bases = await _db.Currencies
            .Where(c => c.IsBaseCurrency && (except == null || c.Id != except.Id))
            .ToListAsync();
        foreach (var b in bases) b.IsBaseCurrency = false;
    }

    private static CurrencyDto ToDto(CurrencyMaster c) => new()
    {
        Id = c.Id,
        CurrencyCode = c.CurrencyCode,
        CurrencyName = c.CurrencyName,
        Symbol = c.Symbol,
        DecimalPlaces = c.DecimalPlaces,
        IsBaseCurrency = c.IsBaseCurrency,
        IsActive = c.IsActive,
        Version = c.Version,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
