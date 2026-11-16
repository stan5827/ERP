using ErpBackend.Data;
using ErpBackend.Models.Finance;
using ErpBackend.Models.MES;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ErpBackend.Tests.Services;

public class ManufacturingCostAllocationServiceTests
{
    [Fact]
    public async Task CloseAccountingPeriod_AllocatesZeroLaborByOutputAndIsIdempotent()
    {
        using var db = CreateInMemoryDbContext();
        var fixture = await SeedAsync(db, "5122", includeOutput: true);

        var (closed, status) = await new FinanceService(db).CloseAccountingPeriodAsync(fixture.Period.Id);
        var (closedAgain, repeatedStatus) =
            await new FinanceService(db).CloseAccountingPeriodAsync(fixture.Period.Id);

        Assert.True(closed);
        Assert.Equal("ok", status);
        Assert.False(closedAgain);
        Assert.Equal("already_closed", repeatedStatus);
        Assert.Equal(1, await db.AccountingVouchers.CountAsync(
            voucher => voucher.ReferenceType == "ManufacturingCostAllocation"));
        var allocationVoucher = await db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .SingleAsync(voucher => voucher.ReferenceType == "ManufacturingCostAllocation");
        Assert.Equal(100m, allocationVoucher.Entries.Sum(entry => entry.CreditAmount));
        Assert.Equal(100m, allocationVoucher.Entries.Sum(entry => entry.DebitAmount));
        Assert.Equal(40m, allocationVoucher.Entries.Single(entry =>
            entry.WorkOrderId == fixture.WorkOrders[0].Id
            && entry.GLAccountId == fixture.WipAccount.Id).DebitAmount);
        Assert.Equal(60m, allocationVoucher.Entries.Single(entry =>
            entry.WorkOrderId == fixture.WorkOrders[1].Id
            && entry.GLAccountId == fixture.WipAccount.Id).DebitAmount);
    }

    [Fact]
    public async Task CloseAccountingPeriod_UsesEqualSharesWhenLaborAndOutputAreZero()
    {
        using var db = CreateInMemoryDbContext();
        var fixture = await SeedAsync(db, "5122", includeOutput: false);

        var (closed, status) = await new FinanceService(db).CloseAccountingPeriodAsync(fixture.Period.Id);

        Assert.True(closed);
        Assert.Equal("ok", status);
        var allocationVoucher = await db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .SingleAsync(voucher => voucher.ReferenceType == "ManufacturingCostAllocation");
        Assert.Equal(50m, allocationVoucher.Entries.Single(entry =>
            entry.WorkOrderId == fixture.WorkOrders[0].Id
            && entry.GLAccountId == fixture.WipAccount.Id).DebitAmount);
        Assert.Equal(50m, allocationVoucher.Entries.Single(entry =>
            entry.WorkOrderId == fixture.WorkOrders[1].Id
            && entry.GLAccountId == fixture.WipAccount.Id).DebitAmount);
    }

    [Fact]
    public async Task CloseAccountingPeriod_Allocates5123ByCostCenterAreaThenMachineHours()
    {
        using var db = CreateInMemoryDbContext();
        var fixture = await SeedAsync(db, "5123", includeOutput: true);
        var firstWorkCenter = await db.WorkCenters.SingleAsync();
        var secondWorkCenter = new WorkCenter
        {
            WorkCenterCode = $"WC-{Guid.NewGuid():N}",
            WorkCenterName = "第二產線"
        };
        var secondCostCenter = new CostCenter
        {
            CostCenterCode = $"CC-{Guid.NewGuid():N}"[..20],
            CostCenterName = "第二產線",
            CostCenterType = "production",
            WorkCenterId = secondWorkCenter.Id,
            EffectiveFrom = fixture.Period.StartDate.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            AreaSquareMeters = 300
        };
        var additionalWorkOrders = new[]
        {
            new WorkOrder
            {
                WoNumber = $"WO-{Guid.NewGuid():N}"[..30],
                ProductSkuId = Guid.NewGuid(),
                Status = "completed"
            },
            new WorkOrder
            {
                WoNumber = $"WO-{Guid.NewGuid():N}"[..30],
                ProductSkuId = Guid.NewGuid(),
                Status = "completed"
            }
        };
        var operationId = await db.Operations.Select(operation => operation.Id).SingleAsync();
        var secondOriginalOperation = await db.WorkOrderOperations
            .SingleAsync(operation => operation.WorkOrderId == fixture.WorkOrders[1].Id);
        secondOriginalOperation.WorkCenterId = secondWorkCenter.Id;

        db.WorkCenters.Add(secondWorkCenter);
        db.CostCenters.Add(secondCostCenter);
        db.WorkOrders.AddRange(additionalWorkOrders);
        db.WorkOrderOperations.AddRange(
            new WorkOrderOperation
            {
                WorkOrderId = additionalWorkOrders[0].Id,
                OperationSeq = 1,
                OperationId = operationId,
                WorkCenterId = firstWorkCenter.Id
            },
            new WorkOrderOperation
            {
                WorkOrderId = additionalWorkOrders[1].Id,
                OperationSeq = 1,
                OperationId = operationId,
                WorkCenterId = secondWorkCenter.Id
            });
        var actualDate = fixture.Period.StartDate.AddDays(10)
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        db.WorkOrderMachineActuals.AddRange(
            new WorkOrderMachineActual
            {
                WorkOrderId = fixture.WorkOrders[0].Id,
                WorkCenterId = firstWorkCenter.Id,
                OperationSeq = 1,
                ActualHours = 1,
                CreatedAt = actualDate
            },
            new WorkOrderMachineActual
            {
                WorkOrderId = additionalWorkOrders[0].Id,
                WorkCenterId = firstWorkCenter.Id,
                OperationSeq = 1,
                ActualHours = 3,
                CreatedAt = actualDate
            },
            new WorkOrderMachineActual
            {
                WorkOrderId = fixture.WorkOrders[1].Id,
                WorkCenterId = secondWorkCenter.Id,
                OperationSeq = 1,
                ActualHours = 3,
                CreatedAt = actualDate
            },
            new WorkOrderMachineActual
            {
                WorkOrderId = additionalWorkOrders[1].Id,
                WorkCenterId = secondWorkCenter.Id,
                OperationSeq = 1,
                ActualHours = 1,
                CreatedAt = actualDate
            });
        await db.SaveChangesAsync();

        var (closed, status) = await new FinanceService(db).CloseAccountingPeriodAsync(fixture.Period.Id);

        Assert.True(closed);
        Assert.Equal("ok", status);
        var allocationVoucher = await db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .SingleAsync(voucher => voucher.ReferenceType == "ManufacturingCostAllocation");
        Assert.Equal(6.25m, allocationVoucher.Entries.Single(entry =>
            entry.WorkOrderId == fixture.WorkOrders[0].Id
            && entry.GLAccountId == fixture.WipAccount.Id).DebitAmount);
        Assert.Equal(18.75m, allocationVoucher.Entries.Single(entry =>
            entry.WorkOrderId == additionalWorkOrders[0].Id
            && entry.GLAccountId == fixture.WipAccount.Id).DebitAmount);
        Assert.Equal(56.25m, allocationVoucher.Entries.Single(entry =>
            entry.WorkOrderId == fixture.WorkOrders[1].Id
            && entry.GLAccountId == fixture.WipAccount.Id).DebitAmount);
        Assert.Equal(18.75m, allocationVoucher.Entries.Single(entry =>
            entry.WorkOrderId == additionalWorkOrders[1].Id
            && entry.GLAccountId == fixture.WipAccount.Id).DebitAmount);
    }

    [Fact]
    public async Task CloseAccountingPeriod_BlocksWhenSelectedDriverIsMissing()
    {
        using var db = CreateInMemoryDbContext();
        var fixture = await SeedAsync(db, "5124", includeOutput: true);

        var (closed, status) = await new FinanceService(db).CloseAccountingPeriodAsync(fixture.Period.Id);

        Assert.False(closed);
        Assert.StartsWith("allocation_blocked:", status, StringComparison.Ordinal);
        Assert.False((await db.AccountingPeriods.SingleAsync()).IsClosed);
        Assert.DoesNotContain(await db.AccountingVouchers.ToListAsync(),
            voucher => voucher.ReferenceType == "ManufacturingCostAllocation");
    }

    [Fact]
    public async Task CloseAccountingPeriod_BlocksAndListsWorkOrderWhenMachineHoursArePartiallyMissing()
    {
        using var db = CreateInMemoryDbContext();
        var fixture = await SeedAsync(db, "5124", includeOutput: true);
        var workCenterId = await db.WorkCenters.Select(center => center.Id).SingleAsync();
        db.WorkOrderMachineActuals.Add(new WorkOrderMachineActual
        {
            WorkOrderId = fixture.WorkOrders[0].Id,
            WorkCenterId = workCenterId,
            OperationSeq = 1,
            ActualHours = 5m,
            CreatedAt = fixture.Period.StartDate.AddDays(10)
                .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var (closed, status) = await new FinanceService(db).CloseAccountingPeriodAsync(fixture.Period.Id);

        Assert.False(closed, $"月結未被阻擋；實際狀態為：{status}");
        Assert.Contains(fixture.WorkOrders[1].WoNumber, status, StringComparison.Ordinal);
        Assert.False((await db.AccountingPeriods.SingleAsync()).IsClosed);
        Assert.DoesNotContain(await db.AccountingVouchers.ToListAsync(),
            voucher => voucher.ReferenceType == "ManufacturingCostAllocation");
    }

    [Fact]
    public async Task CloseAccountingPeriod_BlocksPostingToManufacturingOverheadSummaryAccount()
    {
        using var db = CreateInMemoryDbContext();
        var fixture = await SeedAsync(db, "5121", includeOutput: true);

        var (closed, status) = await new FinanceService(db).CloseAccountingPeriodAsync(fixture.Period.Id);

        Assert.False(closed);
        Assert.Contains("5121", status, StringComparison.Ordinal);
        Assert.False((await db.AccountingPeriods.SingleAsync()).IsClosed);
    }

    private static async Task<Fixture> SeedAsync(
        ErpGeneralDbContext db,
        string expenseAccountCode,
        bool includeOutput)
    {
        var start = new DateOnly(2091, 4, 1);
        var period = new AccountingPeriod
        {
            PeriodCode = $"P-{Guid.NewGuid():N}"[..30],
            StartDate = start,
            EndDate = new DateOnly(2091, 4, 30)
        };
        var expense = new GLAccount
        {
            AccountCode = expenseAccountCode,
            AccountName = "製造費用",
            AccountType = "Expense"
        };
        var balancing = new GLAccount
        {
            AccountCode = $"2{Guid.NewGuid():N}"[..20],
            AccountName = "應付帳款",
            AccountType = "Liability"
        };
        var wip = new GLAccount { AccountCode = "1221", AccountName = "在製品", AccountType = "Asset" };
        var finishedGoods = new GLAccount { AccountCode = "1241", AccountName = "製成品", AccountType = "Asset" };
        var cogs = new GLAccount { AccountCode = "5311", AccountName = "銷貨成本", AccountType = "Expense" };
        db.GLAccounts.AddRange(expense, balancing, wip, finishedGoods, cogs);
        db.GLAccountMappings.AddRange(
            new GLAccountMapping { MappingKey = "work_in_progress", GLAccountId = wip.Id },
            new GLAccountMapping { MappingKey = "finished_goods_inventory", GLAccountId = finishedGoods.Id },
            new GLAccountMapping { MappingKey = "sales_cogs", GLAccountId = cogs.Id });

        var workCenter = new WorkCenter
        {
            WorkCenterCode = $"WC-{Guid.NewGuid():N}",
            WorkCenterName = "自動產線"
        };
        var costCenter = new CostCenter
        {
            CostCenterCode = $"CC-{Guid.NewGuid():N}"[..20],
            CostCenterName = "自動產線",
            CostCenterType = "production",
            WorkCenterId = workCenter.Id,
            EffectiveFrom = start.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            AreaSquareMeters = 100
        };
        var operation = new Operation
        {
            OperationCode = $"OP-{Guid.NewGuid():N}"[..20],
            OperationName = "加工"
        };
        var workOrders = new[]
        {
            new WorkOrder
            {
                WoNumber = $"WO-{Guid.NewGuid():N}"[..30],
                ProductSkuId = Guid.NewGuid(),
                ActualQty = includeOutput ? 4 : 0,
                Status = "completed"
            },
            new WorkOrder
            {
                WoNumber = $"WO-{Guid.NewGuid():N}"[..30],
                ProductSkuId = Guid.NewGuid(),
                ActualQty = includeOutput ? 6 : 0,
                Status = "completed"
            }
        };
        db.WorkCenters.Add(workCenter);
        db.CostCenters.Add(costCenter);
        db.Operations.Add(operation);
        db.WorkOrders.AddRange(workOrders);
        foreach (var workOrder in workOrders)
        {
            db.WorkOrderOperations.Add(new WorkOrderOperation
            {
                WorkOrderId = workOrder.Id,
                OperationSeq = 1,
                OperationId = operation.Id,
                WorkCenterId = workCenter.Id
            });
            db.WorkOrderLaborActuals.Add(new WorkOrderLaborActual
            {
                WorkOrderId = workOrder.Id,
                OperationSeq = 1,
                ActualHours = 0,
                CreatedAt = start.AddDays(10).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            });
            if (includeOutput)
            {
                db.WipReports.Add(new WipReport
                {
                    ReportNumber = $"WR-{Guid.NewGuid():N}"[..30],
                    WorkOrderId = workOrder.Id,
                    OperationSeq = 1,
                    EmployeeId = Guid.NewGuid(),
                    CompletedQty = workOrder.ActualQty,
                    CreatedAt = start.AddDays(10).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    Status = "approved"
                });
            }
        }

        var voucherDate = start.AddDays(10).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        db.AccountingPeriods.Add(period);
        db.AccountingVouchers.Add(new AccountingVoucher
        {
            VoucherNumber = $"SRC-{Guid.NewGuid():N}"[..30],
            VoucherDate = voucherDate,
            VoucherType = "Journal",
            Status = "Posted",
            PostedAt = voucherDate,
            Entries =
            {
                new AccountingEntry
                {
                    GLAccountId = expense.Id,
                    CostCenterId = costCenter.Id,
                    DebitAmount = 100
                },
                new AccountingEntry
                {
                    GLAccountId = balancing.Id,
                    CreditAmount = 100
                }
            }
        });
        await db.SaveChangesAsync();

        return new Fixture(period, workOrders, wip, finishedGoods, cogs);
    }

    private static ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private sealed record Fixture(
        AccountingPeriod Period,
        WorkOrder[] WorkOrders,
        GLAccount WipAccount,
        GLAccount FinishedGoodsAccount,
        GLAccount CogsAccount);
}
