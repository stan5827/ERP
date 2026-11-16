using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.System;

namespace ErpBackend.Services;

public interface IBomService
{
    Task<List<BomDto>> GetBomsAsync(Guid? productSkuId = null);
    Task<BomDto?> GetBomAsync(Guid id);
    Task<(BomDto? dto, string status)> CreateBomAsync(CreateBomRequest request);
    Task<(BomDto? dto, string status)> UpdateBomAsync(Guid id, UpdateBomRequest request);
    Task<(bool ok, string status)> DeleteBomAsync(Guid id);
    Task<(bool ok, string status)> SetBomStatusAsync(Guid id, string status);
    Task<BomCostRollupDto?> GetCostRollupAsync(Guid id);
    Task<(BomStandardCostRollupApprovalDto? dto, string status)> ApplyStandardCostRollupAsync(
        Guid id,
        decimal expectedTotalCost,
        Guid approvedBy,
        CancellationToken cancellationToken);
    Task<BomTreeNodeDto?> GetBomTreeAsync(Guid productSkuId);
    Task<BomDto?> GetLatestActiveRevisionAsync(Guid productSkuId);
}

public class BomService : IBomService
{
    private const int MAX_DEPTH = 20;
    private readonly ErpGeneralDbContext _db;
    private readonly IUomConversionService _uomConversionService;

    public BomService(ErpGeneralDbContext db) : this(db, new UomConversionService(db))
    {
    }

    public BomService(ErpGeneralDbContext db, IUomConversionService uomConversionService)
    {
        _db = db;
        _uomConversionService = uomConversionService;
    }

    public async Task<List<BomDto>> GetBomsAsync(Guid? productSkuId = null)
    {
        var query = _db.Boms.AsNoTracking().Include(b => b.Items).AsQueryable();

        if (productSkuId.HasValue)
            query = query.Where(b => b.ProductSkuId == productSkuId.Value);

        var boms = await query.OrderBy(b => b.BomCode).ToListAsync();

        return await ToDtoListAsync(boms);
    }

    public async Task<BomDto?> GetBomAsync(Guid id)
        => await BuildBomDtoAsync(id);

    public async Task<(BomDto? dto, string status)> CreateBomAsync(CreateBomRequest request)
    {
        var (unitsValid, _) = await SetProductionUnitsAsync(request.Items);
        if (!unitsValid) return (null, "invalid");

        var (valid, _) = ValidateCreateRequest(request);
        if (!valid) return (null, "invalid");

        var duplicate = await _db.Boms.AnyAsync(b => b.BomCode == request.BomCode);
        if (duplicate) return (null, "duplicate");

        var productExists = await _db.Skus.AsNoTracking().AnyAsync(s => s.Id == request.ProductSkuId);
        if (!productExists) return (null, "invalid");

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var bom = new Bom
            {
                BomCode = request.BomCode,
                ProductSkuId = request.ProductSkuId,
                Version = request.Version ?? "1.0",
                Status = request.Status ?? "draft",
                EffectiveDate = NormalizeUtc(request.EffectiveDate ?? DateTime.UtcNow),
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Boms.Add(bom);
            await _db.SaveChangesAsync();

            if (request.Items?.Any() == true)
            {
                foreach (var itemReq in request.Items)
                    _db.BomItems.Add(ToBomItem(bom.Id, itemReq));
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();

            var dto = await BuildBomDtoAsync(bom.Id);
            return (dto, "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(BomDto? dto, string status)> UpdateBomAsync(Guid id, UpdateBomRequest request)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var bom = await _db.Boms.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == id);
            if (bom == null) return (null, "not_found");

            if (request.Items != null)
            {
                var (unitsValid, _) = await SetProductionUnitsAsync(request.Items);
                if (!unitsValid) return (null, "invalid");

                var (valid, _) = ValidateItems(request.Items, bom.ProductSkuId);
                if (!valid) return (null, "invalid");
            }

            if (request.BomCode != null) bom.BomCode = request.BomCode;
            if (request.Version != null) bom.Version = request.Version;
            if (request.Status != null) bom.Status = request.Status;
            if (request.EffectiveDate.HasValue) bom.EffectiveDate = NormalizeUtc(request.EffectiveDate.Value);
            if (request.Notes != null) bom.Notes = request.Notes;

            if (request.Items != null)
            {
                _db.BomItems.RemoveRange(bom.Items);
                foreach (var itemReq in request.Items)
                    _db.BomItems.Add(ToBomItem(bom.Id, itemReq));
            }

            bom.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var dto = await BuildBomDtoAsync(id);
            return (dto, "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool ok, string status)> DeleteBomAsync(Guid id)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var bom = await _db.Boms.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == id);
            if (bom == null) return (false, "not_found");

            _db.BomItems.RemoveRange(bom.Items);
            _db.Boms.Remove(bom);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool ok, string status)> SetBomStatusAsync(Guid id, string status)
    {
        var bom = await _db.Boms.FirstOrDefaultAsync(b => b.Id == id);
        if (bom == null) return (false, "not_found");

        bom.Status = status;
        bom.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    public async Task<BomCostRollupDto?> GetCostRollupAsync(Guid id)
    {
        var bom = await _db.Boms.AsNoTracking().Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == id);
        if (bom == null) return null;

        return await CalculateCostRollupAsync(bom, CancellationToken.None);
    }

    public async Task<(BomStandardCostRollupApprovalDto? dto, string status)> ApplyStandardCostRollupAsync(
        Guid id,
        decimal expectedTotalCost,
        Guid approvedBy,
        CancellationToken cancellationToken)
    {
        if (expectedTotalCost < 0)
            return (null, "invalid_bom");
        if (approvedBy == Guid.Empty)
            throw new ArgumentException("必須提供有效的核准人員", nameof(approvedBy));

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var bom = await LockBomAsync(id, cancellationToken);
        if (bom == null)
            return (null, "not_found");

        var now = DateTime.UtcNow;
        if (!string.Equals(bom.Status, "active", StringComparison.OrdinalIgnoreCase) ||
            bom.EffectiveDate > now ||
            (bom.ExpiryDate.HasValue && bom.ExpiryDate.Value <= now))
            return (null, "inactive_bom");
        var latestBomId = await _db.Boms
            .Where(candidate =>
                candidate.ProductSkuId == bom.ProductSkuId &&
                candidate.Status == "active" &&
                candidate.EffectiveDate <= now &&
                (!candidate.ExpiryDate.HasValue || candidate.ExpiryDate.Value > now))
            .OrderByDescending(candidate => candidate.Version)
            .ThenByDescending(candidate => candidate.EffectiveDate)
            .Select(candidate => candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (latestBomId != bom.Id)
            return (null, "obsolete_bom");
        if (bom.Items.Count == 0)
            return (null, "invalid_bom");

        var rollup = await CalculateCostRollupAsync(bom, cancellationToken);
        if (rollup.TotalCost != expectedTotalCost)
            return (null, "stale_cost");

        var productSku = await LockSkuAsync(bom.ProductSkuId, cancellationToken);
        if (productSku == null)
            return (null, "not_found");

        var previousStandardCost = productSku.StandardCost;
        productSku.StandardCost = rollup.TotalCost;
        productSku.UpdatedAt = now;
        _db.AuditLogs.Add(new AuditLog
        {
            Action = "approve_bom_standard_cost",
            EntityType = nameof(Bom),
            EntityId = bom.Id,
            Details = $"BOM {bom.BomCode} v{bom.Version}: standard cost {previousStandardCost} -> {rollup.TotalCost}; approved by {approvedBy}.",
            UserId = approvedBy,
            TableName = nameof(Sku),
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);
        if (transaction != null)
            await transaction.CommitAsync(cancellationToken);

        return (new BomStandardCostRollupApprovalDto(
            bom.Id,
            bom.BomCode,
            productSku.Id,
            productSku.SkuNumber,
            previousStandardCost,
            rollup.TotalCost,
            rollup.MaterialCost,
            rollup.LaborCost,
            rollup.OverheadCost,
            new DateTimeOffset(DateTime.SpecifyKind(now, DateTimeKind.Utc))),
            "ok");
    }

    private async Task<BomCostRollupDto> CalculateCostRollupAsync(Bom bom, CancellationToken cancellationToken)
    {
        decimal materialCost = 0;
        decimal laborCost = 0;
        decimal overheadCost = 0;

        foreach (var item in bom.Items)
        {
            var sku = await _db.Skus.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == item.ChildSkuId, cancellationToken);
            if (sku == null)
                throw new InvalidOperationException($"BOM {bom.BomCode} 的元件 SKU {item.ChildSkuId} 不存在");

            var baseQuantity = await _uomConversionService.ConvertToBaseUomAsync(sku.Id, item.Quantity, item.Unit);
            var unitCost = IsSelfManufacturedSku(sku)
                ? sku.StandardCost
                : sku.LatestPurchaseCost > 0 ? sku.LatestPurchaseCost : sku.StandardCost;
            var itemMaterialCost = unitCost * baseQuantity;
            materialCost += itemMaterialCost;

            if (IsSelfManufacturedSku(sku))
            {
                laborCost += itemMaterialCost * 0.2m;
                overheadCost += itemMaterialCost * 0.1m;
            }
        }

        return new BomCostRollupDto
        {
            MaterialCost = Math.Round(materialCost, 2),
            LaborCost = Math.Round(laborCost, 2),
            OverheadCost = Math.Round(overheadCost, 2),
            TotalCost = Math.Round(materialCost + laborCost + overheadCost, 2)
        };
    }

    private static bool IsSelfManufacturedSku(Sku sku) =>
        string.Equals(sku.SourceType, "self_made", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(sku.MakeOrBuy, "make", StringComparison.OrdinalIgnoreCase);

    private async Task<Bom?> LockBomAsync(Guid bomId, CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational()
            && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var bom = await _db.Boms
                .FromSqlInterpolated($"SELECT * FROM \"Boms\" WHERE \"Id\" = {bomId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (bom != null)
                await _db.Entry(bom).Collection(item => item.Items).LoadAsync(cancellationToken);
            return bom;
        }

        return await _db.Boms.Include(bom => bom.Items)
            .SingleOrDefaultAsync(bom => bom.Id == bomId, cancellationToken);
    }

    private async Task<Sku?> LockSkuAsync(Guid skuId, CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational()
            && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var sku = await _db.Skus
                .FromSqlInterpolated($"SELECT * FROM \"Skus\" WHERE \"Id\" = {skuId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (sku != null)
                await _db.Entry(sku).ReloadAsync(cancellationToken);
            return sku;
        }

        return await _db.Skus.SingleOrDefaultAsync(sku => sku.Id == skuId, cancellationToken);
    }

    public async Task<BomTreeNodeDto?> GetBomTreeAsync(Guid productSkuId)
    {
        var bom = await _db.Boms.AsNoTracking().Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.ProductSkuId == productSkuId && b.Status == "active");
        if (bom == null) return null;

        var rootSku = await _db.Skus.AsNoTracking().FirstOrDefaultAsync(s => s.Id == productSkuId);
        if (rootSku == null) return null;

        var visited = new HashSet<Guid>();
        return await BuildTreeNodeAsync(rootSku, null, null, visited, 0);
    }

    public async Task<BomDto?> GetLatestActiveRevisionAsync(Guid productSkuId)
    {
        var bom = await _db.Boms.AsNoTracking().Include(b => b.Items)
            .Where(b => b.ProductSkuId == productSkuId && b.Status == "active")
            .OrderByDescending(b => b.Version)
            .ThenByDescending(b => b.EffectiveDate)
            .FirstOrDefaultAsync();
        if (bom == null) return null;

        return await ToDtoAsync(bom);
    }

    private async Task<BomDto?> BuildBomDtoAsync(Guid id)
    {
        var bom = await _db.Boms.AsNoTracking().Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == id);
        if (bom == null) return null;

        return await ToDtoAsync(bom);
    }

    private async Task<BomDto> ToDtoAsync(Bom bom)
    {
        var result = await ToDtoListAsync(new[] { bom });
        return result[0];
    }

    private async Task<List<BomDto>> ToDtoListAsync(IEnumerable<Bom> boms)
    {
        var bomList = boms.ToList();
        var skuIds = bomList
            .SelectMany(bom => bom.Items.Select(item => item.ChildSkuId).Append(bom.ProductSkuId))
            .Distinct()
            .ToList();
        var skus = skuIds.Count > 0
            ? await _db.Skus.AsNoTracking().Where(sku => skuIds.Contains(sku.Id)).ToDictionaryAsync(sku => sku.Id)
            : new Dictionary<Guid, Sku>();

        return bomList.Select(bom =>
        {
            skus.TryGetValue(bom.ProductSkuId, out var productSku);
            return new BomDto
            {
                Id = bom.Id,
                BomCode = bom.BomCode,
                ProductSkuId = bom.ProductSkuId,
                ProductSkuNumber = productSku?.SkuNumber,
                ProductSkuName = productSku?.SkuName,
                Version = bom.Version,
                Status = bom.Status,
                EffectiveDate = bom.EffectiveDate,
                Notes = bom.Notes,
                CreatedAt = bom.CreatedAt,
                UpdatedAt = bom.UpdatedAt,
                Items = bom.Items.Select(item =>
                {
                    skus.TryGetValue(item.ChildSkuId, out var childSku);
                    return new BomItemDto
                    {
                        Id = item.Id,
                        ChildSkuId = item.ChildSkuId,
                        ChildSkuNumber = childSku?.SkuNumber,
                        ChildSkuName = childSku?.SkuName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        ScrapRate = item.ScrapRate,
                        OperationSeq = item.OperationSeq
                    };
                }).ToList()
            };
        }).ToList();
    }

    private static BomItem ToBomItem(Guid bomId, CreateBomItemRequest req) => new()
    {
        BomId = bomId,
        ChildSkuId = req.ChildSkuId,
        Quantity = req.Quantity,
        Unit = req.Unit!,
        ScrapRate = req.ScrapRate,
        OperationSeq = req.OperationSeq
    };

    private static (bool isValid, string error) ValidateCreateRequest(CreateBomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BomCode)) return (false, "BomCode must not be empty");
        if (request.BomCode.Length > 30) return (false, "BomCode exceeds 30 characters");
        if (request.ProductSkuId == Guid.Empty) return (false, "ProductSkuId is required");
        return ValidateItems(request.Items, request.ProductSkuId);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => value
        };
    }

    private static (bool isValid, string error) ValidateItems(IEnumerable<CreateBomItemRequest>? items, Guid productSkuId)
    {
        if (items == null) return (true, string.Empty);

        foreach (var i in items)
        {
            if (i.ChildSkuId == productSkuId) return (false, "BOM child cannot be the parent product (self-reference)");
            if (i.Quantity <= 0) return (false, "Item Quantity must be greater than 0");
        }

        var dup = items.GroupBy(i => i.ChildSkuId).FirstOrDefault(g => g.Count() > 1);
        if (dup != null) return (false, $"Duplicate child SKU '{dup.Key}' is not allowed");

        return (true, string.Empty);
    }

    private async Task<(bool isValid, string error)> SetProductionUnitsAsync(IEnumerable<CreateBomItemRequest>? items)
    {
        if (items == null) return (true, string.Empty);

        var itemList = items.ToList();
        var childSkuIds = itemList.Select(item => item.ChildSkuId).Distinct().ToList();
        var childSkus = await _db.Skus
            .Include(sku => sku.ProductionUom)
            .Where(sku => childSkuIds.Contains(sku.Id))
            .ToDictionaryAsync(sku => sku.Id);

        foreach (var item in itemList)
        {
            if (!childSkus.TryGetValue(item.ChildSkuId, out var childSku))
                return (false, "BOM child SKU was not found");

            var productionUomCode = childSku.ProductionUom?.UomCode ?? childSku.ProductionUomCode;
            if (string.IsNullOrWhiteSpace(productionUomCode))
                return (false, $"Production unit is required for SKU {childSku.SkuNumber}");

            item.Unit = productionUomCode;
        }

        return (true, string.Empty);
    }

    private async Task<BomTreeNodeDto> BuildTreeNodeAsync(Sku sku, decimal? qty, string? unit, HashSet<Guid> visited, int depth)
    {
        var node = new BomTreeNodeDto
        {
            SkuId = sku.Id,
            SkuNumber = sku.SkuNumber,
            SkuName = sku.SkuName,
            Quantity = qty,
            Unit = unit
        };

        if (depth >= MAX_DEPTH) return node;

        visited.Add(sku.Id);

        if (sku.SourceType == "self_made")
        {
            var childBom = await _db.Boms.AsNoTracking().Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.ProductSkuId == sku.Id && b.Status == "active");

            if (childBom != null)
            {
                foreach (var item in childBom.Items)
                {
                    if (visited.Contains(item.ChildSkuId)) continue;

                    var childSku = await _db.Skus.AsNoTracking().FirstOrDefaultAsync(s => s.Id == item.ChildSkuId);
                    if (childSku == null) continue;

                    node.Children.Add(await BuildTreeNodeAsync(childSku, item.Quantity, item.Unit, visited, depth + 1));
                }
            }
        }

        visited.Remove(sku.Id);
        return node;
    }
}
