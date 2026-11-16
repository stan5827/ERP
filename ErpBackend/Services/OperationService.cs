using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface IOperationService
{
    Task<List<OperationDto>> GetOperationsAsync();
    Task<OperationDto?> GetOperationAsync(Guid id);
    Task<(OperationDto? dto, string status)> CreateOperationAsync(CreateOperationRequest request);
    Task<(OperationDto? dto, string status)> UpdateOperationAsync(Guid id, UpdateOperationRequest request);
    Task<(bool ok, string status)> DeleteOperationAsync(Guid id);
}

public class OperationService : IOperationService
{
    private readonly ErpGeneralDbContext _db;

    public OperationService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<OperationDto>> GetOperationsAsync()
    {
        return await _db.Operations
            .Where(o => o.IsActive)
            .OrderBy(o => o.OperationCode)
            .Select(o => MapToDto(o))
            .ToListAsync();
    }

    public async Task<OperationDto?> GetOperationAsync(Guid id)
    {
        var o = await _db.Operations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        return o == null ? null : MapToDto(o);
    }

    public async Task<(OperationDto? dto, string status)> CreateOperationAsync(CreateOperationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OperationCode) || string.IsNullOrWhiteSpace(request.OperationName))
            return (null, "invalid");

        var duplicate = await _db.Operations.AnyAsync(o => o.OperationCode == request.OperationCode);
        if (duplicate) return (null, "duplicate");

        var op = new Operation
        {
            OperationCode = request.OperationCode,
            OperationName = request.OperationName,
            SetupTime = request.SetupTime,
            RunTime = request.RunTime,
            CostPerHour = request.CostPerHour,
            IsOutsourced = request.IsOutsourced,
            IsActive = request.IsActive
        };

        _db.Operations.Add(op);
        await _db.SaveChangesAsync();

        return (MapToDto(op), "ok");
    }

    public async Task<(OperationDto? dto, string status)> UpdateOperationAsync(Guid id, UpdateOperationRequest request)
    {
        var op = await _db.Operations.FirstOrDefaultAsync(o => o.Id == id);
        if (op == null) return (null, "not_found");

        if (request.OperationCode != null) op.OperationCode = request.OperationCode;
        if (request.OperationName != null) op.OperationName = request.OperationName;
        if (request.SetupTime.HasValue) op.SetupTime = request.SetupTime.Value;
        if (request.RunTime.HasValue) op.RunTime = request.RunTime.Value;
        if (request.CostPerHour.HasValue) op.CostPerHour = request.CostPerHour.Value;
        if (request.IsOutsourced.HasValue) op.IsOutsourced = request.IsOutsourced.Value;
        if (request.IsActive.HasValue) op.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();

        return (MapToDto(op), "ok");
    }

    public async Task<(bool ok, string status)> DeleteOperationAsync(Guid id)
    {
        var op = await _db.Operations.FirstOrDefaultAsync(o => o.Id == id);
        if (op == null) return (false, "not_found");

        var isUsed = await _db.RoutingOperations.AnyAsync(ro => ro.OperationId == id);
        if (isUsed) return (false, "guarded");

        _db.Operations.Remove(op);
        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    private static OperationDto MapToDto(Operation o) => new()
    {
        Id = o.Id,
        OperationCode = o.OperationCode,
        OperationName = o.OperationName,
        SetupTime = o.SetupTime,
        RunTime = o.RunTime,
        CostPerHour = o.CostPerHour,
        IsOutsourced = o.IsOutsourced,
        IsActive = o.IsActive
    };
}
