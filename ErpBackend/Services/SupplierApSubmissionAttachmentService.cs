using System.Security.Cryptography;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Infrastructure.CapaAttachments;
using ErpBackend.Models.Procurement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ErpBackend.Services;

public sealed class SupplierApSubmissionAttachmentService(
    ErpGeneralDbContext db,
    ICapaPrivateObjectStorage storage,
    ICapaMalwareScanner scanner,
    IOptions<CapaAttachmentOptions> options) : ISupplierApSubmissionAttachmentService
{
    private const int MaximumFileNameLength = 255;

    public Task<IReadOnlyList<SupplierApSubmissionAttachmentDto>> GetSupplierAttachmentsAsync(
        Guid submissionId,
        Guid supplierId,
        CancellationToken cancellationToken) =>
        ListAttachmentsAsync(submissionId, supplierId, cancellationToken);

    public Task<IReadOnlyList<SupplierApSubmissionAttachmentDto>> GetEmployeeAttachmentsAsync(
        Guid submissionId,
        CancellationToken cancellationToken) =>
        ListAttachmentsAsync(submissionId, null, cancellationToken);

    public async Task<SupplierApSubmissionAttachmentDto> UploadSupplierAttachmentAsync(
        Guid submissionId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        string originalFileName,
        long contentLength,
        Stream content,
        CancellationToken cancellationToken)
    {
        EnsureCanManage(supplierRole);
        ArgumentNullException.ThrowIfNull(content);
        var settings = options.Value;
        if (settings.MaximumFileBytes <= 0 || settings.MaximumAttachmentsPerCapa <= 0)
            throw new CapaAttachmentConfigurationException(
                "AP Submission 附件大小或數量限制設定無效。");
        if (contentLength is <= 0 || contentLength > settings.MaximumFileBytes)
            throw new ArgumentException("附件必須大於 0，且不可超過系統允許的 20 MB。");

        var safeFileName = NormalizeFileName(originalFileName);
        var bytes = await ReadBoundedAsync(
            content,
            contentLength,
            settings.MaximumFileBytes,
            cancellationToken);
        var contentType = DetectFileType(safeFileName, bytes);
        var submission = await db.SupplierApSubmissions
            .Include(item => item.Revisions)
            .SingleOrDefaultAsync(
                item => item.Id == submissionId && item.SupplierId == supplierId,
                cancellationToken)
            ?? throw new KeyNotFoundException("請款申請不存在。");
        if (submission.Status is not ("draft" or "returned"))
            throw new InvalidOperationException("只有草稿或退回補正期間可上傳附件。");

        var revision = submission.Revisions.Single(item =>
            item.RevisionNumber == submission.CurrentRevisionNumber);
        var activeCount = await db.SupplierApSubmissionAttachments.CountAsync(
            item => item.SubmissionId == submissionId
                && item.RevisionId == revision.Id
                && item.RevokedAt == null,
            cancellationToken);
        if (activeCount >= settings.MaximumAttachmentsPerCapa)
            throw new InvalidOperationException("有效請款附件已達系統上限。");

        var now = DateTime.UtcNow;
        var attachment = new SupplierApSubmissionAttachment
        {
            SubmissionId = submissionId,
            RevisionId = revision.Id,
            UploadedBySupplierUserId = supplierUserId,
            StorageKey = $"quarantine/ap-submissions/{submissionId:N}/{Guid.NewGuid():N}",
            OriginalFileName = safeFileName,
            ContentType = contentType,
            SizeBytes = bytes.LongLength,
            Sha256 = Convert.ToHexString(SHA256.HashData(bytes)),
            ScanStatus = "pending_scan",
            UploadedAt = now
        };
        db.SupplierApSubmissionAttachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await using var uploadStream = new MemoryStream(bytes, writable: false);
            await storage.PutAsync(
                attachment.StorageKey,
                uploadStream,
                bytes.LongLength,
                attachment.ContentType,
                cancellationToken);
        }
        catch
        {
            db.SupplierApSubmissionAttachments.Remove(attachment);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }

        try
        {
            var scanResult = await scanner.ScanAsync(bytes, cancellationToken);
            attachment.ScannedAt = DateTime.UtcNow;
            attachment.ScanStatus = scanResult == CapaMalwareScanResult.Clean ? "clean" : "rejected";
            attachment.ScanResult = scanResult == CapaMalwareScanResult.Clean
                ? "clean"
                : "malware_detected";
            await db.SaveChangesAsync(cancellationToken);
            if (scanResult == CapaMalwareScanResult.Infected)
                await storage.DeleteAsync(attachment.StorageKey, cancellationToken);
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

        return ToDto(attachment, revision.RevisionNumber);
    }

    public async Task RevokeSupplierAttachmentAsync(
        Guid submissionId,
        Guid attachmentId,
        Guid supplierId,
        string supplierRole,
        CancellationToken cancellationToken)
    {
        EnsureCanManage(supplierRole);
        var attachment = await db.SupplierApSubmissionAttachments
            .Include(item => item.Submission)
            .Include(item => item.Revision)
            .SingleOrDefaultAsync(
                item => item.Id == attachmentId
                    && item.SubmissionId == submissionId
                    && item.Submission.SupplierId == supplierId
                    && item.RevokedAt == null,
                cancellationToken)
            ?? throw new KeyNotFoundException("請款附件不存在。");
        if (attachment.Submission.Status is not ("draft" or "returned")
            || attachment.Revision.RevisionNumber != attachment.Submission.CurrentRevisionNumber)
            throw new InvalidOperationException("已提交版本或目前狀態不允許撤銷附件。");

        attachment.RevokedAt = DateTime.UtcNow;
        attachment.ScanStatus = "revoked";
        await db.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(attachment.StorageKey, cancellationToken);
    }

    public Task<SupplierApSubmissionAttachmentDownloadDto?> GetSupplierDownloadAsync(
        Guid submissionId,
        Guid attachmentId,
        Guid supplierId,
        CancellationToken cancellationToken) =>
        GetDownloadAsync(submissionId, attachmentId, supplierId, cancellationToken);

    public Task<SupplierApSubmissionAttachmentDownloadDto?> GetEmployeeDownloadAsync(
        Guid submissionId,
        Guid attachmentId,
        CancellationToken cancellationToken) =>
        GetDownloadAsync(submissionId, attachmentId, null, cancellationToken);

    private async Task<SupplierApSubmissionAttachmentDownloadDto?> GetDownloadAsync(
        Guid submissionId,
        Guid attachmentId,
        Guid? supplierId,
        CancellationToken cancellationToken)
    {
        var query = db.SupplierApSubmissionAttachments
            .AsNoTracking()
            .Include(item => item.Revision)
            .Where(item => item.Id == attachmentId
                && item.SubmissionId == submissionId
                && item.RevokedAt == null
                && item.ScanStatus == "clean"
                && item.Revision.RevisionNumber == item.Submission.CurrentRevisionNumber);
        if (supplierId.HasValue)
            query = query.Where(item => item.Submission.SupplierId == supplierId.Value);

        var attachment = await query.SingleOrDefaultAsync(cancellationToken);
        if (attachment is null)
            return null;
        var stream = await storage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        return new SupplierApSubmissionAttachmentDownloadDto(
            ToDto(attachment, attachment.Revision.RevisionNumber),
            stream);
    }

    private async Task<IReadOnlyList<SupplierApSubmissionAttachmentDto>> ListAttachmentsAsync(
        Guid submissionId,
        Guid? supplierId,
        CancellationToken cancellationToken)
    {
        var submissionExists = await db.SupplierApSubmissions.AnyAsync(
            item => item.Id == submissionId
                && (!supplierId.HasValue || item.SupplierId == supplierId.Value),
            cancellationToken);
        if (!submissionExists)
            throw new KeyNotFoundException("請款申請不存在。");
        return await db.SupplierApSubmissionAttachments
            .AsNoTracking()
            .Where(item => item.SubmissionId == submissionId
                && item.RevokedAt == null
                && (!supplierId.HasValue || item.Submission.SupplierId == supplierId.Value))
            .OrderBy(item => item.UploadedAt)
            .Select(item => new SupplierApSubmissionAttachmentDto(
                item.Id,
                item.OriginalFileName,
                item.ContentType,
                item.SizeBytes,
                item.ScanStatus,
                new DateTimeOffset(DateTime.SpecifyKind(item.UploadedAt, DateTimeKind.Utc)),
                item.ScannedAt.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(item.ScannedAt.Value, DateTimeKind.Utc))
                    : null,
                item.Revision.RevisionNumber))
            .ToListAsync(cancellationToken);
    }

    private static SupplierApSubmissionAttachmentDto ToDto(
        SupplierApSubmissionAttachment attachment,
        int revisionNumber) =>
        new(
            attachment.Id,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.ScanStatus,
            new DateTimeOffset(DateTime.SpecifyKind(attachment.UploadedAt, DateTimeKind.Utc)),
            attachment.ScannedAt.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(attachment.ScannedAt.Value, DateTimeKind.Utc))
                : null,
            revisionNumber);

    private static async Task<byte[]> ReadBoundedAsync(
        Stream input,
        long contentLength,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        if (contentLength > maximumBytes)
            throw new ArgumentException("附件超過系統允許大小。");
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
                throw new ArgumentException("附件超過系統允許大小。");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        if (total != contentLength)
            throw new ArgumentException("附件內容長度與上傳資訊不一致。");
        return output.ToArray();
    }

    private static string DetectFileType(
        string fileName,
        byte[] bytes)
    {
        if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            && bytes.AsSpan().StartsWith("%PDF-"u8))
            return "application/pdf";
        if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            && bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return "image/png";
        if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff)
                return "image/jpeg";
        }
        throw new ArgumentException("附件只允許內容簽章相符的 PDF、JPEG 或 PNG 檔案。");
    }

    private static string NormalizeFileName(string originalFileName)
    {
        var safe = Path.GetFileName(originalFileName.Replace('\\', '/')).Trim();
        safe = new string(safe.Where(character => !char.IsControl(character)).ToArray());
        if (string.IsNullOrWhiteSpace(safe) || safe.Length > MaximumFileNameLength)
            throw new ArgumentException("附件檔名不可為空或超過 255 個字元。");
        return safe;
    }

    private static void EnsureCanManage(string supplierRole)
    {
        if (!string.Equals(supplierRole, "sales", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("目前供應商入口角色沒有管理請款附件的權限。");
    }
}
