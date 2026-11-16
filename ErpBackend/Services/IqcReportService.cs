using Microsoft.EntityFrameworkCore;
using System.Data;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.QA;

namespace ErpBackend.Services;

public interface IIqcReportService
{
    Task<(List<IqcReportDto> Items, int TotalCount)> GetReportsAsync(string? result, int page, int pageSize);
    Task<IqcInspectionResponseDto?> GetInspectionAsync(Guid id, CancellationToken cancellationToken);
    Task<IqcInspectionResponseDto> CompleteInspectionAsync(
        Guid id,
        CompleteIqcInspectionRequestDto request,
        Guid inspectorId,
        CancellationToken cancellationToken);
}

public class IqcReportService : IIqcReportService
{
    private readonly ErpGeneralDbContext _db;

    public IqcReportService(ErpGeneralDbContext db) => _db = db;

    public async Task<(List<IqcReportDto> Items, int TotalCount)> GetReportsAsync(string? result, int page, int pageSize)
    {
        var query = _db.IqcReports.AsQueryable();

        if (!string.IsNullOrEmpty(result))
            query = query.Where(r => r.Result == result);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.InspectionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.Sku)
            .Include(r => r.Supplier)
            .ToListAsync();
        var receiptIds = items.Select(item => item.GoodsReceiptId).Distinct().ToList();
        var receiptNumbers = await _db.GoodsReceipts
            .Where(receipt => receiptIds.Contains(receipt.Id))
            .ToDictionaryAsync(receipt => receipt.Id, receipt => receipt.GrNumber);

        var dtos = items.Select(r => new IqcReportDto
        {
            Id = r.Id,
            ReportNumber = r.ReportNumber,
            GoodsReceiptId = r.GoodsReceiptId,
            GoodsReceiptNumber = receiptNumbers.TryGetValue(r.GoodsReceiptId, out var receiptNumber)
                ? receiptNumber
                : null,
            SupplierName = r.Supplier?.SupplierName,
            SkuId = r.SkuId,
            SkuName = r.Sku?.SkuName,
            BatchNumber = r.LotNumber,
            ReceivedQty = r.ReceivedQty,
            SampleQty = r.ReceivedQty > 0 ? r.ReceivedQty : r.SampleSize,
            DefectQty = r.RejectNumber,
            Result = r.Result,
            ReportDate = r.InspectionDate
        }).ToList();

        return (dtos, totalCount);
    }

    public async Task<IqcInspectionResponseDto?> GetInspectionAsync(Guid id, CancellationToken cancellationToken)
    {
        var report = await LoadInspectionAsync(id, cancellationToken);
        return report is null ? null : MapInspection(report);
    }

    public async Task<IqcInspectionResponseDto> CompleteInspectionAsync(
        Guid id,
        CompleteIqcInspectionRequestDto request,
        Guid inspectorId,
        CancellationToken cancellationToken)
    {
        if (inspectorId == Guid.Empty)
            throw new ArgumentException("必須提供有效的檢驗人員", nameof(inspectorId));
        if (request.Result is not ("pass" or "fail"))
            throw new ArgumentException("檢驗結果只可為 pass 或 fail", nameof(request));
        if (request.SampleSize <= 0)
            throw new ArgumentException("抽樣數必須大於 0", nameof(request));
        if (request.AcceptNumber < 0 || request.RejectNumber < 0)
            throw new ArgumentException("允收數及拒收數不可小於 0", nameof(request));
        if (request.Items is null || request.Items.Count == 0)
            throw new ArgumentException("至少須記錄一項檢驗項目", nameof(request));
        if (request.Items.Any(item =>
                string.IsNullOrWhiteSpace(item.InspectionItem) ||
                string.IsNullOrWhiteSpace(item.Specification) ||
                string.IsNullOrWhiteSpace(item.MeasuredValue) ||
                item.Result is not ("pass" or "fail") ||
                (item.LowerLimit.HasValue && item.UpperLimit.HasValue && item.LowerLimit > item.UpperLimit)))
            throw new ArgumentException("檢驗項目須包含名稱、規格、實測值及有效判定", nameof(request));
        if (request.Result == "pass" &&
            (request.RejectNumber != 0 || request.Items.Any(item => item.Result != "pass")))
            throw new ArgumentException("判定合格時不可有拒收數或不合格檢驗項目", nameof(request));
        if (request.Result == "fail" &&
            (request.RejectNumber <= 0 || request.Items.All(item => item.Result != "fail")))
            throw new ArgumentException("判定不合格時須記錄拒收數及至少一項不合格項目", nameof(request));

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var report = await LoadInspectionAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("找不到 IQC 檢驗單");
        if (report.Result != "pending")
            throw new InvalidOperationException("此 IQC 檢驗單已完成判定");
        if (report.GoodsReceipt?.Status != "received" || report.GoodsReceiptItem is null)
            throw new InvalidOperationException("收貨單或收貨明細狀態不允許檢驗判定");
        if (request.SampleSize > report.ReceivedQty)
            throw new ArgumentException("抽樣數不可大於本批收貨數量", nameof(request));

        report.Result = request.Result;
        report.SampleSize = request.SampleSize;
        report.AcceptNumber = request.AcceptNumber;
        report.RejectNumber = request.RejectNumber;
        report.InspectorId = inspectorId;
        report.InspectionDate = DateTime.UtcNow;
        _db.IqcReportItems.RemoveRange(report.Items);
        report.Items.Clear();
        foreach (var item in request.Items)
        {
            var inspectionItem = new IqcReportItem
            {
                IqcReportId = report.Id,
                InspectionItem = item.InspectionItem.Trim(),
                Specification = item.Specification.Trim(),
                LowerLimit = item.LowerLimit,
                UpperLimit = item.UpperLimit,
                MeasuredValue = item.MeasuredValue.Trim(),
                Result = item.Result
            };
            _db.IqcReportItems.Add(inspectionItem);
            report.Items.Add(inspectionItem);
        }

        var receiptItem = report.GoodsReceiptItem;
        receiptItem.IqcResult = request.Result;
        receiptItem.AcceptedQty = request.Result == "pass" ? receiptItem.InventoryQty : 0;
        receiptItem.RejectedQty = request.Result == "fail" ? receiptItem.InventoryQty : 0;

        if (!string.IsNullOrWhiteSpace(report.LotNumber))
            await ApplyLotDispositionAsync(report, request.Result, cancellationToken);

        if (request.Result == "pass")
            await ReleaseInventoryAsync(report, inspectorId, cancellationToken);

        var receiptReports = await _db.IqcReports
            .Where(item => item.GoodsReceiptId == report.GoodsReceiptId)
            .ToListAsync(cancellationToken);
        report.GoodsReceipt.IqcStatus = receiptReports.Any(item => item.Result == "pending")
            ? "pending"
            : receiptReports.Any(item => item.Result == "fail")
                ? "failed"
                : "passed";

        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return MapInspection(report);
    }

    private Task<IqcReport?> LoadInspectionAsync(Guid id, CancellationToken cancellationToken) =>
        _db.IqcReports
            .Include(item => item.GoodsReceipt)
            .Include(item => item.GoodsReceiptItem)
            .Include(item => item.Supplier)
            .Include(item => item.Sku)
            .Include(item => item.ReleaseBinLocation)
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    private async Task ApplyLotDispositionAsync(
        IqcReport report,
        string result,
        CancellationToken cancellationToken)
    {
        var receipt = report.GoodsReceipt!;
        var sourceBinId = report.GoodsReceiptItem!.BinLocationId;
        var lot = await _db.InventoryLots.SingleOrDefaultAsync(
            item => item.SkuId == report.SkuId &&
                    item.LotNumber == report.LotNumber &&
                    item.WarehouseId == receipt.WarehouseId &&
                    item.BinLocationId == sourceBinId,
            cancellationToken);
        if (lot is null || lot.Qty < report.ReceivedQty)
            throw new InvalidOperationException("待檢批號庫存不足，未執行 IQC 判定");

        lot.Status = result == "pass" ? "active" : "rejected";
    }

    private async Task ReleaseInventoryAsync(
        IqcReport report,
        Guid inspectorId,
        CancellationToken cancellationToken)
    {
        var receipt = report.GoodsReceipt!;
        var receiptItem = report.GoodsReceiptItem!;
        var sourceBinId = receiptItem.BinLocationId;
        var targetBinId = report.ReleaseBinLocationId;
        if (sourceBinId is null || targetBinId is null || sourceBinId == targetBinId)
        {
            if (sourceBinId == targetBinId)
                receiptItem.BinLocationId = targetBinId;
            return;
        }

        var targetBin = await _db.BinLocations.SingleOrDefaultAsync(
            item => item.Id == targetBinId &&
                    item.WarehouseId == receipt.WarehouseId &&
                    item.IsActive,
            cancellationToken);
        if (targetBin is null)
            throw new InvalidOperationException("原指定良品儲位不存在、已停用或不屬於收貨倉庫");

        var sourceBalance = await GetBinBalanceAsync(
            report.SkuId!.Value, receipt.WarehouseId, sourceBinId, cancellationToken);
        if (sourceBalance < report.ReceivedQty)
            throw new InvalidOperationException("待檢儲位庫存不足，未執行 IQC 放行");
        var targetBalance = await GetBinBalanceAsync(
            report.SkuId.Value, receipt.WarehouseId, targetBinId, cancellationToken);
        var now = DateTime.UtcNow;

        _db.InventoryTransactions.AddRange(
            new InventoryTransaction
            {
                TrxId = $"IQC-{Guid.NewGuid():N}"[..30],
                SkuId = report.SkuId.Value,
                Type = "iqc_release_out",
                QtyChange = -report.ReceivedQty,
                QtyBefore = sourceBalance,
                QtyAfter = sourceBalance - report.ReceivedQty,
                LotNumber = report.LotNumber,
                RelatedDocType = "IqcReport",
                RelatedDocId = report.Id,
                WarehouseId = receipt.WarehouseId,
                BinLocationId = sourceBinId,
                HandlerId = inspectorId,
                Notes = $"IQC 合格放行 {report.ReportNumber}",
                CreatedAt = now
            },
            new InventoryTransaction
            {
                TrxId = $"IQC-{Guid.NewGuid():N}"[..30],
                SkuId = report.SkuId.Value,
                Type = "iqc_release_in",
                QtyChange = report.ReceivedQty,
                QtyBefore = targetBalance,
                QtyAfter = targetBalance + report.ReceivedQty,
                LotNumber = report.LotNumber,
                RelatedDocType = "IqcReport",
                RelatedDocId = report.Id,
                WarehouseId = receipt.WarehouseId,
                BinLocationId = targetBinId,
                HandlerId = inspectorId,
                Notes = $"IQC 合格放行 {report.ReportNumber}",
                CreatedAt = now
            });

        if (!string.IsNullOrWhiteSpace(report.LotNumber))
        {
            var lot = await _db.InventoryLots.SingleAsync(
                item => item.SkuId == report.SkuId &&
                        item.LotNumber == report.LotNumber &&
                        item.WarehouseId == receipt.WarehouseId &&
                        item.BinLocationId == sourceBinId,
                cancellationToken);
            if (lot.Qty != report.ReceivedQty)
                throw new InvalidOperationException(
                    "批號庫存數量與本次待檢數量不一致，無法安全移轉整批批號");

            lot.BinLocationId = targetBinId;
            lot.Status = "active";
        }

        receiptItem.BinLocationId = targetBinId;
    }

    private async Task<decimal> GetBinBalanceAsync(
        Guid skuId,
        Guid warehouseId,
        Guid? binLocationId,
        CancellationToken cancellationToken)
    {
        var balance = await _db.InventoryTransactions
            .Where(item => item.SkuId == skuId &&
                           item.WarehouseId == warehouseId &&
                           item.BinLocationId == binLocationId)
            .SumAsync(item => (decimal?)item.QtyChange, cancellationToken);
        return balance ?? 0m;
    }

    private static IqcInspectionResponseDto MapInspection(IqcReport report) =>
        new(
            report.Id,
            report.ReportNumber,
            report.GoodsReceiptId,
            report.GoodsReceipt?.GrNumber ?? string.Empty,
            report.GoodsReceiptItemId,
            report.SkuId,
            report.Sku?.SkuNumber,
            report.Sku?.SkuName,
            report.Supplier?.SupplierName,
            report.LotNumber,
            report.ReceivedQty,
            report.ReleaseBinLocationId,
            report.ReleaseBinLocation?.LocationCode,
            report.Result,
            report.SampleSize,
            report.AcceptNumber,
            report.RejectNumber,
            report.InspectorId,
            report.InspectionDate,
            report.Items.Select(item => new IqcInspectionItemResponseDto(
                item.Id,
                item.InspectionItem,
                item.Specification,
                item.LowerLimit,
                item.UpperLimit,
                item.MeasuredValue,
                item.Result)).ToList());
}
