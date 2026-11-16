using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;

namespace ErpBackend.Services;

public sealed class InventorySearchHandler : IToolHandler
{
    private readonly IErpIntegrationService _erpIntegration;

    public InventorySearchHandler(IErpIntegrationService erpIntegration) => _erpIntegration = erpIntegration;

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters) =>
        _erpIntegration.SearchInventoryAsync(
            parameters.GetValueOrDefault("sku_code")?.ToString(),
            parameters.GetValueOrDefault("warehouse_id")?.ToString());
}

public sealed class LowStockCheckHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public LowStockCheckHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var results = await _db.Skus
            .Where(s => s.Qty <= s.SafetyStock)
            .Take(50)
            .Select(s => new
            {
                s.SkuNumber,
                s.SkuName,
                s.Qty,
                s.SafetyStock,
                Shortage = s.SafetyStock - s.Qty
            })
            .ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class BomSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public BomSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var skuIdStr = parameters.GetValueOrDefault("sku_id")?.ToString();
        Guid? skuId = string.IsNullOrEmpty(skuIdStr) ? null : Guid.Parse(skuIdStr);

        var query = _db.BomItems.Include(b => b.Bom).AsQueryable();

        if (skuId.HasValue)
            query = query.Where(b => b.Bom.ProductSkuId == skuId);

        var results = await query.Take(100).Select(b => new
        {
            b.BomId,
            b.ChildSkuId,
            b.Quantity,
            b.Unit,
            b.OperationSeq
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class WarehouseSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public WarehouseSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var warehouseCode = parameters.GetValueOrDefault("warehouse_code")?.ToString();

        var query = _db.Warehouses.AsQueryable();

        if (!string.IsNullOrEmpty(warehouseCode))
            query = query.Where(w => w.WarehouseCode.Contains(warehouseCode));

        var results = await query.Take(50).Select(w => new
        {
            w.WarehouseCode,
            w.WarehouseName,
            w.Type
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class SkuSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public SkuSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var skuNumber = parameters.GetValueOrDefault("sku_number")?.ToString();
        var skuName = parameters.GetValueOrDefault("sku_name")?.ToString();

        var query = _db.Skus.AsQueryable();

        if (!string.IsNullOrEmpty(skuNumber))
            query = query.Where(s => s.SkuNumber.Contains(skuNumber));
        if (!string.IsNullOrEmpty(skuName))
            query = query.Where(s => s.SkuName.Contains(skuName));

        var results = await query.Take(50).Select(s => new
        {
            s.SkuNumber,
            s.SkuName,
            s.Unit,
            s.StandardCost,
            s.SourceType
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class AdjustInventoryHandler : IToolHandler
{
    private readonly IErpIntegrationService _erpIntegration;

    public AdjustInventoryHandler(IErpIntegrationService erpIntegration) => _erpIntegration = erpIntegration;

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters) =>
        _erpIntegration.AdjustInventoryAsync(
            parameters.GetValueOrDefault("sku_id")?.ToString() ?? "",
            parameters.GetValueOrDefault("warehouse_id")?.ToString() ?? "",
            decimal.Parse(parameters.GetValueOrDefault("quantity")?.ToString() ?? "0"),
            parameters.GetValueOrDefault("reason")?.ToString() ?? "manual");
}

public sealed class WarehouseTransferHandler : IToolHandler
{
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        await Task.CompletedTask;
        var sourceWarehouseId = parameters.GetValueOrDefault("source_warehouse_id")?.ToString();
        var targetWarehouseId = parameters.GetValueOrDefault("target_warehouse_id")?.ToString();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { TransferId = Guid.NewGuid(), TransferNumber = $"TRF-{DateTime.Now:yyyyMMddHHmmss}", Status = "created" }) };
    }
}

public sealed class InventoryCountHandler : IToolHandler
{
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        await Task.CompletedTask;
        var warehouseId = parameters.GetValueOrDefault("warehouse_id")?.ToString();
        var skuId = parameters.GetValueOrDefault("sku_id")?.ToString();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { CountId = Guid.NewGuid(), Status = "in_progress", ItemsCount = 0 }) };
    }
}

public sealed class InventoryReportHandler : IToolHandler
{
    private readonly IErpIntegrationService _erpIntegration;

    public InventoryReportHandler(IErpIntegrationService erpIntegration) => _erpIntegration = erpIntegration;

    public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        DateTime? ParseDate(string key) =>
            DateTime.TryParse(parameters.GetValueOrDefault(key)?.ToString(), out var v) ? v : null;

        return _erpIntegration.GenerateInventoryReportAsync(
            parameters.GetValueOrDefault("warehouse_id")?.ToString(),
            ParseDate("date_from"),
            ParseDate("date_to"));
    }
}
