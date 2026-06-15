namespace ErpBackend.Services;

public interface ITenantProvider
{
    Guid? GetTenantId();
}

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetTenantId()
    {
        var tenantHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (Guid.TryParse(tenantHeader, out var tenantId))
            return tenantId;
        
        return null; // For single-tenant or default
    }
}
