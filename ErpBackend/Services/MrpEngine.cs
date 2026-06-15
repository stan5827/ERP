using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Sales;
using ErpBackend.Models.Procurement;

namespace ErpBackend.Services;

public interface IMrpEngine
{
    Task<MrpRun> RunMrpAsync(DateTime horizonStart, DateTime horizonEnd, string runType = "regenerative", bool includeBacklog = true);
    Task<List<MrpRequirement>> CalculateRequirementsAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog = true);
    Task<MrpPlannedOrder> GeneratePlannedOrderAsync(MrpRequirement requirement);
    Task<MrpRun?> GetLatestRunAsync();
    Task<List<MrpRun>> GetRunsAsync(int page = 1, int pageSize = 20);
}

public class MrpEngine : IMrpEngine
{
    private readonly ErpGeneralDbContext _db;
    private readonly ISubstituteService _substituteService;
    private readonly ILogger<MrpEngine> _logger;
    
    public MrpEngine(ErpGeneralDbContext db, ISubstituteService substituteService, ILogger<MrpEngine> logger)
    {
        _db = db;
        _substituteService = substituteService;
        _logger = logger;
    }
    
    /// <summary>
    /// 批量預載入需求資料，避免 N+1 查詢（效能優化）
    /// </summary>
    private class DemandDataCache
    {
        public List<SalesOrderItem> SalesOrderItems { get; set; } = new();
        public List<WorkOrderMaterial> WorkOrderMaterials { get; set; } = new();
        public List<DemandForecast> DemandForecasts { get; set; } = new();
        public List<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new();
        public Dictionary<Guid, decimal> SkuQtys { get; set; } = new();
        public Dictionary<Guid, decimal> SkuReorderPoints { get; set; } = new();
    }
    
    private async Task<DemandDataCache> PreloadDemandDataAsync(List<Guid> skuIds, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog)
    {
        var cache = new DemandDataCache();
        
        // 批量查詢所有需求數據
        _logger.LogInformation("MRP 批量預載入需求數據...");
        
        // 1. 批量查詢銷售訂單
        var startMonth = horizonStart.ToString("yyyy-MM");
        var endMonth = horizonEnd.ToString("yyyy-MM");
        
        cache.SalesOrderItems = await _db.SalesOrderItems
            .Include(soi => soi.SalesOrder)
            .Where(soi => skuIds.Contains(soi.SkuId ?? Guid.Empty) 
                     && soi.Status == "pending")
            .ToListAsync();
        
        // 2. 批量查詢工單需求
        cache.WorkOrderMaterials = await _db.WorkOrderMaterials
            .Include(wom => wom.WorkOrder)
            .Where(wom => skuIds.Contains(wom.SkuId))
            .ToListAsync();
        
        // 3. 批量查詢預測
        cache.DemandForecasts = await _db.DemandForecasts
            .Where(df => skuIds.Contains(df.SkuId)
                      && df.ForecastMonth.CompareTo(startMonth) >= 0
                      && df.ForecastMonth.CompareTo(endMonth) <= 0)
            .ToListAsync();
        
        // 4. 批量查詢採購預計入庫
        cache.PurchaseOrderItems = await _db.PurchaseOrderItems
            .Include(poi => poi.PurchaseOrder)
            .Where(poi => skuIds.Contains(poi.SkuId)
                     && poi.PurchaseOrder != null && poi.PurchaseOrder.Status == "confirmed")
            .ToListAsync();
        
        // 5. 批量查詢 SKU 庫存資料
        var skus = await _db.Skus.Where(s => skuIds.Contains(s.Id)).ToListAsync();
        foreach (var sku in skus)
        {
            cache.SkuQtys[sku.Id] = sku.Qty;
            cache.SkuReorderPoints[sku.Id] = sku.ReorderPoint;
        }
        
        _logger.LogInformation($"預載入完成：SO={cache.SalesOrderItems.Count}, WO={cache.WorkOrderMaterials.Count}, 預測={cache.DemandForecasts.Count}, PO={cache.PurchaseOrderItems.Count}");
        
        return cache;
    }
    
    public async Task<MrpRun> RunMrpAsync(DateTime horizonStart, DateTime horizonEnd, string runType = "regenerative", bool includeBacklog = true)
    {
        var run = new MrpRun
        {
            RunNumber = $"MRP-{DateTime.UtcNow:yyyyMMddHHmmss}",
            RunType = runType,
            PlanningHorizonStart = horizonStart,
            PlanningHorizonEnd = horizonEnd,
            Status = "running",
            StartedAt = DateTime.UtcNow
        };
        
        _db.MrpRuns.Add(run);
        await _db.SaveChangesAsync();
        
        // 修復 MRP-008: 新增事務控制，確保數據完整性
        var strategy = _db.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1. 獲取所有需要計劃的料號（按低階碼排序）
                var skus = await GetSkusForPlanningAsync();
                run.TotalItemsProcessed = skus.Count;
                
                _logger.LogInformation($"MRP運行開始：{run.RunNumber}，處理 {skus.Count} 個料號，包含歷史未結訂單：{includeBacklog}");
                
                // 修復 MRP-005: 批量預載入需求數據，避免 N+1 查詢
                var skuIds = skus.Select(s => s.Id).ToList();
                var demandCache = await PreloadDemandDataAsync(skuIds, horizonStart, horizonEnd, includeBacklog);
                
                // 2. 對每個料號計算需求
                int successCount = 0;
                int failCount = 0;
                
                foreach (var sku in skus)
                {
                    try
                    {
                        var requirements = await CalculateRequirementsWithCacheAsync(sku.Id, horizonStart, horizonEnd, includeBacklog, demandCache);
                        
                        foreach (var req in requirements)
                        {
                            req.MrpRunId = run.Id;
                            _db.MrpRequirements.Add(req);
                            
                            if (req.NetRequirement > 0)
                            {
                                var plannedOrder = await GeneratePlannedOrderAsync(req);
                                plannedOrder.MrpRunId = run.Id;
                                _db.MrpPlannedOrders.Add(plannedOrder);
                                
                                if (plannedOrder.OrderType == "work_order")
                                {
                                    run.PlannedOrdersGenerated++;
                                    // BOM 展開與遞迴保護
                                    await ExplodeBomAsync(plannedOrder, run.Id, 0, new HashSet<Guid> { sku.Id });
                                }
                                else
                                    run.PurchaseRequisitionsGenerated++;
                            }
                        }
                        
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogWarning(ex, "Failed to process SKU {SkuNumber} in MRP run, skipping", sku.SkuNumber);
                    }
                }
                
                run.Status = "completed";
                run.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                
                await transaction.CommitAsync();
                
                _logger.LogInformation($"MRP運行完成：{run.RunNumber}，處理 {successCount} 個料號（失敗 {failCount} 個），生成 {run.PlannedOrdersGenerated} 個工單建議，{run.PurchaseRequisitionsGenerated} 個採購建議");
                
                return run;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                run.Status = "failed";
                run.ErrorMessage = ex.Message;
                run.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            
                _logger.LogError(ex, "MRP運行失敗：{RunNumber} - {Message}", run.RunNumber, ex.ToString());
                throw;
            }
        });
    }
    
    public async Task<List<MrpRequirement>> CalculateRequirementsAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog = true)
    {
        var requirements = new List<MrpRequirement>();
        
        try
        {
            var sku = await _db.Skus.FindAsync(skuId);
            if (sku == null) return requirements;
            
            // 修復 MRP-002: 空值處理，防止 NullReferenceException
            var currentStock = sku.Qty;
            var safetyStock = sku.SafetyStock;
            var moq = sku.Moq > 0 ? sku.Moq : 1;
            var leadTimeDays = sku.SupplierLeadTime > 0 ? sku.SupplierLeadTime : 7;
            
            var demandSources = await GetDemandSourcesAsync(skuId, horizonStart, horizonEnd, includeBacklog);
            
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
            var scheduledReceipts = await GetScheduledReceiptsAsync(skuId, horizonStart, horizonEnd);
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
    
    private async Task<List<DemandSource>> GetDemandSourcesAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog = true)
    {
        var sources = new List<DemandSource>();
        
        // 0. MPS 需求 (優先級最高，因為 MPS 已經處理了預測沖銷)
        try
        {
            var latestMps = await _db.MpsHeaders
                .Where(h => h.Status == "APPROVED" || h.Status == "DRAFT") // 實務上通常只用已核准，此處放寬
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestMps != null)
            {
                var mpsDemands = await _db.MpsLines
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
            IQueryable<SalesOrderItem> soQuery = _db.SalesOrderItems
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
            IQueryable<WorkOrderMaterial> woQuery = _db.WorkOrderMaterials
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
            var sku = await _db.Skus.FindAsync(skuId);
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
            var forecasts = await _db.DemandForecasts
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
            var dependentDemands = await _db.MrpRequirements
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

    /// <summary>
    /// TICKET-002: BOM 展開邏輯，包含循環偵測與深度限制
    /// </summary>
    private async Task ExplodeBomAsync(MrpPlannedOrder parentOrder, Guid runId, int depth, HashSet<Guid> visitedSkus)
    {
        const int MAX_DEPTH = 20; // 支援 20 層 BOM，超過則中止以保安全

        if (depth > MAX_DEPTH)
        {
            _logger.LogError($"BOM 展開超過最大深度 {MAX_DEPTH}，可能是資料異常。父料號 ID: {parentOrder.SkuId}");
            return;
        }

        var bom = await _db.Boms
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.ProductSkuId == parentOrder.SkuId && b.Status == "active");

        if (bom == null || !bom.Items.Any()) return;

        foreach (var item in bom.Items)
        {
            // 循環偵測
            if (visitedSkus.Contains(item.ChildSkuId))
            {
                _logger.LogCritical($"偵測到 BOM 循環依賴！料號 {item.ChildSkuId} 已在路徑中。");
                continue;
            }

            var childRequirement = new MrpRequirement
            {
                MrpRunId = runId,
                SkuId = item.ChildSkuId,
                RequirementType = "dependent",
                SourceDocumentId = parentOrder.Id,
                SourceDocumentNumber = $"PO-{parentOrder.SkuId.ToString().Substring(0,8)}", // 簡化關聯編號
                RequiredDate = parentOrder.PlannedStartDate, // 子料需要在父料開工前備齊
                RequiredQty = item.Quantity * parentOrder.OrderQty,
                ActionCode = "none",
                Notes = $"來自父工單建議的需求 (層級: {depth + 1})"
            };

            _db.MrpRequirements.Add(childRequirement);
            
            // 如果需要深度展開（實務上 MRP 通常按 LLC 處理，故此處只需存下需求，LLC 會負責下一層的計算）
            // 但若要即時檢查或進行特定模擬，可遞迴。在 LLC-based MRP 中，此處 Add 完畢即可。
        }
    }
    
    public async Task<MrpPlannedOrder> GeneratePlannedOrderAsync(MrpRequirement requirement)
    {
        var sku = await _db.Skus.FindAsync(requirement.SkuId);
        var orderType = sku?.MakeOrBuy == "buy" ? "purchase_order" : "work_order";
        
        // 計算提前期
        var leadTimeDays = orderType == "purchase_order" 
            ? (sku?.SupplierLeadTime ?? sku?.LeadTimeDays ?? 7)
            : await CalculateProductionLeadTimeAsync(requirement.SkuId, requirement.NetRequirement);
        
        var plannedEndDate = requirement.RequiredDate;
        var plannedStartDate = plannedEndDate.AddDays(-leadTimeDays);
        
        // 計算經濟批量
        var orderQty = CalculateOrderQuantity(requirement.NetRequirement, sku);
        
        return new MrpPlannedOrder
        {
            SkuId = requirement.SkuId,
            OrderType = orderType,
            OrderQty = orderQty,
            PlannedStartDate = plannedStartDate,
            PlannedEndDate = plannedEndDate,
            SourceRequirementId = requirement.Id,
            Status = "suggested",
            Notes = $"由MRP運行自動生成，需求日期：{requirement.RequiredDate:yyyy/MM/dd}"
        };
    }
    
    public async Task<MrpRun?> GetLatestRunAsync()
    {
        return await _db.MrpRuns
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync();
    }
    
    public async Task<List<MrpRun>> GetRunsAsync(int page = 1, int pageSize = 20)
    {
        return await _db.MrpRuns
            .OrderByDescending(r => r.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    
    private decimal CalculateOrderQuantity(decimal netRequirement, Sku? sku)
    {
        if (sku == null) return netRequirement;
        
        var moq = sku.Moq;
        if (moq > 0 && netRequirement < moq)
        {
            var multiples = Math.Ceiling(netRequirement / moq);
            return moq * multiples;
        }
        
        return netRequirement;
    }
    
    private async Task<int> CalculateProductionLeadTimeAsync(Guid skuId, decimal qty)
    {
        var routing = await _db.Routings
            .Include(r => r.Operations)
            .ThenInclude(ro => ro.Operation)
            .FirstOrDefaultAsync(r => r.ProductSkuId == skuId && r.IsActive);
        
        if (routing == null) return 7;
        
        decimal totalHours = 0;
        foreach (var op in routing.Operations.OrderBy(o => o.OperationSeq))
        {
            var setupTime = op.Operation?.SetupTime ?? 0;
            var runTime = (op.Operation?.RunTime ?? 0) * qty;
            totalHours += setupTime + runTime;
        }
        
        return (int)Math.Ceiling(totalHours / 8);
    }
    
    private async Task<List<Sku>> GetSkusForPlanningAsync()
    {
        return await _db.Skus
            .Where(s => s.Status == "active" && s.IsStocked)
            .OrderBy(s => s.LowLevelCode)
            .ToListAsync();
    }
    
    /// <summary>
    /// 使用預載入緩存的資料計算需求（效能優化版本）
    /// </summary>
    private Task<List<MrpRequirement>> CalculateRequirementsWithCacheAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog, DemandDataCache cache)
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
    
    private async Task<List<ScheduledReceipt>> GetScheduledReceiptsAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd)
    {
        var receipts = new List<ScheduledReceipt>();
        
        try
        {
            var poItems = await _db.PurchaseOrderItems
                .Include(poi => poi.PurchaseOrder)
                .Where(poi => poi.SkuId == skuId
                           && poi.PurchaseOrder.Status == "confirmed"
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
    
    private class ScheduledReceipt
    {
        public Guid PurchaseOrderItemId { get; set; }
        public DateTime ReceiptDate { get; set; }
        public decimal ReceiptQty { get; set; }
        public decimal ReceivedQty { get; set; }
    }
}