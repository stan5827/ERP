using System.Data;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;
using ErpBackend.Models.Sales;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.Inventory;
using ErpBackend.Helpers;

namespace ErpBackend.Services;

public interface IWorkOrderService
{
    Task<List<WorkOrderDto>> GetAllAsync(string? status = null, int page = 1, int pageSize = 20, int? totalCount = null);
    Task<WorkOrderDto?> GetByIdAsync(Guid id);
    Task<(WorkOrderDto? dto, string status)> CreateAsync(CreateWorkOrderRequest req);
    Task<List<WorkOrderTransferCandidateDto>> GetSalesOrderWorkOrderTransferCandidatesAsync();
    Task<(List<WorkOrderDto> workOrders, string status)> CreateFromSalesOrderAsync(Guid salesOrderId);
    Task<(bool found, string status, CreateSupplyOrdersResultDto? result)> CreateSupplyOrdersAsync(Guid id);
    Task<(bool found, string status, int operationCount)> ExplodeRoutingAsync(Guid id);
    Task<(bool found, string status)> ReorderOperationsAsync(Guid id, List<Guid> operationIds);
    Task<(bool found, string status)> UpdateAsync(Guid id, UpdateWorkOrderRequest req);
    Task<string> RecalculateMaterialsAsync(IReadOnlyCollection<Guid> workOrderIds, decimal plannedQty);
    Task<(bool found, string status)> ReleaseAsync(Guid id);
    Task<(bool found, string status)> StartAsync(Guid id);
    Task<(bool found, string status)> DeleteAsync(Guid id);
}

public class WorkOrderService : IWorkOrderService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IUomConversionService _uomConversionService;

    public WorkOrderService(ErpGeneralDbContext db, IUomConversionService? uomConversionService = null)
    {
        _db = db;
        _uomConversionService = uomConversionService ?? new UomConversionService(db);
    }

    public async Task<List<WorkOrderDto>> GetAllAsync(string? status = null, int page = 1, int pageSize = 20, int? totalCount = null)
    {
        var query = _db.WorkOrders.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(w => w.Status == status);

        var items = await query
            .AsNoTracking()
            .Include(workOrder => workOrder.Materials)
                .ThenInclude(material => material.Sku)
                    .ThenInclude(sku => sku!.ProductionUom)
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var skus = await _db.Skus
            .Include(sku => sku.ProductionUom)
            .ToDictionaryAsync(sku => sku.Id);
        var salesOrderIds = items
            .Where(workOrder => workOrder.SalesOrderId.HasValue)
            .Select(workOrder => workOrder.SalesOrderId.GetValueOrDefault())
            .Distinct()
            .ToList();
        var salesOrderNumbers = await _db.SalesOrders
            .Where(order => salesOrderIds.Contains(order.Id))
            .ToDictionaryAsync(order => order.Id, order => order.OrderNumber);

        return items.Select(w => new WorkOrderDto
        {
            Id = w.Id,
            WoNumber = w.WoNumber,
            ProductSkuId = w.ProductSkuId,
            ProductSkuName = skus.GetValueOrDefault(w.ProductSkuId)?.SkuName,
            SalesOrderId = w.SalesOrderId,
            SalesOrderNumber = w.SalesOrderId.HasValue
                ? salesOrderNumbers.GetValueOrDefault(w.SalesOrderId.Value)
                : null,
            BomId = w.BomId,
            BomVersion = w.BomVersion,
            ProductionUomCode = w.ProductionUomCode,
            PlannedQty = w.PlannedQty,
            ProductionQty = w.PlannedQty,
            ActualQty = w.ActualQty,
            Status = w.Status,
            Priority = w.Priority,
            PlannedStartDate = w.PlannedStartDate,
            PlannedEndDate = w.PlannedEndDate,
            CreatedAt = w.CreatedAt,
            Materials = w.Materials
                .Select(MapMaterialSnapshotToDto)
                .ToList()
        }).ToList();
    }

    public async Task<WorkOrderDto?> GetByIdAsync(Guid id)
    {
        var w = await _db.WorkOrders
            .AsNoTracking()
            .Include(workOrder => workOrder.Materials)
                .ThenInclude(material => material.Sku)
                    .ThenInclude(sku => sku!.ProductionUom)
            .Include(workOrder => workOrder.ProductSku)
                .ThenInclude(sku => sku!.ProductionUom)
            .Include(workOrder => workOrder.Operations)
                .ThenInclude(operation => operation.Operation)
            .Include(workOrder => workOrder.Operations)
                .ThenInclude(operation => operation.WorkCenter)
            .FirstOrDefaultAsync(workOrder => workOrder.Id == id);
        if (w == null) return null;

        var sku = w.ProductSku;
        var salesOrderNumber = w.SalesOrderId.HasValue
            ? await _db.SalesOrders
                .Where(order => order.Id == w.SalesOrderId.Value)
                .Select(order => order.OrderNumber)
                .FirstOrDefaultAsync()
            : null;
        var availableBySku = await GetAvailableInventoryBySkuAsync(
            w.Materials.Select(material => material.SkuId).Distinct().ToList());
        var bomUnits = new Dictionary<Guid, string>();
        if (w.BomId.HasValue)
        {
            var bomItems = await _db.BomItems
                .Where(item => item.BomId == w.BomId.Value)
                .Select(item => new { item.ChildSkuId, item.Unit })
                .ToListAsync();
            bomUnits = bomItems
                .GroupBy(item => item.ChildSkuId)
                .ToDictionary(group => group.Key, group => group.First().Unit);
        }

        var dto = new WorkOrderDto
        {
            Id = w.Id,
            WoNumber = w.WoNumber,
            ProductSkuId = w.ProductSkuId,
            ProductSkuName = sku?.SkuName,
            SalesOrderId = w.SalesOrderId,
            SalesOrderNumber = salesOrderNumber,
            BomId = w.BomId,
            BomVersion = w.BomVersion,
            ProductionUomCode = w.ProductionUomCode,
            PlannedQty = w.PlannedQty,
            ProductionQty = w.PlannedQty,
            ActualQty = w.ActualQty,
            Status = w.Status,
            Priority = w.Priority,
            PlannedStartDate = w.PlannedStartDate,
            PlannedEndDate = w.PlannedEndDate,
            ActualStartDate = w.ActualStartDate,
            ActualEndDate = w.ActualEndDate,
            CreatedAt = w.CreatedAt,
            Operations = w.Operations
                .OrderBy(operation => operation.OperationSeq)
                .Select(operation => new WorkOrderOperationDto
                {
                    Id = operation.Id,
                    OperationSeq = operation.OperationSeq,
                    OperationId = operation.OperationId,
                    OperationName = operation.Operation?.OperationName,
                    WorkCenterId = operation.WorkCenterId,
                    WorkCenterName = operation.WorkCenter?.WorkCenterName,
                    PlannedQty = operation.PlannedQty,
                    CompletedQty = operation.CompletedQty,
                    ScrapQty = operation.ScrapQty,
                    Status = operation.Status
                })
                .ToList(),
            Materials = new List<WorkOrderMaterialDto>()
        };
        return await MapWorkOrderMaterialsAsync(w, availableBySku, bomUnits, dto);
    }

    private async Task<WorkOrderDto> MapWorkOrderMaterialsAsync(
        WorkOrder workOrder,
        Dictionary<Guid, decimal> availableBySku,
        Dictionary<Guid, string> bomUnits,
        WorkOrderDto dto)
    {
        var materials = new List<WorkOrderMaterialDto>();
        foreach (var material in workOrder.Materials)
        {
            var bomUnit = bomUnits.GetValueOrDefault(material.SkuId, material.Sku?.Unit ?? string.Empty);
            var inventoryUnit = material.Sku?.Unit ?? bomUnit;
            var availableQty = availableBySku.GetValueOrDefault(material.SkuId);
            var requiredInventoryQty = await ConvertMaterialQtyAsync(
                material.SkuId,
                material.RequiredQty,
                bomUnit,
                inventoryUnit);
            materials.Add(MapMaterialToDto(
                material,
                availableQty,
                bomUnit,
                Math.Max(0, requiredInventoryQty - availableQty)));
        }

        dto.Materials = materials;
        return dto;
    }

    public async Task<(WorkOrderDto? dto, string status)> CreateAsync(CreateWorkOrderRequest req)
    {
        if (req == null) return (null, "invalid");

        var sku = await _db.Skus
            .AsNoTracking()
            .Include(item => item.ProductionUom)
            .FirstOrDefaultAsync(item => item.Id == req.ProductSkuId);
        if (sku == null) return (null, "not_found");
        var productionUnit = GetProductionUomCode(sku);
        if (string.IsNullOrWhiteSpace(productionUnit))
            return (null, "production_uom_not_configured");

        var bom = await GetActiveBomAsync(req.ProductSkuId);
        var wo = new WorkOrder
        {
            WoNumber = DocumentNumberGenerator.GenerateWorkOrder(),
            ProductSkuId = req.ProductSkuId,
            BomId = bom?.Id,
            BomVersion = bom?.Version,
            ProductionUomCode = productionUnit,
            PlannedQty = req.PlannedQty,
            Priority = req.Priority ?? "normal",
            PlannedStartDate = req.PlannedStartDate,
            PlannedEndDate = req.PlannedEndDate,
            Status = "planned"
        };

        if (bom != null)
        {
            var populateStatus = await PopulateWorkOrderMaterialsAsync(
                wo,
                bom,
                req.PlannedQty,
                new HashSet<Guid> { req.ProductSkuId },
                0);
            if (populateStatus != "ok")
                return (null, populateStatus);
        }

        _db.WorkOrders.Add(wo);
        await _db.SaveChangesAsync();

        var dto = new WorkOrderDto
        {
            Id = wo.Id,
            WoNumber = wo.WoNumber,
            ProductSkuId = wo.ProductSkuId,
            ProductSkuName = sku.SkuName,
            ProductionUomCode = productionUnit,
            PlannedQty = wo.PlannedQty,
            ProductionQty = wo.PlannedQty,
            Status = wo.Status,
            Priority = wo.Priority,
            PlannedStartDate = wo.PlannedStartDate,
            PlannedEndDate = wo.PlannedEndDate,
            CreatedAt = wo.CreatedAt
        };

        return (dto, "ok");
    }

    public async Task<(bool found, string status, int operationCount)> ExplodeRoutingAsync(Guid id)
    {
        var workOrder = await _db.WorkOrders
            .Include(order => order.Operations)
            .FirstOrDefaultAsync(order => order.Id == id);
        if (workOrder == null) return (false, "not_found", 0);
        if (workOrder.Operations.Count > 0) return (true, "already_exists", 0);

        var routing = await _db.Routings
            .Include(route => route.Operations)
                .ThenInclude(operation => operation.Operation)
            .Include(route => route.Operations)
                .ThenInclude(operation => operation.WorkCenter)
            .Where(route => route.ProductSkuId == workOrder.ProductSkuId && route.IsActive)
            .OrderByDescending(route => route.Version)
            .FirstOrDefaultAsync();
        if (routing == null) return (true, "routing_not_found", 0);
        if (routing.Operations.Count == 0) return (true, "routing_empty", 0);
        if (routing.Operations.Any(operation => operation.Operation?.IsActive != true
            || operation.WorkCenter?.IsActive != true))
            return (true, "routing_invalid", 0);

        foreach (var routeOperation in routing.Operations.OrderBy(operation => operation.OperationSeq))
        {
            _db.WorkOrderOperations.Add(new WorkOrderOperation
            {
                WorkOrderId = workOrder.Id,
                OperationSeq = routeOperation.OperationSeq,
                OperationId = routeOperation.OperationId,
                WorkCenterId = routeOperation.WorkCenterId,
                PlannedQty = workOrder.PlannedQty,
                Status = "pending"
            });
        }

        var count = routing.Operations.Count;
        await _db.SaveChangesAsync();
        return (true, "ok", count);
    }

    public async Task<(bool found, string status, CreateSupplyOrdersResultDto? result)> CreateSupplyOrdersAsync(Guid id)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync()
            : null;

        var workOrder = await _db.WorkOrders
            .Include(order => order.Materials)
                .ThenInclude(material => material.Sku)
                    .ThenInclude(sku => sku!.ProductionUom)
            .Include(order => order.Materials)
                .ThenInclude(material => material.Sku)
                    .ThenInclude(sku => sku!.PurchaseUom)
            .FirstOrDefaultAsync(order => order.Id == id);
        if (workOrder == null) return (false, "not_found", null);
        if (string.IsNullOrWhiteSpace(workOrder.ProductionUomCode))
            return (true, "work_order_production_unit_unverified", null);

        var context = new SupplyOrderContext();
        var result = await CreateSupplyOrdersForWorkOrderAsync(
            workOrder,
            context,
            new HashSet<Guid> { workOrder.ProductSkuId },
            0);
        if (result != "ok")
            return (true, result, null);

        await _db.SaveChangesAsync();
        if (transaction != null)
            await transaction.CommitAsync();

        return (true, "ok", new CreateSupplyOrdersResultDto
        {
            PrCount = context.PrCount,
            SubWoCount = context.SubWoCount
        });
    }

    private async Task<string> CreateSupplyOrdersForWorkOrderAsync(
        WorkOrder workOrder,
        SupplyOrderContext context,
        HashSet<Guid> bomPath,
        int depth)
    {
        const int maximumBomDepth = 20;
        if (depth > maximumBomDepth)
            return "bom_depth_exceeded";

        if (workOrder.Materials.Count == 0)
        {
            var bom = await GetBomForWorkOrderAsync(workOrder);
            if (bom == null)
                return "bom_not_found";

            var populateStatus = await PopulateWorkOrderMaterialsAsync(
                workOrder,
                bom,
                workOrder.PlannedQty,
                new HashSet<Guid> { workOrder.ProductSkuId },
                0);
            if (populateStatus != "ok")
                return populateStatus;
        }

        var bomUnitsBySku = new Dictionary<Guid, string>();
        if (workOrder.BomId.HasValue)
        {
            var workOrderBomItems = await _db.BomItems
                .Where(item => item.BomId == workOrder.BomId.Value)
                .Select(item => new { item.ChildSkuId, item.Unit })
                .ToListAsync();
            bomUnitsBySku = workOrderBomItems
                .GroupBy(item => item.ChildSkuId)
                .ToDictionary(group => group.Key, group => group.First().Unit);
        }

        var purchaseItems = new List<PurchaseRequisitionItem>();
        foreach (var material in workOrder.Materials)
        {
            if (material.Status == "ordered")
                continue;

            var componentSku = material.Sku
                ?? await _db.Skus
                    .Include(sku => sku.ProductionUom)
                    .Include(sku => sku.PurchaseUom)
                    .FirstOrDefaultAsync(sku => sku.Id == material.SkuId);
            if (componentSku == null)
                return "bom_component_sku_not_found";
            material.Sku = componentSku;

            var requiredUnit = bomUnitsBySku.GetValueOrDefault(material.SkuId, componentSku.Unit ?? string.Empty);
            var inventoryUnit = componentSku.Unit ?? requiredUnit;
            var requiredInventoryQty = await ConvertMaterialQtyAsync(
                material.SkuId,
                material.RequiredQty,
                requiredUnit,
                inventoryUnit);
            var availableQty = await GetRemainingAvailableQtyAsync(material.SkuId, context.RemainingAvailableBySku);
            var coveredQty = Math.Min(requiredInventoryQty, availableQty);
            context.RemainingAvailableBySku[material.SkuId] = availableQty - coveredQty;
            var shortageInventoryQty = Math.Max(0, requiredInventoryQty - coveredQty);
            var shortageQty = await ConvertMaterialQtyAsync(
                material.SkuId,
                shortageInventoryQty,
                inventoryUnit,
                requiredUnit);
            material.ShortageQty = shortageQty;

            if (shortageInventoryQty == 0)
            {
                material.Status = "pending";
                continue;
            }

            var childBom = await GetActiveBomAsync(componentSku.Id);
            var isManufacturable = componentSku.IsManufacturable
                || childBom != null
                || IsMakeSource(componentSku.SourceType)
                || IsMakeSource(componentSku.MakeOrBuy);
            if (isManufacturable)
            {
                if (childBom == null)
                    return "bom_not_found";
                if (bomPath.Contains(componentSku.Id))
                    return "bom_cycle";

                var marker = $"由工單 {workOrder.WoNumber} 自動轉單補貨 [AUTO_SUPPLY_PARENT_MATERIAL:{material.Id}]";
                var childWorkOrder = await _db.WorkOrders
                    .Include(order => order.Materials)
                        .ThenInclude(childMaterial => childMaterial.Sku)
                            .ThenInclude(sku => sku!.ProductionUom)
                    .Include(order => order.Materials)
                        .ThenInclude(childMaterial => childMaterial.Sku)
                            .ThenInclude(sku => sku!.PurchaseUom)
                    .FirstOrDefaultAsync(order => order.Notes == marker);
                var productionUnit = childWorkOrder?.ProductionUomCode
                    ?? GetProductionUomCode(componentSku);
                if (string.IsNullOrWhiteSpace(productionUnit))
                    return $"production_uom_not_configured:{componentSku.SkuNumber}";

                if (childWorkOrder == null)
                {
                    var productionQty = await ConvertMaterialQtyAsync(
                        material.SkuId,
                        shortageInventoryQty,
                        inventoryUnit,
                        productionUnit);
                    childWorkOrder = new WorkOrder
                    {
                        WoNumber = DocumentNumberGenerator.GenerateWorkOrder(),
                        WoType = "regular",
                        ProductSkuId = componentSku.Id,
                        ProductSku = componentSku,
                        SalesOrderId = workOrder.SalesOrderId,
                        BomId = childBom.Id,
                        BomVersion = childBom.Version,
                        ProductionUomCode = productionUnit,
                        PlannedQty = productionQty,
                        PlannedStartDate = workOrder.PlannedStartDate,
                        PlannedEndDate = workOrder.PlannedEndDate,
                        Status = "planned",
                        Priority = workOrder.Priority,
                        Notes = marker
                    };
                    _db.WorkOrders.Add(childWorkOrder);
                    context.SubWoCount++;
                }
                else if (string.IsNullOrWhiteSpace(childWorkOrder.ProductionUomCode))
                {
                    return $"work_order_production_unit_unverified:{childWorkOrder.WoNumber}";
                }

                material.Status = "ordered";
                var childPath = new HashSet<Guid>(bomPath) { componentSku.Id };
                var childStatus = await CreateSupplyOrdersForWorkOrderAsync(
                    childWorkOrder,
                    context,
                    childPath,
                    depth + 1);
                if (childStatus != "ok")
                    return childStatus;
            }
            else if (componentSku.IsPurchasable
                || IsBuySource(componentSku.SourceType)
                || IsBuySource(componentSku.MakeOrBuy))
            {
                var purchaseUnit = componentSku.PurchaseUom?.UomCode
                    ?? componentSku.PurchaseUomCode
                    ?? componentSku.PurchaseUnit
                    ?? componentSku.Unit;
                var purchaseQty = await ConvertMaterialQtyAsync(
                    material.SkuId,
                    shortageInventoryQty,
                    inventoryUnit,
                    purchaseUnit);
                purchaseItems.Add(new PurchaseRequisitionItem
                {
                    Id = Guid.NewGuid(),
                    SkuId = componentSku.Id,
                    Sku = componentSku,
                    Quantity = purchaseQty,
                    Unit = purchaseUnit,
                    EstimatedUnitPrice = componentSku.PurchasePrice,
                    Currency = "TWD",
                    RequiredDate = workOrder.PlannedStartDate,
                    Notes = $"工單 {workOrder.WoNumber} 自動補貨"
                });
                material.Status = "ordered";
            }
            else
            {
                return $"sku_not_purchasable_or_manufacturable:{componentSku.SkuNumber}";
            }
        }

        if (purchaseItems.Count > 0)
        {
            var marker = $"工單 {workOrder.WoNumber} 自動補貨 [AUTO_SUPPLY:{workOrder.Id}]";
            var existingRequisition = await _db.PurchaseRequisitions
                .AnyAsync(requisition => requisition.Purpose == marker);
            if (!existingRequisition)
            {
                var requisition = new PurchaseRequisition
                {
                    PrNumber = DocumentNumberGenerator.GeneratePurchaseRequisition(),
                    PrDate = DateTime.UtcNow,
                    WorkOrderId = workOrder.Id,
                    Purpose = marker,
                    Priority = workOrder.Priority,
                    Status = "DRAFT",
                    WorkflowStatus = "DRAFT",
                    Items = purchaseItems
                };
                requisition.TotalEstimatedAmount = purchaseItems.Sum(item => item.Quantity * item.EstimatedUnitPrice);
                _db.PurchaseRequisitions.Add(requisition);
                context.PrCount++;
            }
        }

        return "ok";
    }

    private static bool IsMakeSource(string? source)
        => source?.Trim().ToLowerInvariant() is "make" or "self_made" or "make_or_buy" or "make_or_outsource";

    private static bool IsBuySource(string? source)
        => source?.Trim().ToLowerInvariant() is "buy" or "purchased" or "make_or_buy";

    private async Task<string> PopulateWorkOrderMaterialsAsync(
        WorkOrder workOrder,
        Bom bom,
        decimal plannedQty,
        HashSet<Guid> bomPath,
        int depth)
    {
        const int maximumBomDepth = 20;
        if (depth > maximumBomDepth)
            return "bom_depth_exceeded";

        var now = DateTime.UtcNow;
        var bomItems = await _db.BomItems
            .Where(item => item.BomId == bom.Id
                && (!item.EffectiveDate.HasValue || item.EffectiveDate.Value <= now)
                && (!item.ExpiryDate.HasValue || item.ExpiryDate.Value >= now))
            .ToListAsync();

        foreach (var bomItem in bomItems)
        {
            var componentSku = await _db.Skus
                .Include(sku => sku.ProductionUom)
                .Include(sku => sku.PurchaseUom)
                .FirstOrDefaultAsync(sku => sku.Id == bomItem.ChildSkuId);
            if (componentSku == null)
                return "bom_component_sku_not_found";

            var requiredQty = bomItem.Quantity
                * (1 + bomItem.ScrapRate / 100m)
                * plannedQty;
            if (bomItem.IsPhantom)
            {
                if (bomPath.Contains(componentSku.Id))
                    return "bom_cycle";
                var phantomBom = await GetActiveBomAsync(componentSku.Id);
                if (phantomBom == null)
                    return "bom_not_found";

                var phantomPath = new HashSet<Guid>(bomPath) { componentSku.Id };
                var phantomStatus = await PopulateWorkOrderMaterialsAsync(
                    workOrder,
                    phantomBom,
                    requiredQty,
                    phantomPath,
                    depth + 1);
                if (phantomStatus != "ok")
                    return phantomStatus;
                continue;
            }

            workOrder.Materials.Add(new WorkOrderMaterial
            {
                WorkOrderId = workOrder.Id,
                SkuId = componentSku.Id,
                Sku = componentSku,
                BomStandardQty = bomItem.Quantity,
                RequiredQty = requiredQty,
                Unit = bomItem.Unit,
                Status = "pending"
            });
        }

        return "ok";
    }

    private Task<Bom?> GetBomForWorkOrderAsync(WorkOrder workOrder)
    {
        if (workOrder.BomId.HasValue)
            return _db.Boms.Include(bom => bom.Items)
                .FirstOrDefaultAsync(bom => bom.Id == workOrder.BomId.Value);
        return GetActiveBomAsync(workOrder.ProductSkuId);
    }

    private async Task<Bom?> GetActiveBomAsync(Guid skuId)
    {
        var now = DateTime.UtcNow;
        return await _db.Boms
            .Include(bom => bom.Items)
            .Where(bom => bom.ProductSkuId == skuId
                && bom.Status == "active"
                && bom.EffectiveDate <= now
                && (!bom.ExpiryDate.HasValue || bom.ExpiryDate.Value >= now))
            .OrderByDescending(bom => bom.EffectiveDate)
            .ThenByDescending(bom => bom.Version)
            .FirstOrDefaultAsync();
    }

    private async Task<decimal> GetRemainingAvailableQtyAsync(
        Guid skuId,
        Dictionary<Guid, decimal> remainingAvailableBySku)
    {
        if (!remainingAvailableBySku.TryGetValue(skuId, out var availableQty))
        {
            var availability = await GetAvailableInventoryBySkuAsync(new List<Guid> { skuId });
            availableQty = availability.GetValueOrDefault(skuId);
            remainingAvailableBySku[skuId] = availableQty;
        }

        return availableQty;
    }

    private sealed class SupplyOrderContext
    {
        public Dictionary<Guid, decimal> RemainingAvailableBySku { get; } = new();
        public int PrCount { get; set; }
        public int SubWoCount { get; set; }
    }

    public async Task<List<WorkOrderTransferCandidateDto>> GetSalesOrderWorkOrderTransferCandidatesAsync()
    {
        var now = DateTime.UtcNow;
        return await _db.SalesOrders
            .AsNoTracking()
            .Where(order =>
                (order.Status == "confirmed" || order.Status == "partial")
                && order.ManufacturingStatus != "sent"
                && order.Items.Any(item =>
                    !item.NeedOutsourcing
                    && (item.Sku == null || !item.Sku.IsOutsourced)
                    && (item.NeedManufacturing
                        || (item.Sku != null && item.Sku.IsManufacturable)
                        || item.BomId.HasValue
                        || item.RoutingId.HasValue
                        || _db.Boms.Any(bom =>
                            bom.ProductSkuId == (item.SkuId ?? item.ProductId)
                            && bom.Status == "active"
                            && bom.EffectiveDate <= now
                            && (bom.ExpiryDate == null || bom.ExpiryDate >= now)))
                    && !_db.SalesOrderProductionLinks.Any(link =>
                        link.SalesOrderItemId == item.Id
                        && link.ProductionDocType == "work_order")))
            .OrderBy(order => order.OrderDate)
            .ThenBy(order => order.OrderNumber)
            .Select(order => new WorkOrderTransferCandidateDto
            {
                SalesOrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.Customer.CustomerName,
                OrderDate = order.OrderDate,
                ExpectedDeliveryDate = order.ExpectedDeliveryDate,
                OrderRemarks = order.OrderRemarks,
                Items = order.Items
                    .OrderBy(item => item.LineNumber)
                    .Select(item => new WorkOrderTransferCandidateItemDto
                    {
                        LineNumber = item.LineNumber,
                        SkuNumber = item.Sku != null ? item.Sku.SkuNumber : item.Product != null ? item.Product.SkuNumber : null,
                        SkuName = item.Sku != null ? item.Sku.SkuName : item.Product != null ? item.Product.SkuName : null,
                        Specification = item.Sku != null ? item.Sku.Specification : item.Product != null ? item.Product.Specification : null,
                        CustomerItemCode = item.CustomerItemCode,
                        Quantity = item.Quantity,
                        Unit = item.Sku != null
                            ? item.Sku.SalesUomCode ?? item.Sku.Unit
                            : item.Product != null ? item.Product.SalesUomCode ?? item.Product.Unit : null,
                        NeedManufacturing = item.NeedManufacturing,
                        NeedOutsourcing = item.NeedOutsourcing,
                        IsOutsourced = item.Sku != null && item.Sku.IsOutsourced,
                        IsAlreadyTransferred = _db.SalesOrderProductionLinks.Any(link =>
                            link.SalesOrderItemId == item.Id
                            && link.ProductionDocType == "work_order"),
                        WillCreateWorkOrder = !item.NeedOutsourcing
                            && (item.Sku == null || !item.Sku.IsOutsourced)
                            && (item.NeedManufacturing
                                || (item.Sku != null && item.Sku.IsManufacturable)
                                || item.BomId.HasValue
                                || item.RoutingId.HasValue
                                || _db.Boms.Any(bom =>
                                    bom.ProductSkuId == (item.SkuId ?? item.ProductId)
                                    && bom.Status == "active"
                                    && bom.EffectiveDate <= now
                                    && (bom.ExpiryDate == null || bom.ExpiryDate >= now)))
                            && !_db.SalesOrderProductionLinks.Any(link =>
                                link.SalesOrderItemId == item.Id
                                && link.ProductionDocType == "work_order"),
                        PlannedStartDate = item.PlannedStartDate,
                        PlannedFinishDate = item.PlannedFinishDate,
                        LineRemarks = item.LineRemarks
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    public async Task<(List<WorkOrderDto> workOrders, string status)> CreateFromSalesOrderAsync(Guid salesOrderId)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync()
            : null;

        var order = await _db.SalesOrders
            .Include(salesOrder => salesOrder.Items)
                .ThenInclude(item => item.Sku)
            .FirstOrDefaultAsync(salesOrder => salesOrder.Id == salesOrderId);
        if (order == null) return (new List<WorkOrderDto>(), "order_not_found");
        if (order.Status != "confirmed" && order.Status != "partial")
            return (new List<WorkOrderDto>(), "order_not_confirmed");

        var now = DateTime.UtcNow;
        var nonOutsourcedItems = order.Items
            .Where(item => !item.NeedOutsourcing && item.Sku?.IsOutsourced != true)
            .ToList();
        var orderSkuIds = nonOutsourcedItems
            .Select(item => item.SkuId ?? item.ProductId)
            .Distinct()
            .ToList();
        var boms = await _db.Boms
            .Include(bom => bom.Items)
            .Where(bom => orderSkuIds.Contains(bom.ProductSkuId)
                && bom.Status == "active"
                && bom.EffectiveDate <= now
                && (bom.ExpiryDate == null || bom.ExpiryDate >= now))
            .ToListAsync();
        var manufacturingItems = nonOutsourcedItems
            .Where(item => item.NeedManufacturing
                || item.Sku?.IsManufacturable == true
                || item.BomId.HasValue
                || item.RoutingId.HasValue
                || boms.Any(bom => bom.ProductSkuId == (item.SkuId ?? item.ProductId)))
            .ToList();
        if (manufacturingItems.Count == 0)
            return (new List<WorkOrderDto>(), "no_manufacturing_items");

        var existingLinks = await _db.SalesOrderProductionLinks
            .Where(link => manufacturingItems.Select(item => item.Id).Contains(link.SalesOrderItemId)
                && link.ProductionDocType == "work_order")
            .ToListAsync();
        var existingWorkOrderIds = existingLinks.Select(link => link.ProductionDocId).ToList();
        var existingWorkOrders = await _db.WorkOrders
            .Where(workOrder => existingWorkOrderIds.Contains(workOrder.Id))
            .ToListAsync();
        var existingByLine = existingLinks
            .Join(existingWorkOrders,
                link => link.ProductionDocId,
                workOrder => workOrder.Id,
                (link, workOrder) => new { link.SalesOrderItemId, WorkOrder = workOrder })
            .ToDictionary(entry => entry.SalesOrderItemId, entry => entry.WorkOrder);

        var pendingItems = manufacturingItems
            .Where(item => !existingByLine.ContainsKey(item.Id))
            .ToList();
        if (pendingItems.Count == 0)
            return (existingWorkOrders
                .Select(workOrder => MapToDto(workOrder, order.OrderNumber))
                .ToList(), "already_created");

        var productSkuIds = pendingItems
            .Select(item => item.SkuId ?? item.ProductId)
            .Distinct()
            .ToList();
        var productSkus = await _db.Skus
            .Include(sku => sku.SalesUom)
            .Include(sku => sku.ProductionUom)
            .Where(sku => productSkuIds.Contains(sku.Id))
            .ToDictionaryAsync(sku => sku.Id);
        var missingSkuLine = pendingItems.FirstOrDefault(item =>
            !productSkus.ContainsKey(item.SkuId ?? item.ProductId));
        if (missingSkuLine != null)
            return (new List<WorkOrderDto>(), $"sku_not_found:{missingSkuLine.LineNumber}");

        var routings = await _db.Routings
            .Include(routing => routing.Operations)
                .ThenInclude(operation => operation.Operation)
            .Include(routing => routing.Operations)
                .ThenInclude(operation => operation.WorkCenter)
            .Where(routing => productSkuIds.Contains(routing.ProductSkuId) && routing.IsActive)
            .ToListAsync();

        var configurations = new List<(
            SalesOrderItem Item,
            Guid SkuId,
            Bom Bom,
            Routing Routing,
            DateTime Start,
            DateTime End,
            decimal ProductionQty,
            string ProductionUomCode)>();
        foreach (var item in pendingItems)
        {
            var skuId = item.SkuId ?? item.ProductId;
            var bom = item.BomId.HasValue
                ? boms.FirstOrDefault(candidate => candidate.Id == item.BomId.Value && candidate.ProductSkuId == skuId)
                : null;
            bom ??= boms
                .Where(candidate => candidate.ProductSkuId == skuId)
                .OrderByDescending(candidate => candidate.EffectiveDate)
                .ThenByDescending(candidate => candidate.Version)
                .FirstOrDefault();
            if (bom == null)
                return (new List<WorkOrderDto>(), $"bom_not_found:{item.LineNumber}");

            var routing = item.RoutingId.HasValue
                ? routings.FirstOrDefault(candidate => candidate.Id == item.RoutingId.Value && candidate.ProductSkuId == skuId)
                : null;
            routing ??= routings.FirstOrDefault(candidate => candidate.ProductSkuId == skuId);
            if (routing == null || routing.Operations.Count == 0)
                return (new List<WorkOrderDto>(), $"routing_not_found:{item.LineNumber}");
            if (routing.Operations.Any(operation => operation.Operation?.IsActive != true
                || operation.WorkCenter?.IsActive != true))
                return (new List<WorkOrderDto>(), $"routing_invalid:{item.LineNumber}");

            var start = item.PlannedStartDate ?? now.Date;
            var end = item.PlannedFinishDate
                ?? order.ConfirmedDeliveryDate
                ?? order.ExpectedDeliveryDate
                ?? start.AddDays(7);
            if (end < start)
                return (new List<WorkOrderDto>(), $"invalid_schedule:{item.LineNumber}");

            var sku = productSkus[skuId];
            var salesUnit = GetSalesUomCode(sku);
            if (string.IsNullOrWhiteSpace(salesUnit))
                return (new List<WorkOrderDto>(), $"sales_uom_not_configured:{item.LineNumber}");
            var productionUnit = GetProductionUomCode(sku);
            if (string.IsNullOrWhiteSpace(productionUnit))
                return (new List<WorkOrderDto>(), $"production_uom_not_configured:{item.LineNumber}");
            var productionQty = await ConvertMaterialQtyAsync(
                skuId,
                item.Quantity,
                salesUnit,
                productionUnit);

            configurations.Add((item, skuId, bom, routing, start, end, productionQty, productionUnit));
        }

        var componentSkuIds = configurations
            .SelectMany(configuration => configuration.Bom.Items)
            .Select(item => item.ChildSkuId)
            .Distinct()
            .ToList();
        var remainingAvailableBySku = await GetAvailableInventoryBySkuAsync(componentSkuIds);
        var componentSkus = await _db.Skus
            .Include(sku => sku.ProductionUom)
            .Where(sku => componentSkuIds.Contains(sku.Id))
            .ToDictionaryAsync(sku => sku.Id);
        foreach (var configuration in configurations)
        {
            if (configuration.Bom.Items.Any(item => !componentSkus.ContainsKey(item.ChildSkuId)))
                return (new List<WorkOrderDto>(), $"bom_component_sku_not_found:{configuration.Item.LineNumber}");
        }
        var availableAtCreationByMaterialId = new Dictionary<Guid, decimal>();
        var bomUnitByMaterialId = new Dictionary<Guid, string>();
        var createdWorkOrders = new List<WorkOrder>();
        foreach (var configuration in configurations)
        {
            var workOrder = new WorkOrder
            {
                WoNumber = DocumentNumberGenerator.GenerateWorkOrder(),
                WoType = "regular",
                ProductSkuId = configuration.SkuId,
                ProductSku = productSkus[configuration.SkuId],
                BomId = configuration.Bom.Id,
                BomVersion = configuration.Bom.Version,
                SalesOrderId = order.Id,
                ProductionUomCode = configuration.ProductionUomCode,
                PlannedQty = configuration.ProductionQty,
                PlannedStartDate = configuration.Start,
                PlannedEndDate = configuration.End,
                Status = "planned",
                Priority = "normal"
            };

            _db.WorkOrders.Add(workOrder);
            createdWorkOrders.Add(workOrder);

            foreach (var bomItem in configuration.Bom.Items)
            {
                var requiredQty = bomItem.Quantity
                    * (1 + bomItem.ScrapRate / 100m)
                    * configuration.ProductionQty;
                var availableQty = remainingAvailableBySku.GetValueOrDefault(bomItem.ChildSkuId);
                var componentSku = componentSkus[bomItem.ChildSkuId];
                var inventoryUnit = componentSku.Unit ?? bomItem.Unit;
                var requiredInventoryQty = await ConvertMaterialQtyAsync(
                    bomItem.ChildSkuId,
                    requiredQty,
                    bomItem.Unit,
                    inventoryUnit);
                var shortageInventoryQty = Math.Max(0, requiredInventoryQty - availableQty);
                var shortageQty = await ConvertMaterialQtyAsync(
                    bomItem.ChildSkuId,
                    shortageInventoryQty,
                    inventoryUnit,
                    bomItem.Unit);
                var material = new WorkOrderMaterial
                {
                    WorkOrderId = workOrder.Id,
                    SkuId = bomItem.ChildSkuId,
                    BomStandardQty = bomItem.Quantity,
                    RequiredQty = requiredQty,
                    ShortageQty = shortageQty,
                    Unit = bomItem.Unit,
                    Status = shortageInventoryQty > 0 ? "shortage" : "pending",
                    Sku = componentSku
                };
                workOrder.Materials.Add(material);
                availableAtCreationByMaterialId[material.Id] = availableQty;
                bomUnitByMaterialId[material.Id] = bomItem.Unit;
                remainingAvailableBySku[bomItem.ChildSkuId] = Math.Max(0, availableQty - requiredInventoryQty);
            }

            foreach (var routeOperation in configuration.Routing.Operations.OrderBy(operation => operation.OperationSeq))
            {
                _db.WorkOrderOperations.Add(new WorkOrderOperation
                {
                    WorkOrderId = workOrder.Id,
                    OperationSeq = routeOperation.OperationSeq,
                    OperationId = routeOperation.OperationId,
                    WorkCenterId = routeOperation.WorkCenterId,
                    PlannedQty = configuration.ProductionQty,
                    Status = "pending"
                });
            }

            _db.SalesOrderProductionLinks.Add(new SalesOrderProductionLink
            {
                SalesOrderItemId = configuration.Item.Id,
                ProductionDocType = "work_order",
                ProductionDocId = workOrder.Id,
                ProductionDocNo = workOrder.WoNumber,
                LinkedQty = configuration.Item.Quantity
            });
            configuration.Item.NeedManufacturing = true;
            configuration.Item.ProductionStatus = "sent";
        }

        order.ManufacturingStatus = "sent";
        order.UpdatedAt = now;
        await _db.SaveChangesAsync();
        if (transaction != null)
            await transaction.CommitAsync();

        var createdDtos = createdWorkOrders.Select(workOrder =>
        {
            var dto = MapToDto(workOrder, order.OrderNumber);
            dto.Materials = workOrder.Materials
                .Select(material => MapMaterialSnapshotToDto(
                    material,
                    availableAtCreationByMaterialId[material.Id],
                    bomUnitByMaterialId[material.Id]))
                .ToList();
            return dto;
        });
        return (existingWorkOrders
            .Select(workOrder => MapToDto(workOrder, order.OrderNumber))
            .Concat(createdDtos)
            .ToList(), "ok");
    }

    public async Task<(bool found, string status)> ReorderOperationsAsync(Guid id, List<Guid> operationIds)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync()
            : null;

        var workOrder = await _db.WorkOrders
            .Include(order => order.Operations)
            .FirstOrDefaultAsync(order => order.Id == id);
        if (workOrder == null)
            return (false, "not_found");
        if (operationIds.Count != workOrder.Operations.Count
            || operationIds.Distinct().Count() != operationIds.Count
            || !operationIds.ToHashSet().SetEquals(workOrder.Operations.Select(operation => operation.Id)))
            return (true, "invalid_order");

        var hasReports = await _db.WipReports.AnyAsync(report => report.WorkOrderId == id);
        if (hasReports)
            return (true, "has_reports");

        var operationsById = workOrder.Operations.ToDictionary(operation => operation.Id);
        for (var index = 0; index < operationIds.Count; index++)
            operationsById[operationIds[index]].OperationSeq = -(index + 1);

        await _db.SaveChangesAsync();

        for (var index = 0; index < operationIds.Count; index++)
            operationsById[operationIds[index]].OperationSeq = index + 1;

        await _db.SaveChangesAsync();
        if (transaction != null)
            await transaction.CommitAsync();

        return (true, "ok");
    }

    public async Task<(bool found, string status)> UpdateAsync(Guid id, UpdateWorkOrderRequest req)
    {
        var wo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id);
        if (wo == null) return (false, "not_found");

        if (req.ProductSkuId is Guid productSkuId && productSkuId != wo.ProductSkuId)
        {
            var sku = await _db.Skus
                .Include(item => item.ProductionUom)
                .FirstOrDefaultAsync(item => item.Id == productSkuId);
            if (sku == null)
                return (true, "sku_not_found");

            var productionUnit = GetProductionUomCode(sku);
            if (string.IsNullOrWhiteSpace(productionUnit))
                return (true, "production_uom_not_configured");

            wo.ProductSkuId = productSkuId;
            wo.ProductionUomCode = productionUnit;
        }
        if (req.PlannedQty != null) wo.PlannedQty = req.PlannedQty.Value;
        if (req.Priority != null) wo.Priority = req.Priority;
        if (req.PlannedStartDate != null) wo.PlannedStartDate = req.PlannedStartDate;
        if (req.PlannedEndDate != null) wo.PlannedEndDate = req.PlannedEndDate;
        wo.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    public async Task<string> RecalculateMaterialsAsync(
        IReadOnlyCollection<Guid> workOrderIds,
        decimal plannedQty)
    {
        if (plannedQty <= 0)
            return "invalid_planned_qty";

        if (workOrderIds == null || workOrderIds.Count is 0 or > 25)
            return "invalid_work_order_ids";
        var ids = workOrderIds.Distinct().OrderBy(id => id).ToList();
        if (ids.Count != workOrderIds.Count)
            return "invalid_work_order_ids";

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        var workOrders = new List<WorkOrder>(ids.Count);
        foreach (var id in ids)
        {
            var lockedOrder = await LockWorkOrderAsync(id);
            if (lockedOrder == null)
                return "not_found";

            var order = await _db.WorkOrders
                .Include(candidate => candidate.Materials)
                .Include(candidate => candidate.Operations)
                .FirstOrDefaultAsync(candidate => candidate.Id == id);
            if (order == null)
                return "not_found";
            workOrders.Add(order);
        }

        var issueIds = await _db.MaterialIssues
            .Where(issue => ids.Contains(issue.WorkOrderId))
            .Select(issue => issue.Id)
            .OrderBy(id => id)
            .ToListAsync();
        foreach (var issueId in issueIds)
            await LockIssueAsync(issueId);

        var issues = await _db.MaterialIssues
            .Where(issue => ids.Contains(issue.WorkOrderId))
            .Include(issue => issue.Items)
                .ThenInclude(item => item.Allocations)
            .ToListAsync();
        var materialUpdates = new Dictionary<Guid, List<(WorkOrderMaterial Current, WorkOrderMaterial Recalculated)>>();

        foreach (var workOrder in workOrders)
        {
            if (workOrder.Status is not ("planned" or "pending" or "released"))
                return "work_order_not_editable";
            if (workOrder.ActualQty != 0
                || workOrder.ReceivedQty != 0
                || workOrder.ScrapQty != 0
                || workOrder.Operations.Any(operation =>
                    operation.CompletedQty != 0 || operation.ScrapQty != 0))
                return "work_order_has_progress";
            if (await _db.WipReports.AnyAsync(report => report.WorkOrderId == workOrder.Id))
                return "work_order_has_progress";
            if (workOrder.Materials.Any(material =>
                    material.IssuedQty != 0
                    || material.ReturnedQty != 0
                    || material.ClosedShortageQty != 0
                    || material.FqcInspectionLotId.HasValue))
                return "material_issue_history";

            var workOrderIssues = issues.Where(issue => issue.WorkOrderId == workOrder.Id).ToList();
            if (workOrderIssues.Any(issue =>
                    issue.Status is not ("draft" or "cancelled")))
                return "material_issue_history";
            if (workOrderIssues.Any(issue => issue.Status == "draft"
                    && issue.Items.Any(item =>
                        item.Allocations.Count > 0 || item.IssuedQty != 0)))
                return "draft_has_posted_allocations";

            var bom = await GetBomForWorkOrderAsync(workOrder);
            if (bom == null)
                return "bom_not_found";

            var recalculatedOrder = new WorkOrder
            {
                ProductSkuId = workOrder.ProductSkuId,
                BomId = workOrder.BomId,
                PlannedQty = plannedQty
            };
            var populateStatus = await PopulateWorkOrderMaterialsAsync(
                recalculatedOrder,
                bom,
                plannedQty,
                new HashSet<Guid> { workOrder.ProductSkuId },
                0);
            if (populateStatus != "ok")
                return populateStatus;
            if (recalculatedOrder.Materials.All(material => material.RequiredQty <= 0))
                return "no_open_material_demand";
            if (!TryBuildMaterialSnapshotUpdates(
                    workOrder.Materials,
                    recalculatedOrder.Materials,
                    out var updates))
                return "bom_composition_changed";
            materialUpdates.Add(workOrder.Id, updates);
        }

        foreach (var workOrder in workOrders)
        {
            workOrder.PlannedQty = plannedQty;
            workOrder.UpdatedAt = DateTime.UtcNow;
            foreach (var operation in workOrder.Operations)
                operation.PlannedQty = plannedQty;
            foreach (var (current, recalculated) in materialUpdates[workOrder.Id])
            {
                current.BomStandardQty = recalculated.BomStandardQty;
                current.RequiredQty = recalculated.RequiredQty;
                current.Unit = recalculated.Unit;
                current.Status = "pending";
            }

            foreach (var issue in issues.Where(candidate =>
                         candidate.WorkOrderId == workOrder.Id && candidate.Status == "draft"))
                issue.Status = "cancelled";

            var newDraft = new MaterialIssue
            {
                IssueNumber = DocumentNumberGenerator.GenerateMaterialIssue(),
                WorkOrderId = workOrder.Id,
                BomVersion = workOrder.BomVersion,
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                Items = workOrder.Materials
                    .Where(material => material.RequiredQty > 0)
                    .Select(material => new MaterialIssueItem
                    {
                        SkuId = material.SkuId,
                        WorkOrderMaterialId = material.Id,
                        Unit = material.Unit,
                        RequiredQty = material.RequiredQty,
                        ShortageQty = material.RequiredQty,
                        Status = "pending"
                    })
                    .ToList()
            };
            _db.MaterialIssues.Add(newDraft);
        }

        await _db.SaveChangesAsync();
        if (transaction != null)
            await transaction.CommitAsync();
        return "ok";
    }

    private static bool TryBuildMaterialSnapshotUpdates(
        ICollection<WorkOrderMaterial> currentMaterials,
        ICollection<WorkOrderMaterial> recalculatedMaterials,
        out List<(WorkOrderMaterial Current, WorkOrderMaterial Recalculated)> updates)
    {
        updates = new List<(WorkOrderMaterial, WorkOrderMaterial)>();
        var currentGroups = currentMaterials
            .GroupBy(material => (material.SkuId, Unit: NormalizeUnit(material.Unit)))
            .ToDictionary(group => group.Key, group => group.OrderBy(material => material.BomStandardQty).ToList());
        var recalculatedGroups = recalculatedMaterials
            .GroupBy(material => (material.SkuId, Unit: NormalizeUnit(material.Unit)))
            .ToDictionary(group => group.Key, group => group.OrderBy(material => material.BomStandardQty).ToList());
        if (currentGroups.Count != recalculatedGroups.Count
            || currentGroups.Any(group => !recalculatedGroups.ContainsKey(group.Key)
                || recalculatedGroups[group.Key].Count != group.Value.Count))
            return false;

        foreach (var (key, currentGroup) in currentGroups)
        {
            var recalculatedGroup = recalculatedGroups[key];
            for (var index = 0; index < currentGroup.Count; index++)
                updates.Add((currentGroup[index], recalculatedGroup[index]));
        }

        return true;
    }

    private static string NormalizeUnit(string? unit) => unit?.Trim().ToUpperInvariant() ?? string.Empty;

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

    public async Task<(bool found, string status)> ReleaseAsync(Guid id)
    {
        var workOrder = await _db.WorkOrders.FirstOrDefaultAsync(order => order.Id == id);
        if (workOrder == null)
            return (false, "not_found");
        if (workOrder.Status is not ("pending" or "planned"))
            return (true, "invalid_transition");

        workOrder.Status = "released";
        workOrder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    public async Task<(bool found, string status)> StartAsync(Guid id)
    {
        var workOrder = await _db.WorkOrders.FirstOrDefaultAsync(order => order.Id == id);
        if (workOrder == null)
            return (false, "not_found");
        if (workOrder.Status != "released")
            return (true, "invalid_transition");

        workOrder.Status = "in_progress";
        workOrder.ActualStartDate ??= DateTime.UtcNow;
        workOrder.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    public async Task<(bool found, string status)> DeleteAsync(Guid id)
    {
        var wo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == id);
        if (wo == null) return (false, "not_found");

        if (wo.Status != "planned")
            return (true, "guarded");

        _db.WorkOrders.Remove(wo);
        await _db.SaveChangesAsync();
        return (true, "ok");
    }

    private static WorkOrderDto MapToDto(WorkOrder workOrder, string? salesOrderNumber = null) => new()
    {
        Id = workOrder.Id,
        WoNumber = workOrder.WoNumber,
        ProductSkuId = workOrder.ProductSkuId,
        SalesOrderId = workOrder.SalesOrderId,
        SalesOrderNumber = salesOrderNumber,
        BomId = workOrder.BomId,
        BomVersion = workOrder.BomVersion,
        ProductionUomCode = workOrder.ProductionUomCode,
        PlannedQty = workOrder.PlannedQty,
        ProductionQty = workOrder.PlannedQty,
        ActualQty = workOrder.ActualQty,
        Status = workOrder.Status,
        Priority = workOrder.Priority,
        PlannedStartDate = workOrder.PlannedStartDate,
        PlannedEndDate = workOrder.PlannedEndDate,
        CreatedAt = workOrder.CreatedAt,
        Materials = workOrder.Materials
            .Select(material => MapMaterialSnapshotToDto(material))
            .ToList()
    };

    private static WorkOrderMaterialDto MapMaterialSnapshotToDto(WorkOrderMaterial material) =>
        MapMaterialSnapshotToDto(material, Math.Max(0, material.RequiredQty - material.ShortageQty));

    private static WorkOrderMaterialDto MapMaterialSnapshotToDto(
        WorkOrderMaterial material,
        decimal availableQty,
        string? bomUnit = null) => new()
    {
        Id = material.Id,
        SkuId = material.SkuId,
        SkuNumber = material.Sku?.SkuNumber,
        SkuName = material.Sku?.SkuName,
        Unit = bomUnit ?? material.Unit ?? material.Sku?.Unit,
        InventoryUnit = material.Sku?.Unit,
        BomStandardQty = material.BomStandardQty,
        RequiredQty = material.RequiredQty,
        IssuedQty = material.IssuedQty,
        UnissuedQty = Math.Max(0, material.RequiredQty - material.IssuedQty - material.ClosedShortageQty),
        ClosedShortageQty = material.ClosedShortageQty,
        OnHandQty = material.Sku?.Qty ?? 0,
        AvailableQty = availableQty,
        ShortageQty = material.ShortageQty,
        Status = material.Status
    };

    private async Task<Dictionary<Guid, decimal>> GetAvailableInventoryBySkuAsync(List<Guid> skuIds)
    {
        if (skuIds.Count == 0)
            return new Dictionary<Guid, decimal>();

        var onHandBySku = await _db.Skus
            .Where(sku => skuIds.Contains(sku.Id))
            .Select(sku => new { sku.Id, sku.Qty })
            .ToDictionaryAsync(sku => sku.Id, sku => sku.Qty);
        var reservations = await _db.InventoryReservations
            .Where(reservation => skuIds.Contains(reservation.SkuId) && reservation.Status == "reserved")
            .Select(reservation => new { reservation.SkuId, reservation.Qty })
            .ToListAsync();
        var reservedBySku = reservations
            .GroupBy(reservation => reservation.SkuId)
            .ToDictionary(group => group.Key, group => group.Sum(reservation => reservation.Qty));
        var quarantinedReports = await _db.IqcReports
            .Where(report => report.SkuId.HasValue
                && skuIds.Contains(report.SkuId.Value)
                && report.GoodsReceiptItemId != null
                && report.Result != "pass"
                && report.Result != "not_required")
            .Select(report => new { SkuId = report.SkuId!.Value, report.ReceivedQty })
            .ToListAsync();
        var quarantinedBySku = quarantinedReports
            .GroupBy(report => report.SkuId)
            .ToDictionary(group => group.Key, group => group.Sum(report => report.ReceivedQty));

        return onHandBySku.ToDictionary(
            entry => entry.Key,
            entry => Math.Max(0, entry.Value
                - reservedBySku.GetValueOrDefault(entry.Key)
                - quarantinedBySku.GetValueOrDefault(entry.Key)));
    }

    private async Task<decimal> ConvertMaterialQtyAsync(
        Guid skuId,
        decimal quantity,
        string? fromUnit,
        string? toUnit)
    {
        if (string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit))
            throw new InvalidOperationException($"UOM is required to compare inventory for SKU {skuId}.");

        if (string.Equals(fromUnit, toUnit, StringComparison.OrdinalIgnoreCase))
            return quantity;

        var conversion = await _uomConversionService.ConvertAsync(skuId, quantity, fromUnit, toUnit);
        return conversion.ConvertedQty;
    }

    private static string? GetProductionUomCode(Sku sku) =>
        sku.ProductionUom?.UomCode ?? sku.ProductionUomCode;

    private static string? GetSalesUomCode(Sku sku) =>
        sku.SalesUom?.UomCode ?? sku.SalesUomCode;

    private static WorkOrderMaterialDto MapMaterialToDto(
        WorkOrderMaterial material,
        decimal availableQty,
        string? bomUnit,
        decimal shortageQty)
    {
        return new WorkOrderMaterialDto
        {
            Id = material.Id,
            SkuId = material.SkuId,
            SkuNumber = material.Sku?.SkuNumber,
            SkuName = material.Sku?.SkuName,
            Unit = bomUnit ?? material.Sku?.Unit,
            InventoryUnit = material.Sku?.Unit,
            BomStandardQty = material.BomStandardQty,
            RequiredQty = material.RequiredQty,
            IssuedQty = material.IssuedQty,
            UnissuedQty = Math.Max(0, material.RequiredQty - material.IssuedQty - material.ClosedShortageQty),
            ClosedShortageQty = material.ClosedShortageQty,
            OnHandQty = material.Sku?.Qty ?? 0,
            AvailableQty = availableQty,
            ShortageQty = shortageQty,
            Status = material.Status == "ordered"
                ? "ordered"
                : shortageQty > 0 ? "shortage" : "pending"
        };
    }
}
