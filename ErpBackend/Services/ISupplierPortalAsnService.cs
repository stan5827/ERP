using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface ISupplierPortalAsnService
{
    Task<IReadOnlyList<SupplierPortalAsnSummaryDto>> GetAsnsAsync(
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierPortalAsnDetailDto?> GetAsnAsync(
        Guid asnId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierPortalAsnOrderOptionsDto?> GetPurchaseOrderOptionsAsync(
        Guid purchaseOrderId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierPortalAsnSubmittedDto> CreateAsnAsync(
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        CreateSupplierPortalAsnRequestDto request,
        CancellationToken cancellationToken);
}
