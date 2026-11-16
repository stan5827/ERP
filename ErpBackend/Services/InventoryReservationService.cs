using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Services;

public interface IInventoryReservationService
{
    Task<ReservationResult> ReserveAsync(string skuCode, decimal qty, string orderType, Guid orderId);
    Task<ReservationResult> ReleaseAsync(string skuCode, decimal qty, string orderType, Guid orderId);
    Task<decimal> GetAvailableAsync(string skuCode);
}

public class InventoryReservationService : IInventoryReservationService
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<InventoryReservationService> _logger;

    public InventoryReservationService(ErpGeneralDbContext db, ILogger<InventoryReservationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReservationResult> ReserveAsync(string skuCode, decimal qty, string orderType, Guid orderId)
    {
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == skuCode);
        if (sku == null)
            return new ReservationResult { Success = false, Message = "料號不存在" };

        var available = await GetAvailableAsync(skuCode);
        if (available < qty)
            return new ReservationResult 
            { 
                Success = false, 
                Message = $"庫存不足，可用量: {available}, 需求: {qty}" 
            };

        var reservation = new InventoryReservation
        {
            SkuId = sku.Id,
            Qty = qty,
            OrderType = orderType,
            OrderId = orderId,
            Status = "reserved",
            ReservedAt = DateTime.UtcNow
        };

        _db.InventoryReservations.Add(reservation);
        await _db.SaveChangesAsync();

        return new ReservationResult
        {
            Success = true,
            Message = "預留成功",
            ReservedQty = qty,
            AvailableQty = available - qty
        };
    }

    public async Task<ReservationResult> ReleaseAsync(string skuCode, decimal qty, string orderType, Guid orderId)
    {
        var reservations = await _db.InventoryReservations
            .Include(r => r.Sku)
            .Where(r => r.Sku != null && r.Sku.SkuNumber == skuCode 
                       && r.OrderType == orderType 
                       && r.OrderId == orderId
                       && r.Status == "reserved")
            .ToListAsync();

        var totalReserved = reservations.Sum(r => r.Qty);
        if (totalReserved < qty)
            return new ReservationResult { Success = false, Message = "無可用預留" };

        var toRelease = Math.Min(qty, totalReserved);
        foreach (var r in reservations)
        {
            var releaseQty = Math.Min(r.Qty, toRelease);
            r.Qty -= releaseQty;
            r.Status = "released";
            r.ReleasedAt = DateTime.UtcNow;
            toRelease -= releaseQty;
            if (toRelease <= 0) break;
        }

        await _db.SaveChangesAsync();
        return new ReservationResult { Success = true, Message = "預留已釋放" };
    }

    public async Task<decimal> GetAvailableAsync(string skuCode)
    {
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == skuCode);
        if (sku == null) return 0;

        var onHand = sku.Qty;
        
        var reserved = await _db.InventoryReservations
            .Where(r => r.SkuId == sku.Id && r.Status == "reserved")
            .SumAsync(r => r.Qty);

        var quarantined = await _db.IqcReports
            .Where(report => report.SkuId == sku.Id &&
                             report.GoodsReceiptItemId != null &&
                             report.Result != "pass" &&
                             report.Result != "not_required")
            .SumAsync(report => (decimal?)report.ReceivedQty) ?? 0m;

        var fqcQuarantined = await _db.FqcInspectionLots
            .Where(lot => lot.ProductSkuId == sku.Id &&
                (lot.DispositionStatus == "Pending" || lot.DispositionStatus == "Concession"))
            .SumAsync(lot => (decimal?)lot.QuarantinedQty) ?? 0m;

        return onHand - reserved - quarantined - fqcQuarantined;
    }
}

public class InventoryReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SkuId { get; set; }
    public decimal Qty { get; set; }
    public string OrderType { get; set; } = null!;
    public Guid OrderId { get; set; }
    public string Status { get; set; } = "reserved";
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAt { get; set; }

    public Sku? Sku { get; set; }
}

public class ReservationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public decimal ReservedQty { get; set; }
    public decimal AvailableQty { get; set; }
}