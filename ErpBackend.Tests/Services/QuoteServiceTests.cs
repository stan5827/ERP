using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;
using ErpBackend.Services;
using Xunit;

namespace ErpBackend.Tests.Services;

public class QuoteServiceTests
{
    [Fact]
    public async Task CreateAsync_GeneratesMonthlyQuoteNumberAndContinuesSequence()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);

        var customer = new Customer { CustomerCode = "C001", CustomerName = "Customer" };
        var prefix = $"QUOT-{DateTime.UtcNow:yyyyMM}-";
        db.Customers.Add(customer);
        db.Quotes.Add(new Quote
        {
            QuoteNumber = $"{prefix}007",
            CustomerId = customer.Id,
            QuoteDate = DateTime.UtcNow,
            Currency = "TWD"
        });
        await db.SaveChangesAsync();

        var service = new QuoteService(db);
        var request = new CreateQuoteRequest { CustomerId = customer.Id, Currency = "TWD" };

        var first = await service.CreateAsync(request);
        var second = await service.CreateAsync(request);

        Assert.Equal($"{prefix}008", first?.QuoteNumber);
        Assert.Equal($"{prefix}009", second?.QuoteNumber);
    }

    [Fact]
    public async Task CreateAsync_RejectsSequenceBeyondThreeDigits()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);

        var customer = new Customer { CustomerCode = "C001", CustomerName = "Customer" };
        var prefix = $"QUOT-{DateTime.UtcNow:yyyyMM}-";
        db.Customers.Add(customer);
        db.Quotes.Add(new Quote
        {
            QuoteNumber = $"{prefix}999",
            CustomerId = customer.Id,
            QuoteDate = DateTime.UtcNow,
            Currency = "TWD"
        });
        await db.SaveChangesAsync();

        var service = new QuoteService(db);
        var request = new CreateQuoteRequest { CustomerId = customer.Id, Currency = "TWD" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_NormalizesDateOnlyInputsToUtc()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);

        var customer = new Customer { CustomerCode = "C001", CustomerName = "Customer" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var service = new QuoteService(db);
        var request = new CreateQuoteRequest
        {
            CustomerId = customer.Id,
            Currency = "TWD",
            QuoteDate = new DateTime(2026, 10, 20),
            ValidUntil = new DateTime(2026, 11, 20),
            CustomerDueDate = new DateTime(2026, 11, 25),
            ConfirmedDeliveryDate = new DateTime(2026, 11, 30)
        };

        var created = await service.CreateAsync(request);
        var savedQuote = await db.Quotes.SingleAsync(quote => quote.Id == created!.Id);

        Assert.Equal(DateTimeKind.Utc, savedQuote.QuoteDate.Kind);
        Assert.Equal(DateTimeKind.Utc, savedQuote.ValidUntil!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, savedQuote.CustomerDueDate!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, savedQuote.ConfirmedDeliveryDate!.Value.Kind);
    }

    [Fact]
    public async Task GetQuotesAsync_IncludesSavedItemsWithSkuDetails()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);

        var customer = new Customer { CustomerCode = "C001", CustomerName = "Customer" };
        var sku = new ErpBackend.Models.Inventory.Sku
        {
            SkuNumber = "FG-001",
            SkuName = "A型眼鏡",
            Unit = "BOX",
            SalesUom = new ErpBackend.Models.Inventory.Uom { UomCode = "BRANCH", UomName = "支" }
        };
        var quote = new Quote
        {
            QuoteNumber = "QUOT-202610-001",
            CustomerId = customer.Id,
            Customer = customer,
            QuoteDate = DateTime.UtcNow,
            Currency = "TWD",
            Items =
            [
                new ErpBackend.Models.Sales.QuoteItem
                {
                    ProductId = sku.Id,
                    SkuId = sku.Id,
                    Quantity = 1000,
                    UnitPrice = 280,
                    DiscountRate = 1,
                    Subtotal = 280000
                }
            ]
        };
        db.Customers.Add(customer);
        db.Skus.Add(sku);
        db.Quotes.Add(quote);
        await db.SaveChangesAsync();

        var service = new QuoteService(db);
        var result = await service.GetQuotesAsync();

        var item = Assert.Single(Assert.Single(result).Items);
        Assert.Equal("FG-001", item.SkuNumber);
        Assert.Equal("A型眼鏡", item.SkuName);
        Assert.Equal("BRANCH", item.Unit);
        Assert.Equal("支", item.UnitName);
        Assert.Equal(1000, item.Quantity);
        Assert.Equal(280, item.UnitPrice);
        Assert.Equal(280000, item.Subtotal);
    }

    [Fact]
    public async Task CreateAsync_UsesFullPriceWhenDiscountRatesAreOmitted()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var customer = new Customer { CustomerCode = "C001", CustomerName = "Customer" };
        var sku = new ErpBackend.Models.Inventory.Sku
        {
            SkuNumber = "FG-001",
            SkuName = "A型眼鏡"
        };
        db.Customers.Add(customer);
        db.Skus.Add(sku);
        await db.SaveChangesAsync();

        var service = new QuoteService(db);
        var created = await service.CreateAsync(new CreateQuoteRequest
        {
            CustomerId = customer.Id,
            Currency = "TWD",
            Items =
            [
                new CreateQuoteItemRequest
                {
                    SkuId = sku.Id,
                    ProductId = sku.Id,
                    Quantity = 1000,
                    UnitPrice = 280
                }
            ]
        });

        var savedQuote = await db.Quotes.SingleAsync(quote => quote.Id == created!.Id);
        var savedItem = await db.QuoteItems.SingleAsync(item => item.QuoteId == savedQuote.Id);
        Assert.Equal(280000, savedItem.Subtotal);
        Assert.Equal(280000, savedQuote.TotalAmount);
        Assert.Equal(280000, savedQuote.FinalAmount);
    }

    [Fact]
    public async Task ApproveQuoteAsync_PersistsApprovedStatus()
    {
        await using var db = CreateInMemoryDbContext();
        var quote = await AddDraftQuoteAsync(db);
        var service = new QuoteService(db);

        var result = await service.ApproveQuoteAsync(quote.Id);

        Assert.True(result.found);
        Assert.Equal("ok", result.status);
        Assert.Equal("approved", (await db.Quotes.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task RejectQuoteAsync_PersistsRejectedStatus()
    {
        await using var db = CreateInMemoryDbContext();
        var quote = await AddDraftQuoteAsync(db);
        var service = new QuoteService(db);

        var result = await service.RejectQuoteAsync(quote.Id);

        Assert.True(result.found);
        Assert.Equal("ok", result.status);
        Assert.Equal("rejected", (await db.Quotes.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task ConvertToOrderAsync_CreatesOrderWithDetailsForApprovedQuote()
    {
        await using var db = CreateInMemoryDbContext();
        var customer = new Customer { CustomerCode = "C001", CustomerName = "Customer" };
        var sku = new ErpBackend.Models.Inventory.Sku
        {
            SkuNumber = "FG-001",
            SkuName = "Product",
            IsManufacturable = true
        };
        var quote = new Quote
        {
            QuoteNumber = "QUOT-202610-001",
            CustomerId = customer.Id,
            Customer = customer,
            QuoteDate = DateTime.UtcNow,
            Currency = "TWD",
            Status = "approved",
            TotalAmount = 280000,
            FinalAmount = 280000,
            Items =
            [
                new ErpBackend.Models.Sales.QuoteItem
                {
                    ProductId = sku.Id,
                    SkuId = sku.Id,
                    Quantity = 1000,
                    UnitPrice = 280,
                    DiscountRate = 1,
                    Subtotal = 280000
                }
            ]
        };
        db.Customers.Add(customer);
        db.Skus.Add(sku);
        db.Quotes.Add(quote);
        await db.SaveChangesAsync();
        var service = new QuoteService(db);

        var result = await service.ConvertToOrderAsync(quote.Id);

        Assert.Equal("ok", result.status);
        Assert.NotNull(result.dto);
        Assert.Equal("pending", result.dto.Status);
        Assert.Equal(294000, result.dto.TotalAmount);
        Assert.Single(result.dto.Items);
        Assert.Equal("converted", (await db.Quotes.AsNoTracking().SingleAsync()).Status);
        var order = await db.SalesOrders.Include(item => item.Items).SingleAsync();
        Assert.Equal(280000, Assert.Single(order.Items).Subtotal);
        Assert.True(order.Items.Single().NeedManufacturing);
    }

    private static ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static async Task<Quote> AddDraftQuoteAsync(ErpGeneralDbContext db)
    {
        var customer = new Customer { CustomerCode = "C001", CustomerName = "Customer" };
        var quote = new Quote
        {
            QuoteNumber = $"QUOT-{Guid.NewGuid():N}"[..17],
            CustomerId = customer.Id,
            Customer = customer,
            QuoteDate = DateTime.UtcNow,
            Currency = "TWD",
            Status = "draft"
        };
        db.Customers.Add(customer);
        db.Quotes.Add(quote);
        await db.SaveChangesAsync();
        return quote;
    }
}
