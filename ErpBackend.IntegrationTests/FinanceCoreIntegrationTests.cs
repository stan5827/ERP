using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Finance;

namespace ErpBackend.IntegrationTests;

public class FinanceCoreIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();

    public FinanceCoreIntegrationTests()
    {
        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.GLAccountMappings.RemoveRange(db.GLAccountMappings);
        db.SaveChanges();
        db.GLAccounts.RemoveRange(db.GLAccounts);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        db.GLAccounts.AddRange(
            new GLAccount { AccountCode = "1101", AccountName = "現金", AccountType = "Asset", IsActive = true, IsCashAccount = true },
            new GLAccount { AccountCode = "1102", AccountName = "銀行存款", AccountType = "Asset", IsActive = true, IsCashAccount = true },
            new GLAccount { AccountCode = "2101", AccountName = "應付帳款", AccountType = "Liability", IsActive = true, IsCashAccount = false },
            new GLAccount { AccountCode = "4101", AccountName = "銷貨收入", AccountType = "Revenue", IsActive = true, IsCashAccount = false },
            new GLAccount { AccountCode = "5101", AccountName = "銷貨成本", AccountType = "Expense", IsActive = true, IsCashAccount = false }
        );
        db.SaveChanges();
    }

    #region GLAccount / Chart of Accounts

    [Fact]
    public async Task GetChartOfAccounts_ReturnsAllAccounts()
    {
        var response = await _client.GetAsync("/api/erp/finance/coa");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(5, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task SeedAccounts_ReturnsOk()
    {
        var response = await _client.PostAsync("/api/erp/finance/seed", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("財務科目已初始化", body);
    }

    #endregion

    #region Vouchers

    [Fact]
    public async Task GetVouchers_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/finance/vouchers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    #endregion

    #region Posting Setups

    [Fact]
    public async Task GetPostingSetups_ReturnsSetups()
    {
        var response = await _client.GetAsync("/api/erp/finance/posting-setups");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(3, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task GetPostingSetups_UsesConfiguredAccountMappings()
    {
        using (var db = _factory.CreateDbContext())
        {
            var accounts = new[]
            {
                new GLAccount { AccountCode = "1131", AccountName = "應收帳款", AccountType = "Asset" },
                new GLAccount { AccountCode = "1201", AccountName = "原物料存貨", AccountType = "Asset" },
                new GLAccount { AccountCode = "1205", AccountName = "成品存貨", AccountType = "Asset" }
            };
            db.GLAccounts.AddRange(accounts);
            db.SaveChanges();

            db.GLAccountMappings.AddRange(
                new GLAccountMapping { MappingKey = "sales_cogs", GLAccountId = db.GLAccounts.Single(account => account.AccountCode == "5101").Id },
                new GLAccountMapping { MappingKey = "finished_goods_inventory", GLAccountId = accounts[2].Id },
                new GLAccountMapping { MappingKey = "accounts_receivable", GLAccountId = accounts[0].Id },
                new GLAccountMapping { MappingKey = "sales_revenue", GLAccountId = db.GLAccounts.Single(account => account.AccountCode == "4101").Id },
                new GLAccountMapping { MappingKey = "raw_material_inventory", GLAccountId = accounts[1].Id },
                new GLAccountMapping { MappingKey = "accounts_payable", GLAccountId = db.GLAccounts.Single(account => account.AccountCode == "2101").Id });
            db.SaveChanges();
        }

        var response = await _client.GetAsync("/api/erp/finance/posting-setups");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var setups = document.RootElement.GetProperty("data")
            .EnumerateArray()
            .ToDictionary(setup => setup.GetProperty("transactionType").GetString()!);
        Assert.Equal("5101", setups["SALES_COGS"].GetProperty("drAccountNo").GetString());
        Assert.Equal("1205", setups["SALES_COGS"].GetProperty("crAccountNo").GetString());
        Assert.Equal("1131", setups["SALES_REV"].GetProperty("drAccountNo").GetString());
        Assert.Equal("4101", setups["SALES_REV"].GetProperty("crAccountNo").GetString());
        Assert.Equal("1201", setups["PUR_REC"].GetProperty("drAccountNo").GetString());
        Assert.Equal("2101", setups["PUR_REC"].GetProperty("crAccountNo").GetString());
    }

    #endregion

    #region Journal Entries

    [Fact]
    public async Task GetJournalEntries_ExistingDocNo_ReturnsEntries()
    {
        using var db = _factory.CreateDbContext();
        var voucher = new AccountingVoucher
        {
            VoucherNumber = "V-TEST-001",
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            Description = "測試傳票",
            Status = "Draft"
        };
        db.AccountingVouchers.Add(voucher);
        db.SaveChanges();

        var account = db.GLAccounts.First();
        db.AccountingEntries.Add(new AccountingEntry
        {
            VoucherId = voucher.Id,
            GLAccountId = account.Id,
            DebitAmount = 1000,
            CreditAmount = 0,
            Memo = "借方測試"
        });
        db.SaveChanges();

        var response = await _client.GetAsync($"/api/erp/finance/journals/{voucher.VoucherNumber}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(1, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task GetJournalEntries_UnknownDocNo_ReturnsEmpty()
    {
        var response = await _client.GetAsync("/api/erp/finance/journals/NONEXISTENT");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(0, data.EnumerateArray().Count());
    }

    #endregion
}
