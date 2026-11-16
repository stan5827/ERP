using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.QA;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ErpBackend.Tests.Services;

public class InventoryQuarantineAvailabilityTests
{
    [Fact]
    public async Task IqcReceiptAndQuarantinedLot_AreCountedOnceInAvailableBalance()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);

        var sku = new Sku { SkuNumber = "FG-002-06", SkuName = "APC-B原料", Qty = 1000m };
        var warehouse = new Warehouse { WarehouseCode = "RAW", WarehouseName = "原料倉" };
        var holdBin = new BinLocation { WarehouseId = warehouse.Id, LocationCode = "IQC-HOLD" };
        var receipt = new GoodsReceipt
        {
            GrNumber = "GR-IQC-TEST-001",
            SupplierId = Guid.NewGuid(),
            WarehouseId = warehouse.Id
        };
        var receiptItem = new GoodsReceiptItem
        {
            GoodsReceipt = receipt,
            SkuId = sku.Id,
            ReceivedQty = 1000m,
            InventoryQty = 1000m,
            LotNumber = "LOT-IQC-TEST-001",
            BinLocationId = holdBin.Id
        };
        receipt.Items.Add(receiptItem);
        var iqcReport = new IqcReport
        {
            ReportNumber = "IQC-TEST-001",
            GoodsReceipt = receipt,
            GoodsReceiptItem = receiptItem,
            SupplierId = receipt.SupplierId,
            SkuId = sku.Id,
            ReceivedQty = 1000m,
            LotNumber = receiptItem.LotNumber,
            Result = "pending"
        };

        db.AddRange(sku, warehouse, holdBin, receipt, iqcReport);
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            TrxId = "GR-IQC-TEST-001",
            SkuId = sku.Id,
            Type = "purchase_receipt",
            QtyChange = 1000m,
            QtyBefore = 0m,
            QtyAfter = 1000m,
            LotNumber = receiptItem.LotNumber,
            WarehouseId = warehouse.Id,
            BinLocationId = holdBin.Id
        });
        db.InventoryLots.Add(new InventoryLot
        {
            SkuId = sku.Id,
            LotNumber = receiptItem.LotNumber!,
            Qty = 1000m,
            WarehouseId = warehouse.Id,
            BinLocationId = holdBin.Id,
            Status = "quarantine"
        });
        await db.SaveChangesAsync();

        var service = new InventoryService(db, Mock.Of<IAccountingService>());
        var atp = await service.CheckATPAsync(sku.SkuNumber, 0m);
        var balance = await service.GetSkuBalanceAsync(sku.SkuNumber);

        Assert.Equal(0m, ReadDecimal(atp, "available"));
        Assert.Equal(1000m, ReadDecimal(atp, "quarantined"));
        Assert.Equal(0m, ReadDecimal(balance, "available_on_hand"));
        Assert.Equal(1000m, ReadDecimal(balance, "quarantined_on_hand"));
    }

    private static decimal ReadDecimal(object value, string propertyName) =>
        (decimal)value.GetType().GetProperty(propertyName)!.GetValue(value)!;
}
