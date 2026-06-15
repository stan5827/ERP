using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface IEsgService
{
    Task<ProductCarbonFootprintDto?> CalculateProductFootprintAsync(Guid skuId);
    Task<IEnumerable<CarbonEmissionRecordDto>> GetRecentEmissionsAsync(string? scope = null);
    Task<IEnumerable<EsgKpiDto>> GetDashboardKpisAsync();
    Task<bool> UpdateEmissionFactorAsync(Guid sourceId, decimal newFactor);
    Task<IEnumerable<GhgEmissionSourceDto>> GetEmissionSourcesAsync();
}
