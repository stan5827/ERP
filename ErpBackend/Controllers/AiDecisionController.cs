using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class AiDecisionController : BaseController
{
    public AiDecisionController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "AiDecisionController is currently being repaired." });
}

