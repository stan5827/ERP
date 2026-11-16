namespace ErpBackend.Services;

public interface IKeycloakSupplierIdentityClient
{
    void ValidateConfiguration();

    Task<string> CreateOrFindInvitedUserAsync(
        string email,
        string contactName,
        Guid invitationCorrelationId,
        Guid supplierUserId,
        CancellationToken cancellationToken);

    Task ResendInvitationAsync(string subject, CancellationToken cancellationToken);

    Task DisableUserAsync(string subject, CancellationToken cancellationToken);

    Task<KeycloakSupplierIdentityStatus> GetUserStatusAsync(
        string subject,
        CancellationToken cancellationToken);
}

public sealed record KeycloakSupplierIdentityStatus(
    bool IsEnabled,
    bool EmailVerified,
    bool HasPendingRequiredActions,
    bool HasTotpCredential,
    Guid? InvitationCorrelationId);
