using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface ISupplierCapaReviewService
{
    Task<IReadOnlyList<SupplierPortalComplaintSummaryDto>> GetComplaintsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<SupplierPortalComplaintDetailDto?> GetComplaintAsync(
        Guid complaintId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SupplierCapaReviewDto>> GetReviewsAsync(
        Guid complaintId,
        CancellationToken cancellationToken);

    Task<SupplierCapaReviewDto> StartReviewAsync(
        Guid complaintId,
        Guid reviewerUserId,
        CancellationToken cancellationToken);

    Task<SupplierCapaReviewDto> ReviewAsync(
        Guid complaintId,
        Guid reviewerUserId,
        SupplierCapaReviewRequestDto request,
        CancellationToken cancellationToken);

    Task<SupplierCapaReviewDto> CloseAsync(
        Guid complaintId,
        Guid reviewerUserId,
        SupplierCapaCloseRequestDto request,
        CancellationToken cancellationToken);
}
