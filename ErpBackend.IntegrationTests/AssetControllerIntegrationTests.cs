using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Asset;

namespace ErpBackend.IntegrationTests;

public class AssetControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();

    public AssetControllerIntegrationTests()
    {
        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.AssetMaintenances.RemoveRange(db.AssetMaintenances);
        db.AssetDepreciations.RemoveRange(db.AssetDepreciations);
        db.Assets.RemoveRange(db.Assets);
        db.AssetLocations.RemoveRange(db.AssetLocations);
        db.AssetCategories.RemoveRange(db.AssetCategories);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var cat = new AssetCategory
        {
            CategoryCode = "MACH",
            CategoryName = "Machinery",
            DepreciationMethod = "straight_line",
            DefaultUsefulLife = 60,
            DefaultResidualRate = 0.1m
        };
        db.AssetCategories.Add(cat);

        var loc = new AssetLocation
        {
            LocationCode = "PLANT-A",
            LocationName = "Plant A",
            Address = "No. 1 Industrial Rd"
        };
        db.AssetLocations.Add(loc);
        db.SaveChanges();

        var asset = new Asset
        {
            AssetNumber = "AST-001",
            AssetName = "CNC Machine",
            Specification = "4-axis CNC",
            Manufacturer = "Haas",
            CategoryId = cat.Id,
            LocationId = loc.Id,
            AcquisitionCost = 1000000,
            AcquisitionDate = new DateTime(2023, 1, 1),
            UsefulLife = 60,
            ResidualValue = 100000,
            DepreciationMethod = "straight_line",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Assets.Add(asset);
        db.SaveChanges();
    }

    #region Asset CRUD

    [Fact]
    public async Task GetAllAssets_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/assets");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("data").EnumerateArray().Count());
    }

    [Fact]
    public async Task GetAllAssets_WithStatusFilter_ReturnsFiltered()
    {
        var response = await _client.GetAsync("/api/erp/assets?status=active");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("data").EnumerateArray().Count());
    }

    [Fact]
    public async Task GetAsset_ExistingId_ReturnsDetail()
    {
        using var db = _factory.CreateDbContext();
        var assetId = db.Assets.First().Id;

        var response = await _client.GetAsync($"/api/erp/assets/{assetId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("AST-001", data.GetProperty("assetNumber").GetString());
        Assert.Equal("CNC Machine", data.GetProperty("assetName").GetString());
        Assert.Equal("PLANT-A", data.GetProperty("locationCode").GetString());
    }

    [Fact]
    public async Task GetAsset_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/erp/assets/00000000-0000-0000-0000-000000000000");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAsset_Valid_ReturnsCreated()
    {
        using var db = _factory.CreateDbContext();
        var catId = db.AssetCategories.First().Id;

        var req = new
        {
            assetNumber = "AST-002",
            assetName = "Lathe Machine",
            specification = "2-axis lathe",
            manufacturer = "Mazak",
            categoryId = catId,
            acquisitionCost = 800000,
            usefulLife = 120,
            residualValue = 80000,
            depreciationMethod = "straight_line",
            status = "active"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/assets", req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("AST-002", root.GetProperty("data").GetProperty("assetNumber").GetString());
    }

    [Fact]
    public async Task CreateAsset_DuplicateNumber_ReturnsBadRequest()
    {
        using var db = _factory.CreateDbContext();
        var catId = db.AssetCategories.First().Id;

        var req = new
        {
            assetNumber = "AST-001",
            assetName = "Duplicate",
            categoryId = catId,
            acquisitionCost = 100,
            usefulLife = 60,
            residualValue = 10,
            depreciationMethod = "straight_line",
            status = "active"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/assets", req);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAsset_ExistingId_ReturnsUpdated()
    {
        using var db = _factory.CreateDbContext();
        var assetId = db.Assets.First().Id;
        var catId = db.AssetCategories.First().Id;

        var req = new
        {
            assetNumber = "AST-001",
            assetName = "CNC Machine Updated",
            categoryId = catId,
            acquisitionCost = 1100000,
            usefulLife = 60,
            residualValue = 100000,
            depreciationMethod = "straight_line",
            status = "active"
        };

        var response = await _client.PutAsJsonAsync($"/api/erp/assets/{assetId}", req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("CNC Machine Updated", root.GetProperty("data").GetProperty("assetName").GetString());
    }

    [Fact]
    public async Task DeleteAsset_ExistingId_ReturnsOk()
    {
        using var db = _factory.CreateDbContext();
        var catId = db.AssetCategories.First().Id;
        var locId = db.AssetLocations.First().Id;

        var asset = new Asset
        {
            AssetNumber = "AST-DELETE",
            AssetName = "To Be Deleted",
            CategoryId = catId,
            LocationId = locId,
            AcquisitionCost = 50000,
            AcquisitionDate = new DateTime(2023, 1, 1),
            UsefulLife = 36,
            ResidualValue = 5000,
            DepreciationMethod = "straight_line",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Assets.Add(asset);
        db.SaveChanges();
        var id = asset.Id;

        var response = await _client.DeleteAsync($"/api/erp/assets/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResp = await _client.GetAsync($"/api/erp/assets/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    #endregion

    #region Maintenance

    [Fact]
    public async Task GetAssetMaintenance_ExistingAsset_ReturnsList()
    {
        using var db = _factory.CreateDbContext();
        var assetId = db.Assets.First().Id;

        var response = await _client.GetAsync($"/api/erp/assets/{assetId}/maintenance");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(0, root.GetProperty("data").EnumerateArray().Count());
    }

    [Fact]
    public async Task AddAssetMaintenance_Valid_ReturnsCreated()
    {
        using var db = _factory.CreateDbContext();
        var assetId = db.Assets.First().Id;

        var req = new
        {
            maintenanceType = "preventive",
            description = "Quarterly inspection",
            partsCost = 5000,
            laborCost = 3000,
            externalCost = 0,
            isCapitalized = false,
            serialNumber = "SN-001"
        };

        var response = await _client.PostAsJsonAsync($"/api/erp/assets/{assetId}/maintenance", req);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("preventive", root.GetProperty("data").GetProperty("maintenanceType").GetString());
    }

    #endregion

    #region Depreciation

    [Fact]
    public async Task RunDepreciation_Valid_ReturnsResult()
    {
        var response = await _client.PostAsync("/api/erp/assets/depreciation/run?year=2024&month=1", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(2024, root.GetProperty("data").GetProperty("year").GetInt32());
        Assert.Equal(1, root.GetProperty("data").GetProperty("month").GetInt32());
    }

    [Fact]
    public async Task GetAssetDepreciation_ExistingAsset_ReturnsHistory()
    {
        using var db = _factory.CreateDbContext();
        var assetId = db.Assets.First().Id;

        var response = await _client.GetAsync($"/api/erp/assets/{assetId}/depreciation");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    #endregion
}
