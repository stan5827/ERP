using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public class SalesOrderServiceTests
{
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
