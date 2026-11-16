using Microsoft.EntityFrameworkCore;
using System.Data;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.System;

namespace ErpBackend.Services;

public interface IInventoryService
{
    Task<object> GetSkuBalanceAsync(string skuCode);
    Task<object> CheckATPAsync(string skuCode, decimal requestedQty);
    Task<object> AdjustBalanceAsync(InventoryAdjustRequest request);
    Task<InventoryStocktakeBalanceDto?> GetStocktakeBalanceAsync(string skuCode, string warehouseCode, string lotNumber);
    Task<InventoryStocktakeResultDto> PostStocktakeAsync(InventoryStocktakeRequestDto request, Guid actorUserId);
    Task<object> PostTransactionAsync(InventoryTransactionRequest request);
    Task<object> GetTransactionsAsync(string? skuCode, string? warehouseCode, string? type, DateTime? fromDate, DateTime? toDate);
    Task<object> GetSummaryAsync(string? warehouseCode);
    Task<List<object>> GetLowStockAsync(int maxItems = 100);
    Task<decimal> GetTotalValuationAsync();
}

public class InventoryService : IInventoryService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IAccountingService _accountingService;

    public InventoryService(ErpGeneralDbContext db, IAccountingService accountingService)
    {
        _db = db;
        _accountingService = accountingService;
    }

    public async Task<object> GetSkuBalanceAsync(string skuCode)
    {
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == skuCode);
        if (sku == null)
            return new { error = "物料不存在" };

        var transactions = await _db.InventoryTransactions
            .Where(t => t.SkuId == sku.Id)
            .Include(t => t.Warehouse)
            .Include(t => t.BinLocation)
            .ToListAsync();

        var balances = transactions
            .GroupBy(t => new
            {
                t.WarehouseId,
                t.BinLocationId,
                WarehouseCode = t.Warehouse == null ? null : t.Warehouse.WarehouseCode,
                WarehouseName = t.Warehouse == null ? null : t.Warehouse.WarehouseName,
                BinLocationCode = t.BinLocation == null ? null : t.BinLocation.LocationCode
            })
            .Select(g => new
            {
                WarehouseId = g.Key.WarehouseId,
                BinLocationId = g.Key.BinLocationId,
                WarehouseCode = g.Key.WarehouseCode,
                WarehouseName = g.Key.WarehouseName,
                BinLocationCode = g.Key.BinLocationCode,
                TotalQty = g.Sum(t => t.QtyChange)
            })
            .ToList();

        var totalOnHand = balances.Sum(b => b.TotalQty);
        var quarantined = await GetQuarantinedQtyAsync(sku.Id);

        return new
        {
            sku_code = skuCode,
            sku_name = sku.SkuName,
            total_on_hand = totalOnHand,
            quarantined_on_hand = quarantined,
            available_on_hand = totalOnHand - quarantined,
            by_warehouse = balances
        };
    }

    public async Task<InventoryStocktakeBalanceDto?> GetStocktakeBalanceAsync(
        string skuCode,
        string warehouseCode,
        string lotNumber)
    {
        var sku = await _db.Skus.FirstOrDefaultAsync(item => item.SkuNumber == skuCode);
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(item => item.WarehouseCode == warehouseCode);
        if (sku is null || warehouse is null)
            return null;

        var systemQty = await _db.InventoryTransactions
            .Where(item => item.SkuId == sku.Id &&
                           item.WarehouseId == warehouse.Id &&
                           item.LotNumber == lotNumber)
            .SumAsync(item => (decimal?)item.QtyChange) ?? 0m;
        var inventoryLot = await _db.InventoryLots.FirstOrDefaultAsync(
            item => item.SkuId == sku.Id && item.LotNumber == lotNumber);

        return new InventoryStocktakeBalanceDto
        {
            SkuCode = sku.SkuNumber,
            SkuName = sku.SkuName,
            WarehouseCode = warehouse.WarehouseCode,
            LotNumber = lotNumber,
            ExpiryDate = inventoryLot?.ExpiryDate,
            SystemQty = systemQty
        };
    }

    public async Task<object> CheckATPAsync(string skuCode, decimal requestedQty)
    {
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == skuCode);
        if (sku == null)
            return new { error = "物料不存在" };

        var onHand = (decimal)await _db.InventoryTransactions
            .Where(t => t.SkuId == sku.Id)
            .Select(t => (double)t.QtyChange)
            .SumAsync();

        var quarantined = await GetQuarantinedQtyAsync(sku.Id);

        var allocated = (decimal)await _db.SalesOrderItems
            .Where(i => i.SkuId == sku.Id && i.Status == "pending")
            .Select(i => (double)i.Quantity)
            .SumAsync();

        var available = onHand - quarantined - allocated;

        return new
        {
            sku_code = skuCode,
            on_hand = onHand,
            quarantined = quarantined,
            allocated = allocated,
            available = available,
            is_sufficient = available >= requestedQty
        };
    }

    private async Task<decimal> GetQuarantinedQtyAsync(Guid skuId)
    {
        var iqcQuarantined = await _db.IqcReports
            .Where(report => report.SkuId == skuId &&
                             report.GoodsReceiptItemId != null &&
                             report.Result != "pass" &&
                             report.Result != "not_required" &&
                             !_db.InventoryLots.Any(lot =>
                                 lot.SkuId == report.SkuId &&
                                 lot.LotNumber == report.LotNumber &&
                                 lot.WarehouseId == report.GoodsReceipt!.WarehouseId &&
                                 lot.BinLocationId == report.GoodsReceiptItem!.BinLocationId &&
                                 lot.Status == "quarantine"))
            .SumAsync(report => (decimal?)report.ReceivedQty) ?? 0m;
        var fqcQuarantined = await _db.InventoryLots
            .Where(lot => lot.SkuId == skuId && lot.Status == "quarantine")
            .SumAsync(lot => (decimal?)lot.Qty) ?? 0m;

        return iqcQuarantined + fqcQuarantined;
    }

    public async Task<object> AdjustBalanceAsync(InventoryAdjustRequest request)
    {
        await using var transactionScope = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)
            : null;
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == request.SkuCode);
        if (sku == null)
            return new { error = "物料不存在" };
        sku = await LockSkuAsync(sku.Id) ?? sku;

        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseCode == request.WarehouseCode);
        if (warehouse == null)
            return new { error = "倉庫不存在" };

        Guid? binId = null;
        if (!string.IsNullOrEmpty(request.BinCode))
        {
            var bin = await _db.BinLocations.FirstOrDefaultAsync(b => 
                b.WarehouseId == warehouse.Id && b.LocationCode == request.BinCode);
            binId = bin?.Id;
        }

        var existingQty = (decimal)await _db.InventoryTransactions
            .Where(t => t.SkuId == sku.Id && t.WarehouseId == warehouse.Id)
            .Select(t => (double)t.QtyChange)
            .SumAsync();
        if (request.QtyChange < 0
            && (existingQty < -request.QtyChange || sku.Qty < -request.QtyChange))
            return new { error = "庫存不足，調整將造成負庫存" };

        var trxId = $"ADJ-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var transaction = new InventoryTransaction
        {
            TrxId = trxId,
            SkuId = sku.Id,
            Type = request.QtyChange > 0 ? "ADJUST_IN" : "ADJUST_OUT",
            QtyChange = request.QtyChange,
            QtyBefore = existingQty,
            QtyAfter = existingQty + request.QtyChange,
            WarehouseId = warehouse.Id,
            BinLocationId = binId,
            LotNumber = request.LotNumber,
            RelatedDocType = "InventoryAdjust",
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.InventoryTransactions.Add(transaction);
        sku.Qty += request.QtyChange;
        sku.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        if (transactionScope != null)
            await transactionScope.CommitAsync();

        return new
        {
            trx_id = trxId,
            sku_code = request.SkuCode,
            qty_before = existingQty,
            qty_change = request.QtyChange,
            qty_after = existingQty + request.QtyChange,
            message = "庫存調整已完成 (不可變記錄)"
        };
    }

    public async Task<InventoryStocktakeResultDto> PostStocktakeAsync(
        InventoryStocktakeRequestDto request,
        Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("無法識別過帳操作人員", nameof(actorUserId));
        if (string.IsNullOrWhiteSpace(request.SkuCode) ||
            string.IsNullOrWhiteSpace(request.WarehouseCode) ||
            string.IsNullOrWhiteSpace(request.LotNumber) ||
            !request.ExpiryDate.HasValue)
            throw new ArgumentException("請提供料號、倉庫、批號與效期");
        if (request.CountedQty < 0 || request.ExpectedSystemQty < 0)
            throw new ArgumentException("實盤數與系統帳面數不可小於零");

        await using var transactionScope = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        var sku = await _db.Skus.FirstOrDefaultAsync(item => item.SkuNumber == request.SkuCode.Trim());
        if (sku is null)
            throw new KeyNotFoundException("物料不存在");
        sku = await LockSkuAsync(sku.Id) ?? sku;

        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(
            item => item.WarehouseCode == request.WarehouseCode.Trim());
        if (warehouse is null)
            throw new KeyNotFoundException("倉庫不存在");

        var lotNumber = request.LotNumber.Trim();
        var qtyBefore = await _db.InventoryTransactions
            .Where(item => item.SkuId == sku.Id &&
                           item.WarehouseId == warehouse.Id &&
                           item.LotNumber == lotNumber)
            .SumAsync(item => (decimal?)item.QtyChange) ?? 0m;
        var warehouseQtyBefore = await _db.InventoryTransactions
            .Where(item => item.SkuId == sku.Id && item.WarehouseId == warehouse.Id)
            .SumAsync(item => (decimal?)item.QtyChange) ?? 0m;
        if (qtyBefore != request.ExpectedSystemQty)
            throw new InvalidOperationException("庫存已變動，請重新讀取系統帳面數後再盤點");

        var qtyChange = request.CountedQty - qtyBefore;
        if (qtyChange < 0 && sku.Qty < -qtyChange)
            throw new InvalidOperationException("料號總庫存不足，盤點調整將造成負庫存");

        var inventoryLot = await _db.InventoryLots.FirstOrDefaultAsync(
            item => item.SkuId == sku.Id && item.LotNumber == lotNumber);
        if (inventoryLot?.WarehouseId is Guid existingWarehouseId &&
            existingWarehouseId != warehouse.Id && inventoryLot.Qty > 0)
            throw new InvalidOperationException("此批號目前登記於其他倉庫，請先確認批號庫存位置");

        var expiryDate = DateTime.SpecifyKind(request.ExpiryDate.Value.Date, DateTimeKind.Utc);
        var expiryDateChanged = inventoryLot is null || inventoryLot.ExpiryDate?.Date != expiryDate;
        if (inventoryLot is null)
        {
            inventoryLot = new InventoryLot
            {
                SkuId = sku.Id,
                LotNumber = lotNumber,
                Qty = request.CountedQty,
                WarehouseId = warehouse.Id,
                ExpiryDate = expiryDate,
                Status = request.CountedQty > 0 ? "active" : "depleted"
            };
            _db.InventoryLots.Add(inventoryLot);
        }
        else
        {
            inventoryLot.Qty = request.CountedQty;
            inventoryLot.WarehouseId = warehouse.Id;
            inventoryLot.ExpiryDate = expiryDate;
            inventoryLot.Status = request.CountedQty > 0 ? "active" : "depleted";
        }

        string? trxId = null;
        if (qtyChange != 0 || expiryDateChanged)
        {
            var now = DateTime.UtcNow;
            trxId = $"STK-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..27];
            var postedTransaction = new InventoryTransaction
            {
                TrxId = trxId,
                SkuId = sku.Id,
                Type = qtyChange > 0 ? "ADJUST_IN" : qtyChange < 0 ? "ADJUST_OUT" : "STOCKTAKE",
                QtyChange = qtyChange,
                QtyBefore = warehouseQtyBefore,
                QtyAfter = warehouseQtyBefore + qtyChange,
                WarehouseId = warehouse.Id,
                LotNumber = lotNumber,
                RelatedDocType = "Stocktake",
                HandlerId = actorUserId,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? "QR 盤點過帳" : request.Notes.Trim(),
                CreatedAt = now
            };
            _db.InventoryTransactions.Add(postedTransaction);
            _db.Entry(postedTransaction).Property<Guid?>("CreatedBy").CurrentValue = actorUserId;
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = actorUserId,
                Action = "stocktake.posted",
                EntityType = nameof(InventoryTransaction),
                EntityId = postedTransaction.Id,
                Details = $"交易 {trxId}；料號 {sku.SkuNumber}；倉庫 {warehouse.WarehouseCode}；批號 {lotNumber}；數量變更 {qtyChange}",
                TableName = nameof(ErpGeneralDbContext.InventoryTransactions),
                CreatedAt = now
            });
            sku.Qty += qtyChange;
            sku.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();
        if (transactionScope is not null)
            await transactionScope.CommitAsync();

        return new InventoryStocktakeResultDto
        {
            TrxId = trxId,
            SkuCode = sku.SkuNumber,
            WarehouseCode = warehouse.WarehouseCode,
            LotNumber = lotNumber,
            QtyBefore = qtyBefore,
            CountedQty = request.CountedQty,
            QtyChange = qtyChange,
            QtyAfter = request.CountedQty,
            ExpiryDate = expiryDate
        };
    }

    public async Task<object> PostTransactionAsync(InventoryTransactionRequest request)
    {
        await using var transactionScope = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)
            : null;
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == request.SkuCode);
        if (sku == null)
            return new { error = "物料不存在" };
        sku = await LockSkuAsync(sku.Id) ?? sku;

        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseCode == request.WarehouseCode);
        if (warehouse == null)
            return new { error = "倉庫不存在" };

        Guid? binId = null;
        if (!string.IsNullOrEmpty(request.BinCode))
        {
            var bin = await _db.BinLocations.FirstOrDefaultAsync(b => 
                b.WarehouseId == warehouse.Id && b.LocationCode == request.BinCode);
            binId = bin?.Id;
        }

        var existingQty = (decimal)await _db.InventoryTransactions
            .Where(t => t.SkuId == sku.Id && t.WarehouseId == warehouse.Id)
            .Select(t => (double)t.QtyChange)
            .SumAsync();
        if (request.Qty < 0
            && (existingQty < -request.Qty || sku.Qty < -request.Qty))
            return new { error = "庫存不足，異動將造成負庫存" };

        var trxId = $"{request.Type}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var transaction = new InventoryTransaction
        {
            TrxId = trxId,
            SkuId = sku.Id,
            Type = request.Type,
            QtyChange = request.Qty,
            QtyBefore = existingQty,
            QtyAfter = existingQty + request.Qty,
            WarehouseId = warehouse.Id,
            BinLocationId = binId,
            LotNumber = request.LotNumber,
            RelatedDocType = request.RelatedDocType,
            RelatedDocId = request.RelatedDocId,
            UnitCost = request.UnitCost,
            TotalCost = request.Qty * (request.UnitCost ?? 0),
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.InventoryTransactions.Add(transaction);
        sku.Qty += request.Qty;
        sku.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        if (transactionScope != null)
            await transactionScope.CommitAsync();

        return new
        {
            trx_id = trxId,
            type = request.Type,
            sku_code = request.SkuCode,
            qty_change = request.Qty,
            qty_after = existingQty + request.Qty,
            created_at = DateTime.UtcNow
        };
    }

    public async Task<object> GetTransactionsAsync(
        string? skuCode, string? warehouseCode, string? type, 
        DateTime? fromDate, DateTime? toDate)
    {
        var query = _db.InventoryTransactions
            .Include(t => t.Sku)
            .Include(t => t.Warehouse)
            .Include(t => t.BinLocation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(skuCode))
            query = query.Where(t => t.Sku!.SkuNumber == skuCode);
        if (!string.IsNullOrEmpty(warehouseCode))
            query = query.Where(t => t.Warehouse!.WarehouseCode == warehouseCode);
        if (!string.IsNullOrEmpty(type))
            query = query.Where(t => t.Type == type);
        if (fromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(t => t.CreatedAt <= toDate.Value);

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(100)
            .Select(t => new
            {
                trx_id = t.TrxId,
                sku_code = t.Sku!.SkuNumber,
                sku_name = t.Sku!.SkuName,
                type = t.Type,
                qty_change = t.QtyChange,
                qty_before = t.QtyBefore,
                qty_after = t.QtyAfter,
                warehouse = t.Warehouse!.WarehouseCode,
                bin_location = t.BinLocation != null ? t.BinLocation.LocationCode : null,
                lot_number = t.LotNumber,
                notes = t.Notes,
                created_at = t.CreatedAt
            })
            .ToListAsync();

        return transactions;
    }

    public async Task<object> GetSummaryAsync(string? warehouseCode)
    {
        var query = _db.InventoryTransactions
            .Include(t => t.Sku)
            .Include(t => t.Warehouse)
            .AsQueryable();

        if (!string.IsNullOrEmpty(warehouseCode))
            query = query.Where(t => t.Warehouse!.WarehouseCode == warehouseCode);

        var summary = await query
            .GroupBy(t => new { t.Sku!.SkuNumber, t.Sku.SkuName, t.Warehouse!.WarehouseCode })
            .Select(g => new
            {
                sku_code = g.Key.SkuNumber,
                sku_name = g.Key.SkuName,
                warehouse_code = g.Key.WarehouseCode,
                total_qty = g.Sum(t => t.QtyChange)
            })
            .Where(s => s.total_qty > 0)
            .OrderBy(s => s.sku_code)
            .ToListAsync();

return summary;
    }

    public async Task<List<object>> GetLowStockAsync(int maxItems = 100)
    {
        var items = await _db.Skus
            .Where(s => s.Qty <= s.ReorderPoint && s.Status == "active")
            .OrderBy(s => (double)s.Qty)
            .Take(maxItems)
            .Select(s => new
            {
                id = s.Id,
                skuNumber = s.SkuNumber,
                skuName = s.SkuName,
                unit = s.Unit,
                qty = s.Qty,
                reorderPoint = s.ReorderPoint,
                safetyStock = s.SafetyStock,
                standardCost = s.StandardCost,
                status = s.Status
            })
            .ToListAsync();

        return items.Cast<object>().ToList();
    }

    public async Task<decimal> GetTotalValuationAsync()
    {
        return (decimal)await _db.Skus
            .Select(s => (double)(s.Qty * s.StandardCost))
            .SumAsync();
    }

    private async Task<Sku?> LockSkuAsync(Guid skuId)
    {
        if (_db.Database.IsRelational()
            && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var sku = await _db.Skus
                .FromSqlInterpolated($"SELECT * FROM \"Skus\" WHERE \"Id\" = {skuId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (sku != null)
                await _db.Entry(sku).ReloadAsync();
            return sku;
        }

        return await _db.Skus.FirstOrDefaultAsync(sku => sku.Id == skuId);
    }
}
