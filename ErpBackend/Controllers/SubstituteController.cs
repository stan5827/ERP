using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class SubstituteController : BaseController
{
    public SubstituteController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "SubstituteController is currently being repaired." });
}

