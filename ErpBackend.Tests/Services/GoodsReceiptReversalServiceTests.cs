using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class GoodsReceiptReversalServiceTests
{
    [Fact]
    public async Task ReverseAsync_AddsCompensatingTransactionAndPreservesOriginal()
    {
        await using var fixture = await CreateFixtureAsync();

        var result = await fixture.Service.ReverseAsync(
            fixture.Receipt.Id, "採購資料更正", fixture.ActorId, CancellationToken.None);

        var transactions = await fixture.Db.InventoryTransactions
            .OrderBy(item => item.CreatedAt)
            .ToListAsync();
        var reversal = Assert.Single(
            transactions, item => item.ReversalOfTransactionId == fixture.OriginalTransaction.Id);
        Assert.Equal(2, transactions.Count);
        Assert.Equal(100m, fixture.OriginalTransaction.QtyChange);
        Assert.Equal(0m, fixture.OriginalTransaction.QtyBefore);
        Assert.Equal(100m, fixture.OriginalTransaction.QtyAfter);
        Assert.Equal(-100m, reversal.QtyChange);
        Assert.Equal(100m, reversal.QtyBefore);
        Assert.Equal(0m, reversal.QtyAfter);
        Assert.Equal(fixture.ActorId, reversal.HandlerId);
        Assert.Contains("採購資料更正", reversal.Notes);
        Assert.Equal("reversed", fixture.Receipt.Status);
        Assert.Equal("cancelled", fixture.Receipt.IqcStatus);
        Assert.Equal("confirmed", fixture.PurchaseOrder.Status);
        Assert.Equal(0m, fixture.Sku.Qty);
        Assert.Equal("confirmed", result.PurchaseOrderStatus);
        Assert.Contains(reversal.TrxId, result.ReversalTransactionIds);

        var audit = await fixture.Db.AuditLogs.SingleAsync();
        Assert.Equal("reverse_goods_receipt", audit.Action);
        Assert.Equal(fixture.Receipt.Id, audit.EntityId);
        Assert.Equal(fixture.ActorId, audit.UserId);
        Assert.Equal("採購資料更正", audit.Details);
    }

    [Fact]
    public async Task ReverseAsync_DecrementsLotAndPurchaseOrderBalances()
    {
        await using var fixture = await CreateFixtureAsync();
        var purchaseOrderItem = new PurchaseOrderItem
        {
            PurchaseOrderId = fixture.PurchaseOrder.Id,
            SkuId = fixture.Sku.Id,
            Quantity = 100m,
            Unit = "KG",
            UnitPrice = 150m,
            Subtotal = 15000m,
            ReceivedQty = 100m,
            Status = "received"
        };
        fixture.PurchaseOrder.ReceivedAmount = 15000m;
        fixture.Receipt.Items.Single().LotNumber = "BATCH-001";
        fixture.OriginalTransaction.LotNumber = "BATCH-001";
        var inventoryLot = new InventoryLot
        {
            SkuId = fixture.Sku.Id,
            LotNumber = "BATCH-001",
            Qty = 100m,
            WarehouseId = fixture.Warehouse.Id,
            BinLocationId = fixture.BinLocation.Id
        };
        fixture.Db.PurchaseOrderItems.Add(purchaseOrderItem);
        fixture.Db.InventoryLots.Add(inventoryLot);
        await fixture.Db.SaveChangesAsync();

        await fixture.Service.ReverseAsync(
            fixture.Receipt.Id, "測試批號沖銷", fixture.ActorId, CancellationToken.None);

        Assert.Equal(0m, inventoryLot.Qty);
        Assert.Equal(0m, purchaseOrderItem.ReceivedQty);
        Assert.Equal("pending", purchaseOrderItem.Status);
        Assert.Equal(0m, fixture.PurchaseOrder.ReceivedAmount);
    }

    [Fact]
    public async Task ReverseAsync_RecalculatesLatestPurchaseCostFromLastValidReceipt()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.Sku.Qty = 150m;
        fixture.Sku.LatestPurchaseCost = 150m;
        fixture.OriginalTransaction.QtyBefore = 50m;
        fixture.OriginalTransaction.QtyAfter = 150m;
        fixture.OriginalTransaction.UnitCost = 150m;
        fixture.OriginalTransaction.TotalCost = 15000m;
        fixture.OriginalTransaction.CreatedAt = DateTime.UtcNow;
        fixture.Db.InventoryTransactions.Add(new InventoryTransaction
        {
            TrxId = "GR-PREVIOUS-001",
            SkuId = fixture.Sku.Id,
            Type = "purchase_receipt",
            QtyChange = 50m,
            QtyBefore = 0m,
            QtyAfter = 50m,
            WarehouseId = fixture.Warehouse.Id,
            BinLocationId = fixture.BinLocation.Id,
            UnitCost = 120m,
            TotalCost = 6000m,
            CreatedAt = fixture.OriginalTransaction.CreatedAt.AddHours(-1)
        });
        await fixture.Db.SaveChangesAsync();

        await fixture.Service.ReverseAsync(
            fixture.Receipt.Id, "沖銷最新收貨", fixture.ActorId, CancellationToken.None);

        Assert.Equal(120m, fixture.Sku.LatestPurchaseCost);
        Assert.Equal(0m, fixture.Sku.StandardCost);
    }

    [Fact]
    public async Task ReverseAsync_RejectsIncorrectOriginalQuantitySnapshot()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.OriginalTransaction.QtyAfter = 0m;
        await fixture.Db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.ReverseAsync(
                fixture.Receipt.Id, "快照錯誤不可沖銷", fixture.ActorId, CancellationToken.None));

        Assert.Contains("快照不一致", exception.Message);
        Assert.Equal("received", fixture.Receipt.Status);
        Assert.Equal(100m, fixture.Sku.Qty);
        Assert.Single(await fixture.Db.InventoryTransactions.ToListAsync());
        Assert.Empty(await fixture.Db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task ReverseAsync_RejectsSecondReversal()
    {
        await using var fixture = await CreateFixtureAsync();
        await fixture.Service.ReverseAsync(
            fixture.Receipt.Id, "更正", fixture.ActorId, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.ReverseAsync(
                fixture.Receipt.Id, "重複沖銷", fixture.ActorId, CancellationToken.None));

        Assert.Contains("received", exception.Message);
        Assert.Equal(2, await fixture.Db.InventoryTransactions.CountAsync());
        Assert.Single(await fixture.Db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task ReverseAsync_RejectsReceiptWhenIqCIsNotPending()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.Receipt.IqcStatus = "passed";
        await fixture.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.ReverseAsync(
                fixture.Receipt.Id, "不應允許", fixture.ActorId, CancellationToken.None));

        Assert.Single(await fixture.Db.InventoryTransactions.ToListAsync());
        Assert.Equal("received", fixture.Receipt.Status);
    }

    [Fact]
    public async Task ReverseAsync_RejectsWhenReceiptInventoryHasBeenConsumed()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.Db.InventoryTransactions.Add(new InventoryTransaction
        {
            TrxId = "ISSUE-001",
            SkuId = fixture.Sku.Id,
            Type = "material_issue",
            QtyChange = -60m,
            QtyBefore = 100m,
            QtyAfter = 40m,
            WarehouseId = fixture.Warehouse.Id,
            BinLocationId = fixture.BinLocation.Id,
            CreatedAt = fixture.OriginalTransaction.CreatedAt.AddMinutes(1)
        });
        await fixture.Db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.ReverseAsync(
                fixture.Receipt.Id, "不應沖銷已耗用庫存", fixture.ActorId, CancellationToken.None));

        Assert.Contains("已有其他庫存異動", exception.Message);
        Assert.Equal("received", fixture.Receipt.Status);
        Assert.Equal(100m, fixture.Sku.Qty);
        Assert.Equal(2, await fixture.Db.InventoryTransactions.CountAsync());
    }

    [Fact]
    public async Task ReverseAsync_RejectsReceiptWhenItemAndLedgerDoNotMatch()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.Receipt.Items.Single().InventoryQty = 90m;
        await fixture.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.ReverseAsync(
                fixture.Receipt.Id, "資料不一致", fixture.ActorId, CancellationToken.None));

        Assert.Equal("received", fixture.Receipt.Status);
        Assert.Single(await fixture.Db.InventoryTransactions.ToListAsync());
        Assert.Empty(await fixture.Db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task ReverseAsync_RejectsWhenAccountsPayableExists()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.Db.AccountsPayables.Add(new AccountsPayable
        {
            ApNumber = "AP-001",
            SupplierId = fixture.PurchaseOrder.SupplierId,
            PurchaseOrderId = fixture.PurchaseOrder.Id,
            GoodsReceiptId = fixture.Receipt.Id
        });
        await fixture.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.ReverseAsync(
                fixture.Receipt.Id, "已有應付帳款", fixture.ActorId, CancellationToken.None));

        Assert.Equal("received", fixture.Receipt.Status);
        Assert.Single(await fixture.Db.InventoryTransactions.ToListAsync());
        Assert.Empty(await fixture.Db.AuditLogs.ToListAsync());
    }

    private static async Task<Fixture> CreateFixtureAsync()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ErpGeneralDbContext(options);
        var sku = new Sku
        {
            SkuNumber = "FG-001-06",
            SkuName = "APC原料",
            Qty = 100m
        };
        var warehouse = new Warehouse
        {
            WarehouseCode = "FW02",
            WarehouseName = "待驗倉"
        };
        var binLocation = new BinLocation
        {
            WarehouseId = warehouse.Id,
            LocationCode = "QA-01"
        };
        var purchaseOrder = new PurchaseOrder
        {
            PoNumber = "PO-TEST-001",
            SupplierId = Guid.NewGuid(),
            Status = "received",
            TotalAmount = 15750m
        };
        var receipt = new GoodsReceipt
        {
            GrNumber = "GR-TEST-001",
            PurchaseOrderId = purchaseOrder.Id,
            SupplierId = purchaseOrder.SupplierId,
            WarehouseId = warehouse.Id,
            Status = "received",
            IqcStatus = "pending"
        };
        receipt.Items.Add(new GoodsReceiptItem
        {
            SkuId = sku.Id,
            PoQty = 100m,
            ReceivedQty = 100m,
            AcceptedQty = 100m,
            InventoryQty = 100m,
            BinLocationId = binLocation.Id
        });
        var originalTransaction = new InventoryTransaction
        {
            TrxId = "GR-ORIGINAL-001",
            SkuId = sku.Id,
            Type = "purchase_receipt",
            QtyChange = 100m,
            QtyBefore = 0m,
            QtyAfter = 100m,
            RelatedDocType = "GoodsReceipt",
            RelatedDocId = receipt.Id,
            WarehouseId = warehouse.Id,
            BinLocationId = binLocation.Id
        };
        db.AddRange(sku, warehouse, binLocation, purchaseOrder, receipt, originalTransaction);
        await db.SaveChangesAsync();

        return new Fixture(
            db,
            new GoodsReceiptReversalService(db),
            sku,
            warehouse,
            binLocation,
            purchaseOrder,
            receipt,
            originalTransaction,
            Guid.NewGuid());
    }

    private sealed record Fixture(
        ErpGeneralDbContext Db,
        GoodsReceiptReversalService Service,
        Sku Sku,
        Warehouse Warehouse,
        BinLocation BinLocation,
        PurchaseOrder PurchaseOrder,
        GoodsReceipt Receipt,
        InventoryTransaction OriginalTransaction,
        Guid ActorId) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }
}
