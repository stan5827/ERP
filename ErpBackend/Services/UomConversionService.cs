using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Services;

public class UomConversionService
{
    private readonly ErpGeneralDbContext _db;
    private const int DefaultDecimalPlaces = 6;

    public UomConversionService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<UomConversionResult> ConvertAsync(Guid skuId, decimal qty, string fromUomCode, string toUomCode)
    {
        if (fromUomCode == toUomCode)
        {
            return new UomConversionResult
            {
                OriginalQty = qty,
                OriginalUom = fromUomCode,
                ConvertedQty = qty,
                TargetUom = toUomCode,
                Factor = 1,
                Operator = "*",
                DecimalPlaces = DefaultDecimalPlaces
            };
        }

        var fromUom = await _db.Uoms.FirstOrDefaultAsync(u => u.UomCode == fromUomCode);
        var toUom = await _db.Uoms.FirstOrDefaultAsync(u => u.UomCode == toUomCode);

        if (fromUom == null || toUom == null)
        {
            throw new InvalidOperationException($"UOM not found: {fromUomCode} or {toUomCode}");
        }

        var (factor, @operator) = await GetConversionFactorAsync(skuId, fromUomCode, toUomCode);
        
        var convertedQty = Calculate(qty, factor, @operator);

        return new UomConversionResult
        {
            OriginalQty = qty,
            OriginalUom = fromUomCode,
            ConvertedQty = convertedQty,
            TargetUom = toUomCode,
            Factor = factor,
            Operator = @operator,
            DecimalPlaces = toUom.DecimalPlaces
        };
    }

    public async Task<(decimal factor, string @operator)> GetConversionFactorAsync(Guid skuId, string fromUomCode, string toUomCode)
    {
        var sku = await _db.Skus.FindAsync(skuId);
        
        if (sku == null)
        {
            throw new InvalidOperationException($"SKU not found: {skuId}");
        }

        var fromUom = await _db.Uoms.FirstOrDefaultAsync(u => u.UomCode == fromUomCode);
        var toUom = await _db.Uoms.FirstOrDefaultAsync(u => u.UomCode == toUomCode);

        if (fromUom == null || toUom == null)
        {
            throw new InvalidOperationException($"UOM not found: {fromUomCode} or {toUomCode}");
        }

        if (fromUom.UomType == "base" && toUom.UomType == "base")
        {
            if (fromUomCode == toUomCode)
                return (1, "*");
            throw new InvalidOperationException($"Cannot convert between two base UOMs: {fromUomCode} -> {toUomCode}");
        }

        var directConversion = await _db.UomConversions
            .Include(c => c.FromUom)
            .Include(c => c.ToUom)
            .FirstOrDefaultAsync(c =>
                c.IsActive &&
                (c.SkuId == null || c.SkuId == skuId) &&
                c.FromUom.UomCode == fromUomCode &&
                c.ToUom.UomCode == toUomCode);

        if (directConversion != null)
        {
            return (directConversion.Factor, directConversion.Operator);
        }

        var reverseConversion = await _db.UomConversions
            .Include(c => c.FromUom)
            .Include(c => c.ToUom)
            .FirstOrDefaultAsync(c =>
                c.IsActive &&
                (c.SkuId == null || c.SkuId == skuId) &&
                c.FromUom.UomCode == toUomCode &&
                c.ToUom.UomCode == fromUomCode);

        if (reverseConversion != null && reverseConversion.InverseFactor.HasValue)
        {
            return (reverseConversion.InverseFactor.Value, "*");
        }
        else if (reverseConversion != null)
        {
            var inverse = Math.Round(1 / reverseConversion.Factor, DefaultDecimalPlaces);
            reverseConversion.InverseFactor = inverse;
            await _db.SaveChangesAsync();
            return (inverse, "*");
        }

        var chainResult = await ConvertViaChainAsync(skuId, fromUomCode, toUomCode);
        if (chainResult.Steps.Count > 0)
        {
            return (chainResult.Steps.Last().Factor, "*");
        }

        throw new InvalidOperationException($"No conversion path found: {fromUomCode} -> {toUomCode}");
    }

    public async Task<UomConversionChainResult> ConvertViaChainAsync(Guid skuId, string fromUomCode, string toUomCode)
    {
        var result = new UomConversionChainResult
        {
            OriginalQty = 0,
            OriginalUom = fromUomCode,
            FinalUom = toUomCode
        };

        var visited = new HashSet<string>();
        var queue = new Queue<(string uom, decimal factor, string @operator)>();
        queue.Enqueue((fromUomCode, 1, "*"));

        var cameFrom = new Dictionary<string, (string from, decimal factor, string @operator)>();
        cameFrom[fromUomCode] = (fromUomCode, 1, "*");

        var sku = await _db.Skus.FindAsync(skuId);
        var skuIdNullable = sku?.Id;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (visited.Contains(current.uom)) continue;
            visited.Add(current.uom);

            if (current.uom == toUomCode)
            {
                var path = new List<(string uom, decimal factor, string @operator)>();
                var pathUom = toUomCode;
                while (pathUom != fromUomCode)
                {
                    var (from, factor, @operator) = cameFrom[pathUom];
                    path.Add((pathUom, factor, @operator));
                    pathUom = from;
                }
                path.Reverse();

                int stepNum = 1;
                decimal runningQty = 0;
                decimal runningFactor = 1;

                foreach (var (uom, factor, @operator) in path)
                {
                    if (stepNum == 1)
                    {
                        runningQty = 1;
                        runningFactor = factor;
                    }
                    else
                    {
                        runningQty = Calculate(runningQty, factor, @operator);
                        runningFactor = Calculate(runningFactor, factor, @operator);
                    }

                    result.Steps.Add(new UomConversionStep
                    {
                        StepNumber = stepNum,
                        FromQty = runningQty / runningFactor,
                        FromUom = stepNum == 1 ? fromUomCode : path[stepNum - 2].uom,
                        ToQty = runningQty,
                        ToUom = uom,
                        Factor = factor,
                        Operator = @operator
                    });

                    stepNum++;
                }

                result.TotalConversions = result.Steps.Count;
                result.FinalQty = runningQty;
                return result;
            }

            var conversions = await _db.UomConversions
                .Include(c => c.FromUom)
                .Include(c => c.ToUom)
                .Where(c => c.IsActive && (c.SkuId == null || c.SkuId == skuIdNullable) && c.FromUom.UomCode == current.uom)
                .ToListAsync();

            foreach (var conv in conversions)
            {
                if (!visited.Contains(conv.ToUom.UomCode))
                {
                    var newFactor = Calculate(current.factor, conv.Factor, conv.Operator);
                    queue.Enqueue((conv.ToUom.UomCode, newFactor, "*"));
                    if (!cameFrom.ContainsKey(conv.ToUom.UomCode))
                    {
                        cameFrom[conv.ToUom.UomCode] = (current.uom, conv.Factor, conv.Operator);
                    }
                }
            }

            var reverseConversions = await _db.UomConversions
                .Include(c => c.FromUom)
                .Include(c => c.ToUom)
                .Where(c => c.IsActive && (c.SkuId == null || c.SkuId == skuIdNullable) && c.ToUom.UomCode == current.uom)
                .ToListAsync();

            foreach (var conv in reverseConversions)
            {
                if (!visited.Contains(conv.FromUom.UomCode))
                {
                    var inverseFactor = conv.InverseFactor ?? Math.Round(1 / conv.Factor, DefaultDecimalPlaces);
                    var newFactor = Calculate(current.factor, inverseFactor, "*");
                    queue.Enqueue((conv.FromUom.UomCode, newFactor, "*"));
                    if (!cameFrom.ContainsKey(conv.FromUom.UomCode))
                    {
                        cameFrom[conv.FromUom.UomCode] = (current.uom, inverseFactor, "*");
                    }
                }
            }
        }

        return result;
    }

    public async Task<decimal> ConvertToBaseUomAsync(Guid skuId, decimal qty, string uomCode)
    {
        var sku = await _db.Skus.FindAsync(skuId);
        if (sku == null)
        {
            throw new InvalidOperationException($"SKU not found: {skuId}");
        }

        var baseUomCode = sku.BaseUomCode ?? sku.Unit;

        if (uomCode == baseUomCode)
        {
            return qty;
        }

        if (uomCode == sku.PurchaseUomCode && sku.PurchaseToBaseFactor.HasValue)
        {
            return Calculate(qty, sku.PurchaseToBaseFactor.Value, "*");
        }

        if (uomCode == sku.SalesUomCode && sku.SalesToBaseFactor.HasValue)
        {
            return Calculate(qty, sku.SalesToBaseFactor.Value, "*");
        }

        if (uomCode == sku.ProductionUomCode && sku.ProductionToBaseFactor.HasValue)
        {
            return Calculate(qty, sku.ProductionToBaseFactor.Value, "*");
        }

        var result = await ConvertAsync(skuId, qty, uomCode, baseUomCode!);
        return result.ConvertedQty;
    }

    public async Task<decimal> ConvertFromBaseUomAsync(Guid skuId, decimal qty, string targetUomCode)
    {
        var sku = await _db.Skus.FindAsync(skuId);
        if (sku == null)
        {
            throw new InvalidOperationException($"SKU not found: {skuId}");
        }

        var baseUomCode = sku.BaseUomCode ?? sku.Unit;

        if (targetUomCode == baseUomCode)
        {
            return qty;
        }

        if (targetUomCode == sku.PurchaseUomCode && sku.PurchaseToBaseFactor.HasValue && sku.PurchaseToBaseFactor.Value != 0)
        {
            return Calculate(qty, 1 / sku.PurchaseToBaseFactor.Value, "*");
        }

        if (targetUomCode == sku.SalesUomCode && sku.SalesToBaseFactor.HasValue && sku.SalesToBaseFactor.Value != 0)
        {
            return Calculate(qty, 1 / sku.SalesToBaseFactor.Value, "*");
        }

        if (targetUomCode == sku.ProductionUomCode && sku.ProductionToBaseFactor.HasValue && sku.ProductionToBaseFactor.Value != 0)
        {
            return Calculate(qty, 1 / sku.ProductionToBaseFactor.Value, "*");
        }

        var result = await ConvertAsync(skuId, qty, baseUomCode!, targetUomCode);
        return result.ConvertedQty;
    }

    public decimal Calculate(decimal qty, decimal factor, string @operator)
    {
        return @operator switch
        {
            "*" => Math.Round(qty * factor, DefaultDecimalPlaces),
            "/" => factor != 0 ? Math.Round(qty / factor, DefaultDecimalPlaces) : throw new DivideByZeroException(),
            _ => throw new InvalidOperationException($"Unknown operator: {@operator}")
        };
    }

    public async Task<bool> ValidateConversionAsync(Guid skuId, string fromUomCode, string toUomCode)
    {
        try
        {
            await GetConversionFactorAsync(skuId, fromUomCode, toUomCode);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
