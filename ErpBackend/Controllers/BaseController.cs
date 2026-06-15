using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/erp/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> Ok<T>(T data, string? message = null, int? totalCount = null)
        => base.Ok(ApiResponse<T>.Ok(data, message, totalCount));

    protected ActionResult<ApiResponse> Ok(string? message = null)
        => base.Ok(ApiResponse.Ok(message));

    protected ActionResult<ApiResponse> OkObject(object data, string? message = null)
        => base.Ok(ApiResponse.Ok(data, message));

    protected ActionResult<ApiResponse> Fail(string message)
        => base.Ok(ApiResponse.Fail(message));

    protected ActionResult<ApiResponse<T>> Fail<T>(string message)
        => base.Ok(ApiResponse<T>.Fail(message));

    protected int GetPage() => int.TryParse(HttpContext.Request.Query["page"], out var p) ? Math.Max(1, p) : 1;
    protected int GetPageSize() => int.TryParse(HttpContext.Request.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
    protected string? GetSearch() => HttpContext.Request.Query["search"].FirstOrDefault();
    protected string? GetSortBy() => HttpContext.Request.Query["sortBy"].FirstOrDefault();
    protected bool GetSortDesc() => bool.TryParse(HttpContext.Request.Query["sortDesc"], out var d) && d;

    protected Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
