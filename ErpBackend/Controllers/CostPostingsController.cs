using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class CostPostingsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public CostPostingsController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "CostPostingsController is currently being repaired." });
}

