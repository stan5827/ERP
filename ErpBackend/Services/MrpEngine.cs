using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ErpBackend.Data;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

    public interface IMrpEngine
    {
        Task<MrpRun> RunMrpAsync(DateTime horizonStart, DateTime horizonEnd, string runType = "regenerative", bool includeBacklog = true);
        Task<List<MrpRequirement>> CalculateRequirementsAsync(Guid skuId, DateTime horizonStart, DateTime horizonEnd, bool includeBacklog = true);
        Task<MrpPlannedOrder> GeneratePlannedOrderAsync(MrpRequirement requirement);
        Task<MrpRun?> GetLatestRunAsync();
        Task<List<MrpRun>> GetRunsAsync(int page = 1, int pageSize = 20);
        Task<List<MrpPlannedOrder>> GetPlannedOrdersAsync(Guid runId, string? search = null);
        Task<object?> GetMpsSummaryAsync();
    }

public class MrpEngine : IMrpEngine
{
    private readonly ErpGeneralDbContext _db;
    private readonly IMrpDataReader _reader;
    private readonly IPlannedOrderEngine _plannedOrder;
    private readonly IMrpNettingEngine _netting;
    private readonly ILogger<MrpEngine> _logger;

    public MrpEngine(ErpGeneralDbContext db, IMrpDataReader reader, IPlannedOrderEngine plannedOrder,
        IMrpNettingEngine netting, ILogger<MrpEngine> logger)
    {
        _db = db;
        _reader = reader;
        _plannedOrder = plannedOrder;
        _netting = netting;
        _logger = logger;
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
                var skus = await _reader.GetSkusForPlanningAsync();
                run.TotalItemsProcessed = skus.Count;

                _logger.LogInformation($"MRP運行開始：{run.RunNumber}，處理 {skus.Count} 個料號，包含歷史未結訂單：{includeBacklog}");

                // 修復 MRP-005: 批量預載入需求數據，避免 N+1 查詢
                var skuIds = skus.Select(s => s.Id).ToList();
                var demandCache = await _reader.PreloadDemandDataAsync(skuIds, horizonStart, horizonEnd, includeBacklog);

                // 2. 對每個料號計算需求
                int successCount = 0;
                int failCount = 0;

                foreach (var sku in skus)
                {
                    try
                    {
                        var requirements = await _netting.CalculateRequirementsWithCacheAsync(
                            sku.Id, horizonStart, horizonEnd, includeBacklog, demandCache);

                        foreach (var req in requirements)
                        {
                            req.MrpRunId = run.Id;
                            _db.MrpRequirements.Add(req);

                            if (req.NetRequirement > 0)
                            {
                                var plannedOrder = await _plannedOrder.GeneratePlannedOrderAsync(req);
                                plannedOrder.MrpRunId = run.Id;
                                _db.MrpPlannedOrders.Add(plannedOrder);

                                if (plannedOrder.OrderType == "work_order")
                                {
                                    run.PlannedOrdersGenerated++;
                                    // BOM 展開與遞迴保護
                                    await _plannedOrder.ExplodeBomAsync(plannedOrder, run.Id, 0, new HashSet<Guid> { sku.Id });
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
        => await _netting.CalculateRequirementsAsync(skuId, horizonStart, horizonEnd, includeBacklog);

    public async Task<MrpPlannedOrder> GeneratePlannedOrderAsync(MrpRequirement requirement)
        => await _plannedOrder.GeneratePlannedOrderAsync(requirement);

    public async Task<MrpRun?> GetLatestRunAsync()
        => await _reader.GetLatestRunAsync();

    public async Task<List<MrpRun>> GetRunsAsync(int page = 1, int pageSize = 20)
        => await _reader.GetRunsAsync(page, pageSize);

    public async Task<List<MrpPlannedOrder>> GetPlannedOrdersAsync(Guid runId, string? search = null)
    {
        var query = _db.MrpPlannedOrders
            .Include(o => o.Sku)
            .Where(o => o.MrpRunId == runId)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(o => o.Sku.SkuNumber.ToLower().Contains(searchLower) || o.Sku.SkuName.ToLower().Contains(searchLower));
        }

        return await query.OrderBy(o => o.PlannedStartDate).ToListAsync();
    }

    public async Task<object?> GetMpsSummaryAsync()
    {
        var latestRun = await _reader.GetLatestRunAsync();
        if (latestRun == null) return new { hasRuns = false };

        var plannedOrders = await _db.MrpPlannedOrders
            .Where(o => o.MrpRunId == latestRun.Id)
            .GroupBy(o => o.OrderType)
            .Select(g => new { Type = g.Key, Count = g.Count(), Qty = g.Sum(x => x.OrderQty) })
            .ToListAsync();

        return new
        {
            hasRuns = true,
            latestRunNo = latestRun.RunNumber,
            completedAt = latestRun.CompletedAt,
            plannedOrders
        };
    }
}
