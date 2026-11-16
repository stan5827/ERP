using System.Security.Claims;
using ErpBackend.Data;
using ErpBackend.Models.Portal;
using ErpBackend.Models.Procurement;
using ErpBackend.Security.SupplierPortal;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class SupplierPortalIdentityServiceTests
{
    [Fact]
    public async Task ResolveAsync_MapsValidatedSubjectToItsApprovedSupplier()
    {
        await using var db = CreateDbContext();
        var supplierA = CreateSupplier("A001", "Supplier A");
        var supplierB = CreateSupplier("B001", "Supplier B");
        db.Suppliers.AddRange(supplierA, supplierB);
        db.SupplierUsers.AddRange(
            CreateSupplierUser(supplierA.Id, "supplier-a-subject", "a.user"),
            CreateSupplierUser(supplierB.Id, "supplier-b-subject", "b.user"));
        await db.SaveChangesAsync();

        var service = new SupplierPortalIdentityService(db);
        var result = await service.ResolveAsync(
            CreatePrincipal("supplier-a-subject", supplierB.Id),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(supplierA.Id, result.SupplierId);
        Assert.Equal("A001", result.SupplierCode);
        Assert.Equal("a.user", result.Username);
    }

    [Fact]
    public async Task ResolveAsync_DoesNotResolveUnknownSubject()
    {
        await using var db = CreateDbContext();
        var service = new SupplierPortalIdentityService(db);

        var result = await service.ResolveAsync(CreatePrincipal("unmapped-subject"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_DoesNotResolvePrincipalWithoutSubject()
    {
        await using var db = CreateDbContext();
        var service = new SupplierPortalIdentityService(db);
        var principal = new ClaimsPrincipal(new ClaimsIdentity("test"));

        var result = await service.ResolveAsync(principal, CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(false, "APPROVED", "ACTIVE")]
    [InlineData(true, "PENDING", "ACTIVE")]
    [InlineData(true, "APPROVED", "INACTIVE")]
    public async Task ResolveAsync_RejectsInactiveOrUnapprovedAccounts(
        bool userActive,
        string approvalStatus,
        string supplierStatus)
    {
        await using var db = CreateDbContext();
        var supplier = CreateSupplier("A001", "Supplier A");
        supplier.ApprovalStatus = approvalStatus;
        supplier.Status = supplierStatus;
        db.Suppliers.Add(supplier);
        db.SupplierUsers.Add(CreateSupplierUser(supplier.Id, "supplier-a-subject", "a.user", userActive));
        await db.SaveChangesAsync();

        var service = new SupplierPortalIdentityService(db);
        var result = await service.ResolveAsync(CreatePrincipal("supplier-a-subject"), CancellationToken.None);

        Assert.Null(result);
    }

    private static ErpGeneralDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static Supplier CreateSupplier(string code, string name) =>
        new()
        {
            SupplierCode = code,
            SupplierName = name,
            ApprovalStatus = "APPROVED",
            Status = "ACTIVE"
        };

    private static SupplierUser CreateSupplierUser(
        Guid supplierId,
        string subject,
        string username,
        bool isActive = true) =>
        new()
        {
            SupplierId = supplierId,
            IdentityProviderSubject = subject,
            Username = username,
            PasswordHash = "not-used-by-keycloak",
            IsActive = isActive,
            PortalInvitationStatus = "Active"
        };

    private static ClaimsPrincipal CreatePrincipal(string subject, Guid? supplierId = null)
    {
        var claims = new List<Claim> { new("sub", subject) };
        if (supplierId is Guid requestedSupplierId)
            claims.Add(new Claim("supplier_id", requestedSupplierId.ToString()));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
