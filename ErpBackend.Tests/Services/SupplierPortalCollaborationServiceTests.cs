using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class SupplierPortalCollaborationServiceTests
{
    [Fact]
    public async Task GetRfqsAsync_ListsOnlyRfqAssignedToSupplier()
    {
        await using var db = CreateDbContext();
        var supplier = ApprovedSupplier("SUP-A");
        var otherSupplier = ApprovedSupplier("SUP-B");
        var assignedRfq = OpenRfq("RFQ-A");
        var otherRfq = OpenRfq("RFQ-B");
        db.Suppliers.AddRange(supplier, otherSupplier);
        db.Rfqs.AddRange(assignedRfq, otherRfq);
        db.RfqSupplierInvitations.Add(new RfqSupplierInvitation
        {
            RfqId = assignedRfq.Id,
            SupplierId = supplier.Id
        });
        db.RfqSupplierInvitations.Add(new RfqSupplierInvitation
        {
            RfqId = otherRfq.Id,
            SupplierId = otherSupplier.Id
        });
        await db.SaveChangesAsync();

        var service = new SupplierPortalCollaborationService(db);

        var result = await service.GetRfqsAsync(supplier.Id, CancellationToken.None);

        var rfq = Assert.Single(result);
        Assert.Equal(assignedRfq.Id, rfq.RfqId);
    }

    [Fact]
    public async Task SubmitQuoteAsync_UsesServerSupplierAndRejectsUnassignedRfq()
    {
        await using var db = CreateDbContext();
        var supplier = ApprovedSupplier("SUP-A");
        var otherSupplier = ApprovedSupplier("SUP-B");
        var rfq = OpenRfq("RFQ-A");
        rfq.Items.Add(new RfqItem { RfqId = rfq.Id, SkuId = Guid.NewGuid(), Quantity = 10 });
        db.Suppliers.AddRange(supplier, otherSupplier);
        db.Rfqs.Add(rfq);
        db.RfqSupplierInvitations.Add(new RfqSupplierInvitation
        {
            RfqId = rfq.Id,
            SupplierId = otherSupplier.Id
        });
        await db.SaveChangesAsync();

        var service = new SupplierPortalCollaborationService(db);
        var request = new SubmitSupplierRfqQuoteRequestDto
        {
            Items =
            [
                new SupplierRfqQuoteLineRequestDto
                {
                    SkuId = rfq.Items.Single().SkuId,
                    UnitPrice = 12.5m,
                    PromisedDeliveryDate = DateTimeOffset.UtcNow.AddDays(20)
                }
            ]
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SubmitQuoteAsync(
            rfq.Id,
            supplier.Id,
            Guid.NewGuid(),
            "sales",
            request,
            CancellationToken.None));

        Assert.Empty(await db.RfqSupplierResponses.ToListAsync());
    }

    [Fact]
    public async Task SubmitQuoteAsync_UpdatesOnlyAssignedSuppliersOwnQuote()
    {
        await using var db = CreateDbContext();
        var supplier = ApprovedSupplier("SUP-A");
        var otherSupplier = ApprovedSupplier("SUP-B");
        var skuId = Guid.NewGuid();
        var rfq = OpenRfq("RFQ-A");
        rfq.Items.Add(new RfqItem { RfqId = rfq.Id, SkuId = skuId, Quantity = 10 });
        db.Suppliers.AddRange(supplier, otherSupplier);
        db.Rfqs.Add(rfq);
        db.RfqSupplierInvitations.Add(new RfqSupplierInvitation
        {
            RfqId = rfq.Id,
            SupplierId = supplier.Id
        });
        db.RfqSupplierResponses.Add(new RfqSupplierResponse
        {
            RfqId = rfq.Id,
            SupplierId = otherSupplier.Id,
            SkuId = skuId,
            UnitPrice = 9,
            SubmittedAt = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();
        var service = new SupplierPortalCollaborationService(db);

        var result = await service.SubmitQuoteAsync(
            rfq.Id,
            supplier.Id,
            Guid.NewGuid(),
            "sales",
            new SubmitSupplierRfqQuoteRequestDto
            {
                Items =
                [
                    new SupplierRfqQuoteLineRequestDto
                    {
                        SkuId = skuId,
                        UnitPrice = 12.5m,
                        LeadTimeDays = 14,
                        MinimumOrderQuantity = 5,
                        PromisedDeliveryDate = DateTimeOffset.UtcNow.AddDays(20)
                    }
                ]
            },
            CancellationToken.None);

        Assert.Equal(1, result.QuotedItemCount);
        var quotes = await db.RfqSupplierResponses.OrderBy(response => response.SupplierId).ToListAsync();
        Assert.Equal(2, quotes.Count);
        Assert.Equal(12.5m, quotes.Single(response => response.SupplierId == supplier.Id).UnitPrice);
        Assert.Equal(9m, quotes.Single(response => response.SupplierId == otherSupplier.Id).UnitPrice);
        Assert.Equal("Responded", (await db.RfqSupplierInvitations.SingleAsync()).Status);
    }

    [Fact]
    public async Task SubmitPurchaseOrderResponseAsync_RecordsPerLineChangeAndPreventsCrossSupplierAccess()
    {
        await using var db = CreateDbContext();
        var supplier = ApprovedSupplier("SUP-A");
        var otherSupplier = ApprovedSupplier("SUP-B");
        var order = ConfirmedPurchaseOrder(supplier);
        var otherOrder = ConfirmedPurchaseOrder(otherSupplier);
        db.Suppliers.AddRange(supplier, otherSupplier);
        db.PurchaseOrders.AddRange(order, otherOrder);
        await db.SaveChangesAsync();
        var service = new SupplierPortalCollaborationService(db);

        var result = await service.SubmitPurchaseOrderResponseAsync(
            order.Id,
            supplier.Id,
            Guid.NewGuid(),
            "sales",
            new SubmitSupplierPurchaseOrderResponseRequestDto
            {
                ResponseType = "change_requested",
                Reason = "產能安排需調整",
                Items =
                [
                    new SupplierPurchaseOrderProposalItemRequestDto
                    {
                        PurchaseOrderItemId = order.Items.Single().Id,
                        ProposedQuantity = 5,
                        ProposedDeliveryDate = DateTimeOffset.UtcNow.AddDays(25)
                    }
                ]
            },
            CancellationToken.None);

        var storedResponse = await db.PurchaseOrderSupplierResponses
            .Include(response => response.Items)
            .SingleAsync();
        Assert.Equal(result.ResponseId, storedResponse.Id);
        Assert.Equal(order.Version, storedResponse.PurchaseOrderVersion);
        Assert.Equal(5m, Assert.Single(storedResponse.Items).ProposedQuantity);
        Assert.Null(await service.GetPurchaseOrderAsync(otherOrder.Id, supplier.Id, CancellationToken.None));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SubmitPurchaseOrderResponseAsync(
            otherOrder.Id,
            supplier.Id,
            Guid.NewGuid(),
            "sales",
            new SubmitSupplierPurchaseOrderResponseRequestDto { ResponseType = "accepted" },
            CancellationToken.None));
    }

    [Fact]
    public async Task SubmitPurchaseOrderResponseAsync_RequiresReasonAndRestrictsResponseRole()
    {
        await using var db = CreateDbContext();
        var supplier = ApprovedSupplier("SUP-A");
        var order = ConfirmedPurchaseOrder(supplier);
        db.Suppliers.Add(supplier);
        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync();
        var service = new SupplierPortalCollaborationService(db);

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitPurchaseOrderResponseAsync(
            order.Id,
            supplier.Id,
            Guid.NewGuid(),
            "sales",
            new SubmitSupplierPurchaseOrderResponseRequestDto { ResponseType = "rejected" },
            CancellationToken.None));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SubmitPurchaseOrderResponseAsync(
            order.Id,
            supplier.Id,
            Guid.NewGuid(),
            "viewer",
            new SubmitSupplierPurchaseOrderResponseRequestDto { ResponseType = "accepted" },
            CancellationToken.None));
        Assert.Empty(await db.PurchaseOrderSupplierResponses.ToListAsync());
    }

    private static ErpGeneralDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Supplier ApprovedSupplier(string supplierCode) =>
        new()
        {
            SupplierCode = supplierCode,
            SupplierName = supplierCode,
            ApprovalStatus = "APPROVED",
            Status = "ACTIVE"
        };

    private static Rfq OpenRfq(string number) =>
        new()
        {
            RfqNumber = number,
            RfqDate = DateTime.UtcNow,
            Deadline = DateTime.UtcNow.AddDays(10),
            Status = "open"
        };

    private static PurchaseOrder ConfirmedPurchaseOrder(Supplier supplier)
    {
        var order = new PurchaseOrder
        {
            Supplier = supplier,
            SupplierId = supplier.Id,
            PoNumber = $"PO-{Guid.NewGuid():N}",
            PoDate = DateTime.UtcNow,
            Status = "confirmed",
            Version = 3,
            Currency = "TWD",
            TotalAmount = 100
        };
        order.Items.Add(new PurchaseOrderItem
        {
            PurchaseOrderId = order.Id,
            SkuId = Guid.NewGuid(),
            Quantity = 10,
            UnitPrice = 10,
            Subtotal = 100
        });
        return order;
    }
}
