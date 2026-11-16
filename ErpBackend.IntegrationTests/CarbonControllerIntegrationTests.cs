using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Digital;

namespace ErpBackend.IntegrationTests;

public class CarbonControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();

    public CarbonControllerIntegrationTests()
    {
        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.GhgEmissionRecords.RemoveRange(db.GhgEmissionRecords);
        db.GhgEmissionSources.RemoveRange(db.GhgEmissionSources);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var source1 = new GhgEmissionSource
        {
            SourceCode = "SRC-001",
            SourceName = "Natural Gas Boiler",
            Scope = "scope1",
            EnergyType = "Natural Gas",
            EmissionFactor = 2.0m,
            GwpValue = 1
        };
        var source2 = new GhgEmissionSource
        {
            SourceCode = "SRC-002",
            SourceName = "Purchased Electricity",
            Scope = "scope2",
            EnergyType = "Electricity",
            EmissionFactor = 0.5m,
            GwpValue = 1
        };
        db.GhgEmissionSources.AddRange(source1, source2);
        db.SaveChanges();

        var record1 = new GhgEmissionRecord
        {
            SourceId = source1.Id,
            Period = "202401",
            ActivityData = 1000,
            Unit = "m3",
            CalculatedEmissions = 2000,
            CreatedAt = DateTime.UtcNow
        };
        var record2 = new GhgEmissionRecord
        {
            SourceId = source2.Id,
            Period = "202401",
            ActivityData = 5000,
            Unit = "kWh",
            CalculatedEmissions = 2500,
            CreatedAt = DateTime.UtcNow
        };
        db.GhgEmissionRecords.AddRange(record1, record2);
        db.SaveChanges();
    }

    [Fact]
    public async Task GetDashboard_ValidYear_ReturnsDashboard()
    {
        var response = await _client.GetAsync("/api/erp/carbon/dashboard?year=2024");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(2024, data.GetProperty("year").GetInt32());
        Assert.Equal(4500, data.GetProperty("totalEmissions").GetDecimal());
    }

    [Fact]
    public async Task GetEmissionRecords_WithScope_ReturnsFiltered()
    {
        var response = await _client.GetAsync("/api/erp/carbon/records?scope=scope1&year=2024");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("data").EnumerateArray().Count());
    }

    [Fact]
    public async Task GetEmissionRecords_WithoutScope_ReturnsAll()
    {
        var response = await _client.GetAsync("/api/erp/carbon/records?year=2024");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(2, root.GetProperty("data").EnumerateArray().Count());
    }

    [Fact]
    public async Task GetSummary_ValidYear_ReturnsSummary()
    {
        var response = await _client.GetAsync("/api/erp/carbon/summary?year=2024");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(2024, data.GetProperty("year").GetInt32());
        Assert.Equal(4500, data.GetProperty("totalEmissions").GetDecimal());
        Assert.Equal(2000, data.GetProperty("scope1Total").GetDecimal());
        Assert.Equal(2500, data.GetProperty("scope2Total").GetDecimal());
    }

    [Fact]
    public async Task GetIntensity_ValidYear_ReturnsIntensity()
    {
        var response = await _client.GetAsync("/api/erp/carbon/intensity?year=2024");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(2024, root.GetProperty("data").GetProperty("year").GetInt32());
    }

    [Fact]
    public async Task GetPing_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/erp/carbon/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
