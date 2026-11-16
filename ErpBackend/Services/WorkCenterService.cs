using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface IWorkCenterService
{
    Task<List<WorkCenterDto>> GetWorkCentersAsync();
    Task<WorkCenterDto?> GetWorkCenterAsync(Guid id);
    Task<(WorkCenterDto? dto, string status)> CreateWorkCenterAsync(CreateWorkCenterRequest request);
    Task<(WorkCenterDto? dto, string status)> UpdateWorkCenterAsync(Guid id, UpdateWorkCenterRequest request);
}

public class WorkCenterService : IWorkCenterService
{
    private readonly ErpGeneralDbContext _db;

    public WorkCenterService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<WorkCenterDto>> GetWorkCentersAsync() =>
        await _db.WorkCenters.AsNoTracking()
            .Where(w => w.IsActive)
            .OrderBy(w => w.WorkCenterCode)
            .Select(w => ToDto(w))
            .ToListAsync();

    public async Task<WorkCenterDto?> GetWorkCenterAsync(Guid id)
    {
        var w = await _db.WorkCenters.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id);
        return w == null ? null : ToDto(w);
    }

    public async Task<(WorkCenterDto? dto, string status)> CreateWorkCenterAsync(CreateWorkCenterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WorkCenterCode)) return (null, "invalid");
        if (await _db.WorkCenters.AnyAsync(w => w.WorkCenterCode == request.WorkCenterCode)) return (null, "duplicate");

        var w = new WorkCenter
        {
            WorkCenterCode = request.WorkCenterCode,
            WorkCenterName = request.WorkCenterName,
            Capacity = request.Capacity,
            Efficiency = request.Efficiency,
            IsActive = request.IsActive
        };

        _db.WorkCenters.Add(w);
        await _db.SaveChangesAsync();

        return (ToDto(w), "ok");
    }

    public async Task<(WorkCenterDto? dto, string status)> UpdateWorkCenterAsync(Guid id, UpdateWorkCenterRequest request)
    {
        var w = await _db.WorkCenters.FirstOrDefaultAsync(w => w.Id == id);
        if (w == null) return (null, "not_found");

        if (!string.IsNullOrEmpty(request.WorkCenterCode) && request.WorkCenterCode != w.WorkCenterCode
            && await _db.WorkCenters.AnyAsync(x => x.WorkCenterCode == request.WorkCenterCode && x.Id != id))
            return (null, "duplicate");

        if (request.WorkCenterCode != null) w.WorkCenterCode = request.WorkCenterCode;
        if (request.WorkCenterName != null) w.WorkCenterName = request.WorkCenterName;
        if (request.Capacity.HasValue) w.Capacity = request.Capacity.Value;
        if (request.Efficiency.HasValue) w.Efficiency = request.Efficiency.Value;
        if (request.IsActive.HasValue) w.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();
        return (ToDto(w), "ok");
    }

    private static WorkCenterDto ToDto(WorkCenter w) => new()
    {
        Id = w.Id,
        WorkCenterCode = w.WorkCenterCode,
        WorkCenterName = w.WorkCenterName,
        Capacity = w.Capacity,
        Efficiency = w.Efficiency,
        IsActive = w.IsActive
    };
}
