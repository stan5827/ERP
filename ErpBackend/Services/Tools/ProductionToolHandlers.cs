using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;

namespace ErpBackend.Services;

public sealed class WorkOrderSearchHandler : IToolHandler
{
    private readonly IErpIntegrationService _erpIntegration;

    public WorkOrderSearchHandler(IErpIntegrationService erpIntegration) => _erpIntegration = erpIntegration;

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters) =>
        _erpIntegration.SearchWorkOrdersAsync(
            parameters.GetValueOrDefault("wo_number")?.ToString(),
            parameters.GetValueOrDefault("status")?.ToString());
}

public sealed class WorkCenterSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public WorkCenterSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var results = await _db.WorkCenters
            .Take(50)
            .Select(w => new
            {
                w.WorkCenterCode,
                w.WorkCenterName,
                w.Capacity
            })
            .ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class CreateWorkOrderHandler : IToolHandler
{
    private readonly IErpIntegrationService _erpIntegration;

    public CreateWorkOrderHandler(IErpIntegrationService erpIntegration) => _erpIntegration = erpIntegration;

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters) =>
        _erpIntegration.CreateWorkOrderAsync(
            parameters.GetValueOrDefault("sku_id")?.ToString() ?? "",
            decimal.Parse(parameters.GetValueOrDefault("quantity")?.ToString() ?? "1"),
            parameters.GetValueOrDefault("priority")?.ToString());
}

public sealed class UpdateWorkOrderHandler : IToolHandler
{
    private readonly IErpIntegrationService _erpIntegration;

    public UpdateWorkOrderHandler(IErpIntegrationService erpIntegration) => _erpIntegration = erpIntegration;

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters) =>
        _erpIntegration.UpdateWorkOrderStatusAsync(
            parameters.GetValueOrDefault("wo_id")?.ToString() ?? "",
            parameters.GetValueOrDefault("status")?.ToString() ?? "updated");
}

public sealed class CompleteWorkOrderHandler : IToolHandler
{
    private readonly IErpIntegrationService _erpIntegration;

    public CompleteWorkOrderHandler(IErpIntegrationService erpIntegration) => _erpIntegration = erpIntegration;

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters) =>
        _erpIntegration.CompleteWorkOrderAsync(
            parameters.GetValueOrDefault("wo_id")?.ToString() ?? "",
            decimal.Parse(parameters.GetValueOrDefault("quantity")?.ToString() ?? "0"));
}

public sealed class IssueMaterialHandler : IToolHandler
{
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        await Task.CompletedTask;
        var woIdStr = parameters.GetValueOrDefault("wo_id")?.ToString();

        if (string.IsNullOrEmpty(woIdStr))
            return new ToolResult { Success = false, Error = "wo_id is required" };

        return new ToolResult
        {
            Success = true,
            Data = JsonSerializer.Serialize(new
            {
                IssueId = Guid.NewGuid(),
                WorkOrderId = woIdStr,
                Status = "issued",
                IssuedAt = DateTime.UtcNow
            })
        };
    }
}

public sealed class ProductionReportHandler : IToolHandler
{
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        await Task.CompletedTask;

        return new ToolResult
        {
            Success = true,
            Data = JsonSerializer.Serialize(new
            {
                ReportId = Guid.NewGuid(),
                ReportType = "production",
                GeneratedAt = DateTime.UtcNow,
                Format = "PDF"
            })
        };
    }
}

public sealed class ProductionEfficiencyHandler : IToolHandler
{
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        await Task.CompletedTask;
        var workCenterIdStr = parameters.GetValueOrDefault("work_center_id")?.ToString();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { WorkCenterId = workCenterIdStr ?? "all", Availability = 0.92m, Performance = 0.88m, Quality = 0.98m, Oee = 0.79m }) };
    }
}