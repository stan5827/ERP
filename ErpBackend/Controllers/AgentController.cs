using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class AgentController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public AgentController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "AgentController is currently being repaired." });
}

