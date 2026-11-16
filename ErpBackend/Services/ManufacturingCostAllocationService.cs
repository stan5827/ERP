using ErpBackend.Data;
using ErpBackend.Helpers;
using ErpBackend.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface IManufacturingCostAllocationService
{
    Task<(bool success, string status)> AllocatePeriodAsync(AccountingPeriod period);
}

public class ManufacturingCostAllocationService : IManufacturingCostAllocationService
{
    private static readonly string[] AllocatedAccountCodes =
        ["5122", "5123", "5124", "5125", "5126", "5127", "5128", "5129"];

    private readonly ErpGeneralDbContext _db;

    public ManufacturingCostAllocationService(ErpGeneralDbContext db) => _db = db;

    public async Task<(bool success, string status)> AllocatePeriodAsync(AccountingPeriod period)
    {
        var existingVoucher = await _db.AccountingVouchers
            .FirstOrDefaultAsync(voucher =>
                voucher.ReferenceType == "ManufacturingCostAllocation"
                && voucher.ReferenceId == period.Id
                && voucher.PostingPurpose == "monthly_overhead");
        if (existingVoucher is not null)
            return existingVoucher.Status == "Posted"
                ? (true, "already_allocated")
                : (false, "allocation_voucher_pending");

        var (start, endExclusive) = GetPeriodBounds(period);
        var sourceEntries = await _db.AccountingEntries
            .AsNoTracking()
            .Where(entry => entry.Voucher.Status == "Posted"
                && entry.Voucher.VoucherDate >= start
                && entry.Voucher.VoucherDate < endExclusive
                && (entry.GLAccount.AccountCode == "5121"
                    || AllocatedAccountCodes.Contains(entry.GLAccount.AccountCode))
                && entry.Voucher.ReferenceType != "ManufacturingCostAllocation"
                && entry.DebitAmount != entry.CreditAmount)
            .Select(entry => new SourceEntry(
                entry.GLAccountId,
                entry.GLAccount.AccountCode,
                entry.CostCenterId,
                entry.WorkOrderId,
                entry.Voucher.ReferenceType,
                entry.Voucher.ReferenceId,
                entry.DebitAmount - entry.CreditAmount))
            .ToListAsync();

        if (sourceEntries.Any(entry => entry.AccountCode == "5121"))
            return (false, "allocation_blocked:5121 為製造費用彙總科目，請將直接過帳改至 5122–5129 明細科目");
        if (sourceEntries.Count == 0)
            return (true, "no_costs");

        var mappings = await _db.GLAccountMappings
            .AsNoTracking()
            .Where(mapping => mapping.MappingKey == "work_in_progress"
                || mapping.MappingKey == "finished_goods_inventory"
                || mapping.MappingKey == "sales_cogs")
            .Join(_db.GLAccounts.Where(account => account.IsActive),
                mapping => mapping.GLAccountId,
                account => account.Id,
                (mapping, account) => new { mapping.MappingKey, account.Id })
            .ToDictionaryAsync(item => item.MappingKey, item => item.Id);
        if (!mappings.TryGetValue("work_in_progress", out var wipAccountId)
            || !mappings.TryGetValue("finished_goods_inventory", out var finishedGoodsAccountId)
            || !mappings.TryGetValue("sales_cogs", out var cogsAccountId)
            || new[] { wipAccountId, finishedGoodsAccountId, cogsAccountId }.Distinct().Count() != 3)
            return (false, "allocation_blocked:請先設定互不相同且有效的在製品、成品存貨與銷貨成本科目映射");

        var apIds = sourceEntries
            .Where(entry => entry.AccountCode == "5128"
                && entry.WorkOrderId is null
                && entry.ReferenceType == "AccountsPayable"
                && entry.ReferenceId.HasValue)
            .Select(entry => entry.ReferenceId!.Value)
            .Distinct()
            .ToList();
        var workOrderByAp = apIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await (
                from payable in _db.AccountsPayables.AsNoTracking()
                join purchaseOrder in _db.PurchaseOrders.AsNoTracking()
                    on payable.PurchaseOrderId equals purchaseOrder.Id
                join requisition in _db.PurchaseRequisitions.AsNoTracking()
                    on purchaseOrder.PrId equals requisition.Id
                where apIds.Contains(payable.Id) && requisition.WorkOrderId.HasValue
                select new { payable.Id, WorkOrderId = requisition.WorkOrderId!.Value })
                .ToDictionaryAsync(item => item.Id, item => item.WorkOrderId);

        var errorMessages = new List<string>();
        var normalizedEntries = new List<SourceEntry>(sourceEntries.Count);
        foreach (var entry in sourceEntries)
        {
            if (entry.AccountCode == "5128" && entry.WorkOrderId is null)
            {
                if (entry.ReferenceType == "AccountsPayable"
                    && entry.ReferenceId.HasValue
                    && workOrderByAp.TryGetValue(entry.ReferenceId.Value, out var linkedWorkOrderId))
                {
                    normalizedEntries.Add(entry with { WorkOrderId = linkedWorkOrderId });
                }
                else
                {
                    errorMessages.Add($"5128 分錄 {entry.ReferenceId?.ToString("N") ?? "無來源單據"} 未連結委外工單");
                }
            }
            else
            {
                normalizedEntries.Add(entry);
            }
        }

        var sourceCostCenterIds = normalizedEntries
            .Where(entry => entry.CostCenterId.HasValue)
            .Select(entry => entry.CostCenterId!.Value)
            .Distinct()
            .ToList();
        var sourceCenters = sourceCostCenterIds.Count == 0
            ? new Dictionary<Guid, CostCenter>()
            : await _db.CostCenters.AsNoTracking()
                .Where(center => sourceCostCenterIds.Contains(center.Id))
                .ToDictionaryAsync(center => center.Id);
        var productionCenters = await _db.CostCenters.AsNoTracking()
            .Where(center => center.IsActive
                && center.CostCenterType == "production"
                && center.EffectiveFrom <= endExclusive
                && (!center.EffectiveTo.HasValue || center.EffectiveTo >= start))
            .ToListAsync();
        var effectiveWorkCenterCosts = await _db.WorkCenterCosts.AsNoTracking()
            .Where(cost => cost.EffectiveFrom < endExclusive
                && (!cost.EffectiveTo.HasValue || cost.EffectiveTo >= start))
            .OrderByDescending(cost => cost.EffectiveFrom)
            .ToListAsync();
        var costCenterByWorkCenter = effectiveWorkCenterCosts
            .GroupBy(cost => cost.WorkCenterId)
            .ToDictionary(group => group.Key, group => group.First().CostCenterId);
        foreach (var center in productionCenters.Where(center => center.WorkCenterId.HasValue))
            costCenterByWorkCenter.TryAdd(center.WorkCenterId!.Value, center.Id);
        var targetCenters = productionCenters
            .Where(center => center.WorkCenterId.HasValue
                || effectiveWorkCenterCosts.Any(cost => cost.CostCenterId == center.Id))
            .ToDictionary(center => center.Id);
        var centerById = productionCenters.ToDictionary(center => center.Id);

        var laborActuals = await _db.WorkOrderLaborActuals.AsNoTracking()
            .Where(actual => actual.CreatedAt >= start && actual.CreatedAt < endExclusive)
            .ToListAsync();
        var machineActuals = await _db.WorkOrderMachineActuals.AsNoTracking()
            .Where(actual => actual.CreatedAt >= start && actual.CreatedAt < endExclusive)
            .ToListAsync();
        var materialActuals = await _db.WorkOrderMaterialActuals.AsNoTracking()
            .Where(actual => actual.TransactionDate >= start && actual.TransactionDate < endExclusive)
            .ToListAsync();
        var approvedReports = await _db.WipReports.AsNoTracking()
            .Where(report => report.Status == "approved"
                && report.CreatedAt >= start && report.CreatedAt < endExclusive)
            .ToListAsync();
        var receiptOutput = await (
            from item in _db.ProductionReceiptItems.AsNoTracking()
            join receipt in _db.ProductionReceipts.AsNoTracking()
                on item.ProductionReceiptId equals receipt.Id
            where receipt.Status == "posted"
                && receipt.ReceivedAt >= start && receipt.ReceivedAt < endExclusive
            group item by receipt.WorkOrderId into workOrderItems
            select new { WorkOrderId = workOrderItems.Key, Quantity = workOrderItems.Sum(item => item.Quantity) })
            .ToDictionaryAsync(item => item.WorkOrderId, item => item.Quantity);

        var workOrderIds = laborActuals.Select(actual => actual.WorkOrderId)
            .Concat(machineActuals.Select(actual => actual.WorkOrderId))
            .Concat(materialActuals.Select(actual => actual.WorkOrderId))
            .Concat(approvedReports.Select(report => report.WorkOrderId))
            .Concat(receiptOutput.Keys)
            .Concat(normalizedEntries.Where(entry => entry.AccountCode == "5128" && entry.WorkOrderId.HasValue)
                .Select(entry => entry.WorkOrderId!.Value))
            .Distinct()
            .ToList();
        var workOrders = await _db.WorkOrders.AsNoTracking()
            .Where(order => workOrderIds.Contains(order.Id))
            .Select(order => new WorkOrderState(order.Id, order.WoNumber, order.Status, order.ActualQty))
            .ToDictionaryAsync(order => order.Id);
        var operations = await _db.WorkOrderOperations.AsNoTracking()
            .Where(operation => workOrderIds.Contains(operation.WorkOrderId))
            .Select(operation => new OperationCenter(
                operation.WorkOrderId,
                operation.OperationSeq,
                operation.WorkCenterId))
            .ToListAsync();
        var operationCenters = operations
            .Where(operation => costCenterByWorkCenter.ContainsKey(operation.WorkCenterId))
            .ToDictionary(
                operation => (operation.WorkOrderId, operation.OperationSeq),
                operation => costCenterByWorkCenter[operation.WorkCenterId]);
        var firstCenterByWorkOrder = operations
            .GroupBy(operation => operation.WorkOrderId)
            .ToDictionary(group => group.Key, group =>
            {
                var first = group.OrderBy(operation => operation.OperationSeq).First();
                return operationCenters.GetValueOrDefault((first.WorkOrderId, first.OperationSeq));
            });
        var lastCenterByWorkOrder = operations
            .GroupBy(operation => operation.WorkOrderId)
            .ToDictionary(group => group.Key, group =>
            {
                var last = group.OrderByDescending(operation => operation.OperationSeq).First();
                return operationCenters.GetValueOrDefault((last.WorkOrderId, last.OperationSeq));
            });

        var drivers = new Dictionary<(Guid WorkOrderId, Guid CostCenterId), DriverTotals>();
        DriverTotals GetDriver(Guid workOrderId, Guid costCenterId)
        {
            var key = (workOrderId, costCenterId);
            if (!drivers.TryGetValue(key, out var totals))
            {
                totals = new DriverTotals();
                drivers.Add(key, totals);
            }

            return totals;
        }

        foreach (var actual in laborActuals)
        {
            if (operationCenters.TryGetValue((actual.WorkOrderId, actual.OperationSeq), out var centerId))
                GetDriver(actual.WorkOrderId, centerId).LaborHours += Math.Max(actual.ActualHours, 0);
        }
        foreach (var actual in machineActuals)
        {
            var centerId = actual.WorkCenterId.HasValue
                && costCenterByWorkCenter.TryGetValue(actual.WorkCenterId.Value, out var mappedCenterId)
                    ? mappedCenterId
                    : operationCenters.GetValueOrDefault((actual.WorkOrderId, actual.OperationSeq));
            if (centerId != Guid.Empty)
                GetDriver(actual.WorkOrderId, centerId).MachineHours += Math.Max(actual.ActualHours, 0);
        }
        foreach (var actual in materialActuals)
        {
            if (firstCenterByWorkOrder.TryGetValue(actual.WorkOrderId, out var centerId)
                && centerId != Guid.Empty)
                GetDriver(actual.WorkOrderId, centerId).MaterialCost += actual.TotalAmount;
        }

        var finalOperationByWorkOrder = operations
            .GroupBy(operation => operation.WorkOrderId)
            .ToDictionary(group => group.Key, group => group.Max(operation => operation.OperationSeq));
        var reportOutput = approvedReports
            .Where(report => finalOperationByWorkOrder.TryGetValue(report.WorkOrderId, out var seq)
                && report.OperationSeq == seq)
            .GroupBy(report => report.WorkOrderId)
            .ToDictionary(group => group.Key, group => group.Sum(report => Math.Max(report.CompletedQty, 0)));
        foreach (var workOrderId in workOrderIds)
        {
            var outputQty = reportOutput.GetValueOrDefault(workOrderId);
            if (outputQty <= 0)
                outputQty = receiptOutput.GetValueOrDefault(workOrderId);
            var centerId = lastCenterByWorkOrder.GetValueOrDefault(workOrderId);
            if (outputQty > 0 && centerId != Guid.Empty)
                GetDriver(workOrderId, centerId).OutputQuantity += outputQty;
        }

        foreach (var actual in laborActuals)
        {
            if (operationCenters.TryGetValue((actual.WorkOrderId, actual.OperationSeq), out var centerId))
                GetDriver(actual.WorkOrderId, centerId).DirectLaborCost += actual.TotalAmount;
        }

        foreach (var entry in normalizedEntries.Where(entry => entry.AccountCode != "5128"))
        {
            if (!entry.CostCenterId.HasValue)
                errorMessages.Add($"{entry.AccountCode} 分錄缺少成本中心");
            else if (!sourceCenters.TryGetValue(entry.CostCenterId.Value, out var sourceCenter)
                || !sourceCenter.IsActive
                || sourceCenter.EffectiveFrom >= endExclusive
                || sourceCenter.EffectiveTo.HasValue && sourceCenter.EffectiveTo.Value < start)
                errorMessages.Add($"{entry.AccountCode} 分錄成本中心不存在、已停用或未在本期生效");
        }

        var distributions = new List<Allocation>();
        foreach (var entry in normalizedEntries)
        {
            if (entry.AccountCode == "5128")
            {
                var targetCenterId = entry.WorkOrderId.HasValue
                    ? lastCenterByWorkOrder.GetValueOrDefault(entry.WorkOrderId.Value)
                    : Guid.Empty;
                if (!entry.WorkOrderId.HasValue
                    || !workOrders.ContainsKey(entry.WorkOrderId.Value)
                    || targetCenterId == Guid.Empty)
                {
                    errorMessages.Add($"5128 分錄 {entry.ReferenceId?.ToString("N") ?? "無來源單據"} 未連結有效的生產工單／工作中心");
                    continue;
                }

                distributions.Add(new Allocation(
                    entry.GLAccountId,
                    entry.AccountCode,
                    entry.CostCenterId,
                    targetCenterId,
                    entry.WorkOrderId.Value,
                    entry.Amount,
                    "委外工單實際成本"));
                continue;
            }

            if (!entry.CostCenterId.HasValue)
                continue;

            var basis = GetBasis(entry.AccountCode);
            var targetCenterIds = GetTargetCenterIds(
                entry.AccountCode,
                entry.CostCenterId.Value,
                targetCenters,
                centerById);
            if (targetCenterIds.Count == 0)
            {
                errorMessages.Add($"{entry.AccountCode} 找不到可分攤的生產成本中心");
                continue;
            }

            var workOrderBasis = entry.AccountCode == "5123" ? DriverBasis.MachineHours : basis;
            if (workOrderBasis == DriverBasis.MachineHours)
            {
                var targetCenterIdSet = targetCenterIds.ToHashSet();
                var missingMachineHours = operationCenters
                    .Where(operation => targetCenterIdSet.Contains(operation.Value)
                        && workOrders.ContainsKey(operation.Key.WorkOrderId))
                    .Select(operation => (
                        WorkOrderId: operation.Key.WorkOrderId,
                        CostCenterId: operation.Value))
                    .Distinct()
                    .Where(target => !drivers.TryGetValue(
                            (target.WorkOrderId, target.CostCenterId),
                            out var totals)
                        || totals.MachineHours <= 0)
                    .GroupBy(target => target.CostCenterId)
                    .ToList();
                foreach (var missingCenter in missingMachineHours)
                {
                    var centerCode = centerById[missingCenter.Key].CostCenterCode;
                    var workOrderNumbers = missingCenter
                        .Select(target => workOrders[target.WorkOrderId].WoNumber)
                        .Distinct()
                        .OrderBy(number => number, StringComparer.Ordinal);
                    errorMessages.Add(
                        $"{entry.AccountCode} 成本中心 {centerCode} 工單缺少機器工時：{string.Join("、", workOrderNumbers)}");
                }

                if (missingMachineHours.Count > 0)
                    continue;
            }

            var centerWeights = targetCenterIds
                .Select(centerId => new WeightedTarget(
                    centerId,
                    entry.AccountCode == "5123"
                        ? targetCenters[centerId].AreaSquareMeters ?? 0m
                        : GetCenterWeight(centerId, basis, drivers)))
                .ToList();

            if (entry.AccountCode == "5122" && centerWeights.Sum(target => target.Weight) <= 0)
            {
                centerWeights = targetCenterIds
                    .Select(centerId => new WeightedTarget(
                        centerId,
                        targetCenters[centerId].AreaSquareMeters ?? 0m))
                    .ToList();
                if (centerWeights.Sum(target => target.Weight) <= 0)
                {
                    centerWeights = targetCenterIds
                        .Select(centerId => new WeightedTarget(
                            centerId,
                            drivers.Count(pair => pair.Key.CostCenterId == centerId
                                && workOrders.ContainsKey(pair.Key.WorkOrderId))))
                        .ToList();
                }
            }

            if (centerWeights.Sum(target => target.Weight) <= 0)
            {
                errorMessages.Add(entry.AccountCode == "5123"
                    ? "5123 分攤缺少生產成本中心面積"
                    : $"{entry.AccountCode} 分攤缺少{GetBasisLabel(basis)}資料");
                continue;
            }

            foreach (var centerPart in Split(entry.Amount, centerWeights))
            {
                var workOrderWeights = drivers
                    .Where(pair => pair.Key.CostCenterId == centerPart.TargetId
                        && workOrders.ContainsKey(pair.Key.WorkOrderId))
                    .Select(pair => new WeightedTarget(
                        pair.Key.WorkOrderId,
                        GetWeight(pair.Value, workOrderBasis)))
                    .Where(target => target.Weight > 0)
                    .ToList();

                if (entry.AccountCode == "5122" && workOrderWeights.Sum(target => target.Weight) <= 0)
                {
                    workOrderWeights = drivers
                        .Where(pair => pair.Key.CostCenterId == centerPart.TargetId
                            && workOrders.ContainsKey(pair.Key.WorkOrderId))
                        .Select(pair => new WeightedTarget(pair.Key.WorkOrderId, pair.Value.OutputQuantity))
                        .Where(target => target.Weight > 0)
                        .ToList();
                    if (workOrderWeights.Count == 0)
                        workOrderWeights = drivers
                            .Where(pair => pair.Key.CostCenterId == centerPart.TargetId
                                && workOrders.ContainsKey(pair.Key.WorkOrderId))
                            .Select(pair => new WeightedTarget(pair.Key.WorkOrderId, 1m))
                            .ToList();
                }

                if (workOrderWeights.Count == 0 || workOrderWeights.Sum(target => target.Weight) <= 0)
                {
                    errorMessages.Add($"{entry.AccountCode} 成本中心 {centerPart.TargetId:N} 缺少{GetBasisLabel(workOrderBasis)}分攤資料");
                    continue;
                }

                foreach (var workOrderPart in Split(centerPart.Amount, workOrderWeights))
                {
                    distributions.Add(new Allocation(
                        entry.GLAccountId,
                        entry.AccountCode,
                        entry.CostCenterId,
                        centerPart.TargetId,
                        workOrderPart.TargetId,
                        workOrderPart.Amount,
                        entry.AccountCode == "5123"
                            ? "成本中心面積／工單機器工時"
                            : GetBasisLabel(basis)));
                }
            }
        }

        if (errorMessages.Count > 0)
            return (false, $"allocation_blocked:{string.Join("；", errorMessages.Distinct())}");
        if (distributions.Count == 0)
            return (false, "allocation_blocked:本期製造費用沒有可分攤的工單");

        var settledWorkOrderIds = distributions
            .Where(item => workOrders[item.WorkOrderId].Status.Equals("settled", StringComparison.OrdinalIgnoreCase))
            .Select(item => item.WorkOrderId)
            .Distinct()
            .ToList();
        var settledSnapshots = await _db.WorkOrderCosts
            .Where(cost => settledWorkOrderIds.Contains(cost.WorkOrderId)
                && cost.Status == "settled")
            .ToDictionaryAsync(cost => cost.WorkOrderId);
        foreach (var workOrderId in settledWorkOrderIds)
        {
            if (!settledSnapshots.ContainsKey(workOrderId))
                errorMessages.Add($"已結轉工單 {workOrders[workOrderId].WoNumber} 缺少成本快照");
        }

        var settledInventoryShares = await GetSettledInventorySharesAsync(
            settledWorkOrderIds,
            settledSnapshots,
            endExclusive,
            errorMessages);
        if (errorMessages.Count > 0)
            return (false, $"allocation_blocked:{string.Join("；", errorMessages.Distinct())}");

        var voucherDate = endExclusive == DateTime.MaxValue
            ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            : endExclusive.AddTicks(-1);
        var voucher = new AccountingVoucher
        {
            VoucherNumber = DocumentNumberGenerator.Generate("MCAL"),
            VoucherDate = voucherDate,
            VoucherType = "Journal",
            Description = $"製造費用月結分攤 - {period.PeriodCode}",
            ReferenceType = "ManufacturingCostAllocation",
            ReferenceId = period.Id,
            PostingPurpose = "monthly_overhead",
            Status = "Posted",
            PostedAt = DateTime.UtcNow
        };

        var lineNumber = 1;
        foreach (var allocation in distributions)
        {
            var workOrder = workOrders[allocation.WorkOrderId];
            var memo = $"{allocation.AccountCode} {allocation.Basis} - {workOrder.WoNumber}";
            var isSettled = workOrder.Status.Equals("settled", StringComparison.OrdinalIgnoreCase);
            if (!isSettled)
            {
                AddPair(
                    voucher,
                    allocation.AccountId,
                    wipAccountId,
                    allocation.CostCenterId,
                    allocation.TargetCostCenterId,
                    allocation.WorkOrderId,
                    allocation.Amount,
                    memo,
                    ref lineNumber);
                continue;
            }

            var share = settledInventoryShares[allocation.WorkOrderId];
            var inventoryAmount = Math.Round(allocation.Amount * share, 2, MidpointRounding.AwayFromZero);
            var cogsAmount = allocation.Amount - inventoryAmount;
            if (inventoryAmount != 0)
                AddPair(
                    voucher,
                    allocation.AccountId,
                    finishedGoodsAccountId,
                    allocation.CostCenterId,
                    allocation.TargetCostCenterId,
                    allocation.WorkOrderId,
                    inventoryAmount,
                    $"{memo}（未售成品）",
                    ref lineNumber);
            if (cogsAmount != 0)
                AddPair(
                    voucher,
                    allocation.AccountId,
                    cogsAccountId,
                    allocation.CostCenterId,
                    allocation.TargetCostCenterId,
                    allocation.WorkOrderId,
                    cogsAmount,
                    $"{memo}（已售銷貨成本）",
                    ref lineNumber);
        }

        AccountingVoucherRules.ValidateForPosting(voucher);
        _db.AccountingVouchers.Add(voucher);
        foreach (var allocation in distributions.Where(item =>
                     workOrders[item.WorkOrderId].Status.Equals("settled", StringComparison.OrdinalIgnoreCase)))
        {
            var snapshot = settledSnapshots[allocation.WorkOrderId];
            var share = settledInventoryShares[allocation.WorkOrderId];
            snapshot.OverheadActualCost += allocation.Amount;
            snapshot.TotalActualCost += allocation.Amount;
            snapshot.CapitalizedActualCost += Math.Round(
                allocation.Amount * share,
                2,
                MidpointRounding.AwayFromZero);
            snapshot.VarianceAmount += allocation.Amount;
            snapshot.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    private async Task<Dictionary<Guid, decimal>> GetSettledInventorySharesAsync(
        List<Guid> workOrderIds,
        Dictionary<Guid, WorkOrderCost> snapshots,
        DateTime endExclusive,
        List<string> errors)
    {
        if (workOrderIds.Count == 0)
            return [];

        var receiptLots = await (
            from item in _db.ProductionReceiptItems.AsNoTracking()
            join receipt in _db.ProductionReceipts.AsNoTracking()
                on item.ProductionReceiptId equals receipt.Id
            join lot in _db.FqcInspectionLots.AsNoTracking()
                on item.FqcInspectionLotId equals lot.Id
            where workOrderIds.Contains(receipt.WorkOrderId)
                && receipt.Status == "posted"
                && receipt.ReceivedAt < endExclusive
            select new ProducedLot(
                receipt.WorkOrderId,
                item.SkuId,
                lot.LotNumber,
                item.Quantity))
            .ToListAsync();

        var skuIds = receiptLots.Select(lot => lot.SkuId).Distinct().ToList();
        var lotNumbers = receiptLots.Select(lot => lot.LotNumber).Distinct().ToList();
        List<InventoryLotMovement> inventoryTransactions = skuIds.Count == 0
            ? []
            : await _db.InventoryTransactions.AsNoTracking()
                .Where(transaction => skuIds.Contains(transaction.SkuId)
                    && lotNumbers.Contains(transaction.LotNumber!)
                    && transaction.CreatedAt < endExclusive)
                .Select(transaction => new InventoryLotMovement(
                    transaction.SkuId,
                    transaction.LotNumber!,
                    transaction.Type,
                    transaction.QtyChange))
                .ToListAsync();

        var result = new Dictionary<Guid, decimal>();
        foreach (var workOrderId in workOrderIds)
        {
            if (!snapshots.TryGetValue(workOrderId, out var snapshot) || snapshot.TotalOutputQty <= 0)
            {
                errors.Add($"已結轉工單 {workOrderId:N} 缺少有效完工數");
                continue;
            }

            var lots = receiptLots.Where(lot => lot.WorkOrderId == workOrderId).ToList();
            if (lots.Count == 0)
            {
                errors.Add($"已結轉工單 {workOrderId:N} 缺少可追溯的生產批次");
                continue;
            }

            var receivedQty = lots.Sum(lot => lot.Quantity);
            var lotKeys = lots.Select(lot => (lot.SkuId, lot.LotNumber)).ToHashSet();
            var transactions = inventoryTransactions
                .Where(transaction => transaction.LotNumber is not null
                    && lotKeys.Contains((transaction.SkuId, transaction.LotNumber)))
                .ToList();
            var onHandQty = transactions.Sum(transaction => transaction.QtyChange);
            var shippedQty = -transactions
                .Where(transaction => transaction.Type == "sales_delivery")
                .Sum(transaction => Math.Min(transaction.QtyChange, 0));
            if (onHandQty < 0
                || Math.Abs(receivedQty - onHandQty - shippedQty) > 0.01m
                || receivedQty + 0.01m < snapshot.TotalOutputQty)
            {
                errors.Add($"已結轉工單 {workOrderId:N} 的批次庫存與出貨數量無法核對");
                continue;
            }

            result[workOrderId] = Math.Clamp(onHandQty / snapshot.TotalOutputQty, 0m, 1m);
        }

        return result;
    }

    private static void AddPair(
        AccountingVoucher voucher,
        Guid expenseAccountId,
        Guid inventoryAccountId,
        Guid? sourceCostCenterId,
        Guid targetCostCenterId,
        Guid workOrderId,
        decimal amount,
        string memo,
        ref int lineNumber)
    {
        AddSignedEntry(
            voucher,
            expenseAccountId,
            sourceCostCenterId ?? targetCostCenterId,
            workOrderId,
            -amount,
            memo,
            ref lineNumber);
        AddSignedEntry(voucher, inventoryAccountId, targetCostCenterId, workOrderId, amount, memo, ref lineNumber);
    }

    private static void AddSignedEntry(
        AccountingVoucher voucher,
        Guid accountId,
        Guid? costCenterId,
        Guid workOrderId,
        decimal amount,
        string memo,
        ref int lineNumber)
    {
        if (amount == 0)
            return;

        voucher.Entries.Add(new AccountingEntry
        {
            LineNumber = lineNumber++,
            GLAccountId = accountId,
            CostCenterId = costCenterId,
            WorkOrderId = workOrderId,
            DebitAmount = Math.Max(amount, 0),
            CreditAmount = Math.Max(-amount, 0),
            Memo = memo.Length <= 200 ? memo : memo[..200]
        });
    }

    private static List<WeightedAllocation> Split(decimal amount, List<WeightedTarget> targets)
    {
        var eligible = targets.Where(target => target.Weight > 0).ToList();
        var totalWeight = eligible.Sum(target => target.Weight);
        if (totalWeight <= 0)
            return [];

        var result = new List<WeightedAllocation>(eligible.Count);
        var allocated = 0m;
        for (var index = 0; index < eligible.Count; index++)
        {
            var target = eligible[index];
            var part = index == eligible.Count - 1
                ? amount - allocated
                : Math.Round(amount * target.Weight / totalWeight, 2, MidpointRounding.AwayFromZero);
            allocated += part;
            result.Add(new WeightedAllocation(target.TargetId, part));
        }

        return result;
    }

    private static decimal GetCenterWeight(
        Guid centerId,
        DriverBasis basis,
        Dictionary<(Guid WorkOrderId, Guid CostCenterId), DriverTotals> drivers) =>
        drivers.Where(pair => pair.Key.CostCenterId == centerId)
            .Sum(pair => GetWeight(pair.Value, basis));

    private static decimal GetWeight(DriverTotals totals, DriverBasis basis) =>
        basis switch
        {
            DriverBasis.LaborHours => totals.LaborHours,
            DriverBasis.MachineHours => totals.MachineHours,
            DriverBasis.OutputQuantity => totals.OutputQuantity,
            DriverBasis.PrimeCost => totals.MaterialCost + totals.DirectLaborCost,
            _ => 0
        };

    private static DriverBasis GetBasis(string accountCode) => accountCode switch
    {
        "5122" => DriverBasis.LaborHours,
        "5123" => DriverBasis.MachineHours,
        "5124" or "5125" or "5126" => DriverBasis.MachineHours,
        "5127" => DriverBasis.OutputQuantity,
        "5129" => DriverBasis.PrimeCost,
        _ => throw new ArgumentOutOfRangeException(nameof(accountCode), accountCode, "未知製造費用科目")
    };

    private static string GetBasisLabel(DriverBasis basis) => basis switch
    {
        DriverBasis.LaborHours => "直接人工工時",
        DriverBasis.MachineHours => "機器工時",
        DriverBasis.OutputQuantity => "完工產量",
        DriverBasis.PrimeCost => "直接材料與人工成本",
        _ => "未設定動因"
    };

    private static List<Guid> GetTargetCenterIds(
        string accountCode,
        Guid sourceCenterId,
        Dictionary<Guid, CostCenter> targetCenters,
        Dictionary<Guid, CostCenter> allProductionCenters)
    {
        if (accountCode != "5123"
            && targetCenters.ContainsKey(sourceCenterId))
            return [sourceCenterId];

        return targetCenters.Keys
            .OrderBy(id => allProductionCenters[id].DisplayOrder)
            .ThenBy(id => allProductionCenters[id].CostCenterCode)
            .ToList();
    }

    private static (DateTime start, DateTime endExclusive) GetPeriodBounds(AccountingPeriod period)
    {
        var start = period.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusive = period.EndDate == DateOnly.MaxValue
            ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            : period.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return (start, endExclusive);
    }

    private sealed record SourceEntry(
        Guid GLAccountId,
        string AccountCode,
        Guid? CostCenterId,
        Guid? WorkOrderId,
        string? ReferenceType,
        Guid? ReferenceId,
        decimal Amount);

    private sealed record WorkOrderState(Guid Id, string WoNumber, string Status, decimal ActualQty);
    private sealed record OperationCenter(Guid WorkOrderId, int OperationSeq, Guid WorkCenterId);
    private sealed record ProducedLot(Guid WorkOrderId, Guid SkuId, string LotNumber, decimal Quantity);
    private sealed record InventoryLotMovement(Guid SkuId, string LotNumber, string Type, decimal QtyChange);
    private sealed record WeightedTarget(Guid TargetId, decimal Weight);
    private sealed record WeightedAllocation(Guid TargetId, decimal Amount);
    private sealed record Allocation(
        Guid AccountId,
        string AccountCode,
        Guid? CostCenterId,
        Guid TargetCostCenterId,
        Guid WorkOrderId,
        decimal Amount,
        string Basis);

    private enum DriverBasis
    {
        LaborHours,
        MachineHours,
        OutputQuantity,
        PrimeCost
    }

    private sealed class DriverTotals
    {
        public decimal LaborHours { get; set; }
        public decimal MachineHours { get; set; }
        public decimal OutputQuantity { get; set; }
        public decimal MaterialCost { get; set; }
        public decimal DirectLaborCost { get; set; }
    }
}
