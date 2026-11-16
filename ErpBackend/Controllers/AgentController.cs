using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class AgentController : BaseController
{
    public AgentController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "AgentController is currently being repaired." });
}

