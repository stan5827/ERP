using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class WorkCenterCostsController : BaseController
{
    public WorkCenterCostsController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "WorkCenterCostsController is currently being repaired." });
}

