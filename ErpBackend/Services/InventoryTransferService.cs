using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Services;

public interface IInventoryTransferService
{
    Task<List<object>> GetTransferOrdersAsync();
    Task<object?> GetTransferOrderAsync(Guid id);
    Task<(object? dto, string status)> CreateTransferOrderAsync(CreateTransferOrderDto model, Guid? userId = null);
    Task<(object? dto, string status)> ApproveTransferOrderAsync(Guid id, Guid? userId = null);
    Task<(object? dto, string status)> ExecuteTransferOrderAsync(Guid id, Guid? userId = null);
    Task<(bool ok, string status)> CancelTransferOrderAsync(Guid id);
}

public class InventoryTransferService : IInventoryTransferService
{
    private readonly ErpGeneralDbContext _db;

    public InventoryTransferService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<object>> GetTransferOrdersAsync()
    {
        return await _db.TransferOrders
            .Include(t => t.Items).ThenInclude(i => i.Sku)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                id = t.Id,
                transferNumber = t.TransferNumber,
                status = t.Status,
                sourceWarehouseId = t.SourceWarehouseId,
                targetWarehouseId = t.TargetWarehouseId,
                approvedBy = t.ApprovedBy,
                createdBy = t.CreatedBy,
                createdAt = t.CreatedAt,
                items = t.Items.Select(i => new
                {
                    id = i.Id,
                    skuId = i.SkuId,
                    skuNumber = i.Sku != null ? i.Sku.SkuNumber : null,
                    skuName = i.Sku != null ? i.Sku.SkuName : null,
                    transferQty = i.TransferQty,
                    lotNumber = i.LotNumber,
                    status = i.Status
                }).ToList()
            })
            .Cast<object>()
            .ToListAsync();
    }

    public async Task<object?> GetTransferOrderAsync(Guid id)
    {
        var t = await _db.TransferOrders
            .Include(t => t.Items).ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (t == null) return null;

        return new
        {
            id = t.Id,
            transferNumber = t.TransferNumber,
            status = t.Status,
            sourceWarehouseId = t.SourceWarehouseId,
            targetWarehouseId = t.TargetWarehouseId,
            approvedBy = t.ApprovedBy,
            createdBy = t.CreatedBy,
            createdAt = t.CreatedAt,
            items = t.Items.Select(i => new
            {
                id = i.Id,
                skuId = i.SkuId,
                skuNumber = i.Sku != null ? i.Sku.SkuNumber : null,
                skuName = i.Sku != null ? i.Sku.SkuName : null,
                transferQty = i.TransferQty,
                lotNumber = i.LotNumber,
                status = i.Status
            }).ToList()
        };
    }

    public async Task<(object? dto, string status)> CreateTransferOrderAsync(CreateTransferOrderDto model, Guid? userId = null)
    {
        if (string.IsNullOrEmpty(model.TransferNumber))
            return (null, "invalid");

        if (model.SourceWarehouseId == model.TargetWarehouseId)
            return (null, "invalid");

        var exists = await _db.TransferOrders.AnyAsync(t => t.TransferNumber == model.TransferNumber);
        if (exists)
            return (null, "duplicate");

        var sourceWarehouse = await _db.Warehouses.FindAsync(model.SourceWarehouseId);
        var targetWarehouse = await _db.Warehouses.FindAsync(model.TargetWarehouseId);
        if (sourceWarehouse == null || targetWarehouse == null)
            return (null, "not_found");

        if (model.Items == null || model.Items.Count == 0)
            return (null, "invalid");

        var transferOrder = new TransferOrder
        {
            TransferNumber = model.TransferNumber,
            SourceWarehouseId = model.SourceWarehouseId,
            TargetWarehouseId = model.TargetWarehouseId,
            Status = "draft",
            ApprovedBy = null,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            Items = model.Items.Select(i => new TransferOrderItem
            {
                SkuId = i.SkuId,
                LotNumber = i.LotNumber,
                TransferQty = i.TransferQty,
                Status = "pending"
            }).ToList()
        };

        _db.TransferOrders.Add(transferOrder);
        await _db.SaveChangesAsync();

        return (await GetTransferOrderAsync(transferOrder.Id), "ok");
    }

    public async Task<(object? dto, string status)> ApproveTransferOrderAsync(Guid id, Guid? userId = null)
    {
        var transferOrder = await _db.TransferOrders.FirstOrDefaultAsync(t => t.Id == id);
        if (transferOrder == null)
            return (null, "not_found");

        if (transferOrder.Status != "draft")
            return (null, "invalid");

        transferOrder.Status = "approved";
        transferOrder.ApprovedBy = userId;
        await _db.SaveChangesAsync();

        return (await GetTransferOrderAsync(id), "ok");
    }

    public async Task<(object? dto, string status)> ExecuteTransferOrderAsync(Guid id, Guid? userId = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var transferOrder = await _db.TransferOrders
                .Include(t => t.Items).ThenInclude(i => i.Sku)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (transferOrder == null)
                return (null, "not_found");

            if (transferOrder.Status != "approved")
                return (null, "invalid");

            var targetWarehouse = await _db.Warehouses.FindAsync(transferOrder.TargetWarehouseId);
            var sourceWarehouse = await _db.Warehouses.FindAsync(transferOrder.SourceWarehouseId);
            if (sourceWarehouse == null || targetWarehouse == null)
                return (null, "not_found");

            foreach (var item in transferOrder.Items)
            {
                var sku = item.Sku ?? await _db.Skus.FindAsync(item.SkuId);
                if (sku == null)
                    return (null, "not_found");

                var sourceQty = (decimal)await _db.InventoryTransactions
                    .Where(t => t.SkuId == sku.Id && t.WarehouseId == transferOrder.SourceWarehouseId)
                    .Select(t => (double)t.QtyChange)
                    .SumAsync();

                if (sourceQty < item.TransferQty)
                    return (null, "insufficient_stock");

                var outTrx = new InventoryTransaction
                {
                    TrxId = $"TO-OUT-{transferOrder.TransferNumber}-{item.Id:N}",
                    SkuId = sku.Id,
                    Type = "TRANSFER_OUT",
                    QtyChange = -item.TransferQty,
                    QtyBefore = sourceQty,
                    QtyAfter = sourceQty - item.TransferQty,
                    WarehouseId = transferOrder.SourceWarehouseId,
                    LotNumber = item.LotNumber,
                    RelatedDocType = "TransferOrder",
                    RelatedDocId = transferOrder.Id,
                    Notes = $"Transfer out to {targetWarehouse.WarehouseCode}",
                    CreatedAt = DateTime.UtcNow
                };
                _db.InventoryTransactions.Add(outTrx);

                var targetQty = (decimal)await _db.InventoryTransactions
                    .Where(t => t.SkuId == sku.Id && t.WarehouseId == transferOrder.TargetWarehouseId)
                    .Select(t => (double)t.QtyChange)
                    .SumAsync();

                var inTrx = new InventoryTransaction
                {
                    TrxId = $"TO-IN-{transferOrder.TransferNumber}-{item.Id:N}",
                    SkuId = sku.Id,
                    Type = "TRANSFER_IN",
                    QtyChange = item.TransferQty,
                    QtyBefore = targetQty,
                    QtyAfter = targetQty + item.TransferQty,
                    WarehouseId = transferOrder.TargetWarehouseId,
                    LotNumber = item.LotNumber,
                    RelatedDocType = "TransferOrder",
                    RelatedDocId = transferOrder.Id,
                    Notes = $"Transfer in from {sourceWarehouse.WarehouseCode}",
                    CreatedAt = DateTime.UtcNow
                };
                _db.InventoryTransactions.Add(inTrx);

                sku.Qty += item.TransferQty;
                sku.UpdatedAt = DateTime.UtcNow;

                item.Status = "completed";
            }

            transferOrder.Status = "completed";
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (await GetTransferOrderAsync(id), "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool ok, string status)> CancelTransferOrderAsync(Guid id)
    {
        var transferOrder = await _db.TransferOrders.FirstOrDefaultAsync(t => t.Id == id);
        if (transferOrder == null)
            return (false, "not_found");

        if (transferOrder.Status == "completed" || transferOrder.Status == "cancelled")
            return (false, "invalid");

        transferOrder.Status = "cancelled";
        foreach (var item in transferOrder.Items)
            item.Status = "cancelled";

        await _db.SaveChangesAsync();
        return (true, "ok");
    }
}
