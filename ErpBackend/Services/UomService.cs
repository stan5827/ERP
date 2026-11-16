using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface IUomService
{
    Task<List<UomDto>> GetUomsAsync(string? uomType, bool? isActive, string? searchTerm);
    Task<UomDto?> GetUomAsync(Guid id);
    Task<(UomDto? dto, string status)> CreateUomAsync(CreateUomDto model);
    Task<(UomDto? dto, string status)> UpdateUomAsync(Guid id, UpdateUomDto model);
    Task<(bool ok, string status)> DeleteUomAsync(Guid id);
}

public class UomService : IUomService
{
    private readonly ErpGeneralDbContext _db;

    public UomService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<UomDto>> GetUomsAsync(string? uomType, bool? isActive, string? searchTerm)
    {
        var query = _db.Uoms.AsQueryable();

        if (!string.IsNullOrEmpty(uomType))
            query = query.Where(u => u.UomType == uomType);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(u => u.UomCode.Contains(searchTerm) || u.UomName.Contains(searchTerm));

        var entities = await query
            .OrderBy(u => u.DisplayOrder)
            .ThenBy(u => u.UomCode)
            .ToListAsync();

        return entities.Select(ToDto).ToList();
    }

    public async Task<UomDto?> GetUomAsync(Guid id)
    {
        var uom = await _db.Uoms.FindAsync(id);
        if (uom == null) return null;
        return ToDto(uom);
    }

    public async Task<(UomDto? dto, string status)> CreateUomAsync(CreateUomDto model)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            if (string.IsNullOrEmpty(model.UomCode))
                return (null, "invalid");

            var exists = await _db.Uoms.AnyAsync(u => u.UomCode == model.UomCode);
            if (exists)
                return (null, "duplicate");

            var uom = new Uom
            {
                UomCode = model.UomCode!,
                UomName = model.UomName!,
                UomNameEn = model.UomNameEn,
                UomType = string.IsNullOrEmpty(model.UomType) ? "base" : model.UomType,
                DecimalPlaces = model.DecimalPlaces,
                IsActive = model.IsActive,
                AllowDecimal = model.AllowDecimal,
                Description = model.Description,
                DisplayOrder = model.DisplayOrder
            };

            _db.Uoms.Add(uom);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return (ToDto(uom), "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(UomDto? dto, string status)> UpdateUomAsync(Guid id, UpdateUomDto model)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var uom = await _db.Uoms.FindAsync(id);
            if (uom == null) return (null, "not_found");

            if (string.IsNullOrEmpty(model.UomCode))
                return (null, "invalid");

            var dup = await _db.Uoms.AnyAsync(u => u.UomCode == model.UomCode && u.Id != id);
            if (dup) return (null, "duplicate");

            uom.UomCode = model.UomCode!;
            uom.UomName = model.UomName!;
            uom.UomNameEn = model.UomNameEn;
            uom.UomType = string.IsNullOrEmpty(model.UomType) ? "base" : model.UomType;
            uom.DecimalPlaces = model.DecimalPlaces;
            uom.IsActive = model.IsActive;
            uom.AllowDecimal = model.AllowDecimal;
            uom.Description = model.Description;
            uom.DisplayOrder = model.DisplayOrder;
            uom.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return (ToDto(uom), "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool ok, string status)> DeleteUomAsync(Guid id)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var uom = await _db.Uoms.FindAsync(id);
            if (uom == null) return (false, "not_found");

            var hasConversions = await _db.UomConversions
                .AnyAsync(c => c.FromUomId == id || c.ToUomId == id);
            if (hasConversions) return (false, "has_conversions");

            _db.Uoms.Remove(uom);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static UomDto ToDto(Uom u) => new()
    {
        Id = u.Id,
        UomCode = u.UomCode,
        UomName = u.UomName,
        UomNameEn = u.UomNameEn,
        UomType = u.UomType,
        DecimalPlaces = u.DecimalPlaces,
        IsActive = u.IsActive,
        AllowDecimal = u.AllowDecimal,
        Description = u.Description,
        DisplayOrder = u.DisplayOrder,
        CreatedAt = u.CreatedAt
    };
}