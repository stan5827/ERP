using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;

namespace ErpBackend.Services;

public sealed class RoutingSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public RoutingSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var routingCode = parameters.GetValueOrDefault("routing_code")?.ToString();
        var productSkuIdStr = parameters.GetValueOrDefault("product_sku_id")?.ToString();

        Guid? productSkuId = string.IsNullOrEmpty(productSkuIdStr) ? null : Guid.Parse(productSkuIdStr);
        var query = _db.Routings.AsQueryable();
        if (!string.IsNullOrEmpty(routingCode))
            query = query.Where(r => r.RoutingCode.Contains(routingCode));
        if (productSkuId.HasValue)
            query = query.Where(r => r.ProductSkuId == productSkuId);

        var results = await query.Take(50).Select(r => new { r.RoutingCode, r.RoutingName, r.IsActive }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class OperationSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public OperationSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var operationCode = parameters.GetValueOrDefault("operation_code")?.ToString();

        var query = _db.Operations.AsQueryable();
        if (!string.IsNullOrEmpty(operationCode))
            query = query.Where(o => o.OperationCode.Contains(operationCode));

        var results = await query.Take(50).Select(o => new { o.OperationCode, o.OperationName, o.IsActive }).ToListAsync();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class BomCostRollupHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public BomCostRollupHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var bomIdStr = parameters.GetValueOrDefault("bom_id")?.ToString();
        Guid? bomId = string.IsNullOrEmpty(bomIdStr) ? null : Guid.Parse(bomIdStr);

        var bomItems = await _db.BomItems.Where(b => bomId == null || b.BomId == bomId).Take(10).ToListAsync();
        var childSkuIds = bomItems.Select(b => b.ChildSkuId).ToList();
        var skuCosts = await _db.Skus.Where(s => childSkuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.StandardCost);
        var totalCost = bomItems.Sum(b => (b.Quantity * (skuCosts.GetValueOrDefault(b.ChildSkuId, 0))));

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { BomId = bomIdStr ?? "all", ItemCount = bomItems.Count, EstimatedMaterialCost = totalCost }) };
    }
}
