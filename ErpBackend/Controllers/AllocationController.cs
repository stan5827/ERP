using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class AllocationController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public AllocationController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "AllocationController is currently being repaired." });
}

