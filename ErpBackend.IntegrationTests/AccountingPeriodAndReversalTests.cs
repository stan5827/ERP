using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Services;
using Xunit;

namespace ErpBackend.IntegrationTests;

public class AccountingPeriodAndReversalTests
{
    private static readonly CustomWebApplicationFactory Factory = new();

    [Fact]
    public async Task SaveChangesAsync_RejectsPostingIntoClosedAccountingPeriod()
    {
        using var db = Factory.CreateDbContext();
        var postingDate = new DateOnly(2097, 5, 12);
        db.AccountingPeriods.Add(new AccountingPeriod
        {
            PeriodCode = $"C-{Guid.NewGuid():N}"[..30],
            StartDate = new DateOnly(2097, 5, 1),
            EndDate = new DateOnly(2097, 5, 31),
            IsClosed = true,
            ClosedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        db.AccountingVouchers.Add(CreateVoucher(postingDate));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.SaveChangesAsync());

        Assert.Contains("會計期間已關帳", exception.Message);
    }

    [Fact]
    public async Task FinanceService_RejectsOverlappingPeriodsAndKeepsClosedPeriodsImmutable()
    {
        using var db = Factory.CreateDbContext();
        var service = new FinanceService(db);
        var start = new DateOnly(2093, 1, 1);
        var (created, createStatus, period) = await service.CreateAccountingPeriodAsync(
            new CreateAccountingPeriodRequestDto
            {
                PeriodCode = $"P-{Guid.NewGuid():N}"[..30],
                StartDate = start,
                EndDate = new DateOnly(2093, 1, 31)
            });

        Assert.True(created);
        Assert.Equal("ok", createStatus);
        Assert.NotNull(period);

        var (overlapCreated, overlapStatus, _) = await service.CreateAccountingPeriodAsync(
            new CreateAccountingPeriodRequestDto
            {
                PeriodCode = $"O-{Guid.NewGuid():N}"[..30],
                StartDate = new DateOnly(2093, 1, 15),
                EndDate = new DateOnly(2093, 2, 15)
            });
        Assert.False(overlapCreated);
        Assert.Equal("period_overlap", overlapStatus);

        var (closed, closeStatus) = await service.CloseAccountingPeriodAsync(period!.Id);
        Assert.True(closed);
        Assert.Equal("ok", closeStatus);

        var trackedPeriod = await db.AccountingPeriods.FindAsync(period.Id);
        trackedPeriod!.EndDate = new DateOnly(2093, 2, 1);
        var immutableException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.SaveChangesAsync());
        Assert.Contains("已關帳會計期間不可修改", immutableException.Message);
    }

    [Fact]
    public async Task SaveChangesAsync_RejectsChangesToPostedVoucherAndEntries()
    {
        using var db = Factory.CreateDbContext();
        var voucher = CreateVoucher(new DateOnly(2098, 4, 10));
        db.AccountingVouchers.Add(voucher);
        await db.SaveChangesAsync();

        voucher.Description = "不得修改";
        var voucherException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.SaveChangesAsync());
        Assert.Contains("已過帳傳票不可直接修改", voucherException.Message);

        db.Entry(voucher).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
        db.Entry(voucher.Entries.First()).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
        var entryException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.SaveChangesAsync());
        Assert.Contains("已過帳傳票分錄不可直接修改", entryException.Message);
    }

    [Fact]
    public async Task ReverseVoucherAsync_CreatesBalancedOppositeVoucherAndPreventsSecondReversal()
    {
        using var db = Factory.CreateDbContext();
        var original = CreateVoucher(new DateOnly(2099, 6, 15));
        original.Entries.First().DebitAmount = 325m;
        original.Entries.Last().CreditAmount = 325m;
        db.AccountingVouchers.Add(original);
        await db.SaveChangesAsync();

        var service = new AccountingService(db);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (success, status, reversal) = await service.ReverseVoucherAsync(
            original.Id,
            new DateOnly(2099, 6, 16),
            "測試沖銷");
        await transaction.CommitAsync();

        Assert.True(success);
        Assert.Equal("ok", status);
        Assert.NotNull(reversal);
        Assert.Equal(original.Id, reversal.ReversalOfVoucherId);
        Assert.Equal(original.Entries.First().DebitAmount, reversal.Entries.First().CreditAmount);
        Assert.Equal(original.Entries.Last().CreditAmount, reversal.Entries.Last().DebitAmount);
        Assert.Equal(original.Entries.Sum(entry => entry.DebitAmount),
            reversal.Entries.Sum(entry => entry.CreditAmount));

        var (secondSuccess, secondStatus, existingReversal) = await service.ReverseVoucherAsync(
            original.Id,
            new DateOnly(2099, 6, 16),
            "重複沖銷");
        Assert.False(secondSuccess);
        Assert.Equal("already_reversed", secondStatus);
        Assert.Equal(reversal.Id, existingReversal?.Id);
    }

    [Fact]
    public async Task ReverseVoucherAsync_RejectsDateInsideClosedPeriod()
    {
        using var db = Factory.CreateDbContext();
        var original = CreateVoucher(new DateOnly(2094, 12, 31));
        db.AccountingVouchers.Add(original);
        db.AccountingPeriods.Add(new AccountingPeriod
        {
            PeriodCode = $"C-{Guid.NewGuid():N}"[..30],
            StartDate = new DateOnly(2095, 1, 1),
            EndDate = new DateOnly(2095, 1, 31),
            IsClosed = true,
            ClosedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var (success, status, reversal) = await new AccountingService(db).ReverseVoucherAsync(
            original.Id,
            new DateOnly(2095, 1, 15),
            "關帳期間沖銷");

        Assert.False(success);
        Assert.Equal("period_closed", status);
        Assert.Null(reversal);
    }

    private static AccountingVoucher CreateVoucher(DateOnly date) =>
        new()
        {
            VoucherNumber = $"T{Guid.NewGuid():N}"[..30],
            VoucherDate = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
            VoucherType = "Journal",
            Status = "Posted",
            Entries =
            {
                new AccountingEntry { GLAccountId = Guid.NewGuid(), DebitAmount = 100m },
                new AccountingEntry { GLAccountId = Guid.NewGuid(), CreditAmount = 100m }
            }
        };
}
