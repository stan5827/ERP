using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class EdiController : BaseController
{
    public EdiController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "EdiController is currently being repaired." });
}

