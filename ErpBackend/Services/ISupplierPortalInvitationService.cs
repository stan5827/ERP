using System.Security.Claims;
using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface ISupplierPortalInvitationService
{
    Task<IReadOnlyList<SupplierPortalUserStatusResponseDto>> GetUsersAsync(
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierPortalInvitationResponseDto> InviteAsync(
        Guid supplierId,
        CreateSupplierPortalInvitationRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<SupplierPortalInvitationResponseDto> RetryAsync(
        Guid userId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<SupplierPortalInvitationResponseDto> ResendAsync(
        Guid userId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task CancelAsync(Guid userId, Guid actorUserId, CancellationToken cancellationToken);

    Task<SupplierPortalInvitationCompletedResponseDto> CompleteAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);
}
