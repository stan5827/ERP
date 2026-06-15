using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Services;

public interface ILotManagementService
{
    Task<InventoryLot> GenerateLotAsync(string skuCode, DateTime? expiryDate = null);
    Task<InventoryLot?> GetLotAsync(string lotNumber);
    Task<List<InventoryLot>> GetLotsBySkuAsync(string skuCode);
    Task<decimal> GetLotQtyAsync(string lotNumber);
    Task UpdateLotQtyAsync(string lotNumber, decimal newQty, string reason);
}

public class LotManagementService : ILotManagementService
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<LotManagementService> _logger;

    public LotManagementService(ErpGeneralDbContext db, ILogger<LotManagementService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<InventoryLot> GenerateLotAsync(string skuCode, DateTime? expiryDate = null)
    {
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == skuCode);
        if (sku == null)
            throw new ArgumentException($"料號 {skuCode} 不存在");

        var lot = new InventoryLot
        {
            LotNumber = GenerateLotCode(),
            SkuId = sku.Id,
            Qty = 0,
            Status = "active",
            ExpiryDate = expiryDate
        };

        _db.InventoryLots.Add(lot);
        await _db.SaveChangesAsync();

        _logger.LogInformation("建立新批次：{LotNumber} for SKU: {SkuCode}", lot.LotNumber, skuCode);
        return lot;
    }

    public async Task<InventoryLot?> GetLotAsync(string lotNumber)
    {
        return await _db.InventoryLots
            .Include(l => l.Sku)
            .FirstOrDefaultAsync(l => l.LotNumber == lotNumber);
    }

    public async Task<List<InventoryLot>> GetLotsBySkuAsync(string skuCode)
    {
        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == skuCode);
        if (sku == null)
            return new List<InventoryLot>();

        return await _db.InventoryLots
            .Where(l => l.SkuId == sku.Id && l.Status == "active")
            .OrderByDescending(l => l.Id)
            .ToListAsync();
    }

    public async Task<decimal> GetLotQtyAsync(string lotNumber)
    {
        var lot = await _db.InventoryLots.FirstOrDefaultAsync(l => l.LotNumber == lotNumber);
        return lot?.Qty ?? 0;
    }

    public async Task UpdateLotQtyAsync(string lotNumber, decimal newQty, string reason)
    {
        var lot = await _db.InventoryLots.FirstOrDefaultAsync(l => l.LotNumber == lotNumber);
        if (lot == null)
            throw new ArgumentException($"批次 {lotNumber} 不存在");

        var oldQty = lot.Qty;
        lot.Qty = newQty;
        await _db.SaveChangesAsync();

        _logger.LogInformation("批次 {LotNumber} 數量更新：{OldQty} -> {NewQty}，原因：{Reason}",
            lotNumber, oldQty, newQty, reason);
    }

    private string GenerateLotCode()
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var seq = Interlocked.Increment(ref _lotSequence).ToString("D4");
        return $"LOT-{date}-{seq}";
    }

    private static long _lotSequence;
}