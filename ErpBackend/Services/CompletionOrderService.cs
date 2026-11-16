using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Services;

public interface ICompletionOrderService
{
    Task<List<object>> GetCompletionOrdersAsync();
    Task<object?> GetCompletionOrderAsync(Guid id);
    Task<(object? dto, string status)> CreateCompletionOrderAsync(CreateCompletionOrderDto model, Guid? userId = null);
    Task<(object? dto, string status)> CompleteOrderAsync(Guid id, Guid? userId = null);
    Task<(bool ok, string status)> CancelCompletionOrderAsync(Guid id);
    Task<List<object>> GetCompletableWorkOrdersAsync();
}

public class CompletionOrderService : ICompletionOrderService
{
    private readonly ErpGeneralDbContext _db;

    public CompletionOrderService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<object>> GetCompletionOrdersAsync()
    {
        return await _db.CompletionOrders
            .Include(c => c.Items).ThenInclude(i => i.Sku)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                id = c.Id,
                completionNumber = c.CompletionNumber,
                workOrderId = c.WorkOrderId,
                warehouseId = c.WarehouseId,
                status = c.Status,
                qaStatus = c.QaStatus,
                receivedBy = c.ReceivedBy,
                createdAt = c.CreatedAt,
                items = c.Items.Select(i => new
                {
                    id = i.Id,
                    skuId = i.SkuId,
                    skuNumber = i.Sku != null ? i.Sku.SkuNumber : null,
                    skuName = i.Sku != null ? i.Sku.SkuName : null,
                    receivedQty = i.ReceivedQty,
                    lotNumber = i.LotNumber,
                    manufactureDate = i.ManufactureDate,
                    expiryDate = i.ExpiryDate,
                    binLocationId = i.BinLocationId
                }).ToList()
            })
            .Cast<object>()
            .ToListAsync();
    }

    public async Task<object?> GetCompletionOrderAsync(Guid id)
    {
        var c = await _db.CompletionOrders
            .Include(c => c.Items).ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (c == null) return null;

        return new
        {
            id = c.Id,
            completionNumber = c.CompletionNumber,
            workOrderId = c.WorkOrderId,
            warehouseId = c.WarehouseId,
            status = c.Status,
            qaStatus = c.QaStatus,
            receivedBy = c.ReceivedBy,
            createdAt = c.CreatedAt,
            items = c.Items.Select(i => new
            {
                id = i.Id,
                skuId = i.SkuId,
                skuNumber = i.Sku != null ? i.Sku.SkuNumber : null,
                skuName = i.Sku != null ? i.Sku.SkuName : null,
                receivedQty = i.ReceivedQty,
                lotNumber = i.LotNumber,
                manufactureDate = i.ManufactureDate,
                expiryDate = i.ExpiryDate,
                binLocationId = i.BinLocationId
            }).ToList()
        };
    }

    public async Task<(object? dto, string status)> CreateCompletionOrderAsync(CreateCompletionOrderDto model, Guid? userId = null)
    {
        if (string.IsNullOrEmpty(model.CompletionNumber))
            return (null, "invalid");

        var exists = await _db.CompletionOrders.AnyAsync(c => c.CompletionNumber == model.CompletionNumber);
        if (exists)
            return (null, "duplicate");

        var warehouse = await _db.Warehouses.FindAsync(model.WarehouseId);
        if (warehouse == null)
            return (null, "not_found");

        if (model.Items == null || model.Items.Count == 0)
            return (null, "invalid");

        var completionOrder = new CompletionOrder
        {
            CompletionNumber = model.CompletionNumber,
            WorkOrderId = model.WorkOrderId,
            WarehouseId = model.WarehouseId,
            Status = "draft",
            QaStatus = "pending",
            ReceivedBy = userId,
            CreatedAt = DateTime.UtcNow,
            Items = model.Items.Select(i => new CompletionOrderItem
            {
                SkuId = i.SkuId,
                LotNumber = i.LotNumber,
                ReceivedQty = i.ReceivedQty,
                ManufactureDate = i.ManufactureDate,
                ExpiryDate = i.ExpiryDate,
                BinLocationId = i.BinLocationId
            }).ToList()
        };

        _db.CompletionOrders.Add(completionOrder);
        await _db.SaveChangesAsync();

        return (await GetCompletionOrderAsync(completionOrder.Id), "ok");
    }

    public async Task<(object? dto, string status)> CompleteOrderAsync(Guid id, Guid? userId = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var completionOrder = await _db.CompletionOrders
                .Include(c => c.Items).ThenInclude(i => i.Sku)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (completionOrder == null)
                return (null, "not_found");

            if (completionOrder.Status != "draft")
                return (null, "invalid");

            var warehouse = await _db.Warehouses.FindAsync(completionOrder.WarehouseId);
            if (warehouse == null)
                return (null, "not_found");

            foreach (var item in completionOrder.Items)
            {
                var sku = item.Sku ?? await _db.Skus.FindAsync(item.SkuId);
                if (sku == null)
                    return (null, "not_found");

                var existingQty = (decimal)await _db.InventoryTransactions
                    .Where(t => t.SkuId == sku.Id && t.WarehouseId == completionOrder.WarehouseId)
                    .Select(t => (double)t.QtyChange)
                    .SumAsync();

                var inTrx = new InventoryTransaction
                {
                    TrxId = $"CO-IN-{completionOrder.CompletionNumber}-{item.Id:N}",
                    SkuId = sku.Id,
                    Type = "COMPLETION_RECEIPT",
                    QtyChange = item.ReceivedQty,
                    QtyBefore = existingQty,
                    QtyAfter = existingQty + item.ReceivedQty,
                    WarehouseId = completionOrder.WarehouseId,
                    BinLocationId = item.BinLocationId,
                    LotNumber = item.LotNumber,
                    RelatedDocType = "CompletionOrder",
                    RelatedDocId = completionOrder.Id,
                    Notes = $"Work order completion receipt",
                    CreatedAt = DateTime.UtcNow
                };
                _db.InventoryTransactions.Add(inTrx);

                sku.Qty += item.ReceivedQty;
                sku.UpdatedAt = DateTime.UtcNow;
            }

            completionOrder.Status = "completed";
            completionOrder.QaStatus = "passed";
            completionOrder.ReceivedBy = userId ?? completionOrder.ReceivedBy;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (await GetCompletionOrderAsync(id), "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool ok, string status)> CancelCompletionOrderAsync(Guid id)
    {
        var completionOrder = await _db.CompletionOrders.FirstOrDefaultAsync(c => c.Id == id);
        if (completionOrder == null)
            return (false, "not_found");

        if (completionOrder.Status == "completed" || completionOrder.Status == "cancelled")
            return (false, "invalid");

        completionOrder.Status = "cancelled";
        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    public async Task<List<object>> GetCompletableWorkOrdersAsync()
    {
        return await _db.CompletionOrders
            .Where(c => c.Status == "draft")
            .Select(c => new
            {
                id = c.Id,
                woNumber = c.CompletionNumber,
                productSkuId = c.Items.Select(i => i.SkuId).FirstOrDefault(),
                plannedQty = c.Items.Sum(i => i.ReceivedQty),
                actualQty = c.Items.Sum(i => i.ReceivedQty),
                status = c.Status
            })
            .Cast<object>()
            .ToListAsync();
    }
}
