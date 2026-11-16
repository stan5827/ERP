using ErpBackend.DTOs;
using ErpBackend.Security.SupplierPortal;
using ErpBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpBackend.Controllers;

/// <summary>Completes a supplier's Keycloak invitation after required identity checks.</summary>
[ApiController]
[Route("api/supplier-portal/invitations")]
public sealed class SupplierPortalInvitationCompletionController(
    ISupplierPortalInvitationService invitationService) : ControllerBase
{
    /// <summary>Activates the ERP supplier account after email verification and MFA setup.</summary>
    [HttpPost("complete")]
    [Authorize(Policy = SupplierPortalAuthentication.InvitationCompletionPolicy)]
    [ProducesResponseType(typeof(ApiResponse<SupplierPortalInvitationCompletedResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<SupplierPortalInvitationCompletedResponseDto>>> Complete(
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await invitationService.CompleteAsync(User, cancellationToken);
            return Ok(ApiResponse<SupplierPortalInvitationCompletedResponseDto>.Ok(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(ApiResponse.Fail("供應商身分尚未驗證。"));
        }
        catch (KeyNotFoundException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Fail(ex.Message));
        }
        catch (KeycloakSupplierIdentityException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                ApiResponse.Fail("目前無法驗證 Keycloak 帳號狀態，請稍後重試。"));
        }
    }
}
