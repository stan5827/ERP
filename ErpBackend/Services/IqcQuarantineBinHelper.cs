using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Services;

internal static class IqcQuarantineBinHelper
{
    internal const string LocationCode = "IQC-HOLD";

    internal static async Task<BinLocation> GetOrCreateAsync(
        ErpGeneralDbContext db,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var bin = await db.BinLocations.SingleOrDefaultAsync(
            item => item.WarehouseId == warehouseId && item.LocationCode == LocationCode,
            cancellationToken);
        if (bin is not null)
        {
            if (!bin.IsActive)
                throw new InvalidOperationException("待檢隔離儲位已停用，請先啟用儲位");
            return bin;
        }

        bin = new BinLocation
        {
            WarehouseId = warehouseId,
            LocationCode = LocationCode,
            Zone = "QA",
            Rack = "HOLD",
            IsActive = true
        };
        db.BinLocations.Add(bin);
        return bin;
    }
}
