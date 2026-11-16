using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface ISupplierPortalCollaborationService
{
    Task<IReadOnlyList<SupplierPortalRfqSummaryDto>> GetRfqsAsync(
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierPortalRfqDetailDto?> GetRfqAsync(
        Guid rfqId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierPortalQuoteSubmittedDto> SubmitQuoteAsync(
        Guid rfqId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        SubmitSupplierRfqQuoteRequestDto request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SupplierPortalPurchaseOrderSummaryDto>> GetPurchaseOrdersAsync(
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierPortalPurchaseOrderDetailDto?> GetPurchaseOrderAsync(
        Guid purchaseOrderId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierPortalPurchaseOrderResponseSubmittedDto> SubmitPurchaseOrderResponseAsync(
        Guid purchaseOrderId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        SubmitSupplierPurchaseOrderResponseRequestDto request,
        CancellationToken cancellationToken);
}
