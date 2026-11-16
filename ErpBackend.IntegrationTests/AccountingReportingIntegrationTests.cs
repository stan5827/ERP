using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.IntegrationTests;

public class AccountingReportingIntegrationTests
{
    private static readonly CustomWebApplicationFactory Factory = new();

    [Fact]
    public async Task Reports_ReconcilePostedEntriesAndGeneralLedgerLinksBackToVoucher()
    {
        using var db = Factory.CreateDbContext();
        var cash = new GLAccount
        {
            AccountCode = $"C{Guid.NewGuid():N}"[..20],
            AccountName = "報表測試現金",
            AccountType = "Asset"
        };
        var revenue = new GLAccount
        {
            AccountCode = $"R{Guid.NewGuid():N}"[..20],
            AccountName = "報表測試收入",
            AccountType = "Revenue"
        };
        var expense = new GLAccount
        {
            AccountCode = $"E{Guid.NewGuid():N}"[..20],
            AccountName = "報表測試費用",
            AccountType = "Expense"
        };
        db.GLAccounts.AddRange(cash, revenue, expense);
        await db.SaveChangesAsync();

        var firstVoucher = CreateVoucher(
            new DateOnly(2091, 1, 2),
            new AccountingEntry { GLAccountId = cash.Id, DebitAmount = 100m },
            new AccountingEntry { GLAccountId = revenue.Id, CreditAmount = 100m });
        var expenseVoucher = CreateVoucher(
            new DateOnly(2091, 1, 10),
            new AccountingEntry { GLAccountId = expense.Id, DebitAmount = 25m },
            new AccountingEntry { GLAccountId = cash.Id, CreditAmount = 25m });
        var secondRevenueVoucher = CreateVoucher(
            new DateOnly(2091, 1, 15),
            new AccountingEntry { GLAccountId = cash.Id, DebitAmount = 50m },
            new AccountingEntry { GLAccountId = revenue.Id, CreditAmount = 50m });
        db.AccountingVouchers.AddRange(firstVoucher, expenseVoucher, secondRevenueVoucher);
        await db.SaveChangesAsync();

        var reporting = new AccountingReportingService(db);
        var ledger = await reporting.GetGeneralLedgerAsync(
            new GeneralLedgerQueryRequestDto
            {
                AccountId = cash.Id,
                FromDate = new DateOnly(2091, 1, 1),
                ToDate = new DateOnly(2091, 1, 31),
                PageSize = 10
            },
            CancellationToken.None);
        var trialBalance = await reporting.GetTrialBalanceAsync(
            new TrialBalanceQueryRequestDto
            {
                FromDate = new DateOnly(2091, 1, 10),
                ToDate = new DateOnly(2091, 1, 31)
            },
            CancellationToken.None);
        var statements = await reporting.GetFinancialStatementsAsync(
            new DateOnly(2091, 1, 1),
            new DateOnly(2091, 1, 31),
            CancellationToken.None);

        Assert.NotNull(ledger);
        Assert.Equal(3, ledger.TotalCount);
        Assert.Equal(150m, ledger.PeriodDebitTotal);
        Assert.Equal(25m, ledger.PeriodCreditTotal);
        Assert.Equal(125m, ledger.ClosingDebitBalance);
        Assert.Equal(firstVoucher.Id, ledger.Entries[0].VoucherId);
        Assert.Equal(100m, ledger.Entries[0].RunningDebitBalance);
        Assert.Equal(75m, ledger.Entries[1].RunningDebitBalance);
        Assert.Equal(125m, ledger.Entries[2].RunningDebitBalance);
        Assert.True(trialBalance.IsBalanced);
        Assert.Equal(100m, trialBalance.OpeningDebitTotal);
        Assert.Equal(100m, trialBalance.OpeningCreditTotal);
        Assert.Equal(trialBalance.PeriodDebitTotal, trialBalance.PeriodCreditTotal);
        Assert.Equal(125m, statements.TotalAssets);
        Assert.Equal(125m, statements.PeriodNetIncome);
        Assert.Equal(125m, statements.CumulativeUnclosedProfit);
        Assert.True(statements.IsBalanceSheetBalanced);
        Assert.Equal(0m, statements.BalanceSheetDifference);
    }

    [Fact]
    public async Task CloseAccountingPeriod_RejectsDraftAndUnbalancedPostedVouchers()
    {
        using var db = Factory.CreateDbContext();
        var account = new GLAccount
        {
            AccountCode = $"A{Guid.NewGuid():N}"[..20],
            AccountName = "關帳檢核科目",
            AccountType = "Asset"
        };
        var period = new AccountingPeriod
        {
            PeriodCode = $"P-{Guid.NewGuid():N}"[..30],
            StartDate = new DateOnly(2092, 2, 1),
            EndDate = new DateOnly(2092, 2, 28)
        };
        var unbalancedPeriod = new AccountingPeriod
        {
            PeriodCode = $"U-{Guid.NewGuid():N}"[..30],
            StartDate = new DateOnly(2092, 3, 1),
            EndDate = new DateOnly(2092, 3, 31)
        };
        db.GLAccounts.Add(account);
        db.AccountingPeriods.AddRange(period, unbalancedPeriod);
        await db.SaveChangesAsync();

        var draft = CreateVoucher(
            new DateOnly(2092, 2, 10),
            new AccountingEntry { GLAccountId = account.Id, DebitAmount = 10m },
            new AccountingEntry { GLAccountId = account.Id, CreditAmount = 10m });
        draft.Status = "Draft";
        db.AccountingVouchers.Add(draft);
        await db.SaveChangesAsync();

        var service = new FinanceService(db);
        var (draftClosed, draftStatus) = await service.CloseAccountingPeriodAsync(period.Id);
        Assert.False(draftClosed);
        Assert.Equal("unposted_vouchers", draftStatus);

        var unbalancedVoucher = CreateVoucher(
            new DateOnly(2092, 3, 12),
            new AccountingEntry { GLAccountId = account.Id, DebitAmount = 10m },
            new AccountingEntry { GLAccountId = account.Id, CreditAmount = 10m });
        db.AccountingVouchers.Add(unbalancedVoucher);
        await db.SaveChangesAsync();
        await db.AccountingEntries
            .Where(entry => entry.Id == unbalancedVoucher.Entries.Last().Id)
            .ExecuteUpdateAsync(update => update.SetProperty(entry => entry.CreditAmount, 9m));

        var (unbalancedClosed, unbalancedStatus) =
            await service.CloseAccountingPeriodAsync(unbalancedPeriod.Id);
        Assert.False(unbalancedClosed);
        Assert.Equal("unbalanced_vouchers", unbalancedStatus);
        Assert.False((await db.AccountingPeriods.SingleAsync(item => item.Id == unbalancedPeriod.Id)).IsClosed);
    }

    private static AccountingVoucher CreateVoucher(
        DateOnly date,
        params AccountingEntry[] entries) =>
        new()
        {
            VoucherNumber = $"T{Guid.NewGuid():N}"[..30],
            VoucherDate = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
            VoucherType = "Journal",
            Status = "Posted",
            Entries = entries.ToList()
        };
}
