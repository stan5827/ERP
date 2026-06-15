using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/bin-locations")]
[Route("api/erp/BinLocations")]
public class BinLocationsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public BinLocationsController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet]
    public async Task<dynamic> Get()
    {
        var data = await _db.BinLocations.ToListAsync();
        return Ok(data);
    }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "BinLocationsController restored." });
}
