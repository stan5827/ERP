using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Services;

public interface IPlanningService
{
    Task<List<PlanningSuggestionDto>> GetSuggestionsAsync();
}

public class PlanningService : IPlanningService
{
    private readonly ErpGeneralDbContext _db;

    public PlanningService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<PlanningSuggestionDto>> GetSuggestionsAsync()
    {
        var skus = await _db.Skus
            .Where(s => s.Qty < s.ReorderPoint && s.Status == "active")
            .ToListAsync();

        return skus.Select(s => new PlanningSuggestionDto
        {
            SkuCode = s.SkuNumber,
            SkuName = s.SkuName,
            CurrentQty = s.Qty,
            ReorderPoint = s.ReorderPoint,
            SafetyStock = s.SafetyStock,
            SuggestedQty = Math.Max(s.MaxStock - s.Qty, s.Moq > 0 ? s.Moq : 1),
            Reason = s.Qty < s.SafetyStock ? "SAFETY_STOCK_ALERT" : "REORDER_POINT_REACHED"
        }).ToList();
    }
}
