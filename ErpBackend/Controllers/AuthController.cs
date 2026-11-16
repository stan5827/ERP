using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/[controller]")]
[ApiController]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null) return Unauthorized(ApiHelper.Fail("Invalid credentials"));
        return Ok(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public IActionResult Register() =>
        StatusCode(StatusCodes.Status410Gone, ApiHelper.Fail("Self-service registration is disabled. Contact an administrator."));

    [HttpPost("change-password")]
    [Authorize(Policy = "AuthenticatedAccount")]
    public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();
        var result = await _authService.ChangePasswordAsync(userId, request.OldPassword ?? "", request.NewPassword);
        
        if (!result.Success)
        {
            return Fail(result.Error ?? "Password change failed");
        }
        
        return Ok("Password changed successfully");
    }

    [HttpPost("admin-reset")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> AdminReset([FromBody] AdminResetRequest request)
    {
        var actorId = GetUserId();
        if (actorId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Authenticated user identity is required" });

        var configuredSecret = Environment.GetEnvironmentVariable("ADMIN_RESET_SECRET")
            ?? _configuration["AdminReset:Secret"];
        if (string.IsNullOrWhiteSpace(configuredSecret)
            || configuredSecret.StartsWith("${", StringComparison.Ordinal))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false, message = "Administrative password reset is not configured" });

        if (!string.Equals(request.Secret, configuredSecret, StringComparison.Ordinal))
            return BadRequest(new { success = false, message = "Invalid secret" });
            
        var result = await _authService.ResetUserPasswordAsync(request.Username, request.NewPassword, actorId);
        if (!result)
            return BadRequest(new { success = false, message = "Reset failed" });
            
        return Ok(new { success = true, message = "Password reset successfully" });
    }
}
