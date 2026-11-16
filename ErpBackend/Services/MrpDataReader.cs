using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Sales;
using ErpBackend.Models.Procurement;

namespace ErpBackend.Services;

public interface IMrpDataReader
{
    Task<DemandDataCache> PreloadDemandDataAsync(List<Guid> skuIds, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog);
    Task<List<DemandSource>> GetDemandSourcesAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog = true);
    Task<List<ScheduledReceipt>> GetScheduledReceiptsAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd);
    Task<List<Sku>> GetSkusForPlanningAsync();
    Task<MrpRun?> GetLatestRunAsync();
    Task<List<MrpRun>> GetRunsAsync(int page = 1, int pageSize = 20);
}

public class DemandDataCache
{
    public List<SalesOrderItem> SalesOrderItems { get; set; } = new();
    public List<WorkOrderMaterial> WorkOrderMaterials { get; set; } = new();
    public List<DemandForecast> DemandForecasts { get; set; } = new();
    public List<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new();
    public Dictionary<Guid, decimal> SkuQtys { get; set; } = new();
    public Dictionary<Guid, decimal> SkuReorderPoints { get; set; } = new();
}

public class ScheduledReceipt
{
    public Guid PurchaseOrderItemId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public decimal ReceiptQty { get; set; }
    public decimal ReceivedQty { get; set; }
}

public class MrpDataReader : IMrpDataReader
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<MrpDataReader> _logger;

    public MrpDataReader(ErpGeneralDbContext db, ILogger<MrpDataReader> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 批量預載入需求資料，避免 N+1 查詢（效能優化版本）
    /// </summary>
    public async Task<DemandDataCache> PreloadDemandDataAsync(List<Guid> skuIds, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog)
    {
        var cache = new DemandDataCache();

        // 批量查詢所有需求數據
        _logger.LogInformation("MRP 批量預載入需求數據...");

        // 1. 批量查詢銷售訂單
        var startMonth = horizonStart.ToString("yyyy-MM");
        var endMonth = horizonEnd.ToString("yyyy-MM");

        cache.SalesOrderItems = await _db.SalesOrderItems.AsNoTracking()
            .Include(soi => soi.SalesOrder)
            .Where(soi => skuIds.Contains(soi.SkuId ?? Guid.Empty)
                     && soi.Status == "pending")
            .ToListAsync();

        // 2. 批量查詢工單需求
        cache.WorkOrderMaterials = await _db.WorkOrderMaterials.AsNoTracking()
            .Include(wom => wom.WorkOrder)
            .Where(wom => skuIds.Contains(wom.SkuId))
            .ToListAsync();

        // 3. 批量查詢預測
        cache.DemandForecasts = await _db.DemandForecasts.AsNoTracking()
            .Where(df => skuIds.Contains(df.SkuId)
                    && df.ForecastMonth.CompareTo(startMonth) >= 0
                    && df.ForecastMonth.CompareTo(endMonth) <= 0)
            .ToListAsync();

        // 4. 批量查詢採購預計入庫
        cache.PurchaseOrderItems = await _db.PurchaseOrderItems.AsNoTracking()
            .Include(poi => poi.PurchaseOrder)
            .Where(poi => skuIds.Contains(poi.SkuId)
                     && poi.PurchaseOrder != null && poi.PurchaseOrder.Status == "confirmed")
            .ToListAsync();

        // 5. 批量查詢 SKU 庫存資料
        var skus = await _db.Skus.AsNoTracking()
            .Where(s => skuIds.Contains(s.Id)).ToListAsync();
        foreach (var sku in skus)
        {
            cache.SkuQtys[sku.Id] = sku.Qty;
            cache.SkuReorderPoints[sku.Id] = sku.ReorderPoint;
        }

        _logger.LogInformation($"預載入完成：SO={cache.SalesOrderItems.Count}, WO={cache.WorkOrderMaterials.Count}, 預測={cache.DemandForecasts.Count}, PO={cache.PurchaseOrderItems.Count}");

        return cache;
    }

    public async Task<List<DemandSource>> GetDemandSourcesAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog = true)
    {
        var sources = new List<DemandSource>();

        // 0. MPS 需求 (優先級最高，因為 MPS 已經處理了預測沖銷)
        try
        {
            var latestMps = await _db.MpsHeaders.AsNoTracking()
                .Where(h => h.Status == "APPROVED" || h.Status == "DRAFT") // 實務上通常只用已核准，此處放寬
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestMps != null)
            {
                var mpsDemands = await _db.MpsLines.AsNoTracking()
                    .Where(l => l.MpsHeaderId == latestMps.Id && l.SkuId == skuId)
                    .ToListAsync();

                if (mpsDemands.Any())
                {
                    sources.AddRange(mpsDemands.Select(l => new DemandSource
                    {
                        RequirementType = "mps",
                        SourceDocumentId = l.Id,
                        SourceDocumentNumber = latestMps.MpsNo,
                        RequiredDate = l.BucketStart,
                        RequiredQty = l.AdjustedQty
                    }));

                    _logger.LogDebug($"料號 {skuId}：從 MPS {latestMps.MpsNo} 獲取需求，已跳過原始 SO/Forecast 以免重覆");
                    // 如果該料號已由 MPS 管理，則不再讀取原始 SO/Forecast
                    return sources;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "獲取 MPS 需求失敗");
        }

        // 1. 銷售訂單需求
        try
        {
            IQueryable<SalesOrderItem> soQuery = _db.SalesOrderItems.AsNoTracking()
                .Include(soi => soi.SalesOrder)
                .Where(soi => soi.SkuId == skuId
                           && soi.SalesOrder.Status != "cancelled"
                           && soi.SalesOrder.Status != "completed");

            if (includeBacklog)
            {
                // 包含歷史未結訂單：查詢所有未完成且未完全出貨的訂單
                soQuery = soQuery.Where(soi => soi.Quantity > soi.DeliveredQty);
            }
            else
            {
                // 只查詢時間範圍內的訂單
                soQuery = soQuery.Where(soi => soi.PlannedFinishDate >= horizonStart
                                           && soi.PlannedFinishDate <= horizonEnd);
            }

            var soItems = await soQuery.ToListAsync();

            sources.AddRange(soItems.Select(soi => new DemandSource
            {
                RequirementType = "sales_order",
                SourceDocumentId = soi.SalesOrderId,
                SourceDocumentNumber = soi.SalesOrder.OrderNumber,
                RequiredDate = soi.PlannedFinishDate ?? horizonEnd,
                RequiredQty = soi.Quantity - soi.DeliveredQty
            }));

            _logger.LogDebug($"料號 {skuId}：找到 {soItems.Count} 個銷售訂單需求");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "獲取銷售訂單需求失敗");
        }

        // 2. 工單物料需求
        try
        {
            IQueryable<WorkOrderMaterial> woQuery = _db.WorkOrderMaterials.AsNoTracking()
                .Include(wom => wom.WorkOrder)
                .Where(wom => wom.SkuId == skuId
                           && wom.WorkOrder.Status != "cancelled"
                           && wom.WorkOrder.Status != "completed");

            if (includeBacklog)
            {
                // 包含歷史未結工單
                woQuery = woQuery.Where(wom => wom.RequiredQty > wom.IssuedQty);
            }
            else
            {
                woQuery = woQuery.Where(wom => wom.WorkOrder.PlannedStartDate >= horizonStart
                                            && wom.WorkOrder.PlannedStartDate <= horizonEnd);
            }

            var woMaterials = await woQuery.ToListAsync();

            sources.AddRange(woMaterials.Select(wom => new DemandSource
            {
                RequirementType = "work_order",
                SourceDocumentId = wom.WorkOrderId,
                SourceDocumentNumber = wom.WorkOrder.WoNumber,
                RequiredDate = wom.WorkOrder.PlannedStartDate ?? horizonStart,
                RequiredQty = wom.RequiredQty - wom.IssuedQty
            }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "獲取工單需求失敗");
        }

        // 3. 安全庫存補貨需求
        try
        {
            var sku = await _db.Skus.AsNoTracking().FirstOrDefaultAsync(s => s.Id == skuId);
            if (sku != null && sku.Qty <= sku.ReorderPoint)
            {
                sources.Add(new DemandSource
                {
                    RequirementType = "safety_stock",
                    RequiredDate = horizonStart,
                    RequiredQty = sku.ReorderPoint - sku.Qty
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "獲取安全庫存需求失敗");
        }

        // 4. 預測需求（可選）
        try
        {
            var forecasts = await _db.DemandForecasts.AsNoTracking()
                .Where(df => df.SkuId == skuId
                          && df.ForecastMonth.CompareTo(horizonStart.ToString("yyyy-MM")) >= 0
                          && df.ForecastMonth.CompareTo(horizonEnd.ToString("yyyy-MM")) <= 0)
                .ToListAsync();

            sources.AddRange(forecasts.Select(f => new DemandSource
            {
                RequirementType = "forecast",
                RequiredDate = DateTime.Parse($"{f.ForecastMonth}-01"),
                RequiredQty = f.ForecastQty
            }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "獲取預測需求失敗");
        }

        // 5. 來自本輪 MRP 的受入需求 (Dependent Requirements from Parents)
        try
        {
            var dependentDemands = await _db.MrpRequirements.AsNoTracking()
                .Where(r => r.SkuId == skuId
                         && r.RequirementType == "dependent"
                         && r.MrpRunId != Guid.Empty) // 這裡通常是本輪剛產生的
                .ToListAsync();

            sources.AddRange(dependentDemands.Select(r => new DemandSource
            {
                RequirementType = "dependent",
                SourceDocumentId = r.Id,
                SourceDocumentNumber = r.SourceDocumentNumber,
                RequiredDate = r.RequiredDate,
                RequiredQty = r.RequiredQty
            }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "獲取受入需求失敗");
        }

        return sources;
    }

    public async Task<List<ScheduledReceipt>> GetScheduledReceiptsAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd)
    {
        var receipts = new List<ScheduledReceipt>();

        try
        {
            var poItems = await _db.PurchaseOrderItems.AsNoTracking()
                .Include(poi => poi.PurchaseOrder)
                .Where(poi => poi.SkuId == skuId
                           && poi.PurchaseOrder != null && poi.PurchaseOrder.Status == "confirmed"
                           && poi.PurchaseOrder.ExpectedDeliveryDate >= horizonStart
                           && poi.PurchaseOrder.ExpectedDeliveryDate <= horizonEnd
                           && poi.ReceivedQty < poi.Quantity)
                .ToListAsync();

            receipts.AddRange(poItems.Select(poi => new ScheduledReceipt
            {
                PurchaseOrderItemId = poi.Id,
                ReceiptDate = poi.PurchaseOrder.ExpectedDeliveryDate ?? horizonEnd,
                ReceiptQty = poi.Quantity,
                ReceivedQty = poi.ReceivedQty
            }));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get scheduled receipts for SKU {SkuId}", skuId);
        }

        return receipts;
    }

    public async Task<List<Sku>> GetSkusForPlanningAsync()
    {
        return await _db.Skus.AsNoTracking()
            .Where(s => s.Status == "active" && s.IsStocked)
            .OrderBy(s => s.LowLevelCode)
            .ToListAsync();
    }

    public async Task<MrpRun?> GetLatestRunAsync()
    {
        return await _db.MrpRuns.AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<MrpRun>> GetRunsAsync(int page = 1, int pageSize = 20)
    {
        return await _db.MrpRuns.AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
