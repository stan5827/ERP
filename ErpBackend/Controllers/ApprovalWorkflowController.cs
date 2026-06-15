using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;

namespace ErpBackend.Controllers;

public class ApprovalWorkflowController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public ApprovalWorkflowController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "ApprovalWorkflowController is currently being repaired." });
}

