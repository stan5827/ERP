using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class SubstituteController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public SubstituteController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "SubstituteController is currently being repaired." });
}

