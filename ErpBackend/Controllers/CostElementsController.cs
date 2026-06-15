using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;

namespace ErpBackend.Controllers;

public class CostElementsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public CostElementsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CostElementDto>>>> GetCostElements(
        [FromQuery] string? search,
        [FromQuery] string? elementType,
        [FromQuery] bool? isActive)
    {
        var query = _db.CostElements.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(e => e.ElementCode.Contains(search) || e.ElementName.Contains(search));

        if (!string.IsNullOrEmpty(elementType))
            query = query.Where(e => e.ElementType == elementType);

        if (isActive.HasValue)
            query = query.Where(e => e.IsActive == isActive.Value);

        var items = await query
            .OrderBy(e => e.DisplayOrder)
            .ThenBy(e => e.ElementCode)
            .ToListAsync();

        var dtos = items.Select(e => new CostElementDto
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
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CostElementDto>>> GetCostElement(Guid id)
    {
        var e = await _db.CostElements.FindAsync(id);
        if (e == null) return NotFound(ApiHelper.Fail("Cost element not found"));

        var dto = new CostElementDto
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

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CostElementDto>>> Create([FromBody] CreateCostElementRequest req)
    {
        if (await _db.CostElements.AnyAsync(e => e.ElementCode == req.ElementCode))
            return BadRequest(ApiHelper.Fail($"Element code {req.ElementCode} already exists"));

        var element = new CostElement
        {
            ElementCode = req.ElementCode,
            ElementName = req.ElementName,
            ElementType = req.ElementType ?? "overhead",
            GLAccountCode = req.GLAccountCode,
            GLAccountName = req.GLAccountName,
            Description = req.Description,
            DisplayOrder = req.DisplayOrder,
            IsActive = true
        };

        _db.CostElements.Add(element);
        await _db.SaveChangesAsync();

        var dto = new CostElementDto
        {
            Id = element.Id,
            ElementCode = element.ElementCode,
            ElementName = element.ElementName,
            ElementType = element.ElementType,
            IsActive = element.IsActive,
            DisplayOrder = element.DisplayOrder
        };

        return Ok(dto, "Cost element created");
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CostElementDto>>> Update(Guid id, [FromBody] UpdateCostElementRequest req)
    {
        var element = await _db.CostElements.FindAsync(id);
        if (element == null) return NotFound(ApiHelper.Fail("Cost element not found"));

        element.ElementName = req.ElementName;
        element.ElementType = req.ElementType;
        element.GLAccountCode = req.GLAccountCode;
        element.GLAccountName = req.GLAccountName;
        element.Description = req.Description;
        element.DisplayOrder = req.DisplayOrder;
        element.IsActive = req.IsActive;

        await _db.SaveChangesAsync();

        var dto = new CostElementDto
        {
            Id = element.Id,
            ElementCode = element.ElementCode,
            ElementName = element.ElementName,
            ElementType = element.ElementType,
            GLAccountCode = element.GLAccountCode,
            GLAccountName = element.GLAccountName,
            IsActive = element.IsActive,
            Description = element.Description,
            DisplayOrder = element.DisplayOrder
        };

        return Ok(dto, "Cost element updated");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var element = await _db.CostElements.FindAsync(id);
        if (element == null) return NotFound(ApiHelper.Fail("Cost element not found"));

        if (await _db.CostElementAssignments.AnyAsync(a => a.CostElementId == id))
            return BadRequest(ApiHelper.Fail("Cannot delete: cost element assignments exist"));

        _db.CostElements.Remove(element);
        await _db.SaveChangesAsync();

        return Ok("Cost element deleted");
    }
}

