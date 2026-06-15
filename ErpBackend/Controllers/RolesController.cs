using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class RolesController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public RolesController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "RolesController is currently being repaired." });
}

