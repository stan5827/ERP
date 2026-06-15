using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class AttendanceController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public AttendanceController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "AttendanceController is currently being repaired." });
}

