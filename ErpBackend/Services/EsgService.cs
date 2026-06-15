using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Digital;
using ErpBackend.Models.MES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ErpBackend.Services;

public class EsgService : IEsgService
{
    private readonly ErpGeneralDbContext _db;

    public EsgService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<ProductCarbonFootprintDto?> CalculateProductFootprintAsync(Guid skuId)
    {
        var sku = await _db.Skus.FindAsync(skuId);
        if (sku == null) return null;

        var result = new ProductCarbonFootprintDto
        {
            SkuId = skuId,
            SkuNumber = sku.SkuCode,
            SkuName = sku.SkuName,
            TotalCo2e = 0,
            Unit = "kgCO2e",
            Components = new List<CarbonFootprintComponentDto>()
        };

        // 1. Scope 3: Materials (BOM Recursive)
        await CalculateBomFootprint(skuId, 1, result);

        // 2. Scope 2: Manufacturing (Routing & Machine Energy)
        await CalculateManufacturingFootprint(skuId, result);

        // Calculate Percentages
        if (result.TotalCo2e > 0)
        {
            foreach (var comp in result.Components)
            {
                comp.Percentage = Math.Round((comp.Co2e / result.TotalCo2e) * 100, 2);
            }
        }

        return result;
    }

    private async Task CalculateBomFootprint(Guid skuId, decimal multiplier, ProductCarbonFootprintDto result)
    {
        var bom = await _db.Boms
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.ProductSkuId == skuId && b.Status == "active");

        if (bom == null)
        {
            // Base material - find its emission factor
            var factor = await _db.GhgEmissionSources
                .FirstOrDefaultAsync(s => s.SkuId == skuId);
            
            if (factor != null)
            {
                var sku = await _db.Skus.FindAsync(skuId);
                var co2e = multiplier * factor.EmissionFactor;
                result.TotalCo2e += co2e;
                
                result.Components.Add(new CarbonFootprintComponentDto
                {
                    Category = "Material",
                    Name = sku?.SkuName ?? "Unknown Material",
                    Co2e = co2e,
                    ActivityData = multiplier,
                    ActivityUnit = sku?.Unit ?? "unit"
                });
            }
        }
        else
        {
            foreach (var item in bom.Items)
            {
                await CalculateBomFootprint(item.ChildSkuId, multiplier * item.Quantity, result);
            }
        }
    }

    private async Task CalculateManufacturingFootprint(Guid skuId, ProductCarbonFootprintDto result)
    {
        var routings = await _db.Routings
            .Include(r => r.Operations)
                .ThenInclude(o => o.Operation)
            .Where(r => r.ProductSkuId == skuId && r.IsActive)
            .ToListAsync();

        foreach (var r in routings)
        {
            foreach (var ro in r.Operations)
            {
                var factor = await _db.GhgEmissionSources
                    .FirstOrDefaultAsync(s => s.WorkCenterId == ro.WorkCenterId);

                if (factor == null)
                {
                    factor = await _db.GhgEmissionSources.FirstOrDefaultAsync(s => s.Scope == "scope2");
                }

                if (factor != null && ro.Operation != null)
                {
                    decimal totalHours = (decimal)(ro.Operation.SetupTime + ro.Operation.RunTime);
                    decimal co2e = totalHours * factor.EmissionFactor;
                    
                    result.TotalCo2e += co2e;
                    result.Components.Add(new CarbonFootprintComponentDto
                    {
                        Category = "Manufacturing",
                        Name = ro.Operation.OperationName,
                        Co2e = co2e,
                        ActivityData = totalHours,
                        ActivityUnit = "hours"
                    });
                }
            }
        }
    }

    public async Task<IEnumerable<CarbonEmissionRecordDto>> GetRecentEmissionsAsync(string? scope = null)
    {
        var query = _db.GhgEmissionRecords
            .Include(r => r.Source)
            .AsQueryable();

        if (!string.IsNullOrEmpty(scope))
        {
            query = query.Where(r => r.Source.Scope == scope);
        }

        var records = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return records.Select(r => new CarbonEmissionRecordDto
        {
            Id = r.Id,
            SourceId = r.SourceId,
            SourceName = r.Source?.SourceName ?? "Unknown Source",
            Period = r.Period ?? string.Empty,
            ActivityData = r.ActivityData,
            Unit = r.Unit ?? string.Empty,
            CalculatedEmissions = r.CalculatedEmissions,
            CreatedAt = r.CreatedAt
        });
    }

    public async Task<IEnumerable<EsgKpiDto>> GetDashboardKpisAsync()
    {
        var totalEmissions = await _db.GhgEmissionRecords
            .Where(r => r.Period == DateTime.Now.ToString("yyyy-MM"))
            .SumAsync(r => r.CalculatedEmissions);

        return new List<EsgKpiDto>
        {
            new EsgKpiDto { KpiName = "Total CO2e (MT)", CurrentValue = totalEmissions / 1000, TargetValue = 50.0m, ChangePercentage = -4.2m, Status = "normal" },
            new EsgKpiDto { KpiName = "Carbon Intensity", CurrentValue = 12.5m, TargetValue = 10.0m, ChangePercentage = 2.1m, Status = "warning" },
            new EsgKpiDto { KpiName = "Renewable Energy Mix", CurrentValue = 28.5m, TargetValue = 30.0m, ChangePercentage = 15.0m, Status = "normal" }
        };
    }

    public async Task<bool> UpdateEmissionFactorAsync(Guid sourceId, decimal newFactor)
    {
        var source = await _db.GhgEmissionSources.FindAsync(sourceId);
        if (source == null) return false;

        source.EmissionFactor = newFactor;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<GhgEmissionSourceDto>> GetEmissionSourcesAsync()
    {
        return await _db.GhgEmissionSources
            .Select(s => new GhgEmissionSourceDto
            {
                Id = s.Id,
                SourceCode = s.SourceCode ?? string.Empty,
                SourceName = s.SourceName ?? string.Empty,
                Scope = s.Scope ?? string.Empty,
                EnergyType = s.EnergyType,
                EmissionFactor = s.EmissionFactor,
                GwpValue = s.GwpValue ?? 1m
            }).ToListAsync();
    }
}
