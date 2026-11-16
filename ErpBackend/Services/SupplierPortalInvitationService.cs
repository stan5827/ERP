using System.Security.Claims;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Portal;
using ErpBackend.Models.System;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ErpBackend.Services;

public sealed class SupplierPortalInvitationService(
    ErpGeneralDbContext db,
    IKeycloakSupplierIdentityClient keycloakClient,
    IConfiguration configuration,
    ILogger<SupplierPortalInvitationService> logger) : ISupplierPortalInvitationService
{
    private static readonly HashSet<string> AllowedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "viewer", "sales", "finance" };

    public async Task<IReadOnlyList<SupplierPortalUserStatusResponseDto>> GetUsersAsync(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        return await db.SupplierUsers
            .AsNoTracking()
            .Where(user => user.SupplierId == supplierId)
            .OrderBy(user => user.Username)
            .Select(user => new SupplierPortalUserStatusResponseDto(
                user.Id,
                user.SupplierId,
                user.Email ?? string.Empty,
                user.ContactName,
                user.Role,
                user.IsActive,
                user.PortalInvitationStatus,
                user.InvitationExpiresAt,
                user.InvitationSentAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierPortalInvitationResponseDto> InviteAsync(
        Guid supplierId,
        CreateSupplierPortalInvitationRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        keycloakClient.ValidateConfiguration();
        ValidateInvitationLifespan();
        var email = request.Email.Trim().ToLowerInvariant();
        var role = request.Role.Trim().ToLowerInvariant();
        if (!AllowedRoles.Contains(role))
            throw new ArgumentException("供應商入口角色不在允許清單中。");

        var supplier = await db.Suppliers
            .SingleOrDefaultAsync(item => item.Id == supplierId, cancellationToken)
            ?? throw new KeyNotFoundException("供應商不存在。");
        EnsureSupplierCanReceiveInvitation(supplier);

        var emailAlreadyUsed = await db.SupplierUsers.AnyAsync(
            user => user.Email != null && user.Email.ToLower() == email,
            cancellationToken);
        if (emailAlreadyUsed)
            throw new InvalidOperationException("此 Email 已有供應商入口帳號，請使用既有帳號管理流程。");

        var user = new SupplierUser
        {
            SupplierId = supplierId,
            Username = $"sp-{Guid.NewGuid():N}",
            PasswordHash = string.Empty,
            Email = email,
            ContactName = request.ContactName.Trim(),
            Role = role,
            IsActive = false,
            PortalInvitationStatus = "Provisioning",
            InvitationCorrelationId = Guid.NewGuid(),
            InvitationExpiresAt = DateTime.UtcNow.AddSeconds(GetInvitationLifespanSeconds()),
            InvitationAttemptCount = 1
        };
        db.SupplierUsers.Add(user);
        AddAuditLog(
            actorUserId,
            "SUPPLIER_PORTAL_INVITATION_CREATED",
            user,
            user.InvitationCorrelationId!.Value.ToString());
        await db.SaveChangesAsync(cancellationToken);

        return await ProvisionAsync(user, actorUserId, cancellationToken);
    }

    public async Task<SupplierPortalInvitationResponseDto> RetryAsync(
        Guid userId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var user = await db.SupplierUsers
            .Include(item => item.Supplier)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("供應商入口帳號不存在。");
        keycloakClient.ValidateConfiguration();
        EnsureSupplierCanReceiveInvitation(user.Supplier);
        if (user.PortalInvitationStatus != "ProvisioningFailed"
            || user.InvitationCorrelationId is null
            || user.IdentityProviderSubject is not null)
            throw new InvalidOperationException("只有尚未綁定 Keycloak subject 的失敗邀請可以重試。");

        user.PortalInvitationStatus = "Provisioning";
        user.InvitationAttemptCount++;
        user.InvitationLastError = null;
        AddAuditLog(actorUserId, "SUPPLIER_PORTAL_INVITATION_RETRY", user, null);
        await db.SaveChangesAsync(cancellationToken);

        return await ProvisionAsync(user, actorUserId, cancellationToken);
    }

    public async Task<SupplierPortalInvitationResponseDto> ResendAsync(
        Guid userId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        keycloakClient.ValidateConfiguration();
        var user = await db.SupplierUsers
            .Include(item => item.Supplier)
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("供應商入口帳號不存在。");
        EnsureSupplierCanReceiveInvitation(user.Supplier);
        if (user.PortalInvitationStatus != "InvitationSent"
            || string.IsNullOrWhiteSpace(user.IdentityProviderSubject)
            || user.InvitationCorrelationId is null)
            throw new InvalidOperationException("此帳號目前沒有可重寄的 Keycloak 邀請。");

        user.InvitationAttemptCount++;
        try
        {
            await keycloakClient.ResendInvitationAsync(user.IdentityProviderSubject, cancellationToken);
            user.InvitationExpiresAt = DateTime.UtcNow.AddSeconds(GetInvitationLifespanSeconds());
            user.InvitationSentAt = DateTime.UtcNow;
            user.InvitationLastError = null;
            AddAuditLog(actorUserId, "SUPPLIER_PORTAL_INVITATION_RESENT", user, null);
            await db.SaveChangesAsync(cancellationToken);
            return ToInvitationResponse(user);
        }
        catch (KeycloakSupplierIdentityException ex)
        {
            user.InvitationLastError = ex.ErrorCode;
            AddAuditLog(actorUserId, "SUPPLIER_PORTAL_INVITATION_RESEND_FAILED", user, ex.ErrorCode);
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task CancelAsync(
        Guid userId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var user = await db.SupplierUsers.SingleOrDefaultAsync(
            item => item.Id == userId,
            cancellationToken) ?? throw new KeyNotFoundException("供應商入口帳號不存在。");
        if (user.PortalInvitationStatus == "Provisioning")
            throw new InvalidOperationException("邀請仍在處理中，請稍後再取消或重試。");
        var cancellationRetry = user.PortalInvitationStatus == "Cancelled";
        if (cancellationRetry
            && (string.IsNullOrWhiteSpace(user.IdentityProviderSubject)
                || string.IsNullOrWhiteSpace(user.InvitationLastError)))
            return;

        if (!cancellationRetry)
        {
            user.IsActive = false;
            user.PortalInvitationStatus = "Cancelled";
            user.InvitationExpiresAt = DateTime.UtcNow;
            user.InvitationLastError = null;
            AddAuditLog(actorUserId, "SUPPLIER_PORTAL_INVITATION_CANCELLED", user, null);
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(user.IdentityProviderSubject))
        {
            try
            {
                await keycloakClient.DisableUserAsync(user.IdentityProviderSubject, cancellationToken);
            }
            catch (KeycloakSupplierIdentityException ex)
            {
                user.InvitationLastError = ex.ErrorCode;
                AddAuditLog(actorUserId, "SUPPLIER_PORTAL_IDENTITY_DISABLE_FAILED", user, ex.ErrorCode);
                await db.SaveChangesAsync(cancellationToken);
                throw;
            }
            catch (InvalidOperationException)
            {
                user.InvitationLastError = "ProviderNotConfigured";
                AddAuditLog(actorUserId, "SUPPLIER_PORTAL_IDENTITY_DISABLE_FAILED", user, "ProviderNotConfigured");
                await db.SaveChangesAsync(cancellationToken);
                throw new KeycloakSupplierIdentityException("ProviderNotConfigured");
            }

            if (cancellationRetry && !string.IsNullOrWhiteSpace(user.InvitationLastError))
            {
                user.InvitationLastError = null;
                AddAuditLog(actorUserId, "SUPPLIER_PORTAL_IDENTITY_DISABLE_RETRIED", user, null);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<SupplierPortalInvitationCompletedResponseDto> CompleteAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        keycloakClient.ValidateConfiguration();
        var subject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
            throw new UnauthorizedAccessException("供應商身分缺少 subject。");

        var user = await db.SupplierUsers
            .Include(item => item.Supplier)
            .SingleOrDefaultAsync(item => item.IdentityProviderSubject == subject, cancellationToken)
            ?? throw new KeyNotFoundException("邀請綁定不存在。");
        if (user.PortalInvitationStatus == "Active" && user.IsActive)
        {
            EnsureSupplierCanReceiveInvitation(user.Supplier);
            return new SupplierPortalInvitationCompletedResponseDto(
                "Active",
                user.Supplier.SupplierCode,
                user.Supplier.SupplierName);
        }

        if (user.PortalInvitationStatus != "InvitationSent"
            || user.IsActive
            || user.InvitationExpiresAt <= DateTime.UtcNow
            || user.InvitationCorrelationId is null)
            throw new InvalidOperationException("邀請已失效或目前不可啟用。");

        EnsureSupplierCanReceiveInvitation(user.Supplier);
        var identityStatus = await keycloakClient.GetUserStatusAsync(subject, cancellationToken);
        var requiresTotp = configuration.GetValue("SupplierIdentity:RequireTotp", true);
        if (!identityStatus.IsEnabled
            || !identityStatus.EmailVerified
            || identityStatus.HasPendingRequiredActions
            || identityStatus.InvitationCorrelationId != user.InvitationCorrelationId
            || requiresTotp && !identityStatus.HasTotpCredential)
            throw new InvalidOperationException("請先完成邀請信中的 Email、密碼與雙因素驗證設定。");

        user.IsActive = true;
        user.PortalInvitationStatus = "Active";
        user.InvitationLastError = null;
        user.LastLoginAt = DateTime.UtcNow;
        AddAuditLog(null, "SUPPLIER_PORTAL_INVITATION_ACTIVATED", user, null);
        await db.SaveChangesAsync(cancellationToken);

        return new SupplierPortalInvitationCompletedResponseDto(
            "Active",
            user.Supplier.SupplierCode,
            user.Supplier.SupplierName);
    }

    private async Task<SupplierPortalInvitationResponseDto> ProvisionAsync(
        SupplierUser user,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            var subject = await keycloakClient.CreateOrFindInvitedUserAsync(
                user.Email!,
                user.ContactName ?? string.Empty,
                user.InvitationCorrelationId!.Value,
                user.Id,
                cancellationToken);

            var subjectAlreadyBound = await db.SupplierUsers.AnyAsync(
                other => other.Id != user.Id && other.IdentityProviderSubject == subject,
                cancellationToken);
            if (subjectAlreadyBound)
                throw new KeycloakSupplierIdentityException("IdentityAlreadyBound");

            user.IdentityProviderSubject = subject;
            user.PortalInvitationStatus = "InvitationSent";
            user.InvitationSentAt = DateTime.UtcNow;
            user.InvitationLastError = null;
            AddAuditLog(actorUserId, "SUPPLIER_PORTAL_INVITATION_SENT", user, null);
            await db.SaveChangesAsync(cancellationToken);
            return ToInvitationResponse(user);
        }
        catch (KeycloakSupplierIdentityException ex)
        {
            user.IdentityProviderSubject = null;
            user.PortalInvitationStatus = "ProvisioningFailed";
            user.InvitationLastError = ex.ErrorCode;
            AddAuditLog(actorUserId, "SUPPLIER_PORTAL_INVITATION_FAILED", user, ex.ErrorCode);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning(
                "Supplier portal invite provisioning failed for supplier user {SupplierUserId}: {ErrorCode}",
                user.Id,
                ex.ErrorCode);
            throw;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_SupplierUsers_IdentityProviderSubject"
            })
        {
            user.IdentityProviderSubject = null;
            user.PortalInvitationStatus = "ProvisioningFailed";
            user.InvitationLastError = "IdentityAlreadyBound";
            AddAuditLog(actorUserId, "SUPPLIER_PORTAL_INVITATION_FAILED", user, "IdentityAlreadyBound");
            await db.SaveChangesAsync(cancellationToken);
            throw new KeycloakSupplierIdentityException("IdentityAlreadyBound");
        }
    }

    private void AddAuditLog(Guid? actorUserId, string action, SupplierUser user, string? details)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = actorUserId,
            Action = action,
            EntityType = nameof(SupplierUser),
            EntityId = user.Id,
            Details = details,
            TableName = "SupplierUsers",
            CreatedAt = DateTime.UtcNow
        });
    }

    private int GetInvitationLifespanSeconds() =>
        configuration.GetValue("SupplierIdentity:InvitationLifespanSeconds", 86400);

    private void ValidateInvitationLifespan()
    {
        var lifespan = GetInvitationLifespanSeconds();
        if (lifespan is < 300 or > 604800)
            throw new InvalidOperationException("邀請有效期間設定必須介於 300 至 604800 秒。");
    }

    private static void EnsureSupplierCanReceiveInvitation(ErpBackend.Models.Procurement.Supplier supplier)
    {
        if (!string.Equals(supplier.ApprovalStatus, "APPROVED", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(supplier.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("只有已核准且啟用中的供應商可以邀請入口帳號。");
    }

    private static SupplierPortalInvitationResponseDto ToInvitationResponse(SupplierUser user) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.PortalInvitationStatus,
            user.InvitationExpiresAt,
            user.InvitationAttemptCount);
}
