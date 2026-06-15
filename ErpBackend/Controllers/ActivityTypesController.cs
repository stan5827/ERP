using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class ActivityTypesController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public ActivityTypesController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "ActivityTypesController is currently being repaired." });
}

