using System.Security.Claims;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Portal;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class SupplierPortalInvitationServiceTests
{
    [Fact]
    public async Task InviteAsync_CreatesInactiveUserAndPersistsReturnedKeycloakSubject()
    {
        await using var db = CreateDbContext();
        var supplier = ApprovedSupplier();
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var keycloak = new Mock<IKeycloakSupplierIdentityClient>();
        keycloak.Setup(client => client.CreateOrFindInvitedUserAsync(
                "contact@example.com",
                "Vendor Contact",
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("keycloak-subject-1");

        var service = CreateService(db, keycloak.Object);
        var response = await service.InviteAsync(
            supplier.Id,
            InvitationRequest(),
            Guid.NewGuid(),
            CancellationToken.None);

        var savedUser = await db.SupplierUsers.SingleAsync();
        Assert.Equal("InvitationSent", response.InvitationStatus);
        Assert.Equal("keycloak-subject-1", savedUser.IdentityProviderSubject);
        Assert.Equal(response.UserId, savedUser.Id);
        Assert.False(savedUser.IsActive);
        Assert.Equal("InvitationSent", savedUser.PortalInvitationStatus);
        Assert.Equal(string.Empty, savedUser.PasswordHash);
        Assert.Single(await db.AuditLogs.Where(log =>
            log.Action == "SUPPLIER_PORTAL_INVITATION_SENT").ToListAsync());
    }

    [Fact]
    public async Task InviteAsync_RejectsUnapprovedSupplierBeforeCallingKeycloak()
    {
        await using var db = CreateDbContext();
        var supplier = ApprovedSupplier();
        supplier.ApprovalStatus = "PENDING";
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var keycloak = new Mock<IKeycloakSupplierIdentityClient>();
        var service = CreateService(db, keycloak.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.InviteAsync(
            supplier.Id,
            InvitationRequest(),
            Guid.NewGuid(),
            CancellationToken.None));

        keycloak.Verify(client => client.CreateOrFindInvitedUserAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(await db.SupplierUsers.ToListAsync());
    }

    [Fact]
    public async Task InviteAsync_PersistsFailedStateAndRetryBindsTheSameSubject()
    {
        await using var db = CreateDbContext();
        var supplier = ApprovedSupplier();
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var keycloak = new Mock<IKeycloakSupplierIdentityClient>();
        keycloak.SetupSequence(client => client.CreateOrFindInvitedUserAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeycloakSupplierIdentityException("ProviderUnavailable"))
            .ReturnsAsync("keycloak-subject-2");
        var service = CreateService(db, keycloak.Object);

        await Assert.ThrowsAsync<KeycloakSupplierIdentityException>(() => service.InviteAsync(
            supplier.Id,
            InvitationRequest(),
            Guid.NewGuid(),
            CancellationToken.None));

        var pendingUser = await db.SupplierUsers.SingleAsync();
        Assert.Equal("ProvisioningFailed", pendingUser.PortalInvitationStatus);
        Assert.False(pendingUser.IsActive);

        var response = await service.RetryAsync(pendingUser.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal("InvitationSent", response.InvitationStatus);
        Assert.Equal("keycloak-subject-2", pendingUser.IdentityProviderSubject);
        Assert.Equal(2, pendingUser.InvitationAttemptCount);
    }

    [Fact]
    public async Task CompleteAsync_ActivatesOnlyWhenKeycloakReportsVerifiedEmailAndTotp()
    {
        await using var db = CreateDbContext();
        var supplier = ApprovedSupplier();
        var user = PendingUser(supplier);
        db.Suppliers.Add(supplier);
        db.SupplierUsers.Add(user);
        await db.SaveChangesAsync();

        var keycloak = new Mock<IKeycloakSupplierIdentityClient>();
        keycloak.Setup(client => client.GetUserStatusAsync("keycloak-subject-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KeycloakSupplierIdentityStatus(
                true,
                true,
                false,
                true,
                user.InvitationCorrelationId));
        var service = CreateService(db, keycloak.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "keycloak-subject-3"), new Claim("email_verified", "true") },
            "test"));

        var response = await service.CompleteAsync(principal, CancellationToken.None);

        Assert.Equal("Active", response.Status);
        Assert.True(user.IsActive);
        Assert.Equal("Active", user.PortalInvitationStatus);
    }

    [Fact]
    public async Task CompleteAsync_RejectsUnconfiguredMfaCredential()
    {
        await using var db = CreateDbContext();
        var supplier = ApprovedSupplier();
        var user = PendingUser(supplier);
        db.Suppliers.Add(supplier);
        db.SupplierUsers.Add(user);
        await db.SaveChangesAsync();

        var keycloak = new Mock<IKeycloakSupplierIdentityClient>();
        keycloak.Setup(client => client.GetUserStatusAsync("keycloak-subject-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KeycloakSupplierIdentityStatus(
                true,
                true,
                false,
                false,
                user.InvitationCorrelationId));
        var service = CreateService(db, keycloak.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "keycloak-subject-3") },
            "test"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAsync(principal, CancellationToken.None));

        Assert.False(user.IsActive);
        Assert.Equal("InvitationSent", user.PortalInvitationStatus);
    }

    [Fact]
    public async Task CancelAsync_DisablesErpAccessBeforeCallingKeycloak()
    {
        await using var db = CreateDbContext();
        var supplier = ApprovedSupplier();
        var user = PendingUser(supplier);
        user.IsActive = true;
        user.PortalInvitationStatus = "Active";
        db.Suppliers.Add(supplier);
        db.SupplierUsers.Add(user);
        await db.SaveChangesAsync();

        var keycloak = new Mock<IKeycloakSupplierIdentityClient>();
        keycloak.SetupSequence(client => client.DisableUserAsync("keycloak-subject-3", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeycloakSupplierIdentityException("ProviderUnavailable"))
            .Returns(Task.CompletedTask);
        var service = CreateService(db, keycloak.Object);

        await Assert.ThrowsAsync<KeycloakSupplierIdentityException>(() =>
            service.CancelAsync(user.Id, Guid.NewGuid(), CancellationToken.None));

        Assert.False(user.IsActive);
        Assert.Equal("Cancelled", user.PortalInvitationStatus);
        Assert.Equal("ProviderUnavailable", user.InvitationLastError);

        await service.CancelAsync(user.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.Null(user.InvitationLastError);
        keycloak.Verify(
            client => client.DisableUserAsync("keycloak-subject-3", It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private static SupplierPortalInvitationService CreateService(
        ErpGeneralDbContext db,
        IKeycloakSupplierIdentityClient keycloakClient) =>
        new(
            db,
            keycloakClient,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SupplierIdentity:InvitationLifespanSeconds"] = "86400",
                ["SupplierIdentity:RequireTotp"] = "true"
            }).Build(),
            NullLogger<SupplierPortalInvitationService>.Instance);

    private static ErpGeneralDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Supplier ApprovedSupplier() =>
        new()
        {
            SupplierCode = "S-INVITE",
            SupplierName = "Invitation Supplier",
            ApprovalStatus = "APPROVED",
            Status = "ACTIVE"
        };

    private static SupplierUser PendingUser(Supplier supplier) =>
        new()
        {
            Supplier = supplier,
            SupplierId = supplier.Id,
            Username = "sp-pending-user",
            PasswordHash = string.Empty,
            Email = "contact@example.com",
            ContactName = "Vendor Contact",
            Role = "sales",
            IsActive = false,
            PortalInvitationStatus = "InvitationSent",
            IdentityProviderSubject = "keycloak-subject-3",
            InvitationCorrelationId = Guid.NewGuid(),
            InvitationExpiresAt = DateTime.UtcNow.AddHours(12)
        };

    private static CreateSupplierPortalInvitationRequestDto InvitationRequest() =>
        new()
        {
            Email = "Contact@Example.com",
            ContactName = "Vendor Contact",
            Role = "sales"
        };
}
