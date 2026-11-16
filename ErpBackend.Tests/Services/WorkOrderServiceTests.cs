using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Sales;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class WorkOrderServiceTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ErpGeneralDbContext(options);
    }

    [Fact]
    public async Task CreateFromSalesOrderAsync_ConvertsSalesQuantityToProductionUom()
    {
        using var db = CreateInMemoryDbContext();
        var salesUom = new Uom { UomCode = "BOX", UomName = "箱", UomType = "package" };
        var productionUom = new Uom { UomCode = "PIECE", UomName = "片", UomType = "base" };
        var product = new Sku
        {
            SkuNumber = "FG-BOX",
            SkuName = "箱裝成品",
            IsManufacturable = true,
            SalesUom = salesUom,
            SalesUomId = salesUom.Id,
            ProductionUom = productionUom,
            ProductionUomId = productionUom.Id
        };
        var component = new Sku { SkuNumber = "RM-BOX", SkuName = "原料", Unit = "G" };
        var customer = new Customer { CustomerCode = "CUST-BOX", CustomerName = "箱裝測試客戶" };
        db.AddRange(salesUom, productionUom, product, component, customer);
        await db.SaveChangesAsync();
        db.UomConversions.Add(new UomConversion
        {
            SkuId = product.Id,
            FromUomId = salesUom.Id,
            ToUomId = productionUom.Id,
            Factor = 10,
            Operator = "*",
            FromUom = salesUom,
            ToUom = productionUom
        });

        var order = new SalesOrder
        {
            OrderNumber = "SO-BOX-001",
            CustomerId = customer.Id,
            Status = "confirmed"
        };
        order.Items.Add(new SalesOrderItem
        {
            SalesOrderId = order.Id,
            LineNumber = 1,
            ProductId = product.Id,
            SkuId = product.Id,
            Quantity = 10,
            NeedManufacturing = true
        });
        var bom = new Bom
        {
            BomCode = "BOM-BOX-001",
            ProductSkuId = product.Id,
            Version = "1",
            Status = "active",
            EffectiveDate = DateTime.UtcNow.Date.AddDays(-1)
        };
        bom.Items.Add(new BomItem
        {
            BomId = bom.Id,
            ChildSkuId = component.Id,
            Quantity = 2,
            Unit = "G"
        });
        var workCenter = new WorkCenter { WorkCenterCode = "WC-BOX", WorkCenterName = "測試中心" };
        var operation = new Operation { OperationCode = "OP-BOX", OperationName = "組裝" };
        var routing = new Routing
        {
            RoutingCode = "RT-BOX",
            RoutingName = "箱裝製程",
            ProductSkuId = product.Id,
            IsActive = true
        };
        routing.Operations.Add(new RoutingOperation
        {
            RoutingId = routing.Id,
            OperationSeq = 1,
            OperationId = operation.Id,
            WorkCenterId = workCenter.Id,
            Operation = operation,
            WorkCenter = workCenter
        });
        db.AddRange(order, bom, workCenter, operation, routing);
        await db.SaveChangesAsync();

        var (workOrders, status) = await new WorkOrderService(db).CreateFromSalesOrderAsync(order.Id);

        Assert.Equal("ok", status);
        var workOrder = Assert.Single(workOrders);
        Assert.Equal(100m, workOrder.PlannedQty);
        Assert.Equal("PIECE", workOrder.ProductionUomCode);
        Assert.Equal(200m, await db.WorkOrderMaterials.SumAsync(material => material.RequiredQty));
    }

    [Fact]
    public async Task GetSalesOrderWorkOrderTransferCandidatesAsync_OnlyReturnsConfirmedUnsentManufacturingOrders()
    {
        using var db = CreateInMemoryDbContext();
        var customer = new Customer { CustomerCode = "CUST-TRANSFER", CustomerName = "轉發測試客戶" };
        var manufacturableSku = new Sku
        {
            SkuNumber = "FG-TRANSFER",
            SkuName = "內製品",
            IsManufacturable = true
        };
        var nonManufacturableSku = new Sku
        {
            SkuNumber = "FG-NON-MFG",
            SkuName = "非內製品",
            IsManufacturable = false
        };
        db.AddRange(customer, manufacturableSku, nonManufacturableSku);

        SalesOrder CreateOrder(string orderNumber, string status, string manufacturingStatus, Sku sku, bool needManufacturing)
        {
            var order = new SalesOrder
            {
                OrderNumber = orderNumber,
                CustomerId = customer.Id,
                Customer = customer,
                Status = status,
                ManufacturingStatus = manufacturingStatus
            };
            order.Items.Add(new SalesOrderItem
            {
                SalesOrderId = order.Id,
                LineNumber = 1,
                ProductId = sku.Id,
                SkuId = sku.Id,
                Sku = sku,
                Quantity = 12m,
                NeedManufacturing = needManufacturing
            });
            return order;
        }

        var candidate = CreateOrder("SO-TRANSFER-001", "confirmed", "pending", manufacturableSku, false);
        candidate.Items.Add(new SalesOrderItem
        {
            SalesOrderId = candidate.Id,
            LineNumber = 2,
            ProductId = nonManufacturableSku.Id,
            SkuId = nonManufacturableSku.Id,
            Sku = nonManufacturableSku,
            Quantity = 4m
        });
        var unconfirmed = CreateOrder("SO-TRANSFER-002", "pending", "pending", manufacturableSku, true);
        var alreadySent = CreateOrder("SO-TRANSFER-003", "confirmed", "sent", manufacturableSku, true);
        var noManufacturing = CreateOrder("SO-TRANSFER-004", "confirmed", "pending", nonManufacturableSku, false);
        db.AddRange(candidate, unconfirmed, alreadySent, noManufacturing);
        await db.SaveChangesAsync();

        var result = await new WorkOrderService(db).GetSalesOrderWorkOrderTransferCandidatesAsync();

        var transferCandidate = Assert.Single(result);
        Assert.Equal(candidate.Id, transferCandidate.SalesOrderId);
        Assert.Equal(candidate.OrderNumber, transferCandidate.OrderNumber);
        Assert.Equal(customer.CustomerName, transferCandidate.CustomerName);
        Assert.Equal(2, transferCandidate.Items.Count);
        Assert.Equal("FG-TRANSFER", transferCandidate.Items[0].SkuNumber);
        Assert.Equal(12m, transferCandidate.Items[0].Quantity);
        Assert.True(transferCandidate.Items[0].WillCreateWorkOrder);
        Assert.False(transferCandidate.Items[0].IsAlreadyTransferred);
        Assert.Equal("FG-NON-MFG", transferCandidate.Items[1].SkuNumber);
        Assert.False(transferCandidate.Items[1].WillCreateWorkOrder);
    }

    private async Task<ErpGeneralDbContext> SeedAsync(ErpGeneralDbContext db)
    {
        var sku = new Sku { SkuNumber = "SKU001", SkuName = "TestSku" };
        db.Skus.Add(sku);
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllWorkOrders()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var skuId = (await db.Skus.FirstAsync()).Id;
        var customer = new Customer { CustomerCode = "CUST-ORDER", CustomerName = "Order Customer" };
        var salesOrder = new SalesOrder
        {
            OrderNumber = "SO-WO-001",
            CustomerId = customer.Id,
            Status = "confirmed"
        };

        db.WorkOrders.AddRange(
            new WorkOrder { WoNumber = "WO-001", ProductSkuId = skuId, Status = "planned", PlannedQty = 100, Priority = "high", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new WorkOrder { WoNumber = "WO-002", ProductSkuId = skuId, SalesOrderId = salesOrder.Id, Status = "released", PlannedQty = 200, Priority = "normal", CreatedAt = DateTime.UtcNow });
        db.AddRange(customer, salesOrder);
        await db.SaveChangesAsync();

        var service = new WorkOrderService(db);
        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("WO-002", result[0].WoNumber);
        Assert.Equal("SO-WO-001", result[0].SalesOrderNumber);
        Assert.Null(result[1].SalesOrderNumber);

        var detail = await service.GetByIdAsync(result[0].Id);
        Assert.Equal("SO-WO-001", detail!.SalesOrderNumber);
    }

    [Fact]
    public async Task GetAllAsync_IncludesIssuedAndRemainingMaterialQuantities()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var sku = await db.Skus.SingleAsync();
        var workOrder = new WorkOrder
        {
            WoNumber = "WO-MATERIAL-PROGRESS",
            ProductSkuId = sku.Id,
            Status = "in_progress",
            CreatedAt = DateTime.UtcNow
        };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();
        db.WorkOrderMaterials.Add(new WorkOrderMaterial
        {
            WorkOrderId = workOrder.Id,
            SkuId = sku.Id,
            RequiredQty = 20,
            IssuedQty = 8,
            ShortageQty = 10,
            ClosedShortageQty = 2,
            Unit = "KG",
            Status = "partially_issued"
        });
        await db.SaveChangesAsync();

        var result = await new WorkOrderService(db).GetAllAsync();

        var material = Assert.Single(Assert.Single(result).Materials);
        Assert.Equal("SKU001", material.SkuNumber);
        Assert.Equal("KG", material.Unit);
        Assert.Equal(20m, material.RequiredQty);
        Assert.Equal(8m, material.IssuedQty);
        Assert.Equal(10m, material.UnissuedQty);
        Assert.Equal(2m, material.ClosedShortageQty);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByStatus()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var skuId = (await db.Skus.FirstAsync()).Id;

        db.WorkOrders.AddRange(
            new WorkOrder { WoNumber = "WO-001", ProductSkuId = skuId, Status = "planned", CreatedAt = DateTime.UtcNow },
            new WorkOrder { WoNumber = "WO-002", ProductSkuId = skuId, Status = "completed", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new WorkOrderService(db);
        var result = await service.GetAllAsync(status: "planned");

        Assert.Single(result);
        Assert.Equal("WO-001", result[0].WoNumber);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var skuId = (await db.Skus.FirstAsync()).Id;

        var wo = new WorkOrder { WoNumber = "WO-001", ProductSkuId = skuId, Status = "planned", PlannedQty = 100, Priority = "high" };
        db.WorkOrders.Add(wo);
        await db.SaveChangesAsync();

        var service = new WorkOrderService(db);
        var dto = await service.GetByIdAsync(wo.Id);

        Assert.NotNull(dto);
        Assert.Equal("WO-001", dto!.WoNumber);
        Assert.Equal("TestSku", dto.ProductSkuName);
    }

    [Fact]
    public async Task GetByIdAsync_UsesSupplyWorkOrderPlannedQuantity()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var skuId = (await db.Skus.FirstAsync()).Id;
        var customer = new Customer { CustomerCode = "CUST-PRODUCTION-QTY", CustomerName = "Production Qty Customer" };
        var salesOrder = new SalesOrder
        {
            OrderNumber = "SO-PRODUCTION-QTY",
            CustomerId = customer.Id,
            Customer = customer,
            Status = "confirmed"
        };
        var salesOrderItem = new SalesOrderItem
        {
            SalesOrder = salesOrder,
            ProductId = skuId,
            SkuId = skuId,
            Quantity = 5000
        };
        var rootWorkOrder = new WorkOrder
        {
            WoNumber = "WO-ROOT-QTY",
            ProductSkuId = skuId,
            SalesOrderId = salesOrder.Id,
            PlannedQty = 5000,
            Status = "planned"
        };
        var parentMaterial = new WorkOrderMaterial
        {
            WorkOrder = rootWorkOrder,
            SkuId = skuId,
            RequiredQty = 416.67m
        };
        var childWorkOrder = new WorkOrder
        {
            WoNumber = "WO-CHILD-QTY",
            ProductSkuId = skuId,
            SalesOrderId = salesOrder.Id,
            PlannedQty = 416.67m,
            Status = "planned",
            Notes = $"由工單 {rootWorkOrder.WoNumber} 自動轉單補貨 [AUTO_SUPPLY_PARENT_MATERIAL:{parentMaterial.Id}]"
        };
        var productionLink = new SalesOrderProductionLink
        {
            SalesOrderItem = salesOrderItem,
            ProductionDocType = "work_order",
            ProductionDocId = rootWorkOrder.Id,
            LinkedQty = salesOrderItem.Quantity
        };
        db.AddRange(
            customer,
            salesOrder,
            salesOrderItem,
            rootWorkOrder,
            parentMaterial,
            childWorkOrder,
            productionLink);
        await db.SaveChangesAsync();

        var dto = await new WorkOrderService(db).GetByIdAsync(childWorkOrder.Id);

        Assert.NotNull(dto);
        Assert.Equal(416.67m, dto!.PlannedQty);
        Assert.Equal(416.67m, dto.ProductionQty);
    }

    [Fact]
    public async Task ReleaseAsync_ChangesPlannedWorkOrderToReleased()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var skuId = (await db.Skus.FirstAsync()).Id;
        var workOrder = new WorkOrder { WoNumber = "WO-RELEASE", ProductSkuId = skuId, Status = "planned" };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var result = await new WorkOrderService(db).ReleaseAsync(workOrder.Id);

        Assert.True(result.found);
        Assert.Equal("ok", result.status);
        Assert.Equal("released", (await db.WorkOrders.FindAsync(workOrder.Id))!.Status);
    }

    [Fact]
    public async Task ReleaseAsync_RejectsWorkOrderOutsidePlannedOrPendingStatus()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var skuId = (await db.Skus.FirstAsync()).Id;
        var workOrder = new WorkOrder { WoNumber = "WO-IN-PROGRESS", ProductSkuId = skuId, Status = "in_progress" };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var result = await new WorkOrderService(db).ReleaseAsync(workOrder.Id);

        Assert.True(result.found);
        Assert.Equal("invalid_transition", result.status);
        Assert.Equal("in_progress", (await db.WorkOrders.FindAsync(workOrder.Id))!.Status);
    }

    [Fact]
    public async Task StartAsync_ChangesReleasedWorkOrderToInProgress()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var skuId = (await db.Skus.FirstAsync()).Id;
        var workOrder = new WorkOrder { WoNumber = "WO-START", ProductSkuId = skuId, Status = "released" };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var result = await new WorkOrderService(db).StartAsync(workOrder.Id);

        Assert.True(result.found);
        Assert.Equal("ok", result.status);
        var updatedWorkOrder = await db.WorkOrders.FindAsync(workOrder.Id);
        Assert.Equal("in_progress", updatedWorkOrder!.Status);
        Assert.NotNull(updatedWorkOrder.ActualStartDate);
    }

    [Fact]
    public async Task StartAsync_RejectsWorkOrderNotReleased()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var skuId = (await db.Skus.FirstAsync()).Id;
        var workOrder = new WorkOrder { WoNumber = "WO-START-PLANNED", ProductSkuId = skuId, Status = "planned" };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var result = await new WorkOrderService(db).StartAsync(workOrder.Id);

        Assert.True(result.found);
        Assert.Equal("invalid_transition", result.status);
        Assert.Equal("planned", (await db.WorkOrders.FindAsync(workOrder.Id))!.Status);
    }

    [Fact]
    public async Task GetByIdAsync_UsesBomUnitForRequirementsAndSkuUnitForInventory()
    {
        using var db = CreateInMemoryDbContext();
        var product = new Sku { SkuNumber = "FG-UNIT", SkuName = "Product", Unit = "PCS" };
        var materialSku = new Sku
        {
            SkuNumber = "RM-UNIT",
            SkuName = "Material",
            Unit = "KG",
            BaseUomCode = "KG",
            Qty = 50
        };
        var gram = new Uom { UomCode = "G", UomName = "Gram", UomType = "weight" };
        var kilogram = new Uom { UomCode = "KG", UomName = "Kilogram", UomType = "weight" };
        db.Uoms.AddRange(gram, kilogram);
        db.UomConversions.Add(new UomConversion
        {
            FromUomId = gram.Id,
            FromUom = gram,
            ToUomId = kilogram.Id,
            ToUom = kilogram,
            Factor = 0.001m,
            Operator = "*",
            IsActive = true
        });
        var bom = new Bom
        {
            BomCode = "BOM-UNIT",
            ProductSkuId = product.Id,
            Status = "active",
            EffectiveDate = DateTime.UtcNow.AddDays(-1)
        };
        bom.Items.Add(new BomItem
        {
            BomId = bom.Id,
            ChildSkuId = materialSku.Id,
            Quantity = 20,
            Unit = "G"
        });
        var workOrder = new WorkOrder
        {
            WoNumber = "WO-UNIT",
            ProductSkuId = product.Id,
            BomId = bom.Id,
            ProductionUomCode = "PIECE",
            PlannedQty = 1000
        };
        workOrder.Materials.Add(new WorkOrderMaterial
        {
            WorkOrderId = workOrder.Id,
            SkuId = materialSku.Id,
            Sku = materialSku,
            BomStandardQty = 20,
            RequiredQty = 20000,
            Status = "shortage"
        });
        db.AddRange(product, materialSku, bom, workOrder);
        await db.SaveChangesAsync();

        var result = await new WorkOrderService(db).GetByIdAsync(workOrder.Id);

        var material = Assert.Single(result!.Materials);
        Assert.Equal("G", material.Unit);
        Assert.Equal("KG", material.InventoryUnit);
        Assert.Equal(20, material.BomStandardQty);
        Assert.Equal(20000, material.RequiredQty);
        Assert.Equal(50, material.AvailableQty);
        Assert.Equal(0, material.ShortageQty);
        Assert.Equal("pending", material.Status);

        var supplyResult = await new WorkOrderService(db).CreateSupplyOrdersAsync(workOrder.Id);

        Assert.Equal("ok", supplyResult.status);
        Assert.Equal(0, supplyResult.result!.PrCount);
        Assert.Equal(0, supplyResult.result.SubWoCount);
        var persistedMaterial = await db.WorkOrderMaterials.SingleAsync();
        Assert.Equal(0, persistedMaterial.ShortageQty);
        Assert.Equal("pending", persistedMaterial.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new WorkOrderService(db);

        Assert.Null(await service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ReorderOperationsAsync_UpdatesSequenceToRequestedOrder()
    {
        using var db = CreateInMemoryDbContext();
        var sku = new Sku { SkuNumber = "SKU-ROUTE", SkuName = "Routed Product" };
        var workOrder = new WorkOrder { WoNumber = "WO-ROUTE", ProductSkuId = sku.Id };
        var inspection = new WorkOrderOperation
        {
            WorkOrderId = workOrder.Id,
            OperationSeq = 1,
            OperationId = Guid.NewGuid(),
            WorkCenterId = Guid.NewGuid()
        };
        var molding = new WorkOrderOperation
        {
            WorkOrderId = workOrder.Id,
            OperationSeq = 2,
            OperationId = Guid.NewGuid(),
            WorkCenterId = Guid.NewGuid()
        };
        workOrder.Operations.Add(inspection);
        workOrder.Operations.Add(molding);
        db.AddRange(sku, workOrder);
        await db.SaveChangesAsync();

        var result = await new WorkOrderService(db)
            .ReorderOperationsAsync(workOrder.Id, new List<Guid> { molding.Id, inspection.Id });

        Assert.True(result.found);
        Assert.Equal("ok", result.status);
        Assert.Equal(1, (await db.WorkOrderOperations.FindAsync(molding.Id))!.OperationSeq);
        Assert.Equal(2, (await db.WorkOrderOperations.FindAsync(inspection.Id))!.OperationSeq);
    }

    [Fact]
    public async Task ReorderOperationsAsync_RejectsIncompleteOrder()
    {
        using var db = CreateInMemoryDbContext();
        var sku = new Sku { SkuNumber = "SKU-ROUTE", SkuName = "Routed Product" };
        var workOrder = new WorkOrder { WoNumber = "WO-ROUTE", ProductSkuId = sku.Id };
        workOrder.Operations.Add(new WorkOrderOperation
        {
            WorkOrderId = workOrder.Id,
            OperationSeq = 1,
            OperationId = Guid.NewGuid(),
            WorkCenterId = Guid.NewGuid()
        });
        db.AddRange(sku, workOrder);
        await db.SaveChangesAsync();

        var result = await new WorkOrderService(db)
            .ReorderOperationsAsync(workOrder.Id, new List<Guid>());

        Assert.True(result.found);
        Assert.Equal("invalid_order", result.status);
        Assert.Equal(1, (await db.WorkOrderOperations.SingleAsync()).OperationSeq);
    }

    [Fact]
    public async Task GetByIdAsync_IncludesWorkOrderOperations()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var skuId = (await db.Skus.FirstAsync()).Id;
        var operation = new Operation
        {
            OperationCode = "OP-001",
            OperationName = "組裝"
        };
        var workCenter = new WorkCenter
        {
            WorkCenterCode = "WC-001",
            WorkCenterName = "組裝中心"
        };
        var workOrder = new WorkOrder
        {
            WoNumber = "WO-DETAIL-001",
            ProductSkuId = skuId,
            PlannedQty = 10
        };
        workOrder.Operations.Add(new WorkOrderOperation
        {
            WorkOrderId = workOrder.Id,
            OperationSeq = 1,
            OperationId = operation.Id,
            Operation = operation,
            WorkCenterId = workCenter.Id,
            WorkCenter = workCenter,
            PlannedQty = 10,
            Status = "pending"
        });
        db.AddRange(workCenter, workOrder);
        await db.SaveChangesAsync();

        var result = await new WorkOrderService(db).GetByIdAsync(workOrder.Id);

        var resultOperation = Assert.Single(Assert.IsType<List<WorkOrderOperationDto>>(result!.Operations));
        Assert.Equal("組裝", resultOperation.OperationName);
        Assert.Equal("組裝中心", resultOperation.WorkCenterName);
        Assert.Equal(10, resultOperation.PlannedQty);
        Assert.Equal("pending", resultOperation.Status);
    }

    [Fact]
    public async Task CreateAsync_ReturnsOk_WhenSkuExists()
    {
        using var db = CreateInMemoryDbContext();
        var productionUom = new Uom { UomCode = "PIECE", UomName = "片" };
        var sku = new Sku
        {
            SkuNumber = "SKU001",
            SkuName = "TestSku",
            ProductionUom = productionUom,
            ProductionUomId = productionUom.Id
        };
        db.AddRange(productionUom, sku);
        await db.SaveChangesAsync();

        var service = new WorkOrderService(db);
        var req = new CreateWorkOrderRequest
        {
            ProductSkuId = sku.Id,
            PlannedQty = 500,
            Priority = "high",
            PlannedStartDate = DateTime.UtcNow,
            PlannedEndDate = DateTime.UtcNow.AddDays(7)
        };

        var (dto, status) = await service.CreateAsync(req);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Equal("planned", dto!.Status);
        Assert.Equal(500, dto.PlannedQty);
        Assert.Equal("PIECE", dto.ProductionUomCode);
    }

    [Fact]
    public async Task CreateAsync_RejectsSkuWithoutProductionUom()
    {
        using var db = CreateInMemoryDbContext();
        var sku = new Sku { SkuNumber = "SKU-NO-UOM", SkuName = "未設定生產單位" };
        db.Skus.Add(sku);
        await db.SaveChangesAsync();

        var (dto, status) = await new WorkOrderService(db).CreateAsync(new CreateWorkOrderRequest
        {
            ProductSkuId = sku.Id,
            PlannedQty = 10
        });

        Assert.Null(dto);
        Assert.Equal("production_uom_not_configured", status);
        Assert.Empty(await db.WorkOrders.ToListAsync());
    }

    [Fact]
    public async Task CreateSupplyOrdersAsync_RecursivelyCreatesSubWorkOrderAndPurchaseRequisition_OnlyOnce()
    {
        using var db = CreateInMemoryDbContext();
        var gram = new Uom { UomCode = "G", UomName = "公克", UomType = "base" };
        var kilogram = new Uom { UomCode = "KG", UomName = "公斤", UomType = "package" };
        var parentSku = new Sku { SkuNumber = "PARENT", SkuName = "Parent", IsManufacturable = true };
        var assemblySku = new Sku
        {
            SkuNumber = "ASSEMBLY",
            SkuName = "Assembly",
            Unit = "PCS",
            ProductionUom = new Uom { UomCode = "PCS", UomName = "片" },
            SourceType = "make",
            MakeOrBuy = "make",
            IsManufacturable = false,
            IsPurchasable = false
        };
        var componentSku = new Sku
        {
            SkuNumber = "COMPONENT",
            SkuName = "Component",
            Unit = "G",
            Qty = 2,
            PurchasePrice = 5,
            PurchaseUom = kilogram,
            PurchaseUomId = kilogram.Id,
            IsPurchasable = true
        };
        var parentOrder = new WorkOrder
        {
            WoNumber = "WO-PARENT",
            ProductSkuId = parentSku.Id,
            ProductionUomCode = "PCS",
            PlannedQty = 1,
            Status = "planned"
        };
        parentOrder.Materials.Add(new WorkOrderMaterial
        {
            WorkOrderId = parentOrder.Id,
            SkuId = assemblySku.Id,
            Sku = assemblySku,
            BomStandardQty = 1,
            RequiredQty = 4,
            ShortageQty = 4,
            Status = "shortage"
        });
        var assemblyBom = new Bom
        {
            BomCode = "BOM-ASSEMBLY",
            ProductSkuId = assemblySku.Id,
            Status = "active",
            EffectiveDate = DateTime.UtcNow.AddDays(-1)
        };
        assemblyBom.Items.Add(new BomItem
        {
            BomId = assemblyBom.Id,
            ChildSkuId = componentSku.Id,
            Quantity = 3,
            Unit = "G"
        });
        db.AddRange(gram, kilogram, parentSku, assemblySku, componentSku, parentOrder, assemblyBom);
        db.UomConversions.Add(new UomConversion
        {
            SkuId = componentSku.Id,
            FromUomId = gram.Id,
            ToUomId = kilogram.Id,
            Factor = 0.001m,
            Operator = "*",
            FromUom = gram,
            ToUom = kilogram
        });
        await db.SaveChangesAsync();

        var service = new WorkOrderService(db);
        var firstResult = await service.CreateSupplyOrdersAsync(parentOrder.Id);

        Assert.True(firstResult.found);
        Assert.Equal("ok", firstResult.status);
        Assert.Equal(1, firstResult.result!.SubWoCount);
        Assert.Equal(1, firstResult.result.PrCount);
        var parentMaterialId = parentOrder.Materials.Single().Id;
        var subWorkOrder = await db.WorkOrders.SingleAsync(order =>
            order.Notes == $"由工單 {parentOrder.WoNumber} 自動轉單補貨 [AUTO_SUPPLY_PARENT_MATERIAL:{parentMaterialId}]");
        Assert.Equal(4, subWorkOrder.PlannedQty);
        Assert.Equal("PCS", subWorkOrder.ProductionUomCode);
        var purchaseRequisition = await db.PurchaseRequisitions
            .Include(requisition => requisition.Items)
            .SingleAsync(requisition => requisition.WorkOrderId == subWorkOrder.Id);
        var purchaseItem = Assert.Single(purchaseRequisition.Items);
        Assert.Equal(componentSku.Id, purchaseItem.SkuId);
        Assert.Equal("KG", purchaseItem.Unit);
        Assert.Equal(0.01m, purchaseItem.Quantity);

        var secondResult = await service.CreateSupplyOrdersAsync(parentOrder.Id);

        Assert.Equal("ok", secondResult.status);
        Assert.Equal(0, secondResult.result!.SubWoCount);
        Assert.Equal(0, secondResult.result.PrCount);
        Assert.Equal(2, await db.WorkOrders.CountAsync());
        Assert.Single(await db.PurchaseRequisitions.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_ReturnsNotFound_WhenSkuDoesNotExist()
    {
        using var db = CreateInMemoryDbContext();
        var service = new WorkOrderService(db);

        var req = new CreateWorkOrderRequest
        {
            ProductSkuId = Guid.NewGuid(),
            PlannedQty = 500
        };

        var (dto, status) = await service.CreateAsync(req);

        Assert.Null(dto);
        Assert.Equal("not_found", status);
    }

    [Fact]
    public async Task CreateFromSalesOrderAsync_CreatesLinkedWorkOrderWithBomAndRouting_AndIsIdempotent()
    {
        using var db = CreateInMemoryDbContext();
        var productUom = new Uom { UomCode = "PIECE", UomName = "片" };
        var product = new Sku
        {
            SkuNumber = "FG-001",
            SkuName = "成品",
            IsManufacturable = true,
            SalesUom = productUom,
            SalesUomId = productUom.Id,
            ProductionUom = productUom,
            ProductionUomId = productUom.Id
        };
        var component = new Sku { SkuNumber = "RM-001", SkuName = "原料" };
        var customer = new Customer { CustomerCode = "CUST-001", CustomerName = "測試客戶" };
        db.AddRange(product, component, customer);
        await db.SaveChangesAsync();

        var order = new SalesOrder
        {
            OrderNumber = "SO-TEST-001",
            CustomerId = customer.Id,
            Status = "confirmed",
            ExpectedDeliveryDate = DateTime.UtcNow.Date.AddDays(5)
        };
        var orderItem = new SalesOrderItem
        {
            SalesOrderId = order.Id,
            LineNumber = 1,
            ProductId = product.Id,
            SkuId = product.Id,
            Quantity = 10,
            NeedManufacturing = false
        };
        order.Items.Add(orderItem);

        var workCenter = new WorkCenter
        {
            WorkCenterCode = "WC-001",
            WorkCenterName = "組裝中心"
        };
        var operation = new Operation
        {
            OperationCode = "OP-001",
            OperationName = "組裝"
        };
        var bom = new Bom
        {
            BomCode = "BOM-001",
            ProductSkuId = product.Id,
            Version = "1.0",
            Status = "active",
            EffectiveDate = DateTime.UtcNow.Date.AddDays(-1)
        };
        bom.Items.Add(new BomItem
        {
            BomId = bom.Id,
            ChildSkuId = component.Id,
            Quantity = 2,
            Unit = "PCS"
        });
        var routing = new Routing
        {
            RoutingCode = "RT-001",
            RoutingName = "成品製程",
            ProductSkuId = product.Id,
            IsActive = true
        };
        routing.Operations.Add(new RoutingOperation
        {
            RoutingId = routing.Id,
            OperationSeq = 1,
            OperationId = operation.Id,
            WorkCenterId = workCenter.Id,
            Operation = operation,
            WorkCenter = workCenter
        });
        db.AddRange(order, bom, workCenter, operation, routing);
        await db.SaveChangesAsync();

        var service = new WorkOrderService(db);
        var (workOrders, status) = await service.CreateFromSalesOrderAsync(order.Id);

        Assert.Equal("ok", status);
        var workOrder = Assert.Single(workOrders);
        Assert.Equal("planned", workOrder.Status);
        Assert.Equal(order.Id, workOrder.SalesOrderId);
        Assert.Equal("SO-TEST-001", workOrder.SalesOrderNumber);
        Assert.Equal(bom.Id, workOrder.BomId);
        Assert.Equal(10, workOrder.PlannedQty);
        Assert.Equal("PIECE", workOrder.ProductionUomCode);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(5), workOrder.PlannedEndDate);
        Assert.Equal(20, await db.WorkOrderMaterials.SumAsync(material => material.RequiredQty));
        Assert.Single(await db.WorkOrderOperations.ToListAsync());
        Assert.Equal("sent", order.ManufacturingStatus);
        Assert.Equal("sent", orderItem.ProductionStatus);

        var (replayedWorkOrders, replayStatus) = await service.CreateFromSalesOrderAsync(order.Id);

        Assert.Equal("already_created", replayStatus);
        Assert.Single(replayedWorkOrders);
        Assert.Single(await db.WorkOrders.ToListAsync());
        Assert.Single(await db.SalesOrderProductionLinks.ToListAsync());
    }

    [Fact]
    public async Task CreateFromSalesOrderAsync_RequiresConfirmedOrder()
    {
        using var db = CreateInMemoryDbContext();
        var customer = new Customer { CustomerCode = "CUST-001", CustomerName = "測試客戶" };
        var order = new SalesOrder
        {
            OrderNumber = "SO-TEST-002",
            CustomerId = customer.Id,
            Status = "pending"
        };
        db.AddRange(customer, order);
        await db.SaveChangesAsync();

        var (workOrders, status) = await new WorkOrderService(db).CreateFromSalesOrderAsync(order.Id);

        Assert.Empty(workOrders);
        Assert.Equal("order_not_confirmed", status);
        Assert.Empty(await db.WorkOrders.ToListAsync());
    }

    [Fact]
    public async Task CreateFromSalesOrderAsync_RejectsMissingBomWithoutCreatingPartialWorkOrders()
    {
        using var db = CreateInMemoryDbContext();
        var product = new Sku { SkuNumber = "FG-002", SkuName = "未設定 BOM 的成品" };
        var customer = new Customer { CustomerCode = "CUST-002", CustomerName = "測試客戶" };
        db.AddRange(product, customer);
        await db.SaveChangesAsync();

        var order = new SalesOrder
        {
            OrderNumber = "SO-TEST-003",
            CustomerId = customer.Id,
            Status = "confirmed"
        };
        order.Items.Add(new SalesOrderItem
        {
            SalesOrderId = order.Id,
            LineNumber = 1,
            ProductId = product.Id,
            SkuId = product.Id,
            Quantity = 3,
            NeedManufacturing = true
        });
        db.SalesOrders.Add(order);
        await db.SaveChangesAsync();

        var (workOrders, status) = await new WorkOrderService(db).CreateFromSalesOrderAsync(order.Id);

        Assert.Empty(workOrders);
        Assert.Equal("bom_not_found:1", status);
        Assert.Empty(await db.WorkOrders.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsOk_WhenWorkOrderExists()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var skuId = (await db.Skus.FirstAsync()).Id;

        var wo = new WorkOrder { WoNumber = "WO-001", ProductSkuId = skuId, Status = "planned", PlannedQty = 100, Priority = "normal" };
        db.WorkOrders.Add(wo);
        await db.SaveChangesAsync();

        var service = new WorkOrderService(db);
        var req = new UpdateWorkOrderRequest { Priority = "high", PlannedQty = 200 };

        var (found, status) = await service.UpdateAsync(wo.Id, req);

        Assert.True(found);
        Assert.Equal("ok", status);
    }

    [Fact]
    public async Task RecalculateMaterialsAsync_UpdatesPlanAndCancelsUnpostedDraft()
    {
        using var db = CreateInMemoryDbContext();
        var product = new Sku { SkuNumber = "FG-RECALC", SkuName = "測試成品" };
        var component = new Sku { SkuNumber = "RM-RECALC", SkuName = "APC", Unit = "G" };
        var bom = new Bom
        {
            BomCode = "BOM-RECALC",
            ProductSkuId = product.Id,
            Version = "1",
            Status = "active",
            EffectiveDate = DateTime.UtcNow.Date.AddDays(-1)
        };
        bom.Items.Add(new BomItem
        {
            BomId = bom.Id,
            ChildSkuId = component.Id,
            Quantity = 10,
            Unit = "G"
        });
        var workOrder = new WorkOrder
        {
            WoNumber = "WO-RECALC",
            ProductSkuId = product.Id,
            BomId = bom.Id,
            BomVersion = bom.Version,
            PlannedQty = 4583.335m,
            Status = "planned"
        };
        workOrder.Materials.Add(new WorkOrderMaterial
        {
            WorkOrderId = workOrder.Id,
            SkuId = component.Id,
            Sku = component,
            BomStandardQty = 10,
            RequiredQty = 45833.35m,
            Unit = "G",
            Status = "pending"
        });
        var draft = new MaterialIssue
        {
            IssueNumber = "MAT-RECALC",
            WorkOrderId = workOrder.Id,
            Status = "draft"
        };
        db.AddRange(product, component, bom, workOrder, draft);
        await db.SaveChangesAsync();

        var status = await new WorkOrderService(db).RecalculateMaterialsAsync(
            [workOrder.Id],
            5000);

        Assert.Equal("ok", status);
        Assert.Equal(5000, workOrder.PlannedQty);
        Assert.Equal(50000, Assert.Single(workOrder.Materials).RequiredQty);
        Assert.Equal("cancelled", draft.Status);
        var replacementDraft = await db.MaterialIssues
            .SingleAsync(issue => issue.WorkOrderId == workOrder.Id && issue.Status == "draft");
        Assert.Equal(50000, Assert.Single(replacementDraft.Items).RequiredQty);
    }

    [Fact]
    public async Task RecalculateMaterialsAsync_RejectsIssuedWorkOrderWithoutChangingIt()
    {
        using var db = CreateInMemoryDbContext();
        var product = new Sku { SkuNumber = "FG-ISSUED", SkuName = "測試成品" };
        var component = new Sku { SkuNumber = "RM-ISSUED", SkuName = "APC", Unit = "G" };
        var bom = new Bom
        {
            BomCode = "BOM-ISSUED",
            ProductSkuId = product.Id,
            Version = "1",
            Status = "active",
            EffectiveDate = DateTime.UtcNow.Date.AddDays(-1)
        };
        bom.Items.Add(new BomItem
        {
            BomId = bom.Id,
            ChildSkuId = component.Id,
            Quantity = 10,
            Unit = "G"
        });
        var workOrder = new WorkOrder
        {
            WoNumber = "WO-ISSUED",
            ProductSkuId = product.Id,
            BomId = bom.Id,
            PlannedQty = 4583.335m,
            Status = "planned"
        };
        workOrder.Materials.Add(new WorkOrderMaterial
        {
            WorkOrderId = workOrder.Id,
            SkuId = component.Id,
            Sku = component,
            BomStandardQty = 10,
            RequiredQty = 45833.35m,
            IssuedQty = 1000,
            Unit = "G",
            Status = "pending"
        });
        db.AddRange(product, component, bom, workOrder);
        await db.SaveChangesAsync();

        var status = await new WorkOrderService(db).RecalculateMaterialsAsync(
            [workOrder.Id],
            5000);

        Assert.Equal("material_issue_history", status);
        Assert.Equal(4583.335m, workOrder.PlannedQty);
        Assert.Equal(45833.35m, Assert.Single(workOrder.Materials).RequiredQty);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenWorkOrderDoesNotExist()
    {
        using var db = CreateInMemoryDbContext();
        var service = new WorkOrderService(db);

        var (found, status) = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(found);
        Assert.Equal("not_found", status);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsGuarded_WhenStatusNotPlanned()
    {
        using var db = CreateInMemoryDbContext();
        await SeedAsync(db);
        var skuId = (await db.Skus.FirstAsync()).Id;

        var wo = new WorkOrder { WoNumber = "WO-001", ProductSkuId = skuId, Status = "completed", PlannedQty = 100, Priority = "normal" };
        db.WorkOrders.Add(wo);
        await db.SaveChangesAsync();

        var service = new WorkOrderService(db);

        var (found, status) = await service.DeleteAsync(wo.Id);

        Assert.True(found);
        Assert.Equal("guarded", status);
    }
}
