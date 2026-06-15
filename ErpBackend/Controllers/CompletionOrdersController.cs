using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/completion-orders")]
[Route("api/erp/CompletionOrders")]
public class CompletionOrdersController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public CompletionOrdersController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet]
    public dynamic Get()
    {
        // 暫時回傳空列表，避免 404
        return Ok(new List<object>());
    }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "CompletionOrdersController restored." });
}
