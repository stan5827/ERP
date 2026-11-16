using System.Security.Claims;
using ErpBackend.Configs;
using ErpBackend.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ErpBackend.Services;

public interface IAgentLlmEgressPolicyService
{
    Task<AgentLlmEgressDecisionDto> EvaluateAndAuditAsync(
        AgentLlmEgressRequestDto request,
        CancellationToken cancellationToken = default);
}

public sealed class AgentLlmEgressPolicyService : IAgentLlmEgressPolicyService
{
    public const string ToolName = "agent.llm.egress.v1";
    public const string PolicyVersion = "llm-egress-v1";

    private readonly AgentLlmEgressOptions _options;
    private readonly IAgentDisclosureAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AgentLlmEgressPolicyService(
        IOptions<AgentLlmEgressOptions> options,
        IAgentDisclosureAuditService audit,
        IHttpContextAccessor httpContextAccessor)
    {
        _options = options.Value;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AgentLlmEgressDecisionDto> EvaluateAndAuditAsync(
        AgentLlmEgressRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var reason = Evaluate(request);
        var context = _httpContextAccessor.HttpContext;
        var userId = context?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context?.User.FindFirst("sub")?.Value
            ?? context?.User.FindFirst("user_id")?.Value;
        Guid? actorUserId = Guid.TryParse(userId, out var parsedUserId)
            ? parsedUserId
            : null;

        await _audit.RecordAsync(
            new AgentDisclosureAuditEntryDto(
                actorUserId,
                context?.TraceIdentifier ?? Guid.NewGuid().ToString("N"),
                reason == "approved_for_local_provider" ? "egress_approved" : "egress_blocked",
                reason,
                null,
                [],
                context?.Connection.RemoteIpAddress?.ToString(),
                context?.Request.Headers.UserAgent.ToString(),
                ToolName,
                PolicyVersion,
                request.DataClassification),
            cancellationToken);

        return new AgentLlmEgressDecisionDto(
            reason == "approved_for_local_provider",
            reason);
    }

    private string Evaluate(AgentLlmEgressRequestDto request)
    {
        if (!_options.Enabled)
            return "egress_feature_disabled";

        if (!string.Equals(request.DataClassification, "internal", StringComparison.Ordinal))
            return "data_classification_not_approved";

        if (request.HasProviderCredentials)
            return "provider_credentials_not_allowed";

        if (string.IsNullOrWhiteSpace(request.Provider)
            || string.IsNullOrWhiteSpace(request.Model)
            || !TryGetLoopbackEndpoint(request.Endpoint, out var endpoint))
            return "destination_not_loopback";

        var isApproved = (_options.ApprovedDestinations ?? [])
            .Any(destination => destination is not null
                && string.Equals(destination.Provider, request.Provider, StringComparison.OrdinalIgnoreCase)
                && string.Equals(destination.Model, request.Model, StringComparison.Ordinal)
                && TryGetLoopbackEndpoint(destination.Endpoint, out var approvedEndpoint)
                && Uri.Compare(
                    endpoint,
                    approvedEndpoint,
                    UriComponents.AbsoluteUri,
                    UriFormat.SafeUnescaped,
                    StringComparison.Ordinal) == 0);

        return isApproved
            ? "approved_for_local_provider"
            : "provider_model_or_endpoint_not_allowlisted";
    }

    private static bool TryGetLoopbackEndpoint(string? value, out Uri endpoint)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsed)
            && parsed.Scheme == Uri.UriSchemeHttp
            && parsed.IsLoopback
            && string.IsNullOrEmpty(parsed.UserInfo)
            && string.IsNullOrEmpty(parsed.Query)
            && string.IsNullOrEmpty(parsed.Fragment)
            && parsed.Port > 0)
        {
            endpoint = parsed;
            return true;
        }

        endpoint = null!;
        return false;
    }
}

public interface IPolicyGuardedLlmService : ILlmService
{
}

public sealed class PolicyGuardedLlmService : IPolicyGuardedLlmService
{
    private readonly ILlmService _inner;
    private readonly IAgentLlmEgressPolicyService _egressPolicy;
    private readonly LlmConfig _config;

    public PolicyGuardedLlmService(
        ILlmService inner,
        IAgentLlmEgressPolicyService egressPolicy,
        LlmConfig config)
    {
        _inner = inner;
        _egressPolicy = egressPolicy;
        _config = config;
    }

    public async Task<LlmResponse> CompleteAsync(LlmRequest request)
    {
        var requestedModel = string.IsNullOrWhiteSpace(request.Model)
            ? _config.DefaultModel
            : request.Model;
        var decision = await _egressPolicy.EvaluateAndAuditAsync(
            new AgentLlmEgressRequestDto(
                _config.Provider,
                requestedModel,
                _config.BaseUrl,
                request.DataClassification,
                !string.IsNullOrWhiteSpace(_config.ApiKey)));

        if (!decision.Allowed)
        {
            return new LlmResponse
            {
                Success = false,
                Error = "LLM request blocked by the server egress policy.",
                Model = requestedModel
            };
        }

        return await _inner.CompleteAsync(request);
    }

    public Task<string> GetModelNameAsync() => _inner.GetModelNameAsync();
}
