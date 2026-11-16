using System.Data;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public sealed class SupplierPortalAsnService(ErpGeneralDbContext db)
    : ISupplierPortalAsnService
{
    private static readonly HashSet<string> EligiblePurchaseOrderStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "confirmed", "partial_received" };

    private static readonly HashSet<string> OpenAsnStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "submitted", "partially_received" };

    public async Task<IReadOnlyList<SupplierPortalAsnSummaryDto>> GetAsnsAsync(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var asns = await db.Asns
            .AsNoTracking()
            .Where(asn => asn.SupplierId == supplierId && asn.PurchaseOrderId != null)
            .OrderByDescending(asn => asn.CreatedAt)
            .Take(100)
            .Select(asn => new SupplierPortalAsnSummaryDto(
                asn.Id,
                asn.AsnNumber,
                asn.PurchaseOrderId ?? Guid.Empty,
                asn.PurchaseOrder == null ? string.Empty : asn.PurchaseOrder.PoNumber,
                ToUtcOffset(asn.ShipDate),
                asn.ExpectedArrival.HasValue ? ToUtcOffset(asn.ExpectedArrival.Value) : null,
                asn.Status,
                asn.Carrier,
                asn.TrackingNumber,
                asn.Items.Count,
                ToUtcOffset(asn.CreatedAt)))
            .ToListAsync(cancellationToken);

        return asns;
    }

    public async Task<SupplierPortalAsnDetailDto?> GetAsnAsync(
        Guid asnId,
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var asn = await db.Asns
            .AsNoTracking()
            .Where(item =>
                item.Id == asnId
                && item.SupplierId == supplierId
                && item.PurchaseOrderId != null)
            .Include(item => item.PurchaseOrder)
            .Include(item => item.Items)
                .ThenInclude(item => item.Sku)
            .SingleOrDefaultAsync(cancellationToken);
        if (asn is null || asn.PurchaseOrder is null)
            return null;

        return ToDetail(asn);
    }

    public async Task<SupplierPortalAsnOrderOptionsDto?> GetPurchaseOrderOptionsAsync(
        Guid purchaseOrderId,
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders
            .AsNoTracking()
            .Where(item =>
                item.Id == purchaseOrderId
                && item.SupplierId == supplierId
                && EligiblePurchaseOrderStatuses.Contains(item.Status)
                && !item.IsLocked)
            .Include(item => item.Items)
                .ThenInclude(item => item.Sku)
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null)
            return null;

        var reserved = await GetReservedQuantitiesAsync(order.Id, cancellationToken);
        var items = order.Items
            .GroupBy(item => item.SkuId)
            .Select(group =>
            {
                var first = group.First();
                var ordered = group.Sum(item => item.Quantity);
                var received = group.Sum(item => item.ReceivedQty);
                reserved.TryGetValue(group.Key, out var reservedQuantity);
                return new SupplierPortalAsnOrderItemDto(
                    group.Key,
                    first.Sku?.SkuNumber,
                    first.Sku?.SkuName,
                    first.Unit,
                    ordered,
                    received,
                    reservedQuantity,
                    Math.Max(0m, ordered - received - reservedQuantity),
                    first.Sku?.LotControlled == true);
            })
            .OrderBy(item => item.SkuNumber)
            .ToArray();

        return new SupplierPortalAsnOrderOptionsDto(
            order.Id,
            order.PoNumber,
            order.Status,
            order.ExpectedDeliveryDate.HasValue ? ToUtcOffset(order.ExpectedDeliveryDate.Value) : null,
            items);
    }

    public async Task<SupplierPortalAsnSubmittedDto> CreateAsnAsync(
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        CreateSupplierPortalAsnRequestDto request,
        CancellationToken cancellationToken)
    {
        EnsureCanSubmit(supplierRole);
        ValidateRequest(request);

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var order = await db.PurchaseOrders
            .Include(item => item.Items)
                .ThenInclude(item => item.Sku)
            .SingleOrDefaultAsync(
                item => item.Id == request.PurchaseOrderId && item.SupplierId == supplierId,
                cancellationToken)
            ?? throw new KeyNotFoundException("採購單不存在。");

        if (!EligiblePurchaseOrderStatuses.Contains(order.Status) || order.IsLocked)
            throw new InvalidOperationException("此採購單目前不可建立出貨通知。");

        var orderSkuIds = order.Items.Select(item => item.SkuId).ToHashSet();
        if (request.Items.Any(item => !orderSkuIds.Contains(item.SkuId)))
            throw new ArgumentException("出貨明細包含不屬於此採購單的品號。");

        ValidateLotDetails(order, request);

        var reservedQuantities = await GetReservedQuantitiesAsync(order.Id, cancellationToken);
        var requestedBySku = request.Items
            .GroupBy(item => item.SkuId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.ShippedQuantity));
        foreach (var (skuId, requestedQuantity) in requestedBySku)
        {
            var matchingOrderItems = order.Items.Where(item => item.SkuId == skuId).ToArray();
            var remainingQuantity = matchingOrderItems.Sum(item => item.Quantity - item.ReceivedQty);
            reservedQuantities.TryGetValue(skuId, out var reservedQuantity);
            if (requestedQuantity > remainingQuantity - reservedQuantity)
                throw new InvalidOperationException("出貨數量超過採購單尚未收貨且未被其他出貨通知保留的數量。");
        }

        var now = DateTime.UtcNow;
        var asn = new Asn
        {
            AsnNumber = $"ASN-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..29],
            PurchaseOrderId = order.Id,
            SupplierId = supplierId,
            ShipDate = request.ShipDate.UtcDateTime,
            ExpectedArrival = request.ExpectedArrival.UtcDateTime,
            Carrier = NormalizeOptional(request.Carrier),
            TrackingNumber = NormalizeOptional(request.TrackingNumber),
            Status = "submitted",
            CreatedAt = now
        };
        foreach (var item in request.Items)
        {
            asn.Items.Add(new AsnItem
            {
                AsnId = asn.Id,
                SkuId = item.SkuId,
                ShippedQty = item.ShippedQuantity,
                LotNumber = NormalizeOptional(item.LotNumber),
                ManufactureDate = item.ManufactureDate?.UtcDateTime,
                ExpiryDate = item.ExpiryDate?.UtcDateTime
            });
        }

        db.Asns.Add(asn);
        db.Entry(asn).Property<Guid?>("CreatedBy").CurrentValue = supplierUserId;
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return new SupplierPortalAsnSubmittedDto(
            asn.Id,
            asn.AsnNumber,
            asn.Status,
            ToUtcOffset(asn.CreatedAt));
    }

    private async Task<Dictionary<Guid, decimal>> GetReservedQuantitiesAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken)
    {
        var openAsns = await db.Asns
            .AsNoTracking()
            .Where(asn =>
                asn.PurchaseOrderId == purchaseOrderId
                && OpenAsnStatuses.Contains(asn.Status))
            .Include(asn => asn.Items)
            .ToListAsync(cancellationToken);
        if (openAsns.Count == 0)
            return [];

        var asnIds = openAsns.Select(asn => asn.Id).ToArray();
        var receivedByAsnAndSku = await db.GoodsReceiptItems
            .AsNoTracking()
            .Where(item => item.GoodsReceipt.AsnId.HasValue
                && asnIds.Contains(item.GoodsReceipt.AsnId.Value))
            .Select(item => new
            {
                AsnId = item.GoodsReceipt.AsnId!.Value,
                item.SkuId,
                item.ReceivedQty
            })
            .ToListAsync(cancellationToken);
        var receivedLookup = receivedByAsnAndSku
            .GroupBy(item => (item.AsnId, item.SkuId))
            .ToDictionary(group => group.Key, group => group.Sum(item => item.ReceivedQty));
        var reservedBySku = new Dictionary<Guid, decimal>();

        foreach (var asn in openAsns)
        foreach (var item in asn.Items)
        {
            receivedLookup.TryGetValue((asn.Id, item.SkuId), out var received);
            var outstanding = Math.Max(0m, item.ShippedQty - received);
            reservedBySku[item.SkuId] = reservedBySku.GetValueOrDefault(item.SkuId) + outstanding;
        }

        return reservedBySku;
    }

    private static void ValidateRequest(CreateSupplierPortalAsnRequestDto request)
    {
        if (request.PurchaseOrderId == Guid.Empty)
            throw new ArgumentException("必須指定採購單。");
        if (request.Items is null || request.Items.Count is < 1 or > 100)
            throw new ArgumentException("出貨通知必須包含 1 到 100 筆明細。");
        if (request.Items.Any(item => item.SkuId == Guid.Empty || item.ShippedQuantity <= 0))
            throw new ArgumentException("出貨品號不可空白，出貨數量必須大於零。");
        if (request.Items
            .GroupBy(item => item.SkuId)
            .Any(group => group
                .Select(item => NormalizeOptional(item.LotNumber) ?? string.Empty)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != group.Count()))
            throw new ArgumentException("同一品號不可重複提交相同批號。");
        if (request.ExpectedArrival < request.ShipDate)
            throw new ArgumentException("預計到貨日期不可早於出貨日期。");
    }

    private static void ValidateLotDetails(
        PurchaseOrder order,
        CreateSupplierPortalAsnRequestDto request)
    {
        var lotControlledSkuIds = order.Items
            .Where(item => item.Sku?.LotControlled == true)
            .Select(item => item.SkuId)
            .ToHashSet();
        foreach (var item in request.Items)
        {
            var manufactureDate = item.ManufactureDate?.UtcDateTime.Date;
            var expiryDate = item.ExpiryDate?.UtcDateTime.Date;
            if (lotControlledSkuIds.Contains(item.SkuId)
                && (string.IsNullOrWhiteSpace(item.LotNumber)
                    || manufactureDate is null
                    || expiryDate is null))
                throw new ArgumentException("批號管制品項必須提供批號、製造日期及有效期限。");
            if (manufactureDate.HasValue && manufactureDate.Value > request.ShipDate.UtcDateTime.Date)
                throw new ArgumentException("製造日期不可晚於出貨日期。");
            if (expiryDate.HasValue && expiryDate.Value < request.ExpectedArrival.UtcDateTime.Date)
                throw new ArgumentException("有效期限不可早於預計到貨日期。");
            if (manufactureDate.HasValue
                && expiryDate.HasValue
                && expiryDate.Value <= manufactureDate.Value)
                throw new ArgumentException("有效期限必須晚於製造日期。");
        }
    }

    private static void EnsureCanSubmit(string supplierRole)
    {
        if (!string.Equals(supplierRole, "sales", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("目前供應商入口角色沒有提交出貨通知的權限。");
    }

    private static SupplierPortalAsnDetailDto ToDetail(Asn asn) =>
        new(
            asn.Id,
            asn.AsnNumber,
            asn.PurchaseOrderId ?? Guid.Empty,
            asn.PurchaseOrder!.PoNumber,
            ToUtcOffset(asn.ShipDate),
            asn.ExpectedArrival.HasValue ? ToUtcOffset(asn.ExpectedArrival.Value) : null,
            asn.Status,
            asn.Carrier,
            asn.TrackingNumber,
            ToUtcOffset(asn.CreatedAt),
            asn.Items.Select(item => new SupplierPortalAsnItemDto(
                item.SkuId,
                item.Sku?.SkuNumber,
                item.Sku?.SkuName,
                item.ShippedQty,
                item.LotNumber,
                item.ManufactureDate.HasValue ? ToUtcOffset(item.ManufactureDate.Value) : null,
                item.ExpiryDate.HasValue ? ToUtcOffset(item.ExpiryDate.Value) : null))
                .ToArray());

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
