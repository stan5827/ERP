using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/erp/[controller]")]
public class InventoryController : Controller
{
    private readonly ErpGeneralDbContext _db;
    private readonly IAccountingService _accountingService;

    public InventoryController(ErpGeneralDbContext db, IAccountingService accountingService)
    {
        _db = db;
        _accountingService = accountingService;
    }

    [HttpGet("ping")]
    public object Ping() => new { message = "InventoryController operational" };

    [HttpGet("{skuCode}/balance")]
    public async Task<object> GetSkuBalance(string skuCode)
    {
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == skuCode);
        if (sku == null)
            return NotFound(new { error = "物料不存在" });

        var balances = await _db.InventoryTransactions
            .Where(t => t.SkuId == sku.Id)
            .GroupBy(t => new { t.WarehouseId, t.BinLocationId })
            .Select(g => new
            {
                WarehouseId = g.Key.WarehouseId,
                BinLocationId = g.Key.BinLocationId,
                TotalQty = g.Sum(t => t.QtyChange)
            })
            .ToListAsync();

        var totalOnHand = balances.Sum(b => b.TotalQty);

        return Ok(new
        {
            sku_code = skuCode,
            sku_name = sku.SkuName,
            total_on_hand = totalOnHand,
            by_warehouse = balances
        });
    }

    [HttpGet("atp")]
    public async Task<object> CheckATP([FromQuery] string skuCode, [FromQuery] decimal requestedQty)
    {
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == skuCode);
        if (sku == null)
            return NotFound(new { error = "物料不存在" });

        var onHand = await _db.InventoryTransactions
            .Where(t => t.SkuId == sku.Id)
            .SumAsync(t => t.QtyChange);

        var allocated = await _db.SalesOrderItems
            .Where(i => i.SkuId == sku.Id && i.Status == "pending")
            .SumAsync(i => i.Quantity);

        var available = onHand - allocated;

        return Ok(new
        {
            sku_code = skuCode,
            on_hand = onHand,
            allocated = allocated,
            available = available,
            is_sufficient = available >= requestedQty
        });
    }

    [HttpPost("adjust")]
    public async Task<object> AdjustBalance([FromBody] InventoryAdjustRequest request)
    {
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == request.SkuCode);
        if (sku == null)
            return NotFound(new { error = "物料不存在" });

        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseCode == request.WarehouseCode);
        if (warehouse == null)
            return NotFound(new { error = "倉庫不存在" });

        Guid? binId = null;
        if (!string.IsNullOrEmpty(request.BinCode))
        {
            var bin = await _db.BinLocations.FirstOrDefaultAsync(b => 
                b.WarehouseId == warehouse.Id && b.LocationCode == request.BinCode);
            binId = bin?.Id;
        }

        var existingQty = await _db.InventoryTransactions
            .Where(t => t.SkuId == sku.Id && t.WarehouseId == warehouse.Id)
            .SumAsync(t => t.QtyChange);

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
        await _db.SaveChangesAsync();

        return Ok(new
        {
            trx_id = trxId,
            sku_code = request.SkuCode,
            qty_before = existingQty,
            qty_change = request.QtyChange,
            qty_after = existingQty + request.QtyChange,
            message = "庫存調整已完成 (不可變記錄)"
        });
    }

    [HttpPost("transaction")]
    public async Task<object> PostTransaction([FromBody] InventoryTransactionRequest request)
    {
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == request.SkuCode);
        if (sku == null)
            return NotFound(new { error = "物料不存在" });

        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseCode == request.WarehouseCode);
        if (warehouse == null)
            return NotFound(new { error = "倉庫不存在" });

        Guid? binId = null;
        if (!string.IsNullOrEmpty(request.BinCode))
        {
            var bin = await _db.BinLocations.FirstOrDefaultAsync(b => 
                b.WarehouseId == warehouse.Id && b.LocationCode == request.BinCode);
            binId = bin?.Id;
        }

        var existingQty = await _db.InventoryTransactions
            .Where(t => t.SkuId == sku.Id && t.WarehouseId == warehouse.Id)
            .SumAsync(t => t.QtyChange);

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
        await _db.SaveChangesAsync();

        return Ok(new
        {
            trx_id = trxId,
            type = request.Type,
            sku_code = request.SkuCode,
            qty_change = request.Qty,
            qty_after = existingQty + request.Qty,
            created_at = DateTime.UtcNow
        });
    }

    [HttpGet("transactions")]
    public async Task<object> GetTransactions(
        [FromQuery] string? skuCode = null,
        [FromQuery] string? warehouseCode = null,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = _db.InventoryTransactions
            .Include(t => t.Sku)
            .Include(t => t.Warehouse)
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
                lot_number = t.LotNumber,
                notes = t.Notes,
                created_at = t.CreatedAt
            })
            .ToListAsync();

        return Ok(transactions);
    }

    [HttpGet("summary")]
    public async Task<object> GetSummary([FromQuery] string? warehouseCode = null)
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

        return Ok(summary);
    }
}

public class InventoryAdjustRequest
{
    public string SkuCode { get; set; } = null!;
    public string WarehouseCode { get; set; } = null!;
    public string? BinCode { get; set; }
    public decimal QtyChange { get; set; }
    public string? LotNumber { get; set; }
    public string? Notes { get; set; }
}

public class InventoryTransactionRequest
{
    public string SkuCode { get; set; } = null!;
    public string WarehouseCode { get; set; } = null!;
    public string? BinCode { get; set; }
    public string Type { get; set; } = null!;
    public decimal Qty { get; set; }
    public string? LotNumber { get; set; }
    public string? RelatedDocType { get; set; }
    public Guid? RelatedDocId { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }
}