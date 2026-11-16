using System.Security.Cryptography;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Infrastructure.CapaAttachments;
using ErpBackend.Models.QA;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ErpBackend.Services;

public sealed class CapaAttachmentService(
    ErpGeneralDbContext db,
    ICapaPrivateObjectStorage storage,
    ICapaMalwareScanner scanner,
    IOptions<CapaAttachmentOptions> options,
    ILogger<CapaAttachmentService> logger) : ICapaAttachmentService
{
    private const int MaximumFileNameLength = 255;
    private static readonly HashSet<string> EditableCapaStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "open", "in_progress", "revision_requested" };

    public async Task<CapaAttachmentUploadDto> UploadSupplierAttachmentAsync(
        Guid complaintId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        string originalFileName,
        long contentLength,
        Stream content,
        CancellationToken cancellationToken)
    {
        EnsureCanManageAttachments(supplierRole);
        ArgumentNullException.ThrowIfNull(content);
        var settings = options.Value;
        ValidateConfiguration(settings);
        if (contentLength is <= 0 || contentLength > settings.MaximumFileBytes)
            throw new ArgumentException("附件必須大於 0，且不可超過 20 MB。");

        var safeFileName = NormalizeFileName(originalFileName);
        var (mediaType, signature) = GetAllowedFileType(safeFileName);
        var bytes = await ReadBoundedAsync(content, contentLength, settings.MaximumFileBytes, cancellationToken);
        if (!signature(bytes))
            throw new ArgumentException("檔案內容與允許的 PDF、JPEG 或 PNG 格式不符。");

        var complaint = await db.SupplierComplaints
            .Include(item => item.Capa)
            .SingleOrDefaultAsync(
                item => item.Id == complaintId && item.SupplierId == supplierId,
                cancellationToken)
            ?? throw new KeyNotFoundException("客訴不存在。");
        var capa = complaint.Capa ?? throw new InvalidOperationException("請先儲存 CAPA 草稿再上傳附件。");
        if (!EditableCapaStatuses.Contains(capa.Status))
            throw new InvalidOperationException("只有 CAPA 草稿或退回補正期間可上傳附件。");

        var activeCount = await db.SupplierCapaAttachments.CountAsync(
            item => item.SupplierCapaId == capa.Id && item.RevokedAt == null,
            cancellationToken);
        if (activeCount >= settings.MaximumAttachmentsPerCapa)
            throw new InvalidOperationException("每個 CAPA 最多可保留 10 個有效附件。");

        var attachmentId = Guid.NewGuid();
        var uploadedAt = DateTime.UtcNow;
        var attachment = new SupplierCapaAttachment
        {
            Id = attachmentId,
            SupplierCapaId = capa.Id,
            UploadedBySupplierUserId = supplierUserId,
            StorageKey = $"quarantine/capa/{capa.Id:N}/{attachmentId:N}",
            OriginalFileName = safeFileName,
            ContentType = mediaType,
            SizeBytes = bytes.LongLength,
            Sha256 = Convert.ToHexString(SHA256.HashData(bytes)),
            ScanStatus = "pending_scan",
            UploadedAt = uploadedAt
        };
        db.SupplierCapaAttachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await using var uploadStream = new MemoryStream(bytes, writable: false);
            await storage.PutAsync(
                attachment.StorageKey,
                uploadStream,
                bytes.LongLength,
                mediaType,
                cancellationToken);
        }
        catch
        {
            db.SupplierCapaAttachments.Remove(attachment);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        return await ScanAndUpdateAsync(attachment, bytes, cancellationToken);
    }

    public Task<IReadOnlyList<CapaAttachmentDto>> GetSupplierAttachmentsAsync(
        Guid complaintId,
        Guid supplierId,
        CancellationToken cancellationToken) =>
        ListAttachmentsAsync(complaintId, supplierId, cancellationToken);

    public Task<IReadOnlyList<CapaAttachmentDto>> GetEmployeeAttachmentsAsync(
        Guid complaintId,
        CancellationToken cancellationToken) =>
        ListAttachmentsAsync(complaintId, null, cancellationToken);

    public async Task<CapaAttachmentDownloadDto?> GetSupplierDownloadAsync(
        Guid complaintId,
        Guid attachmentId,
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var attachment = await FindDownloadableAttachmentAsync(
            complaintId,
            attachmentId,
            supplierId,
            cancellationToken);
        return attachment is null
            ? null
            : new CapaAttachmentDownloadDto(ToDto(attachment), await storage.OpenReadAsync(
                attachment.StorageKey,
                cancellationToken));
    }

    public async Task<CapaAttachmentDownloadDto?> GetEmployeeDownloadAsync(
        Guid complaintId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var attachment = await FindDownloadableAttachmentAsync(
            complaintId,
            attachmentId,
            null,
            cancellationToken);
        return attachment is null
            ? null
            : new CapaAttachmentDownloadDto(ToDto(attachment), await storage.OpenReadAsync(
                attachment.StorageKey,
                cancellationToken));
    }

    public async Task RevokeSupplierAttachmentAsync(
        Guid complaintId,
        Guid attachmentId,
        Guid supplierId,
        string supplierRole,
        CancellationToken cancellationToken)
    {
        EnsureCanManageAttachments(supplierRole);
        var attachment = await db.SupplierCapaAttachments
            .Include(item => item.SupplierCapa)
                .ThenInclude(capa => capa.Complaint)
            .SingleOrDefaultAsync(
                item => item.Id == attachmentId
                    && item.SupplierCapa.ComplaintId == complaintId
                    && item.SupplierCapa.Complaint.SupplierId == supplierId
                    && item.RevokedAt == null,
                cancellationToken)
            ?? throw new KeyNotFoundException("附件不存在。");

        if (attachment.SupplierCapaResponseRevisionId.HasValue)
            throw new InvalidOperationException("已提交版本的附件不可撤銷。");
        if (!EditableCapaStatuses.Contains(attachment.SupplierCapa.Status))
            throw new InvalidOperationException("目前 CAPA 狀態不允許撤銷附件。");

        attachment.RevokedAt = DateTime.UtcNow;
        attachment.ScanStatus = "revoked";
        await db.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(attachment.StorageKey, cancellationToken);
    }

    public async Task<CapaAttachmentDto> RescanAsync(
        Guid complaintId,
        Guid attachmentId,
        Guid reviewerUserId,
        CancellationToken cancellationToken)
    {
        var attachment = await db.SupplierCapaAttachments
            .SingleOrDefaultAsync(
                item => item.Id == attachmentId
                    && item.SupplierCapa.ComplaintId == complaintId
                    && item.RevokedAt == null
                    && (item.ScanStatus == "pending_scan" || item.ScanStatus == "scan_failed"),
                cancellationToken)
            ?? throw new KeyNotFoundException("待掃描附件不存在。");
        await using var content = new MemoryStream();
        await storage.CopyToAsync(attachment.StorageKey, content, cancellationToken);
        if (content.Length > options.Value.MaximumFileBytes)
            throw new InvalidOperationException("儲存中的附件超過系統允許大小，已拒絕掃描。");

        attachment.ScanStatus = "pending_scan";
        await db.SaveChangesAsync(cancellationToken);
        db.Entry(attachment).Property<Guid?>("ModifiedBy").CurrentValue = reviewerUserId;
        await ScanAndUpdateAsync(attachment, content.ToArray(), cancellationToken);
        return ToDto(attachment);
    }

    private async Task<CapaAttachmentUploadDto> ScanAndUpdateAsync(
        SupplierCapaAttachment attachment,
        ReadOnlyMemory<byte> bytes,
        CancellationToken cancellationToken)
    {
        try
        {
            var scanResult = await scanner.ScanAsync(bytes, cancellationToken);
            attachment.ScannedAt = DateTime.UtcNow;
            if (scanResult == CapaMalwareScanResult.Infected)
            {
                attachment.ScanStatus = "rejected";
                attachment.ScanResult = "malware_detected";
                await db.SaveChangesAsync(cancellationToken);
                try
                {
                    await storage.DeleteAsync(attachment.StorageKey, cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception,
                        "Could not delete rejected CAPA attachment {AttachmentId}",
                        attachment.Id);
                    throw;
                }
            }
            else
            {
                attachment.ScanStatus = "clean";
                attachment.ScanResult = "clean";
                await db.SaveChangesAsync(cancellationToken);
            }

            return new CapaAttachmentUploadDto(
                attachment.Id,
                attachment.OriginalFileName,
                attachment.ScanStatus,
                ToUtcOffset(attachment.UploadedAt));
        }
        catch (CapaAttachmentScannerUnavailableException)
        {
            attachment.ScanStatus = "scan_failed";
            attachment.ScanResult = "scanner_unavailable";
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (CapaAttachmentConfigurationException)
        {
            attachment.ScanStatus = "scan_failed";
            attachment.ScanResult = "scanner_misconfigured";
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<IReadOnlyList<CapaAttachmentDto>> ListAttachmentsAsync(
        Guid complaintId,
        Guid? supplierId,
        CancellationToken cancellationToken)
    {
        var complaintExists = await db.SupplierComplaints.AnyAsync(
            complaint => complaint.Id == complaintId
                && (!supplierId.HasValue || complaint.SupplierId == supplierId.Value),
            cancellationToken);
        if (!complaintExists)
            throw new KeyNotFoundException("客訴不存在。");

        var query = db.SupplierCapaAttachments
            .AsNoTracking()
            .Where(item => item.SupplierCapa.ComplaintId == complaintId
                && item.RevokedAt == null);
        if (supplierId.HasValue)
            query = query.Where(item => item.SupplierCapa.Complaint.SupplierId == supplierId.Value);

        return await query
            .OrderBy(item => item.UploadedAt)
            .Select(item => new CapaAttachmentDto(
                item.Id,
                item.OriginalFileName,
                item.ContentType,
                item.SizeBytes,
                item.ScanStatus,
                ToUtcOffset(item.UploadedAt),
                item.ScannedAt.HasValue ? ToUtcOffset(item.ScannedAt.Value) : null,
                item.ResponseRevision == null ? null : item.ResponseRevision.RevisionNumber))
            .ToListAsync(cancellationToken);
    }

    private Task<SupplierCapaAttachment?> FindDownloadableAttachmentAsync(
        Guid complaintId,
        Guid attachmentId,
        Guid? supplierId,
        CancellationToken cancellationToken)
    {
        var query = db.SupplierCapaAttachments
            .AsNoTracking()
            .Where(item => item.Id == attachmentId
                && item.SupplierCapa.ComplaintId == complaintId
                && item.RevokedAt == null
                && item.ScanStatus == "clean");
        if (supplierId.HasValue)
            query = query.Where(item => item.SupplierCapa.Complaint.SupplierId == supplierId.Value);
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<CapaAttachmentUploadDto> ScanAndUpdateAsync(
        SupplierCapaAttachment attachment,
        byte[] bytes,
        CancellationToken cancellationToken) =>
        await ScanAndUpdateAsync(attachment, new ReadOnlyMemory<byte>(bytes), cancellationToken);

    private static CapaAttachmentDto ToDto(SupplierCapaAttachment attachment) =>
        new(
            attachment.Id,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.ScanStatus,
            ToUtcOffset(attachment.UploadedAt),
            attachment.ScannedAt.HasValue ? ToUtcOffset(attachment.ScannedAt.Value) : null,
            attachment.ResponseRevision?.RevisionNumber);

    private static async Task<byte[]> ReadBoundedAsync(
        Stream input,
        long contentLength,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        await using var output = new MemoryStream((int)contentLength);
        var buffer = new byte[64 * 1024];
        long total = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;
            total += read;
            if (total > maximumBytes)
                throw new ArgumentException("附件不可超過 20 MB。");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (total != contentLength)
            throw new ArgumentException("附件實際大小與上傳宣告不一致。");
        return output.ToArray();
    }

    private static (string ContentType, Func<byte[], bool> IsValidSignature) GetAllowedFileType(
        string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => ("application/pdf", bytes =>
                bytes.Length >= 5 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8)),
            ".jpg" or ".jpeg" => ("image/jpeg", bytes =>
                bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF),
            ".png" => ("image/png", bytes =>
                bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(
                    new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })),
            _ => throw new ArgumentException("僅允許 PDF、JPEG 或 PNG 附件。")
        };

    private static string NormalizeFileName(string originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("附件必須有檔名。");
        var fileName = Path.GetFileName(originalFileName.Trim());
        if (fileName.Length is 0 or > MaximumFileNameLength
            || fileName.Any(char.IsControl)
            || fileName is "." or "..")
            throw new ArgumentException("附件檔名不合法或超過 255 個字元。");
        _ = GetAllowedFileType(fileName);
        return fileName;
    }

    private static void ValidateConfiguration(CapaAttachmentOptions settings)
    {
        if (settings.MaximumFileBytes != 20 * 1024 * 1024
            || settings.MaximumAttachmentsPerCapa != 10)
            throw new CapaAttachmentConfigurationException(
                "CAPA 附件上限目前固定為每檔 20 MB、每個 CAPA 10 個。");
    }

    private static void EnsureCanManageAttachments(string supplierRole)
    {
        if (!string.Equals(supplierRole, "sales", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("目前供應商入口角色沒有管理 CAPA 附件的權限。");
    }

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
