using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class EdiController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public EdiController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "EdiController is currently being repaired." });
}

