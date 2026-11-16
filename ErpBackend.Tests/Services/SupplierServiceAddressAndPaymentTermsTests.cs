using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class SupplierServiceAddressAndPaymentTermsTests
{
    [Fact]
    public async Task GetSuppliersAsync_ReturnsInvoiceAddressAndPaymentTermsForEditForm()
    {
        await using var db = CreateDbContext();
        db.Suppliers.Add(new Supplier
        {
            SupplierCode = "SUP-ADDRESS-TEST",
            SupplierName = "Address Test Supplier",
            Address = "Main address",
            InvoiceAddress = "Invoice address",
            DeliveryAddress = "Delivery address",
            PaymentTerms = "月結30天",
            Currency = "TWD"
        });
        await db.SaveChangesAsync();

        var (items, totalCount) = await new ProcurementService(db)
            .GetSuppliersAsync(null, 1, 20);

        var supplier = Assert.Single(items);
        Assert.Equal(1, totalCount);
        Assert.Equal("Main address", supplier.Address);
        Assert.Equal("Invoice address", supplier.InvoiceAddress);
        Assert.Equal("Delivery address", supplier.DeliveryAddress);
        Assert.Equal("月結30天", supplier.PaymentTerms);
    }

    [Fact]
    public async Task CreateAndUpdateSupplierAsync_PersistAndReturnInvoiceAddressAndPaymentTerms()
    {
        await using var db = CreateDbContext();
        var service = new ProcurementService(db);

        var created = await service.CreateSupplierAsync(new CreateSupplierRequest
        {
            SupplierCode = "SUP-SAVE-TEST",
            SupplierName = "Save Test Supplier",
            InvoiceAddress = "Original invoice address",
            DeliveryAddress = "Original delivery address",
            PaymentTerms = "月結30天",
            Currency = "TWD"
        });

        await service.UpdateSupplierAsync(created.Id, new CreateSupplierRequest
        {
            SupplierCode = created.SupplierCode!,
            SupplierName = created.SupplierName!,
            InvoiceAddress = "Updated invoice address",
            DeliveryAddress = "Updated delivery address",
            PaymentTerms = "月結60天",
            Currency = "TWD"
        });

        var saved = await service.GetSupplierAsync(created.Id);
        Assert.NotNull(saved);
        Assert.Equal("Updated invoice address", saved.InvoiceAddress);
        Assert.Equal("Updated delivery address", saved.DeliveryAddress);
        Assert.Equal("月結60天", saved.PaymentTerms);

        var listItem = Assert.Single((await service.GetSuppliersAsync(null, 1, 20)).Items);
        Assert.Equal("Updated invoice address", listItem.InvoiceAddress);
        Assert.Equal("月結60天", listItem.PaymentTerms);
    }

    private static ErpGeneralDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }
}
