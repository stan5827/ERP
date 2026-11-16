using ErpBackend.Data;
using ErpBackend.Models.MES;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface IRccpResourceService
{
    Task<List<RccpResource>> ListResourcesAsync();
    Task<(RccpResource? Resource, string Status)> CreateResourceAsync(RccpResource model);
    Task<(RccpResource? Resource, string Status)> UpdateResourceAsync(Guid id, RccpResource model);
    Task<List<RccpItemResource>> GetItemResourcesAsync(Guid skuId);
    Task<(bool Success, string Message)> UpdateItemResourcesAsync(List<RccpItemResource> items);
}

public class RccpResourceService : IRccpResourceService
{
    private readonly ErpGeneralDbContext _db;

    public RccpResourceService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<List<RccpResource>> ListResourcesAsync()
    {
        return await _db.RccpResources
            .Include(r => r.WorkCenter)
            .ToListAsync();
    }

    public async Task<(RccpResource? Resource, string Status)> CreateResourceAsync(RccpResource model)
    {
        if (model == null)
            return (null, "invalid");

        if (await _db.RccpResources.AnyAsync(r => r.ResourceCode == model.ResourceCode))
            return (null, "duplicate");

        _db.RccpResources.Add(model);
        await _db.SaveChangesAsync();
        return (model, "ok");
    }

    public async Task<(RccpResource? Resource, string Status)> UpdateResourceAsync(Guid id, RccpResource model)
    {
        if (model == null || model.Id != id)
            return (null, "invalid");

        var existing = await _db.RccpResources.FirstOrDefaultAsync(r => r.Id == id);
        if (existing == null)
            return (null, "not_found");

        existing.ResourceCode = model.ResourceCode;
        existing.ResourceName = model.ResourceName;
        existing.ResourceType = model.ResourceType;
        existing.WorkCenterId = model.WorkCenterId;
        existing.CapacityPerBucket = model.CapacityPerBucket;
        existing.Uom = model.Uom;
        existing.Efficiency = model.Efficiency;
        existing.Utilization = model.Utilization;
        existing.IsActive = model.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Version = model.Version;

        await _db.SaveChangesAsync();
        return (existing, "ok");
    }

    public async Task<List<RccpItemResource>> GetItemResourcesAsync(Guid skuId)
    {
        return await _db.RccpItemResources
            .Include(ir => ir.Resource)
            .Where(ir => ir.SkuId == skuId)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> UpdateItemResourcesAsync(List<RccpItemResource> items)
    {
        if (items == null || !items.Any())
            return (false, "No items provided");

        var skuId = items.First().SkuId;

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var existing = await _db.RccpItemResources.Where(ir => ir.SkuId == skuId).ToListAsync();
            _db.RccpItemResources.RemoveRange(existing);

            foreach (var item in items)
            {
                item.Id = Guid.NewGuid();
                _db.RccpItemResources.Add(item);
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Saved successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, ex.Message);
        }
    }
}
