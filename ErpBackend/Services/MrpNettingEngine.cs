using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface IMrpNettingEngine
{
    Task<List<MrpRequirement>> CalculateRequirementsAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog = true);
    Task<List<MrpRequirement>> CalculateRequirementsWithCacheAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog, DemandDataCache cache);
}

public class MrpNettingEngine : IMrpNettingEngine
{
    private readonly ErpGeneralDbContext _db;
    private readonly IMrpDataReader _reader;
    private readonly ISubstituteService _substituteService;
    private readonly ILogger<MrpNettingEngine> _logger;

    public MrpNettingEngine(ErpGeneralDbContext db, IMrpDataReader reader, ISubstituteService substituteService, ILogger<MrpNettingEngine> logger)
    {
        _db = db;
        _reader = reader;
        _substituteService = substituteService;
        _logger = logger;
    }

    public async Task<List<MrpRequirement>> CalculateRequirementsAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog = true)
    {
        var requirements = new List<MrpRequirement>();

        try
        {
            var sku = await _db.Skus.AsNoTracking().FirstOrDefaultAsync(s => s.Id == skuId);
            if (sku == null) return requirements;

            // 修復 MRP-002: 空值處理，防止 NullReferenceException
            var currentStock = sku.Qty;
            var safetyStock = sku.SafetyStock;
            var moq = sku.Moq > 0 ? sku.Moq : 1;
            var leadTimeDays = sku.SupplierLeadTime > 0 ? sku.SupplierLeadTime : 7;

            var demandSources = await _reader.GetDemandSourcesAsync(skuId, horizonStart, horizonEnd, includeBacklog);

            if (!demandSources.Any())
            {
                if (currentStock <= sku.ReorderPoint)
                {
                    var replenishQty = Math.Max(moq, (sku.ReorderPoint * 2) - currentStock);
                    requirements.Add(new MrpRequirement
                    {
                        SkuId = skuId,
                        RequirementType = "reorder_point",
                        RequiredDate = horizonStart,
                        RequiredQty = replenishQty,
                        AvailableQty = currentStock - safetyStock,
                        ShortageQty = Math.Max(0, replenishQty - (currentStock - safetyStock)),
                        PlannedReceiptQty = 0,
                        NetRequirement = replenishQty,
                        ActionCode = "create_pr",
                        Notes = "庫存低於再訂購點，自動產生補貨需求"
                    });
                }
                return requirements;
            }

            var sortedDemands = demandSources.OrderBy(d => d.RequiredDate).ToList();
            var availableStock = currentStock - safetyStock;
            var scheduledReceipts = await _reader.GetScheduledReceiptsAsync(skuId, horizonStart, horizonEnd);
            var scheduledReceiptTotal = scheduledReceipts.Sum(pr => pr.ReceiptQty - pr.ReceivedQty);

            foreach (var demand in sortedDemands)
            {
                try
                {
                    availableStock += scheduledReceipts
                        .Where(pr => pr.ReceiptDate <= demand.RequiredDate)
                        .Sum(pr => pr.ReceiptQty - pr.ReceivedQty);

                    var shortage = Math.Max(0, demand.RequiredQty - availableStock);
                    var netRequirement = shortage > 0 ? Math.Max(moq, shortage) : 0;

                    var plannedReceiptDate = demand.RequiredDate.AddDays(-(double)leadTimeDays);
                    var plannedReceiptQty = 0m;

                    if (netRequirement > 0)
                    {
                        var relatedReceipt = scheduledReceipts.FirstOrDefault(pr =>
                            pr.ReceiptDate > plannedReceiptDate && pr.ReceiptQty > pr.ReceivedQty);

                        if (relatedReceipt != null)
                        {
                            var availableFromReceipt = relatedReceipt.ReceiptQty - relatedReceipt.ReceivedQty;
                            if (availableFromReceipt >= netRequirement)
                            {
                                netRequirement = 0;
                                relatedReceipt.ReceivedQty += shortage;
                            }
                            else
                            {
                                netRequirement = shortage - availableFromReceipt;
                                relatedReceipt.ReceivedQty = relatedReceipt.ReceiptQty;
                            }
                        }
                    }

                    Guid? substituteSkuId = null;
                    if (netRequirement > 0)
                    {
                        try
                        {
                            var substitute = await _substituteService.FindBestSubstitute(skuId, netRequirement);
                            if (substitute != null)
                            {
                                substituteSkuId = substitute.SkuId;
                                netRequirement = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to find substitute for SKU {SkuId}, proceeding without substitution", skuId);
                        }
                    }

                    requirements.Add(new MrpRequirement
                    {
                        SkuId = skuId,
                        RequirementType = demand.RequirementType,
                        SourceDocumentId = demand.SourceDocumentId,
                        SourceDocumentNumber = demand.SourceDocumentNumber,
                        RequiredDate = demand.RequiredDate,
                        RequiredQty = demand.RequiredQty,
                        AvailableQty = availableStock,
                        ShortageQty = shortage,
                        PlannedReceiptQty = plannedReceiptQty,
                        NetRequirement = netRequirement,
                        ActionCode = netRequirement > 0 ? "create_wo" : (substituteSkuId.HasValue ? "substitute" : "none"),
                        SubstituteSkuId = substituteSkuId,
                        Notes = substituteSkuId.HasValue ? "將使用替代料" : (plannedReceiptQty > 0 ? $"預計 receipt date: {plannedReceiptDate:yyyy-MM-dd}" : null)
                    });

                    availableStock -= demand.RequiredQty;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process demand for SKU {SkuId}, skipping", skuId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate requirements for SKU {SkuId}", skuId);
        }

        return requirements;
    }

    /// <summary>
    /// 使用預載入緩存的資料計算需求（效能優化版本）
    /// </summary>
    public Task<List<MrpRequirement>> CalculateRequirementsWithCacheAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog, DemandDataCache cache)
    {
        var requirements = new List<MrpRequirement>();

        try
        {
            var currentStock = cache.SkuQtys.TryGetValue(skuId, out var qty) ? qty : 0m;
            var safetyStock = cache.SkuReorderPoints.TryGetValue(skuId, out var rop) ? rop : 0m;
            var moq = 1m;

            // 從緩存取得需求資料
            var soItems = cache.SalesOrderItems.Where(soi => soi.SkuId == skuId).ToList();
            var woMaterials = cache.WorkOrderMaterials.Where(wom => wom.SkuId == skuId).ToList();
            var forecasts = cache.DemandForecasts.Where(df => df.SkuId == skuId).ToList();
            var poItems = cache.PurchaseOrderItems.Where(poi => poi.SkuId == skuId).ToList();

            var availableStock = currentStock;

            foreach (var soi in soItems)
            {
                if (soi.Status == "pending")
                {
                    availableStock -= soi.Quantity;
                }
            }

            foreach (var wom in woMaterials)
            {
                if (wom.WorkOrder.Status == "released" || wom.WorkOrder.Status == "pending")
                {
                    var shortage = wom.RequiredQty - wom.IssuedQty;
                    if (shortage > 0)
                    {
                        requirements.Add(new MrpRequirement
                        {
                            MrpRunId = Guid.Empty,
                            SkuId = skuId,
                            RequirementType = "work_order",
                            SourceDocumentId = wom.WorkOrderId,
                            SourceDocumentNumber = wom.WorkOrder.WoNumber,
                            RequiredDate = wom.WorkOrder.PlannedStartDate ?? horizonStart,
                            RequiredQty = shortage,
                            AvailableQty = availableStock,
                            NetRequirement = Math.Max(0, shortage),
                            ActionCode = "create_wo"
                        });
                        availableStock = 0;
                    }
                }
            }

            foreach (var f in forecasts)
            {
                requirements.Add(new MrpRequirement
                {
                    MrpRunId = Guid.Empty,
                    SkuId = skuId,
                    RequirementType = "forecast",
                    RequiredDate = DateTime.Parse($"{f.ForecastMonth}-01"),
                    RequiredQty = f.ForecastQty,
                    AvailableQty = availableStock,
                    NetRequirement = Math.Max(0, f.ForecastQty - availableStock),
                    ActionCode = "none"
                });
            }

            if (currentStock <= rop)
            {
                var replenishQty = Math.Max(moq, (rop * 2) - currentStock);
                requirements.Add(new MrpRequirement
                {
                    MrpRunId = Guid.Empty,
                    SkuId = skuId,
                    RequirementType = "reorder_point",
                    RequiredDate = horizonStart,
                    RequiredQty = replenishQty,
                    AvailableQty = currentStock,
                    NetRequirement = replenishQty,
                    ActionCode = "create_pr"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "計算需求失敗 for SKU {SkuId}", skuId);
        }

        return Task.FromResult(requirements);
    }
}
