using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Finance;

namespace ErpBackend.IntegrationTests;

public class FinanceControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();

    public FinanceControllerIntegrationTests()
    {
        SeedFinanceData();
    }

    private void SeedFinanceData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.GLAccounts.RemoveRange(db.GLAccounts);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        db.GLAccounts.AddRange(
            new GLAccount { AccountCode = "1101", AccountName = "現金", AccountType = "Asset", IsActive = true, IsCashAccount = true },
            new GLAccount { AccountCode = "1102", AccountName = "銀行存款", AccountType = "Asset", IsActive = true, IsCashAccount = true },
            new GLAccount { AccountCode = "2101", AccountName = "應付帳款", AccountType = "Liability", IsActive = true, IsCashAccount = false },
            new GLAccount { AccountCode = "4101", AccountName = "銷貨收入", AccountType = "Revenue", IsActive = true, IsCashAccount = false }
        );
        db.SaveChanges();
    }

    [Fact]
    public async Task GetChartOfAccounts_ReturnsAccounts()
    {
        var response = await _client.GetAsync("/api/erp/finance/coa");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(4, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task SeedAccounts_ReturnsOk()
    {
        var response = await _client.PostAsync("/api/erp/finance/seed", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("財務科目已初始化", body);
    }

    [Fact]
    public async Task GetLoans_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/erp/finance/loans");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateLoan_ValidRequest_ReturnsOk()
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

    [Fact]
    public async Task GetBankAccounts_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/erp/finance/bank-accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateBankAccount_ValidRequest_ReturnsOk()
    {
        var request = new
        {
            BankCode = "004",
            BankName = "測試銀行",
            AccountNumber = "1234567890",
            Currency = "TWD",
            CurrentBalance = 500000
        };

        var response = await _client.PostAsJsonAsync("/api/erp/finance/bank-accounts", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("銀行帳戶已建立", body);
    }

    [Fact]
    public async Task GetInvestments_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/erp/finance/investments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateInvestment_ValidRequest_ReturnsOk()
    {
        var request = new
        {
            InvestmentName = "台積電股票",
            InvestmentCategory = "stock",
            TotalCost = 500000,
            CurrentMarketValue = 550000
        };

        var response = await _client.PostAsJsonAsync("/api/erp/finance/investments", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("投資已建立", body);
    }

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

    [Fact]
    public async Task GetPostingSetups_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/finance/posting-setups");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }
}
