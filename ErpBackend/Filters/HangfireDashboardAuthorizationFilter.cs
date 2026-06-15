using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace ErpBackend.Filters;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // In a real production scenario, we would check for admin role or a specific claim
        // For development/demo purposes, we'll allow all authenticated users or just return true
        // if we are in development mode.
        
        // return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("admin");
        
        return true; // For now, allow access for simplicity in development
    }
}
