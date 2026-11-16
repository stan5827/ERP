using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class PermissionsController : BaseController
{
    public PermissionsController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "PermissionsController is currently being repaired." });
}

