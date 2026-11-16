using System.Text.Json;

namespace ErpBackend.Services;

public sealed class IqcReportSearchHandler : IToolHandler
{
    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var reportNumber = parameters.GetValueOrDefault("report_number")?.ToString();
        var supplierName = parameters.GetValueOrDefault("supplier_name")?.ToString();

        var data = new[] { new { ReportNumber = reportNumber ?? "IQC-001", SupplierName = supplierName ?? "測試供應商", Result = "OK" } };
        return Task.FromResult(new ToolResult { Success = true, Data = JsonSerializer.Serialize(data) });
    }
}

public sealed class QualityComplaintSearchHandler : IToolHandler
{
    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var complaintNumber = parameters.GetValueOrDefault("complaint_number")?.ToString();
        var status = parameters.GetValueOrDefault("status")?.ToString();

        var data = new[] { new { ComplaintNumber = complaintNumber ?? "QC-001", Status = status ?? "open", Priority = "high" } };
        return Task.FromResult(new ToolResult { Success = true, Data = JsonSerializer.Serialize(data) });
    }
}

public sealed class QualityReportHandler : IToolHandler
{
    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var reportType = parameters.GetValueOrDefault("report_type")?.ToString() ?? "iqc_summary";
        return Task.FromResult(new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { ReportId = Guid.NewGuid(), ReportType = reportType, GeneratedAt = DateTime.UtcNow, Format = "PDF" }) });
    }
}
