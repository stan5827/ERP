using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class SupplierQuoteServiceTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ErpGeneralDbContext(options);
    }

    [Fact]
    public async Task GetSupplierQuotesAsync_ReturnsAllQuotes()
    {
        using var db = CreateInMemoryDbContext();
        var supplier = new Supplier { SupplierCode = "S001", SupplierName = "Supplier 1" };
        var rfq = new Rfq { RfqNumber = "RFQ001", Status = "open" };
        var sku = new Sku { SkuNumber = "SKU001", SkuName = "Product 1" };
        db.Suppliers.Add(supplier);
        db.Rfqs.Add(rfq);
        db.Skus.Add(sku);
        await db.SaveChangesAsync();

        db.RfqSupplierResponses.AddRange(
            new RfqSupplierResponse { RfqId = rfq.Id, SupplierId = supplier.Id, SkuId = sku.Id, UnitPrice = 10m },
            new RfqSupplierResponse { RfqId = rfq.Id, SupplierId = supplier.Id, SkuId = sku.Id, UnitPrice = 20m });
        await db.SaveChangesAsync();

        var service = new SupplierQuoteService(db);
        var result = await service.GetSupplierQuotesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(20m, result[0].UnitPrice); // ordered by UnitPrice desc
    }

    [Fact]
    public async Task GetSupplierQuotesAsync_FiltersByRfqId()
    {
        using var db = CreateInMemoryDbContext();
        var rfq1 = new Rfq { RfqNumber = "RFQ001" };
        var rfq2 = new Rfq { RfqNumber = "RFQ002" };
        var supplier = new Supplier { SupplierCode = "S001", SupplierName = "Supplier 1" };
        var sku = new Sku { SkuNumber = "SKU001", SkuName = "Product 1" };
        db.Rfqs.AddRange(rfq1, rfq2);
        db.Suppliers.Add(supplier);
        db.Skus.Add(sku);
        await db.SaveChangesAsync();

        db.RfqSupplierResponses.AddRange(
            new RfqSupplierResponse { RfqId = rfq1.Id, SupplierId = supplier.Id, SkuId = sku.Id, UnitPrice = 10m },
            new RfqSupplierResponse { RfqId = rfq2.Id, SupplierId = supplier.Id, SkuId = sku.Id, UnitPrice = 20m });
        await db.SaveChangesAsync();

        var service = new SupplierQuoteService(db);
        var result = await service.GetSupplierQuotesAsync(rfqId: rfq1.Id.ToString());

        Assert.Single(result);
    }

    [Fact]
    public async Task GetSupplierQuoteAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var supplier = new Supplier { SupplierCode = "S001", SupplierName = "Supplier 1" };
        var rfq = new Rfq { RfqNumber = "RFQ001", Status = "open" };
        var sku = new Sku { SkuNumber = "SKU001", SkuName = "Product 1" };
        db.Suppliers.Add(supplier);
        db.Rfqs.Add(rfq);
        db.Skus.Add(sku);
        await db.SaveChangesAsync();

        var quote = new RfqSupplierResponse { RfqId = rfq.Id, SupplierId = supplier.Id, SkuId = sku.Id, UnitPrice = 15m, Moq = 100 };
        db.RfqSupplierResponses.Add(quote);
        await db.SaveChangesAsync();

        var service = new SupplierQuoteService(db);
        var dto = await service.GetSupplierQuoteAsync(quote.Id);

        Assert.NotNull(dto);
        Assert.Equal(15m, dto!.UnitPrice);
        Assert.Equal("Supplier 1", dto.SupplierName);
    }

    [Fact]
    public async Task GetSupplierQuoteAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new SupplierQuoteService(db);
        Assert.Null(await service.GetSupplierQuoteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SelectQuoteAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var quote = new RfqSupplierResponse { RfqId = Guid.NewGuid(), SupplierId = Guid.NewGuid(), SkuId = Guid.NewGuid(), UnitPrice = 10m, IsSelected = false };
        db.RfqSupplierResponses.Add(quote);
        await db.SaveChangesAsync();

        var service = new SupplierQuoteService(db);
        var (ok, status) = await service.SelectQuoteAsync(quote.Id);

        Assert.True(ok);
        Assert.Equal("ok", status);
        Assert.True(quote.IsSelected);
    }

    [Fact]
    public async Task SelectQuoteAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new SupplierQuoteService(db);

        var (ok, status) = await service.SelectQuoteAsync(Guid.NewGuid());

        Assert.False(ok);
        Assert.Equal("not_found", status);
    }

    [Fact]
    public async Task DeleteSupplierQuoteAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var quote = new RfqSupplierResponse { RfqId = Guid.NewGuid(), SupplierId = Guid.NewGuid(), SkuId = Guid.NewGuid(), UnitPrice = 10m };
        db.RfqSupplierResponses.Add(quote);
        await db.SaveChangesAsync();

        var service = new SupplierQuoteService(db);
        var (ok, status) = await service.DeleteSupplierQuoteAsync(quote.Id);

        Assert.True(ok);
        Assert.Equal("ok", status);
        Assert.Empty(db.RfqSupplierResponses);
    }

    [Fact]
    public async Task DeleteSupplierQuoteAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new SupplierQuoteService(db);

        var (ok, status) = await service.DeleteSupplierQuoteAsync(Guid.NewGuid());

        Assert.False(ok);
        Assert.Equal("not_found", status);
    }
}
