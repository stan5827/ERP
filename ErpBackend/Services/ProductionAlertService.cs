using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using System.Text;

namespace ErpBackend.Services;

public interface IProductionAlertService
{
    Task AnalyzeWipAnomalyAsync(Guid wipReportId);
}

public class ProductionAlertService : IProductionAlertService
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<ProductionAlertService> _logger;

    public ProductionAlertService(ErpGeneralDbContext db, ILogger<ProductionAlertService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task AnalyzeWipAnomalyAsync(Guid wipReportId)
    {
        try
        {
            var report = await _db.WipReports
                .Include(r => r.WorkOrder)
                .FirstOrDefaultAsync(r => r.Id == wipReportId && r.Status == "approved");

            if (report == null || report.WorkOrder == null) return;

            var opConfig = await _db.WorkOrderOperations
                .FirstOrDefaultAsync(o => o.WorkOrderId == report.WorkOrderId && o.OperationSeq == report.OperationSeq);

            var anomalies = new List<string>();

            // 1. 品質異常偵測
            if (report.DefectQty > 0)
            {
                anomalies.Add($"品質異常：偵測到不良品 {report.DefectQty} PCS");
            }
            
            decimal yield = (report.CompletedQty + report.DefectQty + report.ScrapQty) > 0 
                ? report.CompletedQty / (report.CompletedQty + report.DefectQty + report.ScrapQty) 
                : 1;
            
            if (yield < 0.9m)
            {
                anomalies.Add($"產出效率低：良率為 {yield:P1}，低於系統閾值 (90%)");
            }

            // 2. 效率異常偵測
            if (opConfig != null && report.ActualManHours > 0)
            {
                var plannedRunTime = 0.5m; 
                var expectedHours = report.CompletedQty * plannedRunTime;
                if (report.ActualManHours > expectedHours * 1.3m)
                {
                    anomalies.Add($"人工成本異常：實際工時 {report.ActualManHours}hr 顯著高於預期 {expectedHours}hr (+30%↑)");
                }
            }

            // 3. ESG 能耗異常
            if (report.EnergyConsumption > 100) 
            {
                anomalies.Add($"能耗異常：生產能耗高達 {report.EnergyConsumption} kWh");
            }

            if (anomalies.Any())
            {
                report.HasAnomaly = true;
                report.AnomalyType = string.Join("|", anomalies.Select(a => a.Split('：')[0]));
                
                await TriggerAlert(report, anomalies);
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "發生 AI 異常偵測錯誤");
        }
    }

    private async Task TriggerAlert(WipReport report, List<string> anomalies)
    {
        var alertMessage = new StringBuilder();
        alertMessage.AppendLine($"🚨 [ProMES 生產異常警告]");
        alertMessage.AppendLine($"工單編號：{report.WorkOrder?.WoNumber}");
        alertMessage.AppendLine($"工序：Seq {report.OperationSeq}");
        alertMessage.AppendLine($"報報編號：{report.ReportNumber}");
        alertMessage.AppendLine("---------------------------");
        foreach (var a in anomalies)
        {
            alertMessage.AppendLine($"⚠️ {a}");
        }

        // Keep WIP data local until an approved LLM data-egress policy is available.
        await SendLineNotification(alertMessage.ToString());
    }

    private async Task SendLineNotification(string message)
    {
        _logger.LogWarning("Sending LINE Notification: \n{Message}", message);
        await Task.CompletedTask;
    }
}
