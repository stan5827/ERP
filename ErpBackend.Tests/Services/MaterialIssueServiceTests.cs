using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.Finance;
using ErpBackend.Models.QA;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ErpBackend.Tests.Services;

public class MaterialIssueServiceTests
{
    [Fact]
    public async Task CreateDraft_UsesWorkOrderMaterialSnapshotAndRemainingDemand()
    {
        await using var db = CreateDbContext();
        var (workOrder, _, material, _) = SeedWorkOrder(db, stockQty: 25);
        material.RequiredQty = 20;
        material.IssuedQty = 4;
        material.ClosedShortageQty = 3;
        material.Unit = "g";
        material.Sku.Unit = "g";
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (issue, status) = await service.CreateDraftAsync(workOrder.Id);

        Assert.Equal("ok", status);
        var item = Assert.Single(issue!.Items);
        Assert.Equal(13m, item.RequiredQty);
        Assert.Equal("g", item.Unit);

        var (sameIssue, secondStatus) = await service.CreateDraftAsync(workOrder.Id);
        Assert.Equal("ok", secondStatus);
        Assert.Equal(issue.Id, sameIssue!.Id);
    }

    [Fact]
    public async Task Post_UsesBomUnitForDemandAndInventoryUnitForStock()
    {
        await using var db = CreateDbContext();
        var (workOrder, sku, material, warehouse) = SeedWorkOrder(db, stockQty: 50);
        sku.Unit = "KG";
        material.Unit = "G";
        material.RequiredQty = 20000;
        material.ShortageQty = 20000;
        db.Uoms.AddRange(
            new Uom { UomCode = "G", UomName = "公克", UomType = "重量" },
            new Uom { UomCode = "KG", UomName = "公斤", UomType = "重量" });
        db.UomConversions.Add(new UomConversion
        {
            FromUomId = db.Uoms.Local.Single(uom => uom.UomCode == "KG").Id,
            ToUomId = db.Uoms.Local.Single(uom => uom.UomCode == "G").Id,
            Factor = 1000,
            Operator = "*",
            InverseFactor = 0.001m,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (draft, draftStatus) = await service.CreateDraftAsync(workOrder.Id);

        Assert.Equal("ok", draftStatus);
        var item = Assert.Single(draft!.Items);
        Assert.Equal(20000m, item.RequiredQty);
        Assert.Equal("G", item.Unit);
        Assert.Equal("KG", item.InventoryUnit);
        Assert.Equal(50m, item.OnHandQty);
        Assert.Equal(20m, item.DefaultIssueQty);

        var (posted, status) = await service.PostAsync(
            draft.Id,
            new PostMaterialIssueRequestDto
            {
                Allocations =
                [
                    new MaterialIssueAllocationRequestDto
                    {
                        IssueItemId = item.Id,
                        WarehouseId = warehouse.Id,
                        IssuedQty = 5
                    }
                ]
            },
            Guid.NewGuid());

        Assert.Equal("ok", status);
        var postedItem = Assert.Single(posted!.Items);
        Assert.Equal(5000m, postedItem.IssuedQty);
        Assert.Equal(15000m, postedItem.ShortageQty);
        Assert.Equal(45m, (await db.Skus.FindAsync(sku.Id))!.Qty);
        Assert.Equal(5000m, material.IssuedQty);
        Assert.Equal(15000m, material.ShortageQty);
        Assert.Equal(-5m, await db.InventoryTransactions
            .Where(transaction => transaction.Type == "work_order_issue")
            .SumAsync(transaction => transaction.QtyChange));

        var (closed, closeStatus) = await service.CloseShortageAsync(
            posted.Id,
            item.Id,
            new CloseMaterialIssueShortageRequestDto { Quantity = 15000, Reason = "剩餘需求取消" });
        Assert.Equal("ok", closeStatus);
        Assert.Equal(15000m, Assert.Single(closed!.Items).ClosedShortageQty);
        Assert.Equal(15000m, material.ClosedShortageQty);
        Assert.Equal(0m, material.ShortageQty);

        var (reversal, reverseStatus) = await service.ReverseAsync(posted.Id, Guid.NewGuid());
        Assert.Equal("ok", reverseStatus);
        Assert.Equal("reversal", reversal!.Status);
        Assert.Equal(50m, (await db.Skus.FindAsync(sku.Id))!.Qty);
        Assert.Equal(0m, material.IssuedQty);
        Assert.Equal(15000m, material.ClosedShortageQty);
        Assert.Equal(5000m, material.ShortageQty);
        Assert.Equal("shortage", material.Status);
    }

    [Fact]
    public async Task Post_PartialIssueUpdatesLedgerAndWorkOrderSnapshot()
    {
        await using var db = CreateDbContext();
        var (workOrder, sku, material, warehouse) = SeedWorkOrder(db, stockQty: 10);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (draft, draftStatus) = await service.CreateDraftAsync(workOrder.Id);
        Assert.Equal("ok", draftStatus);
        var itemId = Assert.Single(draft!.Items).Id;

        var (posted, status) = await service.PostAsync(
            draft.Id,
            new PostMaterialIssueRequestDto
            {
                Allocations =
                [
                    new MaterialIssueAllocationRequestDto
                    {
                        IssueItemId = itemId,
                        WarehouseId = warehouse.Id,
                        IssuedQty = 4
                    }
                ]
            },
            Guid.NewGuid());

        Assert.Equal("ok", status);
        Assert.Equal("partially_issued", posted!.Status);
        Assert.Equal(4m, Assert.Single(posted.Items).IssuedQty);
        Assert.Equal(6m, Assert.Single(posted.Items).ShortageQty);
        Assert.Equal(6m, (await db.Skus.FindAsync(sku.Id))!.Qty);
        Assert.Equal(4m, material.IssuedQty);
        Assert.Equal(6m, await db.InventoryTransactions
            .Where(transaction => transaction.SkuId == sku.Id)
            .SumAsync(transaction => transaction.QtyChange));
    }

    [Fact]
    public async Task Post_WhenStockIsInsufficient_DoesNotWriteInventoryTransaction()
    {
        await using var db = CreateDbContext();
        var (workOrder, sku, _, warehouse) = SeedWorkOrder(db, stockQty: 3);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (draft, _) = await service.CreateDraftAsync(workOrder.Id);
        var itemId = Assert.Single(draft!.Items).Id;
        var transactionCountBefore = await db.InventoryTransactions.CountAsync();

        var (posted, status) = await service.PostAsync(
            draft.Id,
            new PostMaterialIssueRequestDto
            {
                Allocations =
                [
                    new MaterialIssueAllocationRequestDto
                    {
                        IssueItemId = itemId,
                        WarehouseId = warehouse.Id,
                        IssuedQty = 4
                    }
                ]
            },
            Guid.NewGuid());

        Assert.Null(posted);
        Assert.StartsWith("insufficient_stock:", status);
        Assert.Equal(transactionCountBefore, await db.InventoryTransactions.CountAsync());
        Assert.Equal(3m, (await db.Skus.FindAsync(sku.Id))!.Qty);
    }

    [Fact]
    public async Task Post_RegularWorkOrderCannotIssueFqcQuarantineStock()
    {
        await using var db = CreateDbContext();
        var (workOrder, sku, _, warehouse) = SeedWorkOrder(db, stockQty: 10);
        var quarantineBin = new BinLocation
        {
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
            LocationCode = "FQC-HOLD",
            IsActive = true
        };
        db.BinLocations.Add(quarantineBin);
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            TrxId = "FQC-HOLD-REGULAR",
            SkuId = sku.Id,
            Type = "fqc_quarantine_in",
            QtyChange = 10,
            QtyBefore = 0,
            QtyAfter = 10,
            WarehouseId = warehouse.Id,
            BinLocationId = quarantineBin.Id,
            LotNumber = "FQC-LOT"
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (draft, _) = await service.CreateDraftAsync(workOrder.Id);
        var itemId = Assert.Single(draft!.Items).Id;
        var (posted, status) = await service.PostAsync(
            draft.Id,
            new PostMaterialIssueRequestDto
            {
                Allocations =
                [
                    new MaterialIssueAllocationRequestDto
                    {
                        IssueItemId = itemId,
                        WarehouseId = warehouse.Id,
                        BinLocationId = quarantineBin.Id,
                        LotNumber = "FQC-LOT",
                        IssuedQty = 1
                    }
                ]
            },
            Guid.NewGuid());

        Assert.Null(posted);
        Assert.Equal("fqc_quarantine_requires_rework", status);
        Assert.Equal(10m, (await db.Skus.FindAsync(sku.Id))!.Qty);
    }

    [Fact]
    public async Task Post_ReworkMaterialRequiresItsFqcQuarantineLotAndUsesActualUnitCost()
    {
        await using var db = CreateDbContext();
        var sku = new Sku
        {
            SkuNumber = "FG-REWORK-1",
            SkuName = "Finished product",
            Unit = "ea",
            Qty = 10
        };
        var warehouse = new Warehouse { WarehouseCode = "QA", WarehouseName = "QA quarantine" };
        var quarantineBin = new BinLocation
        {
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
            LocationCode = "FQC-HOLD",
            IsActive = true
        };
        var wrongBin = new BinLocation
        {
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
            LocationCode = "A-01",
            IsActive = true
        };
        var sourceWorkOrder = new WorkOrder
        {
            WoNumber = "WO-SOURCE-1",
            ProductSkuId = sku.Id,
            ProductSku = sku,
            PlannedQty = 10,
            ActualQty = 10,
            Status = "completed"
        };
        var fqcLot = new FqcInspectionLot
        {
            InspectionNumber = "FQC-REWORK-1",
            WorkOrderId = sourceWorkOrder.Id,
            WorkOrder = sourceWorkOrder,
            ProductSkuId = sku.Id,
            ProductSku = sku,
            LotNumber = "LOT-REWORK-1",
            ProducedQty = 10,
            InspectionStatus = "Failed",
            DispositionStatus = "Rework",
            InspectorName = "QA",
            QuarantinedQty = 10,
            QuarantineWarehouseId = warehouse.Id,
            QuarantineWarehouse = warehouse,
            QuarantineBinLocationId = quarantineBin.Id,
            QuarantineBinLocation = quarantineBin
        };
        var reworkOrder = new WorkOrder
        {
            WoNumber = "WO-REWORK-1",
            WoType = "rework",
            ProductSkuId = sku.Id,
            ProductSku = sku,
            PlannedQty = 10,
            Status = "planned"
        };
        var workOrderMaterial = new WorkOrderMaterial
        {
            WorkOrderId = reworkOrder.Id,
            WorkOrder = reworkOrder,
            SkuId = sku.Id,
            Sku = sku,
            FqcInspectionLotId = fqcLot.Id,
            FqcInspectionLot = fqcLot,
            RequiredQty = 10,
            Unit = "ea",
            ShortageQty = 10,
            Status = "pending"
        };
        reworkOrder.Materials.Add(workOrderMaterial);
        db.Skus.Add(sku);
        db.Warehouses.Add(warehouse);
        db.BinLocations.AddRange(quarantineBin, wrongBin);
        db.WorkOrders.AddRange(sourceWorkOrder, reworkOrder);
        db.FqcInspectionLots.Add(fqcLot);
        db.WorkOrderMaterials.Add(workOrderMaterial);
        db.InventoryLots.Add(new InventoryLot
        {
            SkuId = sku.Id,
            Sku = sku,
            LotNumber = fqcLot.LotNumber,
            WarehouseId = warehouse.Id,
            BinLocationId = quarantineBin.Id,
            Qty = 10,
            Status = "quarantine"
        });
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            TrxId = "FQC-HOLD-IN",
            SkuId = sku.Id,
            Type = "fqc_quarantine_in",
            QtyChange = 10,
            QtyBefore = 0,
            QtyAfter = 10,
            LotNumber = fqcLot.LotNumber,
            WarehouseId = warehouse.Id,
            BinLocationId = quarantineBin.Id
        });
        db.WorkOrderMaterialActuals.Add(new WorkOrderMaterialActual
        {
            WorkOrderId = sourceWorkOrder.Id,
            SkuId = sku.Id,
            Quantity = 10,
            UnitCost = 6,
            TotalAmount = 60
        });
        db.WorkOrderLaborActuals.Add(new WorkOrderLaborActual
        {
            WorkOrderId = sourceWorkOrder.Id,
            ActualHours = 1,
            HourlyRate = 2,
            TotalAmount = 2
        });
        db.WorkOrderMachineActuals.Add(new WorkOrderMachineActual
        {
            WorkOrderId = sourceWorkOrder.Id,
            ActualHours = 1,
            MachineRate = 1,
            OverheadRate = 1,
            TotalAmount = 1,
            OverheadAmount = 1
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var (draft, draftStatus) = await service.CreateDraftAsync(reworkOrder.Id);
        Assert.Equal("ok", draftStatus);
        var issueItem = Assert.Single(draft!.Items);
        var wrongLocationResult = await service.PostAsync(
            draft.Id,
            new PostMaterialIssueRequestDto
            {
                Allocations =
                [
                    new MaterialIssueAllocationRequestDto
                    {
                        IssueItemId = issueItem.Id,
                        WarehouseId = warehouse.Id,
                        BinLocationId = wrongBin.Id,
                        LotNumber = fqcLot.LotNumber,
                        IssuedQty = 6
                    }
                ]
            },
            Guid.NewGuid());
        Assert.Equal("fqc_rework_source_must_match_quarantine_lot", wrongLocationResult.status);
        Assert.Equal(10m, (await db.Skus.FindAsync(sku.Id))!.Qty);
        Assert.Equal(10m, fqcLot.QuarantinedQty);

        var result = await service.PostAsync(
            draft.Id,
            new PostMaterialIssueRequestDto
            {
                Allocations =
                [
                    new MaterialIssueAllocationRequestDto
                    {
                        IssueItemId = issueItem.Id,
                        WarehouseId = warehouse.Id,
                        BinLocationId = quarantineBin.Id,
                        LotNumber = fqcLot.LotNumber,
                        IssuedQty = 6
                    }
                ]
            },
            Guid.NewGuid());

        Assert.Equal("ok", result.status);
        Assert.Equal(4m, (await db.Skus.FindAsync(sku.Id))!.Qty);
        Assert.Equal(4m, fqcLot.QuarantinedQty);
        Assert.Equal(4m, (await db.InventoryLots.SingleAsync()).Qty);
        var issueTrx = await db.InventoryTransactions.SingleAsync(transaction => transaction.Type == "fqc_rework_issue");
        Assert.Equal(6.4m, issueTrx.UnitCost);
        Assert.Equal(fqcLot.Id, issueTrx.RelatedDocId);
        Assert.Equal("FqcReworkIssue", issueTrx.RelatedDocType);
    }

    [Fact]
    public async Task CloseShortageAndReverse_PreserveClosedShortageAndRestoreInventory()
    {
        await using var db = CreateDbContext();
        var (workOrder, sku, material, warehouse) = SeedWorkOrder(db, stockQty: 10);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var (draft, _) = await service.CreateDraftAsync(workOrder.Id);
        var itemId = Assert.Single(draft!.Items).Id;

        var (posted, postStatus) = await service.PostAsync(
            draft.Id,
            new PostMaterialIssueRequestDto
            {
                Allocations =
                [
                    new MaterialIssueAllocationRequestDto
                    {
                        IssueItemId = itemId,
                        WarehouseId = warehouse.Id,
                        IssuedQty = 4
                    }
                ]
            },
            Guid.NewGuid());

        Assert.Equal("ok", postStatus);
        var (closed, closeStatus) = await service.CloseShortageAsync(
            posted!.Id,
            itemId,
            new CloseMaterialIssueShortageRequestDto { Quantity = 6, Reason = "需求取消" });
        Assert.Equal("ok", closeStatus);
        Assert.Equal("issued", closed!.Status);

        var (reversal, reverseStatus) = await service.ReverseAsync(posted.Id, Guid.NewGuid());

        Assert.Equal("ok", reverseStatus);
        Assert.Equal("reversal", reversal!.Status);
        Assert.Equal("reversed", (await service.GetIssueAsync(posted.Id))!.Status);
        Assert.Equal(10m, (await db.Skus.FindAsync(sku.Id))!.Qty);
        Assert.Equal(0m, material.IssuedQty);
        Assert.Equal(6m, material.ClosedShortageQty);
        Assert.Equal(4m, material.ShortageQty);
        Assert.Equal(10m, await db.InventoryTransactions
            .Where(transaction => transaction.SkuId == sku.Id)
            .SumAsync(transaction => transaction.QtyChange));
    }

    private static ErpGeneralDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static MaterialIssueService CreateService(ErpGeneralDbContext db)
    {
        var costing = new Mock<ICostingService>();
        costing.Setup(service => service.RecordMaterialActualAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<string?>(),
                It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);
        return new MaterialIssueService(db, costing.Object, new UomConversionService(db));
    }

    private static (WorkOrder WorkOrder, Sku Sku, WorkOrderMaterial Material, Warehouse Warehouse)
        SeedWorkOrder(ErpGeneralDbContext db, decimal stockQty)
    {
        var product = new Sku
        {
            SkuNumber = "PRODUCT-1",
            SkuName = "Test product",
            Unit = "ea",
            Qty = 0
        };
        var sku = new Sku
        {
            SkuNumber = "MATERIAL-1",
            SkuName = "Test material",
            Unit = "kg",
            Qty = stockQty
        };
        var warehouse = new Warehouse
        {
            WarehouseCode = "WH-1",
            WarehouseName = "Main warehouse"
        };
        sku.DefaultWarehouseId = warehouse.Id;
        var workOrder = new WorkOrder
        {
            WoNumber = "WO-TEST-1",
            ProductSkuId = product.Id,
            ProductSku = product,
            PlannedQty = 1,
            Status = "released"
        };
        var material = new WorkOrderMaterial
        {
            WorkOrderId = workOrder.Id,
            WorkOrder = workOrder,
            SkuId = sku.Id,
            Sku = sku,
            RequiredQty = 10,
            Unit = sku.Unit,
            Status = "shortage"
        };
        workOrder.Materials.Add(material);
        db.Skus.AddRange(product, sku);
        db.Warehouses.Add(warehouse);
        db.WorkOrders.Add(workOrder);
        db.WorkOrderMaterials.Add(material);
        db.InventoryTransactions.Add(new InventoryTransaction
        {
            TrxId = "OPEN-TEST",
            SkuId = sku.Id,
            Type = "opening_balance",
            QtyChange = stockQty,
            QtyBefore = 0,
            QtyAfter = stockQty,
            WarehouseId = warehouse.Id,
            UnitCost = 2,
            TotalCost = stockQty * 2,
            CreatedAt = DateTime.UtcNow
        });
        return (workOrder, sku, material, warehouse);
    }
}
