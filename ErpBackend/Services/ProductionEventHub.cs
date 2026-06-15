using ErpBackend.DTOs.Events;
using System.Text.Json;

namespace ErpBackend.Services;

public interface IProductionEventHub
{
    Task PublishAsync<T>(T @event) where T : BaseWorkOrderEvent;
}

public class ProductionEventHub : IProductionEventHub
{
    private readonly ILogger<ProductionEventHub> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ProductionEventHub(ILogger<ProductionEventHub> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<T>(T @event) where T : BaseWorkOrderEvent
    {
        var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation("Event Published: {EventType} | ID: {EventId}\n{Data}", @event.EventType, @event.EventId, json);

        // 這裡模擬事件分發邏輯 (Event Middleware)
        // 在真實場景中，這裡會發送到 Kafka 或 Azure Event Grid
        
        switch (@event)
        {
            case WorkOrderPointCompletedEvent pointEvent:
                await HandlePointCompleted(pointEvent);
                break;
            case WorkOrderStatusChangedEvent statusEvent:
                await HandleStatusChanged(statusEvent);
                break;
            // 更多事件處理...
        }
    }

    private async Task HandlePointCompleted(WorkOrderPointCompletedEvent e)
    {
        // 自動觸發 AI 決策助手進行異常偵測
        using var scope = _serviceProvider.CreateScope();
        var alertService = scope.ServiceProvider.GetRequiredService<IProductionAlertService>();
        
        // 將事件數據轉換為報工 ID 或直接處理實體
        _logger.LogInformation("AI Assistant monitoring data from {WoNumber} Seq {Seq}", e.Data.WoNumber, e.Data.OperationIndex);
        
        // 這裡可以呼叫 alertService 進行進一步分析
        await Task.CompletedTask;
    }

    private async Task HandleStatusChanged(WorkOrderStatusChangedEvent e)
    {
        _logger.LogInformation("Work Order {WoNumber} state transition: {Prev} -> {Curr}", 
            e.Data.WoNumber, e.Data.PreviousStatus ?? "None", e.Data.CurrentStatus);
        await Task.CompletedTask;
    }
}
