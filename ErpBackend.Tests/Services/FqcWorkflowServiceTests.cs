using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.QA;
using ErpBackend.Models.Sales;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ErpBackend.Tests.Services;

public class FqcWorkflowServiceTests
{
    [Fact]
    public void InventoryLotIndex_AllowsSameLotAcrossDifferentLocations()
    {
        using var db = CreateDbContext();

        var index = db.Model.FindEntityType(typeof(InventoryLot))!
            .GetIndexes()
            .Single(candidate => candidate.IsUnique &&
                candidate.Properties.Any(property => property.Name == nameof(InventoryLot.LotNumber)));

        Assert.Equal(
            new[]
            {
                nameof(InventoryLot.SkuId),
                nameof(InventoryLot.LotNumber),
                nameof(InventoryLot.WarehouseId),
                nameof(InventoryLot.BinLocationId)
            },
            index.Properties.Select(property => property.Name));
    }

    [Fact]
    public async Task GetWorkOrderOptionsAsync_SearchesWorkOrderSkuAndProductName()
    {
        await using var db = CreateDbContext();
        await SeedWorkOrderAsync(db);
        var service = CreateService(db);

        var byWorkOrder = await service.GetWorkOrderOptionsAsync("WO-FQC-001");
        var bySku = await service.GetWorkOrderOptionsAsync("FG-001");
        var byName = await service.GetWorkOrderOptionsAsync("Finished product");

        Assert.All(new[] { byWorkOrder, bySku, byName }, options =>
        {
            var option = Assert.Single(options);
            Assert.Equal("WO-FQC-001", option.WoNumber);
            Assert.Equal("FG-001", option.ProductSkuNumber);
            Assert.Equal("Finished product", option.ProductSkuName);
        });
    }

    [Fact]
    public async Task GetDefaultFinishedLotAsync_UsesApprovedLotFromFinalOperation()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db);
        db.WipReports.Add(new WipReport
        {
            ReportNumber = "WIP-FQC-NONFINAL",
            WorkOrderId = seeded.WorkOrderId,
            OperationSeq = 5,
            Status = "approved",
            CompletedQty = 15,
            FinishedLotNumber = "NONFINAL-LOT"
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetDefaultFinishedLotAsync(seeded.WorkOrderId);

        Assert.NotNull(result);
        Assert.Equal("FG-LOT-001", result.LotNumber);
        Assert.Equal(100m, result.ProducedQty);
    }

    [Fact]
    public async Task GetWarehouseOptionsAsync_ListsActiveWarehousesAndSelectableBins()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db);
        var inactiveWarehouse = new Warehouse
        {
            WarehouseCode = "INACTIVE",
            WarehouseName = "Inactive warehouse",
            IsActive = false
        };
        db.Warehouses.Add(inactiveWarehouse);
        db.BinLocations.AddRange(
            new BinLocation
            {
                WarehouseId = seeded.WarehouseId,
                LocationCode = "FQC-HOLD",
                IsActive = true
            },
            new BinLocation
            {
                WarehouseId = seeded.WarehouseId,
                LocationCode = "DISABLED",
                IsActive = false
            });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var warehouses = await service.GetWarehouseOptionsAsync();
        var bins = await service.GetBinLocationOptionsAsync(seeded.WarehouseId);

        var warehouse = Assert.Single(warehouses);
        Assert.Equal("FG", warehouse.WarehouseCode);
        var bin = Assert.Single(bins);
        Assert.Equal("A-01", bin.LocationCode);
    }

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
        var expiryDate = new DateTime(2028, 4, 30);
        var inspection = await CreateInspectionAsync(
            service, seeded.WorkOrderId, critical: true, expiryDate: expiryDate);

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
        var quarantinedLot = await db.InventoryLots.SingleAsync();
        Assert.Equal(expiryDate, quarantinedLot.ExpiryDate);
        Assert.Equal(DateTimeKind.Utc, quarantinedLot.ExpiryDate!.Value.Kind);
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
        var inventoryLot = await db.InventoryLots.FirstAsync();
        Assert.Equal(expiryDate, inventoryLot.ExpiryDate);
        Assert.Equal(DateTimeKind.Utc, inventoryLot.ExpiryDate!.Value.Kind);
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

        var scrapNote = await service.CreateScrapNoteAsync(
            inspection.Id,
            "03",
            "不可修復",
            Guid.NewGuid(),
            "Warehouse Operator");

        Assert.Equal(100m, scrapNote.Quantity);
        Assert.Equal(600m, await db.ScrapRecords.Select(record => record.CostAmount).SingleAsync());
        Assert.Equal("pending_quality_review", scrapNote.Status);
        Assert.Equal("ScrapPendingQualityReview", (await db.FqcInspectionLots.SingleAsync()).DispositionStatus);
        Assert.Equal(100m, (await db.FqcInspectionLots.SingleAsync()).QuarantinedQty);
        Assert.Equal(100m, (await db.Skus.SingleAsync()).Qty);
        Assert.DoesNotContain(await db.InventoryTransactions.ToListAsync(),
            transaction => transaction.Type == "scrap");
        Assert.Empty(await db.AccountingVouchers.ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateReworkWorkOrderAsync(inspection.Id, "PMC"));
    }

    [Fact]
    public async Task ScrapApproval_RequiresQualityBeforeFinanceAndNeverPostsInventoryOrVoucher()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db);
        await AddWorkOrderActualCostAsync(db, seeded.WorkOrderId);
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
        var requestorId = Guid.NewGuid();
        var qaId = Guid.NewGuid();
        var financeId = Guid.NewGuid();
        var scrap = await service.CreateScrapNoteAsync(
            inspection.Id, "03", "不可修復", requestorId, "Warehouse Operator");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DecideFqcScrapFinanceAsync(scrap.Id, financeId, "Finance", true, null));

        await service.DecideFqcScrapQualityAsync(scrap.Id, qaId, "QA lead", true, "確認不可修復");
        await service.DecideFqcScrapFinanceAsync(scrap.Id, financeId, "Finance Manager", true, "核准");

        var updatedScrap = await db.ScrapRecords.SingleAsync();
        var updatedLot = await db.FqcInspectionLots.SingleAsync();
        Assert.Equal("approved_pending_disposal", updatedScrap.ScrapStatus);
        Assert.Equal("ScrapApprovedPendingExecution", updatedLot.DispositionStatus);
        Assert.Equal(3, await db.FqcScrapApprovalEvents.CountAsync());
        Assert.Equal(100m, updatedLot.QuarantinedQty);
        Assert.Equal(100m, (await db.Skus.SingleAsync()).Qty);
        Assert.DoesNotContain(await db.InventoryTransactions.ToListAsync(),
            transaction => transaction.Type == "scrap");
        Assert.Empty(await db.AccountingVouchers.ToListAsync());
    }

    [Fact]
    public async Task ScrapRejection_CanOnlyBeResubmittedByRequesterAndRestartsQualityReview()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db);
        await AddWorkOrderActualCostAsync(db, seeded.WorkOrderId);
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
        var requestorId = Guid.NewGuid();
        var scrap = await service.CreateScrapNoteAsync(
            inspection.Id, "03", "不可修復", requestorId, "Warehouse Operator");

        await service.DecideFqcScrapQualityAsync(scrap.Id, Guid.NewGuid(), "QA lead", false, "補充檢驗依據");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResubmitFqcScrapNoteAsync(
            scrap.Id, "03", "補充說明", Guid.NewGuid(), "Other Operator"));
        await service.ResubmitFqcScrapNoteAsync(
            scrap.Id, "03", "已補充檢驗依據", requestorId, "Warehouse Operator");

        Assert.Equal("pending_quality_review", (await db.ScrapRecords.SingleAsync()).ScrapStatus);
        Assert.Equal(
            new[] { "submitted", "rejected", "resubmitted" },
            await db.FqcScrapApprovalEvents
                .OrderBy(item => item.OccurredAt)
                .Select(item => item.Decision)
                .ToArrayAsync());
    }

    [Fact]
    public async Task ReworkReinspection_CannotCloseBeforeFqcReceiptAndClosesOnceReconciled()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeAccountingAccounts: true);
        var service = CreateService(db);
        var originalInspection = await CreateInspectionAsync(service, seeded.WorkOrderId);
        await service.DecideInspectionLotAsync(
            originalInspection.Id,
            new DecideFqcInspectionLotRequestDto
            {
                InspectionStatus = "Failed",
                ApprovedQty = 0,
                WarehouseId = seeded.WarehouseId,
                DecisionBy = "QA lead"
            });
        var reworkDisposition = await service.CreateReworkWorkOrderAsync(originalInspection.Id, "PMC");
        var reworkOrder = await db.WorkOrders
            .Include(order => order.Materials)
            .Include(order => order.Operations)
            .SingleAsync(order => order.Id == reworkDisposition.ReworkWorkOrderId);
        reworkOrder.Status = "pending_fqc";
        var reworkMaterial = Assert.Single(reworkOrder.Materials);
        reworkMaterial.IssuedQty = reworkMaterial.RequiredQty;
        var sourceLot = await db.FqcInspectionLots.SingleAsync(item => item.Id == originalInspection.Id);
        sourceLot.QuarantinedQty = 0;
        sourceLot.DispositionStatus = "ReworkIssued";
        db.WipReports.Add(new WipReport
        {
            ReportNumber = "WIP-REWORK-001",
            WorkOrderId = reworkOrder.Id,
            OperationSeq = 10,
            Status = "approved",
            CompletedQty = reworkOrder.PlannedQty,
            FinishedLotNumber = originalInspection.LotNumber
        });
        await db.SaveChangesAsync();

        var reinspection = await service.CreateInspectionLotAsync(new CreateFqcInspectionLotRequestDto
        {
            WorkOrderId = reworkOrder.Id,
            LotNumber = originalInspection.LotNumber,
            SampledQty = 10,
            DefectiveQty = 0,
            InspectorName = "Re-inspector"
        });

        Assert.Equal(originalInspection.Id, reinspection.ReworkSourceInspectionLotId);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CloseReworkWorkOrderAsync(reworkOrder.Id, "PMC"));

        await service.DecideInspectionLotAsync(
            reinspection.Id,
            new DecideFqcInspectionLotRequestDto
            {
                InspectionStatus = "Passed",
                ApprovedQty = reinspection.ProducedQty,
                WarehouseId = seeded.WarehouseId,
                DecisionBy = "QA lead"
            });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CloseReworkWorkOrderAsync(reworkOrder.Id, "PMC"));

        await AddWorkOrderActualCostAsync(db, reworkOrder.Id);
        await service.CreateProductionReceiptAsync(new CreateProductionReceiptRequestDto
        {
            FqcInspectionLotId = reinspection.Id,
            Quantity = reinspection.ProducedQty,
            WarehouseId = seeded.WarehouseId,
            BinLocationId = seeded.GoodBinId
        });

        await service.CloseReworkWorkOrderAsync(reworkOrder.Id, "PMC");

        Assert.Equal("completed", (await db.WorkOrders.SingleAsync(item => item.Id == reworkOrder.Id)).Status);
        Assert.Equal("ReworkCompleted",
            (await db.FqcInspectionLots.SingleAsync(item => item.Id == originalInspection.Id)).DispositionStatus);
    }

    [Fact]
    public async Task ReworkWorkOrder_CannotCreateASecondReworkCycle()
    {
        await using var db = CreateDbContext();
        var seeded = await SeedWorkOrderAsync(db);
        var service = CreateService(db);
        var originalInspection = await CreateInspectionAsync(service, seeded.WorkOrderId);
        await service.DecideInspectionLotAsync(
            originalInspection.Id,
            new DecideFqcInspectionLotRequestDto
            {
                InspectionStatus = "Failed",
                ApprovedQty = 0,
                WarehouseId = seeded.WarehouseId,
                DecisionBy = "QA lead"
            });
        var rework = await service.CreateReworkWorkOrderAsync(originalInspection.Id, "PMC");
        var reworkOrder = await db.WorkOrders.SingleAsync(item => item.Id == rework.ReworkWorkOrderId);
        var reworkInspection = new FqcInspectionLot
        {
            InspectionNumber = "FQC-REWORK-001",
            WorkOrderId = reworkOrder.Id,
            ProductSkuId = reworkOrder.ProductSkuId,
            LotNumber = "FG-LOT-001",
            ProducedQty = 100,
            SampledQty = 10,
            InspectorName = "Inspector",
            InspectionStatus = "Failed",
            ReworkSourceInspectionLotId = originalInspection.Id,
            WorkOrder = reworkOrder,
            ProductSku = (await db.Skus.SingleAsync())
        };
        db.FqcInspectionLots.Add(reworkInspection);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateReworkWorkOrderAsync(reworkInspection.Id, "PMC"));
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
