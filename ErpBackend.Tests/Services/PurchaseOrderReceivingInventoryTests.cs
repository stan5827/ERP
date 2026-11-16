using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class PurchaseOrderReceivingInventoryTests
{
    [Fact]
    public async Task PurchaseOrderQueries_ReturnAmountsAndItemsForListsAndDetails()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);

        var supplier = new Supplier
        {
            SupplierCode = "SUP-001",
            SupplierName = "Supplier"
        };
        var sku = new Sku
        {
            SkuNumber = "FG-001-06",
            SkuName = "APC material"
        };
        var purchaseOrder = new PurchaseOrder
        {
            PoNumber = "PO-TEST-003",
            Supplier = supplier,
            PoDate = DateTime.UtcNow,
            Status = "received",
            Currency = "TWD",
            Subtotal = 15000m,
            TaxAmount = 750m,
            TotalAmount = 15750m,
            ReceivedAmount = 15000m
        };
        purchaseOrder.Items.Add(new PurchaseOrderItem
        {
            Sku = sku,
            Quantity = 100m,
            Unit = "KG",
            UnitPrice = 150m,
            Subtotal = 15000m,
            ReceivedQty = 100m,
            Status = "received"
        });
        db.PurchaseOrders.Add(purchaseOrder);
        await db.SaveChangesAsync();

        var service = new ProcurementService(db);

        var (orders, totalCount) = await service.GetPurchaseOrdersAsync(
            null, purchaseOrder.PoNumber, 1, 10);
        var detail = await service.GetPurchaseOrderAsync(purchaseOrder.Id);

        Assert.Equal(1, totalCount);
        Assert.Collection(orders,
            order =>
            {
                Assert.Equal(15000m, order.Subtotal);
                Assert.Equal(750m, order.TaxAmount);
                Assert.Equal(15750m, order.TotalAmount);
                Assert.Equal(15000m, order.ReceivedAmount);
                Assert.Single(order.Items);
                Assert.Equal("FG-001-06", order.Items[0].SkuNumber);
                Assert.Equal(15000m, order.Items[0].Subtotal);
                Assert.Equal(100m, order.Items[0].ReceivedQty);
            });

        Assert.NotNull(detail);
        Assert.Equal(15000m, detail.Subtotal);
        Assert.Equal(750m, detail.TaxAmount);
        Assert.Equal(15750m, detail.TotalAmount);
        Assert.Single(detail.Items);
        Assert.Equal("FG-001-06", detail.Items[0].SkuNumber);
        Assert.Equal("received", detail.Items[0].Status);
    }

    [Fact]
    public async Task ReceivePurchaseOrderAsync_RecordsLedgerBasedBeforeAndAfterQuantities()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var sku = new Sku
        {
            SkuNumber = "FG-001-06",
            SkuName = "APC原料",
            Qty = 50m,
            LotControlled = true
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
            PoNumber = "PO-TEST-002",
            SupplierId = Guid.NewGuid(),
            Status = "confirmed"
        };
        purchaseOrder.Items.Add(new PurchaseOrderItem
        {
            SkuId = sku.Id,
            Quantity = 100m,
            UnitPrice = 157.5m,
            Subtotal = 15750m
        });
        db.AddRange(sku, warehouse, binLocation, purchaseOrder);
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            TrxId = "OPENING-001",
            SkuId = sku.Id,
            Type = "opening_balance",
            QtyChange = 50m,
            QtyBefore = 0m,
            QtyAfter = 50m,
            WarehouseId = warehouse.Id,
            BinLocationId = binLocation.Id
        });
        await db.SaveChangesAsync();

        var service = new ProcurementService(db);
        var purchaseOrderItem = purchaseOrder.Items.Single();
        await service.ReceivePurchaseOrderAsync(
            purchaseOrder.PoNumber,
            new ReceivePurchaseOrderRequest
            {
                WarehouseId = warehouse.Id,
                BinLocationId = binLocation.Id,
                Items =
                [
                    new ReceivePurchaseOrderItemRequestDto
                    {
                        PurchaseOrderItemId = purchaseOrderItem.Id,
                        Quantity = 100m,
                        LotNumber = "BATCH-TEST-001"
                    }
                ]
            });

        var receiptTransaction = await db.InventoryTransactions
            .SingleAsync(item => item.Type == "purchase_receipt");
        Assert.Equal(0m, receiptTransaction.QtyBefore);
        Assert.Equal(100m, receiptTransaction.QtyAfter);
        Assert.Equal(100m, receiptTransaction.QtyChange);
        Assert.Equal("BATCH-TEST-001", receiptTransaction.LotNumber);
        Assert.Equal("IQC-HOLD", (await db.BinLocations.SingleAsync(bin => bin.Id == receiptTransaction.BinLocationId)).LocationCode);
        Assert.Equal(100m, purchaseOrderItem.ReceivedQty);
        Assert.Equal(15750m, purchaseOrder.ReceivedAmount);
        Assert.Equal("received", purchaseOrder.Status);
        Assert.Equal(150m, sku.Qty);
        var lot = await db.InventoryLots.SingleAsync();
        Assert.Equal(100m, lot.Qty);
        Assert.Equal("quarantine", lot.Status);
        var receiptItem = await db.GoodsReceiptItems.SingleAsync();
        Assert.Equal("BATCH-TEST-001", receiptItem.LotNumber);
        Assert.Equal(0m, receiptItem.AcceptedQty);
        Assert.Equal("pending", receiptItem.IqcResult);
        var iqcReport = await db.IqcReports.SingleAsync();
        Assert.Equal("pending", iqcReport.Result);
        Assert.Equal(100m, iqcReport.ReceivedQty);
        Assert.Equal(binLocation.Id, iqcReport.ReleaseBinLocationId);
    }

    [Fact]
    public async Task ReceivePurchaseOrderAsync_RejectsLotControlledSkuWithoutLot()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var sku = new Sku
        {
            SkuNumber = "FG-LOT-001",
            SkuName = "Lot controlled item",
            LotControlled = true
        };
        var warehouse = new Warehouse
        {
            WarehouseCode = "FW02",
            WarehouseName = "Raw materials"
        };
        var binLocation = new BinLocation
        {
            WarehouseId = warehouse.Id,
            LocationCode = "QA-01"
        };
        var purchaseOrder = new PurchaseOrder
        {
            PoNumber = "PO-LOT-001",
            SupplierId = Guid.NewGuid(),
            Status = "confirmed"
        };
        var purchaseOrderItem = new PurchaseOrderItem
        {
            SkuId = sku.Id,
            Quantity = 10m,
            Unit = "KG",
            UnitPrice = 5m
        };
        purchaseOrder.Items.Add(purchaseOrderItem);
        db.AddRange(sku, warehouse, binLocation, purchaseOrder);
        await db.SaveChangesAsync();

        var service = new ProcurementService(db);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReceivePurchaseOrderAsync(
                purchaseOrder.PoNumber,
                new ReceivePurchaseOrderRequest
                {
                    WarehouseId = warehouse.Id,
                    BinLocationId = binLocation.Id,
                    Items =
                    [
                        new ReceivePurchaseOrderItemRequestDto
                        {
                            PurchaseOrderItemId = purchaseOrderItem.Id,
                            Quantity = 10m
                        }
                    ]
                }));

        Assert.Contains("需批號", exception.Message);
        Assert.Empty(await db.GoodsReceipts.ToListAsync());
        Assert.Empty(await db.InventoryTransactions.ToListAsync());
        Assert.Empty(await db.InventoryLots.ToListAsync());
        Assert.Equal(0m, sku.Qty);
        Assert.Equal(0m, purchaseOrderItem.ReceivedQty);
        Assert.Equal("confirmed", purchaseOrder.Status);
    }

    [Fact]
    public async Task ReceivePurchaseOrderAsync_LinksReceiptToAsnAndUpdatesAsnProgress()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var sku = new Sku
        {
            SkuNumber = "FG-ASN-001",
            SkuName = "ASN material",
            LotControlled = true
        };
        var supplier = new Supplier
        {
            SupplierCode = "SUP-ASN-001",
            SupplierName = "ASN Supplier"
        };
        var warehouse = new Warehouse
        {
            WarehouseCode = "FW-ASN",
            WarehouseName = "Raw materials"
        };
        var binLocation = new BinLocation
        {
            WarehouseId = warehouse.Id,
            LocationCode = "ASN-01"
        };
        var purchaseOrder = new PurchaseOrder
        {
            PoNumber = "PO-ASN-001",
            Supplier = supplier,
            Status = "confirmed"
        };
        var purchaseOrderItem = new PurchaseOrderItem
        {
            Sku = sku,
            Quantity = 10m,
            UnitPrice = 5m
        };
        purchaseOrder.Items.Add(purchaseOrderItem);
        var asn = new Asn
        {
            AsnNumber = "ASN-001",
            Supplier = supplier,
            PurchaseOrder = purchaseOrder,
            ShipDate = DateTime.UtcNow.Date,
            ExpectedArrival = DateTime.UtcNow.Date.AddDays(1),
            Status = "submitted"
        };
        var manufactureDate = DateTime.UtcNow.Date.AddDays(-10);
        var expiryDate = DateTime.UtcNow.Date.AddDays(90);
        asn.Items.Add(new AsnItem
        {
            Asn = asn,
            Sku = sku,
            ShippedQty = 8m,
            LotNumber = "LOT-ASN-1",
            ManufactureDate = manufactureDate,
            ExpiryDate = expiryDate
        });
        db.AddRange(sku, supplier, warehouse, binLocation, purchaseOrder, asn);
        await db.SaveChangesAsync();

        var service = new ProcurementService(db);
        await service.ReceivePurchaseOrderAsync(
            purchaseOrder.PoNumber,
            new ReceivePurchaseOrderRequest
            {
                AsnId = asn.Id,
                WarehouseId = warehouse.Id,
                BinLocationId = binLocation.Id,
                Items =
                [
                    new ReceivePurchaseOrderItemRequestDto
                    {
                        PurchaseOrderItemId = purchaseOrderItem.Id,
                        Quantity = 3m,
                        LotNumber = "LOT-ASN-1"
                    }
                ]
            });

        var receipt = await db.GoodsReceipts.SingleAsync();
        Assert.Equal(asn.Id, receipt.AsnId);
        Assert.Equal("partially_received", asn.Status);
        Assert.Equal(3m, purchaseOrderItem.ReceivedQty);
        var receiptItem = await db.GoodsReceiptItems.SingleAsync();
        Assert.Equal(manufactureDate, receiptItem.ManufactureDate);
        Assert.Equal(expiryDate, receiptItem.ExpiryDate);
    }

    [Fact]
    public async Task ReceivePurchaseOrderAsync_RejectsReceiptExceedingAsnShipmentQuantity()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var sku = new Sku
        {
            SkuNumber = "FG-ASN-002",
            SkuName = "ASN material"
        };
        var supplier = new Supplier
        {
            SupplierCode = "SUP-ASN-002",
            SupplierName = "ASN Supplier"
        };
        var purchaseOrder = new PurchaseOrder
        {
            PoNumber = "PO-ASN-002",
            Supplier = supplier,
            Status = "confirmed"
        };
        var purchaseOrderItem = new PurchaseOrderItem
        {
            Sku = sku,
            Quantity = 10m,
            UnitPrice = 5m
        };
        purchaseOrder.Items.Add(purchaseOrderItem);
        var asn = new Asn
        {
            AsnNumber = "ASN-002",
            Supplier = supplier,
            PurchaseOrder = purchaseOrder,
            ShipDate = DateTime.UtcNow.Date,
            ExpectedArrival = DateTime.UtcNow.Date.AddDays(1),
            Status = "submitted"
        };
        asn.Items.Add(new AsnItem { Asn = asn, Sku = sku, ShippedQty = 2m });
        db.AddRange(sku, supplier, purchaseOrder, asn);
        await db.SaveChangesAsync();

        var service = new ProcurementService(db);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReceivePurchaseOrderAsync(
                purchaseOrder.PoNumber,
                new ReceivePurchaseOrderRequest
                {
                    AsnId = asn.Id,
                    Items =
                    [
                        new ReceivePurchaseOrderItemRequestDto
                        {
                            PurchaseOrderItemId = purchaseOrderItem.Id,
                            Quantity = 3m
                        }
                    ]
                }));

        Assert.Contains("出貨通知", exception.Message);
        Assert.Empty(await db.GoodsReceipts.ToListAsync());
        Assert.Equal(0m, purchaseOrderItem.ReceivedQty);
    }
}
