using System.Security.Cryptography;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Infrastructure.CapaAttachments;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class SupplierApSubmissionAttachmentServiceTests
{
    [Fact]
    public async Task UploadSupplierAttachmentAsync_ScansAndReturnsMetadataWithoutStorageKey()
    {
        await using var db = CreateDbContext();
        var (submission, supplierUserId) = await SeedSubmissionAsync(db);
        var storage = new InMemoryPrivateStorage();
        var service = CreateService(db, storage, new CleanScanner());
        var bytes = "%PDF-1.4\n"u8.ToArray();

        var result = await service.UploadSupplierAttachmentAsync(
            submission.Id,
            submission.SupplierId,
            supplierUserId,
            "sales",
            @"..\invoice.pdf",
            bytes.Length,
            new MemoryStream(bytes),
            CancellationToken.None);

        Assert.Equal("clean", result.ScanStatus);
        Assert.Equal("invoice.pdf", result.OriginalFileName);
        Assert.Equal(1, result.RevisionNumber);
        Assert.Single(storage.Objects);
        var saved = await db.SupplierApSubmissionAttachments.SingleAsync();
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)), saved.Sha256);
        Assert.DoesNotContain("StorageKey", typeof(SupplierApSubmissionAttachmentDto).GetProperties()
            .Select(property => property.Name));
    }

    [Fact]
    public async Task UploadSupplierAttachmentAsync_RejectsWrongSignatureAndCrossSupplierAccess()
    {
        await using var db = CreateDbContext();
        var (submission, supplierUserId) = await SeedSubmissionAsync(db);
        var service = CreateService(db, new InMemoryPrivateStorage(), new CleanScanner());

        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadSupplierAttachmentAsync(
            submission.Id,
            submission.SupplierId,
            supplierUserId,
            "sales",
            "invoice.pdf",
            4,
            new MemoryStream("nope"u8.ToArray()),
            CancellationToken.None));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UploadSupplierAttachmentAsync(
            submission.Id,
            Guid.NewGuid(),
            supplierUserId,
            "sales",
            "invoice.pdf",
            9,
            new MemoryStream("%PDF-1.4\n"u8.ToArray()),
            CancellationToken.None));
        Assert.Empty(await db.SupplierApSubmissionAttachments.ToListAsync());
    }

    [Fact]
    public async Task UploadSupplierAttachmentAsync_RejectsInfectedFileAndDeletesPrivateObject()
    {
        await using var db = CreateDbContext();
        var (submission, supplierUserId) = await SeedSubmissionAsync(db);
        var storage = new InMemoryPrivateStorage();
        var service = CreateService(db, storage, new InfectedScanner());

        var result = await service.UploadSupplierAttachmentAsync(
            submission.Id,
            submission.SupplierId,
            supplierUserId,
            "sales",
            "invoice.pdf",
            9,
            new MemoryStream("%PDF-1.4\n"u8.ToArray()),
            CancellationToken.None);

        Assert.Equal("rejected", result.ScanStatus);
        Assert.Empty(storage.Objects);
    }

    private static SupplierApSubmissionAttachmentService CreateService(
        ErpGeneralDbContext db,
        ICapaPrivateObjectStorage storage,
        ICapaMalwareScanner scanner) =>
        new(
            db,
            storage,
            scanner,
            Options.Create(new CapaAttachmentOptions
            {
                MaximumFileBytes = 1024,
                MaximumAttachmentsPerCapa = 5
            }));

    private static ErpGeneralDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static async Task<(SupplierApSubmission Submission, Guid SupplierUserId)> SeedSubmissionAsync(
        ErpGeneralDbContext db)
    {
        var supplier = new Supplier
        {
            SupplierCode = $"SUP-{Guid.NewGuid():N}"[..20],
            SupplierName = "Attachment test supplier"
        };
        var supplierUser = new ErpBackend.Models.Portal.SupplierUser
        {
            SupplierId = supplier.Id,
            Username = $"supplier-{Guid.NewGuid():N}"[..40],
            PasswordHash = "test-hash",
            Role = "sales"
        };
        var submission = new SupplierApSubmission
        {
            SubmissionNumber = $"APS-{Guid.NewGuid():N}"[..30],
            SupplierId = supplier.Id,
            SubmittedBySupplierUserId = supplierUser.Id,
            IdempotencyKey = $"key-{Guid.NewGuid():N}",
            IdempotencyRequestHash = new string('A', 64),
            SupplierInvoiceNumberKey = "INV-1",
            Status = "draft",
            CurrentRevisionNumber = 1
        };
        var revision = new SupplierApSubmissionRevision
        {
            SubmissionId = submission.Id,
            RevisionNumber = 1,
            SupplierInvoiceNumber = "INV-1",
            SupplierInvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Currency = "TWD",
            PurchaseOrderId = Guid.NewGuid(),
            GoodsReceiptId = Guid.NewGuid(),
            SubmittedBySupplierUserId = supplierUser.Id
        };
        submission.Revisions.Add(revision);
        db.AddRange(supplier, supplierUser, submission);
        await db.SaveChangesAsync();
        return (submission, supplierUser.Id);
    }

    private sealed class CleanScanner : ICapaMalwareScanner
    {
        public Task<CapaMalwareScanResult> ScanAsync(
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken) =>
            Task.FromResult(CapaMalwareScanResult.Clean);
    }

    private sealed class InfectedScanner : ICapaMalwareScanner
    {
        public Task<CapaMalwareScanResult> ScanAsync(
            ReadOnlyMemory<byte> content,
            CancellationToken cancellationToken) =>
            Task.FromResult(CapaMalwareScanResult.Infected);
    }

    private sealed class InMemoryPrivateStorage : ICapaPrivateObjectStorage
    {
        public Dictionary<string, byte[]> Objects { get; } = [];

        public async Task PutAsync(
            string storageKey,
            Stream content,
            long contentLength,
            string contentType,
            CancellationToken cancellationToken)
        {
            await using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            Objects[storageKey] = buffer.ToArray();
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
}
