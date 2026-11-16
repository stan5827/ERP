using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class ActivityTypesController : BaseController
{
    public ActivityTypesController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "ActivityTypesController is currently being repaired." });
}

