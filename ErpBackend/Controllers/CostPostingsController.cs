using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class CostPostingsController : BaseController
{
    public CostPostingsController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "CostPostingsController is currently being repaired." });
}

