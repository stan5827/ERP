using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Helpers;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Sales;
using System.Data;

namespace ErpBackend.Services;

public interface IDeliveryOrderService
{
    Task<List<DeliveryOrderDto>> GetDeliveryOrdersAsync(string? status = null, string? searchTerm = null, int page = 1, int pageSize = 20);
    Task<DeliveryOrderDto?> GetDeliveryOrderAsync(Guid id);
    Task<(DeliveryOrderDto? dto, string status)> CreateDeliveryOrderAsync(CreateDeliveryOrderRequest req);
    Task<(bool found, string status)> UpdateStatusAsync(Guid id, UpdateDeliveryStatusRequest req);
    Task<(bool found, string status)> SignDeliveryAsync(Guid id, SignDeliveryRequest req);
}

public class DeliveryOrderService : IDeliveryOrderService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IAccountingService _accounting;

    public DeliveryOrderService(ErpGeneralDbContext db, IAccountingService accounting)
    {
        _db = db;
        _accounting = accounting;
    }

    public async Task<List<DeliveryOrderDto>> GetDeliveryOrdersAsync(string? status = null, string? searchTerm = null, int page = 1, int pageSize = 20)
    {
        var query = _db.DeliveryOrders
            .Include(d => d.SalesOrder)
                .ThenInclude(s => s!.Customer)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == status);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(d => d.DeliveryNumber.Contains(searchTerm) ||
                (d.SalesOrder != null && d.SalesOrder.Customer != null && d.SalesOrder.Customer.CustomerName.Contains(searchTerm)));

        var items = await query
            .AsNoTracking()
            .OrderByDescending(d => d.DeliveryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return items.Select(d => new DeliveryOrderDto
        {
            Id = d.Id,
            DeliveryNumber = d.DeliveryNumber,
            SalesOrderId = d.SalesOrderId,
            SalesOrderNumber = d.SalesOrder?.OrderNumber,
            CustomerName = d.SalesOrder?.Customer?.CustomerName,
            DeliveryDate = d.DeliveryDate,
            ShippingMethod = d.ShippingMethod,
            TrackingNumber = d.TrackingNumber,
            DeliveryAddress = d.DeliveryAddress,
            SignedBy = d.SignedBy,
            SignedAt = d.SignedAt,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            ItemCount = d.Items.Count
        }).ToList();
    }

    public async Task<DeliveryOrderDto?> GetDeliveryOrderAsync(Guid id)
    {
        var d = await _db.DeliveryOrders
            .AsNoTracking()
            .Include(d => d.SalesOrder)
                .ThenInclude(s => s!.Customer)
            .Include(d => d.Items)
                .ThenInclude(i => i.Sku)
            .Include(d => d.Items)
                .ThenInclude(i => i.Warehouse)
            .Include(d => d.Items)
                .ThenInclude(i => i.BinLocation)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (d == null) return null;

        return new DeliveryOrderDto
        {
            Id = d.Id,
            DeliveryNumber = d.DeliveryNumber,
            SalesOrderId = d.SalesOrderId,
            SalesOrderNumber = d.SalesOrder?.OrderNumber,
            CustomerName = d.SalesOrder?.Customer?.CustomerName,
            DeliveryDate = d.DeliveryDate,
            ShippingMethod = d.ShippingMethod,
            TrackingNumber = d.TrackingNumber,
            DeliveryAddress = d.DeliveryAddress,
            SignedBy = d.SignedBy,
            SignedAt = d.SignedAt,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            Items = d.Items.Select(i => new DeliveryOrderItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                WarehouseId = i.WarehouseId,
                BinLocationId = i.BinLocationId,
                LotNumber = i.LotNumber,
                DeliveredQty = i.DeliveredQty,
                SerialNumber = i.SerialNumber
            }).ToList()
        };
    }

    public async Task<(DeliveryOrderDto? dto, string status)> CreateDeliveryOrderAsync(CreateDeliveryOrderRequest req)
    {
        var salesOrder = await _db.SalesOrders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == req.SalesOrderId);
        if (salesOrder == null) return (null, "sales_order_not_found");
        if (req.Items == null || req.Items.Count == 0 ||
            req.Items.Any(item => item.Quantity <= 0 || item.WarehouseId == null ||
                                  item.BinLocationId == null || string.IsNullOrWhiteSpace(item.LotNumber)))
            return (null, "invalid_items");

        foreach (var item in req.Items)
        {
            var sku = await _db.Skus.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == item.SkuId);
            if (sku == null || !salesOrder.Items.Any(orderItem =>
                    (orderItem.SkuId ?? orderItem.ProductId) == item.SkuId))
                return (null, "sku_not_in_sales_order");

            var bin = await _db.BinLocations.AsNoTracking().FirstOrDefaultAsync(candidate =>
                candidate.Id == item.BinLocationId && candidate.WarehouseId == item.WarehouseId &&
                candidate.IsActive);
            if (bin == null ||
                !await _db.Warehouses.AnyAsync(warehouse =>
                    warehouse.Id == item.WarehouseId && warehouse.IsActive))
                return (null, "invalid_inventory_location");
        }

        var skuQuantities = req.Items.GroupBy(item => item.SkuId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        foreach (var (skuId, quantity) in skuQuantities)
        {
            var orderedQuantity = salesOrder.Items
                .Where(item => (item.SkuId ?? item.ProductId) == skuId)
                .Sum(item => item.Quantity);
            var plannedDeliveryIds = await _db.DeliveryOrders
                .Where(item => item.SalesOrderId == salesOrder.Id && item.Status != "cancelled")
                .Select(item => item.Id)
                .ToListAsync();
            var previouslyPlanned = await _db.DeliveryOrderItems
                .Where(item => item.SkuId == skuId && plannedDeliveryIds.Contains(item.DeliveryOrderId))
                .SumAsync(item => (decimal?)item.DeliveredQty) ?? 0m;
            if (previouslyPlanned + quantity > orderedQuantity)
                return (null, "order_quantity_exceeded");
        }

        var delivery = new DeliveryOrder
        {
            DeliveryNumber = DocumentNumberGenerator.Generate("DEL"),
            SalesOrderId = req.SalesOrderId,
            DeliveryDate = req.DeliveryDate ?? DateTime.UtcNow,
            ShippingMethod = req.ShippingMethod,
            TrackingNumber = req.TrackingNumber,
            DeliveryAddress = req.DeliveryAddress,
            Status = "pending"
        };

        foreach (var item in req.Items)
        {
            delivery.Items.Add(new DeliveryOrderItem
            {
                DeliveryOrderId = delivery.Id,
                SkuId = item.SkuId,
                LotNumber = item.LotNumber!.Trim(),
                DeliveredQty = item.Quantity,
                WarehouseId = item.WarehouseId,
                BinLocationId = item.BinLocationId,
                SerialNumber = item.SerialNumber
            });
        }

        _db.DeliveryOrders.Add(delivery);
        await _db.SaveChangesAsync();

        return (new DeliveryOrderDto
        {
            Id = delivery.Id,
            DeliveryNumber = delivery.DeliveryNumber,
            SalesOrderId = delivery.SalesOrderId,
            DeliveryDate = delivery.DeliveryDate,
            Status = delivery.Status,
            CreatedAt = delivery.CreatedAt
        }, "ok");
    }

    public async Task<(bool found, string status)> UpdateStatusAsync(Guid id, UpdateDeliveryStatusRequest req)
    {
        var delivery = await _db.DeliveryOrders.FirstOrDefaultAsync(d => d.Id == id);
        if (delivery == null) return (false, "not_found");

        if (req.Status is not ("pending" or "cancelled") || delivery.Status == "completed")
            return (true, "guarded");

        delivery.Status = req.Status;
        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    public async Task<(bool found, string status)> SignDeliveryAsync(Guid id, SignDeliveryRequest req)
    {
        await using var transactionScope = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var delivery = await _db.DeliveryOrders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id);
        if (delivery == null) return (false, "not_found");
        if (delivery.Status != "pending" || delivery.Items.Count == 0)
            return (true, "guarded");

        var salesOrder = await _db.SalesOrders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == delivery.SalesOrderId);
        if (salesOrder == null) return (true, "sales_order_not_found");

        var deliveryQuantities = delivery.Items
            .GroupBy(item => item.SkuId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.DeliveredQty));
        foreach (var (skuId, quantity) in deliveryQuantities)
        {
            var orderedQuantity = salesOrder.Items
                .Where(item => (item.SkuId ?? item.ProductId) == skuId)
                .Sum(item => item.Quantity);
            if (orderedQuantity == 0) return (true, "sku_not_in_sales_order");

            var previousDeliveryIds = await _db.DeliveryOrders
                .Where(item => item.SalesOrderId == salesOrder.Id &&
                               item.Status == "completed" && item.Id != delivery.Id)
                .Select(item => item.Id)
                .ToListAsync();
            var previouslyDelivered = await _db.DeliveryOrderItems
                .Where(item => item.SkuId == skuId && previousDeliveryIds.Contains(item.DeliveryOrderId))
                .SumAsync(item => (decimal?)item.DeliveredQty) ?? 0m;
            if (previouslyDelivered + quantity > orderedQuantity)
                return (true, "order_quantity_exceeded");
        }

        var stockGroups = delivery.Items.GroupBy(item => new
        {
            item.SkuId,
            item.WarehouseId,
            item.BinLocationId,
            item.LotNumber
        });
        var postingLines = new List<(InventoryLot Lot, Sku Sku, decimal Quantity, decimal BinBalance, decimal UnitCost)>();
        foreach (var group in stockGroups)
        {
            var item = group.First();
            var quantity = group.Sum(line => line.DeliveredQty);
            var sku = await _db.Skus.FirstOrDefaultAsync(candidate => candidate.Id == item.SkuId);
            if (sku == null) return (true, "sku_not_found");
            var lot = await _db.InventoryLots.FirstOrDefaultAsync(stock =>
                stock.SkuId == item.SkuId && stock.LotNumber == item.LotNumber &&
                stock.WarehouseId == item.WarehouseId && stock.BinLocationId == item.BinLocationId &&
                stock.Status == "active");
            if (lot == null || lot.Qty < quantity)
                return (true, "insufficient_stock");

            var binBalance = await _db.InventoryTransactions
                .Where(transaction => transaction.SkuId == item.SkuId &&
                                      transaction.WarehouseId == item.WarehouseId &&
                                      transaction.BinLocationId == item.BinLocationId &&
                                      transaction.LotNumber == item.LotNumber)
                .SumAsync(transaction => (decimal?)transaction.QtyChange) ?? 0m;
            var totalSkuQuantity = delivery.Items
                .Where(line => line.SkuId == item.SkuId)
                .Sum(line => line.DeliveredQty);
            if (binBalance < quantity || sku.Qty < totalSkuQuantity)
                return (true, "insufficient_stock");

            var costTransactions = await _db.InventoryTransactions
                .Where(transaction => transaction.SkuId == item.SkuId &&
                                      transaction.WarehouseId == item.WarehouseId &&
                                      transaction.BinLocationId == item.BinLocationId &&
                                      transaction.LotNumber == item.LotNumber &&
                                      transaction.QtyChange > 0)
                .Select(transaction => new
                {
                    transaction.QtyChange,
                    transaction.UnitCost,
                    transaction.TotalCost
                })
                .ToListAsync();
            if (costTransactions.Count == 0 || costTransactions.Any(transaction =>
                    !transaction.UnitCost.HasValue || transaction.UnitCost.Value < 0 ||
                    transaction.TotalCost < 0))
                return (true, "lot_cost_unavailable");

            var receivedQty = costTransactions.Sum(transaction => transaction.QtyChange);
            if (receivedQty <= 0)
                return (true, "lot_cost_unavailable");
            var totalReceiptCost = costTransactions.Sum(transaction =>
                transaction.TotalCost ?? transaction.QtyChange * transaction.UnitCost!.Value);
            var unitCost = totalReceiptCost / receivedQty;
            postingLines.Add((lot, sku, quantity, binBalance, unitCost));
        }

        var missingCogsMapping = await _accounting.ValidateDeliveryCogsMappingsAsync();
        if (missingCogsMapping is not null)
            return (true, $"account_mapping_missing:{missingCogsMapping}");

        foreach (var postingLine in postingLines)
        {
            var unitCost = postingLine.UnitCost;
            var quantity = postingLine.Quantity;
            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                TrxId = DocumentNumberGenerator.GenerateShort("DEL"),
                SkuId = postingLine.Sku.Id,
                Type = "sales_delivery",
                QtyChange = -quantity,
                QtyBefore = postingLine.BinBalance,
                QtyAfter = postingLine.BinBalance - quantity,
                LotNumber = postingLine.Lot.LotNumber,
                RelatedDocType = "DeliveryOrder",
                RelatedDocId = delivery.Id,
                WarehouseId = postingLine.Lot.WarehouseId,
                BinLocationId = postingLine.Lot.BinLocationId,
                UnitCost = unitCost,
                TotalCost = Math.Round(quantity * unitCost, 2),
                Notes = $"銷貨出貨 {delivery.DeliveryNumber}",
                CreatedAt = DateTime.UtcNow
            });
            postingLine.Lot.Qty -= quantity;
            if (postingLine.Lot.Qty == 0)
                postingLine.Lot.Status = "depleted";
            postingLine.Sku.Qty -= quantity;
            postingLine.Sku.UpdatedAt = DateTime.UtcNow;
        }
        var totalCogs = postingLines.Sum(line => Math.Round(line.Quantity * line.UnitCost, 2));
        await _accounting.CreateVoucherFromDeliveryAsync(delivery.Id, delivery.DeliveryNumber, totalCogs);

        var completedDeliveryIds = await _db.DeliveryOrders
            .Where(item => item.SalesOrderId == salesOrder.Id &&
                           item.Status == "completed" && item.Id != delivery.Id)
            .Select(item => item.Id)
            .ToListAsync();
        completedDeliveryIds.Add(delivery.Id);
        var deliveredBySku = await _db.DeliveryOrderItems
            .Where(item => completedDeliveryIds.Contains(item.DeliveryOrderId))
            .GroupBy(item => item.SkuId)
            .Select(group => new { SkuId = group.Key, Quantity = group.Sum(item => item.DeliveredQty) })
            .ToDictionaryAsync(item => item.SkuId, item => item.Quantity);
        foreach (var orderItem in salesOrder.Items)
        {
            var skuId = orderItem.SkuId ?? orderItem.ProductId;
            var alreadyAssigned = salesOrder.Items
                .Where(candidate => candidate.LineNumber < orderItem.LineNumber &&
                                    (candidate.SkuId ?? candidate.ProductId) == skuId)
                .Sum(candidate => candidate.DeliveredQty);
            var remainingDelivered = Math.Max(0, deliveredBySku.GetValueOrDefault(skuId) - alreadyAssigned);
            orderItem.DeliveredQty = Math.Min(orderItem.Quantity, remainingDelivered);
        }

        delivery.SignedBy = req.SignedBy;
        delivery.SignedAt = DateTime.UtcNow;
        delivery.Status = "completed";
        await _db.SaveChangesAsync();
        if (transactionScope != null)
            await transactionScope.CommitAsync();

        return (true, "ok");
    }
}
