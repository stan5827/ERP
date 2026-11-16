using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public class FinanceBankAccountServiceTests
{
    private static ErpGeneralDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GetBankAccountsAsync_ReturnsInactiveAccountsAndAccountNames()
    {
        await using var db = CreateDbContext();
        db.BankAccounts.AddRange(
            new BankAccount { BankName = "銀行甲", AccountNumber = "001", AccountName = "薪資帳戶" },
            new BankAccount { BankName = "銀行乙", AccountNumber = "002", AccountName = "備用帳戶", IsActive = false });
        await db.SaveChangesAsync();

        var accounts = await new FinanceService(db).GetBankAccountsAsync();

        Assert.Equal(2, accounts.Count);
        Assert.Contains(accounts, account => !account.IsActive && account.AccountName == "備用帳戶");
        Assert.Contains(accounts, account => account.IsActive && account.AccountName == "薪資帳戶");
    }

    [Fact]
    public async Task UpdateBankAccountAsync_RejectsInvalidGlAccountAndPreservesBalance()
    {
        await using var db = CreateDbContext();
        var cashAccount = new GLAccount { AccountCode = "1101", AccountName = "銀行存款", IsCashAccount = true };
        var expenseAccount = new GLAccount { AccountCode = "5101", AccountName = "費用", IsCashAccount = false };
        var bankAccount = new BankAccount
        {
            BankName = "原銀行",
            AccountNumber = "123",
            AccountName = "原帳戶",
            CurrentBalance = 1234.56m
        };
        db.GLAccounts.AddRange(cashAccount, expenseAccount);
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync();
        var service = new FinanceService(db);

        var (invalidSuccess, invalidStatus) = await service.UpdateBankAccountAsync(
            bankAccount.Id,
            new UpdateBankAccountRequestDto
            {
                BankName = "新銀行",
                AccountNumber = "456",
                Currency = "usd",
                GLAccountId = expenseAccount.Id
            });

        Assert.False(invalidSuccess);
        Assert.Equal("invalid_gl_account", invalidStatus);
        Assert.Equal("原銀行", (await db.BankAccounts.SingleAsync()).BankName);

        var (success, status) = await service.UpdateBankAccountAsync(
            bankAccount.Id,
            new UpdateBankAccountRequestDto
            {
                BankName = "新銀行",
                BranchName = "總行",
                AccountNumber = "456",
                AccountName = "新帳戶",
                Currency = "usd",
                GLAccountId = cashAccount.Id
            });

        var updated = await db.BankAccounts.SingleAsync();
        Assert.True(success);
        Assert.Equal("updated", status);
        Assert.Equal("新銀行", updated.BankName);
        Assert.Equal("總行", updated.BranchName);
        Assert.Equal("USD", updated.Currency);
        Assert.Equal(cashAccount.Id, updated.GLAccountId);
        Assert.Equal(1234.56m, updated.CurrentBalance);
    }

    [Fact]
    public async Task SetBankAccountStatusAsync_TogglesStatusAndReportsMissingAccounts()
    {
        await using var db = CreateDbContext();
        var account = new BankAccount { BankName = "銀行", AccountNumber = "001" };
        db.BankAccounts.Add(account);
        await db.SaveChangesAsync();
        var service = new FinanceService(db);

        Assert.True(await service.SetBankAccountStatusAsync(account.Id, false));
        Assert.False((await db.BankAccounts.SingleAsync()).IsActive);
        Assert.False(await service.SetBankAccountStatusAsync(Guid.NewGuid(), true));
    }
}
