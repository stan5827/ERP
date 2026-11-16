using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface IEquipmentService
{
    Task<List<EquipmentDto>> GetEquipmentAsync(string? status, string? type, CancellationToken cancellationToken);
    Task<EquipmentDto?> GetEquipmentAsync(Guid id, CancellationToken cancellationToken);
    Task<(EquipmentDto? dto, string status)> CreateEquipmentAsync(CreateEquipmentRequestDto request, CancellationToken cancellationToken);
    Task<(EquipmentDto? dto, string status)> UpdateEquipmentAsync(Guid id, UpdateEquipmentRequestDto request, CancellationToken cancellationToken);
    Task<string> DeleteEquipmentAsync(Guid id, CancellationToken cancellationToken);
    Task<EquipmentStatsDto> GetStatsAsync(CancellationToken cancellationToken);
}

public sealed class EquipmentService : IEquipmentService
{
    private readonly ErpGeneralDbContext _db;

    public EquipmentService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<EquipmentDto>> GetEquipmentAsync(
        string? status,
        string? type,
        CancellationToken cancellationToken)
    {
        var query = _db.Equipments.AsNoTracking()
            .Where(equipment => equipment.IsActive);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(equipment => equipment.Status == status);
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(equipment => equipment.EquipmentType == type);

        return await query
            .OrderBy(equipment => equipment.EquipmentCode)
            .Select(equipment => new EquipmentDto
            {
                Id = equipment.Id,
                EquipmentCode = equipment.EquipmentCode,
                EquipmentName = equipment.EquipmentName,
                EquipmentType = equipment.EquipmentType,
                WorkCenterId = equipment.WorkCenterId,
                WorkCenterName = equipment.WorkCenter == null ? null : equipment.WorkCenter.WorkCenterName,
                SupplierId = equipment.SupplierId,
                SupplierName = equipment.Supplier == null ? null : equipment.Supplier.SupplierName,
                SerialNumber = equipment.SerialNumber,
                Model = equipment.Model,
                Manufacturer = equipment.Manufacturer,
                PurchaseDate = equipment.PurchaseDate,
                PurchasePrice = equipment.PurchasePrice,
                Status = equipment.Status,
                MaintenanceStatus = equipment.MaintenanceStatus,
                LastMaintenanceDate = equipment.LastMaintenanceDate,
                NextMaintenanceDate = equipment.NextMaintenanceDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<EquipmentDto?> GetEquipmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var equipment = await _db.Equipments.AsNoTracking()
            .Where(item => item.Id == id && item.IsActive)
            .Select(item => new EquipmentDto
            {
                Id = item.Id,
                EquipmentCode = item.EquipmentCode,
                EquipmentName = item.EquipmentName,
                EquipmentType = item.EquipmentType,
                WorkCenterId = item.WorkCenterId,
                WorkCenterName = item.WorkCenter == null ? null : item.WorkCenter.WorkCenterName,
                SupplierId = item.SupplierId,
                SupplierName = item.Supplier == null ? null : item.Supplier.SupplierName,
                SerialNumber = item.SerialNumber,
                Model = item.Model,
                Manufacturer = item.Manufacturer,
                PurchaseDate = item.PurchaseDate,
                PurchasePrice = item.PurchasePrice,
                Status = item.Status,
                MaintenanceStatus = item.MaintenanceStatus,
                LastMaintenanceDate = item.LastMaintenanceDate,
                NextMaintenanceDate = item.NextMaintenanceDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        return equipment;
    }

    public async Task<(EquipmentDto? dto, string status)> CreateEquipmentAsync(
        CreateEquipmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var equipmentCode = request.EquipmentCode.Trim();
        if (string.IsNullOrWhiteSpace(equipmentCode) || string.IsNullOrWhiteSpace(request.EquipmentName))
            return (null, "invalid");

        if (await _db.Equipments.AnyAsync(
                equipment => equipment.EquipmentCode == equipmentCode,
                cancellationToken))
            return (null, "duplicate");

        var relatedStatus = await ValidateReferencesAsync(request.WorkCenterId, request.SupplierId, cancellationToken);
        if (relatedStatus != "ok")
            return (null, relatedStatus);

        var equipment = new Equipment
        {
            EquipmentCode = equipmentCode,
            EquipmentName = request.EquipmentName.Trim(),
            EquipmentType = request.EquipmentType,
            WorkCenterId = request.WorkCenterId,
            SupplierId = request.SupplierId,
            SerialNumber = request.SerialNumber,
            Model = request.Model,
            Manufacturer = request.Manufacturer,
            PurchaseDate = NormalizeUtc(request.PurchaseDate),
            PurchasePrice = request.PurchasePrice,
            Status = "active",
            MaintenanceStatus = "normal",
            IsActive = true
        };

        _db.Equipments.Add(equipment);
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetEquipmentAsync(equipment.Id, cancellationToken), "ok");
    }

    public async Task<(EquipmentDto? dto, string status)> UpdateEquipmentAsync(
        Guid id,
        UpdateEquipmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var equipment = await _db.Equipments
            .FirstOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);
        if (equipment == null)
            return (null, "not_found");

        var relatedStatus = await ValidateReferencesAsync(request.WorkCenterId, request.SupplierId, cancellationToken);
        if (relatedStatus != "ok")
            return (null, relatedStatus);

        if (request.EquipmentName != null) equipment.EquipmentName = request.EquipmentName.Trim();
        if (request.EquipmentType != null) equipment.EquipmentType = request.EquipmentType;
        if (request.WorkCenterId.HasValue) equipment.WorkCenterId = request.WorkCenterId;
        if (request.SupplierId.HasValue) equipment.SupplierId = request.SupplierId;
        if (request.SerialNumber != null) equipment.SerialNumber = request.SerialNumber;
        if (request.Model != null) equipment.Model = request.Model;
        if (request.Manufacturer != null) equipment.Manufacturer = request.Manufacturer;
        if (request.PurchaseDate.HasValue) equipment.PurchaseDate = NormalizeUtc(request.PurchaseDate);
        if (request.PurchasePrice.HasValue) equipment.PurchasePrice = request.PurchasePrice.Value;
        if (request.Status != null)
        {
            equipment.Status = request.Status;
            if (request.Status == "maintenance") equipment.MaintenanceStatus = "warning";
            if (request.Status == "active") equipment.MaintenanceStatus = "normal";
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await GetEquipmentAsync(id, cancellationToken), "ok");
    }

    public async Task<string> DeleteEquipmentAsync(Guid id, CancellationToken cancellationToken)
    {
        var equipment = await _db.Equipments
            .FirstOrDefaultAsync(item => item.Id == id && item.IsActive, cancellationToken);
        if (equipment == null)
            return "not_found";

        equipment.IsActive = false;
        equipment.Status = "inactive";
        await _db.SaveChangesAsync(cancellationToken);
        return "ok";
    }

    public async Task<EquipmentStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);

        var activeEquipment = _db.Equipments.AsNoTracking().Where(equipment => equipment.IsActive);
        return new EquipmentStatsDto
        {
            TotalCount = await activeEquipment.CountAsync(cancellationToken),
            RepairCount = await activeEquipment.CountAsync(equipment => equipment.Status == "repair", cancellationToken),
            WarningCount = await activeEquipment.CountAsync(equipment => equipment.MaintenanceStatus == "warning", cancellationToken),
            MonthlyMaintenanceCost = await _db.MaintenanceRecords.AsNoTracking()
                .Where(record => record.StartTime >= monthStart && record.StartTime < nextMonthStart)
                .SumAsync(record => (decimal?)record.TotalCost, cancellationToken) ?? 0
        };
    }

    private async Task<string> ValidateReferencesAsync(
        Guid? workCenterId,
        Guid? supplierId,
        CancellationToken cancellationToken)
    {
        if (workCenterId.HasValue && !await _db.WorkCenters.AnyAsync(
                workCenter => workCenter.Id == workCenterId.Value && workCenter.IsActive,
                cancellationToken))
            return "work_center_not_found";

        if (supplierId.HasValue && !await _db.Suppliers.AnyAsync(
                supplier => supplier.Id == supplierId.Value,
                cancellationToken))
            return "supplier_not_found";

        return "ok";
    }

    private static DateTime? NormalizeUtc(DateTime? value) =>
        value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : null;
}
