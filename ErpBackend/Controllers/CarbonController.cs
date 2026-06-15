using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class CarbonController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public CarbonController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "CarbonController is currently being repaired." });
}

