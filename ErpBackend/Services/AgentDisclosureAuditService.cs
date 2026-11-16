using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.System;
using Microsoft.AspNetCore.Http;

namespace ErpBackend.Services;

public interface IAgentDisclosureAuditService
{
    Task RecordAsync(AgentDisclosureAuditEntryDto entry, CancellationToken cancellationToken = default);
}

public sealed class AgentDisclosureAuditService : IAgentDisclosureAuditService
{
    public const string AuditRecordedItemKey = "AgentDisclosureAuditRecorded";
    public const string ToolName = "agent.wip.work-order.read.v1";
    public const string PolicyVersion = "wip-scope-v1";

    private readonly ErpGeneralDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AgentDisclosureAuditService(
        ErpGeneralDbContext db,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RecordAsync(
        AgentDisclosureAuditEntryDto entry,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entry.TraceId))
            throw new ArgumentException("Disclosure audit requires a trace ID.", nameof(entry));
        if (entry.DisclosedFields is null)
            throw new ArgumentNullException(nameof(entry), "Disclosure fields must be provided.");

        if (_httpContextAccessor.HttpContext is { } httpContext)
            httpContext.Items[AuditRecordedItemKey] = true;

        var createdAt = DateTime.UtcNow;
        var fields = string.Join(",", entry.DisclosedFields.OrderBy(field => field, StringComparer.Ordinal));
        var reason = Limit(entry.DecisionReason, 1000);
        var userAgent = Limit(entry.UserAgent, 500);
        var ipAddress = Limit(entry.IpAddress, 50);
        var toolName = Limit(entry.ToolName, 100)!;
        var policyVersion = Limit(entry.PolicyVersion, 30)!;
        var dataClassification = Limit(entry.DataClassification, 20)!;
        var auditDetails = Limit(
            $"trace={entry.TraceId};tool={toolName};policy={policyVersion};outcome={entry.Outcome};fields={fields}",
            500);

        _db.DisclosureLogs.Add(new DisclosureLog
        {
            ActorUserId = entry.ActorUserId,
            TraceId = entry.TraceId,
            ToolName = toolName,
            PolicyVersion = policyVersion,
            Outcome = entry.Outcome,
            DataClassification = dataClassification,
            DisclosedFields = fields,
            DecisionReason = reason,
            ResourceId = entry.ResourceId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = createdAt
        });
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = entry.ActorUserId,
            Action = Limit(
                toolName == ToolName
                    ? $"agent_wip.read:{entry.Outcome}"
                    : $"{toolName}:{entry.Outcome}",
                50)!,
            EntityType = toolName == ToolName ? "AgentWipQuery" : toolName,
            EntityId = entry.ResourceId,
            Details = auditDetails,
            TableName = "DisclosureLogs",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = createdAt
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string? Limit(string? value, int maximumLength)
        => value is null || value.Length <= maximumLength
            ? value
            : value[..maximumLength];
}
