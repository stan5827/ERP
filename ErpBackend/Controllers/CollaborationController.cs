using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class CollaborationController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public CollaborationController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "CollaborationController is currently being repaired." });
}

