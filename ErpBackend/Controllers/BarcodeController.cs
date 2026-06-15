using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class BarcodeController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public BarcodeController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "BarcodeController is currently being repaired." });
}

