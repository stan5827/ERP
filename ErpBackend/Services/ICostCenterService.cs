using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface ICostCenterService
{
    Task<List<CostCenterDto>> GetAllAsync(string? search = null);
    Task<List<CostCenterDto>> GetTreeAsync();
    Task<List<CostCenterBudgetDto>> GetBudgetAsync();
    Task<CostCenterDto?> GetByIdAsync(Guid id);
    Task<(bool success, string status, CostCenterDto? costCenter)> CreateAsync(SaveCostCenterRequestDto request);
    Task<(bool success, string status, CostCenterDto? costCenter)> UpdateAsync(Guid id, SaveCostCenterRequestDto request);
    Task<(bool success, string status)> DeleteAsync(Guid id);
}
