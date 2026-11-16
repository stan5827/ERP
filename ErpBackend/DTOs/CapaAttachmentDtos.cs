namespace ErpBackend.DTOs;

public sealed record CapaAttachmentDto(
    Guid AttachmentId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string ScanStatus,
    DateTimeOffset UploadedAt,
    DateTimeOffset? ScannedAt,
    int? RevisionNumber);

public sealed record CapaAttachmentUploadDto(
    Guid AttachmentId,
    string OriginalFileName,
    string ScanStatus,
    DateTimeOffset UploadedAt);

public sealed record CapaAttachmentDownloadDto(CapaAttachmentDto Metadata, Stream Content);
