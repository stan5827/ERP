using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class CalendarController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public CalendarController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "CalendarController is currently being repaired." });
}

