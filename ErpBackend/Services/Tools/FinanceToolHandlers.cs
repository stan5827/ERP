using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;

namespace ErpBackend.Services;

public sealed class CostCenterSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public CostCenterSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var costCenterCode = parameters.GetValueOrDefault("cost_center_code")?.ToString();
        var departmentIdStr = parameters.GetValueOrDefault("department_id")?.ToString();

        var results = await _db.CostCenters
            .Where(c => string.IsNullOrEmpty(costCenterCode) || c.CostCenterCode.Contains(costCenterCode))
            .Take(50)
            .Select(c => new { c.CostCenterCode, c.CostCenterName, c.IsActive })
            .ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class AssetSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public AssetSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var assetNumber = parameters.GetValueOrDefault("asset_number")?.ToString();
        var status = parameters.GetValueOrDefault("status")?.ToString();

        var query = _db.Assets.AsQueryable();
        if (!string.IsNullOrEmpty(assetNumber))
            query = query.Where(a => a.AssetNumber.Contains(assetNumber));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(a => a.Status == status);

        var results = await query.Take(50).Select(a => new { a.AssetNumber, a.AssetName, a.Status }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class LoanSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public LoanSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var bankName = parameters.GetValueOrDefault("bank_name")?.ToString();
        var status = parameters.GetValueOrDefault("status")?.ToString();

        var query = _db.Loans.AsQueryable();
        if (!string.IsNullOrEmpty(bankName))
            query = query.Where(l => l.BankName != null && l.BankName.Contains(bankName));

        var results = await query.Take(50).Select(l => new { l.LoanNumber, l.BankName, l.LoanAmount, l.Status }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class InvestmentSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public InvestmentSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var category = parameters.GetValueOrDefault("category")?.ToString();
        var status = parameters.GetValueOrDefault("status")?.ToString();

        var query = _db.Investments.AsQueryable();
        if (!string.IsNullOrEmpty(category))
            query = query.Where(i => i.InvestmentCategory == category);

        var results = await query.Take(50).Select(i => new { i.InvestmentName, i.InvestmentCategory, i.TotalCost }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class FinanceReportHandler : IToolHandler
{
    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var reportType = parameters.GetValueOrDefault("report_type")?.ToString() ?? "summary";
        return Task.FromResult(new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { ReportId = Guid.NewGuid(), ReportType = reportType, GeneratedAt = DateTime.UtcNow, Format = "PDF" }) });
    }
}
