using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.Sales;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ErpBackend.Tests.Services;

public class FqcWorkflowServiceTests
{
    [Fact]
    public async Task CreateInspectionLotAsync_UsesFinalOperationReportedQuantityForFinishedLot()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db);
        var service = CreateService(db);
        var expiryDate = new DateTime(2028, 4, 30);

        var lot = await service.CreateInspectionLotAsync(new CreateFqcInspectionLotRequestDto
        {
            WorkOrderId = seeded.WorkOrderId,
            LotNumber = "FG-LOT-001",
            ExpiryDate = expiryDate,
            SampledQty = 10,
            DefectiveQty = 1,
            DefectCode = "VISUAL",
            InspectorName = "QA"
        });

        Assert.Equal(100m, lot.ProducedQty);
        Assert.Equal("Pending", lot.InspectionStatus);
        Assert.Equal(0m, lot.AvailableReceiptQty);
        Assert.Equal(expiryDate, lot.ExpiryDate);
        Assert.Equal(expiryDate, (await db.FqcInspectionLots.SingleAsync()).ExpiryDate);
    }

    [Fact]
    public async Task DecideInspectionLotAsync_CriticalDefectForcesFailureAndQuarantineBlocksReceipt()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db);
        var service = CreateService(db);
        var inspection = await CreateInspectionAsync(service, seeded.WorkOrderId, critical: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DecideInspectionLotAsync(
            inspection.Id,
            new DecideFqcInspectionLotRequestDto
            {
                InspectionStatus = "Passed",
                ApprovedQty = 99,
                WarehouseId = seeded.WarehouseId,
                DecisionBy = "QA lead"
            }));

        var failed = await service.DecideInspectionLotAsync(
            inspection.Id,
            new DecideFqcInspectionLotRequestDto
            {
                InspectionStatus = "Failed",
                ApprovedQty = 0,
                WarehouseId = seeded.WarehouseId,
                DecisionBy = "QA lead"
            });

        Assert.Equal("Failed", failed.InspectionStatus);
        Assert.Equal(100m, failed.QuarantinedQty);
        Assert.Equal(100m, (await db.Skus.SingleAsync()).Qty);
        Assert.Equal("FQC-HOLD", await db.BinLocations
            .Where(bin => bin.LocationCode == "FQC-HOLD")
            .Select(bin => bin.LocationCode)
            .SingleAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateProductionReceiptAsync(
            new CreateProductionReceiptRequestDto
            {
                FqcInspectionLotId = inspection.Id,
                Quantity = 1,
                WarehouseId = seeded.WarehouseId,
                BinLocationId = seeded.GoodBinId
            }));
    }

    [Fact]
    public async Task CreateProductionReceiptAsync_EnforcesFqcApprovalAndPostsInventoryAndVoucher()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeAccountingAccounts: true);
        await AddWorkOrderActualCostAsync(db, seeded.WorkOrderId);
        var service = CreateService(db);
        var expiryDate = new DateTime(2028, 4, 30);
        var inspection = await CreateInspectionAsync(service, seeded.WorkOrderId, expiryDate: expiryDate);
        await service.DecideInspectionLotAsync(
            inspection.Id,
            new DecideFqcInspectionLotRequestDto
            {
                InspectionStatus = "Passed",
                ApprovedQty = 98,
                WarehouseId = seeded.WarehouseId,
                DecisionBy = "QA lead"
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateProductionReceiptAsync(
            new CreateProductionReceiptRequestDto
            {
                FqcInspectionLotId = inspection.Id,
                Quantity = 99,
                WarehouseId = seeded.WarehouseId,
                BinLocationId = seeded.GoodBinId
            }));

        var firstReceipt = await service.CreateProductionReceiptAsync(new CreateProductionReceiptRequestDto
        {
            FqcInspectionLotId = inspection.Id,
            Quantity = 50,
            WarehouseId = seeded.WarehouseId,
            BinLocationId = seeded.GoodBinId
        });
        var secondReceipt = await service.CreateProductionReceiptAsync(new CreateProductionReceiptRequestDto
        {
            FqcInspectionLotId = inspection.Id,
            Quantity = 48,
            WarehouseId = seeded.WarehouseId,
            BinLocationId = seeded.GoodBinId
        });

        Assert.Equal(50m, firstReceipt.Quantity);
        Assert.Equal(48m, secondReceipt.Quantity);
        Assert.Equal(100m, (await db.Skus.SingleAsync()).Qty);
        Assert.Equal(98m, (await db.WorkOrders.SingleAsync()).ReceivedQty);
        Assert.Equal(expiryDate, (await db.InventoryLots.FirstAsync()).ExpiryDate);
        Assert.Equal(2, await db.AccountingVouchers.CountAsync(voucher => voucher.ReferenceType == "ProductionReceipt"));
        Assert.Equal(2, await db.InventoryTransactions.CountAsync(transaction => transaction.Type == "production_receipt"));
        Assert.Equal(2m, await db.InventoryTransactions
            .Where(transaction => transaction.Type == "fqc_quarantine_in")
            .SumAsync(transaction => transaction.QtyChange));
        Assert.All(await db.ProductionReceiptItems.ToListAsync(), item => Assert.Equal(7m, item.UnitCost));
        Assert.All(await db.InventoryTransactions
            .Where(transaction => transaction.Type == "production_receipt")
            .ToListAsync(), item => Assert.Equal(7m, item.UnitCost));
        Assert.Equal(686m, await db.ProductionReceiptItems.SumAsync(item => item.TotalCost));
    }

    [Fact]
    public async Task CreateProductionReceiptAsync_RejectsMissingActualCostInsteadOfUsingStandardCost()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeAccountingAccounts: true);
        var service = CreateService(db);
        var inspection = await CreateInspectionAsync(service, seeded.WorkOrderId);
        await service.DecideInspectionLotAsync(
            inspection.Id,
            new DecideFqcInspectionLotRequestDto
            {
                InspectionStatus = "Passed",
                ApprovedQty = 98,
                WarehouseId = seeded.WarehouseId,
                DecisionBy = "QA lead"
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateProductionReceiptAsync(
            new CreateProductionReceiptRequestDto
            {
                FqcInspectionLotId = inspection.Id,
                Quantity = 10,
                WarehouseId = seeded.WarehouseId,
                BinLocationId = seeded.GoodBinId
            }));

        Assert.Empty(await db.ProductionReceipts.ToListAsync());
        Assert.Empty(await db.InventoryTransactions
            .Where(transaction => transaction.Type == "production_receipt")
            .ToListAsync());
        Assert.Equal(0m, (await db.WorkOrders.SingleAsync()).ReceivedQty);
    }

    [Fact]
    public async Task CreateReworkWorkOrderAsync_CreatesTraceableOrderAndKeepsQuantityQuarantined()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db);
        var service = CreateService(db);
        var inspection = await CreateInspectionAsync(service, seeded.WorkOrderId);
        await service.DecideInspectionLotAsync(
            inspection.Id,
            new DecideFqcInspectionLotRequestDto
            {
                InspectionStatus = "Failed",
                ApprovedQty = 0,
                WarehouseId = seeded.WarehouseId,
                DecisionBy = "QA lead"
            });

        var disposition = await service.CreateReworkWorkOrderAsync(inspection.Id, "PMC");

        Assert.Equal("Rework", disposition.DispositionStatus);
        Assert.Equal("PMC", disposition.DispositionBy);
        Assert.NotNull(disposition.ReworkWorkOrderId);
        var reworkOrder = await db.WorkOrders
            .Include(order => order.Operations)
            .SingleAsync(order => order.Id == disposition.ReworkWorkOrderId);
        Assert.Equal("rework", reworkOrder.WoType);
        Assert.Equal(100m, reworkOrder.PlannedQty);
        Assert.Single(reworkOrder.Operations);
        var reworkMaterial = Assert.Single(reworkOrder.Materials);
        Assert.Equal(inspection.Id, reworkMaterial.FqcInspectionLotId);
        Assert.Equal(100m, reworkMaterial.RequiredQty);
        Assert.Equal(100m, (await db.FqcInspectionLots.SingleAsync()).QuarantinedQty);
        Assert.Equal(100m, (await db.Skus.SingleAsync()).Qty);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateReworkWorkOrderAsync(inspection.Id, "PMC"));
    }

    [Fact]
    public async Task CreateScrapNoteAsync_LeavesQuarantinedStockUntouchedUntilAccountingApproval()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db);
        var service = CreateService(db);
        var inspection = await CreateInspectionAsync(service, seeded.WorkOrderId);
        var workOrder = await db.WorkOrders.SingleAsync(order => order.Id == seeded.WorkOrderId);
        workOrder.ActualQty = 100;
        db.WorkOrderMaterialActuals.Add(new WorkOrderMaterialActual
        {
            WorkOrderId = workOrder.Id,
            SkuId = workOrder.ProductSkuId,
            Quantity = 100,
            UnitCost = 6,
            TotalAmount = 600
        });
        await db.SaveChangesAsync();
        await service.DecideInspectionLotAsync(
            inspection.Id,
            new DecideFqcInspectionLotRequestDto
            {
                InspectionStatus = "Failed",
                ApprovedQty = 0,
                WarehouseId = seeded.WarehouseId,
                DecisionBy = "QA lead"
            });

        var scrapNote = await service.CreateScrapNoteAsync(inspection.Id, "不可修復", "Finance pending");

        Assert.Equal(100m, scrapNote.Quantity);
        Assert.Equal(600m, await db.ScrapRecords.Select(record => record.CostAmount).SingleAsync());
        Assert.Equal("pending_accounting", scrapNote.Status);
        Assert.Equal("ScrapPendingAccounting", (await db.FqcInspectionLots.SingleAsync()).DispositionStatus);
        Assert.Equal(100m, (await db.FqcInspectionLots.SingleAsync()).QuarantinedQty);
        Assert.Equal(100m, (await db.Skus.SingleAsync()).Qty);
        Assert.DoesNotContain(await db.InventoryTransactions.ToListAsync(),
            transaction => transaction.Type == "scrap");
        Assert.Empty(await db.AccountingVouchers.ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateReworkWorkOrderAsync(inspection.Id, "PMC"));
    }

    [Fact]
    public async Task CreateProductionReceiptAsync_UpdatesManufacturingProgressWithoutClosingSalesOrder()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeAccountingAccounts: true);
        await AddWorkOrderActualCostAsync(db, seeded.WorkOrderId);
        var sku = await db.Skus.SingleAsync();
        var customer = new Customer { CustomerCode = "C-FQC-001", CustomerName = "FQC customer" };
        var order = new SalesOrder
        {
            OrderNumber = "SO-FQC-001",
            CustomerId = customer.Id,
            Customer = customer,
            Status = "confirmed",
            ManufacturingStatus = "sent"
        };
        var orderItem = new SalesOrderItem
        {
            SalesOrderId = order.Id,
            SalesOrder = order,
            LineNumber = 1,
            ProductId = sku.Id,
            Product = sku,
            SkuId = sku.Id,
            Sku = sku,
            Quantity = 98,
            NeedManufacturing = true,
            ProductionStatus = "sent"
        };
        db.SalesOrders.Add(order);
        db.SalesOrderItems.Add(orderItem);
        db.SalesOrderProductionLinks.Add(new SalesOrderProductionLink
        {
            SalesOrderItemId = orderItem.Id,
            SalesOrderItem = orderItem,
            ProductionDocType = "work_order",
            ProductionDocId = seeded.WorkOrderId,
            LinkedQty = 100
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var inspection = await CreateInspectionAsync(service, seeded.WorkOrderId);
        await service.DecideInspectionLotAsync(
            inspection.Id,
            new DecideFqcInspectionLotRequestDto
            {
                InspectionStatus = "Passed",
                ApprovedQty = 98,
                WarehouseId = seeded.WarehouseId,
                DecisionBy = "QA lead"
            });
        await service.CreateProductionReceiptAsync(new CreateProductionReceiptRequestDto
        {
            FqcInspectionLotId = inspection.Id,
            Quantity = 98,
            WarehouseId = seeded.WarehouseId,
            BinLocationId = seeded.GoodBinId
        });

        Assert.Equal("completed", (await db.SalesOrderItems.SingleAsync()).ProductionStatus);
        Assert.Equal("completed", (await db.SalesOrders.SingleAsync()).ManufacturingStatus);
        Assert.Equal("confirmed", (await db.SalesOrders.SingleAsync()).Status);
    }

    private static async Task<FqcInspectionLotDto> CreateInspectionAsync(
        FqcWorkflowService service,
        Guid workOrderId,
        bool critical = false,
        DateTime? expiryDate = null) =>
        await service.CreateInspectionLotAsync(new CreateFqcInspectionLotRequestDto
        {
            WorkOrderId = workOrderId,
            LotNumber = "FG-LOT-001",
            ExpiryDate = expiryDate,
            SampledQty = 10,
            DefectiveQty = 1,
            CriticalDefectFound = critical,
            DefectCode = "VISUAL",
            InspectorName = "Inspector"
        });

    private static ErpGeneralDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static FqcWorkflowService CreateService(ErpGeneralDbContext db)
    {
        var accounting = new AccountingService(db);
        var costing = new WorkOrderCostingService(db, accounting, new UomConversionService(db));
        return new FqcWorkflowService(db, accounting, costing);
    }

    private static async Task AddWorkOrderActualCostAsync(ErpGeneralDbContext db, Guid workOrderId)
    {
        var workOrder = await db.WorkOrders.SingleAsync(order => order.Id == workOrderId);
        workOrder.ActualQty = 100;
        db.WorkOrderMaterialActuals.Add(new WorkOrderMaterialActual
        {
            WorkOrderId = workOrderId,
            Quantity = 100,
            UnitCost = 7,
            TotalAmount = 700
        });
        await db.SaveChangesAsync();
    }

    private static async Task<(Guid WorkOrderId, Guid WarehouseId, Guid GoodBinId)> SeedWorkOrderAsync(
        ErpGeneralDbContext db,
        bool includeAccountingAccounts = false)
    {
        var sku = new Sku
        {
            SkuNumber = "FG-001",
            SkuName = "Finished product",
            StandardCost = 5
        };
        var operation = new Operation
        {
            OperationCode = "FINAL",
            OperationName = "Final operation"
        };
        var workOrder = new WorkOrder
        {
            WoNumber = "WO-FQC-001",
            ProductSkuId = sku.Id,
            ProductSku = sku,
            PlannedQty = 100,
            Status = "completed"
        };
        workOrder.Operations.Add(new WorkOrderOperation
        {
            WorkOrderId = workOrder.Id,
            WorkOrder = workOrder,
            OperationSeq = 10,
            OperationId = operation.Id,
            Operation = operation,
            WorkCenterId = Guid.NewGuid(),
            PlannedQty = 100,
            CompletedQty = 100,
            Status = "completed"
        });
        var warehouse = new Warehouse
        {
            WarehouseCode = "FG",
            WarehouseName = "Finished goods",
            IsActive = true
        };
        var goodBin = new BinLocation
        {
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
            LocationCode = "A-01",
            IsActive = true
        };
        db.WorkOrders.Add(workOrder);
        db.WipReports.Add(new WipReport
        {
            ReportNumber = "WIP-FQC-001",
            WorkOrderId = workOrder.Id,
            OperationSeq = 10,
            Status = "approved",
            CompletedQty = 100,
            FinishedLotNumber = "FG-LOT-001"
        });
        db.Warehouses.Add(warehouse);
        db.BinLocations.Add(goodBin);
        if (includeAccountingAccounts)
        {
            var finishedGoods = new GLAccount
            {
                AccountCode = "1205",
                AccountName = "Finished goods",
                AccountType = "Asset"
            };
            var workInProgress = new GLAccount
            {
                AccountCode = "1206",
                AccountName = "Work in progress",
                AccountType = "Asset"
            };
            db.GLAccounts.AddRange(finishedGoods, workInProgress);
            db.GLAccountMappings.AddRange(
                new GLAccountMapping
                {
                    MappingKey = "finished_goods_inventory",
                    GLAccount = finishedGoods
                },
                new GLAccountMapping
                {
                    MappingKey = "work_in_progress",
                    GLAccount = workInProgress
                });
        }
        await db.SaveChangesAsync();
        return (workOrder.Id, warehouse.Id, goodBin.Id);
    }
}
