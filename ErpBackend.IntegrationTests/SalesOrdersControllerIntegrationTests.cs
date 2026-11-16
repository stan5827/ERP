using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.Sales;

namespace ErpBackend.IntegrationTests;

[CollectionDefinition("SalesOrders", DisableParallelization = true)]
public sealed class SalesOrdersCollection { }

[Collection("SalesOrders")]
public class SalesOrdersControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();
    private readonly Guid _orderId;

    public SalesOrdersControllerIntegrationTests()
    {
        _orderId = SeedViaApi();
    }

    private Guid SeedViaApi()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.SalesOrderItems.RemoveRange(db.SalesOrderItems);
        db.SalesOrders.RemoveRange(db.SalesOrders);
        db.Customers.RemoveRange(db.Customers);
        db.Skus.RemoveRange(db.Skus);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var customer = new ErpBackend.Models.Sales.Customer
        {
            CustomerCode = "CUST-001",
            CustomerName = "測試客戶 A",
            CreditLimit = 100000,
            CreditStatus = "normal",
            Currency = "TWD",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Customers.Add(customer);
        db.SaveChanges();

        var sku = new ErpBackend.Models.Inventory.Sku
        {
            SkuNumber = "SKU-SO-001",
            SkuName = "銷售測試物料",
            Unit = "PCS",
            SalesUomCode = "PCS",
            ProductionUomCode = "PCS",
            Status = "active",
            Qty = 1000,
            SafetyStock = 10,
            ReorderPoint = 20,
            MaxStock = 5000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Skus.Add(sku);
        db.SaveChanges();

        var request = new
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            CustomerPoNumber = "PO-123",
            PaymentTerms = "30 days",
            Items = new[]
            {
                new
                {
                    SkuId = sku.Id,
                    Quantity = 5,
                    UnitPrice = 100,
                    DiscountRate = 0
                }
            }
        };

        var resp = _client.PostAsJsonAsync("/api/erp/sales-orders", request).Result;
        resp.EnsureSuccessStatusCode();

        var json = resp.Content.ReadAsStringAsync().Result;
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        return data.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task GetSalesOrders_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/sales-orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.True(data.ValueKind == JsonValueKind.Array);
        Assert.True(data.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetSalesOrders_WithStatusFilter_ReturnsFiltered()
    {
        var response = await _client.GetAsync("/api/erp/sales-orders?status=pending");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetSalesOrder_ExistingId_ReturnsOrder()
    {
        var response = await _client.GetAsync($"/api/erp/sales-orders/{_orderId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.False(string.IsNullOrEmpty(data.GetProperty("orderNumber").GetString()));
        Assert.Equal("pending", data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetSalesOrder_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/erp/sales-orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateSalesOrder_ValidRequest_ReturnsCreatedOrder()
    {
        using var db = _factory.CreateDbContext();
        var customerId = db.Customers.First().Id;
        var skuId = db.Skus.First().Id;

        var request = new
        {
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            CustomerPoNumber = "PO-456",
            PaymentTerms = "30 days",
            Items = new[]
            {
                new
                {
                    SkuId = skuId,
                    Quantity = 3,
                    UnitPrice = 200,
                    DiscountRate = 0
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/erp/sales-orders", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.NotEqual(Guid.Empty, data.GetProperty("id").GetGuid());
        Assert.False(string.IsNullOrEmpty(data.GetProperty("orderNumber").GetString()));
    }

    [Fact]
    public async Task ConfirmSalesOrder_ValidId_ReturnsOk()
    {
        using (var db = _factory.CreateDbContext())
        {
            var order = await db.SalesOrders
                .Include(salesOrder => salesOrder.Items)
                .SingleAsync(salesOrder => salesOrder.Id == _orderId);
            var customer = await db.Customers.SingleAsync(value => value.Id == order.CustomerId);
            var sku = await db.Skus.SingleAsync(value => value.Id == order.Items.First().SkuId!.Value);
            customer.CreditLimit = 100;
            sku.Qty = 3;
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsync($"/api/erp/sales-orders/{_orderId}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("已確認", json);
        using (var document = JsonDocument.Parse(json))
        {
            var result = document.RootElement.GetProperty("data");
            Assert.True(result.GetProperty("creditLimitExceeded").GetBoolean());
            var warning = Assert.Single(result.GetProperty("inventoryWarnings").EnumerateArray());
            Assert.Equal(3, warning.GetProperty("availableQty").GetDecimal());
            Assert.Equal(2, warning.GetProperty("shortageQty").GetDecimal());
        }

        using var verifyDb = _factory.CreateDbContext();
        var confirmedOrder = await verifyDb.SalesOrders.SingleAsync(order => order.Id == _orderId);
        Assert.Equal("confirmed", confirmedOrder.Status);
        Assert.True(confirmedOrder.CreditChecked);
        Assert.NotNull(confirmedOrder.ConfirmedAt);
        Assert.NotNull(confirmedOrder.UpdatedAt);
    }

    [Fact]
    public async Task CreateWorkOrdersFromSalesOrder_CreatesLinkedWorkOrderAndDoesNotDuplicate()
    {
        Guid skuId;
        Guid salesOrderItemId;
        using (var db = _factory.CreateDbContext())
        {
            var order = await db.SalesOrders
                .Include(salesOrder => salesOrder.Items)
                .SingleAsync(salesOrder => salesOrder.Id == _orderId);
            order.Status = "confirmed";
            var item = Assert.Single(order.Items);
            item.NeedManufacturing = true;
            skuId = item.SkuId!.Value;
            salesOrderItemId = item.Id;

            var component = new Sku
            {
                SkuNumber = "SKU-COMP-001",
                SkuName = "工單測試用料",
                Unit = "PCS",
                Status = "active",
                Qty = 3,
                SafetyStock = 0,
                ReorderPoint = 0,
                MaxStock = 1000,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var bom = new Bom
            {
                BomCode = "BOM-SO-WO-001",
                ProductSkuId = skuId,
                Version = "1.0",
                Status = "active",
                EffectiveDate = DateTime.UtcNow.AddDays(-1)
            };
            bom.Items.Add(new BomItem
            {
                BomId = bom.Id,
                ChildSkuId = component.Id,
                Quantity = 2,
                Unit = "PCS"
            });
            var workCenter = new WorkCenter
            {
                WorkCenterCode = "WC-SO-WO-001",
                WorkCenterName = "工單測試工作中心",
                Capacity = 100,
                Efficiency = 100,
                IsActive = true
            };
            var operation = new Operation
            {
                OperationCode = "OP-SO-WO-001",
                OperationName = "工單測試工序",
                IsActive = true
            };
            var routing = new Routing
            {
                RoutingCode = "RT-SO-WO-001",
                RoutingName = "工單測試路由",
                ProductSkuId = skuId,
                IsActive = true
            };
            routing.Operations.Add(new RoutingOperation
            {
                RoutingId = routing.Id,
                OperationSeq = 1,
                OperationId = operation.Id,
                WorkCenterId = workCenter.Id
            });
            db.AddRange(component, bom, workCenter, operation, routing);
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsync(
            $"/api/erp/work-orders/from-sales-order/{_orderId}",
            content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        using (var document = JsonDocument.Parse(responseBody))
        {
            var workOrders = document.RootElement.GetProperty("data");
            Assert.True(workOrders.GetArrayLength() > 0, responseBody);
            var workOrder = Assert.Single(workOrders.EnumerateArray());
            var material = Assert.Single(workOrder.GetProperty("materials").EnumerateArray());
            Assert.Equal(10, material.GetProperty("requiredQty").GetDecimal());
            Assert.Equal(3, material.GetProperty("availableQty").GetDecimal());
            Assert.Equal(7, material.GetProperty("shortageQty").GetDecimal());
            Assert.Equal("shortage", material.GetProperty("status").GetString());
        }

        using (var db = _factory.CreateDbContext())
        {
            var workOrder = await db.WorkOrders.SingleAsync(order => order.SalesOrderId == _orderId);
            Assert.Equal(skuId, workOrder.ProductSkuId);
            Assert.Equal("planned", workOrder.Status);
            Assert.Equal(5, workOrder.PlannedQty);
            Assert.True(workOrder.BomId.HasValue);
            Assert.Contains(await db.WorkOrderMaterials.ToListAsync(), material =>
                material.WorkOrderId == workOrder.Id && material.RequiredQty == 10
                    && material.ShortageQty == 7 && material.Status == "shortage");
            Assert.Single(await db.WorkOrderOperations
                .Where(operation => operation.WorkOrderId == workOrder.Id)
                .ToListAsync());
            Assert.Contains(await db.SalesOrderProductionLinks.ToListAsync(), link =>
                link.SalesOrderItemId == salesOrderItemId && link.ProductionDocId == workOrder.Id);
            Assert.Equal("sent", (await db.SalesOrders.SingleAsync(order => order.Id == _orderId)).ManufacturingStatus);
        }

        var detailResponse = await _client.GetAsync(
            $"/api/erp/work-orders/{(await GetSalesOrderWorkOrderIdAsync())}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        using (var detailDocument = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync()))
        {
            var data = detailDocument.RootElement.GetProperty("data");
            var operations = data.GetProperty("operations");
            var operation = Assert.Single(operations.EnumerateArray());
            Assert.Equal("工單測試工序", operation.GetProperty("operationName").GetString());
            Assert.Equal("工單測試工作中心", operation.GetProperty("workCenterName").GetString());
            var material = Assert.Single(data.GetProperty("materials").EnumerateArray());
            Assert.Equal(10, material.GetProperty("requiredQty").GetDecimal());
            Assert.Equal(3, material.GetProperty("availableQty").GetDecimal());
            Assert.Equal(7, material.GetProperty("shortageQty").GetDecimal());
        }

        var replayResponse = await _client.PostAsync(
            $"/api/erp/work-orders/from-sales-order/{_orderId}",
            content: null);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);

        using var verifyDb = _factory.CreateDbContext();
        Assert.Single(await verifyDb.WorkOrders.Where(order => order.SalesOrderId == _orderId).ToListAsync());
    }

    private async Task<Guid> GetSalesOrderWorkOrderIdAsync()
    {
        using var db = _factory.CreateDbContext();
        var workOrder = await db.WorkOrders.SingleAsync(order => order.SalesOrderId == _orderId);
        return workOrder.Id;
    }
}
