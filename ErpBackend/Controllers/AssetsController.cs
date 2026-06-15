using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class AssetsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public AssetsController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "AssetsController is currently being repaired." });
}

