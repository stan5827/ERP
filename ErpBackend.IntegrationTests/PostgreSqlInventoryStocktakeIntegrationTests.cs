using System.Security.Claims;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.System;
using ErpBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.IntegrationTests;

public class PostgreSqlInventoryStocktakeIntegrationTests
{
    [PostgreSqlFact]
    public async Task PostStocktake_NormalizesExpiryDateToUtcAndPersistsInventory()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ERP_TEST_POSTGRES_CONNECTION_STRING")!;
        await using var database = await PostgreSqlTestDatabase.CreateAsync(connectionString);
        const string lotNumber = "LOT-POSTGRES-STOCKTAKE";
        var warehouse = new Warehouse
        {
            WarehouseCode = "STK-PG",
            WarehouseName = "Stocktake PostgreSQL Test",
            Type = "normal",
            IsActive = true
        };
        var sku = new Sku
        {
            SkuNumber = "SKU-STK-PG",
            SkuName = "Stocktake PostgreSQL Test SKU",
            Unit = "PCS",
            Status = "active",
            Qty = 40m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var actor = new User
        {
            Username = $"stocktake-{Guid.NewGuid():N}",
            PasswordHash = "integration-test-only"
        };

        await using (var setupDb = new ErpGeneralDbContext(database.Options))
        {
            setupDb.AddRange(warehouse, sku, actor);
            setupDb.InventoryTransactions.Add(new InventoryTransaction
            {
                TrxId = $"STK-PG-{Guid.NewGuid():N}"[..30],
                SkuId = sku.Id,
                Type = "IN",
                QtyChange = 40m,
                QtyBefore = 0m,
                QtyAfter = 40m,
                WarehouseId = warehouse.Id,
                CreatedAt = DateTime.UtcNow
            });
            await setupDb.SaveChangesAsync();
        }

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, actor.Id.ToString())],
                    "integration-test"))
            }
        };
        var serviceOptions = new DbContextOptionsBuilder<ErpGeneralDbContext>(database.Options)
            .AddInterceptors(new AuditInterceptor(httpContextAccessor))
            .Options;

        await using (var db = new ErpGeneralDbContext(serviceOptions))
        {
            var service = new InventoryService(db, new AccountingService(db));
            var result = await service.PostStocktakeAsync(new InventoryStocktakeRequestDto
            {
                SkuCode = sku.SkuNumber,
                WarehouseCode = warehouse.WarehouseCode,
                LotNumber = lotNumber,
                ExpiryDate = new DateTime(2027, 12, 31, 0, 0, 0, DateTimeKind.Unspecified),
                CountedQty = 1m,
                ExpectedSystemQty = 0m,
                Notes = "PostgreSQL stocktake integration test"
            }, actor.Id);

            Assert.Equal(DateTimeKind.Utc, result.ExpiryDate!.Value.Kind);
            Assert.Equal(new DateTime(2027, 12, 31), result.ExpiryDate.Value.Date);
        }

        await using (var verificationDb = new ErpGeneralDbContext(database.Options))
        {
            var lot = await verificationDb.InventoryLots.SingleAsync(item => item.LotNumber == lotNumber);
            var transaction = await verificationDb.InventoryTransactions.SingleAsync(
                item => item.RelatedDocType == "Stocktake" && item.LotNumber == lotNumber);
            var auditLog = await verificationDb.AuditLogs.SingleAsync(
                item => item.EntityId == transaction.Id && item.Action == "stocktake.posted");
            var persistedSku = await verificationDb.Skus.SingleAsync(item => item.Id == sku.Id);
            var createdBy = verificationDb.Entry(transaction).Property<Guid?>("CreatedBy").CurrentValue;

            Assert.Equal(DateTimeKind.Utc, lot.ExpiryDate!.Value.Kind);
            Assert.Equal(new DateTime(2027, 12, 31), lot.ExpiryDate.Value.Date);
            Assert.Equal(1m, lot.Qty);
            Assert.Equal(1m, transaction.QtyChange);
            Assert.Equal(40m, transaction.QtyBefore);
            Assert.Equal(41m, transaction.QtyAfter);
            Assert.Equal(actor.Id, transaction.HandlerId);
            Assert.Equal(actor.Id, createdBy);
            Assert.Equal(41m, persistedSku.Qty);
            Assert.Equal(actor.Id, auditLog.UserId);
            Assert.Equal(nameof(InventoryTransaction), auditLog.EntityType);
            Assert.Equal(transaction.Id, auditLog.EntityId);
        }
    }
}
