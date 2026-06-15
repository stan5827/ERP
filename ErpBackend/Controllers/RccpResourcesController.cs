using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/rccp")]
public class RccpResourcesController : BaseController
{
    private readonly ErpGeneralDbContext _context;

    public RccpResourcesController(ErpGeneralDbContext context)
    {
        _context = context;
    }

    // --- Resources ---

    [HttpGet("resources")]
    public async Task<ActionResult<ApiResponse<List<RccpResource>>>> ListResources()
    {
        var list = await _context.RccpResources
            .Include(r => r.WorkCenter)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("resources")]
    public async Task<ActionResult<ApiResponse<RccpResource>>> CreateResource(RccpResource model)
    {
        _context.RccpResources.Add(model);
        await _context.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("resources/{id}")]
    public async Task<ActionResult<ApiResponse<RccpResource>>> UpdateResource(Guid id, RccpResource model)
    {
        _context.Entry(model).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return Ok(model);
    }

    // --- Capacity Bills (Item Resources) ---

    [HttpGet("item-resources/{skuId}")]
    public async Task<ActionResult<ApiResponse<List<RccpItemResource>>>> GetItemResources(Guid skuId)
    {
        var list = await _context.RccpItemResources
            .Include(ir => ir.Resource)
            .Where(ir => ir.SkuId == skuId)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("item-resources")]
    public async Task<ActionResult<ApiResponse>> UpdateItemResources([FromBody] List<RccpItemResource> items)
    {
        if (items == null || !items.Any()) return BadRequest();
        
        var skuId = items.First().SkuId;
        var existing = await _context.RccpItemResources.Where(ir => ir.SkuId == skuId).ToListAsync();
        _context.RccpItemResources.RemoveRange(existing);
        
        foreach(var item in items) {
            item.Id = Guid.NewGuid();
            _context.RccpItemResources.Add(item);
        }
        
        await _context.SaveChangesAsync();
        return Ok("Saved successfully");
    }
}
