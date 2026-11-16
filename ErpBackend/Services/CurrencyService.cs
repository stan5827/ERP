using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ErpBackend.Data;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface ICurrencyService
{
    Task<bool> UpdateExchangeRatesAsync();
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, DateTime? date = null);
}

public class CurrencyService : ICurrencyService
{
    private readonly ErpGeneralDbContext _context;
    private readonly ILogger<CurrencyService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public CurrencyService(ErpGeneralDbContext context, ILogger<CurrencyService> logger, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> UpdateExchangeRatesAsync()
    {
        _logger.LogInformation("Starting exchange rate synchronization...");
        try
        {
            var baseCurrency = "TWD"; // 企業本幣
            var targetCurrencies = await _context.Currencies
                .Where(c => c.IsActive && c.CurrencyCode != baseCurrency)
                .Select(c => c.CurrencyCode)
                .ToListAsync();

            if (!targetCurrencies.Any()) return true;

            // 模擬調用金融外部 API (如 Fixer.io, OER, etc.)
            // 在實際環境中會使用 _httpClientFactory 發送請求
            foreach (var target in targetCurrencies)
            {
                var rate = await FetchRateFromMockApi(baseCurrency, target);

                var existingRate = await _context.ExchangeRates
                    .FirstOrDefaultAsync(r => r.ToCurrency == target && r.ValidFrom >= DateTime.Today);

                if (existingRate != null)
                {
                    existingRate.Rate = rate;
                    existingRate.UpdatedAt = DateTime.UtcNow;
                    existingRate.Source = "AutoSync (Mock API)";
                }
                else
                {
                    _context.ExchangeRates.Add(new ExchangeRate
                    {
                        FromCurrency = baseCurrency,
                        ToCurrency = target,
                        Rate = rate,
                        ValidFrom = DateTime.Today,
                        RateType = "Closing",
                        Source = "AutoSync (Mock API)"
                    });
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Exchange rate synchronization completed.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to synchronize exchange rates.");
            return false;
        }
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency, DateTime? date = null)
    {
        if (fromCurrency == toCurrency) return amount;

        // 簡化邏輯：優先找 toCurrency 的匯率 (假設 baseCurrency 是 TWD)
        var rate = await _context.ExchangeRates
            .Where(r => r.ToCurrency == (fromCurrency == "TWD" ? toCurrency : fromCurrency))
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefaultAsync();

        if (rate == null) return amount; // 若無匯率則回傳原值

        if (fromCurrency == "TWD")
        {
            // TWD -> USD (amount / rate)
            return amount / rate.Rate;
        }
        else if (toCurrency == "TWD")
        {
            // USD -> TWD (amount * rate)
            return amount * rate.Rate;
        }

        // TODO: 處理交差匯率 (USD -> EUR)
        return amount;
    }

    private async Task<decimal> FetchRateFromMockApi(string baseCurrency, string targetCurrency)
    {
        // 僅為展示目的，實際需對接 API
        await Task.Delay(100);
        return targetCurrency switch
        {
            "USD" => 31.5m + (decimal)new Random().NextDouble(),
            "EUR" => 34.2m + (decimal)new Random().NextDouble(),
            "JPY" => 0.21m + (decimal)new Random().NextDouble() * 0.01m,
            _ => 1.0m
        };
    }
}
