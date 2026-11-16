using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface ICapaAttachmentService
{
    Task<CapaAttachmentUploadDto> UploadSupplierAttachmentAsync(
        Guid complaintId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        string originalFileName,
        long contentLength,
        Stream content,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CapaAttachmentDto>> GetSupplierAttachmentsAsync(
        Guid complaintId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CapaAttachmentDto>> GetEmployeeAttachmentsAsync(
        Guid complaintId,
        CancellationToken cancellationToken);

    Task<CapaAttachmentDownloadDto?> GetSupplierDownloadAsync(
        Guid complaintId,
        Guid attachmentId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<CapaAttachmentDownloadDto?> GetEmployeeDownloadAsync(
        Guid complaintId,
        Guid attachmentId,
        CancellationToken cancellationToken);

    Task RevokeSupplierAttachmentAsync(
        Guid complaintId,
        Guid attachmentId,
        Guid supplierId,
        string supplierRole,
        CancellationToken cancellationToken);

    Task<CapaAttachmentDto> RescanAsync(
        Guid complaintId,
        Guid attachmentId,
        Guid reviewerUserId,
        CancellationToken cancellationToken);
}
