using System.Data;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;

namespace ErpBackend.Services;

public interface IQuoteService
{
    Task<List<QuoteDto>> GetQuotesAsync(Guid? customerId = null, int page = 1, int pageSize = 20);
    Task<QuoteDto?> CreateAsync(CreateQuoteRequest req, CancellationToken cancellationToken = default);
    Task<(bool found, string status)> ApproveQuoteAsync(Guid id);
    Task<(bool found, string status)> RejectQuoteAsync(Guid id);
    Task<(SalesOrderDto? dto, string status)> ConvertToOrderAsync(Guid id);
}

public class QuoteService : IQuoteService
{
    private readonly ErpGeneralDbContext _db;

    public QuoteService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<QuoteDto>> GetQuotesAsync(Guid? customerId = null, int page = 1, int pageSize = 20)
    {
        var query = _db.Quotes
            .Include(q => q.Customer)
            .Include(q => q.Items)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(q => q.CustomerId == customerId.Value);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var skus = await _db.Skus
            .AsNoTracking()
            .Include(sku => sku.SalesUom)
            .ToDictionaryAsync(sku => sku.Id);

        return items.Select(q => new QuoteDto
        {
            Id = q.Id,
            QuoteNumber = q.QuoteNumber,
            CustomerId = q.CustomerId,
            CustomerName = q.Customer?.CustomerName,
            QuoteDate = q.QuoteDate,
            ValidUntil = q.ValidUntil,
            TotalAmount = q.TotalAmount,
            DiscountRate = q.DiscountRate,
            FinalAmount = q.FinalAmount,
            Currency = q.Currency,
            Status = q.Status ?? "draft",
            CreatedAt = q.CreatedAt,
            Items = q.Items.Select(i => new QuoteItemDto
            {
                Id = i.Id,
                QuoteId = i.QuoteId,
                ProductId = i.ProductId,
                SkuId = i.SkuId,
                SkuNumber = i.SkuId.HasValue ? skus.GetValueOrDefault(i.SkuId.Value)?.SkuNumber : null,
                SkuName = i.SkuId.HasValue ? skus.GetValueOrDefault(i.SkuId.Value)?.SkuName : null,
                Unit = i.SkuId.HasValue
                    ? skus.GetValueOrDefault(i.SkuId.Value)?.SalesUom?.UomCode
                        ?? skus.GetValueOrDefault(i.SkuId.Value)?.Unit
                    : null,
                UnitName = i.SkuId.HasValue
                    ? skus.GetValueOrDefault(i.SkuId.Value)?.SalesUom?.UomName
                    : null,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountRate = i.DiscountRate,
                Subtotal = i.Subtotal
            }).ToList()
        }).ToList();
    }

    public async Task<QuoteDto?> CreateAsync(
        CreateQuoteRequest req,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            : null;

        var quoteDate = NormalizeUtc(req.QuoteDate) ?? DateTime.UtcNow;
        var quoteNumber = await GenerateQuoteNumberAsync(DateTime.UtcNow, cancellationToken);
        var customer = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.CustomerId, cancellationToken);
        if (customer == null) return null;

        var entity = new Quote
        {
            QuoteNumber = quoteNumber,
            CustomerId = req.CustomerId,
            QuoteDate = quoteDate,
            ValidUntil = NormalizeUtc(req.ValidUntil),
            DiscountRate = req.DiscountRate ?? 1m,
            Currency = req.Currency!,
            Status = "draft",
            CustomerPoNumber = req.CustomerPoNumber,
            CustomerDueDate = NormalizeUtc(req.CustomerDueDate),
            ConfirmedDeliveryDate = NormalizeUtc(req.ConfirmedDeliveryDate),
        };

        decimal totalSubtotal = 0;
        var skuIds = req.Items.Where(i => i.SkuId.HasValue).Select(i => i.SkuId!.Value).ToList();
        var skus = await _db.Skus.Where(s => skuIds.Contains(s.Id))
            .AsNoTracking()
            .Include(sku => sku.SalesUom)
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        foreach (var item in req.Items)
        {
            var subtotal = item.Quantity * item.UnitPrice * item.DiscountRate;
            totalSubtotal += subtotal;

            entity.Items.Add(new QuoteItem
            {
                ProductId = item.ProductId ?? Guid.Empty,
                SkuId = item.SkuId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountRate = item.DiscountRate,
                Subtotal = subtotal,
            });
        }

        entity.TotalAmount = totalSubtotal;
        entity.FinalAmount = totalSubtotal * (req.DiscountRate ?? 1m);

        _db.Quotes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return new QuoteDto
        {
            Id = entity.Id,
            QuoteNumber = entity.QuoteNumber,
            CustomerId = entity.CustomerId,
            CustomerName = customer.CustomerName,
            QuoteDate = entity.QuoteDate,
            ValidUntil = entity.ValidUntil,
            TotalAmount = entity.TotalAmount,
            DiscountRate = entity.DiscountRate,
            FinalAmount = entity.FinalAmount,
            Currency = entity.Currency!,
            Status = entity.Status,
            CustomerPoNumber = entity.CustomerPoNumber,
            CustomerDueDate = entity.CustomerDueDate,
            ConfirmedDeliveryDate = entity.ConfirmedDeliveryDate,
            CreatedAt = entity.CreatedAt,
            Items = entity.Items.Select(i => new QuoteItemDto
            {
                Id = i.Id,
                QuoteId = i.QuoteId,
                ProductId = i.ProductId,
                SkuId = i.SkuId,
                SkuNumber = i.SkuId.HasValue ? skus.GetValueOrDefault(i.SkuId.Value)?.SkuNumber : null,
                SkuName = i.SkuId.HasValue ? skus.GetValueOrDefault(i.SkuId.Value)?.SkuName : null,
                Unit = i.SkuId.HasValue
                    ? skus.GetValueOrDefault(i.SkuId.Value)?.SalesUom?.UomCode
                        ?? skus.GetValueOrDefault(i.SkuId.Value)?.Unit
                    : null,
                UnitName = i.SkuId.HasValue
                    ? skus.GetValueOrDefault(i.SkuId.Value)?.SalesUom?.UomName
                    : null,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountRate = i.DiscountRate,
                Subtotal = i.Subtotal
            }).ToList()
        };
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (value is null) return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => value.Value
        };
    }

    private async Task<string> GenerateQuoteNumberAsync(
        DateTime issuedAtUtc,
        CancellationToken cancellationToken)
    {
        var prefix = $"QUOT-{issuedAtUtc:yyyyMM}-";

        if (_db.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({prefix}, 0))",
                cancellationToken);

        var existingNumbers = await _db.Quotes
            .Where(quote => quote.QuoteNumber.StartsWith(prefix))
            .Select(quote => quote.QuoteNumber)
            .ToListAsync(cancellationToken);

        var latestSequence = 0;
        foreach (var number in existingNumbers)
        {
            var suffix = number[prefix.Length..];
            if (suffix.Length == 3 &&
                int.TryParse(suffix, out var sequence) &&
                sequence > latestSequence)
            {
                latestSequence = sequence;
            }
        }

        if (latestSequence >= 999)
            throw new InvalidOperationException($"報價編號 {prefix} 的三位數流水號已用完");

        return $"{prefix}{latestSequence + 1:D3}";
    }

    public async Task<(bool found, string status)> ApproveQuoteAsync(Guid id)
    {
        var quote = await _db.Quotes
            .Include(q => q.Customer)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quote == null) return (false, "not_found");
        if (quote.Status != "draft") return (true, "invalid_state");

        quote.Status = "approved";
        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    public async Task<(bool found, string status)> RejectQuoteAsync(Guid id)
    {
        var quote = await _db.Quotes
            .Include(q => q.Customer)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quote == null) return (false, "not_found");
        if (quote.Status != "draft") return (true, "invalid_state");

        quote.Status = "rejected";
        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    public async Task<(SalesOrderDto? dto, string status)> ConvertToOrderAsync(Guid id)
    {
        var quote = await _db.Quotes
            .Include(q => q.Items)
                .ThenInclude(item => item.Sku)
                    .ThenInclude(sku => sku!.SalesUom)
            .Include(q => q.Customer)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quote == null) return (null, "not_found");
        if (quote.Status != "approved") return (null, "not_approved");

        if (quote.ValidUntil.HasValue && quote.ValidUntil.Value < DateTime.UtcNow)
            return (null, "expired");

        var orderNumber = $"SO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var order = new SalesOrder
        {
            OrderNumber = orderNumber,
            CustomerId = quote.CustomerId,
            QuoteId = quote.Id,
            OrderDate = DateTime.UtcNow,
            Currency = quote.Currency,
            Subtotal = quote.FinalAmount,
            TaxAmount = Math.Round(quote.FinalAmount * 0.05m, 2),
            TotalAmount = quote.FinalAmount + Math.Round(quote.FinalAmount * 0.05m, 2),
            CustomerPoNumber = quote.CustomerPoNumber,
            CustomerDueDate = quote.CustomerDueDate,
            ConfirmedDeliveryDate = quote.ConfirmedDeliveryDate,
            Status = "pending",
        };

        foreach (var qi in quote.Items)
        {
            order.Items.Add(new SalesOrderItem
            {
                ProductId = qi.ProductId,
                SkuId = qi.SkuId,
                LineNumber = order.Items.Count + 1,
                Quantity = qi.Quantity,
                UnitPrice = qi.UnitPrice,
                DiscountRate = qi.DiscountRate,
                Subtotal = qi.Subtotal,
                NeedManufacturing = qi.Sku?.IsManufacturable == true,
                NeedOutsourcing = qi.Sku?.IsOutsourced == true
            });
        }

        _db.SalesOrders.Add(order);
        quote.Status = "converted";

        await _db.SaveChangesAsync();

        var dto = new SalesOrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerName = quote.Customer?.CustomerName,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            Items = order.Items.Select(item => new SalesOrderItemDto
            {
                Id = item.Id,
                OrderId = order.Id,
                SkuId = item.SkuId ?? item.ProductId,
                SkuNumber = item.SkuId.HasValue
                    ? quote.Items.FirstOrDefault(quoteItem => quoteItem.SkuId == item.SkuId)?.Sku?.SkuNumber
                    : null,
                SkuName = item.SkuId.HasValue
                    ? quote.Items.FirstOrDefault(quoteItem => quoteItem.SkuId == item.SkuId)?.Sku?.SkuName
                    : null,
                Unit = item.SkuId.HasValue
                    ? quote.Items.FirstOrDefault(quoteItem => quoteItem.SkuId == item.SkuId)?.Sku?.SalesUom?.UomCode
                        ?? quote.Items.FirstOrDefault(quoteItem => quoteItem.SkuId == item.SkuId)?.Sku?.Unit
                    : null,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountRate = item.DiscountRate,
                Subtotal = item.Subtotal,
                NeedManufacturing = item.NeedManufacturing,
                NeedOutsourcing = item.NeedOutsourcing
            }).ToList()
        };

        return (dto, "ok");
    }
}
