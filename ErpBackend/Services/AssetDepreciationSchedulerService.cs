using ErpBackend.Data;
using ErpBackend.Models.Asset;

namespace ErpBackend.Services;

public class AssetDepreciationSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AssetDepreciationSchedulerService> _logger;
    private readonly int _runHour = 1; // 凌晨 1 點執行

    public AssetDepreciationSchedulerService(IServiceProvider serviceProvider, ILogger<AssetDepreciationSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var nextRun = new DateTime(now.Year, now.Month, now.Day, _runHour, 0, 0);
                if (now > nextRun) nextRun = nextRun.AddMonths(1);
                
                var delay = nextRun - now;
                _logger.LogInformation("下次折舊計算排程: {NextRun}", nextRun);
                
                await Task.Delay(delay, stoppingToken);
                
                await RunDepreciationAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "折舊排程執行失敗");
            }
        }
    }

    private async Task RunDepreciationAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAssetDepreciationService>();
        
        var now = DateTime.Now;
        var result = await service.CalculateMonthlyDepreciationAsync(now.Year, now.Month);
        
        _logger.LogInformation("折舊計算完成: {Message}, 處理筆數: {Count}, 金額: {Amount}", 
            result.Message, result.ProcessedCount, result.TotalAmount);
    }
}