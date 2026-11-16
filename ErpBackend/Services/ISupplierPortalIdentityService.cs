using System.Security.Claims;
using ErpBackend.Security.SupplierPortal;

namespace ErpBackend.Services;

public interface ISupplierPortalIdentityService
{
    Task<SupplierPortalUser?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
