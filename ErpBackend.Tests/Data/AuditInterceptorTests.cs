using System.Security.Claims;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.System;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Data;

public class AuditInterceptorTests
{
    private sealed class CapturingAuditInterceptor : AuditInterceptor
    {
        public CapturingAuditInterceptor() : base(new HttpContextAccessor()) { }

        public DateTime? ReceiptModifiedAt { get; private set; }

        public override async ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
            Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var interceptionResult = await base.SavingChangesAsync(eventData, result, cancellationToken);
            var receiptEntry = eventData.Context?.ChangeTracker
                .Entries<ProductionReceipt>()
                .SingleOrDefault();
            if (receiptEntry is not null)
                ReceiptModifiedAt = receiptEntry.Property<DateTime?>("ModifiedAt").CurrentValue;
            return interceptionResult;
        }
    }

    [Fact]
    public async Task SavingChangesAsync_UsesAuthenticatedUserForCreatedByAndModifiedBy()
    {
        var actorId = Guid.NewGuid();
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, actorId.ToString())],
                    "test"))
            }
        };
        var interceptor = new AuditInterceptor(accessor);
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var db = new ErpGeneralDbContext(options);
        var user = new User
        {
            Username = "audit-interceptor-test",
            PasswordHash = "not-used"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        Assert.Equal(actorId, db.Entry(user).Property<Guid?>("CreatedBy").CurrentValue);

        user.Username = "audit-interceptor-updated";
        await db.SaveChangesAsync();

        Assert.Equal(actorId, db.Entry(user).Property<Guid?>("ModifiedBy").CurrentValue);
    }

    [Fact]
    public async Task SavingChangesAsync_DoesNotWriteAnEmptyUserIdWithoutAuthenticatedClaim()
    {
        var interceptor = new AuditInterceptor(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var db = new ErpGeneralDbContext(options);
        var user = new User
        {
            Username = "audit-anonymous-test",
            PasswordHash = "not-used"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        Assert.Null(db.Entry(user).Property<Guid?>("CreatedBy").CurrentValue);
    }

    [Fact]
    public async Task SavingChangesAsync_ClearsModifiedAtForNewEntities()
    {
        var interceptor = new CapturingAuditInterceptor();
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var db = new ErpGeneralDbContext(options);
        var receipt = new ProductionReceipt
        {
            ReceiptNumber = "MGR-AUDIT-TEST",
            WorkOrderId = Guid.NewGuid()
        };
        db.ProductionReceipts.Add(receipt);
        db.Entry(receipt).Property<DateTime?>("ModifiedAt").CurrentValue =
            new DateTime(2026, 11, 10, 7, 21, 0, DateTimeKind.Unspecified);

        await db.SaveChangesAsync();

        Assert.Null(interceptor.ReceiptModifiedAt);
    }

    [Fact]
    public async Task SavingChangesAsync_AttributesInventoryTransactionAndWritesLinkedAuditLog()
    {
        var actorId = Guid.NewGuid();
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("sub", actorId.ToString())],
                    "test"))
            }
        };
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditInterceptor(accessor))
            .Options;

        await using var db = new ErpGeneralDbContext(options);
        var sku = new Sku { SkuNumber = "AUDIT-SKU", SkuName = "Audit test SKU" };
        var warehouse = new Warehouse
        {
            WarehouseCode = "AUDIT",
            WarehouseName = "Audit test warehouse"
        };
        var transaction = new InventoryTransaction
        {
            TrxId = "AUDIT-TRX",
            SkuId = sku.Id,
            Type = "ADJUST_OUT",
            QtyChange = -2m,
            QtyBefore = 5m,
            QtyAfter = 3m,
            WarehouseId = warehouse.Id,
            CreatedAt = DateTime.UtcNow
        };
        db.AddRange(
            new User { Id = actorId, Username = "inventory-audit-test", PasswordHash = "not-used" },
            sku,
            warehouse,
            transaction);

        await db.SaveChangesAsync();

        var auditLog = await db.AuditLogs.SingleAsync();
        Assert.Equal(actorId, transaction.HandlerId);
        Assert.Equal(actorId, db.Entry(transaction).Property<Guid?>("CreatedBy").CurrentValue);
        Assert.Equal(actorId, auditLog.UserId);
        Assert.Equal(nameof(InventoryTransaction), auditLog.EntityType);
        Assert.Equal(transaction.Id, auditLog.EntityId);
        Assert.Equal("inventory.transaction.created", auditLog.Action);
    }
}
