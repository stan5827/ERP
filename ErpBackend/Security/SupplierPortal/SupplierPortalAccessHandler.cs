using Microsoft.AspNetCore.Authorization;
using ErpBackend.Services;

namespace ErpBackend.Security.SupplierPortal;

public sealed class SupplierPortalAccessHandler(
    ISupplierPortalIdentityService identityService,
    SupplierPortalIdentityContext identityContext)
    : AuthorizationHandler<SupplierPortalAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SupplierPortalAccessRequirement requirement)
    {
        var supplierUser = await identityService.ResolveAsync(
            context.User,
            context.Resource is HttpContext httpContext
                ? httpContext.RequestAborted
                : CancellationToken.None);

        if (supplierUser is null)
            return;

        identityContext.SetCurrent(supplierUser);
        context.Succeed(requirement);
    }
}
