using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Asset;

namespace ErpBackend.Services;

public class AssetService : IAssetService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IAssetDepreciationService _depService;

    public AssetService(ErpGeneralDbContext db, IAssetDepreciationService depService)
    {
        _db = db;
        _depService = depService;
    }

    public async Task<List<AssetDto>> GetAllAsync(string? search, string? status, string? categoryCode)
    {
        var q = _db.Assets
            .Include(a => a.Category)
            .Include(a => a.Depreciations)
            .Include(a => a.Maintenances)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
            q = q.Where(a => a.AssetNumber.Contains(search) || a.AssetName.Contains(search));
        if (!string.IsNullOrEmpty(status))
            q = q.Where(a => a.Status == status);
        if (!string.IsNullOrEmpty(categoryCode))
            q = q.Where(a => a.Category.CategoryCode == categoryCode);

        var list = await q.OrderBy(a => a.AssetNumber).ToListAsync();
        var locationIds = list.Where(a => a.LocationId.HasValue).Select(a => a.LocationId!.Value).Distinct().ToList();
        var locations = await _db.AssetLocations
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id);

        return list.Select(a => ToDto(a, locations)).ToList();
    }

    public async Task<AssetDto?> GetByIdAsync(Guid id)
    {
        var a = await _db.Assets
            .Include(x => x.Category)
            .Include(x => x.Depreciations)
            .Include(x => x.Maintenances)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return null;

        AssetLocation? location = null;
        if (a.LocationId.HasValue)
            location = await _db.AssetLocations.FirstOrDefaultAsync(l => l.Id == a.LocationId.Value);

        var locDict = new Dictionary<Guid, AssetLocation>();
        if (a.LocationId.HasValue)
            locDict[a.LocationId.Value] = location!;

        return ToDto(a, locDict);
    }

    public async Task<(AssetDto? Dto, string Status)> CreateAsync(AssetUpsertDto req)
    {
        if (await _db.Assets.AnyAsync(a => a.AssetNumber == req.AssetNumber))
            return (null, "duplicate");

        var categoryExists = await _db.AssetCategories.AnyAsync(c => c.Id == req.CategoryId);
        if (!categoryExists)
            return (null, "category_not_found");

        var asset = new Asset
        {
            AssetNumber = req.AssetNumber,
            AssetName = req.AssetName,
            Specification = req.Specification,
            Manufacturer = req.Manufacturer,
            SupplierId = req.SupplierId,
            CategoryId = req.CategoryId,
            LocationId = req.LocationId,
            CustodianDeptId = req.CustodianDeptId,
            CustodianEmployeeId = req.CustodianEmployeeId,
            AcquisitionCost = req.AcquisitionCost,
            AcquisitionDate = req.AcquisitionDate,
            CommissionDate = req.CommissionDate,
            UsefulLife = req.UsefulLife,
            ResidualValue = req.ResidualValue,
            DepreciationMethod = req.DepreciationMethod,
            PurchaseOrderId = req.PurchaseOrderId,
            InvoiceNumber = req.InvoiceNumber,
            Status = req.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();

        var dto = await GetByIdAsync(asset.Id);
        return (dto, "created");
    }

    public async Task<(AssetDto? Dto, string Status)> UpdateAsync(Guid id, AssetUpsertDto req)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id);
        if (asset == null)
            return (null, "not_found");

        var dup = await _db.Assets.AnyAsync(a => a.AssetNumber == req.AssetNumber && a.Id != id);
        if (dup)
            return (null, "duplicate");

        var categoryExists = await _db.AssetCategories.AnyAsync(c => c.Id == req.CategoryId);
        if (!categoryExists)
            return (null, "category_not_found");

        asset.AssetNumber = req.AssetNumber;
        asset.AssetName = req.AssetName;
        asset.Specification = req.Specification;
        asset.Manufacturer = req.Manufacturer;
        asset.SupplierId = req.SupplierId;
        asset.CategoryId = req.CategoryId;
        asset.LocationId = req.LocationId;
        asset.CustodianDeptId = req.CustodianDeptId;
        asset.CustodianEmployeeId = req.CustodianEmployeeId;
        asset.AcquisitionCost = req.AcquisitionCost;
        asset.AcquisitionDate = req.AcquisitionDate;
        asset.CommissionDate = req.CommissionDate;
        asset.UsefulLife = req.UsefulLife;
        asset.ResidualValue = req.ResidualValue;
        asset.DepreciationMethod = req.DepreciationMethod;
        asset.PurchaseOrderId = req.PurchaseOrderId;
        asset.InvoiceNumber = req.InvoiceNumber;
        asset.Status = req.Status;
        asset.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var dto = await GetByIdAsync(asset.Id);
        return (dto, "updated");
    }

    public async Task<(bool Ok, string Status)> DeleteAsync(Guid id)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id);
        if (asset == null)
            return (false, "not_found");

        _db.Assets.Remove(asset);
        await _db.SaveChangesAsync();
        return (true, "deleted");
    }

    public async Task<List<AssetMaintenanceDto>> GetMaintenanceAsync(Guid assetId)
    {
        var asset = await _db.Assets
            .Include(a => a.Maintenances)
            .FirstOrDefaultAsync(a => a.Id == assetId);
        if (asset == null)
            return new List<AssetMaintenanceDto>();

        return asset.Maintenances
            .OrderByDescending(m => m.Id)
            .Select(m => new AssetMaintenanceDto
            {
                Id = m.Id,
                AssetId = m.AssetId,
                MaintenanceType = m.MaintenanceType,
                Description = m.Description,
                PartsCost = m.PartsCost,
                LaborCost = m.LaborCost,
                ExternalCost = m.ExternalCost,
                IsCapitalized = m.IsCapitalized,
                NextMaintenanceDate = m.NextMaintenanceDate,
                CompletedDate = m.CompletedDate,
                SerialNumber = m.SerialNumber
            })
            .ToList();
    }

    public async Task<(AssetMaintenanceDto? Dto, string Status)> AddMaintenanceAsync(Guid assetId, AssetMaintenanceUpsertDto req)
    {
        var assetExists = await _db.Assets.AnyAsync(a => a.Id == assetId);
        if (!assetExists)
            return (null, "not_found");

        var m = new AssetMaintenance
        {
            AssetId = assetId,
            MaintenanceType = req.MaintenanceType,
            Description = req.Description,
            PartsCost = req.PartsCost,
            LaborCost = req.LaborCost,
            ExternalCost = req.ExternalCost,
            IsCapitalized = req.IsCapitalized,
            NextMaintenanceDate = req.NextMaintenanceDate,
            CompletedDate = req.CompletedDate,
            SerialNumber = req.SerialNumber
        };

        _db.AssetMaintenances.Add(m);
        await _db.SaveChangesAsync();

        return (new AssetMaintenanceDto
        {
            Id = m.Id,
            AssetId = m.AssetId,
            MaintenanceType = m.MaintenanceType,
            Description = m.Description,
            PartsCost = m.PartsCost,
            LaborCost = m.LaborCost,
            ExternalCost = m.ExternalCost,
            IsCapitalized = m.IsCapitalized,
            NextMaintenanceDate = m.NextMaintenanceDate,
            CompletedDate = m.CompletedDate,
            SerialNumber = m.SerialNumber
        }, "created");
    }

    public async Task<DepreciationResultDto> RunDepreciationAsync(int year, int month)
        => await _depService.CalculateMonthlyDepreciationAsync(year, month);

    public async Task<List<DepreciationRecordDto>> GetDepreciationHistoryAsync(Guid assetId)
        => await _depService.GetDepreciationScheduleAsync(assetId);

    private static AssetDto ToDto(Asset a, Dictionary<Guid, AssetLocation> locations)
    {
        AssetLocation? location = null;
        locations.TryGetValue(a.LocationId ?? Guid.Empty, out location);

        return new AssetDto
        {
            Id = a.Id,
            AssetNumber = a.AssetNumber,
            AssetName = a.AssetName,
            Specification = a.Specification,
            Manufacturer = a.Manufacturer,
            SupplierId = a.SupplierId,
            CategoryId = a.CategoryId,
            CategoryCode = a.Category?.CategoryCode,
            CategoryName = a.Category?.CategoryName,
            LocationId = a.LocationId,
            LocationCode = location?.LocationCode,
            LocationName = location?.LocationName,
            CustodianDeptId = a.CustodianDeptId,
            CustodianDeptName = null,
            CustodianEmployeeId = a.CustodianEmployeeId,
            AcquisitionCost = a.AcquisitionCost,
            AcquisitionDate = a.AcquisitionDate,
            CommissionDate = a.CommissionDate,
            UsefulLife = a.UsefulLife,
            ResidualValue = a.ResidualValue,
            DepreciationMethod = a.DepreciationMethod,
            PurchaseOrderId = a.PurchaseOrderId,
            InvoiceNumber = a.InvoiceNumber,
            Status = a.Status,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            Depreciations = (a.Depreciations ?? new List<AssetDepreciation>())
                .OrderBy(d => d.Period)
                .Select(d => new DepreciationRecordDto
                {
                    AssetId = a.Id,
                    AssetNumber = a.AssetNumber,
                    AssetName = a.AssetName,
                    Period = d.Period,
                    DepreciationAmount = d.DepreciationAmount,
                    AccumulatedDepreciation = d.AccumulatedDepreciation,
                    BookValue = d.BookValue
                })
                .ToList(),
            Maintenances = (a.Maintenances ?? new List<AssetMaintenance>())
                .Select(m => new AssetMaintenanceDto
                {
                    Id = m.Id,
                    AssetId = m.AssetId,
                    MaintenanceType = m.MaintenanceType,
                    Description = m.Description,
                    PartsCost = m.PartsCost,
                    LaborCost = m.LaborCost,
                    ExternalCost = m.ExternalCost,
                    IsCapitalized = m.IsCapitalized,
                    NextMaintenanceDate = m.NextMaintenanceDate,
                    CompletedDate = m.CompletedDate,
                    SerialNumber = m.SerialNumber
                })
                .ToList()
        };
    }
}
