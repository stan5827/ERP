using System.Data;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public sealed class SupplierPortalCollaborationService(ErpGeneralDbContext db)
    : ISupplierPortalCollaborationService
{
    private static readonly HashSet<string> ViewablePurchaseOrderStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "confirmed", "partial_received", "received", "completed"
        };

    public async Task<IReadOnlyList<SupplierPortalRfqSummaryDto>> GetRfqsAsync(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        return await db.RfqSupplierInvitations
            .AsNoTracking()
            .Where(invitation => invitation.SupplierId == supplierId
                && invitation.Status != "Withdrawn")
            .OrderByDescending(invitation => invitation.Rfq.RfqDate)
            .Take(100)
            .Select(invitation => new SupplierPortalRfqSummaryDto(
                invitation.RfqId,
                invitation.Rfq.RfqNumber,
                ToUtcOffset(invitation.Rfq.RfqDate),
                invitation.Rfq.Deadline.HasValue
                    ? ToUtcOffset(invitation.Rfq.Deadline.Value)
                    : null,
                invitation.Rfq.Status,
                invitation.Rfq.Items.Count,
                invitation.Rfq.SupplierResponses
                    .Where(response => response.SupplierId == supplierId)
                    .Select(response => response.SkuId)
                    .Distinct()
                    .Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierPortalRfqDetailDto?> GetRfqAsync(
        Guid rfqId,
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var rfq = await db.Rfqs
            .AsNoTracking()
            .Where(item => item.Id == rfqId
                && item.SupplierInvitations.Any(invitation =>
                    invitation.SupplierId == supplierId
                    && invitation.Status != "Withdrawn"))
            .Include(item => item.Items)
                .ThenInclude(item => item.Sku)
            .Include(item => item.SupplierResponses.Where(response => response.SupplierId == supplierId))
            .Include(item => item.SupplierInvitations)
                .ThenInclude(invitation => invitation.Supplier)
            .SingleOrDefaultAsync(cancellationToken);

        if (rfq is null)
            return null;

        var currency = rfq.SupplierInvitations
            .Where(invitation => invitation.SupplierId == supplierId)
            .Select(invitation => invitation.Supplier.Currency)
            .FirstOrDefault() ?? "TWD";
        var ownQuotes = rfq.SupplierResponses
            .GroupBy(response => response.SkuId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(response => response.SubmittedAt).First());
        var items = rfq.Items.Select(item =>
        {
            ownQuotes.TryGetValue(item.SkuId, out var quote);
            return new SupplierPortalRfqItemDto(
                item.SkuId,
                item.Sku?.SkuNumber,
                item.Sku?.SkuName,
                item.Quantity,
                item.Unit,
                item.Specifications,
                quote?.UnitPrice,
                quote?.PromisedDeliveryDate is DateTime promisedDate ? ToUtcOffset(promisedDate) : null,
                quote?.LeadTimeDays,
                quote?.Moq,
                quote?.TradeTerms,
                quote?.Notes,
                quote is null ? null : ToUtcOffset(quote.SubmittedAt));
        }).ToArray();

        return new SupplierPortalRfqDetailDto(
            rfq.Id,
            rfq.RfqNumber,
            ToUtcOffset(rfq.RfqDate),
            rfq.Deadline.HasValue ? ToUtcOffset(rfq.Deadline.Value) : null,
            rfq.Status,
            currency,
            items);
    }

    public async Task<SupplierPortalQuoteSubmittedDto> SubmitQuoteAsync(
        Guid rfqId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        SubmitSupplierRfqQuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        EnsureCanRespond(supplierRole);
        if (request.Items.Count == 0)
            throw new ArgumentException("至少需提供一筆報價明細。");
        if (request.Items.Select(item => item.SkuId).Distinct().Count() != request.Items.Count)
            throw new ArgumentException("同一張報價不可重複提供相同品號。");

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            : null;

        var rfq = await db.Rfqs
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == rfqId, cancellationToken)
            ?? throw new KeyNotFoundException("RFQ 不存在。");
        var invitation = await db.RfqSupplierInvitations
            .SingleOrDefaultAsync(item =>
                item.RfqId == rfqId
                && item.SupplierId == supplierId
                && item.Status != "Withdrawn",
                cancellationToken)
            ?? throw new KeyNotFoundException("RFQ 不存在。");

        if (!string.Equals(rfq.Status, "open", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("此 RFQ 已停止收件。");
        if (rfq.Deadline is DateTime deadline && deadline.ToUniversalTime() < DateTime.UtcNow)
            throw new InvalidOperationException("已超過 RFQ 報價截止時間。");

        var requestedSkuIds = request.Items.Select(item => item.SkuId).ToHashSet();
        var rfqSkuIds = rfq.Items.Select(item => item.SkuId).ToHashSet();
        if (!requestedSkuIds.IsSubsetOf(rfqSkuIds))
            throw new ArgumentException("報價包含不屬於此 RFQ 的品號。");
        if (rfq.Items.GroupBy(item => item.SkuId).Any(group => group.Count() > 1)
            && request.Items.Any(item => rfq.Items.Count(rfqItem => rfqItem.SkuId == item.SkuId) > 1))
            throw new InvalidOperationException("RFQ 含有重複品號，請由採購人員修正後再報價。");
        if (request.Items.Any(item => item.PromisedDeliveryDate is DateTimeOffset date
            && date.UtcDateTime < DateTime.UtcNow.Date))
            throw new ArgumentException("預計交貨日期不可早於今天。");

        var existingQuotes = await db.RfqSupplierResponses
            .Where(response => response.RfqId == rfqId
                && response.SupplierId == supplierId
                && requestedSkuIds.Contains(response.SkuId))
            .ToListAsync(cancellationToken);
        var submittedAt = DateTime.UtcNow;
        foreach (var quoteLine in request.Items)
        {
            var quote = existingQuotes
                .Where(response => response.SkuId == quoteLine.SkuId)
                .OrderByDescending(response => response.SubmittedAt)
                .FirstOrDefault();

            if (quote is null)
            {
                quote = new RfqSupplierResponse
                {
                    RfqId = rfqId,
                    SupplierId = supplierId,
                    SkuId = quoteLine.SkuId
                };
                db.RfqSupplierResponses.Add(quote);
                existingQuotes.Add(quote);
            }

            quote.UnitPrice = quoteLine.UnitPrice;
            quote.LeadTimeDays = quoteLine.LeadTimeDays;
            quote.Moq = quoteLine.MinimumOrderQuantity;
            quote.TradeTerms = NormalizeOptional(quoteLine.TradeTerms);
            quote.Notes = NormalizeOptional(quoteLine.Notes);
            quote.PromisedDeliveryDate = quoteLine.PromisedDeliveryDate?.UtcDateTime;
            quote.SubmittedAt = submittedAt;
        }

        invitation.Status = "Responded";
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return new SupplierPortalQuoteSubmittedDto(
            rfqId,
            request.Items.Count,
            ToUtcOffset(submittedAt));
    }

    public async Task<IReadOnlyList<SupplierPortalPurchaseOrderSummaryDto>> GetPurchaseOrdersAsync(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var orders = await db.PurchaseOrders
            .AsNoTracking()
            .Where(order => order.SupplierId == supplierId
                && ViewablePurchaseOrderStatuses.Contains(order.Status))
            .OrderByDescending(order => order.PoDate)
            .Take(100)
            .Select(order => new
            {
                order.Id,
                order.PoNumber,
                order.PoDate,
                order.ExpectedDeliveryDate,
                order.Status,
                order.Version,
                order.Currency,
                order.TotalAmount,
                ItemCount = order.Items.Count,
                LatestResponseType = order.SupplierResponses
                    .OrderByDescending(response => response.SubmittedAt)
                    .Select(response => response.ResponseType)
                    .FirstOrDefault(),
                LatestResponseVersion = order.SupplierResponses
                    .OrderByDescending(response => response.SubmittedAt)
                    .Select(response => (int?)response.PurchaseOrderVersion)
                    .FirstOrDefault(),
                LatestResponseAt = order.SupplierResponses
                    .OrderByDescending(response => response.SubmittedAt)
                    .Select(response => (DateTime?)response.SubmittedAt)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return orders.Select(order => new SupplierPortalPurchaseOrderSummaryDto(
            order.Id,
            order.PoNumber,
            ToUtcOffset(order.PoDate),
            order.ExpectedDeliveryDate.HasValue
                ? ToUtcOffset(order.ExpectedDeliveryDate.Value)
                : null,
            order.Status,
            order.Version,
            order.Currency,
            order.TotalAmount,
            order.ItemCount,
            order.LatestResponseType,
            order.LatestResponseVersion,
            order.LatestResponseAt.HasValue
                ? ToUtcOffset(order.LatestResponseAt.Value)
                : null)).ToArray();
    }

    public async Task<SupplierPortalPurchaseOrderDetailDto?> GetPurchaseOrderAsync(
        Guid purchaseOrderId,
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders
            .AsNoTracking()
            .Where(item => item.Id == purchaseOrderId
                && item.SupplierId == supplierId
                && ViewablePurchaseOrderStatuses.Contains(item.Status))
            .Include(item => item.Items)
                .ThenInclude(item => item.Sku)
            .Include(item => item.SupplierResponses)
                .ThenInclude(response => response.Items)
            .SingleOrDefaultAsync(cancellationToken);
        if (order is null)
            return null;

        return new SupplierPortalPurchaseOrderDetailDto(
            order.Id,
            order.PoNumber,
            ToUtcOffset(order.PoDate),
            order.ExpectedDeliveryDate.HasValue ? ToUtcOffset(order.ExpectedDeliveryDate.Value) : null,
            order.Status,
            order.Version,
            order.Currency,
            order.PaymentTerms,
            order.TradeTerms,
            order.Notes,
            order.Subtotal,
            order.TaxAmount,
            order.TotalAmount,
            order.Items.Select(item => new SupplierPortalPurchaseOrderItemDto(
                item.Id,
                item.SkuId,
                item.Sku?.SkuNumber,
                item.Sku?.SkuName,
                item.Quantity,
                item.Unit,
                item.UnitPrice,
                item.Subtotal)).ToArray(),
            order.SupplierResponses
                .OrderByDescending(response => response.SubmittedAt)
                .Select(response => new SupplierPortalPurchaseOrderResponseHistoryDto(
                    response.Id,
                    response.ResponseType,
                    response.Reason,
                    response.PurchaseOrderVersion,
                    ToUtcOffset(response.SubmittedAt),
                    response.Items.Select(item => new SupplierPortalPurchaseOrderProposalItemDto(
                        item.PurchaseOrderItemId,
                        item.ProposedQuantity,
                        item.ProposedDeliveryDate.HasValue
                            ? ToUtcOffset(item.ProposedDeliveryDate.Value)
                            : null)).ToArray()))
                .ToArray());
    }

    public async Task<SupplierPortalPurchaseOrderResponseSubmittedDto> SubmitPurchaseOrderResponseAsync(
        Guid purchaseOrderId,
        Guid supplierId,
        Guid supplierUserId,
        string supplierRole,
        SubmitSupplierPurchaseOrderResponseRequestDto request,
        CancellationToken cancellationToken)
    {
        EnsureCanRespond(supplierRole);
        var responseType = request.ResponseType.Trim().ToLowerInvariant();
        if (responseType is not ("accepted" or "change_requested" or "rejected"))
            throw new ArgumentException("採購單回覆類型無效。");

        var reason = NormalizeOptional(request.Reason);
        if (responseType is "change_requested" or "rejected"
            && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("要求變更或拒絕採購單時必須填寫原因。");
        if (responseType != "change_requested" && request.Items.Count > 0)
            throw new ArgumentException("只有要求變更時才能提供替代交期或數量。");
        if (responseType == "change_requested" && request.Items.Count == 0)
            throw new ArgumentException("要求變更時，至少需提出一筆品項交期或數量提案。");
        if (request.Items.Select(item => item.PurchaseOrderItemId).Distinct().Count() != request.Items.Count)
            throw new ArgumentException("同一筆回覆不可重複提出相同採購品項。");
        if (request.Items.Any(item => item.ProposedQuantity is null && item.ProposedDeliveryDate is null))
            throw new ArgumentException("每筆變更提案至少需提供替代數量或交期。");
        if (request.Items.Any(item => item.ProposedDeliveryDate is DateTimeOffset date
            && date.UtcDateTime < DateTime.UtcNow.Date))
            throw new ArgumentException("替代交貨日期不可早於今天。");

        var order = await db.PurchaseOrders
            .Include(item => item.Items)
            .Include(item => item.SupplierResponses)
            .SingleOrDefaultAsync(item => item.Id == purchaseOrderId && item.SupplierId == supplierId, cancellationToken)
            ?? throw new KeyNotFoundException("採購單不存在。");

        if (!string.Equals(order.Status, "confirmed", StringComparison.OrdinalIgnoreCase)
            || order.IsLocked
            || order.Items.Any(item => item.ReceivedQty > 0))
            throw new InvalidOperationException("此採購單目前不可回覆。");

        var latestResponse = order.SupplierResponses
            .OrderByDescending(response => response.SubmittedAt)
            .FirstOrDefault();
        if (latestResponse is not null
            && latestResponse.PurchaseOrderVersion >= order.Version
            && latestResponse.ResponseType is "accepted" or "rejected")
            throw new InvalidOperationException("此採購單在目前版本已完成供應商回覆。");

        var orderItemIds = order.Items.Select(item => item.Id).ToHashSet();
        if (request.Items.Any(item => !orderItemIds.Contains(item.PurchaseOrderItemId)))
            throw new ArgumentException("變更提案包含不屬於此採購單的品項。");

        var submittedAt = DateTime.UtcNow;
        var supplierResponse = new PurchaseOrderSupplierResponse
        {
            PurchaseOrderId = order.Id,
            SupplierId = supplierId,
            SupplierUserId = supplierUserId,
            ResponseType = responseType,
            Reason = reason,
            PurchaseOrderVersion = order.Version,
            SubmittedAt = submittedAt
        };
        foreach (var proposal in request.Items)
        {
            supplierResponse.Items.Add(new PurchaseOrderSupplierResponseItem
            {
                PurchaseOrderItemId = proposal.PurchaseOrderItemId,
                ProposedQuantity = proposal.ProposedQuantity,
                ProposedDeliveryDate = proposal.ProposedDeliveryDate?.UtcDateTime
            });
        }

        db.PurchaseOrderSupplierResponses.Add(supplierResponse);
        await db.SaveChangesAsync(cancellationToken);

        return new SupplierPortalPurchaseOrderResponseSubmittedDto(
            order.Id,
            supplierResponse.Id,
            supplierResponse.ResponseType,
            ToUtcOffset(submittedAt));
    }

    private static void EnsureCanRespond(string supplierRole)
    {
        if (!string.Equals(supplierRole, "sales", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("目前供應商入口角色沒有詢價或採購單回覆權限。");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
