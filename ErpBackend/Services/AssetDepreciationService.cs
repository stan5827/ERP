using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Asset;

namespace ErpBackend.Services;

public interface IAssetDepreciationService
{
    Task<DepreciationResultDto> CalculateMonthlyDepreciationAsync(int year, int month);
    Task<List<DepreciationRecordDto>> GetDepreciationScheduleAsync(Guid assetId);
    Task<DepreciationSummaryDto> GetDepreciationSummaryAsync(DateTime startDate, DateTime endDate);
}

public class DepreciationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public int ProcessedCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class DepreciationRecordDto
{
    public Guid AssetId { get; set; }
    public string AssetNumber { get; set; } = null!;
    public string AssetName { get; set; } = null!;
    public string Period { get; set; } = null!;
    public decimal DepreciationAmount { get; set; }
    public decimal AccumulatedDepreciation { get; set; }
    public decimal BookValue { get; set; }
}

public class DepreciationSummaryDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalAssets { get; set; }
    public decimal TotalDepreciation { get; set; }
    public decimal TotalAccumulated { get; set; }
    public decimal TotalBookValue { get; set; }
    public List<DepreciationByCategoryDto> ByCategory { get; set; } = new();
}

public class DepreciationByCategoryDto
{
    public string CategoryName { get; set; } = null!;
    public int AssetCount { get; set; }
    public decimal TotalDepreciation { get; set; }
}

public class AssetDepreciationService : IAssetDepreciationService
{
    private readonly ErpGeneralDbContext _db;

    public AssetDepreciationService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<DepreciationResultDto> CalculateMonthlyDepreciationAsync(int year, int month)
    {
        var period = $"{year}{month:D2}";
        var assets = await _db.Assets
            .Include(a => a.Category)
            .Include(a => a.Depreciations)
            .Where(a => a.Status == "active" && a.AcquisitionDate != null)
            .ToListAsync();

        var processedCount = 0;
        var totalAmount = 0m;

        foreach (var asset in assets)
        {
            var existingDep = asset.Depreciations?.FirstOrDefault(d => d.Period == period);
            if (existingDep != null)
                continue;

            var monthsInService = (year - asset.AcquisitionDate!.Value.Year) * 12 + (month - asset.AcquisitionDate.Value.Month);
            if (monthsInService < 0 || monthsInService >= asset.UsefulLife)
                continue;

            var depreciation = CalculateDepreciation(asset, monthsInService + 1);

            var record = new AssetDepreciation
            {
                AssetId = asset.Id,
                Period = period,
                DepreciationAmount = depreciation.Amount,
                AccumulatedDepreciation = depreciation.Accumulated,
                BookValue = depreciation.BookValue
            };

            _db.AssetDepreciations.Add(record);
            processedCount++;
            totalAmount += depreciation.Amount;
        }

        await _db.SaveChangesAsync();

        return new DepreciationResultDto
        {
            Success = true,
            Message = $"已處理 {processedCount} 筆資產折舊",
            Year = year,
            Month = month,
            ProcessedCount = processedCount,
            TotalAmount = totalAmount
        };
    }

    public async Task<List<DepreciationRecordDto>> GetDepreciationScheduleAsync(Guid assetId)
    {
        var asset = await _db.Assets
            .Include(a => a.Depreciations)
            .FirstOrDefaultAsync(a => a.Id == assetId);

        if (asset == null)
            return new List<DepreciationRecordDto>();

        return asset.Depreciations?
            .OrderBy(d => d.Period)
            .Select(d => new DepreciationRecordDto
            {
                AssetId = asset.Id,
                AssetNumber = asset.AssetNumber,
                AssetName = asset.AssetName,
                Period = d.Period,
                DepreciationAmount = d.DepreciationAmount,
                AccumulatedDepreciation = d.AccumulatedDepreciation,
                BookValue = d.BookValue
            })
            .ToList() ?? new List<DepreciationRecordDto>();
    }

    public async Task<DepreciationSummaryDto> GetDepreciationSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var startPeriod = $"{startDate.Year}{startDate.Month:D2}";
        var endPeriod = $"{endDate.Year}{endDate.Month:D2}";

        var records = await _db.AssetDepreciations
            .Include(d => d.Asset)
            .ThenInclude(a => a.Category)
            .Where(d => string.Compare(d.Period, startPeriod) >= 0 && string.Compare(d.Period, endPeriod) <= 0)
            .ToListAsync();

        var byCategory = records
            .GroupBy(r => r.Asset.Category?.CategoryName ?? "未分類")
            .Select(g => new DepreciationByCategoryDto
            {
                CategoryName = g.Key,
                AssetCount = g.Select(r => r.AssetId).Distinct().Count(),
                TotalDepreciation = g.Sum(r => r.DepreciationAmount)
            })
            .ToList();

        return new DepreciationSummaryDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalAssets = records.Select(r => r.AssetId).Distinct().Count(),
            TotalDepreciation = records.Sum(r => r.DepreciationAmount),
            TotalAccumulated = records.Sum(r => r.AccumulatedDepreciation),
            TotalBookValue = records.Sum(r => r.BookValue),
            ByCategory = byCategory
        };
    }

    private (decimal Amount, decimal Accumulated, decimal BookValue) CalculateDepreciation(Asset asset, int period)
    {
        var cost = asset.AcquisitionCost;
        var residual = asset.ResidualValue;
        var usefulLife = asset.UsefulLife;

        decimal amount = 0, accumulated = 0, bookValue = 0;

        if (asset.DepreciationMethod == "straight_line")
        {
            amount = (cost - residual) / usefulLife;
            accumulated = amount * period;
            bookValue = cost - accumulated;
        }
        else if (asset.DepreciationMethod == "declining_balance")
        {
            var rate = 2.0m / usefulLife;
            var yearValue = cost * rate;
            amount = period == 1 ? yearValue : (cost - accumulated) * rate;
            accumulated = Math.Min(cost * rate * period, cost - residual);
            bookValue = cost - accumulated;
        }
        else
        {
            amount = (cost - residual) / usefulLife;
            accumulated = amount * period;
            bookValue = cost - accumulated;
        }

        return (Math.Round(amount, 2), Math.Round(accumulated, 2), Math.Round(bookValue, 2));
    }
}
