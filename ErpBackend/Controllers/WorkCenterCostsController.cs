using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class WorkCenterCostsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public WorkCenterCostsController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "WorkCenterCostsController is currently being repaired." });
}

