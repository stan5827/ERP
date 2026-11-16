using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Models.Inventory;

namespace ErpBackend.IntegrationTests;

public class ApsIntegrationControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();

    public ApsIntegrationControllerIntegrationTests()
    {
        SeedApsData();
    }

    private void SeedApsData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.WorkOrderOperations.RemoveRange(db.WorkOrderOperations);
        db.ApsScheduleResults.RemoveRange(db.ApsScheduleResults);
        db.WorkOrders.RemoveRange(db.WorkOrders);
        db.RccpResources.RemoveRange(db.RccpResources);
        db.BomItems.RemoveRange(db.BomItems);
        db.Boms.RemoveRange(db.Boms);
        db.Skus.RemoveRange(db.Skus);
        db.SaveChanges();

        var skuParent = new Sku
        {
            SkuNumber = "SKU-PARENT",
            SkuName = "組裝品 A",
            Unit = "SET",
            Category = "FG",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var skuChild = new Sku
        {
            SkuNumber = "SKU-CHILD",
            SkuName = "零組件 B",
            Unit = "PCS",
            Category = "RM",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Skus.AddRange(skuParent, skuChild);
        db.SaveChanges();

        var bom = new Bom
        {
            BomCode = "BOM-001",
            ProductSkuId = skuParent.Id,
            Version = "1.0",
            Status = "active",
            EffectiveDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Boms.Add(bom);
        db.SaveChanges();

        db.BomItems.Add(new BomItem
        {
            BomId = bom.Id,
            ChildSkuId = skuChild.Id,
            Quantity = 2,
            Unit = "PCS",
            ScrapRate = 0,
            OperationSeq = 1
        });
        db.SaveChanges();

        db.RccpResources.AddRange(
            new RccpResource
            {
                ResourceCode = "RES-CNC",
                ResourceName = "CNC 機台",
                ResourceType = "MACHINE",
                CapacityPerBucket = 40,
                Uom = "HOURS",
                Efficiency = 1,
                Utilization = 0.9m,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            },
            new RccpResource
            {
                ResourceCode = "RES-ASM",
                ResourceName = "組裝線",
                ResourceType = "MACHINE",
                CapacityPerBucket = 30,
                Uom = "HOURS",
                Efficiency = 1,
                Utilization = 0.9m,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow,
                Version = 1
            });
        db.SaveChanges();

        var wo1 = new WorkOrder
        {
            WoNumber = "WO-001",
            WoType = "regular",
            ProductSkuId = skuParent.Id,
            PlannedQty = 100,
            Status = "pending",
            Priority = "normal",
            PlannedStartDate = DateTime.UtcNow.AddDays(-1),
            PlannedEndDate = DateTime.UtcNow.AddDays(5),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var wo2 = new WorkOrder
        {
            WoNumber = "WO-002",
            WoType = "regular",
            ProductSkuId = skuParent.Id,
            PlannedQty = 50,
            Status = "released",
            Priority = "high",
            PlannedStartDate = DateTime.UtcNow.AddDays(1),
            PlannedEndDate = DateTime.UtcNow.AddDays(10),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.WorkOrders.AddRange(wo1, wo2);
        db.SaveChanges();

        db.WorkOrderOperations.AddRange(
            new WorkOrderOperation
            {
                WorkOrderId = wo1.Id,
                OperationSeq = 10,
                OperationId = Guid.NewGuid(),
                WorkCenterId = Guid.NewGuid(),
                PlannedQty = 100,
                CompletedQty = 0,
                Status = "pending"
            },
            new WorkOrderOperation
            {
                WorkOrderId = wo2.Id,
                OperationSeq = 10,
                OperationId = Guid.NewGuid(),
                WorkCenterId = Guid.NewGuid(),
                PlannedQty = 50,
                CompletedQty = 0,
                Status = "pending"
            });
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");
    }

    [Fact]
    public async Task ExportData_ReturnsSkusBomsResourcesWorkOrders()
    {
        var response = await _client.GetAsync("/api/erp/aps/export");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(200, root.GetProperty("code").GetInt32());
        var data = root.GetProperty("data");

        Assert.Equal(2, data.GetProperty("items").EnumerateArray().Count());
        Assert.Equal(1, data.GetProperty("boms").EnumerateArray().Count());
        Assert.Equal(2, data.GetProperty("resources").EnumerateArray().Count());
        Assert.Equal(2, data.GetProperty("workOrders").EnumerateArray().Count());
    }

    [Fact]
    public async Task ExportData_WithDateRange_FiltersWorkOrders()
    {
        var fromDate = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.AddDays(8).ToString("yyyy-MM-dd");
        var response = await _client.GetAsync($"/api/erp/aps/export?fromDate={fromDate}&toDate={toDate}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(200, root.GetProperty("code").GetInt32());
        var data = root.GetProperty("data");
        Assert.True(data.GetProperty("workOrders").EnumerateArray().Count() <= 2);
    }

    [Fact]
    public async Task ImportSchedule_ValidRequest_ImportsSuccessfully()
    {
        var request = new
        {
            ScheduleData = "test-schedule-data",
            ScheduleVersion = "v1.0",
            Results = new[]
            {
                new
                {
                    OrderNo = "WO-001",
                    OperationSeq = 10,
                    ResourceCode = "RES-CNC",
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(2),
                    Sequence = 0
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/erp/aps/import-schedule", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(200, root.GetProperty("code").GetInt32());
        Assert.Contains("v1.0", root.GetProperty("message").GetString()!);
    }

    [Fact]
    public async Task ImportSchedule_MissingScheduleVersion_ReturnsBadRequest()
    {
        var request = new
        {
            ScheduleData = "",
            ScheduleVersion = "",
            Results = new List<object>()
        };

        var response = await _client.PostAsJsonAsync("/api/erp/aps/import-schedule", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
