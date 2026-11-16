using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface ISupplierApSubmissionAttachmentService
{
    Task<IReadOnlyList<SupplierApSubmissionAttachmentDto>> GetSupplierAttachmentsAsync(
        Guid submissionId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SupplierApSubmissionAttachmentDto>> GetEmployeeAttachmentsAsync(
        Guid submissionId,
        CancellationToken cancellationToken);

    Task<SupplierApSubmissionAttachmentDto> UploadSupplierAttachmentAsync(
        Guid submissionId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        string originalFileName,
        long contentLength,
        Stream content,
        CancellationToken cancellationToken);

    Task RevokeSupplierAttachmentAsync(
        Guid submissionId,
        Guid attachmentId,
        Guid supplierId,
        string supplierRole,
        CancellationToken cancellationToken);

    Task<SupplierApSubmissionAttachmentDownloadDto?> GetSupplierDownloadAsync(
        Guid submissionId,
        Guid attachmentId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<SupplierApSubmissionAttachmentDownloadDto?> GetEmployeeDownloadAsync(
        Guid submissionId,
        Guid attachmentId,
        CancellationToken cancellationToken);
}
