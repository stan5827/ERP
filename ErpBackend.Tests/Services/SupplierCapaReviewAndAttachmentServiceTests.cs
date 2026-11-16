using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Infrastructure.CapaAttachments;
using ErpBackend.Models.QA;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class SupplierCapaReviewAndAttachmentServiceTests
{
    [Fact]
    public async Task ReviewAsync_RequiresUnderReviewAndExactSubmittedRevision()
    {
        await using var db = CreateDbContext();
        var (complaint, capa, revision) = SeedSubmittedCapa(db);
        await db.SaveChangesAsync();
        var service = new SupplierCapaReviewService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewAsync(
            complaint.Id,
            Guid.NewGuid(),
            ReviewRequest(revision.RevisionNumber),
            CancellationToken.None));

        await service.StartReviewAsync(complaint.Id, Guid.NewGuid(), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewAsync(
            complaint.Id,
            Guid.NewGuid(),
            ReviewRequest(revision.RevisionNumber + 1),
            CancellationToken.None));

        Assert.Equal("under_review", (await db.SupplierCapas.SingleAsync()).Status);
        Assert.Single(await db.SupplierCapaReviews.ToListAsync());
    }

    [Fact]
    public async Task ReviewAsync_RecordsApprovalAndRequiresSeparateClose()
    {
        await using var db = CreateDbContext();
        var (complaint, capa, revision) = SeedSubmittedCapa(db);
        await db.SaveChangesAsync();
        var service = new SupplierCapaReviewService(db);
        await service.StartReviewAsync(complaint.Id, Guid.NewGuid(), CancellationToken.None);

        var decision = await service.ReviewAsync(
            complaint.Id,
            Guid.NewGuid(),
            ReviewRequest(revision.RevisionNumber, "approve"),
            CancellationToken.None);
        Assert.Equal("approve", decision.Decision);
        Assert.Equal("approved", (await db.SupplierCapas.SingleAsync()).Status);
        Assert.Equal("open", (await db.SupplierComplaints.SingleAsync()).Status);

        var close = await service.CloseAsync(
            complaint.Id,
            Guid.NewGuid(),
            new SupplierCapaCloseRequestDto { Comments = "Effectiveness confirmed" },
            CancellationToken.None);
        Assert.Equal("closed", close.Decision);
        Assert.Equal("closed", capa.Status);
        Assert.Equal("closed", complaint.Status);
        Assert.Equal(3, (await db.SupplierCapaReviews.ToListAsync()).Count);
    }

    [Fact]
    public async Task ReviewAsync_RequiresReturnReasonAndMovesCapaToRevisionRequested()
    {
        await using var db = CreateDbContext();
        var (complaint, capa, revision) = SeedSubmittedCapa(db);
        await db.SaveChangesAsync();
        var service = new SupplierCapaReviewService(db);
        await service.StartReviewAsync(complaint.Id, Guid.NewGuid(), CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ReviewAsync(
            complaint.Id,
            Guid.NewGuid(),
            ReviewRequest(revision.RevisionNumber, "revision_requested"),
            CancellationToken.None));
        var returned = await service.ReviewAsync(
            complaint.Id,
            Guid.NewGuid(),
            ReviewRequest(revision.RevisionNumber, "revision_requested", "Provide objective evidence"),
            CancellationToken.None);

        Assert.Equal("revision_requested", returned.Decision);
        Assert.Equal("revision_requested", capa.Status);
        Assert.Equal("Provide objective evidence", returned.Comments);
    }

    [Fact]
    public async Task UploadSupplierAttachmentAsync_ScansAndAllowsOnlyCleanDownloads()
    {
        await using var db = CreateDbContext();
        var (complaint, capa, supplierId, supplierUserId) = SeedEditableCapa(db);
        await db.SaveChangesAsync();
        var storage = new FakeCapaObjectStorage();
        var service = CreateAttachmentService(db, storage, new FakeCapaScanner(CapaMalwareScanResult.Clean));
        var bytes = PdfContent();

        var uploaded = await service.UploadSupplierAttachmentAsync(
            complaint.Id,
            supplierId,
            supplierUserId,
            "sales",
            "evidence.pdf",
            bytes.Length,
            new MemoryStream(bytes),
            CancellationToken.None);

        Assert.Equal("clean", uploaded.ScanStatus);
        Assert.Single(storage.Objects);
        var download = await service.GetSupplierDownloadAsync(
            complaint.Id,
            uploaded.AttachmentId,
            supplierId,
            CancellationToken.None);
        Assert.NotNull(download);
        Assert.Equal(bytes, await ReadAllAsync(download.Content));
        await download.Content.DisposeAsync();
        Assert.Equal(capa.Id, (await db.SupplierCapaAttachments.SingleAsync()).SupplierCapaId);
    }

    [Fact]
    public async Task UploadSupplierAttachmentAsync_RejectsInfectedContentAndDeletesStoredObject()
    {
        await using var db = CreateDbContext();
        var (complaint, _, supplierId, supplierUserId) = SeedEditableCapa(db);
        await db.SaveChangesAsync();
        var storage = new FakeCapaObjectStorage();
        var service = CreateAttachmentService(db, storage, new FakeCapaScanner(CapaMalwareScanResult.Infected));
        var bytes = PdfContent();

        var uploaded = await service.UploadSupplierAttachmentAsync(
            complaint.Id,
            supplierId,
            supplierUserId,
            "sales",
            "infected.pdf",
            bytes.Length,
            new MemoryStream(bytes),
            CancellationToken.None);

        Assert.Equal("rejected", uploaded.ScanStatus);
        Assert.Empty(storage.Objects);
        Assert.Null(await service.GetSupplierDownloadAsync(
            complaint.Id,
            uploaded.AttachmentId,
            supplierId,
            CancellationToken.None));
    }

    [Fact]
    public async Task UploadSupplierAttachmentAsync_FailsClosedWhenScannerIsUnavailable()
    {
        await using var db = CreateDbContext();
        var (complaint, _, supplierId, supplierUserId) = SeedEditableCapa(db);
        await db.SaveChangesAsync();
        var storage = new FakeCapaObjectStorage();
        var scanner = new FakeCapaScanner(new CapaAttachmentScannerUnavailableException("scanner unavailable"));
        var service = CreateAttachmentService(db, storage, scanner);
        var bytes = PdfContent();

        await Assert.ThrowsAsync<CapaAttachmentScannerUnavailableException>(() =>
            service.UploadSupplierAttachmentAsync(
                complaint.Id,
                supplierId,
                supplierUserId,
                "sales",
                "evidence.pdf",
                bytes.Length,
                new MemoryStream(bytes),
                CancellationToken.None));

        var attachment = await db.SupplierCapaAttachments.SingleAsync();
        Assert.Equal("scan_failed", attachment.ScanStatus);
        Assert.Null(await service.GetSupplierDownloadAsync(
            complaint.Id,
            attachment.Id,
            supplierId,
            CancellationToken.None));
    }

    [Fact]
    public async Task UploadSupplierAttachmentAsync_MarksAttachmentFailedWhenScannerIsMisconfigured()
    {
        await using var db = CreateDbContext();
        var (complaint, _, supplierId, supplierUserId) = SeedEditableCapa(db);
        await db.SaveChangesAsync();
        var storage = new FakeCapaObjectStorage();
        var scanner = new FakeCapaScanner(new CapaAttachmentConfigurationException("scanner misconfigured"));
        var service = CreateAttachmentService(db, storage, scanner);
        var bytes = PdfContent();

        await Assert.ThrowsAsync<CapaAttachmentConfigurationException>(() =>
            service.UploadSupplierAttachmentAsync(
                complaint.Id,
                supplierId,
                supplierUserId,
                "sales",
                "evidence.pdf",
                bytes.Length,
                new MemoryStream(bytes),
                CancellationToken.None));

        var attachment = await db.SupplierCapaAttachments.SingleAsync();
        Assert.Equal("scan_failed", attachment.ScanStatus);
        Assert.Equal("scanner_misconfigured", attachment.ScanResult);
        Assert.Single(storage.Objects);
    }

    [Fact]
    public async Task UploadSupplierAttachmentAsync_EnforcesSupplierScopeSignatureAndRole()
    {
        await using var db = CreateDbContext();
        var (complaint, _, supplierId, supplierUserId) = SeedEditableCapa(db);
        await db.SaveChangesAsync();
        var service = CreateAttachmentService(
            db,
            new FakeCapaObjectStorage(),
            new FakeCapaScanner(CapaMalwareScanResult.Clean));
        var bytes = PdfContent();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UploadSupplierAttachmentAsync(
            complaint.Id,
            Guid.NewGuid(),
            supplierUserId,
            "sales",
            "evidence.pdf",
            bytes.Length,
            new MemoryStream(bytes),
            CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadSupplierAttachmentAsync(
            complaint.Id,
            supplierId,
            supplierUserId,
            "sales",
            "evidence.png",
            bytes.Length,
            new MemoryStream(bytes),
            CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UploadSupplierAttachmentAsync(
            complaint.Id,
            supplierId,
            supplierUserId,
            "viewer",
            "evidence.pdf",
            bytes.Length,
            new MemoryStream(bytes),
            CancellationToken.None));
        Assert.Empty(await db.SupplierCapaAttachments.ToListAsync());
    }

    private static ErpGeneralDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (SupplierComplaint Complaint, SupplierCapa Capa, Guid SupplierId, Guid SupplierUserId)
        SeedEditableCapa(ErpGeneralDbContext db)
    {
        var supplierId = Guid.NewGuid();
        var supplierUserId = Guid.NewGuid();
        var complaint = new SupplierComplaint
        {
            ComplaintNumber = $"COMP-{Guid.NewGuid():N}",
            SupplierId = supplierId,
            Status = "open"
        };
        var capa = new SupplierCapa
        {
            CapaNumber = $"CAPA-{Guid.NewGuid():N}",
            ComplaintId = complaint.Id,
            Complaint = complaint,
            Status = "in_progress"
        };
        complaint.Capa = capa;
        db.SupplierComplaints.Add(complaint);
        db.SupplierCapas.Add(capa);
        return (complaint, capa, supplierId, supplierUserId);
    }

    private static (SupplierComplaint Complaint, SupplierCapa Capa, SupplierCapaResponseRevision Revision)
        SeedSubmittedCapa(ErpGeneralDbContext db)
    {
        var complaint = new SupplierComplaint
        {
            ComplaintNumber = $"COMP-{Guid.NewGuid():N}",
            SupplierId = Guid.NewGuid(),
            Status = "open"
        };
        var capa = new SupplierCapa
        {
            CapaNumber = $"CAPA-{Guid.NewGuid():N}",
            ComplaintId = complaint.Id,
            Complaint = complaint,
            Status = "submitted"
        };
        var revision = new SupplierCapaResponseRevision
        {
            SupplierCapaId = capa.Id,
            SupplierCapa = capa,
            RevisionNumber = 1,
            SupplierUserId = Guid.NewGuid(),
            Action = "submitted",
            D1_Team = "Team",
            D2_ProblemDescription = "Problem",
            D3_InterimContainment = "Containment",
            D4_RootCause = "Cause",
            D5_PermanentAction = "Action",
            D6_Verification = "Verification",
            D7_Prevention = "Prevention",
            D8_Closure = "Closure"
        };
        capa.ResponseRevisions.Add(revision);
        complaint.Capa = capa;
        db.SupplierComplaints.Add(complaint);
        db.SupplierCapas.Add(capa);
        db.SupplierCapaResponseRevisions.Add(revision);
        return (complaint, capa, revision);
    }

    private static SupplierCapaReviewRequestDto ReviewRequest(
        int revisionNumber,
        string decision = "approve",
        string? comments = null) =>
        new()
        {
            Decision = decision,
            RevisionNumber = revisionNumber,
            Comments = comments
        };

    private static ICapaAttachmentService CreateAttachmentService(
        ErpGeneralDbContext db,
        ICapaPrivateObjectStorage storage,
        ICapaMalwareScanner scanner) =>
        new CapaAttachmentService(
            db,
            storage,
            scanner,
            Options.Create(new CapaAttachmentOptions()),
            NullLogger<CapaAttachmentService>.Instance);

    private static byte[] PdfContent() =>
        "%PDF-1.7\nsafe test content"u8.ToArray();

    private static async Task<byte[]> ReadAllAsync(Stream stream)
    {
        await using var output = new MemoryStream();
        await stream.CopyToAsync(output);
        return output.ToArray();
    }

    private sealed class FakeCapaObjectStorage : ICapaPrivateObjectStorage
    {
        public Dictionary<string, byte[]> Objects { get; } = new(StringComparer.Ordinal);

        public async Task PutAsync(
            string storageKey,
            Stream content,
            long contentLength,
            string contentType,
            CancellationToken cancellationToken)
        {
            await using var output = new MemoryStream();
            await content.CopyToAsync(output, cancellationToken);
            Objects[storageKey] = output.ToArray();
        }

        public async Task CopyToAsync(
            string storageKey,
            Stream destination,
            CancellationToken cancellationToken) =>
            await destination.WriteAsync(Objects[storageKey], cancellationToken);

        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream(Objects[storageKey], writable: false));

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            Objects.Remove(storageKey);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCapaScanner : ICapaMalwareScanner
    {
        private readonly CapaMalwareScanResult? _result;
        private readonly Exception? _exception;

        public FakeCapaScanner(CapaMalwareScanResult result) => _result = result;

        public FakeCapaScanner(Exception exception) => _exception = exception;

        public Task<CapaMalwareScanResult> ScanAsync(
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken) =>
            _exception is not null
                ? Task.FromException<CapaMalwareScanResult>(_exception)
                : Task.FromResult(_result!.Value);
    }
}
