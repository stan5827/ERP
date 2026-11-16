using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class AllocationController : BaseController
{
    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "AllocationController is currently being repaired." });
}
