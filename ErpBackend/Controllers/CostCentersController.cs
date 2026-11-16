using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class CostCentersController : BaseController
{
    public CostCentersController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "CostCentersController is currently being repaired." });
}

