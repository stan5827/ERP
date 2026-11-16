using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class RndController : BaseController
{
    public RndController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "RndController is currently being repaired." });
}

