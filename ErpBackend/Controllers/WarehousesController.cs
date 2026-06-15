using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Controllers;

public class WarehousesController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public WarehousesController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WarehouseDto>>>> GetWarehouses()
    {
        var items = await _db.Warehouses.OrderBy(w => w.WarehouseCode).ToListAsync();
        var dtos = items.Select(w => new WarehouseDto
        {
            Id = w.Id,
            WarehouseCode = w.WarehouseCode,
            WarehouseName = w.WarehouseName,
            IsActive = true
        }).ToList();

        return Ok(dtos);
    }
}
