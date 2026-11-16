using System.Collections.Generic;
using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface IAssetService
{
    Task<List<AssetDto>> GetAllAsync(string? search, string? status, string? categoryCode);
    Task<AssetDto?> GetByIdAsync(Guid id);
    Task<(AssetDto? Dto, string Status)> CreateAsync(AssetUpsertDto req);
    Task<(AssetDto? Dto, string Status)> UpdateAsync(Guid id, AssetUpsertDto req);
    Task<(bool Ok, string Status)> DeleteAsync(Guid id);

    Task<List<AssetMaintenanceDto>> GetMaintenanceAsync(Guid assetId);
    Task<(AssetMaintenanceDto? Dto, string Status)> AddMaintenanceAsync(Guid assetId, AssetMaintenanceUpsertDto req);

    Task<DepreciationResultDto> RunDepreciationAsync(int year, int month);
    Task<List<DepreciationRecordDto>> GetDepreciationHistoryAsync(Guid assetId);
}
