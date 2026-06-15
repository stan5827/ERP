using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Controllers;

public class UomsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public UomsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UomDto>>>> GetUoms(
        [FromQuery] string? uomType,
        [FromQuery] bool? isActive,
        [FromQuery] string? searchTerm)
    {
        var query = _db.Uoms.AsQueryable();

        if (!string.IsNullOrEmpty(uomType))
            query = query.Where(u => u.UomType == uomType);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(u => u.UomCode.Contains(searchTerm) || u.UomName.Contains(searchTerm));

        var items = await query
            .OrderBy(u => u.DisplayOrder)
            .ThenBy(u => u.UomCode)
            .ToListAsync();

        var dtos = items.Select(u => new UomDto
        {
            Id = u.Id,
            UomCode = u.UomCode,
            UomName = u.UomName,
            UomNameEn = u.UomNameEn,
            UomType = u.UomType,
            DecimalPlaces = u.DecimalPlaces,
            IsActive = u.IsActive,
            AllowDecimal = u.AllowDecimal,
            Description = u.Description,
            DisplayOrder = u.DisplayOrder,
            CreatedAt = u.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UomDto>>> GetUom(Guid id)
    {
        var uom = await _db.Uoms.FindAsync(id);
        if (uom == null) return NotFound(ApiHelper.Fail("Error"));

        return Ok(new UomDto
        {
            Id = uom.Id,
            UomCode = uom.UomCode,
            UomName = uom.UomName,
            UomNameEn = uom.UomNameEn,
            UomType = uom.UomType,
            DecimalPlaces = uom.DecimalPlaces,
            IsActive = uom.IsActive,
            AllowDecimal = uom.AllowDecimal,
            Description = uom.Description,
            DisplayOrder = uom.DisplayOrder,
            CreatedAt = uom.CreatedAt
        });
    }
}
