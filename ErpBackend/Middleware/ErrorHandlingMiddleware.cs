using System.Net;
using System.Text.Json;
using ErpBackend.DTOs;

namespace ErpBackend.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString()[..8];
        
        var (statusCode, errorCode, message) = GetErrorDetails(exception);

        _logger.LogError(
            exception,
            "[{RequestId}] Error occurred - StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, Message: {Message}",
            requestId, statusCode, errorCode, message
        );

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = new
            {
                ErrorCode = errorCode,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow.ToString("o")
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        
        await context.Response.WriteAsync(json);
    }

    private (int statusCode, string errorCode, string message) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "UNAUTHORIZED", "未經授權，請重新登入"),
            KeyNotFoundException => ((int)HttpStatusCode.NotFound, "NOT_FOUND", "資源不存在"),
            InvalidOperationException => ((int)HttpStatusCode.BadRequest, "INVALID_OPERATION", exception.Message),
            ArgumentException => ((int)HttpStatusCode.BadRequest, "INVALID_ARGUMENT", exception.Message),
            TimeoutException => ((int)HttpStatusCode.RequestTimeout, "TIMEOUT", "操作逾時，請稍後再試"),
            Microsoft.EntityFrameworkCore.DbUpdateException => ((int)HttpStatusCode.Conflict, "DB_CONFLICT", "資料庫操作失敗"),
            System.ComponentModel.DataAnnotations.ValidationException => ((int)HttpStatusCode.BadRequest, "VALIDATION_ERROR", exception.Message),
            OperationCanceledException => ((int)HttpStatusCode.Gone, "OPERATION_CANCELLED", "操作已被取消"),
            NotImplementedException => ((int)HttpStatusCode.NotImplemented, "NOT_IMPLEMENTED", "功能尚未實作"),
            _ => ((int)HttpStatusCode.InternalServerError, "INTERNAL_ERROR", $"伺服器發生錯誤：{exception.Message}")
        };
    }
}

public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ErrorHandlingMiddleware>();
    }
}