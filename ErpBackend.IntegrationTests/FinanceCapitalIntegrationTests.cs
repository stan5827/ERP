using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Finance;

namespace ErpBackend.IntegrationTests;

public class FinanceCapitalIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();
    private Guid _bankAccountId;

    public FinanceCapitalIntegrationTests()
    {
        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.TreasuryTransfers.RemoveRange(db.TreasuryTransfers);
        db.Investments.RemoveRange(db.Investments);
        db.Loans.RemoveRange(db.Loans);
        db.BankAccounts.RemoveRange(db.BankAccounts);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var ba = new BankAccount
        {
            BankCode = "HNB",
            BankName = "本國銀行",
            AccountNumber = "123-456-789",
            AccountName = "ERP 主要帳戶",
            Currency = "TWD",
            CurrentBalance = 1000000,
            IsActive = true
        };
        db.BankAccounts.Add(ba);
        db.SaveChanges();
        _bankAccountId = ba.Id;
    }

    #region Bank Accounts

    [Fact]
    public async Task GetBankAccounts_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/finance/bank-accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(1, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task CreateBankAccount_ValidRequest_Creates()
    {
        var request = new
        {
            BankCode = "CB",
            BankName = "中國銀行",
            AccountNumber = "987-654-321",
            AccountName = "外幣帳戶",
            Currency = "USD",
            CurrentBalance = 50000
        };

        var response = await _client.PostAsJsonAsync("/api/erp/finance/bank-accounts", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("銀行帳戶已建立", body);
    }

    #endregion

    #region Loans

    [Fact]
    public async Task GetLoans_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/finance/loans");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateLoan_ValidRequest_Creates()
    {
        var request = new
        {
            BankName = "測試銀行",
            LoanAmount = 1000000,
            Currency = "TWD",
            AnnualInterestRate = 2.5m,
            LoanDate = DateTime.UtcNow,
            MaturityDate = DateTime.UtcNow.AddYears(5),
            RepaymentMethod = "bullet"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/finance/loans", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("貸款已建立", body);
    }

    #endregion

    #region Investments

    [Fact]
    public async Task GetInvestments_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/finance/investments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateInvestment_ValidRequest_Creates()
    {
        var request = new
        {
            InvestmentName = "台積電股票",
            InvestmentCategory = "stock",
            TotalCost = 500000,
            CurrentMarketValue = 550000,
            PurchaseDate = DateTime.UtcNow,
            Quantity = 1000
        };

        var response = await _client.PostAsJsonAsync("/api/erp/finance/investments", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("投資已建立", body);
    }

    #endregion

    #region Treasury Transfers

    [Fact]
    public async Task GetTreasuryTransfers_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/finance/treasury-transfers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateTreasuryTransfer_ValidRequest_Creates()
    {
        var request = new
        {
            TransferDate = DateTime.UtcNow,
            Reason = "資金調撥測試",
            FromBankAccountId = _bankAccountId,
            ToBankAccountId = Guid.NewGuid(),
            TransferAmount = 100000,
            Currency = "TWD",
            ExchangeRate = 1
        };

        var response = await _client.PostAsJsonAsync("/api/erp/finance/treasury-transfers", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("資金調撥已建立", body);
    }

    #endregion
}
