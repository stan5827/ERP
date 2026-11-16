namespace ErpBackend.Security.SupplierPortal;

public sealed class SupplierPortalIdentityContext
{
    public SupplierPortalUser? Current { get; private set; }

    internal void SetCurrent(SupplierPortalUser supplierPortalUser)
    {
        Current = supplierPortalUser;
    }
}
