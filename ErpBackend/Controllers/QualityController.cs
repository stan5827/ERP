using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class QualityController : BaseController
{
    public QualityController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "QualityController is currently being repaired." });
}

