using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class SupplierAsnController : BaseController
{
    public SupplierAsnController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "SupplierAsnController is currently being repaired." });
}

