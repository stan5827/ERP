using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface IRoutingService
{
    Task<List<RoutingDto>> GetRoutingsAsync(Guid? productSkuId = null, Guid? operationId = null);
    Task<RoutingDto?> GetRoutingAsync(Guid id);
    Task<(RoutingDto? dto, string status)> CreateRoutingAsync(CreateRoutingRequest request);
    Task<(RoutingDto? dto, string status)> UpdateRoutingAsync(Guid id, UpdateRoutingRequest request);
}

public class RoutingService : IRoutingService
{
    private readonly ErpGeneralDbContext _db;

    public RoutingService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<RoutingDto>> GetRoutingsAsync(Guid? productSkuId = null, Guid? operationId = null)
    {
        var q = _db.Routings
            .Include(r => r.ProductSku)
            .Include(r => r.Operations)
                .ThenInclude(ro => ro.Operation)
            .Include(r => r.Operations)
                .ThenInclude(ro => ro.WorkCenter)
            .AsQueryable();

        if (productSkuId.HasValue)
            q = q.Where(r => r.ProductSkuId == productSkuId.Value);
        if (operationId.HasValue)
            q = q.Where(r => r.IsActive && r.Operations.Any(operation => operation.OperationId == operationId.Value));

        return await q.OrderBy(r => r.RoutingCode).Select(r => MapToDto(r)).ToListAsync();
    }

    public async Task<RoutingDto?> GetRoutingAsync(Guid id)
    {
        var r = await _db.Routings
            .Include(r => r.ProductSku)
            .Include(r => r.Operations)
                .ThenInclude(ro => ro.Operation)
            .Include(r => r.Operations)
                .ThenInclude(ro => ro.WorkCenter)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
        return r == null ? null : MapToDto(r);
    }

    public async Task<(RoutingDto? dto, string status)> CreateRoutingAsync(CreateRoutingRequest request)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var routing = new Routing
            {
                RoutingCode = request.RoutingCode,
                RoutingName = request.RoutingName,
                ProductSkuId = request.ProductSkuId,
                Version = request.Version ?? "1.0",
                IsActive = request.IsActive
            };

            _db.Routings.Add(routing);
            await _db.SaveChangesAsync();

            if (request.Operations != null)
            {
                foreach (var opReq in request.Operations)
                    _db.RoutingOperations.Add(ToRoutingOperation(routing.Id, opReq));
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();

            var dto = await GetRoutingAsync(routing.Id);
            return (dto, "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(RoutingDto? dto, string status)> UpdateRoutingAsync(Guid id, UpdateRoutingRequest request)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var routing = await _db.Routings
                .Include(r => r.Operations)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (routing == null) return (null, "not_found");

            if (request.RoutingCode != null) routing.RoutingCode = request.RoutingCode;
            if (request.RoutingName != null) routing.RoutingName = request.RoutingName;
            if (request.Version != null) routing.Version = request.Version;
            if (request.ProductSkuId.HasValue) routing.ProductSkuId = request.ProductSkuId.Value;
            if (request.IsActive.HasValue) routing.IsActive = request.IsActive.Value;

            if (request.Operations != null)
            {
                _db.RoutingOperations.RemoveRange(routing.Operations);
                foreach (var opReq in request.Operations)
                    _db.RoutingOperations.Add(ToRoutingOperation(routing.Id, opReq));
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var dto = await GetRoutingAsync(id);
            return (dto, "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static RoutingDto MapToDto(Routing r) => new()
    {
        Id = r.Id,
        RoutingCode = r.RoutingCode,
        RoutingName = r.RoutingName,
        ProductSkuId = r.ProductSkuId,
        ProductSkuNumber = r.ProductSku?.SkuNumber,
        ProductSkuName = r.ProductSku?.SkuName,
        Version = r.Version,
        IsActive = r.IsActive,
        Operations = r.Operations.OrderBy(ro => ro.OperationSeq).Select(ro => new RoutingOperationDto
        {
            Id = ro.Id,
            RoutingId = ro.RoutingId,
            OperationSeq = ro.OperationSeq,
            OperationId = ro.OperationId,
            WorkCenterId = ro.WorkCenterId,
            TransferTime = ro.TransferTime,
            WaitTime = ro.WaitTime,
            OperationCode = ro.Operation?.OperationCode,
            OperationName = ro.Operation?.OperationName,
            WorkCenterCode = ro.WorkCenter?.WorkCenterCode,
            WorkCenterName = ro.WorkCenter?.WorkCenterName
        }).ToList()
    };

    private static RoutingOperation ToRoutingOperation(Guid routingId, CreateRoutingOperationRequest req) => new()
    {
        RoutingId = routingId,
        OperationSeq = req.OperationSeq,
        OperationId = req.OperationId,
        WorkCenterId = req.WorkCenterId ?? Guid.NewGuid(),
        TransferTime = req.TransferTime ?? 0m,
        WaitTime = req.WaitTime ?? 0m
    };
}
