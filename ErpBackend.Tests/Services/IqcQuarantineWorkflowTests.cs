using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.QA;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class IqcQuarantineWorkflowTests
{
    [Fact]
    public async Task CompleteInspectionAsync_PassingLot_ReleasesStockToGoodBin()
    {
        await using var db = CreateDbContext();
        var fixture = await CreatePendingInspectionAsync(db);
        var service = new IqcReportService(db);

        var result = await service.CompleteInspectionAsync(
            fixture.ReportId,
            CreateDecision("pass", "pass"),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal("pass", result.Result);
        var receipt = await db.GoodsReceipts.Include(item => item.Items)
            .SingleAsync(item => item.Id == fixture.ReceiptId);
        var receiptItem = receipt.Items.Single();
        Assert.Equal("passed", receipt.IqcStatus);
        Assert.Equal("pass", receiptItem.IqcResult);
        Assert.Equal(50m, receiptItem.AcceptedQty);
        Assert.Equal(0m, receiptItem.RejectedQty);
        Assert.Equal(fixture.GoodBinId, receiptItem.BinLocationId);

        var transactions = await db.InventoryTransactions.ToListAsync();
        var balances = transactions
            .Where(item => item.BinLocationId.HasValue)
            .GroupBy(item => item.BinLocationId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.QtyChange));
        Assert.Equal(0m, balances[fixture.HoldBinId]);
        Assert.Equal(50m, balances[fixture.GoodBinId]);
        var lot = await db.InventoryLots.SingleAsync();
        Assert.Equal(50m, lot.Qty);
        Assert.Equal("active", lot.Status);
        Assert.Equal(fixture.GoodBinId, lot.BinLocationId);
        Assert.Equal(50m, await new InventoryReservationService(
            db, NullLogger<InventoryReservationService>.Instance).GetAvailableAsync(fixture.SkuNumber));
    }

    [Fact]
    public async Task CompleteInspectionAsync_FailingLot_RemainsQuarantinedAndUnavailable()
    {
        await using var db = CreateDbContext();
        var fixture = await CreatePendingInspectionAsync(db);
        var service = new IqcReportService(db);

        var result = await service.CompleteInspectionAsync(
            fixture.ReportId,
            CreateDecision("fail", "fail"),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal("fail", result.Result);
        var receipt = await db.GoodsReceipts.Include(item => item.Items)
            .SingleAsync(item => item.Id == fixture.ReceiptId);
        var receiptItem = receipt.Items.Single();
        Assert.Equal("failed", receipt.IqcStatus);
        Assert.Equal("fail", receiptItem.IqcResult);
        Assert.Equal(0m, receiptItem.AcceptedQty);
        Assert.Equal(50m, receiptItem.RejectedQty);
        Assert.Equal(fixture.HoldBinId, receiptItem.BinLocationId);
        Assert.Equal("rejected", (await db.InventoryLots.SingleAsync()).Status);
        Assert.Equal(0m, await new InventoryReservationService(
            db, NullLogger<InventoryReservationService>.Instance).GetAvailableAsync(fixture.SkuNumber));

        var transactions = await db.InventoryTransactions.CountAsync();
        Assert.Equal(1, transactions);
    }

    [Fact]
    public async Task CompleteInspectionAsync_RejectsPassWhenAnyInspectionItemFails()
    {
        await using var db = CreateDbContext();
        var fixture = await CreatePendingInspectionAsync(db);
        var service = new IqcReportService(db);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CompleteInspectionAsync(
            fixture.ReportId,
            CreateDecision("pass", "fail"),
            Guid.NewGuid(),
            CancellationToken.None));

        Assert.Equal("pending", (await db.IqcReports.SingleAsync()).Result);
        Assert.Equal(1, await db.InventoryTransactions.CountAsync());
    }

    private static ErpGeneralDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static CompleteIqcInspectionRequestDto CreateDecision(string overallResult, string itemResult) =>
        new(
            overallResult,
            5,
            0,
            overallResult == "fail" ? 1 : 0,
            [
                new IqcInspectionItemRequestDto(
                    "外觀",
                    "不得有裂痕",
                    null,
                    null,
                    "已檢查",
                    itemResult)
            ]);

    private static async Task<PendingInspectionFixture> CreatePendingInspectionAsync(ErpGeneralDbContext db)
    {
        var supplier = new Supplier { SupplierCode = "SUP-IQC", SupplierName = "IQC Supplier" };
        var sku = new Sku { SkuNumber = "SKU-IQC", SkuName = "IQC Material", Qty = 50m, IqcRequired = true };
        var warehouse = new Warehouse { WarehouseCode = "FW02", WarehouseName = "Raw material" };
        var holdBin = new BinLocation
        {
            WarehouseId = warehouse.Id,
            LocationCode = "IQC-HOLD",
            IsActive = true
        };
        var goodBin = new BinLocation
        {
            WarehouseId = warehouse.Id,
            LocationCode = "RAW-01",
            IsActive = true
        };
        var receipt = new GoodsReceipt
        {
            GrNumber = "GR-IQC-001",
            SupplierId = supplier.Id,
            Supplier = supplier,
            WarehouseId = warehouse.Id,
            Status = "received",
            IqcStatus = "pending"
        };
        var receiptItem = new GoodsReceiptItem
        {
            GoodsReceiptId = receipt.Id,
            SkuId = sku.Id,
            ReceivedQty = 50m,
            InventoryQty = 50m,
            AcceptedQty = 0m,
            LotNumber = "LOT-IQC-001",
            BinLocationId = holdBin.Id,
            IqcResult = "pending"
        };
        receipt.Items.Add(receiptItem);
        var report = new IqcReport
        {
            ReportNumber = "IQC-IQC-001",
            GoodsReceiptId = receipt.Id,
            GoodsReceipt = receipt,
            GoodsReceiptItemId = receiptItem.Id,
            GoodsReceiptItem = receiptItem,
            SupplierId = supplier.Id,
            Supplier = supplier,
            SkuId = sku.Id,
            Sku = sku,
            ReceivedQty = 50m,
            ReleaseBinLocationId = goodBin.Id,
            ReleaseBinLocation = goodBin,
            LotNumber = receiptItem.LotNumber,
            Result = "pending"
        };

        db.AddRange(supplier, sku, warehouse, holdBin, goodBin, receipt, report);
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            TrxId = "GR-IQC-001",
            SkuId = sku.Id,
            Type = "purchase_receipt",
            QtyChange = 50m,
            QtyBefore = 0m,
            QtyAfter = 50m,
            LotNumber = receiptItem.LotNumber,
            RelatedDocType = "GoodsReceipt",
            RelatedDocId = receipt.Id,
            WarehouseId = warehouse.Id,
            BinLocationId = holdBin.Id
        });
        db.InventoryLots.Add(new InventoryLot
        {
            SkuId = sku.Id,
            LotNumber = receiptItem.LotNumber!,
            Qty = 50m,
            WarehouseId = warehouse.Id,
            BinLocationId = holdBin.Id,
            Status = "quarantine"
        });
        await db.SaveChangesAsync();
        var fixture = new PendingInspectionFixture(
            sku.SkuNumber, receipt.Id, report.Id, holdBin.Id, goodBin.Id);
        db.ChangeTracker.Clear();
        return fixture;
    }

    private sealed record PendingInspectionFixture(
        string SkuNumber,
        Guid ReceiptId,
        Guid ReportId,
        Guid HoldBinId,
        Guid GoodBinId);
}
