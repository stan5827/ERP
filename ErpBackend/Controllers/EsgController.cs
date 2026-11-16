using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class EsgController : BaseController
{
    public EsgController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "EsgController is currently being repaired." });
}

