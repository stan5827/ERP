namespace ErpBackend.Security.SupplierPortal;

public sealed record SupplierPortalUser(
    Guid SupplierUserId,
    Guid SupplierId,
    string Username,
    string? ContactName,
    string Role,
    string SupplierCode,
    string SupplierName);
