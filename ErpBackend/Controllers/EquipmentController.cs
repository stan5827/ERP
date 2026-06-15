using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class EquipmentController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public EquipmentController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "EquipmentController is currently being repaired." });
}

