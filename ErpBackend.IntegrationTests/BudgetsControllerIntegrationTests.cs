using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Finance;

namespace ErpBackend.IntegrationTests;

public class BudgetsControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();

    public BudgetsControllerIntegrationTests()
    {
        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.CostCenters.RemoveRange(db.CostCenters);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var cc1 = new CostCenter
        {
            CostCenterCode = "CC-001",
            CostCenterName = "Production Dept",
            CostCenterType = "production",
            IsActive = true,
            BudgetLimit = 1000000,
            BudgetUsed = 100000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var cc2 = new CostCenter
        {
            CostCenterCode = "CC-002",
            CostCenterName = "Sales Dept",
            CostCenterType = "sales",
            IsActive = true,
            BudgetLimit = 100000,
            BudgetUsed = 95000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.CostCenters.AddRange(cc1, cc2);
        db.SaveChanges();
    }

    [Fact]
    public async Task CheckOverrun_NormalAmount_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/erp/budgets/overrun?costCenterCode=CC-001&amount=100000");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("CC-001", data.GetProperty("costCenterCode").GetString());
        Assert.Equal(100000, data.GetProperty("amount").GetDecimal());
        Assert.Equal("ok", data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CheckOverrun_ExceededAmount_ReturnsExceeded()
    {
        var response = await _client.GetAsync("/api/erp/budgets/overrun?costCenterCode=CC-002&amount=10000");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("exceeded", data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CheckOverrun_UnknownCostCenter_ReturnsNoBudget()
    {
        var response = await _client.GetAsync("/api/erp/budgets/overrun?costCenterCode=UNKNOWN&amount=1000");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("no_budget", data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetAlerts_ReturnsAlerts()
    {
        var response = await _client.GetAsync("/api/erp/budgets/alerts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(1, data.EnumerateArray().Count());
        Assert.Equal("CC-002", data[0].GetProperty("costCenterCode").GetString());
    }

    [Fact]
    public async Task GetPing_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/erp/budgets/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
