using System.Security.Claims;
using ErpBackend.Data;
using ErpBackend.Security.SupplierPortal;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public sealed class SupplierPortalIdentityService(ErpGeneralDbContext db) : ISupplierPortalIdentityService
{
    public Task<SupplierPortalUser?> ResolveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var subject = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
            return Task.FromResult<SupplierPortalUser?>(null);

        return db.SupplierUsers
            .AsNoTracking()
            .Where(user => user.IdentityProviderSubject == subject
                && user.IsActive
                && user.PortalInvitationStatus == "Active"
                && user.Supplier.ApprovalStatus.ToUpper() == "APPROVED"
                && user.Supplier.Status.ToUpper() == "ACTIVE")
            .Select(user => new SupplierPortalUser(
                user.Id,
                user.SupplierId,
                user.Username,
                user.ContactName,
                user.Role,
                user.Supplier.SupplierCode,
                user.Supplier.SupplierName))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
