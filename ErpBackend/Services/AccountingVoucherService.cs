using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Helpers;
using ErpBackend.Models.Finance;

namespace ErpBackend.Services;

public interface IAccountingVoucherService
{
    Task<AccountingVoucherDto?> GetVoucherAsync(Guid voucherId, CancellationToken cancellationToken);
    Task<List<AccountingVoucherDto>> GetVouchersAsync(CancellationToken cancellationToken);
    Task<(bool success, string status, AccountingVoucherDto? voucher)> CreateDraftAsync(
        SaveAccountingVoucherRequestDto request,
        Guid userId,
        CancellationToken cancellationToken);
    Task<(bool success, string status, AccountingVoucherDto? voucher)> UpdateDraftAsync(
        Guid voucherId,
        SaveAccountingVoucherRequestDto request,
        Guid userId,
        CancellationToken cancellationToken);
    Task<(bool success, string status, AccountingVoucherDto? voucher)> SubmitDraftAsync(
        Guid voucherId,
        Guid userId,
        CancellationToken cancellationToken);
    Task<(bool success, string status, AccountingVoucherDto? voucher)> ApproveVoucherAsync(
        Guid voucherId,
        Guid userId,
        CancellationToken cancellationToken);
    Task<JournalQueryResultDto> GetJournalAsync(
        JournalQueryRequestDto request,
        CancellationToken cancellationToken);
}

public sealed class AccountingVoucherService : IAccountingVoucherService
{
    private static readonly HashSet<string> SupportedVoucherTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Journal", "CashReceipt", "CashPayment" };

    private readonly ErpGeneralDbContext _db;

    public AccountingVoucherService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<AccountingVoucherDto?> GetVoucherAsync(
        Guid voucherId,
        CancellationToken cancellationToken)
    {
        var voucher = await _db.AccountingVouchers
            .AsNoTracking()
            .Include(item => item.Entries)
                .ThenInclude(entry => entry.GLAccount)
            .FirstOrDefaultAsync(item => item.Id == voucherId, cancellationToken);

        return voucher is null ? null : ToDto(voucher);
    }

    public async Task<List<AccountingVoucherDto>> GetVouchersAsync(CancellationToken cancellationToken)
    {
        var vouchers = await _db.AccountingVouchers
            .AsNoTracking()
            .Include(item => item.Entries)
                .ThenInclude(entry => entry.GLAccount)
            .OrderByDescending(item => item.VoucherDate)
            .ThenByDescending(item => item.VoucherNumber)
            .Take(100)
            .ToListAsync(cancellationToken);

        return vouchers.Select(ToDto).ToList();
    }

    public async Task<(bool success, string status, AccountingVoucherDto? voucher)> CreateDraftAsync(
        SaveAccountingVoucherRequestDto request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var validationStatus = await ValidateRequestAsync(request, cancellationToken);
        if (validationStatus is not null)
            return (false, validationStatus, null);

        var voucher = new AccountingVoucher
        {
            VoucherNumber = DocumentNumberGenerator.Generate(GetVoucherPrefix(request.VoucherType)),
            VoucherDate = request.VoucherDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            VoucherType = request.VoucherType.Trim(),
            Description = request.Description?.Trim(),
            Status = "Draft",
            CreatedBy = userId == Guid.Empty ? null : userId,
            Entries = CreateEntries(request.Entries)
        };

        _db.AccountingVouchers.Add(voucher);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, "ok", await GetVoucherAsync(voucher.Id, cancellationToken));
    }

    public async Task<(bool success, string status, AccountingVoucherDto? voucher)> UpdateDraftAsync(
        Guid voucherId,
        SaveAccountingVoucherRequestDto request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var voucher = await _db.AccountingVouchers
            .Include(item => item.Entries)
            .FirstOrDefaultAsync(item => item.Id == voucherId, cancellationToken);
        if (voucher is null)
            return (false, "not_found", null);
        if (!voucher.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            return (false, "not_draft", null);

        var validationStatus = await ValidateRequestAsync(request, cancellationToken);
        if (validationStatus is not null)
            return (false, validationStatus, null);

        var existingEntries = voucher.Entries
            .OrderBy(entry => entry.LineNumber)
            .ThenBy(entry => entry.Id)
            .ToList();
        voucher.VoucherDate = request.VoucherDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        voucher.VoucherType = request.VoucherType.Trim();
        voucher.Description = request.Description?.Trim();
        _db.Entry(voucher).Property<Guid?>("ModifiedBy").CurrentValue =
            userId == Guid.Empty ? null : userId;
        _db.Entry(voucher).Property<DateTime?>("ModifiedAt").CurrentValue = DateTime.UtcNow;
        for (var index = 0; index < request.Entries.Count; index++)
        {
            var requestedEntry = request.Entries[index];
            var entry = index < existingEntries.Count
                ? existingEntries[index]
                : new AccountingEntry { VoucherId = voucher.Id };

            entry.LineNumber = index + 1;
            entry.GLAccountId = requestedEntry.GLAccountId;
            entry.DebitAmount = requestedEntry.DebitAmount;
            entry.CreditAmount = requestedEntry.CreditAmount;
            entry.CostCenterId = requestedEntry.CostCenterId;
            entry.WorkOrderId = requestedEntry.WorkOrderId;
            entry.Memo = requestedEntry.Memo?.Trim();

            if (index >= existingEntries.Count)
                voucher.Entries.Add(entry);
        }

        for (var index = request.Entries.Count; index < existingEntries.Count; index++)
            _db.AccountingEntries.Remove(existingEntries[index]);

        await _db.SaveChangesAsync(cancellationToken);
        return (true, "ok", await GetVoucherAsync(voucher.Id, cancellationToken));
    }

    public async Task<(bool success, string status, AccountingVoucherDto? voucher)> SubmitDraftAsync(
        Guid voucherId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var voucher = await _db.AccountingVouchers
            .Include(item => item.Entries)
            .ThenInclude(entry => entry.GLAccount)
            .FirstOrDefaultAsync(item => item.Id == voucherId, cancellationToken);
        if (voucher is null)
            return (false, "not_found", null);
        if (!voucher.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
            return (false, "not_draft", null);

        var validationStatus = ValidateForPosting(voucher);
        if (validationStatus is not null)
            return (false, validationStatus, null);
        if (!await AreCostCentersActiveAsync(voucher, cancellationToken))
            return (false, "invalid_cost_center", null);
        if (await IsInClosedPeriodAsync(voucher.VoucherDate, cancellationToken))
            return (false, "period_closed", null);

        voucher.Status = "PendingApproval";
        voucher.SubmittedAt = DateTime.UtcNow;
        voucher.SubmittedBy = userId == Guid.Empty ? null : userId;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, "ok", ToDto(voucher));
    }

    public async Task<(bool success, string status, AccountingVoucherDto? voucher)> ApproveVoucherAsync(
        Guid voucherId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var voucher = await _db.AccountingVouchers
            .Include(item => item.Entries)
                .ThenInclude(entry => entry.GLAccount)
            .FirstOrDefaultAsync(item => item.Id == voucherId, cancellationToken);
        if (voucher is null)
            return (false, "not_found", null);
        if (!voucher.Status.Equals("PendingApproval", StringComparison.OrdinalIgnoreCase))
            return (false, "not_pending_approval", null);
        if (userId != Guid.Empty && voucher.CreatedBy == userId)
            return (false, "creator_cannot_approve", null);

        var validationStatus = ValidateForPosting(voucher);
        if (validationStatus is not null)
            return (false, validationStatus, null);
        if (!await AreCostCentersActiveAsync(voucher, cancellationToken))
            return (false, "invalid_cost_center", null);
        if (await IsInClosedPeriodAsync(voucher.VoucherDate, cancellationToken))
            return (false, "period_closed", null);

        voucher.Status = "Posted";
        voucher.PostedAt = DateTime.UtcNow;
        voucher.PostedBy = userId == Guid.Empty ? null : userId;
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("會計期間已關帳", StringComparison.Ordinal))
        {
            return (false, "period_closed", null);
        }

        return (true, "ok", ToDto(voucher));
    }

    public async Task<JournalQueryResultDto> GetJournalAsync(
        JournalQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var offset = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        IQueryable<AccountingEntry> query = _db.AccountingEntries
            .AsNoTracking()
            .Where(entry => entry.Voucher.Status == "Posted");

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(entry => entry.Voucher.VoucherDate >= from);
        }

        if (request.ToDate.HasValue)
        {
            if (request.ToDate.Value == DateOnly.MaxValue)
            {
                var maximumDate = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
                query = query.Where(entry => entry.Voucher.VoucherDate <= maximumDate);
            }
            else
            {
                var toExclusive = request.ToDate.Value.AddDays(1)
                    .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(entry => entry.Voucher.VoucherDate < toExclusive);
            }
        }

        if (request.AccountId.HasValue)
            query = query.Where(entry => entry.GLAccountId == request.AccountId.Value);

        var voucherNumber = request.VoucherNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(voucherNumber))
            query = query.Where(entry => entry.Voucher.VoucherNumber.Contains(voucherNumber));

        var totalCount = await query.CountAsync(cancellationToken);
        decimal debitTotal;
        decimal creditTotal;
        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var amounts = await query
                .Select(entry => new { entry.DebitAmount, entry.CreditAmount })
                .ToListAsync(cancellationToken);
            debitTotal = amounts.Sum(item => item.DebitAmount);
            creditTotal = amounts.Sum(item => item.CreditAmount);
        }
        else
        {
            debitTotal = await query.SumAsync(
                entry => (decimal?)entry.DebitAmount,
                cancellationToken) ?? 0m;
            creditTotal = await query.SumAsync(
                entry => (decimal?)entry.CreditAmount,
                cancellationToken) ?? 0m;
        }
        var entries = await query
            .OrderBy(entry => entry.Voucher.VoucherDate)
            .ThenBy(entry => entry.Voucher.VoucherNumber)
            .ThenBy(entry => entry.LineNumber)
            .ThenBy(entry => entry.Id)
            .Skip(offset)
            .Take(pageSize)
            .Select(entry => new JournalEntryDto
            {
                Id = entry.Id,
                EntryDate = entry.Voucher.VoucherDate,
                AccountId = entry.GLAccountId,
                AccountNo = entry.GLAccount.AccountCode,
                AccountName = entry.GLAccount.AccountName,
                AmountDr = entry.DebitAmount,
                AmountCr = entry.CreditAmount,
                SourceModule = entry.Voucher.ReferenceType ?? "",
                SourceDocNo = entry.Voucher.VoucherNumber,
                Description = entry.Memo ?? entry.Voucher.Description
            })
            .ToListAsync(cancellationToken);

        return new JournalQueryResultDto
        {
            Entries = entries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            DebitTotal = debitTotal,
            CreditTotal = creditTotal
        };
    }

    private async Task<string?> ValidateRequestAsync(
        SaveAccountingVoucherRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.VoucherDate == default)
            return "invalid_date";
        if (string.IsNullOrWhiteSpace(request.VoucherType) ||
            !SupportedVoucherTypes.Contains(request.VoucherType.Trim()))
            return "invalid_type";
        if (request.Entries is null || request.Entries.Count < 2 || request.Entries.Count > 100)
            return "invalid_entries";
        if (request.Entries.Any(entry =>
                entry.GLAccountId == Guid.Empty ||
                entry.DebitAmount < 0 ||
                entry.CreditAmount < 0 ||
                entry.DebitAmount > 0 && entry.CreditAmount > 0 ||
                entry.Memo?.Length > 200))
            return "invalid_entries";

        var accountIds = request.Entries.Select(entry => entry.GLAccountId).Distinct().ToList();
        var activeAccountIds = await _db.GLAccounts
            .Where(account => accountIds.Contains(account.Id) && account.IsActive)
            .Select(account => account.Id)
            .ToListAsync(cancellationToken);
        if (activeAccountIds.Count != accountIds.Count)
            return "invalid_account";

        var costCenterIds = request.Entries
            .Where(entry => entry.CostCenterId.HasValue)
            .Select(entry => entry.CostCenterId!.Value)
            .Distinct()
            .ToList();
        if (costCenterIds.Count > 0)
        {
            var activeCostCenterCount = await _db.CostCenters
                .CountAsync(center => costCenterIds.Contains(center.Id) && center.IsActive, cancellationToken);
            if (activeCostCenterCount != costCenterIds.Count)
                return "invalid_cost_center";
        }

        var workOrderIds = request.Entries
            .Where(entry => entry.WorkOrderId.HasValue)
            .Select(entry => entry.WorkOrderId!.Value)
            .Distinct()
            .ToList();
        if (workOrderIds.Count > 0)
        {
            var workOrderCount = await _db.WorkOrders
                .CountAsync(order => workOrderIds.Contains(order.Id), cancellationToken);
            if (workOrderCount != workOrderIds.Count)
                return "invalid_work_order";
        }

        var voucherDate = request.VoucherDate;
        if (await _db.AccountingPeriods.AnyAsync(period =>
                period.IsClosed &&
                period.StartDate <= voucherDate &&
                period.EndDate >= voucherDate,
                cancellationToken))
            return "period_closed";

        return null;
    }

    private static List<AccountingEntry> CreateEntries(
        IEnumerable<AccountingVoucherLineRequestDto> entries) =>
        entries.Select((entry, index) => new AccountingEntry
        {
            LineNumber = index + 1,
            GLAccountId = entry.GLAccountId,
            DebitAmount = entry.DebitAmount,
            CreditAmount = entry.CreditAmount,
            CostCenterId = entry.CostCenterId,
            WorkOrderId = entry.WorkOrderId,
            Memo = entry.Memo?.Trim()
        }).ToList();

    private static string GetVoucherPrefix(string voucherType) =>
        voucherType.Trim().ToUpperInvariant() switch
        {
            "CASHRECEIPT" => "RC",
            "CASHPAYMENT" => "PC",
            _ => "JV"
        };

    private async Task<bool> IsInClosedPeriodAsync(
        DateTime voucherDate,
        CancellationToken cancellationToken)
    {
        var date = DateOnly.FromDateTime(voucherDate);
        return await _db.AccountingPeriods.AnyAsync(
            period => period.IsClosed &&
                      period.StartDate <= date &&
                      period.EndDate >= date,
            cancellationToken);
    }

    private async Task<bool> AreCostCentersActiveAsync(
        AccountingVoucher voucher,
        CancellationToken cancellationToken)
    {
        var costCenterIds = voucher.Entries
            .Where(entry => entry.CostCenterId.HasValue)
            .Select(entry => entry.CostCenterId!.Value)
            .Distinct()
            .ToList();
        if (costCenterIds.Count == 0)
            return true;

        var activeCount = await _db.CostCenters
            .CountAsync(center => costCenterIds.Contains(center.Id) && center.IsActive, cancellationToken);
        return activeCount == costCenterIds.Count;
    }

    private static string? ValidateForPosting(AccountingVoucher voucher)
    {
        try
        {
            AccountingVoucherRules.ValidateForPosting(new AccountingVoucher
            {
                Status = "Posted",
                ReferenceType = voucher.ReferenceType,
                ReferenceId = voucher.ReferenceId,
                PostingPurpose = voucher.PostingPurpose,
                Entries = voucher.Entries
            });
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message.Contains("借貸不平衡", StringComparison.Ordinal)
                ? "unbalanced"
                : "invalid_entries";
        }

        if (voucher.Entries.Any(entry => !entry.GLAccount.IsActive))
            return "invalid_account";
        if (voucher.VoucherType.Equals("CashReceipt", StringComparison.OrdinalIgnoreCase) &&
            !voucher.Entries.Any(entry => entry.GLAccount.IsCashAccount && entry.DebitAmount > 0))
            return "invalid_cash_account";
        if (voucher.VoucherType.Equals("CashPayment", StringComparison.OrdinalIgnoreCase) &&
            !voucher.Entries.Any(entry => entry.GLAccount.IsCashAccount && entry.CreditAmount > 0))
            return "invalid_cash_account";

        return null;
    }

    private static AccountingVoucherDto ToDto(AccountingVoucher voucher)
    {
        var entries = voucher.Entries
            .OrderBy(entry => entry.LineNumber)
            .ThenBy(entry => entry.Id)
            .Select(entry => new AccountingVoucherEntryDto
            {
                Id = entry.Id,
                LineNumber = entry.LineNumber,
                GLAccountId = entry.GLAccountId,
                AccountCode = entry.GLAccount.AccountCode,
                AccountName = entry.GLAccount.AccountName,
                CostCenterId = entry.CostCenterId,
                WorkOrderId = entry.WorkOrderId,
                DebitAmount = entry.DebitAmount,
                CreditAmount = entry.CreditAmount,
                Memo = entry.Memo
            })
            .ToList();

        return new AccountingVoucherDto
        {
            Id = voucher.Id,
            VoucherNumber = voucher.VoucherNumber,
            VoucherDate = voucher.VoucherDate,
            VoucherType = voucher.VoucherType,
            Description = voucher.Description,
            ReferenceType = voucher.ReferenceType,
            ReferenceId = voucher.ReferenceId,
            Status = voucher.Status,
            SubmittedAt = voucher.SubmittedAt,
            PostedAt = voucher.PostedAt,
            Entries = entries,
            DebitTotal = entries.Sum(entry => entry.DebitAmount),
            CreditTotal = entries.Sum(entry => entry.CreditAmount)
        };
    }
}
