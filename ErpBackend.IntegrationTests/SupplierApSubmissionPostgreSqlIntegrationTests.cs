using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Portal;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.System;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.IntegrationTests;

public sealed class SupplierApSubmissionPostgreSqlIntegrationTests
{
    [PostgreSqlFact]
    public async Task SubmitAndApprove_CreatesOneApAndReviewSnapshotInPostgreSql()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ERP_TEST_POSTGRES_CONNECTION_STRING")!;
        await using var database = await PostgreSqlTestDatabase.CreateAsync(connectionString);
        Guid supplierId;
        Guid supplierUserId;
        Guid reviewerId;
        Guid purchaseOrderId;
        Guid goodsReceiptId;
        Guid receiptItemId;

        await using (var setupDb = new ErpGeneralDbContext(database.Options))
        {
            var supplier = new Supplier
            {
                SupplierCode = $"SUP-{Guid.NewGuid():N}"[..20],
                SupplierName = "AP Submission PostgreSQL Integration Supplier",
                Currency = "TWD",
                ApprovalStatus = "APPROVED",
                Status = "ACTIVE"
            };
            var supplierUser = new SupplierUser
            {
                SupplierId = supplier.Id,
                Username = $"supplier-{Guid.NewGuid():N}"[..40],
                PasswordHash = "integration-test-hash",
                Role = "sales"
            };
            var reviewer = new User
            {
                Username = $"reviewer-{Guid.NewGuid():N}"[..40],
                PasswordHash = "integration-test-hash"
            };
            var sku = new Sku { SkuNumber = "AP-PG-SKU", SkuName = "AP PostgreSQL test item" };
            var warehouse = new Warehouse
            {
                WarehouseCode = $"AP-{Guid.NewGuid():N}"[..20],
                WarehouseName = "AP Submission PostgreSQL test warehouse"
            };
            var purchaseOrder = new PurchaseOrder
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
                PurchaseOrderId = purchaseOrder.Id,
                SupplierId = supplier.Id,
                GrDate = DateTime.UtcNow.Date,
                WarehouseId = warehouse.Id,
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

            setupDb.AddRange(supplier, supplierUser, reviewer, sku, warehouse, purchaseOrder, receipt);
            await setupDb.SaveChangesAsync();

            supplierId = supplier.Id;
            supplierUserId = supplierUser.Id;
            reviewerId = reviewer.Id;
            purchaseOrderId = purchaseOrder.Id;
            goodsReceiptId = receipt.Id;
            receiptItemId = receipt.Items.Single().Id;
        }

        SupplierApSubmissionResultDto approved;
        await using (var workflowDb = new ErpGeneralDbContext(database.Options))
        {
            var service = new SupplierApSubmissionService(workflowDb);
            var request = new SupplierApSubmissionCreateRequestDto(
                "INV-PG-AP-001",
                DateOnly.FromDateTime(DateTime.UtcNow.Date),
                "TWD",
                10m,
                0m,
                10m,
                purchaseOrderId,
                goodsReceiptId,
                [
                    new SupplierApSubmissionLineRequestDto(
                        receiptItemId,
                        1m,
                        10m,
                        10m,
                        0m,
                        10m)
                ]);
            var created = await service.CreateDraftAsync(
                supplierId,
                supplierUserId,
                "sales",
                "pg-create-ap-submission-001",
                request,
                CancellationToken.None);
            var revision = await workflowDb.SupplierApSubmissionRevisions
                .SingleAsync(item => item.SubmissionId == created.SubmissionId);
            workflowDb.SupplierApSubmissionAttachments.Add(new SupplierApSubmissionAttachment
            {
                SubmissionId = created.SubmissionId,
                RevisionId = revision.Id,
                UploadedBySupplierUserId = supplierUserId,
                StorageKey = "private/integration/invoice.pdf",
                OriginalFileName = "invoice.pdf",
                ContentType = "application/pdf",
                SizeBytes = 10,
                Sha256 = new string('A', 64),
                ScanStatus = "clean"
            });
            await workflowDb.SaveChangesAsync();

            var submitted = await service.SubmitAsync(
                created.SubmissionId,
                supplierId,
                supplierUserId,
                "sales",
                created.Version,
                CancellationToken.None);
            approved = await service.ReviewAsync(
                created.SubmissionId,
                reviewerId,
                "approve",
                submitted.Version,
                string.Empty,
                CancellationToken.None);
        }

        await using var verifyDb = new ErpGeneralDbContext(database.Options);
        var payable = await verifyDb.AccountsPayables
            .Include(item => item.Items)
            .SingleAsync(item => item.Id == approved.AccountsPayableId);
        var review = await verifyDb.SupplierApSubmissionReviews.SingleAsync();
        var submission = await verifyDb.SupplierApSubmissions.SingleAsync();

        Assert.Equal("approved", approved.Status);
        Assert.Equal("approved", submission.Status);
        Assert.Equal("unpaid", payable.PaymentStatus);
        Assert.Equal(10m, payable.TotalAmount);
        Assert.Equal(receiptItemId, (await verifyDb.SupplierApSubmissionRevisionLines
            .SingleAsync()).GoodsReceiptItemId);
        Assert.Single(payable.Items);
        Assert.Equal("matched", review.MatchResult);
        Assert.Contains("ruleVersion", review.MatchSnapshot);
        Assert.Empty(await verifyDb.PaymentRecords.ToListAsync());
        Assert.Empty(await verifyDb.AccountingVouchers.ToListAsync());
    }
}
