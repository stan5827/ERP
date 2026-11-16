using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public class ApprovalWorkflowController : BaseController
{
    public ApprovalWorkflowController() { }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "ApprovalWorkflowController is currently being repaired." });
}

