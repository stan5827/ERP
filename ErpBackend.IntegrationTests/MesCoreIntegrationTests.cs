using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.MES;
using ErpBackend.Models.Inventory;

namespace ErpBackend.IntegrationTests;

public class MesCoreIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();
    private Guid _skuId;
    private Guid _childSkuId;
    private Guid _workCenterId;
    private Guid _operationId;

    public MesCoreIntegrationTests()
    {
        SeedMesCoreData();
    }

    private void SeedMesCoreData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.RoutingOperations.RemoveRange(db.RoutingOperations);
        db.BomItems.RemoveRange(db.BomItems);
        db.WorkOrderOperations.RemoveRange(db.WorkOrderOperations);
        db.WorkCenterCosts.RemoveRange(db.WorkCenterCosts);
        db.Routings.RemoveRange(db.Routings);
        db.Boms.RemoveRange(db.Boms);
        db.WorkOrders.RemoveRange(db.WorkOrders);
        db.WorkCenters.RemoveRange(db.WorkCenters);
        db.CostCenters.RemoveRange(db.CostCenters);
        db.Operations.RemoveRange(db.Operations);
        db.Skus.RemoveRange(db.Skus);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        // SKUs
        var sku = new Sku
        {
            SkuNumber = "SKU-MES-001",
            SkuName = "MES 測試成品",
            Unit = "SET",
            Category = "FG",
            StandardCost = 500,
            SourceType = "self_made",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var childSku = new Sku
        {
            SkuNumber = "SKU-MES-CHILD",
            SkuName = "MES 測試子件",
            Unit = "PCS",
            ProductionUomCode = "PCS",
            Category = "RM",
            StandardCost = 100,
            SourceType = "purchased",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Skus.AddRange(sku, childSku);
        db.SaveChanges();
        _skuId = sku.Id;
        _childSkuId = childSku.Id;

        // WorkCenter
        var wc = new WorkCenter
        {
            WorkCenterCode = "WC-MES",
            WorkCenterName = "MES 工作中心",
            Capacity = 100,
            Efficiency = 90,
            IsActive = true
        };
        db.WorkCenters.Add(wc);
        db.SaveChanges();
        _workCenterId = wc.Id;

        // CostCenter (required by WorkCenterCost FK)
        var cc = new CostCenter
        {
            CostCenterCode = "CC-MES",
            CostCenterName = "MES 成本中心",
            BudgetLimit = 10000,
            IsActive = true
        };
        db.CostCenters.Add(cc);
        db.SaveChanges();

        // WorkCenterCost
        db.WorkCenterCosts.Add(new WorkCenterCost
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wc.Id,
            CostCenterId = cc.Id,
            LaborRate = 150,
            MachineRate = 200,
            OverheadRate = 50,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            EffectiveTo = null,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();

        // Operation
        var op = new Operation
        {
            OperationCode = "OP-MES",
            OperationName = "MES 測試作業",
            SetupTime = 0.5m,
            RunTime = 2,
            CostPerHour = 50,
            IsActive = true
        };
        db.Operations.Add(op);
        db.SaveChanges();
        _operationId = op.Id;

        // WorkOrder
        var wo = new WorkOrder
        {
            WoNumber = "WO-MES-001",
            WoType = "regular",
            ProductSkuId = sku.Id,
            PlannedQty = 100,
            ActualQty = 0,
            Status = "planned",
            Priority = "normal",
            PlannedStartDate = DateTime.UtcNow.AddDays(-1),
            PlannedEndDate = DateTime.UtcNow.AddDays(5),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.WorkOrders.Add(wo);
        db.SaveChanges();

        // WorkOrderOperation
        db.WorkOrderOperations.Add(new WorkOrderOperation
        {
            WorkOrderId = wo.Id,
            OperationSeq = 10,
            OperationId = op.Id,
            WorkCenterId = wc.Id,
            PlannedQty = 100,
            CompletedQty = 0,
            Status = "pending"
        });
        db.SaveChanges();

        // Bom
        var bom = new Bom
        {
            BomCode = "BOM-MES-001",
            ProductSkuId = sku.Id,
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
            ChildSkuId = childSku.Id,
            Quantity = 2,
            Unit = "PCS",
            ScrapRate = 0,
            OperationSeq = 1
        });
        db.SaveChanges();

        // Routing
        var routing = new Routing
        {
            RoutingCode = "RT-MES-001",
            RoutingName = "MES 測試工藝",
            ProductSkuId = sku.Id,
            Version = "1.0",
            IsActive = true
        };
        db.Routings.Add(routing);
        db.SaveChanges();

        db.RoutingOperations.Add(new RoutingOperation
        {
            RoutingId = routing.Id,
            OperationSeq = 10,
            OperationId = op.Id,
            WorkCenterId = wc.Id,
            TransferTime = 0,
            WaitTime = 0
        });
        db.SaveChanges();
    }

    #region WorkOrders

    [Fact]
    public async Task GetWorkOrders_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/work-orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(1, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task GetWorkOrder_ExistingId_ReturnsDto()
    {
        using var db = _factory.CreateDbContext();
        var woId = db.WorkOrders.First().Id;

        var response = await _client.GetAsync($"/api/erp/work-orders/{woId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("WO-MES-001", data.GetProperty("woNumber").GetString());
    }

    [Fact]
    public async Task GetWorkOrder_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/erp/work-orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkOrder_ValidRequest_CreatesWorkOrder()
    {
        var request = new
        {
            ProductSkuId = _skuId,
            PlannedQty = 50,
            Priority = "high",
            PlannedStartDate = DateTime.UtcNow,
            PlannedEndDate = DateTime.UtcNow.AddDays(3)
        };

        var response = await _client.PostAsJsonAsync("/api/erp/work-orders", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Contains("工單已建立", root.GetProperty("message").GetString()!);
    }

    [Fact]
    public async Task CreateWorkOrder_InvalidSku_ReturnsBadRequest()
    {
        var request = new
        {
            ProductSkuId = Guid.NewGuid(),
            PlannedQty = 50
        };

        var response = await _client.PostAsJsonAsync("/api/erp/work-orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkOrder_ValidRequest_Updates()
    {
        using var db = _factory.CreateDbContext();
        var woId = db.WorkOrders.First().Id;

        var request = new
        {
            PlannedQty = 200,
            Priority = "urgent"
        };

        var response = await _client.PutAsJsonAsync($"/api/erp/work-orders/{woId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Contains("工單已更新", root.GetProperty("message").GetString()!);
    }

    [Fact]
    public async Task UpdateWorkOrder_UnknownId_ReturnsNotFound()
    {
        var request = new { PlannedQty = 200 };
        var response = await _client.PutAsJsonAsync($"/api/erp/work-orders/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkOrder_PlannedStatus_Deletes()
    {
        using var db = _factory.CreateDbContext();
        var wo = new WorkOrder
        {
            WoNumber = "WO-MES-DELETE",
            WoType = "regular",
            ProductSkuId = _skuId,
            PlannedQty = 10,
            Status = "planned",
            Priority = "normal",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.WorkOrders.Add(wo);
        db.SaveChanges();
        var woId = wo.Id;

        var response = await _client.DeleteAsync($"/api/erp/work-orders/{woId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkOrder_CompletedStatus_Guarded()
    {
        using var db = _factory.CreateDbContext();
        var wo = new WorkOrder
        {
            WoNumber = "WO-MES-GUARD",
            WoType = "regular",
            ProductSkuId = _skuId,
            PlannedQty = 10,
            Status = "completed",
            Priority = "normal",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.WorkOrders.Add(wo);
        db.SaveChanges();
        var woId = wo.Id;

        var response = await _client.DeleteAsync($"/api/erp/work-orders/{woId}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region WorkCenters

    [Fact]
    public async Task GetWorkCenters_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/work-centers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(1, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task GetWorkCenter_ExistingId_ReturnsDto()
    {
        using var db = _factory.CreateDbContext();
        var wcId = db.WorkCenters.First().Id;

        var response = await _client.GetAsync($"/api/erp/work-centers/{wcId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("WC-MES", data.GetProperty("workCenterCode").GetString());
    }

    [Fact]
    public async Task GetWorkCenter_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/erp/work-centers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkCenter_ValidRequest_Creates()
    {
        var request = new
        {
            WorkCenterCode = "WC-MES-002",
            WorkCenterName = "MES 工作中心 002",
            Capacity = 80,
            Efficiency = 85,
            IsActive = true
        };

        var response = await _client.PostAsJsonAsync("/api/erp/work-centers", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateWorkCenter_DuplicateCode_ReturnsBadRequest()
    {
        var request = new
        {
            WorkCenterCode = "WC-MES",
            WorkCenterName = "重複代碼",
            Capacity = 50,
            Efficiency = 50,
            IsActive = true
        };

        var response = await _client.PostAsJsonAsync("/api/erp/work-centers", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkCenter_ValidRequest_Updates()
    {
        using var db = _factory.CreateDbContext();
        var wcId = db.WorkCenters.First().Id;

        var request = new
        {
            WorkCenterName = "更新後的工作中心",
            Capacity = 150
        };

        var response = await _client.PutAsJsonAsync($"/api/erp/work-centers/{wcId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Routings

    [Fact]
    public async Task GetRoutings_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/routings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(1, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task GetRouting_ExistingId_ReturnsDto()
    {
        using var db = _factory.CreateDbContext();
        var routingId = db.Routings.First().Id;

        var response = await _client.GetAsync($"/api/erp/routings/{routingId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("RT-MES-001", data.GetProperty("routingCode").GetString());
    }

    [Fact]
    public async Task GetRouting_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/erp/routings/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateRouting_ValidRequest_Creates()
    {
        var request = new
        {
            RoutingCode = "RT-MES-002",
            RoutingName = "MES 測試工藝 002",
            ProductSkuId = _skuId,
            Version = "1.0",
            IsActive = true,
            Operations = new[]
            {
                new
                {
                    OperationSeq = 10,
                    OperationId = _operationId,
                    WorkCenterId = _workCenterId,
                    TransferTime = 0,
                    WaitTime = 0
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/erp/routings", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRouting_ValidRequest_Updates()
    {
        using var db = _factory.CreateDbContext();
        var routingId = db.Routings.First().Id;

        var request = new
        {
            RoutingName = "更新後的工藝",
            Version = "2.0",
            Operations = new[]
            {
                new
                {
                    OperationSeq = 10,
                    OperationId = _operationId,
                    WorkCenterId = _workCenterId,
                    TransferTime = 0,
                    WaitTime = 0
                }
            }
        };

        var response = await _client.PutAsJsonAsync($"/api/erp/routings/{routingId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRouting_UnknownId_ReturnsNotFound()
    {
        var request = new { RoutingName = "測試" };
        var response = await _client.PutAsJsonAsync($"/api/erp/routings/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region BOMs

    [Fact]
    public async Task GetBoms_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/boms");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(1, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task GetBom_ExistingId_ReturnsDto()
    {
        using var db = _factory.CreateDbContext();
        var bomId = db.Boms.First().Id;

        var response = await _client.GetAsync($"/api/erp/boms/{bomId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("BOM-MES-001", data.GetProperty("bomCode").GetString());
        Assert.Equal(1, data.GetProperty("items").EnumerateArray().Count());
    }

    [Fact]
    public async Task GetBom_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/erp/boms/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBom_ValidRequest_Creates()
    {
        var request = new
        {
            BomCode = "BOM-MES-002",
            ProductSkuId = _skuId,
            Version = "1.0",
            Status = "draft",
            Notes = "測試 BOM",
            Items = new[]
            {
                new
                {
                    ChildSkuId = _childSkuId,
                    Quantity = 3,
                    Unit = "PCS",
                    ScrapRate = 0,
                    OperationSeq = 1
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/erp/boms", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateBom_DuplicateCode_ReturnsBadRequest()
    {
        var request = new
        {
            BomCode = "BOM-MES-001",
            ProductSkuId = _skuId,
            Version = "1.0",
            Status = "draft",
            Items = new[]
            {
                new
                {
                    ChildSkuId = _childSkuId,
                    Quantity = 1,
                    Unit = "PCS",
                    ScrapRate = 0,
                    OperationSeq = 1
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/erp/boms", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBom_SelfReference_ReturnsBadRequest()
    {
        var request = new
        {
            BomCode = "BOM-MES-SELF",
            ProductSkuId = _skuId,
            Version = "1.0",
            Status = "draft",
            Items = new[]
            {
                new
                {
                    ChildSkuId = _skuId,
                    Quantity = 1,
                    Unit = "PCS",
                    ScrapRate = 0,
                    OperationSeq = 1
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/erp/boms", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBom_ValidRequest_Updates()
    {
        using var db = _factory.CreateDbContext();
        var bomId = db.Boms.First().Id;

        var request = new
        {
            BomCode = "BOM-MES-UPDATED",
            Version = "2.0",
            Status = "active",
            Items = new[]
            {
                new
                {
                    ChildSkuId = _childSkuId,
                    Quantity = 5,
                    Unit = "PCS",
                    ScrapRate = 0,
                    OperationSeq = 1
                }
            }
        };

        var response = await _client.PutAsJsonAsync($"/api/erp/boms/{bomId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBom_ValidRequest_Deletes()
    {
        using var db = _factory.CreateDbContext();
        var bom = new Bom
        {
            BomCode = "BOM-MES-DELETE",
            ProductSkuId = _skuId,
            Version = "1.0",
            Status = "draft",
            EffectiveDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Boms.Add(bom);
        db.SaveChanges();
        var bomId = bom.Id;

        var response = await _client.DeleteAsync($"/api/erp/boms/{bomId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ActivateBom_ValidRequest_Activates()
    {
        using var db = _factory.CreateDbContext();
        var bomId = db.Boms.First().Id;

        var response = await _client.PatchAsync($"/api/erp/boms/{bomId}/activate", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CostRollup_ValidRequest_ReturnsCost()
    {
        using var db = _factory.CreateDbContext();
        var bomId = db.Boms.First().Id;

        var response = await _client.GetAsync($"/api/erp/boms/{bomId}/cost-rollup");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.True(data.TryGetProperty("totalCost", out _));
    }

    #endregion
}
