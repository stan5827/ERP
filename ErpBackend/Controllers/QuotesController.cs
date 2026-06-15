using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;

namespace ErpBackend.Controllers;

public class QuotesController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public QuotesController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<dynamic> GetQuotes([FromQuery] Guid? customerId)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.Quotes.Include(q => q.Customer).AsQueryable();

        if (customerId.HasValue)
            query = query.Where(q => q.CustomerId == customerId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var skus = await _db.Skus.ToDictionaryAsync(s => s.Id);

        var dtos = items.Select(q => new QuoteDto
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
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountRate = i.DiscountRate,
                Subtotal = i.Subtotal
            }).ToList()
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpPost]
    public async Task<dynamic> CreateQuote([FromBody] CreateQuoteRequest req)
    {
        var customer = await _db.Customers.FindAsync(req.CustomerId);
        if (customer == null) return BadRequest(ApiHelper.Fail("Error"));

        var quoteNumber = $"QO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

        var entity = new Quote
        {
            QuoteNumber = quoteNumber,
            CustomerId = req.CustomerId,
            QuoteDate = req.QuoteDate,
            ValidUntil = req.ValidUntil,
            DiscountRate = req.DiscountRate,
            Currency = req.Currency,
            Status = "draft",
            CustomerPoNumber = req.CustomerPoNumber,
            CustomerDueDate = req.CustomerDueDate,
            ConfirmedDeliveryDate = req.ConfirmedDeliveryDate,
        };

        decimal totalSubtotal = 0;
        var skuIds = req.Items.Where(i => i.SkuId.HasValue).Select(i => i.SkuId!.Value).ToList();
        var skus = await _db.Skus.Where(s => skuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id);

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
        entity.FinalAmount = totalSubtotal * req.DiscountRate;

        _db.Quotes.Add(entity);
        await _db.SaveChangesAsync();

        var dto = new QuoteDto
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
            Currency = entity.Currency,
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
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountRate = i.DiscountRate,
                Subtotal = i.Subtotal
            }).ToList()
        };

        return Ok(dto, "?±å¹?®å·²å»ºç?");
    }

    [HttpPatch("{id}/approve")]
    public async Task<dynamic> ApproveQuote(Guid id)
    {
        try
        {
            var quote = await _db.Quotes.Include(q => q.Customer).FirstOrDefaultAsync(q => q.Id == id);
            if (quote == null) return BadRequest(ApiHelper.FailOf<QuoteDto>("?¾ä??°å ±?¹å®"));
            if (quote.Status != "draft") return BadRequest(ApiHelper.Fail("Error"));

            quote.Status = "approved";
            await _db.SaveChangesAsync();

            return Ok(new QuoteDto
            {
                Id = quote.Id,
                QuoteNumber = quote.QuoteNumber,
                Status = quote.Status,
                CustomerName = quote.Customer?.CustomerName
            }, "?±å¹?®å·²å¯©æ ¸");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiHelper.FailOf<QuoteDto>($"å¯©æ ¸?±å¹?®å¤±?ï?{ex.Message}"));
        }
    }

    [HttpPatch("{id}/reject")]
    public async Task<dynamic> RejectQuote(Guid id)
    {
        try
        {
            var quote = await _db.Quotes.Include(q => q.Customer).FirstOrDefaultAsync(q => q.Id == id);
            if (quote == null) return BadRequest(ApiHelper.FailOf<QuoteDto>("?¾ä??°å ±?¹å®"));
            if (quote.Status != "draft") return BadRequest(ApiHelper.FailOf<QuoteDto>("?ªè½å°è?æ¡ä¸­?å ±?¹å®?²è?ä½å»¢"));

            quote.Status = "rejected";
            await _db.SaveChangesAsync();

            return Ok(new QuoteDto
            {
                Id = quote.Id,
                QuoteNumber = quote.QuoteNumber,
                Status = quote.Status,
                CustomerName = quote.Customer?.CustomerName
            }, "?±å¹?®å·²ä½å»¢");
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiHelper.FailOf<QuoteDto>($"ä½å»¢?±å¹?®å¤±?ï?{ex.Message}"));
        }
    }

    [HttpPatch("{id}/convert-to-order")]
    public async Task<dynamic> ConvertToOrder(Guid id)
    {
        try
        {
            var quote = await _db.Quotes
                .Include(q => q.Items)
                .Include(q => q.Customer)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quote == null) return BadRequest(ApiHelper.FailOf<SalesOrderDto>("?¾ä??°å ±?¹å®"));
            if (quote.Status != "approved") return BadRequest(ApiHelper.FailOf<SalesOrderDto>("?ªè½è½å®å·²å¯©?¸éé??å ±?¹å®"));

            if (quote.ValidUntil.HasValue && quote.ValidUntil.Value < DateTime.UtcNow)
                return BadRequest(ApiHelper.Fail("Error"));

            await using var tx = await _db.Database.BeginTransactionAsync();

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
                    Quantity = qi.Quantity,
                    UnitPrice = qi.UnitPrice,
                    DiscountRate = qi.DiscountRate,
                    Subtotal = qi.Subtotal,
                });
            }

            _db.SalesOrders.Add(order);
            quote.Status = "converted";

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var createdOrder = await _db.SalesOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            return Ok(new SalesOrderDto
            {
                Id = createdOrder!.Id,
                OrderNumber = createdOrder.OrderNumber,
                CustomerId = createdOrder.CustomerId,
                CustomerName = quote.Customer?.CustomerName,
                TotalAmount = createdOrder.TotalAmount,
                Status = createdOrder.Status
            }, "Success");
        }
        catch (Exception)
        {
            return StatusCode(500, ApiHelper.FailOf<SalesOrderDto>("Error"));
        }
    }
}

