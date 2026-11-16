using ErpBackend.DTOs;
using ErpBackend.Filters;
using ErpBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

public sealed class AgentWipController : BaseController
{
    private readonly IAgentWipQueryService _wipQueryService;

    public AgentWipController(IAgentWipQueryService wipQueryService)
    {
        _wipQueryService = wipQueryService;
    }

    [HttpGet("work-orders/{workOrderNumber}")]
    [PermissionAuthorize("manufacturing", "view")]
    public async Task<ActionResult<ApiResponse<AgentWipWorkOrderDto>>> GetWorkOrderAsync(
        string workOrderNumber,
        [FromHeader(Name = "X-Agent-Session-Id")] string? sessionId,
        CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["X-Agent-Turn-Id"] = HttpContext.TraceIdentifier;

        var actorUserId = GetUserId();
        if (actorUserId == Guid.Empty)
            return Unauthorized(new { message = "請先登入系統" });

        if (string.IsNullOrWhiteSpace(sessionId))
            sessionId = Guid.NewGuid().ToString("D");
        else if (!Guid.TryParseExact(sessionId, "D", out var parsedSessionId))
            return BadRequest(new { message = "Session ID 格式無效" });
        else
            sessionId = parsedSessionId.ToString("D");

        Response.Headers["X-Agent-Session-Id"] = sessionId;

        var result = await _wipQueryService.GetWorkOrderAsync(
            workOrderNumber,
            actorUserId,
            HttpContext.TraceIdentifier,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return result.Status switch
        {
            AgentWipQueryStatus.Success when result.Data is not null => Ok(result.Data),
            AgentWipQueryStatus.FeatureDisabled => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "WIP 助理目前尚未開放" }),
            AgentWipQueryStatus.NotInSeedCohort => StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "您目前不在 WIP 助理試用名單中" }),
            AgentWipQueryStatus.ScopeUnavailable => StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = "目前無法驗證您的有效生產資料範圍" }),
            AgentWipQueryStatus.NotFound => NotFound(new { message = "找不到可查詢的工單" }),
            AgentWipQueryStatus.DataQualityBlocked => Conflict(
                new { message = "工單資料不完整或有歧義，已停止提供 WIP 數據" }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
