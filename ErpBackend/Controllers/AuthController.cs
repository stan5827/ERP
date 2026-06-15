using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;
using ErpBackend.Filters;

namespace ErpBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) 
    { 
        _authService = authService; 
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null) return Unauthorized(ApiHelper.Fail("帳號或密碼錯誤"));
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register([FromBody] CreateUserRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (result == null) return BadRequest(ApiHelper.Fail("帳號已存在"));
        return Ok(result, "註冊成功");
    }

    [HttpPost("change-password")]
    public async Task ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();
        var result = await _authService.ChangePasswordAsync(userId, request.OldPassword ?? "", request.NewPassword, request.SkipOldPassword);
        
        if (!result.Success)
        {
            Response.StatusCode = 400;
            await Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = false, message = result.Error ?? "密碼變更失敗" }));
            return;
        }
        
        await Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = true, message = "密碼已成功變更，請重新登入" }));
    }

    [HttpPost("admin-reset")]
    public async Task<ActionResult<object>> AdminReset([FromBody] AdminResetRequest request)
    {
        var resetSecret = Environment.GetEnvironmentVariable("ERP_ADMIN_RESET_SECRET");
        if (string.IsNullOrWhiteSpace(resetSecret))
            return StatusCode(503, new { success = false, message = "Admin reset is disabled." });

        if (request.Secret != resetSecret)
            return BadRequest(new { success = false, message = "Invalid secret" });
            
        var result = await _authService.ResetUserPasswordAsync(request.Username, request.NewPassword);
        if (!result)
            return BadRequest(new { success = false, message = "重設失敗" });
            
        return Ok(new { success = true, message = "密碼已重設" });
    }
}

public class ChangePasswordRequest
{
    public string? OldPassword { get; set; }
    public string NewPassword { get; set; } = null!;
    public bool SkipOldPassword { get; set; } = false;
}

public class AdminResetRequest
{
    public string Secret { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
