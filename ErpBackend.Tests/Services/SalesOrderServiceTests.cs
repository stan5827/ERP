using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Sales;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public class SalesOrderServiceTests
{
    [Fact]
    public async Task GetSalesOrdersAsync_IncludesChineseSalesUnitName()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var sku = new Sku
        {
            SkuNumber = "FG-001",
            SkuName = "測試商品",
            Unit = "BRANCH",
            SalesUom = new Uom { UomCode = "BRANCH", UomName = "支" }
        };
        var customer = new Customer
        {
            CustomerCode = "C-TEST",
            CustomerName = "Test customer"
        };
        db.SalesOrders.Add(new SalesOrder
        {
            OrderNumber = "SO-TEST-UNIT",
            CustomerId = customer.Id,
            Customer = customer,
            OrderDate = DateTime.UtcNow,
            Items =
            [
                new SalesOrderItem
                {
                    LineNumber = 1,
                    ProductId = sku.Id,
                    SkuId = sku.Id,
                    Sku = sku,
                    Quantity = 1,
                    UnitPrice = 100
                }
            ]
        });
        await db.SaveChangesAsync();

        var result = await new SalesOrderService(db, new StateMachineService()).GetSalesOrdersAsync();

        var item = Assert.Single(Assert.Single(result).Items);
        Assert.Equal("BRANCH", item.Unit);
        Assert.Equal("支", item.UnitName);
    }

    [Fact]
    public async Task UpdateStatusAsync_PersistsStatusChange()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var originalUpdatedAt = DateTime.UtcNow.AddDays(-1);

        await using (var seedContext = new ErpGeneralDbContext(options))
        {
            seedContext.SalesOrders.Add(new SalesOrder
            {
                Id = orderId,
                OrderNumber = "SO-TEST-001",
                CustomerId = customerId,
                Customer = new Customer
                {
                    Id = customerId,
                    CustomerCode = "C-TEST",
                    CustomerName = "Test customer"
                },
                OrderDate = DateTime.UtcNow.Date,
                Status = "confirmed",
                UpdatedAt = originalUpdatedAt
            });
            await seedContext.SaveChangesAsync();
        }

        await using (var updateContext = new ErpGeneralDbContext(options))
        {
            var service = new SalesOrderService(updateContext, new StateMachineService());
            var result = await service.UpdateStatusAsync(orderId, new UpdateStatusRequest
            {
                NewStatus = "completed"
            });

            Assert.True(result.found);
            Assert.Equal("", result.error);
            Assert.Equal("ok", result.status);
        }

        await using var verificationContext = new ErpGeneralDbContext(options);
        var updatedOrder = await verificationContext.SalesOrders.SingleAsync(order => order.Id == orderId);

        Assert.Equal("completed", updatedOrder.Status);
        Assert.True(updatedOrder.UpdatedAt > originalUpdatedAt);
    }
}
