using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Models.System;

namespace ErpBackend.IntegrationTests;

public class KpisControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();

    public KpisControllerIntegrationTests()
    {
        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.KpiValues.RemoveRange(db.KpiValues);
        db.KpiDefinitions.RemoveRange(db.KpiDefinitions);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var kpi = new KpiDefinition
        {
            KpiCode = "OEE",
            KpiName = "Overall Equipment Effectiveness",
            Description = "設備綜合效率",
            Category = "efficiency",
            Unit = "%",
            TargetValue = 85,
            WarningThreshold = 70,
            CriticalThreshold = 50,
            CalculationFrequency = "daily",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.KpiDefinitions.Add(kpi);
        db.SaveChanges();
    }

    [Fact]
    public async Task GetDashboard_ReturnsDashboard()
    {
        var response = await _client.GetAsync("/api/erp/kpis/dashboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(0, data.GetProperty("averageOee").GetDecimal());
        Assert.Equal(0, data.GetProperty("onTimeDeliveryRate").GetDecimal());
        Assert.Equal(0, data.GetProperty("wipQuantity").GetDecimal());
    }

    [Fact]
    public async Task GetDefinitions_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/kpis/definitions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("data").EnumerateArray().Count());
        Assert.Equal("OEE", root.GetProperty("data")[0].GetProperty("kpiCode").GetString());
    }

    [Fact]
    public async Task GetDefinition_ExistingId_ReturnsDetail()
    {
        using var db = _factory.CreateDbContext();
        var kpiId = db.KpiDefinitions.First().Id;

        var response = await _client.GetAsync($"/api/erp/kpis/definitions/{kpiId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("OEE", root.GetProperty("data").GetProperty("kpiCode").GetString());
    }

    [Fact]
    public async Task GetDefinition_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/erp/kpis/definitions/00000000-0000-0000-0000-000000000000");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDefinition_Valid_ReturnsCreated()
    {
        var req = new
        {
            kpiCode = "OTD",
            kpiName = "On-Time Delivery",
            description = "準時交貨率",
            category = "delivery",
            unit = "%",
            targetValue = 95,
            warningThreshold = 85,
            criticalThreshold = 70,
            calculationFrequency = "daily",
            isActive = true
        };

        var response = await _client.PostAsJsonAsync("/api/erp/kpis/definitions", req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("OTD", root.GetProperty("data").GetProperty("kpiCode").GetString());
    }

    [Fact]
    public async Task GetOtd_ValidRange_ReturnsResult()
    {
        var response = await _client.GetAsync("/api/erp/kpis/otd?fromDate=2024-01-01&toDate=2024-12-31");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(0, data.GetProperty("totalOrders").GetInt32());
    }

    [Fact]
    public async Task GetWip_ReturnsResult()
    {
        var response = await _client.GetAsync("/api/erp/kpis/wip");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(0, root.GetProperty("data").GetProperty("totalWipQuantity").GetDecimal());
    }

    [Fact]
    public async Task GetTrend_ValidKpi_ReturnsEmpty()
    {
        using var db = _factory.CreateDbContext();
        var kpiId = db.KpiDefinitions.First(k => k.KpiCode == "OEE").Id;

        var response = await _client.GetAsync($"/api/erp/kpis/trend?kpiCode=OEE&fromDate=2024-01-01&toDate=2024-12-31");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(0, root.GetProperty("data").EnumerateArray().Count());
    }

    [Fact]
    public async Task GetPing_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/erp/kpis/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
