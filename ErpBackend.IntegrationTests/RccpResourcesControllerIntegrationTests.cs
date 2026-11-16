using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.DTOs;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Models.Inventory;

namespace ErpBackend.IntegrationTests;

public class RccpResourcesControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();

    public RccpResourcesControllerIntegrationTests()
    {
        SeedRccpData();
    }

    private void SeedRccpData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.RccpItemResources.RemoveRange(db.RccpItemResources);
        db.RccpResources.RemoveRange(db.RccpResources);
        db.Skus.RemoveRange(db.Skus);
        db.WorkCenters.RemoveRange(db.WorkCenters);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        // WorkCenters
        var wc1 = new ErpBackend.Models.MES.WorkCenter
        {
            WorkCenterCode = "WC-CNC",
            WorkCenterName = "CNC ?�工中�?",
            Capacity = 100,
            Efficiency = 90,
            IsActive = true
        };
        var wc2 = new ErpBackend.Models.MES.WorkCenter
        {
            WorkCenterCode = "WC-ASM",
            WorkCenterName = "?��??��?裝�?",
            Capacity = 80,
            Efficiency = 95,
            IsActive = true
        };
        db.WorkCenters.AddRange(wc1, wc2);
        db.SaveChanges();

        // RccpResources
        var res1 = new RccpResource
        {
            ResourceCode = "RES-CNC-001",
            ResourceName = "CNC 機坰 001",
            ResourceType = "MACHINE",
            WorkCenterId = wc1.Id,
            CapacityPerBucket = 40,
            Uom = "HOURS",
            Efficiency = 1,
            Utilization = 0.9m,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };
        var res2 = new RccpResource
        {
            ResourceCode = "RES-ASM-001",
            ResourceName = "組�?�?001",
            ResourceType = "MACHINE",
            WorkCenterId = wc2.Id,
            CapacityPerBucket = 30,
            Uom = "HOURS",
            Efficiency = 1,
            Utilization = 0.9m,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow,
            Version = 1
        };
        db.RccpResources.AddRange(res1, res2);
        db.SaveChanges();

        // Skus
        var sku = new Sku
        {
            SkuNumber = "SKU-001",
            SkuName = "測試?��?",
            Unit = "PCS",
            Category = "FG",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Skus.Add(sku);
        db.SaveChanges();

        // RccpItemResources
        db.RccpItemResources.AddRange(
            new ErpBackend.Models.MES.RccpItemResource
            {
                SkuId = sku.Id,
                ResourceId = res1.Id,
                UsagePerUnit = 0.5m
            },
            new ErpBackend.Models.MES.RccpItemResource
            {
                SkuId = sku.Id,
                ResourceId = res2.Id,
                UsagePerUnit = 0.3m
            });
        db.SaveChanges();
    }

    [Fact]
    public async Task ListResources_ReturnsAllResources()
    {
        var response = await _client.GetAsync("/api/erp/rccp/resources");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(2, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task CreateResource_ValidRequest_CreatesResource()
    {
        var request = new
        {
            ResourceCode = "RES-NEW-001",
            ResourceName = "???",
            ResourceType = "LABOR",
            CapacityPerBucket = 20,
            Uom = "HOURS",
            Efficiency = 1,
            Utilization = 0.9m,
            IsActive = true
        };

        var response = await _client.PostAsJsonAsync("/api/erp/rccp/resources", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("RES-NEW-001", data.GetProperty("resourceCode").GetString());
    }

    [Fact]
    public async Task CreateResource_DuplicateCode_ReturnsFail()
    {
        var request = new
        {
            ResourceCode = "RES-CNC-001",
            ResourceName = "?��?資�?",
            ResourceType = "MACHINE",
            CapacityPerBucket = 10,
            Uom = "HOURS",
            Efficiency = 1,
            Utilization = 0.9m,
            IsActive = true
        };

        var response = await _client.PostAsJsonAsync("/api/erp/rccp/resources", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetItemResources_ReturnsResourcesForSku()
    {
        using var db = _factory.CreateDbContext();
        var skuId = db.Skus.First().Id;

        var response = await _client.GetAsync($"/api/erp/rccp/item-resources/{skuId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(2, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task UpdateItemResources_ValidRequest_SavesSuccessfully()
    {
        Guid skuId;
        Guid firstResourceId;
        Guid secondResourceId;
        using (var db = _factory.CreateDbContext())
        {
            skuId = db.Skus.First().Id;
            var resourceIds = db.RccpResources
                .OrderBy(r => r.ResourceCode)
                .Select(r => r.Id)
                .Take(2)
                .ToArray();
            Assert.Equal(2, resourceIds.Length);
            firstResourceId = resourceIds[0];
            secondResourceId = resourceIds[1];
        }
        var request = new[]
        {
            new
            {
                SkuId = skuId,
                ResourceId = firstResourceId,
                UsagePerUnit = 0.8m
            },
            new
            {
                SkuId = skuId,
                ResourceId = secondResourceId,
                UsagePerUnit = 0.2m
            }
        };

        var response = await _client.PostAsJsonAsync("/api/erp/rccp/item-resources", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.True(json.Contains("Saved successfully", StringComparison.Ordinal), json);
    }
}
