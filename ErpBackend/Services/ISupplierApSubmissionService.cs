using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface ISupplierApSubmissionService
{
    Task<SupplierApSubmissionPageDto> GetSupplierSubmissionsAsync(
        Guid supplierId,
        string? status,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? submissionNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SupplierApEligibleSourceDto>> GetEligibleSourcesAsync(
        Guid supplierId,
        Guid? purchaseOrderId,
        CancellationToken cancellationToken);

    Task<SupplierApSubmissionDetailDto?> GetSupplierSubmissionAsync(
        Guid submissionId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierApSubmissionResultDto> CreateDraftAsync(
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        string idempotencyKey,
        SupplierApSubmissionCreateRequestDto request,
        CancellationToken cancellationToken);

    Task<SupplierApSubmissionResultDto> UpdateDraftAsync(
        Guid submissionId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        int expectedVersion,
        SupplierApSubmissionDraftRequestDto request,
        CancellationToken cancellationToken);

    Task<SupplierApSubmissionResultDto> SubmitAsync(
        Guid submissionId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        int expectedVersion,
        CancellationToken cancellationToken);

    Task<SupplierApSubmissionResultDto> CancelAsync(
        Guid submissionId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        int expectedVersion,
        string? reason,
        CancellationToken cancellationToken);

    Task<SupplierApSubmissionPageDto> GetEmployeeSubmissionsAsync(
        string? status,
        Guid? supplierId,
        DateOnly? invoiceDateFrom,
        DateOnly? invoiceDateTo,
        string? submissionNumber,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<SupplierApSubmissionDetailDto?> GetEmployeeSubmissionAsync(
        Guid submissionId,
        CancellationToken cancellationToken);

    Task<SupplierApSubmissionResultDto> ReviewAsync(
        Guid submissionId,
        Guid reviewerUserId,
        string action,
        int expectedVersion,
        string reason,
        CancellationToken cancellationToken);
}
