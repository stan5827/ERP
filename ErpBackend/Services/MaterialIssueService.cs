using System.Data;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Helpers;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.QA;

namespace ErpBackend.Services;

public interface IMaterialIssueService
{
    Task<List<MaterialIssueDto>> GetIssuesAsync(Guid? workOrderId, string? status);
    Task<MaterialIssueDto?> GetIssueAsync(Guid id);
    Task<(MaterialIssueDto? dto, string status)> CreateDraftAsync(Guid workOrderId);
    Task<(MaterialIssueDto? dto, string status)> PostAsync(Guid id, PostMaterialIssueRequestDto request, Guid issuedBy);
    Task<(MaterialIssueDto? dto, string status)> CloseShortageAsync(Guid id, Guid itemId, CloseMaterialIssueShortageRequestDto request);
    Task<(MaterialIssueDto? dto, string status)> CancelDraftAsync(Guid id);
    Task<(MaterialIssueDto? dto, string status)> ReverseAsync(Guid id, Guid reversedBy);
    Task<(object? result, string status)> SubmitIssueAsync(MaterialIssueRequest request);
}

public class MaterialIssueService : IMaterialIssueService
{
    private const string FqcQuarantineLocationCode = "FQC-HOLD";
    private readonly ErpGeneralDbContext _db;
    private readonly ICostingService _costing;
    private readonly IUomConversionService _uomConversionService;

    public MaterialIssueService(
        ErpGeneralDbContext db,
        ICostingService costing,
        IUomConversionService uomConversionService)
    {
        _db = db;
        _costing = costing;
        _uomConversionService = uomConversionService;
    }

    public async Task<List<MaterialIssueDto>> GetIssuesAsync(Guid? workOrderId, string? status)
    {
        var query = _db.MaterialIssues
            .AsNoTracking()
            .Include(issue => issue.Items)
                .ThenInclude(item => item.Sku)
            .Include(issue => issue.Items)
                .ThenInclude(item => item.Allocations)
                    .ThenInclude(allocation => allocation.Warehouse)
            .Include(issue => issue.Items)
                .ThenInclude(item => item.Allocations)
                    .ThenInclude(allocation => allocation.BinLocation)
            .Include(issue => issue.WorkOrder)
            .Include(issue => issue.IssueWarehouse)
            .AsQueryable();

        if (workOrderId.HasValue)
            query = query.Where(issue => issue.WorkOrderId == workOrderId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(issue => issue.Status == status);

        var issues = await query.OrderByDescending(issue => issue.CreatedAt).ToListAsync();
        var dtos = new List<MaterialIssueDto>(issues.Count);
        foreach (var issue in issues)
            dtos.Add(await MapToDtoAsync(issue));
        return dtos;
    }

    public async Task<MaterialIssueDto?> GetIssueAsync(Guid id)
    {
        var issue = await GetIssueQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id);
        return issue == null ? null : await MapToDtoAsync(issue);
    }

    public async Task<(MaterialIssueDto? dto, string status)> CreateDraftAsync(Guid workOrderId)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)
            : null;

        var lockedWorkOrder = await LockWorkOrderAsync(workOrderId);
        if (lockedWorkOrder == null)
            return (null, "work_order_not_found");

        var workOrder = await _db.WorkOrders
            .Include(order => order.Materials)
                .ThenInclude(material => material.Sku)
            .FirstOrDefaultAsync(order => order.Id == workOrderId);
        if (workOrder == null)
            return (null, "work_order_not_found");
        if (string.Equals(workOrder.Status, "completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(workOrder.Status, "closed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(workOrder.Status, "settled", StringComparison.OrdinalIgnoreCase))
            return (null, "work_order_closed");

        var existingDraft = await _db.MaterialIssues
            .Where(issue => issue.WorkOrderId == workOrderId && issue.Status == "draft")
            .OrderByDescending(issue => issue.CreatedAt)
            .FirstOrDefaultAsync();
        if (existingDraft != null)
            return (await GetIssueAsync(existingDraft.Id), "ok");
        if (workOrder.Materials.Count == 0)
            return (null, "work_order_has_no_materials");

        var issue = new MaterialIssue
        {
            IssueNumber = DocumentNumberGenerator.GenerateMaterialIssue(),
            WorkOrderId = workOrder.Id,
            BomVersion = workOrder.BomVersion,
            Status = "draft",
            CreatedAt = DateTime.UtcNow,
            Items = new List<MaterialIssueItem>()
        };

        foreach (var material in workOrder.Materials.OrderBy(item => item.Sku?.SkuNumber))
        {
            if (material.Sku == null)
                return (null, "work_order_material_sku_not_found");

            var remainingWorkOrderQty = Math.Max(
                0,
                material.RequiredQty - material.IssuedQty - material.ClosedShortageQty);
            if (remainingWorkOrderQty == 0)
                continue;

            var requirementUnit = material.Unit ?? material.Sku.Unit;
            issue.Items.Add(new MaterialIssueItem
            {
                SkuId = material.SkuId,
                WorkOrderMaterialId = material.Id,
                Unit = requirementUnit,
                RequiredQty = remainingWorkOrderQty,
                ShortageQty = remainingWorkOrderQty,
                Status = "pending"
            });
        }

        if (issue.Items.Count == 0)
            return (null, "no_open_material_demand");

        _db.MaterialIssues.Add(issue);
        await _db.SaveChangesAsync();
        if (transaction != null)
            await transaction.CommitAsync();

        return (await GetIssueAsync(issue.Id), "ok");
    }

    public async Task<(MaterialIssueDto? dto, string status)> PostAsync(
        Guid id,
        PostMaterialIssueRequestDto request,
        Guid issuedBy)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)
            : null;

        var lockedIssue = await LockIssueAsync(id);
        if (lockedIssue == null)
            return (null, "not_found");
        var issue = await GetIssueQuery()
            .FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (issue == null)
            return (null, "not_found");
        if (issue.Status != "draft")
            return (null, "not_draft");

        var allocations = request.Allocations ?? new List<MaterialIssueAllocationRequestDto>();
        if (allocations.Any(allocation => allocation.IssuedQty <= 0))
            return (null, "invalid_quantity");

        var allocationsByItem = allocations.GroupBy(allocation => allocation.IssueItemId)
            .ToDictionary(group => group.Key, group => group.ToList());
        if (allocationsByItem.Keys.Any(itemId => issue.Items.All(item => item.Id != itemId)))
            return (null, "invalid_issue_item");

        var skuIds = allocationsByItem.Keys
            .Select(itemId => issue.Items.Single(item => item.Id == itemId).SkuId)
            .Distinct()
            .OrderBy(value => value)
            .ToList();
        var skus = new Dictionary<Guid, Sku>();
        foreach (var skuId in skuIds)
        {
            var sku = await LockSkuAsync(skuId);
            if (sku == null)
                return (null, "sku_not_found");
            skus[skuId] = sku;
        }

        var fqcLotsByMaterialId = new Dictionary<Guid, FqcInspectionLot>();
        foreach (var item in issue.Items.Where(candidate => candidate.WorkOrderMaterial?.FqcInspectionLotId != null))
        {
            var sourceLotId = item.WorkOrderMaterial!.FqcInspectionLotId!.Value;
            var sourceLot = await LockFqcInspectionLotAsync(sourceLotId);
            if (sourceLot == null)
                return (null, "fqc_lot_not_found");
            if (!string.Equals(sourceLot.DispositionStatus, "Rework", StringComparison.OrdinalIgnoreCase)
                || sourceLot.QuarantinedQty <= 0
                || sourceLot.ProductSkuId != item.SkuId
                || !sourceLot.QuarantineWarehouseId.HasValue
                || !sourceLot.QuarantineBinLocationId.HasValue)
                return (null, "fqc_rework_stock_unavailable");
            if (!allocationsByItem.TryGetValue(item.Id, out var fqcAllocations)
                || fqcAllocations.Any(allocation =>
                    allocation.WarehouseId != sourceLot.QuarantineWarehouseId.Value
                    || allocation.BinLocationId != sourceLot.QuarantineBinLocationId.Value
                    || !string.Equals(allocation.LotNumber, sourceLot.LotNumber, StringComparison.Ordinal)))
                return (null, "fqc_rework_source_must_match_quarantine_lot");

            var allocationQty = fqcAllocations.Sum(allocation => allocation.IssuedQty);
            if (allocationQty > sourceLot.QuarantinedQty)
                return (null, "fqc_rework_quantity_exceeds_quarantine");
            fqcLotsByMaterialId[item.Id] = sourceLot;
        }

        var availableBySku = new Dictionary<Guid, decimal>();
        var availableByLocation = new Dictionary<(Guid SkuId, Guid WarehouseId, Guid? BinId, string? LotNumber), decimal>();
        foreach (var sku in skus.Values)
        {
            var reservedQty = await _db.InventoryReservations
                .Where(reservation => reservation.SkuId == sku.Id && reservation.Status == "reserved")
                .SumAsync(reservation => (decimal?)reservation.Qty) ?? 0m;
            var quarantinedQty = await _db.IqcReports
                .Where(report => report.SkuId == sku.Id
                    && report.GoodsReceiptItemId != null
                    && report.Result != "pass"
                    && report.Result != "not_required")
                .SumAsync(report => (decimal?)report.ReceivedQty) ?? 0m;
            availableBySku[sku.Id] = Math.Max(0, sku.Qty - reservedQty - quarantinedQty);
        }

        foreach (var item in issue.Items)
        {
            if (!allocationsByItem.TryGetValue(item.Id, out var itemAllocations))
                continue;

            var sku = skus[item.SkuId];
            decimal totalIssuedInRequirementUnit = 0;
            foreach (var allocation in itemAllocations)
            {
                totalIssuedInRequirementUnit += await ConvertQtyAsync(
                    item.SkuId,
                    allocation.IssuedQty,
                    sku.Unit ?? item.Unit,
                    item.Unit ?? sku.Unit);
            }
            if (totalIssuedInRequirementUnit > item.RequiredQty)
                return (null, "issued_exceeds_demand");

            foreach (var requestedAllocation in itemAllocations)
            {
                if (availableBySku[sku.Id] < requestedAllocation.IssuedQty)
                    return (null, $"insufficient_stock:{sku.SkuNumber}");

                var warehouse = await _db.Warehouses
                    .FirstOrDefaultAsync(candidate =>
                        candidate.Id == requestedAllocation.WarehouseId && candidate.IsActive);
                if (warehouse == null)
                    return (null, $"warehouse_not_found:{sku.SkuNumber}");

                if (requestedAllocation.BinLocationId.HasValue)
                {
                    var binLocation = await _db.BinLocations
                        .AsNoTracking()
                        .Where(bin =>
                        bin.Id == requestedAllocation.BinLocationId.Value
                        && bin.WarehouseId == warehouse.Id
                        && bin.IsActive)
                        .Select(bin => new { bin.Id, bin.LocationCode })
                        .FirstOrDefaultAsync();
                    if (binLocation == null)
                        return (null, $"bin_location_invalid:{sku.SkuNumber}");
                    if (string.Equals(binLocation.LocationCode, FqcQuarantineLocationCode, StringComparison.OrdinalIgnoreCase)
                        && !fqcLotsByMaterialId.ContainsKey(item.Id))
                        return (null, "fqc_quarantine_requires_rework");
                }

                if (sku.LotControlled && string.IsNullOrWhiteSpace(requestedAllocation.LotNumber))
                    return (null, $"lot_required:{sku.SkuNumber}");
                if (sku.SerialControlled)
                    return (null, $"serial_controlled_not_supported:{sku.SkuNumber}");

                var locationKey = (
                    SkuId: sku.Id,
                    WarehouseId: warehouse.Id,
                    BinId: requestedAllocation.BinLocationId,
                    LotNumber: sku.LotControlled ? requestedAllocation.LotNumber : null);
                if (!availableByLocation.TryGetValue(locationKey, out var locationQty))
                {
                    locationQty = await GetLocationBalanceAsync(
                        locationKey.SkuId,
                        locationKey.WarehouseId,
                        locationKey.BinId,
                        locationKey.LotNumber);
                    availableByLocation[locationKey] = locationQty;
                }
                if (locationQty < requestedAllocation.IssuedQty)
                    return (null, $"location_stock_insufficient:{sku.SkuNumber}");

                var sourceFqcLot = fqcLotsByMaterialId.GetValueOrDefault(item.Id);
                var unitCost = sourceFqcLot != null
                    ? await GetFqcLotActualUnitCostAsync(sourceFqcLot)
                    : await GetMaterialIssueUnitCostAsync(sku, requestedAllocation.LotNumber);
                InventoryLot? inventoryLot = null;
                if (sourceFqcLot != null)
                {
                    inventoryLot = await _db.InventoryLots.FirstOrDefaultAsync(candidate =>
                        candidate.SkuId == sku.Id
                        && candidate.LotNumber == sourceFqcLot.LotNumber
                        && candidate.WarehouseId == requestedAllocation.WarehouseId
                        && candidate.BinLocationId == requestedAllocation.BinLocationId);
                    if (inventoryLot == null || inventoryLot.Qty < requestedAllocation.IssuedQty)
                        return (null, "fqc_rework_lot_balance_insufficient");
                }
                var inventoryTransaction = new InventoryTransaction
                {
                    TrxId = DocumentNumberGenerator.GenerateShort("ISS"),
                    SkuId = sku.Id,
                    Type = "work_order_issue",
                    QtyChange = -requestedAllocation.IssuedQty,
                    QtyBefore = sku.Qty,
                    QtyAfter = sku.Qty - requestedAllocation.IssuedQty,
                    LotNumber = requestedAllocation.LotNumber,
                    RelatedDocType = "MaterialIssue",
                    RelatedDocId = issue.Id,
                    WarehouseId = warehouse.Id,
                    BinLocationId = requestedAllocation.BinLocationId,
                    HandlerId = issuedBy == Guid.Empty ? null : issuedBy,
                    UnitCost = unitCost,
                    TotalCost = requestedAllocation.IssuedQty * unitCost,
                    Notes = $"領料單 {issue.IssueNumber} / 工單 {issue.WorkOrder?.WoNumber ?? issue.WorkOrderId.ToString()}"
                };
                if (sourceFqcLot != null)
                {
                    inventoryTransaction.RelatedDocType = "FqcReworkIssue";
                    inventoryTransaction.RelatedDocId = sourceFqcLot.Id;
                    inventoryTransaction.Type = "fqc_rework_issue";
                    inventoryTransaction.Notes = $"返修工單 {issue.WorkOrder?.WoNumber ?? issue.WorkOrderId.ToString()} / FQC {sourceFqcLot.InspectionNumber}";
                }
                _db.InventoryTransactions.Add(inventoryTransaction);
                _db.MaterialIssueItemAllocations.Add(new MaterialIssueItemAllocation
                {
                    IssueItemId = item.Id,
                    WarehouseId = warehouse.Id,
                    BinLocationId = requestedAllocation.BinLocationId,
                    LotNumber = requestedAllocation.LotNumber,
                    IssuedQty = requestedAllocation.IssuedQty,
                    UnitCost = unitCost,
                    InventoryTransaction = inventoryTransaction
                });

                sku.Qty -= requestedAllocation.IssuedQty;
                sku.UpdatedAt = DateTime.UtcNow;
                if (sourceFqcLot != null)
                {
                    inventoryLot!.Qty -= requestedAllocation.IssuedQty;
                    if (inventoryLot.Qty == 0)
                        inventoryLot.Status = "consumed";
                    sourceFqcLot.QuarantinedQty -= requestedAllocation.IssuedQty;
                    if (sourceFqcLot.QuarantinedQty == 0)
                        sourceFqcLot.DispositionStatus = "ReworkIssued";
                }
                availableBySku[sku.Id] -= requestedAllocation.IssuedQty;
                availableByLocation[locationKey] -= requestedAllocation.IssuedQty;
                item.IssuedQty += await ConvertQtyAsync(
                    item.SkuId,
                    requestedAllocation.IssuedQty,
                    sku.Unit ?? item.Unit,
                    item.Unit ?? sku.Unit);

                await _costing.RecordMaterialActualAsync(
                    issue.WorkOrderId,
                    sku.Id,
                    requestedAllocation.IssuedQty,
                    unitCost,
                    requestedAllocation.LotNumber,
                    inventoryTransaction.Id);
            }

            item.ShortageQty = Math.Max(0, item.RequiredQty - item.IssuedQty - item.ClosedShortageQty);
            item.Status = GetItemStatus(item);
            if (item.WorkOrderMaterial != null)
            {
                var workOrderMaterialQty = await ConvertQtyAsync(
                    item.SkuId,
                    item.IssuedQty,
                    item.Unit,
                    item.WorkOrderMaterial.Unit ?? item.Sku?.Unit);
                item.WorkOrderMaterial.IssuedQty += workOrderMaterialQty;
                item.WorkOrderMaterial.ShortageQty = Math.Max(
                    0,
                    item.WorkOrderMaterial.RequiredQty
                    - item.WorkOrderMaterial.IssuedQty
                    - item.WorkOrderMaterial.ClosedShortageQty);
                item.WorkOrderMaterial.Status = item.WorkOrderMaterial.ShortageQty == 0
                    ? "completed"
                    : item.WorkOrderMaterial.IssuedQty > 0 ? "partially_issued" : "shortage";
            }
        }

        issue.IssuedBy = issuedBy == Guid.Empty ? null : issuedBy;
        issue.IssuedAt = DateTime.UtcNow;
        issue.Status = issue.Items.All(item => item.ShortageQty == 0)
            ? "issued"
            : "partially_issued";
        var warehouseIds = allocations.Select(allocation => allocation.WarehouseId).Distinct().ToList();
        if (warehouseIds.Count == 1)
            issue.IssueWarehouseId = warehouseIds[0];

        await _db.SaveChangesAsync();
        if (transaction != null)
            await transaction.CommitAsync();

        return (await GetIssueAsync(issue.Id), "ok");
    }

    public async Task<(MaterialIssueDto? dto, string status)> CloseShortageAsync(
        Guid id,
        Guid itemId,
        CloseMaterialIssueShortageRequestDto request)
    {
        if (request.Quantity <= 0 || string.IsNullOrWhiteSpace(request.Reason))
            return (null, "invalid_closure");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)
            : null;
        var lockedIssue = await LockIssueAsync(id);
        if (lockedIssue == null)
            return (null, "not_found");
        var issue = await GetIssueQuery()
            .FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (issue == null)
            return (null, "not_found");
        if (issue.Status != "partially_issued")
            return (null, "not_closable");

        var item = issue.Items.FirstOrDefault(candidate => candidate.Id == itemId);
        if (item == null)
            return (null, "item_not_found");
        var remainingQty = Math.Max(0, item.RequiredQty - item.IssuedQty - item.ClosedShortageQty);
        if (request.Quantity > remainingQty)
            return (null, "closure_exceeds_shortage");
        if (item.WorkOrderMaterial == null)
            return (null, "work_order_material_not_found");

        var workOrderUnit = item.WorkOrderMaterial.Unit ?? item.Sku?.Unit;
        var closedWorkOrderQty = await ConvertQtyAsync(
            item.SkuId,
            request.Quantity,
            item.Unit,
            workOrderUnit);
        item.ClosedShortageQty += request.Quantity;
        item.ShortageClosureReason = request.Reason.Trim();
        item.ShortageQty = Math.Max(0, remainingQty - request.Quantity);
        item.Status = GetItemStatus(item);
        item.WorkOrderMaterial.ClosedShortageQty += closedWorkOrderQty;
        item.WorkOrderMaterial.ShortageQty = Math.Max(
            0,
            item.WorkOrderMaterial.RequiredQty
            - item.WorkOrderMaterial.IssuedQty
            - item.WorkOrderMaterial.ClosedShortageQty);
        item.WorkOrderMaterial.Status = item.WorkOrderMaterial.ShortageQty == 0
            ? "completed"
            : item.WorkOrderMaterial.IssuedQty > 0 ? "partially_issued" : "shortage";

        if (issue.Status != "draft" && issue.Items.All(candidate => candidate.ShortageQty == 0))
            issue.Status = "issued";

        await _db.SaveChangesAsync();
        if (transaction != null)
            await transaction.CommitAsync();
        return (await GetIssueAsync(issue.Id), "ok");
    }

    public async Task<(MaterialIssueDto? dto, string status)> CancelDraftAsync(Guid id)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)
            : null;
        var lockedIssue = await LockIssueAsync(id);
        if (lockedIssue == null)
            return (null, "not_found");
        var issue = await _db.MaterialIssues
            .Include(candidate => candidate.Items)
                .ThenInclude(item => item.Allocations)
            .FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (issue == null)
            return (null, "not_found");
        if (issue.Status != "draft")
            return (null, "not_draft");
        if (issue.Items.Any(item => item.Allocations.Count > 0))
            return (null, "draft_has_posted_allocations");

        issue.Status = "cancelled";
        await _db.SaveChangesAsync();
        if (transaction != null)
            await transaction.CommitAsync();
        return (await GetIssueAsync(id), "ok");
    }

    public async Task<(MaterialIssueDto? dto, string status)> ReverseAsync(Guid id, Guid reversedBy)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)
            : null;
        var lockedIssue = await LockIssueAsync(id);
        if (lockedIssue == null)
            return (null, "not_found");
        var issue = await GetIssueQuery()
            .FirstOrDefaultAsync(candidate => candidate.Id == id);
        if (issue == null)
            return (null, "not_found");
        if (issue.Status is not ("issued" or "partially_issued"))
            return (null, "not_reversible");
        if (await _db.MaterialIssues.AnyAsync(candidate => candidate.ReversalOfIssueId == issue.Id))
            return (null, "already_reversed");
        if (issue.Items.All(item => item.Allocations.Count == 0))
            return (null, "nothing_to_reverse");

        var skuIds = issue.Items.SelectMany(item => item.Allocations)
            .Select(allocation => allocation.IssueItem.SkuId)
            .Distinct()
            .OrderBy(value => value)
            .ToList();
        var skus = new Dictionary<Guid, Sku>();
        foreach (var skuId in skuIds)
        {
            var sku = await LockSkuAsync(skuId);
            if (sku == null)
                return (null, "sku_not_found");
            skus[skuId] = sku;
        }

        var reversal = new MaterialIssue
        {
            IssueNumber = DocumentNumberGenerator.GenerateMaterialIssue(),
            WorkOrderId = issue.WorkOrderId,
            ReversalOfIssueId = issue.Id,
            IssueWarehouseId = issue.IssueWarehouseId,
            BomVersion = issue.BomVersion,
            Notes = $"沖銷領料單 {issue.IssueNumber}",
            Status = "reversal",
            IssuedBy = reversedBy == Guid.Empty ? null : reversedBy,
            IssuedAt = DateTime.UtcNow
        };

        foreach (var item in issue.Items)
        {
            var reversalItem = new MaterialIssueItem
            {
                SkuId = item.SkuId,
                WorkOrderMaterialId = item.WorkOrderMaterialId,
                Unit = item.Unit,
                RequiredQty = item.IssuedQty,
                IssuedQty = item.IssuedQty,
                Status = "reversed"
            };
            foreach (var allocation in item.Allocations)
            {
                var sku = skus[item.SkuId];
                var inventoryTransaction = new InventoryTransaction
                {
                    TrxId = DocumentNumberGenerator.GenerateShort("REV"),
                    SkuId = sku.Id,
                    Type = "work_order_issue_reversal",
                    QtyChange = allocation.IssuedQty,
                    QtyBefore = sku.Qty,
                    QtyAfter = sku.Qty + allocation.IssuedQty,
                    LotNumber = allocation.LotNumber,
                    RelatedDocType = "MaterialIssueReversal",
                    RelatedDocId = reversal.Id,
                    ReversalOfTransactionId = allocation.InventoryTransactionId,
                    WarehouseId = allocation.WarehouseId,
                    BinLocationId = allocation.BinLocationId,
                    HandlerId = reversedBy == Guid.Empty ? null : reversedBy,
                    UnitCost = allocation.UnitCost,
                    TotalCost = allocation.IssuedQty * allocation.UnitCost,
                    Notes = $"沖銷領料單 {issue.IssueNumber}"
                };
                _db.InventoryTransactions.Add(inventoryTransaction);
                reversalItem.Allocations.Add(new MaterialIssueItemAllocation
                {
                    WarehouseId = allocation.WarehouseId,
                    BinLocationId = allocation.BinLocationId,
                    LotNumber = allocation.LotNumber,
                    IssuedQty = allocation.IssuedQty,
                    UnitCost = allocation.UnitCost,
                    InventoryTransaction = inventoryTransaction
                });
                sku.Qty += allocation.IssuedQty;
                sku.UpdatedAt = DateTime.UtcNow;
                await _costing.RecordMaterialActualAsync(
                    issue.WorkOrderId,
                    sku.Id,
                    -allocation.IssuedQty,
                    allocation.UnitCost,
                    allocation.LotNumber,
                    inventoryTransaction.Id);
            }
            reversal.Items.Add(reversalItem);

            if (item.WorkOrderMaterial != null)
            {
                var reversedWorkOrderQty = await ConvertQtyAsync(
                    item.SkuId,
                    item.IssuedQty,
                    item.Unit,
                    item.WorkOrderMaterial.Unit ?? item.Sku?.Unit);
                item.WorkOrderMaterial.IssuedQty = Math.Max(
                    0,
                    item.WorkOrderMaterial.IssuedQty - reversedWorkOrderQty);
                item.WorkOrderMaterial.ShortageQty = Math.Max(
                    0,
                    item.WorkOrderMaterial.RequiredQty
                    - item.WorkOrderMaterial.IssuedQty
                    - item.WorkOrderMaterial.ClosedShortageQty);
                item.WorkOrderMaterial.Status = item.WorkOrderMaterial.ShortageQty == 0
                    ? "completed"
                    : item.WorkOrderMaterial.IssuedQty > 0 ? "partially_issued" : "shortage";
            }
        }

        issue.Status = "reversed";
        _db.MaterialIssues.Add(reversal);
        await _db.SaveChangesAsync();
        if (transaction != null)
            await transaction.CommitAsync();
        return (await GetIssueAsync(reversal.Id), "ok");
    }

    public async Task<(object? result, string status)> SubmitIssueAsync(MaterialIssueRequest request)
    {
        if (request.Quantity <= 0)
            return (null, "invalid_quantity");
        var workOrder = await _db.WorkOrders
            .Include(order => order.Materials)
                .ThenInclude(material => material.Sku)
            .FirstOrDefaultAsync(order => order.WoNumber == request.WoNumber);
        if (workOrder == null)
            return (null, "not_found_wo");
        var sku = await _db.Skus.FirstOrDefaultAsync(candidate => candidate.SkuNumber == request.SkuCode);
        if (sku == null)
            return (null, "not_found_sku");
        if (!sku.DefaultWarehouseId.HasValue)
            return (null, "default_warehouse_required");

        var (draft, draftStatus) = await CreateDraftAsync(workOrder.Id);
        if (draftStatus != "ok" || draft == null)
            return (null, draftStatus);
        var draftItem = draft.Items.FirstOrDefault(item => item.SkuId == sku.Id);
        if (draftItem == null)
            return (null, "no_open_material_demand");
        var (posted, status) = await PostAsync(draft.Id, new PostMaterialIssueRequestDto
        {
            Allocations = new List<MaterialIssueAllocationRequestDto>
            {
                new()
                {
                    IssueItemId = draftItem.Id,
                    WarehouseId = sku.DefaultWarehouseId.Value,
                    BinLocationId = sku.DefaultBinLocationId,
                    LotNumber = request.LotNumber,
                    IssuedQty = request.Quantity
                }
            }
        }, Guid.Empty);

        return status == "ok"
            ? (new { Message = "領料已過帳", IssueNumber = posted!.IssueNumber, Sku = sku.SkuNumber, Quantity = request.Quantity }, "ok")
            : (new { Message = status }, status);
    }

    private async Task<decimal> GetLocationBalanceAsync(
        Guid skuId,
        Guid warehouseId,
        Guid? binLocationId,
        string? lotNumber)
    {
        var query = _db.InventoryTransactions.Where(transaction =>
            transaction.SkuId == skuId
            && transaction.WarehouseId == warehouseId
            && transaction.BinLocationId == binLocationId);
        if (lotNumber != null)
            query = query.Where(transaction => transaction.LotNumber == lotNumber);
        return await query.SumAsync(transaction => (decimal?)transaction.QtyChange) ?? 0m;
    }

    private async Task<decimal> GetMaterialIssueUnitCostAsync(Sku sku, string? lotNumber)
    {
        if (!string.IsNullOrWhiteSpace(lotNumber))
        {
            var normalizedLotNumber = lotNumber.Trim();
            var receiptTransactions = await _db.InventoryTransactions
                .Where(transaction =>
                    transaction.SkuId == sku.Id &&
                    transaction.Type == "purchase_receipt" &&
                    transaction.QtyChange > 0 &&
                    transaction.LotNumber == normalizedLotNumber &&
                    transaction.UnitCost.HasValue &&
                    !_db.InventoryTransactions.Any(reversal =>
                        reversal.ReversalOfTransactionId == transaction.Id))
                .Select(transaction => new
                {
                    transaction.QtyChange,
                    transaction.UnitCost,
                    transaction.TotalCost
                })
                .ToListAsync();

            var receivedQuantity = receiptTransactions.Sum(transaction => transaction.QtyChange);
            if (receivedQuantity > 0)
            {
                var totalCost = receiptTransactions.Sum(transaction =>
                    transaction.TotalCost ?? transaction.QtyChange * transaction.UnitCost!.Value);
                return Math.Round(totalCost / receivedQuantity, 4, MidpointRounding.AwayFromZero);
            }
        }

        return sku.LatestPurchaseCost > 0 ? sku.LatestPurchaseCost : sku.StandardCost;
    }

    private async Task<Sku?> LockSkuAsync(Guid skuId)
    {
        if (_db.Database.IsRelational()
            && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var sku = await _db.Skus
                .FromSqlInterpolated($"SELECT * FROM \"Skus\" WHERE \"Id\" = {skuId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (sku != null)
                await _db.Entry(sku).ReloadAsync();
            return sku;
        }

        return await _db.Skus.FirstOrDefaultAsync(sku => sku.Id == skuId);
    }

    private async Task<MaterialIssue?> LockIssueAsync(Guid issueId)
    {
        if (_db.Database.IsRelational()
            && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var issue = await _db.MaterialIssues
                .FromSqlInterpolated($"SELECT * FROM \"MaterialIssues\" WHERE \"Id\" = {issueId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (issue != null)
                await _db.Entry(issue).ReloadAsync();
            return issue;
        }

        return await _db.MaterialIssues.FirstOrDefaultAsync(issue => issue.Id == issueId);
    }

    private async Task<WorkOrder?> LockWorkOrderAsync(Guid workOrderId)
    {
        if (_db.Database.IsRelational()
            && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var workOrder = await _db.WorkOrders
                .FromSqlInterpolated($"SELECT * FROM \"WorkOrders\" WHERE \"Id\" = {workOrderId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (workOrder != null)
                await _db.Entry(workOrder).ReloadAsync();
            return workOrder;
        }

        return await _db.WorkOrders.FirstOrDefaultAsync(order => order.Id == workOrderId);
    }

    private async Task<FqcInspectionLot?> LockFqcInspectionLotAsync(Guid lotId)
    {
        if (_db.Database.IsRelational()
            && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var lot = await _db.FqcInspectionLots
                .FromSqlInterpolated($"SELECT * FROM \"FqcInspectionLots\" WHERE \"Id\" = {lotId} FOR UPDATE")
                .SingleOrDefaultAsync();
            if (lot != null)
                await _db.Entry(lot).ReloadAsync();
            return lot;
        }

        return await _db.FqcInspectionLots.FirstOrDefaultAsync(lot => lot.Id == lotId);
    }

    private async Task<decimal> GetFqcLotActualUnitCostAsync(FqcInspectionLot lot)
    {
        var outputQty = await _db.WorkOrders
            .Where(order => order.Id == lot.WorkOrderId)
            .Select(order => order.ActualQty)
            .SingleOrDefaultAsync();
        if (outputQty <= 0)
            throw new InvalidOperationException("返修發料缺少來源工單的實際完工數，無法計算 FQC 批次實際單位成本");

        var materialCost = await _db.WorkOrderMaterialActuals
            .Where(actual => actual.WorkOrderId == lot.WorkOrderId)
            .SumAsync(actual => (decimal?)actual.TotalAmount) ?? 0m;
        var laborCost = await _db.WorkOrderLaborActuals
            .Where(actual => actual.WorkOrderId == lot.WorkOrderId)
            .SumAsync(actual => (decimal?)actual.TotalAmount) ?? 0m;
        var machineCost = await _db.WorkOrderMachineActuals
            .Where(actual => actual.WorkOrderId == lot.WorkOrderId)
            .SumAsync(actual => (decimal?)actual.TotalAmount + actual.OverheadAmount) ?? 0m;
        var totalCost = materialCost + laborCost + machineCost;
        if (totalCost <= 0)
            throw new InvalidOperationException("返修發料缺少來源工單實際成本，無法計算 FQC 批次實際單位成本");

        return Math.Round(totalCost / outputQty, 4);
    }

    private async Task<decimal> ConvertQtyAsync(
        Guid skuId,
        decimal quantity,
        string? fromUnit,
        string? toUnit)
    {
        if (string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit))
            throw new InvalidOperationException($"缺少 SKU {skuId} 的單位，無法建立領料數量快照");
        if (string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
            return quantity;
        return (await _uomConversionService.ConvertAsync(skuId, quantity, fromUnit, toUnit)).ConvertedQty;
    }

    private IQueryable<MaterialIssue> GetIssueQuery() => _db.MaterialIssues
        .Include(issue => issue.Items)
            .ThenInclude(item => item.Sku)
        .Include(issue => issue.Items)
            .ThenInclude(item => item.WorkOrderMaterial)
        .Include(issue => issue.Items)
            .ThenInclude(item => item.Allocations)
                .ThenInclude(allocation => allocation.Warehouse)
        .Include(issue => issue.Items)
            .ThenInclude(item => item.Allocations)
                .ThenInclude(allocation => allocation.BinLocation)
        .Include(issue => issue.WorkOrder)
        .Include(issue => issue.IssueWarehouse);

    private static string GetItemStatus(MaterialIssueItem item)
    {
        if (item.IssuedQty + item.ClosedShortageQty >= item.RequiredQty)
            return item.ClosedShortageQty > 0 ? "shortage_closed" : "completed";
        if (item.IssuedQty > 0)
            return "partially_issued";
        return "shortage";
    }

    private async Task<MaterialIssueDto> MapToDtoAsync(MaterialIssue issue)
    {
        var items = new List<MaterialIssueItemDto>(issue.Items.Count);
        foreach (var item in issue.Items)
        {
            var inventoryUnit = item.Sku?.Unit ?? item.Unit;
            var requirementUnit = item.Unit ?? inventoryUnit;
            var defaultIssueQty = item.Sku == null || item.ShortageQty <= 0
                ? 0
                : Math.Min(
                    await ConvertQtyAsync(item.SkuId, item.ShortageQty, requirementUnit, inventoryUnit),
                    Math.Max(0, item.Sku.Qty));
            items.Add(new MaterialIssueItemDto
            {
                Id = item.Id,
                SkuId = item.SkuId,
                SkuNumber = item.Sku?.SkuNumber,
                SkuName = item.Sku?.SkuName,
                WorkOrderMaterialId = item.WorkOrderMaterialId,
                Unit = item.Unit,
                InventoryUnit = inventoryUnit,
                RequiredQty = item.RequiredQty,
                IssuedQty = item.IssuedQty,
                ShortageQty = item.ShortageQty,
                ClosedShortageQty = item.ClosedShortageQty,
                Status = item.Status,
                ShortageClosureReason = item.ShortageClosureReason,
                DefaultWarehouseId = item.Sku?.DefaultWarehouseId,
                DefaultBinLocationId = item.Sku?.DefaultBinLocationId,
                LotControlled = item.Sku?.LotControlled ?? false,
                SerialControlled = item.Sku?.SerialControlled ?? false,
                OnHandQty = item.Sku?.Qty ?? 0,
                DefaultIssueQty = defaultIssueQty,
                Allocations = item.Allocations.Select(allocation => new MaterialIssueAllocationDto
                {
                    Id = allocation.Id,
                    WarehouseId = allocation.WarehouseId,
                    WarehouseCode = allocation.Warehouse?.WarehouseCode,
                    WarehouseName = allocation.Warehouse?.WarehouseName,
                    BinLocationId = allocation.BinLocationId,
                    BinLocationCode = allocation.BinLocation?.LocationCode,
                    LotNumber = allocation.LotNumber,
                    IssuedQty = allocation.IssuedQty,
                    UnitCost = allocation.UnitCost,
                    InventoryTransactionId = allocation.InventoryTransactionId
                }).ToList()
            });
        }

        return new MaterialIssueDto
        {
            Id = issue.Id,
            IssueNumber = issue.IssueNumber,
            WorkOrderId = issue.WorkOrderId,
            WorkOrderNumber = issue.WorkOrder?.WoNumber,
            IssueWarehouseId = issue.IssueWarehouseId,
            WarehouseCode = issue.IssueWarehouse?.WarehouseCode,
            WarehouseName = issue.IssueWarehouse?.WarehouseName,
            BomVersion = issue.BomVersion,
            Status = issue.Status,
            CreatedAt = issue.CreatedAt,
            IssuedAt = issue.IssuedAt,
            ReversalOfIssueId = issue.ReversalOfIssueId,
            Notes = issue.Notes,
            Items = items
        };
    }
}
