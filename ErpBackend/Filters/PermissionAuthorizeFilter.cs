using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.System;
using ErpBackend.Services;

namespace ErpBackend.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAuthorizeAttribute : Attribute
{
    public string Module { get; set; } = null!;
    public string Action { get; set; } = null!;

    public PermissionAuthorizeAttribute(string module, string action)
    {
        Module = module;
        Action = action;
    }
}

public class PermissionAuthorizeFilter : IAsyncAuthorizationFilter
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<PermissionAuthorizeFilter> _logger;
    private readonly IAgentDisclosureAuditService? _agentDisclosureAudit;

    public PermissionAuthorizeFilter(
        ErpGeneralDbContext db,
        ILogger<PermissionAuthorizeFilter> logger,
        IAgentDisclosureAuditService? agentDisclosureAudit = null)
    {
        _db = db;
        _logger = logger;
        _agentDisclosureAudit = agentDisclosureAudit;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint == null || endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null)
            return;

        if (endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(data => string.Equals(data.Policy, "SupplierPortal", StringComparison.Ordinal)
                || string.Equals(data.Policy, "SupplierPortalInvitationCompletion", StringComparison.Ordinal)))
            return;

        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            await RecordAgentAuthorizationDenialAsync(context, null, "unauthenticated");
            context.Result = new UnauthorizedObjectResult(new { message = "請先登入系統" });
            return;
        }

        var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var controllerName = actionDescriptor?.ControllerName + "Controller";
        var actionName = actionDescriptor?.ActionName ?? string.Empty;

        if (string.Equals(controllerName, "AuthController", StringComparison.OrdinalIgnoreCase)
            && string.Equals(actionName, "ChangePassword", StringComparison.OrdinalIgnoreCase))
            return;
        if (string.Equals(controllerName, "SystemSettingsController", StringComparison.OrdinalIgnoreCase)
            && string.Equals(actionName, "GetCurrentUserModules", StringComparison.OrdinalIgnoreCase))
            return;
        if (string.Equals(controllerName, "SystemSettingsController", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(actionName, "GetUserTheme", StringComparison.OrdinalIgnoreCase)
                || string.Equals(actionName, "UpdateUserTheme", StringComparison.OrdinalIgnoreCase)))
            return;
        // ESS endpoints enforce the employee_id claim and scope each operation to that employee.
        if (string.Equals(controllerName, "EmployeeEssController", StringComparison.OrdinalIgnoreCase))
            return;

        var permissionAttr = endpoint.Metadata.GetMetadata<PermissionAuthorizeAttribute>();
        string moduleId;
        string action;
        if (permissionAttr != null)
        {
            moduleId = permissionAttr.Module;
            action = permissionAttr.Action;
        }
        else if (!PermissionCatalog.TryGetModule(controllerName ?? string.Empty, out moduleId))
        {
            _logger.LogWarning("Denied access to unmapped controller {Controller}", controllerName);
            context.Result = new ForbidResult();
            return;
        }
        else
        {
            action = PermissionCatalog.ResolveAction(
                context.HttpContext.Request.Method,
                actionName);
        }

        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.HttpContext.User.FindFirst("sub")?.Value
            ?? context.HttpContext.User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            await RecordAgentAuthorizationDenialAsync(context, null, "user_id_claim_missing_or_invalid");
            context.Result = new UnauthorizedObjectResult(new { message = "請先登入系統" });
            return;
        }

        if (context.HttpContext.User.IsInRole("Admin"))
            return;

        var userPermissions = await _db.UserRoles
            .Where(userRole => userRole.UserId == userId && userRole.Role.IsActive)
            .SelectMany(userRole => userRole.Role.RolePermissions)
            .Where(rolePermission => rolePermission.Permission.IsActive)
            .Select(rolePermission => rolePermission.Permission.PermissionCode)
            .Distinct()
            .ToListAsync(context.HttpContext.RequestAborted);
        var grantedCodes = userPermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requiredPermission = PermissionCatalog.GetActionCode(moduleId, action);
        var hasAccess = grantedCodes.Contains(moduleId)
            || (string.Equals(action, "view", StringComparison.OrdinalIgnoreCase)
                && grantedCodes.Contains($"{moduleId}:page"))
            || grantedCodes.Contains(requiredPermission);

        if (!hasAccess)
        {
            _logger.LogWarning("User {UserId} denied access to {Permission}", userId, requiredPermission);
            await RecordAgentAuthorizationDenialAsync(context, userId, "module_permission_denied");
            context.Result = new ForbidResult();
        }
    }

    private Task RecordAgentAuthorizationDenialAsync(
        AuthorizationFilterContext context,
        Guid? actorUserId,
        string reason)
    {
        var controllerName = (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerName;
        if (!string.Equals(controllerName, "AgentWip", StringComparison.OrdinalIgnoreCase)
            || _agentDisclosureAudit is null)
            return Task.CompletedTask;

        return _agentDisclosureAudit.RecordAsync(
            new AgentDisclosureAuditEntryDto(
                actorUserId,
                context.HttpContext.TraceIdentifier,
                "permission_blocked",
                reason,
                null,
                [],
                context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                context.HttpContext.Request.Headers.UserAgent.ToString()),
            context.HttpContext.RequestAborted);
    }
}

// 記錄所有 API 操作的 Audit Log Filter
public class AuditLogFilter : IAsyncActionFilter
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<AuditLogFilter> _logger;

    public AuditLogFilter(ErpGeneralDbContext db, ILogger<AuditLogFilter> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        context.HttpContext.Items["RequestStartTime"] = DateTime.UtcNow;
        await next();
        if (context.HttpContext.Items[AgentDisclosureAuditService.AuditRecordedItemKey] is true)
            return;

        if (context.HttpContext.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Any(data => string.Equals(data.Policy, "SupplierPortal", StringComparison.Ordinal)
                || string.Equals(data.Policy, "SupplierPortalInvitationCompletion", StringComparison.Ordinal)) == true)
            return;

        var startTime = (DateTime?)context.HttpContext.Items["RequestStartTime"];
        if (startTime == null) return;

        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.HttpContext.User.FindFirst("sub")?.Value
            ?? context.HttpContext.User.FindFirst("user_id")?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            var method = context.HttpContext.Request.Method;
            var path = context.HttpContext.Request.Path;
            var statusCode = context.HttpContext.Response.StatusCode;
            var duration = (DateTime.UtcNow - startTime.Value).TotalMilliseconds;

            // 記錄 CRUD 操作
            string? action = method switch
            {
                "POST" => "create",
                "PUT" or "PATCH" => "update",
                "DELETE" => "delete",
                "GET" => "read",
                _ => null
            };

            if (action != null && path.StartsWithSegments("/api"))
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    Action = $"{action} {path}",
                    TableName = ExtractTableName(path),
                    IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.HttpContext.Request.Headers["User-Agent"].FirstOrDefault(),
                    CreatedAt = DateTime.UtcNow
                };

                _db.AuditLogs.Add(log);
                try
                {
                    await _db.SaveChangesAsync(context.HttpContext.RequestAborted);
                }
                catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
                {
                    _logger.LogWarning("Audit log write was canceled after the request was aborted for {Path}.", path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write audit log for {Path}.", path);
                }
            }
        }
    }

    private static string? ExtractTableName(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && segments[0] == "api")
        {
            return segments[1]; // 如 "salesorders", "employees" 等
        }
        return null;
    }
}