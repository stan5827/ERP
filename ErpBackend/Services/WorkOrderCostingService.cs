using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface IWorkOrderCostingService
{
    Task<List<WorkOrderCostSummaryDto>> GetCostingSummaryAsync();
    Task<WorkOrderCostAnalysisDto?> GetCostAnalysisAsync(Guid id);
    Task<(bool found, string status)> SettleWorkOrderCostAsync(Guid id);
    Task<string> BatchSettleAsync();
}

public class WorkOrderCostingService : IWorkOrderCostingService
{
    private sealed record StandardOperation(
        Guid OperationId,
        Guid WorkCenterId,
        decimal PlannedQty,
        Operation? Operation);

    private readonly ErpGeneralDbContext _db;
    private readonly IAccountingService _accounting;
    private readonly IUomConversionService _uomConversionService;
    private readonly IWorkOrderCostVarianceService _varianceService;

    public WorkOrderCostingService(
        ErpGeneralDbContext db,
        IAccountingService accounting,
        IUomConversionService uomConversionService)
        : this(
            db,
            accounting,
            uomConversionService,
            new WorkOrderCostVarianceService(
                db,
                Microsoft.Extensions.Options.Options.Create(new AccountingPolicyOptions())))
    {
    }

    public WorkOrderCostingService(
        ErpGeneralDbContext db,
        IAccountingService accounting,
        IUomConversionService uomConversionService,
        IWorkOrderCostVarianceService varianceService)
    {
        _db = db;
        _accounting = accounting;
        _uomConversionService = uomConversionService;
        _varianceService = varianceService;
    }

    public async Task<List<WorkOrderCostSummaryDto>> GetCostingSummaryAsync()
    {
        var workOrders = await _db.WorkOrders
            .AsNoTracking()
            .Where(workOrder => workOrder.Status.ToLower() == "completed"
                || workOrder.Status.ToLower() == "settled")
            .OrderBy(workOrder => workOrder.WoNumber)
            .Select(workOrder => new
            {
                workOrder.Id,
                workOrder.WoNumber,
                ProductSkuName = workOrder.ProductSku != null ? workOrder.ProductSku.SkuName : "",
                ProductSkuNumber = workOrder.ProductSku != null ? workOrder.ProductSku.SkuNumber : "",
                workOrder.ActualQty,
                workOrder.Status
            })
            .ToListAsync();

        var summaries = new List<WorkOrderCostSummaryDto>(workOrders.Count);
        foreach (var workOrder in workOrders)
        {
            var analysis = await GetCostAnalysisAsync(workOrder.Id);
            if (analysis == null)
                continue;

            summaries.Add(new WorkOrderCostSummaryDto
            {
                Id = workOrder.Id,
                WoNumber = workOrder.WoNumber,
                ProductSkuName = workOrder.ProductSkuName,
                ProductSkuNumber = workOrder.ProductSkuNumber,
                ActualQty = analysis.ActualQty,
                Status = workOrder.Status,
                ActualMaterialCost = analysis.ActualMaterialCost,
                ActualLaborCost = analysis.ActualLaborCost,
                ActualOverheadCost = analysis.ActualOverheadCost,
                AbnormalCost = analysis.AbnormalCost,
                CapitalizedActualCost = analysis.CapitalizedActualCost,
                TotalStandardCost = analysis.TotalStandardCost
            });
        }

        return summaries;
    }

    public async Task<WorkOrderCostAnalysisDto?> GetCostAnalysisAsync(Guid id)
    {
        var workOrder = await _db.WorkOrders
            .AsNoTracking()
            .Include(order => order.Materials)
                .ThenInclude(material => material.Sku)
            .Include(order => order.Operations)
                .ThenInclude(operation => operation.Operation)
            .FirstOrDefaultAsync(order => order.Id == id);
        if (workOrder == null)
            return null;

        var snapshot = await _db.WorkOrderCosts.AsNoTracking()
            .FirstOrDefaultAsync(cost => cost.WorkOrderId == id && cost.Status.ToLower() == "settled");

        decimal actualMaterialCost;
        decimal actualLaborCost;
        decimal actualMachineCost;
        decimal actualOverheadCost;
        decimal standardMaterialCost;
        decimal standardLaborCost;
        decimal standardOverheadCost;
        decimal totalStandardCost;
        decimal actualQty;
        decimal abnormalCost;
        decimal capitalizedActualCost;

        if (snapshot != null)
        {
            actualMaterialCost = snapshot.MaterialActualCost;
            actualLaborCost = snapshot.LaborActualCost;
            actualMachineCost = snapshot.MachineActualCost;
            actualOverheadCost = snapshot.MachineActualCost + snapshot.OverheadActualCost;
            standardMaterialCost = snapshot.StandardMaterialCost;
            standardLaborCost = snapshot.StandardLaborCost;
            standardOverheadCost = snapshot.StandardOverheadCost;
            totalStandardCost = snapshot.TotalStandardCost;
            actualQty = snapshot.TotalOutputQty;
            abnormalCost = snapshot.AbnormalCost;
            capitalizedActualCost = snapshot.CapitalizedActualCost;
        }
        else
        {
            actualMaterialCost = await _db.WorkOrderMaterialActuals
                .Where(actual => actual.WorkOrderId == id)
                .SumAsync(actual => actual.TotalAmount);
            actualLaborCost = await _db.WorkOrderLaborActuals
                .Where(actual => actual.WorkOrderId == id)
                .SumAsync(actual => actual.TotalAmount);
            actualMachineCost = await _db.WorkOrderMachineActuals
                .Where(actual => actual.WorkOrderId == id)
                .SumAsync(actual => actual.TotalAmount);
            var actualManufacturingOverhead = await _db.WorkOrderMachineActuals
                .Where(actual => actual.WorkOrderId == id)
                .SumAsync(actual => actual.OverheadAmount);
            var allocatedManufacturingOverhead = await GetAllocatedWipOverheadAsync(id);
            actualOverheadCost = actualMachineCost
                + actualManufacturingOverhead
                + allocatedManufacturingOverhead;

            (standardMaterialCost, standardLaborCost, standardOverheadCost) =
                await CalculateStandardCostsAsync(workOrder);
            totalStandardCost = standardMaterialCost + standardLaborCost + standardOverheadCost;
            actualQty = workOrder.ActualQty;
            var totalActualCostBeforeVariance = actualMaterialCost + actualLaborCost + actualOverheadCost;
            var varianceSettlement = await _varianceService.GetSettlementAsync(id, CancellationToken.None);
            abnormalCost = varianceSettlement.ApprovedAllocations.Sum(allocation => allocation.Amount);
            capitalizedActualCost = totalActualCostBeforeVariance - abnormalCost;
            if (capitalizedActualCost < 0)
                throw new InvalidOperationException("核准的異常成本分攤超過工單實際成本");
        }

        var totalActualCost = actualMaterialCost + actualLaborCost + actualOverheadCost;
        var hasActualCost = actualQty > 0 && totalActualCost > 0;
        var varianceAmount = hasActualCost ? totalActualCost - totalStandardCost : (decimal?)null;

        return new WorkOrderCostAnalysisDto
        {
            WorkOrderId = workOrder.Id,
            WoNumber = workOrder.WoNumber,
            ActualMaterialCost = actualMaterialCost,
            ActualLaborCost = actualLaborCost,
            ActualMachineCost = actualMachineCost,
            ActualOverheadCost = actualOverheadCost,
            TotalActualCost = totalActualCost,
            AbnormalCost = abnormalCost,
            CapitalizedActualCost = capitalizedActualCost,
            StandardMaterialCost = standardMaterialCost,
            StandardLaborCost = standardLaborCost,
            StandardOverheadCost = standardOverheadCost,
            TotalStandardCost = totalStandardCost,
            VarianceAmount = varianceAmount,
            VariancePercentage = hasActualCost && totalStandardCost > 0
                ? Math.Round(varianceAmount!.Value / totalStandardCost * 100, 2)
                : null,
            UnitCost = hasActualCost
                ? Math.Round(totalActualCost / actualQty, 2)
                : null,
            ActualQty = actualQty,
            HasActualCost = hasActualCost,
            Status = workOrder.Status
        };
    }

    public async Task<(bool found, string status)> SettleWorkOrderCostAsync(Guid id)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
            : null;

        try
        {
            var result = await SettleWorkOrderCostCoreAsync(id);
            if (result.found && result.status == "ok" && transaction != null)
                await transaction.CommitAsync();
            return result;
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string> BatchSettleAsync()
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable)
            : null;

        try
        {
            var workOrderIds = await _db.WorkOrders
                .Where(workOrder => workOrder.Status.ToLower() == "completed")
                .Select(workOrder => workOrder.Id)
                .ToListAsync();

            var settledCount = 0;
            var skippedCount = 0;
            foreach (var workOrderId in workOrderIds)
            {
                var result = await SettleWorkOrderCostCoreAsync(workOrderId);
                if (result.status == "ok")
                    settledCount++;
                else if (result.status == "no_actual_cost" || result.status == "not_completed"
                    || result.status == "receipt_pending" || result.status == "quality_disposition_pending"
                    || result.status == "variance_approval_pending" || result.status == "variance_exceeds_actual")
                    skippedCount++;
            }

            if (transaction != null)
                await transaction.CommitAsync();
            return $"{settledCount} 工單已結轉，{skippedCount} 筆因結算前置條件未完成而略過";
        }
        catch
        {
            if (transaction != null)
                await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<(bool found, string status)> SettleWorkOrderCostCoreAsync(Guid id)
    {
        var workOrder = await LockWorkOrderAsync(id);
        if (workOrder == null)
            return (false, "not_found");
        if (workOrder.Status.ToLower() == "settled")
            return (true, "already_settled");
        if (workOrder.Status.ToLower() != "completed")
            return (true, "not_completed");

        var analysis = await GetCostAnalysisAsync(id);
        if (analysis == null || !analysis.HasActualCost)
            return (true, "no_actual_cost");
        var varianceSettlement = await _varianceService.GetSettlementAsync(id, CancellationToken.None);
        if (varianceSettlement.HasPendingApproval)
            return (true, "variance_approval_pending");
        var abnormalCost = varianceSettlement.ApprovedAllocations.Sum(allocation => allocation.Amount);
        if (abnormalCost > analysis.TotalActualCost)
            return (true, "variance_exceeds_actual");
        var capitalizedActualCost = analysis.TotalActualCost - abnormalCost;

        var fqcLots = await _db.FqcInspectionLots
            .Where(lot => lot.WorkOrderId == id)
            .ToListAsync();
        if (fqcLots.Count == 0 ||
            fqcLots.Any(lot => lot.InspectionStatus is not ("Passed" or "Concession")) ||
            fqcLots.Sum(lot => lot.ProducedQty) < workOrder.ActualQty)
            return (true, "receipt_pending");
        if (fqcLots.Any(lot => lot.QuarantinedQty > 0))
            return (true, "quality_disposition_pending");

        var fqcLotIds = fqcLots.Select(lot => lot.Id).ToList();
        var approvedQty = fqcLots.Sum(lot => lot.ApprovedQty);
        var receiptedQty = await _db.ProductionReceiptItems
            .Where(item => fqcLotIds.Contains(item.FqcInspectionLotId))
            .SumAsync(item => (decimal?)item.Quantity) ?? 0m;
        if (receiptedQty < approvedQty || workOrder.ReceivedQty < approvedQty)
            return (true, "receipt_pending");

        await _accounting.CreateVoucherForWorkOrderCostSettlementAsync(
            id,
            analysis.TotalActualCost,
            varianceSettlement.ApprovedAllocations);

        var settledAt = DateTime.UtcNow;
        var snapshot = await _db.WorkOrderCosts
            .FirstOrDefaultAsync(cost => cost.WorkOrderId == id);
        if (snapshot == null)
        {
            snapshot = new WorkOrderCost { WorkOrderId = id };
            _db.WorkOrderCosts.Add(snapshot);
        }

        snapshot.TotalOutputQty = analysis.ActualQty;
        snapshot.MaterialActualCost = analysis.ActualMaterialCost;
        snapshot.LaborActualCost = analysis.ActualLaborCost;
        snapshot.MachineActualCost = analysis.ActualMachineCost;
        snapshot.OverheadActualCost = analysis.ActualOverheadCost - analysis.ActualMachineCost;
        snapshot.TotalActualCost = analysis.TotalActualCost;
        snapshot.AbnormalCost = abnormalCost;
        snapshot.CapitalizedActualCost = capitalizedActualCost;
        snapshot.StandardMaterialCost = analysis.StandardMaterialCost;
        snapshot.StandardLaborCost = analysis.StandardLaborCost;
        snapshot.StandardOverheadCost = analysis.StandardOverheadCost;
        snapshot.StandardUnitCost = analysis.TotalStandardCost / analysis.ActualQty;
        snapshot.TotalStandardCost = analysis.TotalStandardCost;
        snapshot.VarianceAmount = analysis.VarianceAmount!.Value;
        snapshot.Status = "settled";
        snapshot.SettledAt = settledAt;
        snapshot.UpdatedAt = settledAt;

        workOrder.Status = "settled";
        workOrder.UpdatedAt = settledAt;
        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    private async Task<WorkOrder?> LockWorkOrderAsync(Guid workOrderId)
    {
        if (_db.Database.IsRelational() &&
            _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var workOrder = await _db.WorkOrders
                .FromSqlInterpolated($"SELECT * FROM \"WorkOrders\" WHERE \"Id\" = {workOrderId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (workOrder is not null)
                await _db.Entry(workOrder).ReloadAsync();
            return workOrder;
        }

        return await _db.WorkOrders.FirstOrDefaultAsync(order => order.Id == workOrderId);
    }

    private async Task<decimal> GetAllocatedWipOverheadAsync(Guid workOrderId)
    {
        var wipAccountId = await _db.GLAccountMappings
            .Where(mapping => mapping.MappingKey == "work_in_progress")
            .Select(mapping => (Guid?)mapping.GLAccountId)
            .FirstOrDefaultAsync();
        if (!wipAccountId.HasValue)
            return 0m;

        return await (
            from entry in _db.AccountingEntries
            join voucher in _db.AccountingVouchers on entry.VoucherId equals voucher.Id
            where entry.WorkOrderId == workOrderId
                && entry.GLAccountId == wipAccountId.Value
                && voucher.ReferenceType == "ManufacturingCostAllocation"
                && voucher.Status == "Posted"
            select entry.DebitAmount - entry.CreditAmount)
            .SumAsync();
    }

    private async Task<(decimal materialCost, decimal laborCost, decimal overheadCost)> CalculateStandardCostsAsync(
        WorkOrder workOrder)
    {
        var quantityScale = workOrder.PlannedQty > 0 && workOrder.ActualQty > 0
            ? workOrder.ActualQty / workOrder.PlannedQty
            : 1m;
        decimal materialCost = 0;
        foreach (var material in workOrder.Materials)
        {
            var sku = material.Sku
                ?? throw new InvalidOperationException($"工單材料 SKU 不存在: {material.SkuId}");
            var requiredQty = material.RequiredQty * quantityScale;
            if (!string.IsNullOrWhiteSpace(material.Unit))
            {
                requiredQty = await _uomConversionService.ConvertToBaseUomAsync(
                    material.SkuId,
                    requiredQty,
                    material.Unit);
            }

            materialCost += requiredQty * sku.StandardCost;
        }

        decimal laborCost = 0;
        decimal overheadCost = 0;
        var rateDate = workOrder.PlannedStartDate ?? workOrder.CreatedAt;
        rateDate = rateDate.Kind switch
        {
            DateTimeKind.Utc => rateDate,
            DateTimeKind.Local => rateDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(rateDate, DateTimeKind.Utc)
        };

        IEnumerable<StandardOperation> standardOperations = workOrder.Operations
            .Select(operation => new StandardOperation(
                operation.OperationId,
                operation.WorkCenterId,
                operation.PlannedQty,
                operation.Operation));
        if (!standardOperations.Any())
        {
            var routing = await _db.Routings
                .AsNoTracking()
                .Include(route => route.Operations)
                    .ThenInclude(operation => operation.Operation)
                .Where(route => route.ProductSkuId == workOrder.ProductSkuId && route.IsActive)
                .OrderByDescending(route => route.Version)
                .FirstOrDefaultAsync();
            standardOperations = routing?.Operations.Select(operation => new StandardOperation(
                operation.OperationId,
                operation.WorkCenterId,
                workOrder.PlannedQty,
                operation.Operation)) ?? Enumerable.Empty<StandardOperation>();
        }

        foreach (var operation in standardOperations)
        {
            if (operation.Operation == null)
                throw new InvalidOperationException($"工單工序定義不存在: {operation.OperationId}");

            var operationQty = operation.PlannedQty * quantityScale;
            var plannedHours = operation.Operation.SetupTime
                + operation.Operation.RunTime * operationQty;
            var workCenterRate = await _db.WorkCenterCosts
                .Where(rate => rate.WorkCenterId == operation.WorkCenterId
                    && rate.EffectiveFrom <= rateDate
                    && (rate.EffectiveTo == null || rate.EffectiveTo >= rateDate))
                .OrderByDescending(rate => rate.EffectiveFrom)
                .FirstOrDefaultAsync();

            var laborRate = operation.Operation.CostPerHour > 0
                ? operation.Operation.CostPerHour
                : workCenterRate?.LaborRate ?? 0;
            laborCost += plannedHours * laborRate;
            overheadCost += plannedHours
                * ((workCenterRate?.MachineRate ?? 0) + (workCenterRate?.OverheadRate ?? 0));
        }

        return (
            Math.Round(materialCost, 2),
            Math.Round(laborCost, 2),
            Math.Round(overheadCost, 2));
    }
}
