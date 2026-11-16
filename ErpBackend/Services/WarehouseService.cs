using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Services;

/// <summary>
/// 倉庫 / 儲位服務介面
/// 提供倉庫與儲位查詢，隔離控制器對 DbContext 的直接依賴。
/// </summary>
public interface IWarehouseService
{
    /// <summary>取得倉庫清單</summary>
    Task<List<WarehouseDto>> GetWarehousesAsync(bool includeInactive = false);

    Task<(WarehouseDto? dto, string status)> CreateWarehouseAsync(CreateWarehouseRequestDto request);
    Task<(WarehouseDto? dto, string status)> UpdateWarehouseAsync(Guid id, UpdateWarehouseRequestDto request);

    Task<(BinLocation? binLocation, string status)> CreateBinLocationAsync(CreateBinLocationRequestDto request);
    Task<(BinLocation? binLocation, string status)> UpdateBinLocationAsync(Guid id, UpdateBinLocationRequestDto request);

    /// <summary>取得指定倉庫的所有儲位</summary>
    Task<List<BinLocation>> GetBinLocationsAsync(Guid warehouseId);

    /// <summary>取得全部儲位</summary>
    Task<List<BinLocation>> GetAllBinLocationsAsync();
}

/// <summary>
/// 倉庫 / 儲位服務實作
/// </summary>
public class WarehouseService : IWarehouseService
{
    private readonly ErpGeneralDbContext _db;

    public WarehouseService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<List<WarehouseDto>> GetWarehousesAsync(bool includeInactive = false)
    {
        var items = await _db.Warehouses
            .AsNoTracking()
            .Where(w => includeInactive || w.IsActive)
            .OrderBy(w => w.WarehouseCode)
            .Select(w => new WarehouseDto
            {
                Id = w.Id,
                WarehouseCode = w.WarehouseCode,
                WarehouseName = w.WarehouseName,
                Address = w.Address,
                Type = w.Type,
                IsActive = w.IsActive,
                BinLocationCount = w.BinLocations.Count
            })
            .ToListAsync();

        return items;
    }

    public async Task<(WarehouseDto? dto, string status)> CreateWarehouseAsync(CreateWarehouseRequestDto request)
    {
        var code = request.WarehouseCode?.Trim();
        var name = request.WarehouseName?.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return (null, "invalid");
        if (await _db.Warehouses.AnyAsync(w => w.WarehouseCode == code))
            return (null, "duplicate");

        var warehouse = new Warehouse
        {
            WarehouseCode = code,
            WarehouseName = name,
            Address = request.Address?.Trim(),
            Type = string.IsNullOrWhiteSpace(request.Type) ? "normal" : request.Type.Trim(),
            IsActive = request.IsActive
        };

        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync();
        return (ToDto(warehouse), "ok");
    }

    public async Task<(WarehouseDto? dto, string status)> UpdateWarehouseAsync(Guid id, UpdateWarehouseRequestDto request)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == id);
        if (warehouse == null) return (null, "not_found");

        var code = request.WarehouseCode?.Trim();
        var name = request.WarehouseName?.Trim();
        if ((request.WarehouseCode != null && string.IsNullOrWhiteSpace(code))
            || (request.WarehouseName != null && string.IsNullOrWhiteSpace(name)))
            return (null, "invalid");
        if (!string.IsNullOrWhiteSpace(code) && code != warehouse.WarehouseCode
            && await _db.Warehouses.AnyAsync(w => w.WarehouseCode == code && w.Id != id))
            return (null, "duplicate");

        if (code != null) warehouse.WarehouseCode = code;
        if (name != null) warehouse.WarehouseName = name;
        if (request.Address != null) warehouse.Address = request.Address.Trim();
        if (!string.IsNullOrWhiteSpace(request.Type)) warehouse.Type = request.Type.Trim();
        if (request.IsActive.HasValue) warehouse.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();
        return (ToDto(warehouse), "ok");
    }

    public async Task<List<BinLocation>> GetBinLocationsAsync(Guid warehouseId)
    {
        return await _db.BinLocations
            .Where(b => b.WarehouseId == warehouseId)
            .OrderBy(b => b.LocationCode)
            .ToListAsync();
    }

    public async Task<List<BinLocation>> GetAllBinLocationsAsync()
    {
        return await _db.BinLocations
            .OrderBy(b => b.WarehouseId)
            .ThenBy(b => b.LocationCode)
            .ToListAsync();
    }

    public async Task<(BinLocation? binLocation, string status)> CreateBinLocationAsync(CreateBinLocationRequestDto request)
    {
        var locationCode = request.LocationCode?.Trim();
        if (request.WarehouseId == Guid.Empty || string.IsNullOrWhiteSpace(locationCode))
            return (null, "invalid");

        var warehouse = await _db.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.IsActive);
        if (warehouse == null)
            return (null, "warehouse_not_found");

        if (await _db.BinLocations.AnyAsync(b =>
                b.WarehouseId == request.WarehouseId && b.LocationCode == locationCode))
            return (null, "duplicate");

        var binLocation = new BinLocation
        {
            WarehouseId = request.WarehouseId,
            LocationCode = locationCode,
            Zone = request.Zone?.Trim(),
            Aisle = request.Aisle?.Trim(),
            Rack = request.Rack?.Trim(),
            Level = request.Level?.Trim(),
            Position = request.Position?.Trim()
        };
        _db.BinLocations.Add(binLocation);
        await _db.SaveChangesAsync();

        binLocation.Warehouse = warehouse;
        return (binLocation, "ok");
    }

    public async Task<(BinLocation? binLocation, string status)> UpdateBinLocationAsync(Guid id, UpdateBinLocationRequestDto request)
    {
        var binLocation = await _db.BinLocations.FirstOrDefaultAsync(b => b.Id == id);
        if (binLocation == null)
            return (null, "not_found");

        var locationCode = request.LocationCode?.Trim();
        if (request.WarehouseId == Guid.Empty || string.IsNullOrWhiteSpace(locationCode))
            return (null, "invalid");

        var warehouse = await _db.Warehouses
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.IsActive);
        if (warehouse == null)
            return (null, "warehouse_not_found");

        if (await _db.BinLocations.AnyAsync(b =>
                b.Id != id && b.WarehouseId == request.WarehouseId && b.LocationCode == locationCode))
            return (null, "duplicate");

        binLocation.WarehouseId = request.WarehouseId;
        binLocation.LocationCode = locationCode;
        binLocation.Zone = request.Zone?.Trim();
        binLocation.Aisle = request.Aisle?.Trim();
        binLocation.Rack = request.Rack?.Trim();
        binLocation.Level = request.Level?.Trim();
        binLocation.Position = request.Position?.Trim();

        await _db.SaveChangesAsync();
        binLocation.Warehouse = warehouse;
        return (binLocation, "ok");
    }

    private static WarehouseDto ToDto(Warehouse warehouse) => new()
    {
        Id = warehouse.Id,
        WarehouseCode = warehouse.WarehouseCode,
        WarehouseName = warehouse.WarehouseName,
        Address = warehouse.Address,
        Type = warehouse.Type,
        IsActive = warehouse.IsActive,
        BinLocationCount = warehouse.BinLocations.Count
    };
}
