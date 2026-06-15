using MassTransit;
using ErpBackend.Events;
using ErpBackend.Data;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Consumers;

public class InventoryReservationConsumer : IConsumer<ISalesOrderCreated>
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<InventoryReservationConsumer> _logger;

    public InventoryReservationConsumer(ErpGeneralDbContext db, ILogger<InventoryReservationConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ISalesOrderCreated> context)
    {
        var order = context.Message;
        _logger.LogInformation("Processing inventory reservation for Order: {OrderNumber}", order.OrderNumber);

        try
        {
            foreach (var item in order.Items)
            {
                var sku = await _db.Skus.FindAsync(item.SkuId);
                if (sku == null || sku.Qty < item.Quantity)
                {
                    throw new Exception($"庫存不足：{sku?.SkuName ?? "未知品項"} (所需: {item.Quantity}, 現有: {sku?.Qty ?? 0})");
                }

                // Create reservation record
                var reservation = new InventoryReservation
                {
                    SkuId = item.SkuId,
                    Qty = item.Quantity,
                    OrderType = "SalesOrder",
                    OrderId = order.OrderId,
                    Status = "reserved",
                    ReservedAt = DateTime.UtcNow
                };

                _db.InventoryReservations.Add(reservation);
                
                // Note: In a real ERP, you might also decrease the 'Available Qty' 
                // but keep 'Physical Qty' until actual shipment.
            }

            await _db.SaveChangesAsync();

            await context.Publish<IInventoryReserved>(new
            {
                OrderId = order.OrderId,
                ReservedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Inventory reserved successfully for Order: {OrderNumber}", order.OrderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory reservation failed for Order: {OrderNumber}", order.OrderNumber);

            await context.Publish<IInventoryReservationFailed>(new
            {
                OrderId = order.OrderId,
                Reason = ex.Message,
                FailedAt = DateTime.UtcNow
            });
        }
    }
}
