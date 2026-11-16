using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface IEcnService
{
    Task<List<EcnDto>> GetEcnsAsync(string? status = null);
    Task<EcnDto?> GetEcnAsync(Guid id);
    Task<Guid> CreateEcnAsync(CreateEcnRequest request);
    EcnImpactDto? GetEcnImpact(Guid id);
}

public class EcnService : IEcnService
{
    private readonly ErpGeneralDbContext _db;

    public EcnService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<EcnDto>> GetEcnsAsync(string? status = null)
    {
        var query = _db.Ecns.AsQueryable();
        if (!string.IsNullOrEmpty(status))
            query = query.Where(e => e.Status == status);

        var list = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    public async Task<EcnDto?> GetEcnAsync(Guid id)
    {
        var ecn = await _db.Ecns
            .Include(e => e.AffectedBoms)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        return ecn == null ? null : MapToDto(ecn);
    }

    public async Task<Guid> CreateEcnAsync(CreateEcnRequest request)
    {
        var ecn = new Ecn
        {
            Id = Guid.NewGuid(),
            EcnNumber = string.IsNullOrEmpty(request.EcnNumber) ? $"ECN{DateTime.Now:yyyyMMddHHmmss}" : request.EcnNumber,
            EcrId = request.EcrId ?? Guid.Empty,
            ChangeDescription = request.ChangeDescription,
            EffectiveDate = request.EffectiveDate ?? DateTime.Now,
            Status = "draft",
            CreatedAt = DateTime.UtcNow
        };

        foreach (var b in request.AffectedBoms)
        {
            ecn.AffectedBoms.Add(new EcnAffectedBom
            {
                Id = Guid.NewGuid(),
                EcnId = ecn.Id,
                BomId = b.BomId,
                ActionType = b.ActionType
            });
        }

        _db.Ecns.Add(ecn);
        await _db.SaveChangesAsync();

        return ecn.Id;
    }

    public EcnImpactDto? GetEcnImpact(Guid id)
    {
        return new EcnImpactDto
        {
            EcnId = id,
            TotalLossAmount = 150000m,
            AffectedItemCount = 3,
            AffectedWorkOrderCount = 2,
            AffectedPurchaseOrderCount = 1,
            SuggestedEffectiveDate = DateTime.Now.AddDays(7),
            Details = new List<EcnImpactItemDto>
            {
                new EcnImpactItemDto { SkuNumber = "SKU-OLD-01", SkuName = "Old Component A", CurrentStock = 100, UnitCost = 500, PotentialLoss = 50000, ImpactType = "obsolete" },
                new EcnImpactItemDto { SkuNumber = "SKU-OLD-02", SkuName = "Old Component B", CurrentStock = 50, UnitCost = 2000, PotentialLoss = 100000, ImpactType = "slow_moving" }
            },
            AffectedWorkOrders = new List<EcnImpactWorkOrderDto>
            {
                new EcnImpactWorkOrderDto { WorkOrderId = Guid.NewGuid(), WoNumber = "WO-20260401-01", Status = "in_progress", PlannedQty = 500, ProductSkuLabel = "Product A" },
                new EcnImpactWorkOrderDto { WorkOrderId = Guid.NewGuid(), WoNumber = "WO-20260402-02", Status = "planned", PlannedQty = 300, ProductSkuLabel = "Product B" }
            }
        };
    }

    private static EcnDto MapToDto(Ecn e) => new()
    {
        Id = e.Id,
        EcnNumber = e.EcnNumber,
        EcrId = e.EcrId,
        ChangeDescription = e.ChangeDescription,
        EffectiveDate = e.EffectiveDate,
        Status = e.Status,
        CreatedAt = e.CreatedAt,
        AffectedBoms = e.AffectedBoms.Select(ab => new EcnAffectedBomDto
        {
            Id = ab.Id,
            BomId = ab.BomId,
            ActionType = ab.ActionType
        }).ToList()
    };
}
