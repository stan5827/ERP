using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;

namespace ErpBackend.IntegrationTests;

public class InventoryControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();

    public InventoryControllerIntegrationTests()
    {
        SeedInventoryData();
    }

    private void SeedInventoryData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.Database.ExecuteSqlRaw("DELETE FROM \"InventoryTransactions\"");
        db.Database.ExecuteSqlRaw("DELETE FROM \"Skus\"");
        db.Database.ExecuteSqlRaw("DELETE FROM \"Warehouses\"");
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var warehouse = new Warehouse
        {
            WarehouseCode = "WH-001",
            WarehouseName = "測試倉庫",
            Type = "normal",
            IsActive = true
        };
        db.Warehouses.Add(warehouse);
        db.SaveChanges();

        var sku = new Sku
        {
            SkuNumber = "SKU-001",
            SkuName = "測試物料 A",
            Unit = "PCS",
            Status = "active",
            Qty = 100,
            SafetyStock = 10,
            ReorderPoint = 20,
            MaxStock = 500,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Skus.Add(sku);
        db.SaveChanges();

        db.InventoryTransactions.AddRange(
            new InventoryTransaction
            {
                TrxId = "TRX-001",
                SkuId = sku.Id,
                Type = "IN",
                QtyChange = 100,
                QtyBefore = 0,
                QtyAfter = 100,
                WarehouseId = warehouse.Id,
                CreatedAt = DateTime.UtcNow
            },
            new InventoryTransaction
            {
                TrxId = "TRX-002",
                SkuId = sku.Id,
                Type = "IN",
                QtyChange = 50,
                QtyBefore = 100,
                QtyAfter = 150,
                WarehouseId = warehouse.Id,
                CreatedAt = DateTime.UtcNow
            },
            new InventoryTransaction
            {
                TrxId = "TRX-003",
                SkuId = sku.Id,
                Type = "OUT",
                QtyChange = -30,
                QtyBefore = 150,
                QtyAfter = 120,
                WarehouseId = warehouse.Id,
                CreatedAt = DateTime.UtcNow
            });
        db.SaveChanges();
    }

    [Fact]
    public async Task Ping_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/erp/inventory/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("InventoryController operational", body);
    }

    [Fact]
    public async Task GetSkuBalance_ExistingSku_ReturnsBalance()
    {
        var response = await _client.GetAsync("/api/erp/inventory/balance/SKU-001");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("SKU-001", data.GetProperty("sku_code").GetString());
        Assert.Equal("測試物料 A", data.GetProperty("sku_name").GetString());
        Assert.Equal(120m, data.GetProperty("total_on_hand").GetDecimal());
    }

    [Fact]
    public async Task GetSkuBalance_NonExistentSku_ReturnsError()
    {
        var response = await _client.GetAsync("/api/erp/inventory/balance/NONEXISTENT");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("物料不存在", data.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CheckATP_ExistingSku_ReturnsAvailability()
    {
        var response = await _client.GetAsync("/api/erp/inventory/atp?skuCode=SKU-001&requestedQty=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("SKU-001", data.GetProperty("sku_code").GetString());
        Assert.True(data.GetProperty("available").GetDecimal() >= 0);
    }

    [Fact]
    public async Task LowStock_ReturnsItems()
    {
        var response = await _client.GetAsync("/api/erp/inventory/low-stock");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task Valuation_ReturnsTotalValue()
    {
        var response = await _client.GetAsync("/api/erp/inventory/valuation");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.True(data.TryGetProperty("totalValue", out _));
    }

    [Fact]
    public async Task AdjustBalance_ValidRequest_ReturnsOk()
    {
        var request = new
        {
            SkuCode = "SKU-001",
            WarehouseCode = "WH-001",
            QtyChange = 10,
            Notes = "測試調整"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/inventory/adjust", request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Adjust returned {(int)response.StatusCode}: {body}");
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("SKU-001", data.GetProperty("sku_code").GetString());
    }

    [Fact]
    public async Task Stocktake_PostsReviewedDifferenceAndRejectsStaleSystemQuantity()
    {
        const string lotNumber = "LOT-COUNT-001";
        const string expiryDate = "2028-04-30";
        var balanceResponse = await _client.GetAsync(
            $"/api/erp/inventory/stocktake/balance?skuCode=SKU-001&warehouseCode=WH-001&lotNumber={lotNumber}");
        Assert.Equal(HttpStatusCode.OK, balanceResponse.StatusCode);

        using var balanceDocument = JsonDocument.Parse(await balanceResponse.Content.ReadAsStringAsync());
        Assert.Equal(0m, balanceDocument.RootElement.GetProperty("data").GetProperty("systemQty").GetDecimal());

        var request = new
        {
            SkuCode = "SKU-001",
            WarehouseCode = "WH-001",
            LotNumber = lotNumber,
            ExpiryDate = expiryDate,
            CountedQty = 4,
            ExpectedSystemQty = 0,
            Notes = "測試盤點"
        };
        var postResponse = await _client.PostAsJsonAsync("/api/erp/inventory/stocktake", request);
        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

        using var postDocument = JsonDocument.Parse(await postResponse.Content.ReadAsStringAsync());
        var result = postDocument.RootElement.GetProperty("data");
        Assert.Equal(4m, result.GetProperty("qtyAfter").GetDecimal());
        Assert.Equal(4m, result.GetProperty("qtyChange").GetDecimal());
        Assert.Equal(expiryDate, result.GetProperty("expiryDate").GetDateTime().ToString("yyyy-MM-dd"));
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("trxId").GetString()));

        var staleResponse = await _client.PostAsJsonAsync("/api/erp/inventory/stocktake", request);
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);

        using var db = _factory.CreateDbContext();
        var inventoryLot = await db.InventoryLots.SingleAsync(item => item.LotNumber == lotNumber);
        Assert.Equal(4m, inventoryLot.Qty);
        Assert.Equal(new DateTime(2028, 4, 30), inventoryLot.ExpiryDate);
        var stocktakeTransaction = await db.InventoryTransactions.SingleAsync(
            item => item.RelatedDocType == "Stocktake" && item.LotNumber == lotNumber);
        Assert.Equal(120m, stocktakeTransaction.QtyBefore);
        Assert.Equal(124m, stocktakeTransaction.QtyAfter);
    }

    [Fact]
    public async Task PostTransaction_ValidRequest_ReturnsOk()
    {
        var request = new
        {
            SkuCode = "SKU-001",
            WarehouseCode = "WH-001",
            Type = "IN",
            Qty = 20,
            Notes = "測試入庫"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/inventory/transaction", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("SKU-001", data.GetProperty("sku_code").GetString());
    }
}
