using System.Data;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Helpers;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.QA;
using ErpBackend.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface IFqcWorkflowService
{
    Task<List<FqcWorkOrderOptionDto>> GetWorkOrderOptionsAsync(
        string? search, CancellationToken cancellationToken = default);
    Task<FqcFinishedLotOptionDto?> GetDefaultFinishedLotAsync(
        Guid workOrderId, CancellationToken cancellationToken = default);
    Task<List<FqcWarehouseOptionDto>> GetWarehouseOptionsAsync(
        CancellationToken cancellationToken = default);
    Task<List<FqcBinLocationOptionDto>> GetBinLocationOptionsAsync(
        Guid warehouseId, CancellationToken cancellationToken = default);
    Task<List<FqcInspectionLotDto>> GetInspectionLotsAsync(Guid? workOrderId, CancellationToken cancellationToken = default);
    Task<FqcInspectionLotDto> CreateInspectionLotAsync(CreateFqcInspectionLotRequestDto request, CancellationToken cancellationToken = default);
    Task<FqcInspectionLotDto> DecideInspectionLotAsync(Guid id, DecideFqcInspectionLotRequestDto request, CancellationToken cancellationToken = default);
    Task<FqcInspectionLotDto> CreateReworkWorkOrderAsync(Guid id, string dispositionBy, CancellationToken cancellationToken = default);
    Task<FqcScrapNoteDto> CreateScrapNoteAsync(
        Guid id, string reasonCode, string reason, Guid requesterUserId, string requesterName,
        CancellationToken cancellationToken = default);
    Task<FqcScrapNoteDto> ResubmitFqcScrapNoteAsync(
        Guid scrapRecordId, string reasonCode, string reason, Guid requesterUserId, string requesterName,
        CancellationToken cancellationToken = default);
    Task<FqcScrapNoteDto> DecideFqcScrapQualityAsync(
        Guid scrapRecordId, Guid actorUserId, string actorName, bool approve, string? comments,
        CancellationToken cancellationToken = default);
    Task<FqcScrapNoteDto> DecideFqcScrapFinanceAsync(
        Guid scrapRecordId, Guid actorUserId, string actorName, bool approve, string? comments,
        CancellationToken cancellationToken = default);
    Task CloseReworkWorkOrderAsync(
        Guid workOrderId, string closedBy, CancellationToken cancellationToken = default);
    Task<ProductionReceiptDto> CreateProductionReceiptAsync(CreateProductionReceiptRequestDto request, CancellationToken cancellationToken = default);
    Task<List<ProductionReceiptDto>> GetProductionReceiptsAsync(Guid? workOrderId, CancellationToken cancellationToken = default);
}

public class FqcWorkflowService : IFqcWorkflowService
{
    private const string QuarantineLocationCode = "FQC-HOLD";
    private readonly ErpGeneralDbContext _db;
    private readonly IAccountingService _accounting;
    private readonly IWorkOrderCostingService _workOrderCosting;

    public FqcWorkflowService(
        ErpGeneralDbContext db,
        IAccountingService accounting,
        IWorkOrderCostingService workOrderCosting)
    {
        _db = db;
        _accounting = accounting;
        _workOrderCosting = workOrderCosting;
    }

    public async Task<List<FqcWorkOrderOptionDto>> GetWorkOrderOptionsAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _db.WorkOrders
            .AsNoTracking()
            .Include(workOrder => workOrder.ProductSku)
            .AsQueryable();
        var searchTerm = search?.Trim();
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(workOrder =>
                workOrder.WoNumber.Contains(searchTerm)
                || (workOrder.ProductSku != null
                    && (workOrder.ProductSku.SkuNumber.Contains(searchTerm)
                        || workOrder.ProductSku.SkuName.Contains(searchTerm))));
        }

        return await query
            .OrderByDescending(workOrder => workOrder.UpdatedAt)
            .ThenByDescending(workOrder => workOrder.CreatedAt)
            .Take(100)
            .Select(workOrder => new FqcWorkOrderOptionDto
            {
                Id = workOrder.Id,
                WoNumber = workOrder.WoNumber,
                ProductSkuId = workOrder.ProductSkuId,
                ProductSkuNumber = workOrder.ProductSku == null ? null : workOrder.ProductSku.SkuNumber,
                ProductSkuName = workOrder.ProductSku == null ? null : workOrder.ProductSku.SkuName,
                Status = workOrder.Status
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<FqcFinishedLotOptionDto?> GetDefaultFinishedLotAsync(
        Guid workOrderId,
        CancellationToken cancellationToken = default)
    {
        var finalOperationSeq = await _db.WorkOrderOperations
            .AsNoTracking()
            .Where(operation => operation.WorkOrderId == workOrderId)
            .MaxAsync(operation => (int?)operation.OperationSeq, cancellationToken);
        if (!finalOperationSeq.HasValue)
            return null;

        var result = await _db.WipReports
            .AsNoTracking()
            .Where(report => report.WorkOrderId == workOrderId
                && report.OperationSeq == finalOperationSeq.Value
                && report.Status == "approved"
                && report.FinishedLotNumber != null
                && report.FinishedLotNumber != ""
                && !_db.FqcInspectionLots.Any(lot =>
                    lot.WorkOrderId == workOrderId
                    && lot.LotNumber == report.FinishedLotNumber))
            .GroupBy(report => report.FinishedLotNumber!)
            .Select(group => new
            {
                LotNumber = group.Key,
                ProducedQty = group.Sum(report => report.CompletedQty),
                LastReportedAt = group.Max(report => report.CreatedAt)
            })
            .OrderByDescending(lot => lot.LastReportedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (result is null)
            return null;

        return new FqcFinishedLotOptionDto
        {
            LotNumber = result.LotNumber,
            ProducedQty = result.ProducedQty
        };
    }

    public Task<List<FqcWarehouseOptionDto>> GetWarehouseOptionsAsync(
        CancellationToken cancellationToken = default) =>
        _db.Warehouses
            .AsNoTracking()
            .Where(warehouse => warehouse.IsActive)
            .OrderBy(warehouse => warehouse.WarehouseCode)
            .Select(warehouse => new FqcWarehouseOptionDto
            {
                Id = warehouse.Id,
                WarehouseCode = warehouse.WarehouseCode,
                WarehouseName = warehouse.WarehouseName
            })
            .ToListAsync(cancellationToken);

    public Task<List<FqcBinLocationOptionDto>> GetBinLocationOptionsAsync(
        Guid warehouseId,
        CancellationToken cancellationToken = default) =>
        _db.BinLocations
            .AsNoTracking()
            .Where(bin => bin.WarehouseId == warehouseId
                && bin.IsActive
                && bin.LocationCode != QuarantineLocationCode)
            .OrderBy(bin => bin.LocationCode)
            .Select(bin => new FqcBinLocationOptionDto
            {
                Id = bin.Id,
                LocationCode = bin.LocationCode
            })
            .ToListAsync(cancellationToken);

    public async Task<List<FqcInspectionLotDto>> GetInspectionLotsAsync(
        Guid? workOrderId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.FqcInspectionLots
            .AsNoTracking()
            .Include(lot => lot.WorkOrder)
            .Include(lot => lot.ProductSku)
            .Include(lot => lot.ReworkSourceInspectionLot)
            .Include(lot => lot.ScrapRecords)
                .ThenInclude(record => record.ApprovalEvents)
            .AsQueryable();
        if (workOrderId.HasValue)
            query = query.Where(lot => lot.WorkOrderId == workOrderId.Value);

        var lots = await query.OrderByDescending(lot => lot.InspectedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        var ids = lots.Select(lot => lot.Id).ToList();
        var receivedByLot = await _db.ProductionReceiptItems
            .Where(item => ids.Contains(item.FqcInspectionLotId))
            .GroupBy(item => item.FqcInspectionLotId)
            .Select(group => new { LotId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToDictionaryAsync(group => group.LotId, group => group.Quantity, cancellationToken);
        return lots.Select(lot => MapInspectionLot(lot, receivedByLot.GetValueOrDefault(lot.Id))).ToList();
    }

    public async Task<FqcInspectionLotDto> CreateInspectionLotAsync(
        CreateFqcInspectionLotRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var lotNumber = request.LotNumber?.Trim();
        if (request.SampledQty <= 0 || request.DefectiveQty < 0 ||
            request.DefectiveQty > request.SampledQty || string.IsNullOrWhiteSpace(request.InspectorName) ||
            string.IsNullOrWhiteSpace(lotNumber) ||
            ((request.DefectiveQty > 0 || request.CriticalDefectFound) && string.IsNullOrWhiteSpace(request.DefectCode)))
            throw new ArgumentException("FQC 抽樣數、不良數或檢驗人員資料不正確");

        var workOrder = await _db.WorkOrders
            .Include(order => order.ProductSku)
            .Include(order => order.Operations)
            .Include(order => order.Materials)
            .FirstOrDefaultAsync(order => order.Id == request.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到工單");
        if (workOrder.Operations.Count == 0)
            throw new InvalidOperationException("工單尚未建立工序，無法建立 FQC 批次");
        if (await _db.FqcInspectionLots.AnyAsync(
                lot => lot.WorkOrderId == workOrder.Id && lot.LotNumber == lotNumber,
                cancellationToken))
            throw new InvalidOperationException("此工單成品批號已建立 FQC 檢驗批次");

        var finalOperationSeq = workOrder.Operations.Max(operation => operation.OperationSeq);
        var producedQty = await _db.WipReports
            .Where(report => report.WorkOrderId == workOrder.Id
                && report.OperationSeq == finalOperationSeq
                && report.Status == "approved"
                && report.FinishedLotNumber == lotNumber)
            .SumAsync(report => (decimal?)report.CompletedQty, cancellationToken) ?? 0m;
        if (producedQty <= 0)
            throw new InvalidOperationException("此成品批號沒有最後工序的報工良品數，請先完成 SFC 批號報工");
        if (request.SampledQty > producedQty)
            throw new ArgumentException("FQC 抽樣數不可大於此批號的已報工良品數");

        FqcInspectionLot? reworkSourceLot = null;
        if (string.Equals(workOrder.WoType, "rework", StringComparison.OrdinalIgnoreCase))
        {
            var sourceMaterial = workOrder.Materials.SingleOrDefault(material => material.FqcInspectionLotId.HasValue)
                ?? throw new InvalidOperationException("返修工單未連結來源 FQC 批次");
            if (workOrder.Materials.Count(material => material.FqcInspectionLotId.HasValue) != 1)
                throw new InvalidOperationException("返修工單只能連結一個來源 FQC 批次");
            reworkSourceLot = await _db.FqcInspectionLots
                .FirstOrDefaultAsync(lot => lot.Id == sourceMaterial.FqcInspectionLotId, cancellationToken)
                ?? throw new InvalidOperationException("返修來源 FQC 批次不存在");
            if (reworkSourceLot.ReworkWorkOrderId != workOrder.Id
                || reworkSourceLot.QuarantinedQty != 0
                || sourceMaterial.RequiredQty <= 0
                || sourceMaterial.IssuedQty < sourceMaterial.RequiredQty)
                throw new InvalidOperationException("返修隔離品尚未全數領用，不能送交複檢");
            if (!string.Equals(lotNumber, reworkSourceLot.LotNumber, StringComparison.Ordinal))
                throw new InvalidOperationException("返修複檢必須沿用來源成品批號");
            if (producedQty > sourceMaterial.IssuedQty)
                throw new InvalidOperationException("返修複檢數量不可超過來源隔離品已領用數量");
            if (await _db.FqcInspectionLots.AnyAsync(
                    lot => lot.ReworkSourceInspectionLotId == reworkSourceLot.Id,
                    cancellationToken))
                throw new InvalidOperationException("此返修工單已建立複檢批次，不可重複送檢");
            if (!string.Equals(workOrder.Status, "pending_fqc", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("返修工單尚未完成報工，不能送交 FQC 複檢");
        }

        var inspectionLot = new FqcInspectionLot
        {
            InspectionNumber = DocumentNumberGenerator.Generate("FQC"),
            WorkOrderId = workOrder.Id,
            ProductSkuId = workOrder.ProductSkuId,
            LotNumber = lotNumber,
            ExpiryDate = request.ExpiryDate?.Date,
            ProducedQty = producedQty,
            SampledQty = request.SampledQty,
            DefectiveQty = request.DefectiveQty,
            CriticalDefectFound = request.CriticalDefectFound,
            InspectionStatus = "Pending",
            DispositionStatus = "Pending",
            DefectCode = string.IsNullOrWhiteSpace(request.DefectCode) ? null : request.DefectCode.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            InspectorName = request.InspectorName.Trim(),
            InspectedAt = DateTime.UtcNow,
            ReworkSourceInspectionLotId = reworkSourceLot?.Id,
            WorkOrder = workOrder,
            ProductSku = workOrder.ProductSku!
        };
        if (request.DefectiveQty > 0 || request.CriticalDefectFound)
        {
            inspectionLot.Findings.Add(new FqcInspectionFinding
            {
                FindingName = request.CriticalDefectFound ? "重大缺點" : "抽樣不良",
                DefectCode = request.DefectCode!.Trim(),
                DefectQty = request.DefectiveQty,
                IsCritical = request.CriticalDefectFound,
                Notes = request.Notes?.Trim()
            });
        }
        _db.FqcInspectionLots.Add(inspectionLot);
        if (reworkSourceLot is not null)
            reworkSourceLot.DispositionStatus = "ReinspectionPending";
        await _db.SaveChangesAsync(cancellationToken);
        return MapInspectionLot(inspectionLot, 0);
    }

    public async Task<FqcInspectionLotDto> DecideInspectionLotAsync(
        Guid id,
        DecideFqcInspectionLotRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        try
        {
            var lot = await LockInspectionLotAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("找不到 FQC 檢驗批次");
            if (!string.Equals(lot.InspectionStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("FQC 批次已判定，不可重複變更整批判定");
            if (string.IsNullOrWhiteSpace(request.DecisionBy))
                throw new ArgumentException("請填寫整批判定人員");

            var status = request.InspectionStatus.Trim();
            if (status.Equals("passed", StringComparison.OrdinalIgnoreCase))
                status = "Passed";
            else if (status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                     status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
                status = "Failed";
            else if (status.Equals("concession", StringComparison.OrdinalIgnoreCase))
                status = "Concession";
            else
                throw new ArgumentException("FQC 整批判定僅接受 Passed、Failed 或 Concession");
            if (lot.ReworkSourceInspectionLotId.HasValue && status == "Concession")
                throw new InvalidOperationException("返修複檢只接受合格或不合格判定，不適用特採");

            if (lot.CriticalDefectFound && status != "Failed")
                throw new InvalidOperationException("發現重大缺點，整批必須判定 Failed，不得放行或特採");
            if (request.ApprovedQty < 0 || request.ApprovedQty > lot.ProducedQty - lot.DefectiveQty)
                throw new ArgumentException("放行良品數不可超過已報工數扣除不良數後的數量");
            if (status == "Failed" && request.ApprovedQty != 0)
                throw new ArgumentException("不合格批次的放行數必須為 0");
            if (status != "Failed" && request.ApprovedQty <= 0)
                throw new ArgumentException("Passed 或 Concession 批次必須有大於 0 的放行數量");

            var sku = await LockSkuAsync(lot.ProductSkuId, cancellationToken)
                ?? throw new InvalidOperationException("FQC 產品不存在");
            var quarantineQty = status == "Failed"
                ? lot.ProducedQty
                : lot.ProducedQty - request.ApprovedQty;
            if (quarantineQty > 0)
            {
                var expiryDateUtc = NormalizeCalendarDateToUtc(lot.ExpiryDate);
                var warehouse = await _db.Warehouses
                    .FirstOrDefaultAsync(item => item.Id == request.WarehouseId && item.IsActive, cancellationToken)
                    ?? throw new ArgumentException("請指定有效的隔離倉庫");
                var quarantineBin = await GetOrCreateQuarantineBinAsync(warehouse.Id, cancellationToken);
                var balance = await GetBinBalanceAsync(sku.Id, warehouse.Id, quarantineBin.Id, cancellationToken);
                var now = DateTime.UtcNow;
                var transactionRecord = new InventoryTransaction
                {
                    TrxId = DocumentNumberGenerator.GenerateShort("FQH"),
                    SkuId = sku.Id,
                    Type = "fqc_quarantine_in",
                    QtyChange = quarantineQty,
                    QtyBefore = balance,
                    QtyAfter = balance + quarantineQty,
                    LotNumber = lot.LotNumber,
                    RelatedDocType = "FqcInspectionLot",
                    RelatedDocId = lot.Id,
                    WarehouseId = warehouse.Id,
                    BinLocationId = quarantineBin.Id,
                    Notes = $"FQC 隔離 {lot.InspectionNumber}",
                    CreatedAt = now
                };
                _db.InventoryTransactions.Add(transactionRecord);
                sku.Qty += quarantineQty;
                sku.UpdatedAt = now;
                var inventoryLot = await _db.InventoryLots.FirstOrDefaultAsync(
                    item => item.SkuId == sku.Id && item.LotNumber == lot.LotNumber
                        && item.WarehouseId == warehouse.Id && item.BinLocationId == quarantineBin.Id,
                    cancellationToken);
                if (inventoryLot == null)
                {
                    _db.InventoryLots.Add(new InventoryLot
                    {
                        SkuId = sku.Id,
                        LotNumber = lot.LotNumber,
                        Qty = quarantineQty,
                        WarehouseId = warehouse.Id,
                        BinLocationId = quarantineBin.Id,
                        ExpiryDate = expiryDateUtc,
                        Status = "quarantine"
                    });
                }
                else
                {
                    inventoryLot.Qty += quarantineQty;
                    inventoryLot.ExpiryDate = expiryDateUtc ?? inventoryLot.ExpiryDate;
                    inventoryLot.Status = "quarantine";
                }

                lot.QuarantineWarehouseId = warehouse.Id;
                lot.QuarantineBinLocationId = quarantineBin.Id;
                lot.QuarantinedQty = quarantineQty;
                lot.QuarantinedAt = now;
            }

            lot.InspectionStatus = status;
            lot.ApprovedQty = status == "Failed" ? 0 : request.ApprovedQty;
            lot.DispositionStatus = status == "Concession" ? "Concession" :
                quarantineQty > 0 ? "Pending" : "Released";
            lot.DecisionBy = request.DecisionBy.Trim();
            lot.DecidedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request.Notes))
                lot.Notes = string.IsNullOrWhiteSpace(lot.Notes)
                    ? request.Notes.Trim()
                    : $"{lot.Notes}{Environment.NewLine}{request.Notes.Trim()}";

            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return MapInspectionLot(lot, 0);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ProductionReceiptDto> CreateProductionReceiptAsync(
        CreateProductionReceiptRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("完工入庫數量必須大於 0");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            var lot = await LockInspectionLotAsync(request.FqcInspectionLotId, cancellationToken)
                ?? throw new KeyNotFoundException("找不到 FQC 檢驗批次");
            if (lot.InspectionStatus is not ("Passed" or "Concession"))
                throw new InvalidOperationException("此批次尚未通過 FQC 整批放行，禁止入庫");

            var alreadyReceived = await _db.ProductionReceiptItems
                .Where(item => item.FqcInspectionLotId == lot.Id)
                .SumAsync(item => (decimal?)item.Quantity, cancellationToken) ?? 0m;
            if (request.Quantity > lot.ApprovedQty - alreadyReceived)
                throw new InvalidOperationException("入庫數量超過 FQC 放行批次的剩餘可入庫良品數");

            var warehouse = await _db.Warehouses
                .FirstOrDefaultAsync(item => item.Id == request.WarehouseId && item.IsActive, cancellationToken)
                ?? throw new ArgumentException("請指定有效的成品倉庫");
            var bin = await _db.BinLocations
                .FirstOrDefaultAsync(item => item.Id == request.BinLocationId
                    && item.WarehouseId == warehouse.Id && item.IsActive, cancellationToken)
                ?? throw new ArgumentException("指定的成品儲位不存在、已停用或不屬於所選倉庫");
            if (bin.LocationCode.Equals(QuarantineLocationCode, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("良品不得入至 FQC 隔離儲位");

            var sku = await LockSkuAsync(lot.ProductSkuId, cancellationToken)
                ?? throw new InvalidOperationException("FQC 產品不存在");
            var workOrder = await _db.WorkOrders.FirstAsync(order => order.Id == lot.WorkOrderId, cancellationToken);
            var receipt = new ProductionReceipt
            {
                ReceiptNumber = DocumentNumberGenerator.Generate("MGR"),
                WorkOrderId = lot.WorkOrderId,
                Status = "posted",
                ReceivedAt = DateTime.UtcNow,
                ReceivedBy = string.IsNullOrWhiteSpace(request.ReceivedBy) ? null : request.ReceivedBy.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };
            var unitCost = await GetWorkOrderActualUnitCostAsync(workOrder.Id, cancellationToken);
            var item = new ProductionReceiptItem
            {
                ProductionReceiptId = receipt.Id,
                FqcInspectionLotId = lot.Id,
                SkuId = sku.Id,
                WarehouseId = warehouse.Id,
                BinLocationId = bin.Id,
                Quantity = request.Quantity,
                UnitCost = unitCost,
                TotalCost = Math.Round(request.Quantity * unitCost, 2)
            };
            receipt.Items.Add(item);
            _db.ProductionReceipts.Add(receipt);

            var balance = await GetBinBalanceAsync(sku.Id, warehouse.Id, bin.Id, cancellationToken);
            var now = DateTime.UtcNow;
            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                TrxId = DocumentNumberGenerator.GenerateShort("MGR"),
                SkuId = sku.Id,
                Type = "production_receipt",
                QtyChange = request.Quantity,
                QtyBefore = balance,
                QtyAfter = balance + request.Quantity,
                LotNumber = lot.LotNumber,
                RelatedDocType = "ProductionReceipt",
                RelatedDocId = receipt.Id,
                WarehouseId = warehouse.Id,
                BinLocationId = bin.Id,
                UnitCost = unitCost,
                TotalCost = item.TotalCost,
                Notes = $"製造完工入庫 {receipt.ReceiptNumber} / FQC {lot.InspectionNumber}",
                CreatedAt = now
            });
            sku.Qty += request.Quantity;
            sku.UpdatedAt = now;
            workOrder.ReceivedQty += request.Quantity;
            workOrder.UpdatedAt = now;
            await UpsertInventoryLotAsync(
                sku.Id, lot.LotNumber, warehouse.Id, bin.Id, request.Quantity, lot.ExpiryDate, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await UpdateSalesOrderProductionStatusAsync(workOrder.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await _accounting.CreateVoucherFromManufacturingReceiptAsync(
                receipt.Id, workOrder.Id, request.Quantity, item.TotalCost);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return MapReceipt(receipt, item, workOrder.WoNumber, lot.InspectionNumber, lot.LotNumber);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FqcInspectionLotDto> CreateReworkWorkOrderAsync(
        Guid id,
        string dispositionBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dispositionBy))
            throw new ArgumentException("請提供返修處置人員");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            var lot = await LockInspectionLotAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("找不到 FQC 檢驗批次");
            var sourceWorkOrder = await _db.WorkOrders
                .Include(order => order.ProductSku)
                .Include(order => order.Operations)
                .FirstOrDefaultAsync(order => order.Id == lot.WorkOrderId, cancellationToken)
                ?? throw new InvalidOperationException("找不到 FQC 批次來源工單");
            if (string.Equals(sourceWorkOrder.WoType, "rework", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("返修工單只允許一次返修與一次複檢；不合格品請轉報廢處置");
            if (lot.InspectionStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("FQC 批次尚未判定，不能建立返修工單");
            if (lot.QuarantinedQty <= 0)
                throw new InvalidOperationException("此 FQC 批次沒有待處置隔離數量");
            if (lot.ReworkWorkOrderId.HasValue ||
                await _db.ScrapRecords.AnyAsync(record => record.FqcInspectionLotId == lot.Id, cancellationToken))
                throw new InvalidOperationException("此 FQC 批次已建立返修工單或報廢單");

            var now = DateTime.UtcNow;
            var reworkWorkOrder = new WorkOrder
            {
                WoNumber = DocumentNumberGenerator.GenerateWorkOrder(),
                WoType = "rework",
                ProductSkuId = sourceWorkOrder.ProductSkuId,
                BomId = sourceWorkOrder.BomId,
                BomVersion = sourceWorkOrder.BomVersion,
                SalesOrderId = sourceWorkOrder.SalesOrderId,
                PlannedQty = lot.QuarantinedQty,
                Status = "planned",
                Priority = sourceWorkOrder.Priority,
                Notes = $"來源 FQC {lot.InspectionNumber}；隔離數量保留於隔離儲位，待既有庫存處理流程領用。",
                CreatedAt = now,
                UpdatedAt = now
            };
            reworkWorkOrder.Materials.Add(new WorkOrderMaterial
            {
                WorkOrderId = reworkWorkOrder.Id,
                WorkOrder = reworkWorkOrder,
                SkuId = lot.ProductSkuId,
                Sku = sourceWorkOrder.ProductSku
                    ?? throw new InvalidOperationException("返修產品 SKU 不存在"),
                FqcInspectionLotId = lot.Id,
                FqcInspectionLot = lot,
                BomStandardQty = 1,
                RequiredQty = lot.QuarantinedQty,
                Unit = sourceWorkOrder.ProductSku.Unit,
                ShortageQty = lot.QuarantinedQty,
                Status = "pending"
            });
            foreach (var sourceOperation in sourceWorkOrder.Operations.OrderBy(operation => operation.OperationSeq))
            {
                reworkWorkOrder.Operations.Add(new WorkOrderOperation
                {
                    WorkOrderId = reworkWorkOrder.Id,
                    WorkOrder = reworkWorkOrder,
                    OperationSeq = sourceOperation.OperationSeq,
                    OperationId = sourceOperation.OperationId,
                    WorkCenterId = sourceOperation.WorkCenterId,
                    PlannedQty = lot.QuarantinedQty,
                    Status = "pending"
                });
            }

            var sourceLinks = await _db.SalesOrderProductionLinks
                .Where(link => link.ProductionDocType == "work_order" && link.ProductionDocId == sourceWorkOrder.Id)
                .ToListAsync(cancellationToken);
            foreach (var sourceLink in sourceLinks)
            {
                _db.SalesOrderProductionLinks.Add(new SalesOrderProductionLink
                {
                    SalesOrderItemId = sourceLink.SalesOrderItemId,
                    ProductionDocType = "work_order",
                    ProductionDocId = reworkWorkOrder.Id,
                    ProductionDocNo = reworkWorkOrder.WoNumber,
                    LinkedQty = lot.QuarantinedQty,
                    LinkedAt = now,
                    Remarks = $"返修來源 FQC {lot.InspectionNumber}"
                });
            }

            _db.WorkOrders.Add(reworkWorkOrder);
            lot.ReworkWorkOrderId = reworkWorkOrder.Id;
            lot.DispositionStatus = "Rework";
            lot.DispositionBy = dispositionBy.Trim();
            lot.DispositionAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return MapInspectionLot(lot, 0);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FqcScrapNoteDto> CreateScrapNoteAsync(
        Guid id,
        string reasonCode,
        string reason,
        Guid requesterUserId,
        string requesterName,
        CancellationToken cancellationToken = default)
    {
        ValidateScrapReason(reasonCode, reason);
        if (requesterUserId == Guid.Empty || string.IsNullOrWhiteSpace(requesterName))
            throw new ArgumentException("無法識別報廢申請人");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            var lot = await LockInspectionLotAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException("找不到 FQC 檢驗批次");
            if (lot.InspectionStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("FQC 批次尚未判定，不能建立報廢單");
            if (lot.QuarantinedQty <= 0)
                throw new InvalidOperationException("此 FQC 批次沒有待處置隔離數量");
            if (lot.ReworkWorkOrderId.HasValue ||
                await _db.ScrapRecords.AnyAsync(record => record.FqcInspectionLotId == lot.Id, cancellationToken))
                throw new InvalidOperationException("此 FQC 批次已建立返修工單或報廢單");

            var workOrder = await _db.WorkOrders
                .FirstOrDefaultAsync(order => order.Id == lot.WorkOrderId, cancellationToken)
                ?? throw new InvalidOperationException("找不到 FQC 批次來源工單");
            var unitCost = await GetWorkOrderActualUnitCostAsync(workOrder.Id, cancellationToken);
            var now = DateTime.UtcNow;
            var scrapRecord = new ScrapRecord
            {
                WorkOrderId = workOrder.Id,
                SkuId = lot.ProductSkuId,
                FqcInspectionLotId = lot.Id,
                ScrapNumber = DocumentNumberGenerator.Generate("SCR"),
                ScrapQty = lot.QuarantinedQty,
                ScrapReasonCode = reasonCode.Trim(),
                Reason = reason.Trim(),
                DispositionCode = "FQC_SCRAP",
                CostAmount = Math.Round(lot.QuarantinedQty * unitCost, 2),
                ScrapStatus = "pending_quality_review",
                DispositionBy = requesterName.Trim(),
                DispositionAt = now,
                RequestedByUserId = requesterUserId,
                RequestedByName = requesterName.Trim(),
                SubmittedAt = now,
                CreatedAt = now
            };
            var submissionEvent = new FqcScrapApprovalEvent
            {
                Stage = "submission",
                Decision = "submitted",
                ActorUserId = requesterUserId,
                ActorName = requesterName.Trim(),
                OccurredAt = now,
                ScrapRecordId = scrapRecord.Id
            };
            _db.FqcScrapApprovalEvents.Add(submissionEvent);
            scrapRecord.ApprovalEvents.Add(submissionEvent);
            _db.ScrapRecords.Add(scrapRecord);
            lot.DispositionStatus = "ScrapPendingQualityReview";
            lot.DispositionBy = requesterName.Trim();
            lot.DispositionAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return MapScrapNote(scrapRecord, lot);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FqcScrapNoteDto> ResubmitFqcScrapNoteAsync(
        Guid scrapRecordId,
        string reasonCode,
        string reason,
        Guid requesterUserId,
        string requesterName,
        CancellationToken cancellationToken = default)
    {
        ValidateScrapReason(reasonCode, reason);
        if (requesterUserId == Guid.Empty || string.IsNullOrWhiteSpace(requesterName))
            throw new ArgumentException("無法識別報廢申請人");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            var record = await LockScrapRecordAsync(scrapRecordId, cancellationToken)
                ?? throw new KeyNotFoundException("找不到 FQC 報廢單");
            var lot = await LockInspectionLotAsync(record.FqcInspectionLotId ?? Guid.Empty, cancellationToken)
                ?? throw new InvalidOperationException("報廢單未連結 FQC 批次");
            if (record.ScrapStatus != "returned_to_requester")
                throw new InvalidOperationException("只有退回申請人的報廢單可以重新送審");
            if (record.RequestedByUserId != requesterUserId)
                throw new InvalidOperationException("只有原申請人可以重新送審此報廢單");

            var now = DateTime.UtcNow;
            record.ScrapReasonCode = reasonCode.Trim();
            record.Reason = reason.Trim();
            record.ScrapStatus = "pending_quality_review";
            record.SubmittedAt = now;
            record.DispositionBy = requesterName.Trim();
            record.DispositionAt = now;
            lot.DispositionStatus = "ScrapPendingQualityReview";
            lot.DispositionBy = requesterName.Trim();
            lot.DispositionAt = now;
            var resubmissionEvent = new FqcScrapApprovalEvent
            {
                Stage = "submission",
                Decision = "resubmitted",
                ActorUserId = requesterUserId,
                ActorName = requesterName.Trim(),
                OccurredAt = now,
                ScrapRecordId = record.Id
            };
            _db.FqcScrapApprovalEvents.Add(resubmissionEvent);
            record.ApprovalEvents.Add(resubmissionEvent);

            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return MapScrapNote(record, lot);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task<FqcScrapNoteDto> DecideFqcScrapQualityAsync(
        Guid scrapRecordId,
        Guid actorUserId,
        string actorName,
        bool approve,
        string? comments,
        CancellationToken cancellationToken = default) =>
        DecideFqcScrapAsync(scrapRecordId, actorUserId, actorName, approve, comments, "quality", cancellationToken);

    public Task<FqcScrapNoteDto> DecideFqcScrapFinanceAsync(
        Guid scrapRecordId,
        Guid actorUserId,
        string actorName,
        bool approve,
        string? comments,
        CancellationToken cancellationToken = default) =>
        DecideFqcScrapAsync(scrapRecordId, actorUserId, actorName, approve, comments, "finance", cancellationToken);

    private async Task<FqcScrapNoteDto> DecideFqcScrapAsync(
        Guid scrapRecordId,
        Guid actorUserId,
        string actorName,
        bool approve,
        string? comments,
        string stage,
        CancellationToken cancellationToken)
    {
        if (actorUserId == Guid.Empty || string.IsNullOrWhiteSpace(actorName))
            throw new ArgumentException("無法識別簽核人員");
        if (!approve && string.IsNullOrWhiteSpace(comments))
            throw new ArgumentException("退回報廢申請時必須填寫原因");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            var record = await LockScrapRecordAsync(scrapRecordId, cancellationToken)
                ?? throw new KeyNotFoundException("找不到 FQC 報廢單");
            var lot = await LockInspectionLotAsync(record.FqcInspectionLotId ?? Guid.Empty, cancellationToken)
                ?? throw new InvalidOperationException("報廢單未連結 FQC 批次");

            var expectedStatus = stage == "quality" ? "pending_quality_review" : "pending_finance_review";
            if (!string.Equals(record.ScrapStatus, expectedStatus, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(stage == "quality"
                    ? "此報廢單目前不在品保簽核階段"
                    : "報廢單必須先通過品保簽核才能由財務核定");

            var now = DateTime.UtcNow;
            var decision = approve ? "approved" : "rejected";
            var approvalEvent = new FqcScrapApprovalEvent
            {
                Stage = stage,
                Decision = decision,
                ActorUserId = actorUserId,
                ActorName = actorName.Trim(),
                Comments = NormalizeOptional(comments),
                OccurredAt = now,
                ScrapRecordId = record.Id
            };
            _db.FqcScrapApprovalEvents.Add(approvalEvent);
            record.ApprovalEvents.Add(approvalEvent);

            if (!approve)
            {
                record.ScrapStatus = "returned_to_requester";
                lot.DispositionStatus = "ScrapReturned";
            }
            else if (stage == "quality")
            {
                record.ScrapStatus = "pending_finance_review";
                lot.DispositionStatus = "ScrapPendingFinanceReview";
            }
            else
            {
                record.ScrapStatus = "approved_pending_disposal";
                lot.DispositionStatus = "ScrapApprovedPendingExecution";
            }
            lot.DispositionBy = actorName.Trim();
            lot.DispositionAt = now;

            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return MapScrapNote(record, lot);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task CloseReworkWorkOrderAsync(
        Guid workOrderId,
        string closedBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(closedBy))
            throw new ArgumentException("無法識別返修工單結案人員");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            var workOrder = await _db.WorkOrders
                .Include(order => order.Materials)
                .FirstOrDefaultAsync(order => order.Id == workOrderId, cancellationToken)
                ?? throw new KeyNotFoundException("找不到返修工單");
            if (!string.Equals(workOrder.WoType, "rework", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("只有返修工單可以從 FQC 流程結案");
            if (workOrder.Status != "pending_fqc")
                throw new InvalidOperationException("返修工單必須完成報工並等待 FQC 複檢後才能結案");

            var sourceMaterial = workOrder.Materials.SingleOrDefault(material => material.FqcInspectionLotId.HasValue)
                ?? throw new InvalidOperationException("返修工單未連結來源 FQC 批次");
            var sourceLot = await LockInspectionLotAsync(
                    sourceMaterial.FqcInspectionLotId!.Value, cancellationToken)
                ?? throw new InvalidOperationException("返修來源 FQC 批次不存在");
            if (sourceLot.ReworkWorkOrderId != workOrder.Id || sourceLot.QuarantinedQty != 0
                || sourceMaterial.IssuedQty < sourceMaterial.RequiredQty)
                throw new InvalidOperationException("來源隔離品尚未全數領用，返修工單不可結案");

            var reinspection = await _db.FqcInspectionLots
                .Include(lot => lot.ScrapRecords)
                .SingleOrDefaultAsync(lot => lot.ReworkSourceInspectionLotId == sourceLot.Id, cancellationToken)
                ?? throw new InvalidOperationException("返修工單尚未建立 FQC 複檢批次");
            if (reinspection.WorkOrderId != workOrder.Id
                || reinspection.InspectionStatus == "Pending")
                throw new InvalidOperationException("返修 FQC 複檢尚未完成判定");

            var receivedQty = await _db.ProductionReceiptItems
                .Where(item => item.FqcInspectionLotId == reinspection.Id)
                .SumAsync(item => (decimal?)item.Quantity, cancellationToken) ?? 0m;
            if (receivedQty != reinspection.ApprovedQty)
                throw new InvalidOperationException("返修複檢合格數尚未全數入庫");

            var approvedScrapQty = reinspection.ScrapRecords
                .Where(record => record.ScrapStatus == "approved_pending_disposal")
                .Sum(record => record.ScrapQty);
            if (approvedScrapQty != reinspection.QuarantinedQty
                || receivedQty + approvedScrapQty != reinspection.ProducedQty)
                throw new InvalidOperationException("返修複檢數量尚未全數入庫或完成報廢核定");

            var now = DateTime.UtcNow;
            workOrder.Status = "completed";
            workOrder.ActualEndDate ??= now;
            workOrder.UpdatedAt = now;
            sourceLot.DispositionStatus = "ReworkCompleted";
            sourceLot.DispositionBy = closedBy.Trim();
            sourceLot.DispositionAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<ScrapRecord?> LockScrapRecordAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational() && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var record = await _db.ScrapRecords
                .FromSqlInterpolated($"SELECT * FROM \"ScrapRecords\" WHERE \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (record is not null)
            {
                await _db.Entry(record).ReloadAsync(cancellationToken);
                await _db.Entry(record).Collection(item => item.ApprovalEvents).LoadAsync(cancellationToken);
            }
            return record;
        }

        var existing = await _db.ScrapRecords.FirstOrDefaultAsync(record => record.Id == id, cancellationToken);
        if (existing is not null)
            await _db.Entry(existing).Collection(record => record.ApprovalEvents).LoadAsync(cancellationToken);
        return existing;
    }

    private static void ValidateScrapReason(string reasonCode, string reason)
    {
        if (!new[] { "01", "02", "03", "04", "05" }.Contains(reasonCode?.Trim()))
            throw new ArgumentException("請選擇有效的報廢原因分類");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("請填寫報廢原因");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static FqcScrapNoteDto MapScrapNote(ScrapRecord record, FqcInspectionLot lot) => new()
    {
        Id = record.Id,
        ScrapNumber = record.ScrapNumber!,
        FqcInspectionLotId = lot.Id,
        InspectionNumber = lot.InspectionNumber,
        LotNumber = lot.LotNumber,
        Quantity = record.ScrapQty,
        Status = record.ScrapStatus!,
        ReasonCode = record.ScrapReasonCode,
        Reason = record.Reason,
        RequestedByUserId = record.RequestedByUserId,
        RequestedByName = record.RequestedByName,
        SubmittedAt = record.SubmittedAt,
        CreatedAt = record.CreatedAt,
        ApprovalEvents = record.ApprovalEvents
            .OrderBy(item => item.OccurredAt)
            .Select(item => new FqcScrapApprovalEventDto
            {
                Id = item.Id,
                Stage = item.Stage,
                Decision = item.Decision,
                ActorUserId = item.ActorUserId,
                ActorName = item.ActorName,
                Comments = item.Comments,
                OccurredAt = item.OccurredAt
            })
            .ToList()
    };

    public async Task<List<ProductionReceiptDto>> GetProductionReceiptsAsync(
        Guid? workOrderId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProductionReceiptItems
            .AsNoTracking()
            .Include(item => item.ProductionReceipt).ThenInclude(receipt => receipt.WorkOrder)
            .Include(item => item.FqcInspectionLot)
            .AsQueryable();
        if (workOrderId.HasValue)
            query = query.Where(item => item.ProductionReceipt.WorkOrderId == workOrderId.Value);

        var items = await query.OrderByDescending(item => item.ProductionReceipt.ReceivedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        return items.Select(item => MapReceipt(
            item.ProductionReceipt, item, item.ProductionReceipt.WorkOrder.WoNumber,
            item.FqcInspectionLot.InspectionNumber, item.FqcInspectionLot.LotNumber)).ToList();
    }

    private async Task<FqcInspectionLot?> LockInspectionLotAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational() && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var lot = await _db.FqcInspectionLots
                .FromSqlInterpolated($"SELECT * FROM \"FqcInspectionLots\" WHERE \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (lot is not null)
                await _db.Entry(lot).ReloadAsync(cancellationToken);
            return lot;
        }

        return await _db.FqcInspectionLots.FirstOrDefaultAsync(lot => lot.Id == id, cancellationToken);
    }

    private async Task<Sku?> LockSkuAsync(Guid id, CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational() && _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var sku = await _db.Skus
                .FromSqlInterpolated($"SELECT * FROM \"Skus\" WHERE \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (sku is not null)
                await _db.Entry(sku).ReloadAsync(cancellationToken);
            return sku;
        }

        return await _db.Skus.FirstOrDefaultAsync(sku => sku.Id == id, cancellationToken);
    }

    private async Task<BinLocation> GetOrCreateQuarantineBinAsync(Guid warehouseId, CancellationToken cancellationToken)
    {
        var bin = await _db.BinLocations.SingleOrDefaultAsync(
            item => item.WarehouseId == warehouseId && item.LocationCode == QuarantineLocationCode,
            cancellationToken);
        if (bin is not null)
        {
            if (!bin.IsActive)
                throw new InvalidOperationException("FQC 隔離儲位已停用，請先啟用儲位");
            return bin;
        }

        bin = new BinLocation
        {
            WarehouseId = warehouseId,
            LocationCode = QuarantineLocationCode,
            Zone = "QA",
            Rack = "FQC-HOLD",
            IsActive = true
        };
        _db.BinLocations.Add(bin);
        return bin;
    }

    private async Task<decimal> GetBinBalanceAsync(
        Guid skuId,
        Guid warehouseId,
        Guid binLocationId,
        CancellationToken cancellationToken) =>
        await _db.InventoryTransactions
            .Where(item => item.SkuId == skuId && item.WarehouseId == warehouseId &&
                           item.BinLocationId == binLocationId)
            .SumAsync(item => (decimal?)item.QtyChange, cancellationToken) ?? 0m;

    private async Task UpsertInventoryLotAsync(
        Guid skuId,
        string lotNumber,
        Guid warehouseId,
        Guid binLocationId,
        decimal quantity,
        DateTime? expiryDate,
        CancellationToken cancellationToken)
    {
        expiryDate = NormalizeCalendarDateToUtc(expiryDate);
        var inventoryLot = await _db.InventoryLots.FirstOrDefaultAsync(
            item => item.SkuId == skuId && item.LotNumber == lotNumber &&
                    item.WarehouseId == warehouseId && item.BinLocationId == binLocationId,
            cancellationToken);
        if (inventoryLot is null)
        {
            _db.InventoryLots.Add(new InventoryLot
            {
                SkuId = skuId,
                LotNumber = lotNumber,
                Qty = quantity,
                WarehouseId = warehouseId,
                BinLocationId = binLocationId,
                ExpiryDate = expiryDate,
                Status = "active"
            });
            return;
        }

        inventoryLot.Qty += quantity;
        inventoryLot.ExpiryDate = expiryDate ?? inventoryLot.ExpiryDate;
        inventoryLot.Status = "active";
    }

    private static DateTime? NormalizeCalendarDateToUtc(DateTime? value) =>
        value.HasValue
            ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc)
            : null;

    private async Task UpdateSalesOrderProductionStatusAsync(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        var currentLinks = await _db.SalesOrderProductionLinks
            .Where(link => link.ProductionDocType == "work_order" && link.ProductionDocId == workOrderId)
            .ToListAsync(cancellationToken);
        var salesOrderItemIds = currentLinks.Select(link => link.SalesOrderItemId).Distinct().ToList();
        if (salesOrderItemIds.Count == 0)
            return;

        var currentItems = await _db.SalesOrderItems
            .Where(item => salesOrderItemIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        var orderIds = currentItems.Select(item => item.SalesOrderId).Distinct().ToList();
        var orderItems = await _db.SalesOrderItems
            .Where(item => orderIds.Contains(item.SalesOrderId))
            .ToListAsync(cancellationToken);
        var allOrderItemIds = orderItems.Select(item => item.Id).ToList();
        var links = await _db.SalesOrderProductionLinks
            .Where(link => allOrderItemIds.Contains(link.SalesOrderItemId) &&
                           link.ProductionDocType == "work_order")
            .ToListAsync(cancellationToken);
        var productionDocIds = links.Select(link => link.ProductionDocId).Distinct().ToList();
        var receiptsByWorkOrder = await _db.ProductionReceiptItems
            .Where(item => productionDocIds.Contains(item.ProductionReceipt.WorkOrderId))
            .GroupBy(item => item.ProductionReceipt.WorkOrderId)
            .Select(group => new { WorkOrderId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToDictionaryAsync(group => group.WorkOrderId, group => group.Quantity, cancellationToken);

        foreach (var item in orderItems.Where(item => links.Any(link => link.SalesOrderItemId == item.Id)))
        {
            var itemLinks = links.Where(link => link.SalesOrderItemId == item.Id);
            var receivedQty = itemLinks.Sum(link => receiptsByWorkOrder.GetValueOrDefault(link.ProductionDocId));
            item.ProductionStatus = receivedQty >= item.Quantity
                ? "completed"
                : receivedQty > 0 ? "partial" : "pending";
        }

        var orders = await _db.SalesOrders
            .Where(order => orderIds.Contains(order.Id))
            .ToListAsync(cancellationToken);
        foreach (var order in orders)
        {
            var manufacturingItems = orderItems.Where(item =>
                item.SalesOrderId == order.Id &&
                (item.NeedManufacturing || links.Any(link => link.SalesOrderItemId == item.Id))).ToList();
            if (manufacturingItems.Count == 0)
                continue;

            var receivedByItem = manufacturingItems.ToDictionary(
                item => item.Id,
                item => links.Where(link => link.SalesOrderItemId == item.Id)
                    .Sum(link => receiptsByWorkOrder.GetValueOrDefault(link.ProductionDocId)));
            var allManufacturingReceived = manufacturingItems.All(item => receivedByItem[item.Id] >= item.Quantity);
            var anyManufacturingReceived = receivedByItem.Values.Any(quantity => quantity > 0);
            order.ManufacturingStatus = allManufacturingReceived
                ? "completed"
                : anyManufacturingReceived ? "partial" : order.ManufacturingStatus;
            order.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<decimal> GetWorkOrderActualUnitCostAsync(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        var analysis = await _workOrderCosting.GetCostAnalysisAsync(workOrderId);
        if (analysis is null)
            throw new InvalidOperationException("找不到來源工單成本分析");
        if (analysis.ActualQty <= 0)
            throw new InvalidOperationException("來源工單尚無實際完工數，無法按實際成本計價");
        if (!analysis.HasActualCost)
            throw new InvalidOperationException("來源工單尚無實際成本，無法按實際成本計價");

        return Math.Round(analysis.TotalActualCost / analysis.ActualQty, 4);
    }

    private static FqcInspectionLotDto MapInspectionLot(FqcInspectionLot lot, decimal receivedQty) => new()
    {
        Id = lot.Id,
        InspectionNumber = lot.InspectionNumber,
        WorkOrderId = lot.WorkOrderId,
        WorkOrderNumber = lot.WorkOrder?.WoNumber,
        ProductSkuId = lot.ProductSkuId,
        ProductSkuNumber = lot.ProductSku?.SkuNumber,
        LotNumber = lot.LotNumber,
        ExpiryDate = lot.ExpiryDate,
        ProducedQty = lot.ProducedQty,
        SampledQty = lot.SampledQty,
        DefectiveQty = lot.DefectiveQty,
        ApprovedQty = lot.ApprovedQty,
        ReceivedQty = receivedQty,
        CriticalDefectFound = lot.CriticalDefectFound,
        InspectionStatus = lot.InspectionStatus,
        DispositionStatus = lot.DispositionStatus,
        DefectCode = lot.DefectCode,
        InspectorName = lot.InspectorName,
        InspectedAt = lot.InspectedAt,
        DecisionBy = lot.DecisionBy,
        DecidedAt = lot.DecidedAt,
        QuarantinedQty = lot.QuarantinedQty,
        ReworkWorkOrderId = lot.ReworkWorkOrderId,
        ReworkSourceInspectionLotId = lot.ReworkSourceInspectionLotId,
        WorkOrderType = lot.WorkOrder?.WoType,
        WorkOrderStatus = lot.WorkOrder?.Status,
        DispositionBy = lot.DispositionBy,
        DispositionAt = lot.DispositionAt,
        Notes = lot.Notes,
        ScrapNote = lot.ScrapRecords
            .OrderByDescending(record => record.SubmittedAt ?? record.CreatedAt)
            .Select(record => MapScrapNote(record, lot))
            .FirstOrDefault()
    };

    private static ProductionReceiptDto MapReceipt(
        ProductionReceipt receipt,
        ProductionReceiptItem item,
        string? workOrderNumber,
        string? fqcNumber,
        string? lotNumber) => new()
    {
        Id = receipt.Id,
        ReceiptNumber = receipt.ReceiptNumber,
        WorkOrderId = receipt.WorkOrderId,
        WorkOrderNumber = workOrderNumber,
        FqcInspectionLotId = item.FqcInspectionLotId,
        FqcInspectionNumber = fqcNumber,
        LotNumber = lotNumber,
        SkuId = item.SkuId,
        Quantity = item.Quantity,
        UnitCost = item.UnitCost,
        TotalCost = item.TotalCost,
        WarehouseId = item.WarehouseId,
        BinLocationId = item.BinLocationId,
        ReceivedAt = receipt.ReceivedAt
    };
}
