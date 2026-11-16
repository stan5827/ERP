using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface ISupplierPortalCapaService
{
    Task<IReadOnlyList<SupplierPortalComplaintSummaryDto>> GetComplaintsAsync(
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierPortalComplaintDetailDto?> GetComplaintAsync(
        Guid complaintId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierPortalCapaResponseSavedDto> SaveDraftAsync(
        Guid complaintId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        SupplierPortalCapaResponseRequestDto request,
        CancellationToken cancellationToken);

    Task<SupplierPortalCapaResponseSavedDto> SubmitResponseAsync(
        Guid complaintId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        SupplierPortalCapaResponseRequestDto request,
        CancellationToken cancellationToken);
}
