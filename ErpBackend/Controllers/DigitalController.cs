using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class DigitalController : BaseController
{
    public DigitalController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "DigitalController is currently being repaired." });
}

