using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class DigitalController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public DigitalController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "DigitalController is currently being repaired." });
}

