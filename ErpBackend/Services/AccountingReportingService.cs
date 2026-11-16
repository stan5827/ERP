using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface IAccountingReportingService
{
    Task<GeneralLedgerResultDto?> GetGeneralLedgerAsync(
        GeneralLedgerQueryRequestDto request,
        CancellationToken cancellationToken);
    Task<TrialBalanceResultDto> GetTrialBalanceAsync(
        TrialBalanceQueryRequestDto request,
        CancellationToken cancellationToken);
    Task<FinancialStatementsResultDto> GetFinancialStatementsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken);
}

public sealed class AccountingReportingService : IAccountingReportingService
{
    private readonly ErpGeneralDbContext _db;

    public AccountingReportingService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<GeneralLedgerResultDto?> GetGeneralLedgerAsync(
        GeneralLedgerQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        var account = await _db.GLAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.AccountId, cancellationToken);
        if (account is null)
            return null;

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var entriesQuery = _db.AccountingEntries
            .AsNoTracking()
            .Where(entry => entry.GLAccountId == account.Id && entry.Voucher.Status == "Posted");
        if (request.FromDate.HasValue)
        {
            var from = StartOfDay(request.FromDate.Value);
            entriesQuery = entriesQuery.Where(entry => entry.Voucher.VoucherDate >= from);
        }
        if (request.ToDate.HasValue)
        {
            var toExclusive = EndExclusive(request.ToDate.Value);
            entriesQuery = entriesQuery.Where(entry => entry.Voucher.VoucherDate < toExclusive);
        }

        var openingQuery = _db.AccountingEntries
            .AsNoTracking()
            .Where(entry => entry.GLAccountId == account.Id && entry.Voucher.Status == "Posted");
        if (request.FromDate.HasValue)
        {
            var from = StartOfDay(request.FromDate.Value);
            openingQuery = openingQuery.Where(entry => entry.Voucher.VoucherDate < from);
        }
        else
        {
            openingQuery = openingQuery.Where(_ => false);
        }

        var openingTotals = await GetTotalsAsync(openingQuery, cancellationToken);
        var allEntries = await entriesQuery
            .OrderBy(entry => entry.Voucher.VoucherDate)
            .ThenBy(entry => entry.Voucher.VoucherNumber)
            .ThenBy(entry => entry.LineNumber)
            .ThenBy(entry => entry.Id)
            .Select(entry => new
            {
                entry.Id,
                entry.VoucherId,
                entry.Voucher.VoucherNumber,
                entry.Voucher.VoucherDate,
                entry.Voucher.VoucherType,
                entry.Voucher.Description,
                entry.Voucher.ReferenceType,
                entry.Voucher.ReferenceId,
                entry.LineNumber,
                entry.DebitAmount,
                entry.CreditAmount
            })
            .ToListAsync(cancellationToken);

        var openingNet = openingTotals.OpeningDebit - openingTotals.OpeningCredit;
        var runningNet = openingNet;
        var ledgerEntries = allEntries.Select(entry =>
        {
            runningNet += entry.DebitAmount - entry.CreditAmount;
            var balances = ToDebitCredit(runningNet);
            return new GeneralLedgerEntryDto
            {
                EntryId = entry.Id,
                VoucherId = entry.VoucherId,
                VoucherNumber = entry.VoucherNumber,
                VoucherDate = entry.VoucherDate,
                VoucherType = entry.VoucherType,
                Description = entry.Description,
                ReferenceType = entry.ReferenceType,
                ReferenceId = entry.ReferenceId,
                LineNumber = entry.LineNumber,
                DebitAmount = entry.DebitAmount,
                CreditAmount = entry.CreditAmount,
                RunningDebitBalance = balances.Debit,
                RunningCreditBalance = balances.Credit
            };
        }).ToList();
        var periodDebit = allEntries.Sum(entry => entry.DebitAmount);
        var periodCredit = allEntries.Sum(entry => entry.CreditAmount);
        var closingBalances = ToDebitCredit(openingNet + periodDebit - periodCredit);

        return new GeneralLedgerResultDto
        {
            AccountId = account.Id,
            AccountCode = account.AccountCode,
            AccountName = account.AccountName,
            AccountType = account.AccountType,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            OpeningDebitBalance = ToDebitCredit(openingNet).Debit,
            OpeningCreditBalance = ToDebitCredit(openingNet).Credit,
            PeriodDebitTotal = periodDebit,
            PeriodCreditTotal = periodCredit,
            ClosingDebitBalance = closingBalances.Debit,
            ClosingCreditBalance = closingBalances.Credit,
            TotalCount = ledgerEntries.Count,
            Page = page,
            PageSize = pageSize,
            Entries = ledgerEntries.Skip((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue))
                .Take(pageSize)
                .ToList()
        };
    }

    public async Task<TrialBalanceResultDto> GetTrialBalanceAsync(
        TrialBalanceQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        var toDate = request.ToDate!.Value;
        var (accounts, balances) = await GetAccountBalancesAsync(
            request.FromDate,
            toDate,
            cancellationToken);
        var rows = accounts.Select(account =>
            ToAccountBalance(account, balances.GetValueOrDefault(account.Id) ?? AccountTotals.Zero)).ToList();

        return new TrialBalanceResultDto
        {
            FromDate = request.FromDate,
            ToDate = toDate,
            Accounts = rows,
            OpeningDebitTotal = rows.Sum(row => row.OpeningDebitBalance),
            OpeningCreditTotal = rows.Sum(row => row.OpeningCreditBalance),
            PeriodDebitTotal = rows.Sum(row => row.PeriodDebitTotal),
            PeriodCreditTotal = rows.Sum(row => row.PeriodCreditTotal),
            ClosingDebitTotal = rows.Sum(row => row.ClosingDebitBalance),
            ClosingCreditTotal = rows.Sum(row => row.ClosingCreditBalance),
            IsBalanced = rows.Sum(row => row.ClosingDebitBalance) ==
                         rows.Sum(row => row.ClosingCreditBalance)
        };
    }

    public async Task<FinancialStatementsResultDto> GetFinancialStatementsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var (accounts, periodBalances) = await GetAccountBalancesAsync(
            fromDate,
            toDate,
            cancellationToken);
        var (_, cumulativeBalances) = await GetAccountBalancesAsync(
            null,
            toDate,
            cancellationToken);
        var accountRows = accounts.Select(account =>
            ToAccountBalance(account, periodBalances.GetValueOrDefault(account.Id) ?? AccountTotals.Zero)).ToList();
        var cumulativeRows = accounts.Select(account =>
            ToAccountBalance(account, cumulativeBalances.GetValueOrDefault(account.Id) ?? AccountTotals.Zero)).ToList();
        var assets = accountRows.Where(row => IsType(row.AccountType, "Asset")).ToList();
        var liabilities = accountRows.Where(row => IsType(row.AccountType, "Liability")).ToList();
        var equity = accountRows.Where(row => IsType(row.AccountType, "Equity")).ToList();
        var revenue = accountRows.Where(row => IsType(row.AccountType, "Revenue")).ToList();
        var expenses = accountRows.Where(row => IsType(row.AccountType, "Expense")).ToList();
        var cumulativeProfit = cumulativeRows
            .Where(row => IsType(row.AccountType, "Revenue"))
            .Sum(row => row.ClosingCreditBalance - row.ClosingDebitBalance)
            - cumulativeRows
                .Where(row => IsType(row.AccountType, "Expense"))
                .Sum(row => row.ClosingDebitBalance - row.ClosingCreditBalance);
        var totalAssets = assets.Sum(row => row.ClosingDebitBalance - row.ClosingCreditBalance);
        var totalLiabilities = liabilities.Sum(row => row.ClosingCreditBalance - row.ClosingDebitBalance);
        var totalEquity = equity.Sum(row => row.ClosingCreditBalance - row.ClosingDebitBalance);
        var periodNetIncome = revenue.Sum(row => row.PeriodCreditTotal - row.PeriodDebitTotal)
                              - expenses.Sum(row => row.PeriodDebitTotal - row.PeriodCreditTotal);
        var balanceSheetDifference = totalAssets - totalLiabilities - totalEquity - cumulativeProfit;

        return new FinancialStatementsResultDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            Assets = assets,
            Liabilities = liabilities,
            Equity = equity,
            Revenue = revenue,
            Expenses = expenses,
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquity = totalEquity,
            PeriodNetIncome = periodNetIncome,
            CumulativeUnclosedProfit = cumulativeProfit,
            BalanceSheetDifference = balanceSheetDifference,
            IsBalanceSheetBalanced = balanceSheetDifference == 0m
        };
    }

    private async Task<(List<GLAccount> Accounts, Dictionary<Guid, AccountTotals> Totals)>
        GetAccountBalancesAsync(
            DateOnly? fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken)
    {
        var accounts = await _db.GLAccounts
            .AsNoTracking()
            .OrderBy(account => account.AccountCode)
            .ThenBy(account => account.Id)
            .ToListAsync(cancellationToken);
        var cutoff = EndExclusive(toDate);
        var postedEntries = _db.AccountingEntries
            .AsNoTracking()
            .Where(entry => entry.Voucher.Status == "Posted" &&
                            entry.Voucher.VoucherDate < cutoff);
        var openingEntries = fromDate.HasValue
            ? postedEntries.Where(entry => entry.Voucher.VoucherDate < StartOfDay(fromDate.Value))
            : postedEntries.Where(_ => false);
        var periodEntries = fromDate.HasValue
            ? postedEntries.Where(entry => entry.Voucher.VoucherDate >= StartOfDay(fromDate.Value))
            : postedEntries;
        var opening = await GetTotalsByAccountAsync(openingEntries, cancellationToken);
        var period = await GetTotalsByAccountAsync(periodEntries, cancellationToken);
        var totals = accounts.ToDictionary(account => account.Id, account =>
        {
            var openingTotals = opening.GetValueOrDefault(account.Id) ?? AccountTotals.Zero;
            var periodTotals = period.GetValueOrDefault(account.Id) ?? AccountTotals.Zero;
            return new AccountTotals(
                openingTotals.OpeningDebit,
                openingTotals.OpeningCredit,
                periodTotals.OpeningDebit,
                periodTotals.OpeningCredit);
        });
        return (accounts, totals);
    }

    private async Task<Dictionary<Guid, AccountTotals>> GetTotalsByAccountAsync(
        IQueryable<AccountingEntry> query,
        CancellationToken cancellationToken)
    {
        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var entries = await query
                .Select(entry => new { entry.GLAccountId, entry.DebitAmount, entry.CreditAmount })
                .ToListAsync(cancellationToken);
            return entries
                .GroupBy(entry => entry.GLAccountId)
                .ToDictionary(
                    group => group.Key,
                    group => new AccountTotals(
                        group.Sum(entry => entry.DebitAmount),
                        group.Sum(entry => entry.CreditAmount),
                        0m,
                        0m));
        }

        var grouped = await query
            .GroupBy(entry => entry.GLAccountId)
            .Select(group => new
            {
                AccountId = group.Key,
                Debit = group.Sum(entry => entry.DebitAmount),
                Credit = group.Sum(entry => entry.CreditAmount)
            })
            .ToListAsync(cancellationToken);
        return grouped.ToDictionary(
            item => item.AccountId,
            item => new AccountTotals(item.Debit, item.Credit, 0m, 0m));
    }

    private async Task<AccountTotals> GetTotalsAsync(
        IQueryable<AccountingEntry> query,
        CancellationToken cancellationToken)
    {
        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var amounts = await query
                .Select(entry => new { entry.DebitAmount, entry.CreditAmount })
                .ToListAsync(cancellationToken);
            return new AccountTotals(
                amounts.Sum(item => item.DebitAmount),
                amounts.Sum(item => item.CreditAmount),
                0m,
                0m);
        }

        var result = await query
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Debit = group.Sum(entry => entry.DebitAmount),
                Credit = group.Sum(entry => entry.CreditAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);
        return new AccountTotals(result?.Debit ?? 0m, result?.Credit ?? 0m, 0m, 0m);
    }

    private static AccountBalanceDto ToAccountBalance(GLAccount account, AccountTotals totals)
    {
        var opening = ToDebitCredit(totals.OpeningDebit - totals.OpeningCredit);
        var closing = ToDebitCredit(
            totals.OpeningDebit - totals.OpeningCredit +
            totals.PeriodDebit - totals.PeriodCredit);
        return new AccountBalanceDto
        {
            AccountId = account.Id,
            AccountCode = account.AccountCode,
            AccountName = account.AccountName,
            AccountType = account.AccountType,
            OpeningDebitBalance = opening.Debit,
            OpeningCreditBalance = opening.Credit,
            PeriodDebitTotal = totals.PeriodDebit,
            PeriodCreditTotal = totals.PeriodCredit,
            ClosingDebitBalance = closing.Debit,
            ClosingCreditBalance = closing.Credit
        };
    }

    private static (decimal Debit, decimal Credit) ToDebitCredit(decimal netBalance) =>
        netBalance >= 0m ? (netBalance, 0m) : (0m, -netBalance);

    private static bool IsType(string actual, string expected) =>
        actual.Equals(expected, StringComparison.OrdinalIgnoreCase);

    private static DateTime StartOfDay(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private static DateTime EndExclusive(DateOnly date) =>
        date == DateOnly.MaxValue
            ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            : date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private sealed record AccountTotals(
        decimal OpeningDebit,
        decimal OpeningCredit,
        decimal PeriodDebit,
        decimal PeriodCredit)
    {
        public static AccountTotals Zero { get; } = new(0m, 0m, 0m, 0m);
    }
}
