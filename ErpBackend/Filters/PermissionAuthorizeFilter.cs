using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.System;

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

    public PermissionAuthorizeFilter(ErpGeneralDbContext db, ILogger<PermissionAuthorizeFilter> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 取得控制器或方法上的權限屬性
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint == null)
        {
            return;
        }

        var permissionAttr = endpoint.Metadata.GetMetadata<PermissionAuthorizeAttribute>();
        if (permissionAttr == null)
        {
            return; // 沒有權限要求，放行
        }

        // 取得目前登入的使用者 ID
        var userIdClaim = context.HttpContext.User.FindFirst("sub")?.Value 
            ?? context.HttpContext.User.FindFirst("user_id")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "請先登入系統" });
            return;
        }

        // 取得使用者的權限清單
        var userPermissions = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => _db.RolePermissions
                .Where(rp => rp.RoleId == ur.RoleId)
                .Select(rp => rp.Permission.PermissionCode))
            .Distinct()
            .ToListAsync();

        // 檢查是否有權限 (格式: Module.Action)
        var requiredPermission = $"{permissionAttr.Module}.{permissionAttr.Action}";
        
        if (!userPermissions.Contains("admin.*") && !userPermissions.Contains(requiredPermission))
        {
            _logger.LogWarning("User {UserId} denied access to {Permission}", userId, requiredPermission);
            context.Result = new ForbidResult();
        }
    }
}

// 記錄所有 API 操作的 Audit Log Filter
public class AuditLogFilter : IActionFilter
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<AuditLogFilter> _logger;

    public AuditLogFilter(ErpGeneralDbContext db, ILogger<AuditLogFilter> logger)
    {
        _db = db;
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        context.HttpContext.Items["RequestStartTime"] = DateTime.UtcNow;
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        var startTime = (DateTime?)context.HttpContext.Items["RequestStartTime"];
        if (startTime == null) return;

        var userIdClaim = context.HttpContext.User.FindFirst("sub")?.Value 
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
                _ = _db.SaveChangesAsync(); // Fire and forget
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