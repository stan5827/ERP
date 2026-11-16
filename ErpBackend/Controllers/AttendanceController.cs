using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class AttendanceController : BaseController
{
    public AttendanceController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "AttendanceController is currently being repaired." });
}

