using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Portal;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.System;
using ErpBackend.Services;
using ErpBackend.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class SupplierApSubmissionServiceTests
{
    [Fact]
    public async Task CreateDraftAsync_IsIdempotentForSameSupplierAndRequest()
    {
        await using var db = CreateDbContext();
        var source = await SeedEligibleSourceAsync(db);
        var service = new SupplierApSubmissionService(db);
        var request = CreateRequest(source);

        var first = await service.CreateDraftAsync(
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            "create-1",
            request,
            CancellationToken.None);
        var retry = await service.CreateDraftAsync(
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            "create-1",
            request,
            CancellationToken.None);

        Assert.Equal(first.SubmissionId, retry.SubmissionId);
        Assert.Single(await db.SupplierApSubmissions.ToListAsync());
        Assert.Equal("draft", retry.Status);
    }

    [Fact]
    public async Task CreateDraftAsync_RejectsInvoiceNumberAlreadyUsedBySameSupplier()
    {
        await using var db = CreateDbContext();
        var source = await SeedEligibleSourceAsync(db);
        var service = new SupplierApSubmissionService(db);
        var request = CreateRequest(source);
        await service.CreateDraftAsync(
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            "create-1",
            request,
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateDraftAsync(
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            "create-2",
            request,
            CancellationToken.None));
        Assert.Single(await db.SupplierApSubmissions.ToListAsync());
    }

    [Fact]
    public async Task CreateDraftAsync_RejectsInvoiceNumberAlreadyUsedByFormalAccountsPayable()
    {
        await using var db = CreateDbContext();
        var source = await SeedEligibleSourceAsync(db);
        db.AccountsPayables.Add(new AccountsPayable
        {
            ApNumber = "AP-EXISTING",
            SupplierId = source.Supplier.Id,
            SupplierInvoiceNumber = " inv-ap-001 "
        });
        await db.SaveChangesAsync();
        var service = new SupplierApSubmissionService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateDraftAsync(
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            "create-1",
            CreateRequest(source),
            CancellationToken.None));

        Assert.Empty(await db.SupplierApSubmissions.ToListAsync());
    }

    [Fact]
    public async Task CancelAsync_WritesSupplierAuditEvent()
    {
        await using var db = CreateDbContext();
        var source = await SeedEligibleSourceAsync(db);
        var service = new SupplierApSubmissionService(db);
        var created = await service.CreateDraftAsync(
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            "create-1",
            CreateRequest(source),
            CancellationToken.None);

        var cancelled = await service.CancelAsync(
            created.SubmissionId,
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            created.Version,
            "duplicate draft",
            CancellationToken.None);

        Assert.Equal("cancelled", cancelled.Status);
        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal("supplier_ap_submission.cancelled", audit.Action);
        Assert.Equal(created.SubmissionId, audit.EntityId);
        Assert.Contains(source.SupplierUser.Id.ToString(), audit.Details);
    }

    [Fact]
    public async Task GetSupplierSubmissionAsync_HidesAnotherSupplierSubmission()
    {
        await using var db = CreateDbContext();
        var source = await SeedEligibleSourceAsync(db);
        var service = new SupplierApSubmissionService(db);
        var created = await service.CreateDraftAsync(
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            "create-1",
            CreateRequest(source),
            CancellationToken.None);

        Assert.Null(await service.GetSupplierSubmissionAsync(
            created.SubmissionId,
            Guid.NewGuid(),
            CancellationToken.None));
        Assert.NotNull(await service.GetSupplierSubmissionAsync(
            created.SubmissionId,
            source.Supplier.Id,
            CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAndApprove_CreateOneMatchedAccountsPayableWithoutPostingVoucher()
    {
        await using var db = CreateDbContext();
        var source = await SeedEligibleSourceAsync(db);
        var reviewer = new User { Username = "ap-reviewer", PasswordHash = "test-hash" };
        db.Users.Add(reviewer);
        var service = new SupplierApSubmissionService(db);
        var created = await service.CreateDraftAsync(
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            "create-1",
            CreateRequest(source),
            CancellationToken.None);
        var revision = await db.SupplierApSubmissionRevisions.SingleAsync();
        db.SupplierApSubmissionAttachments.Add(new SupplierApSubmissionAttachment
        {
            SubmissionId = created.SubmissionId,
            RevisionId = revision.Id,
            UploadedBySupplierUserId = source.SupplierUser.Id,
            StorageKey = "private/test-object",
            OriginalFileName = "invoice.pdf",
            ContentType = "application/pdf",
            SizeBytes = 10,
            Sha256 = new string('A', 64),
            ScanStatus = "clean"
        });
        await db.SaveChangesAsync();

        var submitted = await service.SubmitAsync(
            created.SubmissionId,
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            created.Version,
            CancellationToken.None);
        var retriedSubmit = await service.SubmitAsync(
            created.SubmissionId,
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            created.Version,
            CancellationToken.None);
        var approved = await service.ReviewAsync(
            created.SubmissionId,
            reviewer.Id,
            "approve",
            submitted.Version,
            string.Empty,
            CancellationToken.None);
        var retriedApproval = await service.ReviewAsync(
            created.SubmissionId,
            reviewer.Id,
            "approve",
            submitted.Version,
            string.Empty,
            CancellationToken.None);

        var payable = await db.AccountsPayables.Include(item => item.Items).SingleAsync();
        Assert.Equal(submitted.SubmissionId, retriedSubmit.SubmissionId);
        Assert.Equal("submitted", retriedSubmit.Status);
        Assert.Equal("approved", approved.Status);
        Assert.Equal(approved.AccountsPayableId, retriedApproval.AccountsPayableId);
        Assert.Equal(10m, payable.TotalAmount);
        Assert.Equal(10m, Assert.Single(payable.Items).Amount);
        Assert.Equal("unpaid", payable.PaymentStatus);
        Assert.Empty(await db.AccountingVouchers.ToListAsync());
        Assert.Single(await db.SupplierApSubmissionReviews.ToListAsync());
    }

    [Fact]
    public async Task SubmitAsync_RequiresCleanAttachmentAndDoesNotAdvanceStateOnFailure()
    {
        await using var db = CreateDbContext();
        var source = await SeedEligibleSourceAsync(db);
        var service = new SupplierApSubmissionService(db);
        var created = await service.CreateDraftAsync(
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            "create-1",
            CreateRequest(source),
            CancellationToken.None);

        await Assert.ThrowsAsync<SupplierApSubmissionPolicyException>(() => service.SubmitAsync(
            created.SubmissionId,
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            created.Version,
            CancellationToken.None));
        var submission = await db.SupplierApSubmissions.SingleAsync();
        Assert.Equal("draft", submission.Status);
        Assert.Equal(created.Version, submission.Version);
    }

    [Fact]
    public async Task ReviewAsync_RejectsInvoiceDuplicateCreatedAfterSubmission()
    {
        await using var db = CreateDbContext();
        var source = await SeedEligibleSourceAsync(db);
        var reviewer = new User { Username = "ap-reviewer", PasswordHash = "test-hash" };
        db.Users.Add(reviewer);
        var service = new SupplierApSubmissionService(db);
        var created = await service.CreateDraftAsync(
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            "create-1",
            CreateRequest(source),
            CancellationToken.None);
        var revision = await db.SupplierApSubmissionRevisions.SingleAsync();
        db.SupplierApSubmissionAttachments.Add(new SupplierApSubmissionAttachment
        {
            SubmissionId = created.SubmissionId,
            RevisionId = revision.Id,
            UploadedBySupplierUserId = source.SupplierUser.Id,
            StorageKey = "private/test-object",
            OriginalFileName = "invoice.pdf",
            ContentType = "application/pdf",
            SizeBytes = 10,
            Sha256 = new string('A', 64),
            ScanStatus = "clean"
        });
        await db.SaveChangesAsync();
        var submitted = await service.SubmitAsync(
            created.SubmissionId,
            source.Supplier.Id,
            source.SupplierUser.Id,
            "sales",
            created.Version,
            CancellationToken.None);
        db.AccountsPayables.Add(new AccountsPayable
        {
            ApNumber = "AP-MANUAL",
            SupplierId = source.Supplier.Id,
            SupplierInvoiceNumber = "INV-AP-001"
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewAsync(
            created.SubmissionId,
            reviewer.Id,
            "approve",
            submitted.Version,
            string.Empty,
            CancellationToken.None));

        var submission = await db.SupplierApSubmissions.SingleAsync();
        Assert.Equal("submitted", submission.Status);
        Assert.Single(await db.AccountsPayables.ToListAsync());
    }

    private static ErpGeneralDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static async Task<EligibleSource> SeedEligibleSourceAsync(ErpGeneralDbContext db)
    {
        var supplier = new Supplier
        {
            SupplierCode = $"SUP-{Guid.NewGuid():N}"[..20],
            SupplierName = "AP Submission Test Supplier",
            Currency = "TWD",
            ApprovalStatus = "APPROVED",
            Status = "ACTIVE"
        };
        var supplierUser = new SupplierUser
        {
            SupplierId = supplier.Id,
            Username = $"supplier-{Guid.NewGuid():N}"[..40],
            PasswordHash = "test-hash",
            Role = "sales"
        };
        var sku = new Sku { SkuNumber = "AP-SKU", SkuName = "AP test item" };
        var order = new PurchaseOrder
        {
            PoNumber = $"PO-{Guid.NewGuid():N}"[..20],
            SupplierId = supplier.Id,
            PoDate = DateTime.UtcNow.Date,
            Currency = "TWD",
            ExchangeRate = 1m,
            TaxType = "taxable",
            Status = "confirmed",
            Items =
            [
                new PurchaseOrderItem
                {
                    SkuId = sku.Id,
                    Quantity = 10m,
                    Unit = "EA",
                    UnitPrice = 10m
                }
            ]
        };
        var receipt = new GoodsReceipt
        {
            GrNumber = $"GR-{Guid.NewGuid():N}"[..20],
            PurchaseOrderId = order.Id,
            SupplierId = supplier.Id,
            GrDate = DateTime.UtcNow.Date,
            WarehouseId = Guid.NewGuid(),
            Status = "received",
            IqcStatus = "passed",
            Currency = "TWD",
            Items =
            [
                new GoodsReceiptItem
                {
                    SkuId = sku.Id,
                    AcceptedQty = 5m,
                    ReceivedQty = 5m,
                    PoQty = 5m,
                    PoUnit = "EA",
                    IqcResult = "pass",
                    ForeignCurrencyPrice = 10m
                }
            ]
        };
        db.AddRange(supplier, supplierUser, sku, order, receipt);
        await db.SaveChangesAsync();
        return new EligibleSource(
            supplier,
            supplierUser,
            order,
            receipt,
            order.Items.Single(),
            receipt.Items.Single());
    }

    private static SupplierApSubmissionCreateRequestDto CreateRequest(EligibleSource source) =>
        new(
            "INV-AP-001",
            DateOnly.FromDateTime(DateTime.UtcNow.Date),
            "TWD",
            10m,
            0m,
            10m,
            source.Order.Id,
            source.Receipt.Id,
            [
                new SupplierApSubmissionLineRequestDto(
                    source.ReceiptItem.Id,
                    1m,
                    10m,
                    10m,
                    0m,
                    10m)
            ]);

    private sealed record EligibleSource(
        Supplier Supplier,
        SupplierUser SupplierUser,
        PurchaseOrder Order,
        GoodsReceipt Receipt,
        PurchaseOrderItem OrderItem,
        GoodsReceiptItem ReceiptItem);
}
