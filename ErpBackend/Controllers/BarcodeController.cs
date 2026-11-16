using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class BarcodeController : BaseController
{
    public BarcodeController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "BarcodeController is currently being repaired." });
}

