using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class SupplierPortalAsnServiceTests
{
    [Fact]
    public async Task CreateAsnAsync_UsesExistingProcurementAsnModels()
    {
        await using var db = CreateDbContext();
        var (supplier, sku, order) = SeedPurchaseOrder(db);
        await db.SaveChangesAsync();
        var service = new SupplierPortalAsnService(db);

        var result = await service.CreateAsnAsync(
            supplier.Id,
            Guid.NewGuid(),
            "sales",
            CreateRequest(order.Id, sku.Id, 6),
            CancellationToken.None);

        var asn = await db.Asns.Include(item => item.Items).SingleAsync();
        Assert.Equal(asn.Id, result.AsnId);
        Assert.StartsWith("ASN-", asn.AsnNumber);
        Assert.Equal(supplier.Id, asn.SupplierId);
        Assert.Equal(order.Id, asn.PurchaseOrderId);
        Assert.Equal("submitted", asn.Status);
        Assert.Equal(6m, Assert.Single(asn.Items).ShippedQty);
    }

    [Fact]
    public async Task CreateAsnAsync_RejectsPurchaseOrderOwnedByAnotherSupplier()
    {
        await using var db = CreateDbContext();
        var (supplier, sku, order) = SeedPurchaseOrder(db);
        await db.SaveChangesAsync();
        var service = new SupplierPortalAsnService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsnAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "sales",
            CreateRequest(order.Id, sku.Id, 1),
            CancellationToken.None));

        Assert.Empty(await db.Asns.ToListAsync());
    }

    [Fact]
    public async Task CreateAsnAsync_RejectsQuantityAlreadyReceivedOrReservedByAnotherAsn()
    {
        await using var db = CreateDbContext();
        var (supplier, sku, order) = SeedPurchaseOrder(db, quantity: 10, receivedQuantity: 2);
        var existingAsn = new Asn
        {
            AsnNumber = "ASN-EXISTING",
            SupplierId = supplier.Id,
            PurchaseOrderId = order.Id,
            ShipDate = DateTime.UtcNow.Date,
            ExpectedArrival = DateTime.UtcNow.Date.AddDays(1),
            Status = "submitted"
        };
        existingAsn.Items.Add(new AsnItem
        {
            AsnId = existingAsn.Id,
            SkuId = sku.Id,
            ShippedQty = 5,
            LotNumber = "LOT-1",
            ManufactureDate = DateTime.UtcNow.Date.AddDays(-10),
            ExpiryDate = DateTime.UtcNow.Date.AddDays(60)
        });
        db.Asns.Add(existingAsn);
        await db.SaveChangesAsync();
        var service = new SupplierPortalAsnService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsnAsync(
            supplier.Id,
            Guid.NewGuid(),
            "sales",
            CreateRequest(order.Id, sku.Id, 4),
            CancellationToken.None));

        Assert.Single(await db.Asns.ToListAsync());
    }

    [Fact]
    public async Task GetPurchaseOrderOptionsAsync_DeductsReceivedAndOpenAsnQuantities()
    {
        await using var db = CreateDbContext();
        var (supplier, sku, order) = SeedPurchaseOrder(db, quantity: 10, receivedQuantity: 2);
        order.Status = "partial_received";
        var openAsn = new Asn
        {
            AsnNumber = "ASN-OPEN",
            SupplierId = supplier.Id,
            PurchaseOrderId = order.Id,
            ShipDate = DateTime.UtcNow.Date,
            ExpectedArrival = DateTime.UtcNow.Date.AddDays(1),
            Status = "submitted"
        };
        openAsn.Items.Add(new AsnItem
        {
            AsnId = openAsn.Id,
            SkuId = sku.Id,
            ShippedQty = 5,
            LotNumber = "LOT-OPEN"
        });
        db.Asns.Add(openAsn);
        await db.SaveChangesAsync();
        var service = new SupplierPortalAsnService(db);

        var options = await service.GetPurchaseOrderOptionsAsync(
            order.Id,
            supplier.Id,
            CancellationToken.None);

        var item = Assert.Single(Assert.IsType<SupplierPortalAsnOrderOptionsDto>(options).Items);
        Assert.Equal(10m, item.OrderedQuantity);
        Assert.Equal(2m, item.ReceivedQuantity);
        Assert.Equal(5m, item.ReservedForShipmentQuantity);
        Assert.Equal(3m, item.AvailableToShipQuantity);
        Assert.Null(await service.GetPurchaseOrderOptionsAsync(order.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsnAsync_RequiresLotAndDatesForLotControlledItems()
    {
        await using var db = CreateDbContext();
        var (supplier, sku, order) = SeedPurchaseOrder(db, lotControlled: true);
        await db.SaveChangesAsync();
        var service = new SupplierPortalAsnService(db);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsnAsync(
            supplier.Id,
            Guid.NewGuid(),
            "sales",
            CreateRequest(order.Id, sku.Id, 1) with
            {
                Items =
                [
                    new SupplierPortalAsnItemRequestDto
                    {
                        SkuId = sku.Id,
                        ShippedQuantity = 1
                    }
                ]
            },
            CancellationToken.None));

        Assert.Empty(await db.Asns.ToListAsync());
    }

    [Fact]
    public async Task CreateAsnAsync_PersistsLotTraceabilityForLotControlledItems()
    {
        await using var db = CreateDbContext();
        var (supplier, sku, order) = SeedPurchaseOrder(db, lotControlled: true);
        await db.SaveChangesAsync();
        var manufactureDate = DateTimeOffset.UtcNow.AddDays(-10);
        var expiryDate = DateTimeOffset.UtcNow.AddDays(90);
        var request = CreateRequest(order.Id, sku.Id, 1) with
        {
            Items =
            [
                new SupplierPortalAsnItemRequestDto
                {
                    SkuId = sku.Id,
                    ShippedQuantity = 1,
                    LotNumber = "LOT-VALID",
                    ManufactureDate = manufactureDate,
                    ExpiryDate = expiryDate
                }
            ]
        };
        var service = new SupplierPortalAsnService(db);

        await service.CreateAsnAsync(
            supplier.Id,
            Guid.NewGuid(),
            "sales",
            request,
            CancellationToken.None);

        var item = await db.AsnItems.SingleAsync();
        Assert.Equal("LOT-VALID", item.LotNumber);
        Assert.Equal(manufactureDate.UtcDateTime, item.ManufactureDate);
        Assert.Equal(expiryDate.UtcDateTime, item.ExpiryDate);
    }

    [Fact]
    public async Task GetAsnAsync_DoesNotExposeAnotherSuppliersShipment()
    {
        await using var db = CreateDbContext();
        var (supplier, sku, order) = SeedPurchaseOrder(db);
        var asn = new Asn
        {
            AsnNumber = "ASN-OTHER",
            SupplierId = supplier.Id,
            PurchaseOrderId = order.Id,
            ShipDate = DateTime.UtcNow.Date,
            ExpectedArrival = DateTime.UtcNow.Date.AddDays(1)
        };
        asn.Items.Add(new AsnItem { AsnId = asn.Id, SkuId = sku.Id, ShippedQty = 1 });
        db.Asns.Add(asn);
        await db.SaveChangesAsync();
        var service = new SupplierPortalAsnService(db);

        Assert.Null(await service.GetAsnAsync(asn.Id, Guid.NewGuid(), CancellationToken.None));
        Assert.NotNull(await service.GetAsnAsync(asn.Id, supplier.Id, CancellationToken.None));
    }

    private static ErpGeneralDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static (Supplier Supplier, Sku Sku, PurchaseOrder Order) SeedPurchaseOrder(
        ErpGeneralDbContext db,
        decimal quantity = 10,
        decimal receivedQuantity = 0,
        bool lotControlled = false)
    {
        var supplier = new Supplier
        {
            SupplierCode = "SUP-A",
            SupplierName = "Supplier A",
            ApprovalStatus = "APPROVED",
            Status = "ACTIVE"
        };
        var sku = new Sku
        {
            SkuNumber = "SKU-A",
            SkuName = "Part A",
            LotControlled = lotControlled
        };
        var order = new PurchaseOrder
        {
            PoNumber = "PO-A",
            SupplierId = supplier.Id,
            PoDate = DateTime.UtcNow.Date,
            Status = "confirmed",
            Items =
            [
                new PurchaseOrderItem
                {
                    SkuId = sku.Id,
                    Quantity = quantity,
                    ReceivedQty = receivedQuantity,
                    Unit = "EA"
                }
            ]
        };
        db.Suppliers.Add(supplier);
        db.Skus.Add(sku);
        db.PurchaseOrders.Add(order);
        return (supplier, sku, order);
    }

    private static CreateSupplierPortalAsnRequestDto CreateRequest(
        Guid purchaseOrderId,
        Guid skuId,
        decimal quantity) =>
        new()
        {
            PurchaseOrderId = purchaseOrderId,
            ShipDate = DateTimeOffset.UtcNow.Date,
            ExpectedArrival = DateTimeOffset.UtcNow.Date.AddDays(2),
            Carrier = "Carrier",
            TrackingNumber = "TRACK-1",
            Items =
            [
                new SupplierPortalAsnItemRequestDto
                {
                    SkuId = skuId,
                    ShippedQuantity = quantity
                }
            ]
        };
}
