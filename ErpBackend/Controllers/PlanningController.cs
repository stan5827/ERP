using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/planning")]
public class PlanningController : BaseController
{
    private readonly IPlanningService _planningService;
    private readonly IAIPlanningService _aiPlanningService;
    private readonly IToolFactory _toolFactory;

    public PlanningController(
        IPlanningService planningService,
        IAIPlanningService aiPlanningService,
        IToolFactory toolFactory)
    {
        _planningService = planningService;
        _aiPlanningService = aiPlanningService;
        _toolFactory = toolFactory;
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<ApiResponse<List<PlanningSuggestionDto>>>> GetSuggestions()
        => Ok(await _planningService.GetSuggestionsAsync());

    [HttpPost("suggestions")]
    public async Task<ActionResult<ApiResponse<List<PlanningSuggestionDto>>>> PersistSuggestions()
        => Ok(await _planningService.GetSuggestionsAsync());

    [HttpPost("plan")]
    [PermissionAuthorize("ai", "create")]
    public async Task<ActionResult<ApiResponse<PlanningResponse>>> CreatePlan([FromBody] PlanningRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TaskDescription))
            return BadRequest(ApiResponse<PlanningResponse>.Fail("任務描述不可空白"));

        return Ok(await _aiPlanningService.CreatePlanAsync(request));
    }

    [HttpPost("execute")]
    [PermissionAuthorize("ai", "execute")]
    public async Task<ActionResult<ApiResponse<ExecutionResponse>>> ExecutePlan(
        [FromBody] ExecutionRequest request)
        => Ok(await _aiPlanningService.ExecutePlanAsync(request));

    [HttpPost("review")]
    [PermissionAuthorize("ai", "approve")]
    public async Task<ActionResult<ApiResponse<ReviewResponse>>> ReviewResult(
        [FromBody] ReviewRequest request)
        => Ok(await _aiPlanningService.ReviewResultAsync(request));

    [HttpGet("kpi")]
    [PermissionAuthorize("ai", "view")]
    public async Task<ActionResult<ApiResponse<KpiValidationResult>>> GetKpiValidation()
        => Ok(await _aiPlanningService.ValidateKpiAsync());

    [HttpGet("health")]
    [PermissionAuthorize("ai", "view")]
    public ActionResult<ApiResponse<PlanningHealthDto>> GetHealth()
        => Ok(new PlanningHealthDto
        {
            Status = "healthy",
            Service = "AIPlanningService",
            Version = "1.0",
            Features = ["planning", "review", "tool-approval-required"]
        });

    [HttpGet("state/{executionId}")]
    [PermissionAuthorize("ai", "view")]
    public async Task<ActionResult<ApiResponse<AgentState>>> GetAgentState(string executionId)
        => Ok(await _aiPlanningService.GetAgentStateAsync(executionId));

    [HttpGet("history")]
    [PermissionAuthorize("ai", "view")]
    public async Task<ActionResult<ApiResponse<List<AgentState>>>> GetExecutionHistory(
        [FromQuery] int limit = 10)
        => Ok(await _aiPlanningService.GetExecutionHistoryAsync(Math.Clamp(limit, 1, 100)));

    [HttpGet("tools")]
    [PermissionAuthorize("ai", "view")]
    public ActionResult<ApiResponse<List<ErpTool>>> GetAvailableTools()
        => Ok(_aiPlanningService.GetAvailableTools());

    [HttpPost("tool/execute")]
    [PermissionAuthorize("ai", "execute")]
    public async Task<ActionResult<ApiResponse<ToolResult>>> ExecuteTool(
        [FromBody] ToolExecuteRequestDto request)
    {
        var tool = _toolFactory.GetAvailableTools()
            .FirstOrDefault(candidate => string.Equals(
                candidate.Id,
                request.ToolId,
                StringComparison.OrdinalIgnoreCase));
        if (tool is null || !AgentToolAccessPolicy.IsAllowed(tool.Id, tool.Category))
            return Forbid();

        return Ok(await _toolFactory.ExecuteToolAsync(
            tool.Id,
            request.Parameters ?? new Dictionary<string, object>()));
    }
}
