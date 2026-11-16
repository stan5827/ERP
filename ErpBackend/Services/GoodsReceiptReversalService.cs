using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;

namespace ErpBackend.Services;

public interface IGoodsReceiptReversalService
{
    Task<GoodsReceiptReversalDto> ReverseAsync(
        Guid goodsReceiptId,
        string reason,
        Guid reversedBy,
        CancellationToken cancellationToken);
}

public sealed class GoodsReceiptReversalService : IGoodsReceiptReversalService
{
    private const string ReversalIndexName = "IX_InventoryTransactions_ReversalOfTransactionId";
    private readonly ErpGeneralDbContext _db;

    public GoodsReceiptReversalService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<GoodsReceiptReversalDto> ReverseAsync(
        Guid goodsReceiptId,
        string reason,
        Guid reversedBy,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 400)
            throw new ArgumentException("沖銷原因必須為 1 至 400 個字元", nameof(reason));
        if (reversedBy == Guid.Empty)
            throw new ArgumentException("必須提供有效的操作人員", nameof(reversedBy));

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        try
        {
            var receipt = await _db.GoodsReceipts
                .Include(item => item.Items)
                .Include(item => item.PurchaseOrder)
                .FirstOrDefaultAsync(item => item.Id == goodsReceiptId, cancellationToken)
                ?? throw new KeyNotFoundException("找不到收貨單");

            if (receipt.Status != "received" || receipt.IqcStatus != "pending")
                throw new InvalidOperationException("僅能沖銷狀態為 received 且 IQC 為 pending 的收貨單");

            var purchaseOrder = receipt.PurchaseOrder
                ?? throw new InvalidOperationException("收貨單未關聯採購單，無法安全沖銷");

            if (purchaseOrder.Status is not ("received" or "partial_received"))
                throw new InvalidOperationException("採購單狀態已變更，請先核對採購單");
            if (purchaseOrder.IsLocked)
                throw new InvalidOperationException("採購單已鎖定，須先解除鎖定才能沖銷");

            var hasAccountsPayable = await _db.AccountsPayables.AnyAsync(
                item => item.GoodsReceiptId == receipt.Id || item.PurchaseOrderId == purchaseOrder.Id,
                cancellationToken);
            if (hasAccountsPayable)
                throw new InvalidOperationException("採購單或收貨單已有應付帳款記錄，須先完成財務沖銷");

            var hasOtherActiveReceipt = await _db.GoodsReceipts.AnyAsync(
                item => item.PurchaseOrderId == purchaseOrder.Id &&
                        item.Id != receipt.Id &&
                        item.Status != "reversed" &&
                        item.Status != "void" &&
                        item.Status != "cancelled",
                cancellationToken);
            if (hasOtherActiveReceipt)
                throw new InvalidOperationException("採購單另有有效收貨單，須先核對其他收貨單及採購單狀態");

            if (receipt.Items.Count == 0)
                throw new InvalidOperationException("收貨單沒有明細，無法安全沖銷庫存");

            var originalTransactions = await _db.InventoryTransactions
                .Where(item => item.RelatedDocType == "GoodsReceipt" && item.RelatedDocId == receipt.Id)
                .OrderBy(item => item.CreatedAt)
                .ThenBy(item => item.Id)
                .ToListAsync(cancellationToken);

            if (originalTransactions.Count == 0 ||
                originalTransactions.Any(item => item.Type != "purchase_receipt" || item.QtyChange <= 0))
                throw new InvalidOperationException("找不到可核對的正向收貨庫存交易，未執行沖銷");
            if (originalTransactions.Any(item => item.WarehouseId != receipt.WarehouseId))
                throw new InvalidOperationException("收貨單與庫存交易的倉庫不一致，未執行沖銷");
            if (originalTransactions.Any(item => item.QtyAfter != item.QtyBefore + item.QtyChange))
                throw new InvalidOperationException("原始庫存交易前後量快照不一致，須先修正並核對後再沖銷");

            var expectedByItem = receipt.Items
                .GroupBy(item => (item.SkuId, item.BinLocationId))
                .ToDictionary(group => group.Key, group => group.Sum(item => item.InventoryQty));
            var actualByTransaction = originalTransactions
                .GroupBy(item => (item.SkuId, item.BinLocationId))
                .ToDictionary(group => group.Key, group => group.Sum(item => item.QtyChange));

            if (expectedByItem.Count != actualByTransaction.Count ||
                expectedByItem.Any(item =>
                    item.Value <= 0 ||
                    !actualByTransaction.TryGetValue(item.Key, out var quantity) ||
                    quantity != item.Value) ||
                actualByTransaction.Any(item => item.Value <= 0))
                throw new InvalidOperationException("收貨明細與原始庫存交易數量不一致，須先人工核對");

            var skuIds = originalTransactions.Select(item => item.SkuId).Distinct().ToList();
            var originalTransactionIds = originalTransactions.Select(item => item.Id).ToList();
            var skus = await _db.Skus
                .Where(item => skuIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);

            foreach (var skuId in skuIds.OrderBy(id => id))
            {
                var lockedSku = await LockSkuAsync(skuId, cancellationToken);
                if (lockedSku is not null)
                    skus[skuId] = lockedSku;
            }

            foreach (var group in originalTransactions.GroupBy(item => item.SkuId))
            {
                if (!skus.TryGetValue(group.Key, out var sku))
                    throw new InvalidOperationException("收貨明細包含不存在的物料，未執行沖銷");

                var reversalQuantity = group.Sum(item => item.QtyChange);
                if (sku.Qty < reversalQuantity)
                    throw new InvalidOperationException($"物料 {sku.SkuNumber} 的主檔庫存不足，未執行沖銷");
            }

            var lotsToReverse = new Dictionary<(Guid SkuId, string LotNumber, Guid? WarehouseId, Guid? BinLocationId), InventoryLot>();
            foreach (var group in originalTransactions
                         .Where(item => !string.IsNullOrWhiteSpace(item.LotNumber))
                         .GroupBy(item => (item.SkuId, LotNumber: item.LotNumber!, item.WarehouseId, item.BinLocationId)))
            {
                var lot = await _db.InventoryLots.SingleOrDefaultAsync(
                    item => item.SkuId == group.Key.SkuId &&
                            item.LotNumber == group.Key.LotNumber &&
                            item.WarehouseId == group.Key.WarehouseId &&
                            item.BinLocationId == group.Key.BinLocationId,
                    cancellationToken);
                var quantity = group.Sum(item => item.QtyChange);
                if (lot is null || lot.Qty < quantity)
                    throw new InvalidOperationException(
                        $"物料 {skus[group.Key.SkuId].SkuNumber} 的批號庫存不足或不存在，未執行沖銷");

                lotsToReverse.Add(group.Key, lot);
            }

            var purchaseOrderItems = await _db.PurchaseOrderItems
                .Where(item => item.PurchaseOrderId == purchaseOrder.Id)
                .OrderBy(item => item.Id)
                .ToListAsync(cancellationToken);
            var reversedAmount = 0m;
            var purchaseOrderItemBalances = new Dictionary<Guid, decimal>();
            if (purchaseOrderItems.Count > 0)
            {
                foreach (var group in receipt.Items.GroupBy(item => item.SkuId))
                {
                    var remainingQuantity = group.Sum(item => item.ReceivedQty);
                    foreach (var purchaseOrderItem in purchaseOrderItems.Where(item => item.SkuId == group.Key))
                    {
                        var reversedQuantity = Math.Min(purchaseOrderItem.ReceivedQty, remainingQuantity);
                        purchaseOrderItemBalances[purchaseOrderItem.Id] = purchaseOrderItem.ReceivedQty - reversedQuantity;
                        remainingQuantity -= reversedQuantity;
                        reversedAmount += reversedQuantity * purchaseOrderItem.UnitPrice * purchaseOrder.ExchangeRate;
                    }

                    if (remainingQuantity > 0)
                        throw new InvalidOperationException("採購單明細已收數量與收貨單不一致，未執行沖銷");
                }

                if ((purchaseOrder.ReceivedAmount ?? 0m) < reversedAmount)
                    throw new InvalidOperationException("採購單已收金額不足以沖銷，未執行沖銷");
            }

            foreach (var group in originalTransactions.GroupBy(item =>
                         (item.SkuId, item.WarehouseId, item.BinLocationId)))
            {
                var firstReceiptMovement = group.Min(item => item.CreatedAt);
                var hasSubsequentMovement = await _db.InventoryTransactions.AnyAsync(
                    item => item.SkuId == group.Key.SkuId &&
                            item.WarehouseId == group.Key.WarehouseId &&
                            item.BinLocationId == group.Key.BinLocationId &&
                            item.CreatedAt >= firstReceiptMovement &&
                            !originalTransactionIds.Contains(item.Id),
                    cancellationToken);
                if (hasSubsequentMovement)
                    throw new InvalidOperationException(
                        $"物料 {skus[group.Key.SkuId].SkuNumber} 收貨後已有其他庫存異動，須先人工核對");
            }

            var reversals = new List<InventoryTransaction>(originalTransactions.Count);
            var balances = new Dictionary<(Guid SkuId, Guid? WarehouseId, Guid? BinLocationId), decimal>();
            foreach (var original in originalTransactions)
            {
                var balanceKey = (original.SkuId, original.WarehouseId, original.BinLocationId);
                if (!balances.TryGetValue(balanceKey, out var balance))
                {
                    balance = await _db.InventoryTransactions
                        .Where(item =>
                            item.SkuId == original.SkuId &&
                            item.WarehouseId == original.WarehouseId &&
                            item.BinLocationId == original.BinLocationId)
                        .SumAsync(item => (decimal?)item.QtyChange, cancellationToken) ?? 0m;
                    balances.Add(balanceKey, balance);
                }

                if (balance < original.QtyChange)
                    throw new InvalidOperationException(
                        $"物料 {skus[original.SkuId].SkuNumber} 的庫存已被使用或數量不足，未執行沖銷");

                var reversalId = Guid.NewGuid();
                var reversal = new InventoryTransaction
                {
                    TrxId = $"GRR-{reversalId:N}"[..30],
                    SkuId = original.SkuId,
                    Type = "purchase_receipt_reversal",
                    QtyChange = -original.QtyChange,
                    QtyBefore = balance,
                    QtyAfter = balance - original.QtyChange,
                    LotNumber = original.LotNumber,
                    RelatedDocType = "GoodsReceiptReversal",
                    RelatedDocId = receipt.Id,
                    ReversalOfTransactionId = original.Id,
                    WarehouseId = original.WarehouseId,
                    BinLocationId = original.BinLocationId,
                    HandlerId = reversedBy,
                    UnitCost = original.UnitCost,
                    TotalCost = original.TotalCost.HasValue ? -original.TotalCost.Value : null,
                    Notes = $"沖銷 {receipt.GrNumber}；原交易 {original.TrxId}；原因：{reason}",
                    CreatedAt = DateTime.UtcNow
                };
                reversals.Add(reversal);
                balances[balanceKey] = reversal.QtyAfter;
            }

            foreach (var group in originalTransactions.GroupBy(item => item.SkuId))
                skus[group.Key].Qty -= group.Sum(item => item.QtyChange);
            foreach (var group in originalTransactions
                         .Where(item => !string.IsNullOrWhiteSpace(item.LotNumber))
                         .GroupBy(item => (item.SkuId, LotNumber: item.LotNumber!, item.WarehouseId, item.BinLocationId)))
                lotsToReverse[group.Key].Qty -= group.Sum(item => item.QtyChange);
            if (purchaseOrderItems.Count > 0)
            {
                foreach (var purchaseOrderItem in purchaseOrderItems)
                {
                    if (!purchaseOrderItemBalances.TryGetValue(purchaseOrderItem.Id, out var receivedQty))
                        continue;

                    purchaseOrderItem.ReceivedQty = receivedQty;
                    purchaseOrderItem.Status = receivedQty >= purchaseOrderItem.Quantity
                        ? "received"
                        : receivedQty > 0
                            ? "partial_received"
                            : "pending";
                }
                purchaseOrder.ReceivedAmount = (purchaseOrder.ReceivedAmount ?? 0m) - reversedAmount;
            }

            var reversedAt = DateTime.UtcNow;
            receipt.Status = "reversed";
            receipt.IqcStatus = "cancelled";
            purchaseOrder.Status = "confirmed";

            _db.InventoryTransactions.AddRange(reversals);
            _db.AuditLogs.Add(new ErpBackend.Models.System.AuditLog
            {
                Action = "reverse_goods_receipt",
                EntityType = nameof(GoodsReceipt),
                EntityId = receipt.Id,
                Details = reason,
                UserId = reversedBy,
                TableName = "GoodsReceipts",
                CreatedAt = reversedAt
            });

            await _db.SaveChangesAsync(cancellationToken);
            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);

            return new GoodsReceiptReversalDto(
                receipt.Id,
                receipt.GrNumber,
                purchaseOrder.Id,
                purchaseOrder.Status,
                reversedBy,
                new DateTimeOffset(DateTime.SpecifyKind(reversedAt, DateTimeKind.Utc)),
                reason,
                reversals.Select(item => item.TrxId).ToList());
        }
        catch (DbUpdateException exception) when (IsDuplicateReversal(exception))
        {
            throw new InvalidOperationException("此收貨單已被其他操作沖銷，請重新查詢狀態", exception);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new InvalidOperationException("庫存於沖銷期間已變更，請重新核對後再試", exception);
        }
    }

    private static bool IsDuplicateReversal(DbUpdateException exception)
    {
        for (var inner = exception.InnerException; inner != null; inner = inner.InnerException)
        {
            if (inner is PostgresException postgresException &&
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                postgresException.ConstraintName == ReversalIndexName)
                return true;
        }

        return false;
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

        return await _db.Skus.FirstOrDefaultAsync(sku => sku.Id == skuId, cancellationToken);
    }
}
