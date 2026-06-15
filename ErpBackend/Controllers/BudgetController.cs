using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class BudgetController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public BudgetController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "BudgetController is currently being repaired." });
}

