using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class EsgController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public EsgController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "EsgController is currently being repaired." });
}

