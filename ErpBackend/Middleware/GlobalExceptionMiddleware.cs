using System.Net;
using System.Text.Json;
using ErpBackend.DTOs;

namespace ErpBackend.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "全局異常攔截：{Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, errorCode, message) = exception switch
        {
            ArgumentException argEx => (HttpStatusCode.BadRequest, "INVALID_ARGUMENT", argEx.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", "未授權訪問"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", "資源不存在"),
            InvalidOperationException invalidOp => (HttpStatusCode.Conflict, "INVALID_OPERATION", invalidOp.Message),
            TimeoutException => (HttpStatusCode.RequestTimeout, "TIMEOUT", "操作超時"),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "系統錯誤")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            success = false,
            error = new
            {
                code = errorCode,
                message = message,
                timestamp = DateTime.UtcNow,
                traceId = context.TraceIdentifier
            }
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
