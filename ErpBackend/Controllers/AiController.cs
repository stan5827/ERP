using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class AiController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public AiController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "AiController is currently being repaired." });
}

