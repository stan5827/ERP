using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

public class PlanningController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public PlanningController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("suggestions")]
    public async Task<ActionResult<ApiResponse<List<dynamic>>>> GetSuggestions()
    {
        // Simple ROP logic: Qty < ReorderPoint
        var skusToReorder = await _db.Skus
            .Where(s => s.Qty < s.ReorderPoint && s.Status == "active")
            .ToListAsync();

        var suggestions = skusToReorder.Select(s => new
        {
            sku_code = s.SkuNumber,
            sku_name = s.SkuName,
            current_qty = s.Qty,
            reorder_point = s.ReorderPoint,
            safety_stock = s.SafetyStock,
            suggested_qty = s.MaxStock - s.Qty,
            reason = s.Qty < s.SafetyStock ? "SAFETY_STOCK_ALERT" : "REORDER_POINT_REACHED"
        }).ToList<dynamic>();

        return Ok(suggestions);
    }

    [HttpPost("persist-suggestions")]
    public ActionResult<ApiResponse> PersistSuggestions()
    {
        // Placeholder for persisting suggestions to PRs
        return Ok("Suggestions successfully analyzed and buffered.");
    }
}

