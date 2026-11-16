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
        db.GLAccountMappings.RemoveRange(db.GLAccountMappings);
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
    public async Task CreateGLAccount_ValidRequest_ReturnsCreatedAccount()
    {
        var parentAccountId = await GetAccountIdAsync("1101");
        var request = new
        {
            AccountCode = "1105",
            AccountName = "零用金",
            AccountType = "asset",
            IsCashAccount = true,
            ParentAccountId = parentAccountId
        };

        var response = await _client.PostAsJsonAsync("/api/erp/finance/coa", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var account = document.RootElement.GetProperty("data");
        Assert.Equal("1105", account.GetProperty("accountCode").GetString());
        Assert.Equal("Asset", account.GetProperty("accountType").GetString());
        Assert.True(account.GetProperty("isCashAccount").GetBoolean());
        Assert.Equal(parentAccountId.ToString(), account.GetProperty("parentAccountId").GetString());
    }

    [Fact]
    public async Task CreateGLAccount_DuplicateCode_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/erp/finance/coa", new
        {
            AccountCode = "1101",
            AccountName = "重複現金",
            AccountType = "Asset",
            IsCashAccount = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("科目代碼已存在", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateGLAccount_CashAccountMustBeAsset()
    {
        var response = await _client.PostAsJsonAsync("/api/erp/finance/coa", new
        {
            AccountCode = "5101",
            AccountName = "費用帳戶",
            AccountType = "Expense",
            IsCashAccount = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("現金／銀行科目必須為資產類", await response.Content.ReadAsStringAsync());
    }

    private async Task<Guid> GetAccountIdAsync(string accountCode)
    {
        await using var db = _factory.CreateDbContext();
        return await db.GLAccounts
            .Where(account => account.AccountCode == accountCode)
            .Select(account => account.Id)
            .SingleAsync();
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
    public async Task SeedAccounts_CreatesStandardCostAccountsAndMappingsIdempotently()
    {
        var firstResponse = await _client.PostAsync("/api/erp/finance/seed", null);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var standardAccountCodes = """
            1101 1111 1136 1137 1150 1201 1202 1211 1221 1231 1241 1251 1270 1280
            1501 1502 1503 1504 1505 1506 1507 1509 1781 1841
            2101 2141 2145 2150 2151 2152 2155 2156 2170 2200 2201 2280 2541 2571 2601
            3101 3201 3311 3351 3411 3501
            4111 4121 4131 4191
            5101 5111 5121 5122 5123 5124 5125 5126 5127 5128 5129 5311
            6111 6150 6170 6180 6211 6225 6226 6230 6240 6250 6270 6280 6300 6511 6520
            7101 7480 7501 7880
            """.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var expectedAccounts = new Dictionary<string, (string Name, string Type)>
        {
            ["1136"] = ("應收帳款", "Asset"),
            ["1201"] = ("原料", "Asset"),
            ["1211"] = ("物料", "Asset"),
            ["1221"] = ("在製品", "Asset"),
            ["1231"] = ("半成品", "Asset"),
            ["1241"] = ("製成品", "Asset"),
            ["2101"] = ("短期借款", "Liability"),
            ["2141"] = ("應付帳款", "Liability"),
            ["4111"] = ("產品銷售收入", "Revenue"),
            ["5101"] = ("直接材料", "Expense"),
            ["5111"] = ("直接人工", "Expense"),
            ["5121"] = ("製造費用", "Expense"),
            ["5122"] = ("間接人工", "Expense"),
            ["5129"] = ("製造費用－其他", "Expense"),
            ["5311"] = ("銷貨成本", "Expense"),
            ["6511"] = ("研究發展人員薪資", "Expense"),
            ["7880"] = ("其他營業外支出", "Expense")
        };
        int seededAccountCount;
        using (var db = _factory.CreateDbContext())
        {
            var seeded = await db.GLAccounts
                .Where(account => standardAccountCodes.Contains(account.AccountCode))
                .ToDictionaryAsync(account => account.AccountCode);
            Assert.Equal(80, standardAccountCodes.Length);
            Assert.Equal(standardAccountCodes.Length, seeded.Count);
            foreach (var (code, expected) in expectedAccounts)
            {
                Assert.Equal(expected.Name, seeded[code].AccountName);
                Assert.Equal(expected.Type, seeded[code].AccountType);
            }

            var overhead = seeded["5121"];
            Assert.Equal(
                overhead.Id,
                await db.GLAccounts.Where(account => account.AccountCode == "5122")
                    .Select(account => account.ParentAccountId)
                    .SingleAsync());
            seededAccountCount = await db.GLAccounts.CountAsync();

            var mappings = await db.GLAccountMappings
                .Include(mapping => mapping.GLAccount)
                .ToDictionaryAsync(mapping => mapping.MappingKey);
            Assert.Equal("1136", mappings["accounts_receivable"].GLAccount.AccountCode);
            Assert.Equal("2141", mappings["accounts_payable"].GLAccount.AccountCode);
            Assert.Equal("4111", mappings["sales_revenue"].GLAccount.AccountCode);
            Assert.Equal("5311", mappings["sales_cogs"].GLAccount.AccountCode);
            Assert.Equal("1221", mappings["work_in_progress"].GLAccount.AccountCode);
            Assert.Equal("1241", mappings["finished_goods_inventory"].GLAccount.AccountCode);
        }

        var secondResponse = await _client.PostAsync("/api/erp/finance/seed", null);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using var verifyDb = _factory.CreateDbContext();
        Assert.Equal(seededAccountCount, await verifyDb.GLAccounts.CountAsync());
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
