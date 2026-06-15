using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class KpiController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public KpiController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "KpiController is currently being repaired." });
}

