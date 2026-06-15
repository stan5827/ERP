using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using Microsoft.AspNetCore.Authorization;

namespace ErpBackend.Controllers;

[Route("api/erp/warehouse-types")]
[Authorize]
public class WarehouseTypesController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public WarehouseTypesController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WarehouseTypeDto>>>> GetWarehouseTypes()
    {
        var items = await _db.WarehouseTypes.OrderBy(wt => wt.SortOrder).ThenBy(wt => wt.TypeCode).ToListAsync();
        var dtos = items.Select(wt => new WarehouseTypeDto
        {
            Id = wt.Id,
            TypeCode = wt.TypeCode,
            TypeName = wt.TypeName,
            Description = wt.Description,
            IsActive = wt.IsActive,
            SortOrder = wt.SortOrder
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WarehouseTypeDto>>> CreateWarehouseType([FromBody] WarehouseTypeCreateDto data)
    {
        if (string.IsNullOrEmpty(data.TypeCode) || string.IsNullOrEmpty(data.TypeName))
            return Fail<WarehouseTypeDto>("TypeCode and TypeName are required");

        if (await _db.WarehouseTypes.AnyAsync(wt => wt.TypeCode == data.TypeCode))
            return Fail<WarehouseTypeDto>("TypeCode already exists");

        var warehouseType = new WarehouseType
        {
            Id = Guid.NewGuid(),
            TypeCode = data.TypeCode,
            TypeName = data.TypeName,
            Description = data.Description,
            IsActive = data.IsActive ?? true,
            SortOrder = data.SortOrder ?? 0
        };

        _db.WarehouseTypes.Add(warehouseType);
        await _db.SaveChangesAsync();

        var dto = new WarehouseTypeDto
        {
            Id = warehouseType.Id,
            TypeCode = warehouseType.TypeCode,
            TypeName = warehouseType.TypeName,
            Description = warehouseType.Description,
            IsActive = warehouseType.IsActive,
            SortOrder = warehouseType.SortOrder
        };

        return Ok(dto, "Warehouse type created successfully");
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WarehouseTypeDto>>> UpdateWarehouseType(Guid id, [FromBody] WarehouseTypeCreateDto data)
    {
        var warehouseType = await _db.WarehouseTypes.FindAsync(id);
        if (warehouseType == null)
            return Fail<WarehouseTypeDto>("Warehouse type not found");

        warehouseType.TypeCode = data.TypeCode;
        warehouseType.TypeName = data.TypeName;
        warehouseType.Description = data.Description;
        warehouseType.IsActive = data.IsActive ?? warehouseType.IsActive;
        warehouseType.SortOrder = data.SortOrder ?? warehouseType.SortOrder;

        await _db.SaveChangesAsync();

        var dto = new WarehouseTypeDto
        {
            Id = warehouseType.Id,
            TypeCode = warehouseType.TypeCode,
            TypeName = warehouseType.TypeName,
            Description = warehouseType.Description,
            IsActive = warehouseType.IsActive,
            SortOrder = warehouseType.SortOrder
        };

        return Ok(dto, "Warehouse type updated successfully");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteWarehouseType(Guid id)
    {
        var warehouseType = await _db.WarehouseTypes.FindAsync(id);
        if (warehouseType == null)
            return Fail("Warehouse type not found");

        // Check if in use
        if (await _db.Warehouses.AnyAsync(w => w.Type == warehouseType.TypeCode))
            return Fail("Warehouse type is in use and cannot be deleted");

        _db.WarehouseTypes.Remove(warehouseType);
        await _db.SaveChangesAsync();

        return Ok("Warehouse type deleted successfully");
    }
}
