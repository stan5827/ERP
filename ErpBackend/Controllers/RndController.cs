using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class RndController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public RndController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "RndController is currently being repaired." });
}

