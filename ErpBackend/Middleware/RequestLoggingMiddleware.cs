using System.Text.Json;
using Microsoft.AspNetCore.HttpLogging;

namespace ErpBackend.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString()[..8];
        
        context.Items["RequestId"] = requestId;

        // 記錄請求開始
        _logger.LogInformation(
            "[{RequestId}] {Method} {Path}{Query} started - User: {UserId}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            context.User.FindFirst("sub")?.Value ?? "anonymous"
        );

        try
        {
            await _next(context);
        }
        finally
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var logLevel = context.Response.StatusCode >= 500 ? LogLevel.Error 
                : context.Response.StatusCode >= 400 ? LogLevel.Warning 
                : LogLevel.Information;

            _logger.Log(
                logLevel,
                "[{RequestId}] {Method} {Path} completed - Status: {StatusCode} - Duration: {Duration}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                duration
            );
        }
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}