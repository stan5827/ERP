using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class SupplierComplaintsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public SupplierComplaintsController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "SupplierComplaintsController is currently being repaired." });
}

