using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class FinanceController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public FinanceController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "FinanceController is currently being repaired." });
}

