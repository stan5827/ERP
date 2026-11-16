using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class AiController : BaseController
{
    public AiController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "AiController is currently being repaired." });
}

