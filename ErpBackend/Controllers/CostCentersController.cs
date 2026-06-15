using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class CostCentersController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public CostCentersController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "CostCentersController is currently being repaired." });
}

