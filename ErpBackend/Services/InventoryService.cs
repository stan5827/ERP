using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.Helpers;

namespace ErpBackend.Services;

public interface IInventoryService
{
    Task<decimal> UpdateStockAsync(Guid skuId, Guid warehouseId, decimal qtyChange, string type, string? referenceNumber = null, string? notes = null, Guid? binId = null, string? lotNumber = null);
    Task<decimal> GetOnHandAsync(Guid skuId, Guid? warehouseId = null);
    Task CheckAndAlertLowStockAsync(Guid skuId);
}

public class InventoryService : IInventoryService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IAccountingService _accounting;

    public InventoryService(ErpGeneralDbContext db, IAccountingService accounting)
    {
        _db = db;
        _accounting = accounting;
    }

    public async Task<decimal> UpdateStockAsync(Guid skuId, Guid warehouseId, decimal qtyChange, string type, string? referenceNumber = null, string? notes = null, Guid? binId = null, string? lotNumber = null)
    {
        // 1. Fetch SKU master
        var sku = await _db.Skus.FindAsync(skuId);
        if (sku == null) throw new Exception("物料不存在");

        // 2. Calculate current warehouse balance from transaction history
        var existingQty = await _db.InventoryTransactions
            .Where(t => t.SkuId == skuId && t.WarehouseId == warehouseId)
            .SumAsync(t => t.QtyChange);

        var qtyBefore = existingQty;
        var qtyAfter = existingQty + qtyChange;

        // 3. Create inventory transaction record
        var transaction = new InventoryTransaction
        {
            TrxId = DocumentNumberGenerator.Generate("TR"),
            SkuId = skuId,
            WarehouseId = warehouseId,
            BinId = binId,
            QtyChange = qtyChange,
            QtyBefore = qtyBefore,
            QtyAfter = qtyAfter,
            TrxType = type,
            ReferenceNumber = referenceNumber,
            LotNumber = lotNumber,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.InventoryTransactions.Add(transaction);
        
        // 4. Update SKU on-hand qty
        sku.CurrentStock = qtyAfter;
        sku.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // 5. Trigger alerts if needed
        if (sku.CurrentStock <= sku.ReorderPoint)
        {
            await CheckAndAlertLowStockAsync(skuId);
        }

        return qtyAfter;
    }

    public async Task<decimal> GetOnHandAsync(Guid skuId, Guid? warehouseId = null)
    {
        var query = _db.InventoryTransactions.Where(t => t.SkuId == skuId);
        if (warehouseId.HasValue)
        {
            query = query.Where(t => t.WarehouseId == warehouseId.Value);
        }
        return await query.SumAsync(t => t.QtyChange);
    }

    public async Task CheckAndAlertLowStockAsync(Guid skuId)
    {
        var sku = await _db.Skus.FindAsync(skuId);
        if (sku == null) return;

        var notifType = sku.CurrentStock <= (sku.ReorderPoint * 0.5m) ? "error" : "warning";
        var notification = new SystemNotification
        {
            Id = Guid.NewGuid(),
            Title = $"低庫存警訊：{sku.SkuName} ({sku.SkuCode})",
            Content = $"品號 {sku.SkuCode}（{sku.SkuName}）當前庫存 {sku.CurrentStock} {sku.Unit}，已低於再訂貨點 {sku.ReorderPoint}，請儘速安排採購。",
            Type = notifType,
            TargetRole = "procurement",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.SystemNotifications.Add(notification);
        await _db.SaveChangesAsync();
    }
}
