using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class PermissionsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public PermissionsController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "PermissionsController is currently being repaired." });
}

