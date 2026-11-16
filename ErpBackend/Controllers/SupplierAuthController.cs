using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class SupplierAuthController : BaseController
{
    public SupplierAuthController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "SupplierAuthController is currently being repaired." });
}

