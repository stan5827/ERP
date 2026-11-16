using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;

namespace ErpBackend.Services;

public interface ICostElementService
{
    Task<List<CostElementDto>> GetCostElementsAsync(string? search = null, string? elementType = null, bool? isActive = null);
    Task<CostElementDto?> GetCostElementAsync(Guid id);
    Task<(CostElementDto? dto, string status)> CreateCostElementAsync(CreateCostElementRequest request);
    Task<(CostElementDto? dto, string status)> UpdateCostElementAsync(Guid id, UpdateCostElementRequest request);
    Task<(bool ok, string status)> DeleteCostElementAsync(Guid id);
}

public class CostElementService : ICostElementService
{
    private readonly ErpGeneralDbContext _db;

    public CostElementService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<CostElementDto>> GetCostElementsAsync(string? search = null, string? elementType = null, bool? isActive = null)
    {
        var query = _db.CostElements.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(e => e.ElementCode.Contains(search) || e.ElementName.Contains(search));

        if (!string.IsNullOrEmpty(elementType))
            query = query.Where(e => e.ElementType == elementType);

        if (isActive.HasValue)
            query = query.Where(e => e.IsActive == isActive.Value);

        var items = await query.OrderBy(e => e.DisplayOrder).ThenBy(e => e.ElementCode).ToListAsync();

        return items.Select(MapToDto).ToList();
    }

    public async Task<CostElementDto?> GetCostElementAsync(Guid id)
    {
        var e = await _db.CostElements.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return e == null ? null : MapToDto(e);
    }

    public async Task<(CostElementDto? dto, string status)> CreateCostElementAsync(CreateCostElementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ElementCode) || string.IsNullOrWhiteSpace(request.ElementName))
            return (null, "invalid");

        var duplicate = await _db.CostElements.AnyAsync(e => e.ElementCode == request.ElementCode);
        if (duplicate) return (null, "duplicate");

        var element = new CostElement
        {
            ElementCode = request.ElementCode,
            ElementName = request.ElementName,
            ElementType = request.ElementType ?? "overhead",
            GLAccountCode = request.GLAccountCode,
            GLAccountName = request.GLAccountName,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        _db.CostElements.Add(element);
        await _db.SaveChangesAsync();

        return (MapToDto(element), "ok");
    }

    public async Task<(CostElementDto? dto, string status)> UpdateCostElementAsync(Guid id, UpdateCostElementRequest request)
    {
        var element = await _db.CostElements.FirstOrDefaultAsync(e => e.Id == id);
        if (element == null) return (null, "not_found");

        if (request.ElementName != null) element.ElementName = request.ElementName;
        element.ElementType = request.ElementType ?? element.ElementType;
        element.GLAccountCode = request.GLAccountCode;
        element.GLAccountName = request.GLAccountName;
        element.Description = request.Description;
        element.DisplayOrder = request.DisplayOrder ?? element.DisplayOrder;
        if (request.IsActive.HasValue) element.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();

        return (MapToDto(element), "ok");
    }

    public async Task<(bool ok, string status)> DeleteCostElementAsync(Guid id)
    {
        var element = await _db.CostElements.FirstOrDefaultAsync(e => e.Id == id);
        if (element == null) return (false, "not_found");

        var hasAssignments = await _db.CostElementAssignments.AnyAsync(a => a.CostElementId == id);
        if (hasAssignments) return (false, "guarded");

        _db.CostElements.Remove(element);
        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    private static CostElementDto MapToDto(CostElement e) => new()
    {
        Id = e.Id,
        ElementCode = e.ElementCode,
        ElementName = e.ElementName,
        ElementType = e.ElementType,
        GLAccountCode = e.GLAccountCode,
        GLAccountName = e.GLAccountName,
        IsActive = e.IsActive,
        Description = e.Description,
        DisplayOrder = e.DisplayOrder
    };
}
