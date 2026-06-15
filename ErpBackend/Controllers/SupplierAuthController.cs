using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class SupplierAuthController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public SupplierAuthController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "SupplierAuthController is currently being repaired." });
}

