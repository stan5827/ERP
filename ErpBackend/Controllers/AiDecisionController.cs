using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class AiDecisionController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public AiDecisionController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "AiDecisionController is currently being repaired." });
}

