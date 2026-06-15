using ErpBackend.Models.Finance;
using ErpBackend.Data;
using ErpBackend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public class CostingService : ICostingService
{
    private readonly ErpGeneralDbContext _db;

    public CostingService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public Task<StandardCostBreakdownDto> CalculateStandardCostAsync(Guid workOrderId)
    {
        return Task.FromResult(new StandardCostBreakdownDto
        {
            MaterialCost = 100,
            LaborCost = 50,
            MachineCost = 20
        });
    }

    public async Task<WorkOrderCostAnalysisDto> GetActualCostAnalysisAsync(Guid workOrderId)
    {
        var materials = await _db.WorkOrderMaterialActuals.Where(m => m.WorkOrderId == workOrderId).SumAsync(m => m.TotalAmount);
        var labor = await _db.WorkOrderLaborActuals.Where(l => l.WorkOrderId == workOrderId).SumAsync(l => l.TotalAmount);
        var machine = await _db.WorkOrderMachineActuals.Where(m => m.WorkOrderId == workOrderId).SumAsync(m => m.TotalAmount);

        return new WorkOrderCostAnalysisDto
        {
            WorkOrderId = workOrderId,
            ActualMaterialCost = materials,
            ActualLaborCost = labor,
            ActualOverheadCost = machine,
            TotalActualCost = materials + labor + machine
        };
    }

    public async Task<WorkOrderCost> SettleWorkOrderAsync(Guid workOrderId)
    {
        var analysis = await GetActualCostAnalysisAsync(workOrderId);
        var cost = new WorkOrderCost
        {
            WorkOrderId = workOrderId,
            MaterialCost = analysis.ActualMaterialCost,
            LaborCost = analysis.ActualLaborCost,
            OverheadCost = analysis.ActualOverheadCost,
            TotalCost = analysis.TotalActualCost,
            SettlementDate = DateTime.UtcNow
        };
        _db.WorkOrderCosts.Add(cost);
        await _db.SaveChangesAsync();
        return cost;
    }

    public async Task<CostCenterReportDto> GetCostCenterReportAsync(Guid costCenterId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var cc = await _db.CostCenters.FindAsync(costCenterId);
        if (cc == null)
            throw new ArgumentException($"Cost center {costCenterId} not found");

        var query = _db.CostCenterPostings
            .Include(p => p.CostElement)
            .Where(p => p.CostCenterId == costCenterId);

        if (fromDate.HasValue)
            query = query.Where(p => p.PostingDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(p => p.PostingDate <= toDate.Value);

        var postings = await query.ToListAsync();

        var costByElement = postings
            .GroupBy(p => p.CostElement.ElementName)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var activities = await _db.ActivityTypes
            .Where(a => a.CostCenterId == costCenterId && a.IsActive)
            .ToListAsync();

        var costByActivity = activities.ToDictionary(a => a.ActivityName, a => a.StandardRate);

        return new CostCenterReportDto
        {
            CostCenterId = costCenterId,
            CostCenterCode = cc.CostCenterCode,
            CostCenterName = cc.CostCenterName,
            CostCenterType = cc.CostCenterType,
            TotalBudget = cc.BudgetLimit,
            TotalUsed = postings.Sum(p => p.Amount),
            CostByElement = costByElement,
            CostByActivity = costByActivity
        };
    }

    public async Task<decimal> CalculateActivityCostAsync(Guid activityTypeId, decimal quantity)
    {
        var activity = await _db.ActivityTypes.FindAsync(activityTypeId);
        if (activity == null)
            throw new ArgumentException($"Activity type {activityTypeId} not found");

        var rate = activity.ActualRate ?? activity.StandardRate;
        return rate * quantity;
    }

    public async Task<Dictionary<Guid, decimal>> AllocateCostsAsync(Guid sourceCostCenterId, DateTime periodEnd)
    {
        var rules = await _db.AllocationRules
            .Where(r => r.SourceCostCenterId == sourceCostCenterId
                     && r.IsActive
                     && r.EffectiveFrom <= periodEnd
                     && (!r.EffectiveTo.HasValue || r.EffectiveTo >= periodEnd))
            .ToListAsync();

        var sourcePostings = await _db.CostCenterPostings
            .Where(p => p.CostCenterId == sourceCostCenterId && p.PostingDate <= periodEnd)
            .SumAsync(p => p.Amount);

        var allocations = new Dictionary<Guid, decimal>();
        foreach (var rule in rules)
        {
            var amount = sourcePostings * (rule.Percentage / 100);
            allocations[rule.TargetCostCenterId] = amount;
        }

        return allocations;
    }

    public async Task<decimal> GetWorkCenterTotalRateAsync(Guid workCenterId)
    {
        var cost = await _db.WorkCenterCosts
            .Where(w => w.WorkCenterId == workCenterId
                     && w.IsActiveEffective())
            .FirstOrDefaultAsync();

        return (cost?.LaborRate ?? 0) + (cost?.OverheadRate ?? 0) + (cost?.MachineRate ?? 0);
    }

    public async Task<bool> LinkWorkCenterToCostCenterAsync(Guid workCenterId, Guid costCenterId)
    {
        var cc = await _db.CostCenters.FindAsync(costCenterId);
        if (cc == null) return false;

        cc.WorkCenterId = workCenterId;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<WorkCenterCostDto> SetWorkCenterRatesAsync(CreateWorkCenterCostRequest request)
    {
        var existing = await _db.WorkCenterCosts
            .Where(w => w.WorkCenterId == request.WorkCenterId && w.EffectiveTo == null)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.EffectiveTo = DateTime.UtcNow;
        }

        var entity = new WorkCenterCost
        {
            WorkCenterId = request.WorkCenterId,
            CostCenterId = request.CostCenterId,
            LaborRate = request.LaborRate,
            OverheadRate = request.OverheadRate,
            MachineRate = request.MachineRate,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo
        };

        _db.WorkCenterCosts.Add(entity);
        await _db.SaveChangesAsync();

        var cc = await _db.CostCenters.FindAsync(request.CostCenterId);
        var wc = await _db.WorkCenters.FindAsync(request.WorkCenterId);

        return new WorkCenterCostDto
        {
            Id = entity.Id,
            WorkCenterId = entity.WorkCenterId,
            WorkCenterName = wc?.WorkCenterName,
            CostCenterId = entity.CostCenterId,
            CostCenterCode = cc?.CostCenterCode,
            CostCenterName = cc?.CostCenterName,
            LaborRate = entity.LaborRate,
            OverheadRate = entity.OverheadRate,
            MachineRate = entity.MachineRate,
            EffectiveFrom = entity.EffectiveFrom,
            EffectiveTo = entity.EffectiveTo
        };
    }

    public async Task UpdateBudgetUsedAsync(Guid costCenterId, decimal amount)
    {
        var cc = await _db.CostCenters.FindAsync(costCenterId);
        if (cc != null)
        {
            cc.BudgetUsed += amount;
            cc.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task RecordMaterialActualAsync(Guid woId, Guid skuId, decimal qty, decimal unitCost, string? lotNo = null)
    {
        var actual = new WorkOrderMaterialActual
        {
            WorkOrderId = woId,
            SkuId = skuId,
            Quantity = qty,
            UnitCost = unitCost,
            TotalAmount = qty * unitCost,
            LotNumber = lotNo,
            TransactionDate = DateTime.UtcNow
        };
        _db.WorkOrderMaterialActuals.Add(actual);
        await _db.SaveChangesAsync();
    }

    public async Task RecordLaborActualAsync(Guid woId, Guid reportId, int opSeq, decimal hours, decimal rate, Guid? employeeId = null)
    {
        var actual = new WorkOrderLaborActual
        {
            WorkOrderId = woId,
            WipReportId = reportId,
            OperationSeq = opSeq,
            EmployeeId = employeeId,
            ActualHours = hours,
            HourlyRate = rate,
            TotalAmount = hours * rate,
            CreatedAt = DateTime.UtcNow
        };
        _db.WorkOrderLaborActuals.Add(actual);
        await _db.SaveChangesAsync();
    }

    public async Task RecordMachineActualAsync(Guid woId, Guid reportId, int opSeq, decimal hours, decimal rate, Guid? workCenterId = null)
    {
        var actual = new WorkOrderMachineActual
        {
            WorkOrderId = woId,
            WipReportId = reportId,
            OperationSeq = opSeq,
            WorkCenterId = workCenterId,
            ActualHours = hours,
            MachineRate = rate,
            TotalAmount = hours * rate,
            CreatedAt = DateTime.UtcNow
        };
        _db.WorkOrderMachineActuals.Add(actual);
        await _db.SaveChangesAsync();
    }

    public Task<List<CategoryCostAnalysisDto>> GetCategoryAnalysisAsync(DateTime startDate, DateTime endDate)
    {
        return Task.FromResult(new List<CategoryCostAnalysisDto>());
    }

    public Task<List<DepartmentVarianceDto>> GetDepartmentVarianceAsync(DateTime startDate, DateTime endDate)
    {
        return Task.FromResult(new List<DepartmentVarianceDto>());
    }

    public Task<List<CostTrendPointDto>> GetCostTrendAsync(DateTime startDate, DateTime endDate)
    {
        return Task.FromResult(new List<CostTrendPointDto>());
    }

    public Task SettleAllCompletedWorkOrdersAsync(Guid? jobId = null)
    {
        return Task.CompletedTask;
    }
}

public static class WorkCenterCostExtensions
{
    public static bool IsActiveEffective(this WorkCenterCost w)
    {
        var now = DateTime.UtcNow;
        return w.EffectiveFrom <= now && (!w.EffectiveTo.HasValue || w.EffectiveTo >= now);
    }
}
