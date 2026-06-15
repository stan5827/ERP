using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ErpBackend.Services;

namespace ErpBackend.Services;

public class ExchangeRateSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExchangeRateSyncService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromHours(12);

    public ExchangeRateSyncService(IServiceProvider serviceProvider, ILogger<ExchangeRateSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Exchange Rate Sync Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var currencyService = scope.ServiceProvider.GetRequiredService<ICurrencyService>();
                    await currencyService.UpdateExchangeRatesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while synchronizing exchange rates in background.");
            }

            // 等待下一次執行
            await Task.Delay(_syncInterval, stoppingToken);
        }

        _logger.LogInformation("Exchange Rate Sync Background Service is stopping.");
    }
}
