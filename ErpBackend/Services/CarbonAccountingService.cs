using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Digital;

namespace ErpBackend.Services;

public interface ICarbonAccountingService
{
    Task<CarbonDashboardDto> GetDashboardAsync(int year);
    Task<List<CarbonEmissionRecordDto>> GetEmissionRecordsAsync(string scope, int year);
    Task<CarbonSummaryDto> CalculateSummaryAsync(int year);
    Task<CarbonIntensityDto> CalculateIntensityAsync(int year);
}

public class CarbonAccountingService : ICarbonAccountingService
{
    private readonly ErpGeneralDbContext _db;

    public CarbonAccountingService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<CarbonDashboardDto> GetDashboardAsync(int year)
    {
        var periodPrefix = year.ToString();
        var records = await _db.GhgEmissionRecords
            .Where(r => r.Period.StartsWith(periodPrefix))
            .ToListAsync();

        var totalEmissions = records.Sum(r => r.CalculatedEmissions);

        var monthlyEmissions = records
            .GroupBy(r => r.Period)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.CalculatedEmissions));

        var sources = await _db.GhgEmissionSources.ToListAsync();
        var sourceDict = sources.ToDictionary(s => s.Id, s => s.Scope);
        
        var scope1 = records.Where(r => sourceDict.GetValueOrDefault(r.SourceId, "") == "scope1").Sum(r => r.CalculatedEmissions);
        var scope2 = records.Where(r => sourceDict.GetValueOrDefault(r.SourceId, "") == "scope2").Sum(r => r.CalculatedEmissions);
        var scope3 = records.Where(r => sourceDict.GetValueOrDefault(r.SourceId, "") == "scope3").Sum(r => r.CalculatedEmissions);

        return new CarbonDashboardDto
        {
            Year = year,
            TotalEmissions = totalEmissions,
            Scope1Emissions = scope1,
            Scope2Emissions = scope2,
            Scope3Emissions = scope3,
            ReductionTarget = 0,
            ReductionProgress = 0,
            MonthlyEmissions = monthlyEmissions,
            ByCategory = new Dictionary<string, decimal>()
        };
    }

    public async Task<List<CarbonEmissionRecordDto>> GetEmissionRecordsAsync(string scope, int year)
    {
        var query = _db.GhgEmissionRecords.AsQueryable();

        if (year > 0)
            query = query.Where(r => r.Period.StartsWith(year.ToString()));

        var sources = await _db.GhgEmissionSources.ToDictionaryAsync(s => s.Id, s => s.Scope);

        var records = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        
        if (!string.IsNullOrEmpty(scope))
        {
            records = records.Where(r => sources.GetValueOrDefault(r.SourceId, "") == scope).ToList();
        }

        return records.Select(r => new CarbonEmissionRecordDto
        {
            Id = r.Id,
            SourceId = r.SourceId,
            Period = r.Period,
            ActivityData = r.ActivityData,
            Unit = r.Unit,
            CalculatedEmissions = r.CalculatedEmissions,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<CarbonSummaryDto> CalculateSummaryAsync(int year)
    {
        var records = await _db.GhgEmissionRecords
            .Where(r => r.Period.StartsWith(year.ToString()))
            .ToListAsync();

        var sources = await _db.GhgEmissionSources.ToDictionaryAsync(s => s.Id, s => s.Scope);

        return new CarbonSummaryDto
        {
            Year = year,
            TotalEmissions = records.Sum(r => r.CalculatedEmissions),
            Scope1Total = records.Where(r => sources.GetValueOrDefault(r.SourceId, "") == "scope1").Sum(r => r.CalculatedEmissions),
            Scope2Total = records.Where(r => sources.GetValueOrDefault(r.SourceId, "") == "scope2").Sum(r => r.CalculatedEmissions),
            Scope3Total = records.Where(r => sources.GetValueOrDefault(r.SourceId, "") == "scope3").Sum(r => r.CalculatedEmissions),
            RecordCount = records.Count,
            VerificationStatus = "draft"
        };
    }

    public async Task<CarbonIntensityDto> CalculateIntensityAsync(int year)
    {
        var currentTotal = (await _db.GhgEmissionRecords
            .Where(r => r.Period.StartsWith(year.ToString()))
            .Select(r => r.CalculatedEmissions)
            .ToListAsync())
            .Sum();

        var previousTotal = (await _db.GhgEmissionRecords
            .Where(r => r.Period.StartsWith((year - 1).ToString()))
            .Select(r => r.CalculatedEmissions)
            .ToListAsync())
            .Sum();

        var currentRevenue = (await _db.SalesOrders
            .Where(o => o.OrderDate.Year == year && o.Status == "completed")
            .Select(o => o.TotalAmount)
            .ToListAsync())
            .Sum();

        var previousRevenue = (await _db.SalesOrders
            .Where(o => o.OrderDate.Year == year - 1 && o.Status == "completed")
            .Select(o => o.TotalAmount)
            .ToListAsync())
            .Sum();

        var intensity = currentRevenue > 0 ? currentTotal / currentRevenue * 1000000 : 0;
        var prevIntensity = previousRevenue > 0 ? previousTotal / previousRevenue * 1000000 : 0;
        var yoyChange = prevIntensity > 0 ? (intensity - prevIntensity) / prevIntensity * 100 : 0;

        return new CarbonIntensityDto
        {
            Year = year,
            EmissionsPerRevenue = intensity,
            EmissionsPerUnit = intensity,
            Unit = "tCO2e/Million TWD",
            YoYChange = yoyChange
        };
    }
}
