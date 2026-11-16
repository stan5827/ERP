using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public class CostCenterService : ICostCenterService
{
    private readonly ErpGeneralDbContext _db;

    public CostCenterService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<CostCenterDto>> GetAllAsync(string? search = null)
    {
        var query = _db.CostCenters.AsNoTracking().Where(center => center.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(center => center.CostCenterCode.Contains(term)
                || center.CostCenterName.Contains(term));
        }

        return (await query.OrderBy(center => center.DisplayOrder)
                .ThenBy(center => center.CostCenterCode)
                .ToListAsync())
            .Select(Map)
            .ToList();
    }

    public async Task<List<CostCenterDto>> GetTreeAsync()
    {
        var centers = await _db.CostCenters.AsNoTracking()
            .Where(center => center.IsActive)
            .OrderBy(center => center.DisplayOrder)
            .ThenBy(center => center.CostCenterCode)
            .ToListAsync();
        var byId = centers.ToDictionary(center => center.Id, Map);
        foreach (var center in centers.Where(center => center.ParentCostCenterId.HasValue))
        {
            if (byId.TryGetValue(center.ParentCostCenterId!.Value, out var parent))
                parent.Children.Add(byId[center.Id]);
        }

        return centers
            .Where(center => !center.ParentCostCenterId.HasValue
                || !byId.ContainsKey(center.ParentCostCenterId.Value))
            .Select(center => byId[center.Id])
            .ToList();
    }

    public async Task<List<CostCenterBudgetDto>> GetBudgetAsync() =>
        await _db.CostCenters.AsNoTracking()
            .Where(center => center.IsActive)
            .Select(center => new CostCenterBudgetDto
            {
                Id = center.Id,
                BudgetLimit = center.BudgetLimit,
                BudgetUsed = center.BudgetUsed,
                UtilizationPercent = center.BudgetLimit > 0
                    ? center.BudgetUsed / center.BudgetLimit * 100
                    : 0
            })
            .ToListAsync();

    public async Task<CostCenterDto?> GetByIdAsync(Guid id)
    {
        var center = await _db.CostCenters.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && item.IsActive);
        return center is null ? null : Map(center);
    }

    public async Task<(bool success, string status, CostCenterDto? costCenter)> CreateAsync(
        SaveCostCenterRequestDto request)
    {
        var validation = await ValidateAsync(request, null);
        if (validation is not null)
            return (false, validation, null);

        var center = new CostCenter();
        Apply(center, request);
        _db.CostCenters.Add(center);
        await _db.SaveChangesAsync();
        return (true, "ok", Map(center));
    }

    public async Task<(bool success, string status, CostCenterDto? costCenter)> UpdateAsync(
        Guid id,
        SaveCostCenterRequestDto request)
    {
        var center = await _db.CostCenters
            .FirstOrDefaultAsync(item => item.Id == id && item.IsActive);
        if (center is null)
            return (false, "not_found", null);

        var validation = await ValidateAsync(request, id);
        if (validation is not null)
            return (false, validation, null);

        Apply(center, request);
        center.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, "ok", Map(center));
    }

    public async Task<(bool success, string status)> DeleteAsync(Guid id)
    {
        var center = await _db.CostCenters.FirstOrDefaultAsync(item => item.Id == id);
        if (center is null)
            return (false, "not_found");
        if (await _db.CostCenters.AnyAsync(item => item.ParentCostCenterId == id && item.IsActive))
            return (false, "has_children");
        if (await _db.CostCenterPostings.AnyAsync(posting => posting.CostCenterId == id)
            || await _db.AccountingEntries.AnyAsync(entry => entry.CostCenterId == id))
            return (false, "has_postings");

        center.IsActive = false;
        center.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    private async Task<string?> ValidateAsync(SaveCostCenterRequestDto request, Guid? id)
    {
        var code = request.CostCenterCode?.Trim();
        var name = request.CostCenterName?.Trim();
        var type = request.CostCenterType?.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 20)
            return "invalid_code";
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            return "invalid_name";
        if (string.IsNullOrWhiteSpace(type) || type.Length > 20)
            return "invalid_type";
        if (request.AreaSquareMeters < 0 || request.BudgetLimit < 0)
            return "invalid_amount";
        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value < request.EffectiveFrom)
            return "invalid_effective_range";
        if (await _db.CostCenters.AnyAsync(center => center.Id != id
            && center.CostCenterCode == code))
            return "duplicate_code";
        if (request.ParentCostCenterId.HasValue && request.ParentCostCenterId == id)
            return "invalid_parent";
        if (request.ParentCostCenterId.HasValue
            && !await _db.CostCenters.AnyAsync(center =>
                center.Id == request.ParentCostCenterId.Value && center.IsActive))
            return "invalid_parent";
        if (request.DepartmentId.HasValue
            && !await _db.Departments.AnyAsync(item => item.Id == request.DepartmentId.Value))
            return "invalid_department";
        if (request.WorkCenterId.HasValue
            && !await _db.WorkCenters.AnyAsync(item => item.Id == request.WorkCenterId.Value))
            return "invalid_work_center";
        return null;
    }

    private static void Apply(CostCenter center, SaveCostCenterRequestDto request)
    {
        center.CostCenterCode = request.CostCenterCode.Trim();
        center.CostCenterName = request.CostCenterName.Trim();
        center.CostCenterType = request.CostCenterType.Trim();
        center.ParentCostCenterId = request.ParentCostCenterId;
        center.DepartmentId = request.DepartmentId;
        center.WorkCenterId = request.WorkCenterId;
        center.BudgetLimit = request.BudgetLimit;
        center.AreaSquareMeters = request.AreaSquareMeters;
        center.EffectiveFrom = request.EffectiveFrom;
        center.EffectiveTo = request.EffectiveTo;
        center.DisplayOrder = request.DisplayOrder;
        center.Description = request.Description?.Trim();
    }

    private static CostCenterDto Map(CostCenter center) => new()
    {
        Id = center.Id,
        CostCenterCode = center.CostCenterCode,
        CostCenterName = center.CostCenterName,
        CostCenterType = center.CostCenterType,
        ParentCostCenterId = center.ParentCostCenterId,
        DepartmentId = center.DepartmentId,
        WorkCenterId = center.WorkCenterId,
        BudgetLimit = center.BudgetLimit,
        BudgetUsed = center.BudgetUsed,
        AreaSquareMeters = center.AreaSquareMeters,
        EffectiveFrom = center.EffectiveFrom,
        EffectiveTo = center.EffectiveTo,
        IsActive = center.IsActive,
        DisplayOrder = center.DisplayOrder,
        Description = center.Description
    };
}
