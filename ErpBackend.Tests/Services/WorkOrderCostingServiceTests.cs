using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.QA;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class WorkOrderCostingServiceTests
{
    [Fact]
    public async Task GetCostAnalysisAsync_UsesWorkOrderMaterialsAndRoutingRates()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: true);
        var service = CreateService(db);

        var result = await service.GetCostAnalysisAsync(seeded.WorkOrder.Id);

        Assert.NotNull(result);
        Assert.Equal(40m, result!.StandardMaterialCost);
        Assert.Equal(60m, result.StandardLaborCost);
        Assert.Equal(30m, result.StandardOverheadCost);
        Assert.Equal(130m, result.TotalStandardCost);
        Assert.Equal(334m, result.TotalActualCost);
        Assert.Equal(20m, result.ActualMachineCost);
        Assert.Equal(24m, result.ActualOverheadCost);
        Assert.Equal(204m, result.VarianceAmount);
        Assert.Equal(156.92m, result.VariancePercentage);
        Assert.Equal(33.4m, result.UnitCost);
        Assert.True(result.HasActualCost);
    }

    [Fact]
    public async Task GetCostAnalysisAsync_UsesActiveProductRoutingWhenWorkOrderOperationsHaveNotBeenExpanded()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: false);
        var workOrder = await db.WorkOrders
            .Include(order => order.Operations)
            .SingleAsync(order => order.Id == seeded.WorkOrder.Id);
        var operation = await db.Operations.SingleAsync();
        var workCenter = await db.WorkCenters.SingleAsync();
        db.WorkOrderOperations.RemoveRange(workOrder.Operations);
        workOrder.Operations.Clear();
        var routing = new Routing
        {
            RoutingCode = $"ROUTE-{Guid.NewGuid():N}",
            RoutingName = "Test routing",
            ProductSkuId = workOrder.ProductSkuId,
            IsActive = true,
            Version = "1.0"
        };
        routing.Operations.Add(new RoutingOperation
        {
            RoutingId = routing.Id,
            Routing = routing,
            OperationSeq = 1,
            OperationId = operation.Id,
            Operation = operation,
            WorkCenterId = workCenter.Id,
            WorkCenter = workCenter
        });
        db.Routings.Add(routing);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetCostAnalysisAsync(workOrder.Id);

        Assert.NotNull(result);
        Assert.Equal(60m, result!.StandardLaborCost);
        Assert.Equal(30m, result.StandardOverheadCost);
        Assert.Equal(130m, result.TotalStandardCost);
    }

    [Fact]
    public async Task GetCostAnalysisAsync_ScalesStandardCostsToActualOutputQuantity()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: true, actualQty: 5);
        var service = CreateService(db);

        var result = await service.GetCostAnalysisAsync(seeded.WorkOrder.Id);

        Assert.NotNull(result);
        Assert.Equal(20m, result!.StandardMaterialCost);
        Assert.Equal(35m, result.StandardLaborCost);
        Assert.Equal(17.5m, result.StandardOverheadCost);
        Assert.Equal(72.5m, result.TotalStandardCost);
    }

    [Fact]
    public async Task GetCostAnalysisAsync_DoesNotCalculateVarianceWithoutOutputOrActualCost()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: false, actualQty: 0);
        var service = CreateService(db);

        var result = await service.GetCostAnalysisAsync(seeded.WorkOrder.Id);

        Assert.NotNull(result);
        Assert.False(result!.HasActualCost);
        Assert.Null(result.VarianceAmount);
        Assert.Null(result.VariancePercentage);
        Assert.Null(result.UnitCost);
        Assert.Equal(130m, result.TotalStandardCost);
    }

    [Fact]
    public async Task GetCostAnalysisAsync_DoesNotCalculateVarianceWhenOutputExistsButActualCostIsZero()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: false, actualQty: 10);
        var service = CreateService(db);

        var result = await service.GetCostAnalysisAsync(seeded.WorkOrder.Id);

        Assert.NotNull(result);
        Assert.False(result!.HasActualCost);
        Assert.Null(result.VarianceAmount);
        Assert.Null(result.VariancePercentage);
        Assert.Null(result.UnitCost);
    }

    [Fact]
    public async Task GetCostingSummaryAsync_IncludesCompletedAndSettledRegardlessOfStatusCasing()
    {
        using var db = CreateInMemoryDbContext();
        var completed = await SeedWorkOrderAsync(db, includeActualCosts: true);
        var settled = await SeedWorkOrderAsync(db, includeActualCosts: true, status: "SETTLED");
        var service = CreateService(db);

        var result = await service.GetCostingSummaryAsync();

        Assert.Contains(result, item => item.Id == completed.WorkOrder.Id);
        Assert.Contains(result, item => item.Id == settled.WorkOrder.Id);
        Assert.All(result, item => Assert.Equal(130m, item.TotalStandardCost));
    }

    [Fact]
    public async Task SettleWorkOrderCostAsync_WritesSnapshotAndBalancedAdjustmentVoucher()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: true);
        seeded.WorkOrder.ReceivedQty = seeded.WorkOrder.ActualQty;
        var fqcLot = new FqcInspectionLot
        {
            InspectionNumber = "FQC-COST-001",
            WorkOrderId = seeded.WorkOrder.Id,
            ProductSkuId = seeded.WorkOrder.ProductSkuId,
            LotNumber = "FG-COST-001",
            ProducedQty = seeded.WorkOrder.ActualQty,
            ApprovedQty = seeded.WorkOrder.ActualQty,
            InspectionStatus = "Passed",
            DispositionStatus = "Released",
            InspectorName = "QA"
        };
        var productionReceipt = new ProductionReceipt
        {
            ReceiptNumber = "MGR-COST-001",
            WorkOrderId = seeded.WorkOrder.Id
        };
        db.FqcInspectionLots.Add(fqcLot);
        db.ProductionReceipts.Add(productionReceipt);
        db.ProductionReceiptItems.Add(new ProductionReceiptItem
        {
            ProductionReceiptId = productionReceipt.Id,
            FqcInspectionLotId = fqcLot.Id,
            SkuId = seeded.WorkOrder.ProductSkuId,
            WarehouseId = Guid.NewGuid(),
            BinLocationId = Guid.NewGuid(),
            Quantity = seeded.WorkOrder.ActualQty,
            UnitCost = 5,
            TotalCost = 50
        });
        var finishedGoods = new GLAccount { AccountCode = "1205", AccountName = "存貨-成品" };
        var workInProgress = new GLAccount { AccountCode = "1206", AccountName = "在製品" };
        var productionVariance = new GLAccount
        {
            AccountCode = "5401",
            AccountName = "製造差異",
            AccountType = "Expense"
        };
        db.GLAccounts.AddRange(finishedGoods, workInProgress, productionVariance);
        db.GLAccountMappings.AddRange(
            new GLAccountMapping { MappingKey = "finished_goods_inventory", GLAccount = finishedGoods },
            new GLAccountMapping { MappingKey = "work_in_progress", GLAccount = workInProgress },
            new GLAccountMapping { MappingKey = "production_variance", GLAccount = productionVariance });
        db.AccountingVouchers.Add(new AccountingVoucher
        {
            VoucherNumber = "PRODUCTION-RECEIPT",
            ReferenceType = "WorkOrder",
            ReferenceId = seeded.WorkOrder.Id,
            PostingPurpose = "production_receipt",
            Status = "Posted",
            Entries =
            {
                new AccountingEntry
                {
                    GLAccountId = finishedGoods.Id,
                    DebitAmount = 50,
                    CreditAmount = 0
                },
                new AccountingEntry
                {
                    GLAccountId = workInProgress.Id,
                    DebitAmount = 0,
                    CreditAmount = 50
                }
            }
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var varianceService = CreateVarianceService(db);
        var laborActual = await db.WorkOrderLaborActuals.SingleAsync();
        var allocation = await varianceService.CreateAsync(
            seeded.WorkOrder.Id,
            new CreateWorkOrderCostVarianceAllocationRequestDto
            {
                SourceType = "labor",
                SourceId = laborActual.Id,
                Amount = 25m,
                ReasonCode = "production_anomaly",
                Reason = "產線異常停機"
            },
            Guid.NewGuid(),
            CancellationToken.None);
        await varianceService.ApproveAsync(
            seeded.WorkOrder.Id,
            allocation.Id,
            new ApproveWorkOrderCostVarianceAllocationRequestDto
            {
                ApprovalRole = "DepartmentManager"
            },
            Guid.NewGuid(),
            ["DepartmentManager"],
            CancellationToken.None);

        var firstResult = await service.SettleWorkOrderCostAsync(seeded.WorkOrder.Id);
        var secondResult = await service.SettleWorkOrderCostAsync(seeded.WorkOrder.Id);
        var snapshot = await db.WorkOrderCosts.SingleAsync();
        var settlementVoucher = await db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .SingleAsync(voucher => voucher.ReferenceType == "WorkOrderCostSettlement");

        Assert.Equal("ok", firstResult.status);
        Assert.Equal("already_settled", secondResult.status);
        Assert.Equal("settled", (await db.WorkOrders.SingleAsync()).Status);
        Assert.Equal(334m, snapshot.TotalActualCost);
        Assert.Equal(20m, snapshot.MachineActualCost);
        Assert.Equal(4m, snapshot.OverheadActualCost);
        Assert.Equal(130m, snapshot.TotalStandardCost);
        Assert.Equal(40m, snapshot.StandardMaterialCost);
        Assert.Equal(60m, snapshot.StandardLaborCost);
        Assert.Equal(30m, snapshot.StandardOverheadCost);
        Assert.Equal(204m, snapshot.VarianceAmount);
        Assert.Equal(25m, snapshot.AbnormalCost);
        Assert.Equal(309m, snapshot.CapitalizedActualCost);
        Assert.Equal(284m, settlementVoucher.Entries.Sum(entry => entry.DebitAmount));
        Assert.Equal(284m, settlementVoucher.Entries.Sum(entry => entry.CreditAmount));
        Assert.Equal(259m, settlementVoucher.Entries.Single(entry =>
            entry.GLAccountId == finishedGoods.Id).DebitAmount);
        Assert.Equal(284m, settlementVoucher.Entries.Single(entry =>
            entry.GLAccountId == workInProgress.Id).CreditAmount);
        Assert.Contains(settlementVoucher.Entries, entry =>
            entry.GLAccountId == productionVariance.Id && entry.DebitAmount == 25m);

        seeded.Component.StandardCost = 99;
        await db.SaveChangesAsync();
        var historicalResult = await service.GetCostAnalysisAsync(seeded.WorkOrder.Id);
        Assert.NotNull(historicalResult);
        Assert.Equal(130m, historicalResult!.TotalStandardCost);
        Assert.Equal(25m, historicalResult.AbnormalCost);
        Assert.Equal(309m, historicalResult.CapitalizedActualCost);
    }

    [Fact]
    public async Task SettleWorkOrderCostAsync_RejectsPendingVarianceAllocations()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: true);
        var laborActual = await db.WorkOrderLaborActuals.SingleAsync();
        var service = CreateService(db);
        var varianceService = CreateVarianceService(db);

        await varianceService.CreateAsync(
            seeded.WorkOrder.Id,
            new CreateWorkOrderCostVarianceAllocationRequestDto
            {
                SourceType = "labor",
                SourceId = laborActual.Id,
                Amount = 10m,
                ReasonCode = "engineering_change",
                Reason = "核准中工程變更"
            },
            Guid.NewGuid(),
            CancellationToken.None);

        var result = await service.SettleWorkOrderCostAsync(seeded.WorkOrder.Id);

        Assert.Equal("variance_approval_pending", result.status);
        Assert.Equal("Completed", (await db.WorkOrders.SingleAsync()).Status);
        Assert.Empty(await db.WorkOrderCosts.ToListAsync());
    }

    [Fact]
    public async Task CreateVarianceAllocationAsync_RejectsAllocationAboveSourceCost()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: true);
        var laborActual = await db.WorkOrderLaborActuals.SingleAsync();
        var service = CreateVarianceService(db);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            seeded.WorkOrder.Id,
            new CreateWorkOrderCostVarianceAllocationRequestDto
            {
                SourceType = "labor",
                SourceId = laborActual.Id,
                Amount = laborActual.TotalAmount + 1m,
                ReasonCode = "production_anomaly",
                Reason = "超過來源成本"
            },
            Guid.NewGuid(),
            CancellationToken.None));
    }

    [Fact]
    public async Task ApproveVarianceAllocationAsync_LargeAmountRequiresDistinctCfoAndGeneralManager()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: true);
        var materialActual = await db.WorkOrderMaterialActuals.SingleAsync();
        materialActual.TotalAmount = 100_000m;
        await db.SaveChangesAsync();
        var service = CreateVarianceService(db);
        var creatorId = Guid.NewGuid();
        var allocation = await service.CreateAsync(
            seeded.WorkOrder.Id,
            new CreateWorkOrderCostVarianceAllocationRequestDto
            {
                SourceType = "material",
                SourceId = materialActual.Id,
                Amount = 60_000m,
                ReasonCode = "engineering_change",
                Reason = "工程變更造成材料報廢"
            },
            creatorId,
            CancellationToken.None);

        var cfoId = Guid.NewGuid();
        var firstApproval = await service.ApproveAsync(
            seeded.WorkOrder.Id,
            allocation.Id,
            new ApproveWorkOrderCostVarianceAllocationRequestDto { ApprovalRole = "CFO" },
            cfoId,
            ["CFO"],
            CancellationToken.None);
        Assert.Equal("PendingApproval", firstApproval.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(
            seeded.WorkOrder.Id,
            allocation.Id,
            new ApproveWorkOrderCostVarianceAllocationRequestDto { ApprovalRole = "GeneralManager" },
            cfoId,
            ["GeneralManager"],
            CancellationToken.None));

        var finalApproval = await service.ApproveAsync(
            seeded.WorkOrder.Id,
            allocation.Id,
            new ApproveWorkOrderCostVarianceAllocationRequestDto { ApprovalRole = "GeneralManager" },
            Guid.NewGuid(),
            ["GeneralManager"],
            CancellationToken.None);

        Assert.Equal("Approved", finalApproval.Status);
        Assert.Equal(2, finalApproval.Approvals.Count);
    }

    [Fact]
    public async Task ApproveVarianceAllocationAsync_UsesWorkOrderTotalToPreventSplittingApprovalThreshold()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: true);
        var materialActual = await db.WorkOrderMaterialActuals.SingleAsync();
        materialActual.TotalAmount = 100_000m;
        await db.SaveChangesAsync();
        var service = CreateVarianceService(db);
        var creatorId = Guid.NewGuid();
        var request = new CreateWorkOrderCostVarianceAllocationRequestDto
        {
            SourceType = "material",
            SourceId = materialActual.Id,
            Amount = 30_000m,
            ReasonCode = "engineering_change",
            Reason = "工程變更異常成本"
        };
        var first = await service.CreateAsync(
            seeded.WorkOrder.Id, request, creatorId, CancellationToken.None);
        await service.CreateAsync(
            seeded.WorkOrder.Id,
            new CreateWorkOrderCostVarianceAllocationRequestDto
            {
                SourceType = request.SourceType,
                SourceId = request.SourceId,
                Amount = 30_000m,
                ReasonCode = request.ReasonCode,
                Reason = request.Reason
            },
            creatorId,
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(
            seeded.WorkOrder.Id,
            first.Id,
            new ApproveWorkOrderCostVarianceAllocationRequestDto { ApprovalRole = "DepartmentManager" },
            Guid.NewGuid(),
            ["DepartmentManager"],
            CancellationToken.None));
    }

    [Fact]
    public async Task SettleWorkOrderCostAsync_RejectsWorkOrderWithoutActualCost()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: false);
        var service = CreateService(db);

        var result = await service.SettleWorkOrderCostAsync(seeded.WorkOrder.Id);

        Assert.Equal("no_actual_cost", result.status);
        Assert.Equal("Completed", (await db.WorkOrders.SingleAsync()).Status);
        Assert.Empty(await db.WorkOrderCosts.ToListAsync());
        Assert.Empty(await db.AccountingVouchers.ToListAsync());
    }

    [Fact]
    public async Task CostSettlementVoucher_RejectsPostingIntoClosedAccountingPeriod()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: false);
        var finishedGoods = new GLAccount { AccountCode = "1205", AccountName = "存貨-成品" };
        var workInProgress = new GLAccount { AccountCode = "1206", AccountName = "在製品" };
        db.GLAccounts.AddRange(finishedGoods, workInProgress);
        db.GLAccountMappings.AddRange(
            new GLAccountMapping { MappingKey = "finished_goods_inventory", GLAccount = finishedGoods },
            new GLAccountMapping { MappingKey = "work_in_progress", GLAccount = workInProgress });
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.AccountingPeriods.Add(new AccountingPeriod
        {
            PeriodCode = $"CLOSED-{Guid.NewGuid():N}"[..18],
            StartDate = today,
            EndDate = today,
            IsClosed = true
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new AccountingService(db).CreateVoucherForWorkOrderCostSettlementAsync(
                seeded.WorkOrder.Id,
                50m,
                Array.Empty<WorkOrderCostVarianceAllocation>()));
        Assert.Empty(await db.AccountingVouchers.ToListAsync());
    }

    [Fact]
    public async Task SettleWorkOrderCostAsync_RequiresFqcReleaseAndPostedProductionReceipt()
    {
        using var db = CreateInMemoryDbContext();
        var seeded = await SeedWorkOrderAsync(db, includeActualCosts: true);
        var service = CreateService(db);

        var result = await service.SettleWorkOrderCostAsync(seeded.WorkOrder.Id);

        Assert.Equal("receipt_pending", result.status);
        Assert.Equal("Completed", (await db.WorkOrders.SingleAsync()).Status);
        Assert.Empty(await db.WorkOrderCosts.ToListAsync());
        Assert.Empty(await db.AccountingVouchers.ToListAsync());
    }

    private static ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static WorkOrderCostingService CreateService(ErpGeneralDbContext db)
        => new(db, new AccountingService(db), new UomConversionService(db));

    private static WorkOrderCostVarianceService CreateVarianceService(ErpGeneralDbContext db)
        => new(db, Microsoft.Extensions.Options.Options.Create(new AccountingPolicyOptions()));

    private static async Task<(WorkOrder WorkOrder, Sku Component)> SeedWorkOrderAsync(
        ErpGeneralDbContext db,
        bool includeActualCosts,
        decimal actualQty = 10,
        string status = "Completed")
    {
        var product = new Sku
        {
            SkuNumber = $"PRODUCT-{Guid.NewGuid():N}",
            SkuName = "Test product",
            Unit = "ea"
        };
        var component = new Sku
        {
            SkuNumber = $"COMPONENT-{Guid.NewGuid():N}",
            SkuName = "Test component",
            Unit = "ea",
            StandardCost = 2
        };
        var operation = new Operation
        {
            OperationCode = $"OP-{Guid.NewGuid():N}",
            OperationName = "Assembly",
            SetupTime = 1,
            RunTime = 0.5m,
            CostPerHour = 10
        };
        var workCenter = new WorkCenter
        {
            WorkCenterCode = $"WC-{Guid.NewGuid():N}",
            WorkCenterName = "Assembly center"
        };
        var workOrder = new WorkOrder
        {
            WoNumber = $"WO-{Guid.NewGuid():N}",
            ProductSkuId = product.Id,
            ProductSku = product,
            PlannedQty = 10,
            ActualQty = actualQty,
            Status = status,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            Materials =
            {
                new WorkOrderMaterial
                {
                    SkuId = component.Id,
                    Sku = component,
                    RequiredQty = 20,
                    Unit = "ea"
                }
            },
            Operations =
            {
                new WorkOrderOperation
                {
                    OperationSeq = 1,
                    OperationId = operation.Id,
                    Operation = operation,
                    WorkCenterId = workCenter.Id,
                    WorkCenter = workCenter,
                    PlannedQty = 10,
                    Status = "pending"
                }
            }
        };

        db.WorkOrders.Add(workOrder);
        db.WorkCenterCosts.Add(new WorkCenterCost
        {
            WorkCenterId = workCenter.Id,
            WorkCenter = workCenter,
            LaborRate = 8,
            OverheadRate = 2,
            MachineRate = 3,
            EffectiveFrom = DateTime.UtcNow.AddYears(-1)
        });
        if (includeActualCosts)
        {
            db.WorkOrderMaterialActuals.Add(new WorkOrderMaterialActual
            {
                WorkOrderId = workOrder.Id,
                SkuId = component.Id,
                Sku = component,
                Quantity = 10,
                UnitCost = 25,
                TotalAmount = 250
            });
            db.WorkOrderLaborActuals.Add(new WorkOrderLaborActual
            {
                WorkOrderId = workOrder.Id,
                ActualHours = 3,
                HourlyRate = 20,
                TotalAmount = 60
            });
            db.WorkOrderMachineActuals.Add(new WorkOrderMachineActual
            {
                WorkOrderId = workOrder.Id,
                WorkCenterId = workCenter.Id,
                WorkCenter = workCenter,
                ActualHours = 2,
                MachineRate = 10,
                OverheadRate = 2,
                OverheadAmount = 4,
                TotalAmount = 20
            });
        }

        await db.SaveChangesAsync();
        return (workOrder, component);
    }
}
