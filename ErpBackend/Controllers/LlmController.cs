using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/[controller]")]
public class LlmController : BaseController
{
    private readonly ILlmService _llmService;

    public LlmController(ILlmService llmService)
    {
        _llmService = llmService;
    }

    [HttpPost("complete")]
    [PermissionAuthorize("ai", "execute")]
    public ActionResult<ApiResponse<LlmResponse>> Complete()
    {
        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            ApiHelper.Fail("LLM completion is disabled until an approved data egress policy is configured."));
    }

    // GET /api/erp/llm/model
    [HttpGet("model")]
    public async Task<ActionResult<ApiResponse<string>>> GetModel()
    {
        var model = await _llmService.GetModelNameAsync();
        return Ok<string>(model);
    }

    // GET /api/erp/llm/ping
    [HttpGet("ping")]
    public IActionResult Ping() => new OkObjectResult("pong");
}
