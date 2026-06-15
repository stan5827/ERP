using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ErpBackend.Services;

public class SupplierRatingSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SupplierRatingSchedulerService> _logger;
    private readonly TimeSpan _runInterval = TimeSpan.FromDays(1);

    public SupplierRatingSchedulerService(IServiceProvider serviceProvider, ILogger<SupplierRatingSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Supplier Rating Scheduler Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var ratingService = scope.ServiceProvider.GetRequiredService<ISupplierRatingService>();
                    await ratingService.RunBatchCalculationAsync(12);
                    _logger.LogInformation("Supplier ratings batch calculation completed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating supplier ratings in background.");
            }

            await Task.Delay(_runInterval, stoppingToken);
        }
    }
}