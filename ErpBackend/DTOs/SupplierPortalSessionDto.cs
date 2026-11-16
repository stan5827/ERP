namespace ErpBackend.DTOs;

/// <summary>Represents the authenticated supplier user's portal session.</summary>
public sealed record SupplierPortalSessionDto(
    Guid SupplierUserId,
    string Username,
    string? ContactName,
    string Role,
    string SupplierCode,
    string SupplierName);
