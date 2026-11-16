using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using System.Collections.Generic;

namespace ErpBackend.Services;

public class FinanceService : IFinanceService
{
    private readonly ErpGeneralDbContext _db;

    public FinanceService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<List<GLAccountDto>> GetChartOfAccountsAsync()
    {
        var accounts = await _db.GLAccounts
            .OrderBy(a => a.AccountCode)
            .ToListAsync();
        return accounts.Select(a => new GLAccountDto
        {
            Id = a.Id,
            AccountCode = a.AccountCode,
            AccountName = a.AccountName,
            AccountType = a.AccountType,
            IsActive = a.IsActive,
            IsCashAccount = a.IsCashAccount
        }).ToList();
    }

    public async Task<List<GLAccountMappingDto>> GetAccountingMappingsAsync()
    {
        var mappings = await _db.GLAccountMappings
            .Include(mapping => mapping.GLAccount)
            .ToDictionaryAsync(mapping => mapping.MappingKey);

        return AccountingMappingKeys.All.Select(definition =>
        {
            mappings.TryGetValue(definition.Key, out var mapping);
            var account = mapping?.GLAccount;
            return new GLAccountMappingDto
            {
                MappingKey = definition.Key,
                Name = definition.Name,
                RequiredAccountType = definition.AccountType,
                GLAccountId = mapping?.GLAccountId,
                AccountCode = account?.AccountCode,
                AccountName = account?.AccountName,
                IsConfigured = account is { IsActive: true } &&
                               account.AccountType.Equals(
                                   definition.AccountType,
                                   StringComparison.OrdinalIgnoreCase)
            };
        }).ToList();
    }

    public async Task<(bool success, string status)> SetAccountingMappingAsync(
        string mappingKey,
        Guid glAccountId)
    {
        var definition = AccountingMappingKeys.All
            .FirstOrDefault(item => item.Key.Equals(mappingKey, StringComparison.OrdinalIgnoreCase));
        if (definition is null)
            return (false, "unknown_mapping");

        var account = await _db.GLAccounts
            .FirstOrDefaultAsync(item => item.Id == glAccountId);
        if (account is null || !account.IsActive ||
            !account.AccountType.Equals(definition.AccountType, StringComparison.OrdinalIgnoreCase))
            return (false, "invalid_account");

        var mapping = await _db.GLAccountMappings
            .FirstOrDefaultAsync(item => item.MappingKey == definition.Key);
        if (mapping is null)
        {
            mapping = new GLAccountMapping
            {
                MappingKey = definition.Key,
                GLAccountId = account.Id,
                UpdatedAt = DateTime.UtcNow
            };
            _db.GLAccountMappings.Add(mapping);
        }
        else
        {
            mapping.GLAccountId = account.Id;
            mapping.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    public async Task<List<AccountingPeriodDto>> GetAccountingPeriodsAsync()
    {
        return await _db.AccountingPeriods
            .OrderBy(period => period.StartDate)
            .Select(period => new AccountingPeriodDto
            {
                Id = period.Id,
                PeriodCode = period.PeriodCode,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                IsClosed = period.IsClosed,
                ClosedAt = period.ClosedAt
            })
            .ToListAsync();
    }

    public async Task<(bool success, string status, AccountingPeriodDto? period)> CreateAccountingPeriodAsync(
        CreateAccountingPeriodRequestDto request)
    {
        var periodCode = request.PeriodCode?.Trim();
        if (string.IsNullOrWhiteSpace(periodCode) || periodCode.Length > 30)
            return (false, "invalid_period_code", null);
        if (request.StartDate == default || request.EndDate == default ||
            request.StartDate > request.EndDate)
            return (false, "invalid_date_range", null);
        if (await _db.AccountingPeriods.AnyAsync(period => period.PeriodCode == periodCode))
            return (false, "period_code_exists", null);
        if (await _db.AccountingPeriods.AnyAsync(period =>
                period.StartDate <= request.EndDate && period.EndDate >= request.StartDate))
            return (false, "period_overlap", null);

        var period = new AccountingPeriod
        {
            PeriodCode = periodCode,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };
        _db.AccountingPeriods.Add(period);
        await _db.SaveChangesAsync();

        return (true, "ok", new AccountingPeriodDto
        {
            Id = period.Id,
            PeriodCode = period.PeriodCode,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            IsClosed = period.IsClosed,
            ClosedAt = period.ClosedAt
        });
    }

    public async Task<(bool success, string status)> CloseAccountingPeriodAsync(Guid periodId)
    {
        var period = await _db.AccountingPeriods.FirstOrDefaultAsync(item => item.Id == periodId);
        if (period is null)
            return (false, "not_found");
        if (period.IsClosed)
            return (false, "already_closed");

        var start = period.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusive = period.EndDate == DateOnly.MaxValue
            ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            : period.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var periodVouchers = _db.AccountingVouchers.Where(voucher =>
            voucher.VoucherDate >= start && voucher.VoucherDate < endExclusive);
        if (await periodVouchers.AnyAsync(voucher => voucher.Status != "Posted"))
            return (false, "unposted_vouchers");

        var postedVoucherIds = await periodVouchers
            .Select(voucher => voucher.Id)
            .ToListAsync();
        var postedEntries = await _db.AccountingEntries
            .Where(entry => postedVoucherIds.Contains(entry.VoucherId))
            .Select(entry => new
            {
                entry.VoucherId,
                entry.DebitAmount,
                entry.CreditAmount
            })
            .ToListAsync();
        var balancesByVoucher = postedEntries
            .GroupBy(entry => entry.VoucherId)
            .ToDictionary(
                group => group.Key,
                group => (
                    Debit: group.Sum(entry => entry.DebitAmount),
                    Credit: group.Sum(entry => entry.CreditAmount)));
        if (postedVoucherIds.Any(voucherId =>
                !balancesByVoucher.TryGetValue(voucherId, out var totals) ||
                totals.Debit != totals.Credit))
            return (false, "unbalanced_vouchers");

        var debitTotal = postedEntries.Sum(entry => entry.DebitAmount);
        var creditTotal = postedEntries.Sum(entry => entry.CreditAmount);
        if (debitTotal != creditTotal)
            return (false, "unbalanced_vouchers");

        period.IsClosed = true;
        period.ClosedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    public async Task<List<JournalEntryDto>> GetJournalEntriesAsync(string docNo)
    {
        var entries = await _db.AccountingEntries
            .Include(e => e.GLAccount)
            .Include(e => e.Voucher)
            .Where(e => e.Voucher.VoucherNumber == docNo || e.Voucher.ReferenceId.ToString() == docNo)
            .OrderBy(e => e.Id)
            .ToListAsync();

        return entries.Select(e => new JournalEntryDto
        {
            Id = e.Id,
            EntryDate = e.Voucher.VoucherDate,
            AccountId = e.GLAccountId,
            AccountNo = e.GLAccount.AccountCode,
            AccountName = e.GLAccount.AccountName,
            AmountDr = e.DebitAmount,
            AmountCr = e.CreditAmount,
            SourceModule = e.Voucher.ReferenceType ?? "",
            SourceDocNo = e.Voucher.VoucherNumber,
            Description = e.Memo
        }).ToList();
    }

    public async Task<List<PostingSetupDto>> GetCostingSetupsAsync()
    {
        var setupDefinitions = new[]
        {
            (TransactionType: "SALES_COGS", DebitKey: "sales_cogs", CreditKey: "finished_goods_inventory"),
            (TransactionType: "SALES_REV", DebitKey: "accounts_receivable", CreditKey: "sales_revenue"),
            (TransactionType: "PUR_REC", DebitKey: "raw_material_inventory", CreditKey: "accounts_payable")
        };
        var mappingKeys = setupDefinitions
            .SelectMany(definition => new[] { definition.DebitKey, definition.CreditKey })
            .ToHashSet(StringComparer.Ordinal);
        var mappings = await _db.GLAccountMappings
            .Include(mapping => mapping.GLAccount)
            .Where(mapping => mappingKeys.Contains(mapping.MappingKey))
            .ToDictionaryAsync(mapping => mapping.MappingKey);

        string? GetAccountCode(string mappingKey)
        {
            var definition = AccountingMappingKeys.All
                .First(mapping => mapping.Key == mappingKey);
            if (!mappings.TryGetValue(mappingKey, out var mapping))
                return null;

            var account = mapping.GLAccount;
            return account is { IsActive: true } &&
                   account.AccountType.Equals(definition.AccountType, StringComparison.OrdinalIgnoreCase)
                ? account.AccountCode
                : null;
        }

        return setupDefinitions.Select(definition => new PostingSetupDto
        {
            TransactionType = definition.TransactionType,
            DrAccountNo = GetAccountCode(definition.DebitKey),
            CrAccountNo = GetAccountCode(definition.CreditKey)
        }).ToList();
    }

    public Task SeedAccountsAsync()
    {
        return Task.CompletedTask;
    }

    public async Task SeedBankAccountsAsync()
    {
        var defaultAccounts = new List<BankAccount>
        {
            new BankAccount { BankCode = "HNB", BankName = "本國銀行", AccountNumber = "123-456-789", AccountName = "ERP 主要帳戶", Currency = "TWD", CurrentBalance = 1000000 },
            new BankAccount { BankCode = "HNB", BankName = "本國銀行", AccountNumber = "987-654-321", AccountName = "ERP 外幣帳戶", Currency = "USD", CurrentBalance = 50000 }
        };

        foreach (var acct in defaultAccounts)
        {
            if (!await _db.BankAccounts.AnyAsync(b => b.AccountNumber == acct.AccountNumber))
                _db.BankAccounts.Add(acct);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<LoanDto>> GetLoansAsync(string? status)
    {
        var query = _db.Loans.AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(l => l.Status == status);

        var loans = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
        return loans.Select(l => new LoanDto
        {
            Id = l.Id,
            LoanNumber = l.LoanNumber,
            BankName = l.BankName,
            LoanAmount = l.LoanAmount,
            Currency = l.Currency,
            AnnualInterestRate = l.AnnualInterestRate,
            LoanDate = l.LoanDate,
            MaturityDate = l.MaturityDate,
            RepaymentMethod = l.RepaymentMethod,
            Status = l.Status
        }).ToList();
    }

    public async Task CreateLoanAsync(CreateLoanRequest request)
    {
        var loan = new Loan
        {
            LoanNumber = $"LN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            BankName = request.BankName,
            LoanAmount = request.LoanAmount,
            Currency = request.Currency ?? "TWD",
            AnnualInterestRate = request.AnnualInterestRate,
            LoanDate = request.LoanDate ?? DateTime.UtcNow,
            MaturityDate = request.MaturityDate ?? DateTime.UtcNow.AddYears(5),
            RepaymentMethod = request.RepaymentMethod ?? "bullet",
            Status = "active"
        };

        _db.Loans.Add(loan);
        await _db.SaveChangesAsync();
    }

    public async Task<List<BankAccountDto>> GetBankAccountsAsync()
    {
        var accounts = await _db.BankAccounts
            .Include(account => account.GLAccount)
            .Where(b => b.IsActive)
            .ToListAsync();
        return accounts.Select(a => new BankAccountDto
        {
            Id = a.Id,
            BankCode = a.BankCode,
            BankName = a.BankName,
            AccountNumber = a.AccountNumber,
            GLAccountId = a.GLAccountId,
            GLAccountCode = a.GLAccount?.AccountCode,
            GLAccountName = a.GLAccount?.AccountName,
            Currency = a.Currency,
            CurrentBalance = a.CurrentBalance,
            IsActive = a.IsActive
        }).ToList();
    }

    public async Task<bool> CreateBankAccountAsync(CreateBankAccountRequest request)
    {
        if (request.GLAccountId.HasValue &&
            !await _db.GLAccounts.AnyAsync(account =>
                account.Id == request.GLAccountId.Value && account.IsActive && account.IsCashAccount))
        {
            return false;
        }

        var account = new BankAccount
        {
            BankCode = request.BankCode,
            BankName = request.BankName,
            AccountNumber = request.AccountNumber,
            AccountName = request.AccountName,
            GLAccountId = request.GLAccountId,
            Currency = request.Currency ?? "TWD",
            CurrentBalance = request.CurrentBalance ?? 0
        };

        _db.BankAccounts.Add(account);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetBankAccountGlMappingAsync(Guid bankAccountId, Guid glAccountId)
    {
        var account = await _db.BankAccounts.FirstOrDefaultAsync(item => item.Id == bankAccountId);
        var glAccount = await _db.GLAccounts.FirstOrDefaultAsync(item =>
            item.Id == glAccountId && item.IsActive && item.IsCashAccount);
        if (account == null || glAccount == null)
            return false;

        account.GLAccountId = glAccount.Id;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<InvestmentDto>> GetInvestmentsAsync(string? category)
    {
        var query = _db.Investments.AsQueryable();
        if (!string.IsNullOrEmpty(category)) query = query.Where(i => i.InvestmentCategory == category);

        var investments = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
        return investments.Select(i => new InvestmentDto
        {
            Id = i.Id,
            InvestmentName = i.InvestmentName,
            InvestmentCategory = i.InvestmentCategory,
            TotalCost = i.TotalCost,
            CurrentMarketValue = i.CurrentMarketValue,
            UnrealizedGainLoss = i.UnrealizedGainLoss
        }).ToList();
    }

    public async Task CreateInvestmentAsync(CreateInvestmentRequest request)
    {
        var investment = new Investment
        {
            InvestmentName = request.InvestmentName,
            InvestmentCategory = request.InvestmentCategory ?? "stock",
            TotalCost = request.TotalCost,
            CurrentMarketValue = request.CurrentMarketValue ?? request.TotalCost,
            UnrealizedGainLoss = 0,
            PurchaseDate = request.PurchaseDate ?? DateTime.UtcNow,
            Quantity = request.Quantity ?? 1,
            UnitPrice = request.TotalCost / (request.Quantity ?? 1)
        };

        _db.Investments.Add(investment);
        await _db.SaveChangesAsync();
    }

    public async Task<List<object>> GetTreasuryTransfersAsync()
    {
        var transfers = await _db.TreasuryTransfers.OrderByDescending(t => t.TransferDate).ToListAsync();
        return transfers.Select(t => (object)new
        {
            t.Id, t.TransferNumber, t.TransferDate, t.Reason,
            t.TransferAmount, t.Currency, t.ExchangeRate, t.ExchangeGainLoss
        }).ToList();
    }

    public async Task CreateTreasuryTransferAsync(CreateTreasuryTransferRequest request)
    {
        var transfer = new TreasuryTransfer
        {
            TransferNumber = $"TF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            TransferDate = request.TransferDate ?? DateTime.UtcNow,
            Reason = request.Reason,
            FromBankAccountId = request.FromBankAccountId,
            ToBankAccountId = request.ToBankAccountId,
            TransferAmount = request.TransferAmount,
            Currency = request.Currency ?? "TWD",
            ExchangeRate = request.ExchangeRate ?? 1
        };

        _db.TreasuryTransfers.Add(transfer);
        await _db.SaveChangesAsync();
    }

    public async Task<List<object>> GetGuaranteesAsync()
    {
        var guarantees = await _db.Guarantees.OrderByDescending(g => g.ApplicationDate).ToListAsync();
        return guarantees.Select(g => (object)new { g.Id, g.GuaranteeNumber, g.GuaranteeType, g.Beneficiary, g.GuaranteeAmount, g.Status }).ToList();
    }

    public async Task CreateGuaranteeAsync(CreateGuaranteeRequest request)
    {
        var guarantee = new Guarantee
        {
            GuaranteeNumber = $"GT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            GuaranteeType = request.GuaranteeType ?? "bank",
            Beneficiary = request.Beneficiary,
            GuaranteeAmount = request.GuaranteeAmount,
            ApplicationDate = request.ApplicationDate ?? DateTime.UtcNow,
            ExpiryDate = request.ExpiryDate,
            FeeRate = request.FeeRate ?? 0,
            Status = "active"
        };

        _db.Guarantees.Add(guarantee);
        await _db.SaveChangesAsync();
    }
}
