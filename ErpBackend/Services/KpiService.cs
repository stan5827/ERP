using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.System;

namespace ErpBackend.Services;

public interface IKpiService
{
    Task<KpiDashboardDto> GetDashboardAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<OeeResultDto> CalculateOeeAsync(Guid workCenterId, DateTime date, int? shift = null);
    Task<OtdResultDto> CalculateOtdAsync(DateTime fromDate, DateTime toDate);
    Task<WipResultDto> CalculateWipAsync();
    Task<ScrapResultDto> CalculateScrapAsync(DateTime fromDate, DateTime toDate);
    Task<List<KpiTrendDto>> GetKpiTrendAsync(string kpiCode, DateTime fromDate, DateTime toDate, string periodType = "day");
    Task<List<KpiDefinition>> GetKpiDefinitionsAsync();
    Task<KpiDefinition> CreateKpiDefinitionAsync(KpiDefinition definition);
    Task<List<OeeSummaryDto>> GetAllOeeSummaryAsync(DateTime date);
}

public class KpiService : IKpiService
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<KpiService> _logger;
    private readonly ICacheService _cache;
    
    private static readonly TimeSpan DashboardCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TrendCacheDuration = TimeSpan.FromMinutes(30);
    
    public KpiService(ErpGeneralDbContext db, ILogger<KpiService> logger, ICacheService cache)
    {
        _db = db;
        _logger = logger;
        _cache = cache;
    }
    
public async Task<KpiDashboardDto> GetDashboardAsync(DateTime? fromDate = null, DateTime? toDate = null)
{
    var cacheKey = $"kpi:dashboard:{fromDate?.ToString("yyyyMMdd")}:{toDate?.ToString("yyyyMMdd")}";
    
    // Try to get from cache first
    var cached = await _cache.GetAsync<KpiDashboardDto>(cacheKey);
    if (cached != null)
        return cached;
    
    var end = toDate ?? DateTime.UtcNow;
    var start = fromDate ?? end.AddDays(-30);
    
    var dashboard = new KpiDashboardDto
    {
        PeriodStart = start,
        PeriodEnd = end,
        OeeByWorkCenter = new List<OeeSummaryDto>()
    };
    
    // Batch query all OEE - single query instead of N+1
    var oeeSummaries = await GetAllOeeSummaryAsync(end.Date);
    dashboard.OeeByWorkCenter = oeeSummaries;
    dashboard.AverageOee = oeeSummaries.Any() ? oeeSummaries.Average(o => o.OeeValue) : 0;
        
        // 計算OTD
        var otd = await CalculateOtdAsync(start, end);
        dashboard.OnTimeDeliveryRate = otd.OnTimeRate;
        dashboard.TotalOrders = otd.TotalOrders;
        dashboard.OnTimeOrders = otd.OnTimeOrders;
        dashboard.DelayedOrders = otd.DelayedOrders;
        
        // 計算WIP
        var wip = await CalculateWipAsync();
        dashboard.WipQuantity = wip.TotalWipQuantity;
        dashboard.WipValue = wip.TotalWipValue;
        dashboard.WipByWorkCenter = wip.WipByWorkCenter;
        
        // 計算報廢 - 檢查相關表格是否存在
        try
        {
            var scrap = await CalculateScrapAsync(start, end);
            dashboard.ScrapQuantity = scrap.TotalScrapQuantity;
            dashboard.ScrapAmount = scrap.TotalScrapAmount;
            dashboard.ScrapByReason = scrap.ScrapByReason;
            dashboard.TopScrapItems = scrap.TopScrapItems;
        }
        catch
        {
            dashboard.ScrapQuantity = 0;
            dashboard.ScrapAmount = 0;
            dashboard.ScrapByReason = new Dictionary<string, decimal>();
            dashboard.TopScrapItems = new List<TopScrapItemDto>();
        }
        
        // 計算產能利用率
        try
        {
            dashboard.CapacityUtilization = await CalculateCapacityUtilizationAsync(start, end);
        }
        catch
        {
            dashboard.CapacityUtilization = 0;
        }
        
        // 獲取趨勢數據 - 檢查表格是否存在
        try
        {
            dashboard.OeeTrend = await GetKpiTrendAsync("OEE", start, end, "day");
            dashboard.OtdTrend = await GetKpiTrendAsync("OTD", start, end, "day");
        }
        catch
        {
            dashboard.OeeTrend = new List<KpiTrendDto>();
            dashboard.OtdTrend = new List<KpiTrendDto>();
        }

        // Cache the result
        await _cache.SetAsync(cacheKey, dashboard, DashboardCacheDuration);
        
        return dashboard;
    }
    
    public async Task<List<OeeSummaryDto>> GetAllOeeSummaryAsync(DateTime date)
    {
        // Batch query - single query for all work centers
        var workCenters = await _db.WorkCenters.Where(wc => wc.IsActive).ToListAsync();
        var workCenterIds = workCenters.Select(wc => wc.Id).ToList();
        
        if (!workCenterIds.Any())
            return new List<OeeSummaryDto>();
        
        var records = await _db.MachineOperationRecords
            .Where(r => r.RecordDate.Date == date.Date)
            .ToListAsync();
        
        var results = new List<OeeSummaryDto>();
        
        foreach (var wc in workCenters)
        {
            var wcRecords = records.Where(r => r.WorkCenterId == wc.Id).ToList();
            
            decimal availability = 0, performance = 0, quality = 0, oeeValue = 0;
            
            if (wcRecords.Any())
            {
                var totalPlannedTime = wcRecords.Sum(r => r.PlannedRunTime);
                var totalActualRunTime = wcRecords.Sum(r => r.ActualRunTime);
                availability = totalPlannedTime > 0 ? (totalActualRunTime / totalPlannedTime) * 100 : 0;
                
                var totalTheoreticalOutput = wcRecords.Sum(r => r.TheoreticalOutput);
                var totalActualOutput = wcRecords.Sum(r => r.ActualOutput);
                performance = totalTheoreticalOutput > 0 ? (totalActualOutput / totalTheoreticalOutput) * 100 : 0;
                
                var totalGoodOutput = wcRecords.Sum(r => r.GoodOutput);
                quality = totalActualOutput > 0 ? (totalGoodOutput / totalActualOutput) * 100 : 0;
                
                oeeValue = (availability / 100) * (performance / 100) * (quality / 100) * 100;
            }
            
            results.Add(new OeeSummaryDto
            {
                WorkCenterId = wc.Id,
                WorkCenterName = wc.WorkCenterName,
                Availability = Math.Round(availability, 2),
                Performance = Math.Round(performance, 2),
                Quality = Math.Round(quality, 2),
                OeeValue = Math.Round(oeeValue, 2),
                Status = GetOeeStatus(oeeValue)
            });
        }
        
        return results;
    }
    
    public async Task<OeeResultDto> CalculateOeeAsync(Guid workCenterId, DateTime date, int? shift = null)
    {
        var query = _db.MachineOperationRecords
            .Where(r => r.WorkCenterId == workCenterId && r.RecordDate.Date == date.Date);
        
        if (shift.HasValue)
            query = query.Where(r => r.ShiftNumber == shift.Value);
        
        var records = await query.ToListAsync();
        
        if (!records.Any())
        {
            return new OeeResultDto
            {
                WorkCenterId = workCenterId,
                RecordDate = date,
                Availability = 0,
                Performance = 0,
                Quality = 0,
                OeeValue = 0
            };
        }
        
        // 計算稼動率 (Availability) = 實際運行時間 / 計劃運行時間
        var totalPlannedTime = records.Sum(r => r.PlannedRunTime);
        var totalActualRunTime = records.Sum(r => r.ActualRunTime);
        var availability = totalPlannedTime > 0 ? (totalActualRunTime / totalPlannedTime) * 100 : 0;
        
        // 計算性能效率 (Performance) = 實際產出 / (實際運行時間 × 理論產能)
        var totalTheoreticalOutput = records.Sum(r => r.TheoreticalOutput);
        var totalActualOutput = records.Sum(r => r.ActualOutput);
        var performance = totalTheoreticalOutput > 0 ? (totalActualOutput / totalTheoreticalOutput) * 100 : 0;
        
        // 計算品質率 (Quality) = 良品數量 / 總產出數量
        var totalGoodOutput = records.Sum(r => r.GoodOutput);
        var quality = totalActualOutput > 0 ? (totalGoodOutput / totalActualOutput) * 100 : 0;
        
        // 計算OEE = 稼動率 × 性能效率 × 品質率
        var oeeValue = (availability / 100) * (performance / 100) * (quality / 100) * 100;
        
        return new OeeResultDto
        {
            WorkCenterId = workCenterId,
            RecordDate = date,
            Shift = shift,
            PlannedRunTime = totalPlannedTime,
            ActualRunTime = totalActualRunTime,
            Downtime = records.Sum(r => r.Downtime),
            SetupTime = records.Sum(r => r.SetupTime),
            WaitTime = records.Sum(r => r.WaitTime),
            TheoreticalOutput = totalTheoreticalOutput,
            ActualOutput = totalActualOutput,
            GoodOutput = totalGoodOutput,
            DefectOutput = records.Sum(r => r.DefectOutput),
            Availability = Math.Round(availability, 2),
            Performance = Math.Round(performance, 2),
            Quality = Math.Round(quality, 2),
            OeeValue = Math.Round(oeeValue, 2)
        };
    }
    
    public async Task<OtdResultDto> CalculateOtdAsync(DateTime fromDate, DateTime toDate)
    {
        // 獲取已完成的工單
        var completedOrders = await _db.WorkOrders
            .Where(wo => wo.Status == "completed" || wo.Status == "closed")
            .Where(wo => wo.ActualEndDate >= fromDate && wo.ActualEndDate <= toDate)
            .ToListAsync();
        
        var totalOrders = completedOrders.Count;
        var onTimeOrders = completedOrders.Count(wo => 
            wo.ActualEndDate <= wo.PlannedEndDate || 
            (wo.ActualEndDate.HasValue && wo.PlannedEndDate.HasValue && wo.ActualEndDate.Value <= wo.PlannedEndDate.Value));
        
        var delayedOrders = totalOrders - onTimeOrders;
        var onTimeRate = totalOrders > 0 ? (decimal)onTimeOrders / totalOrders * 100 : 0;
        
        // 計算平均延遲天數
        var delayedOrderList = completedOrders
            .Where(wo => wo.ActualEndDate > wo.PlannedEndDate)
            .ToList();
        
        var avgDelayDays = delayedOrderList.Any() 
            ? delayedOrderList.Average(wo => 
                (wo.ActualEndDate?.Date - wo.PlannedEndDate?.Date)?.Days ?? 0)
            : 0;
        
        return new OtdResultDto
        {
            PeriodStart = fromDate,
            PeriodEnd = toDate,
            TotalOrders = totalOrders,
            OnTimeOrders = onTimeOrders,
            DelayedOrders = delayedOrders,
            OnTimeRate = Math.Round(onTimeRate, 2),
            AverageDelayDays = (decimal)Math.Round(avgDelayDays, 2)
        };
    }
    
    public async Task<WipResultDto> CalculateWipAsync()
    {
        // 獲取所有進行中的工單
        var wipOrders = await _db.WorkOrders
            .Where(wo => wo.Status == "in_progress" || wo.Status == "released")
            .ToListAsync();
        
        var totalWipQuantity = wipOrders.Sum(wo => wo.PlannedQty - wo.ActualQty);
        
        // 計算WIP價值（使用標準成本）
        decimal totalWipValue = 0;
        var wipByWorkCenter = new Dictionary<Guid, decimal>();
        
        foreach (var order in wipOrders)
        {
            var sku = await _db.Skus.FindAsync(order.ProductSkuId);
            if (sku != null)
            {
                var remainingQty = order.PlannedQty - order.ActualQty;
                totalWipValue += remainingQty * sku.StandardCost;
            }
        }
        
        // 按工作中心統計WIP
        var wipTasks = await _db.ScheduleTasks
            .Include(t => t.WorkOrder)
            .Include(t => t.WorkCenter)
            .Where(t => t.Status == "planned" || t.Status == "in_progress")
            .GroupBy(t => t.WorkCenterId)
            .Select(g => new { WorkCenterId = g.Key, WipQuantity = g.Sum(t => t.Quantity) })
            .ToListAsync();
        
        foreach (var item in wipTasks)
        {
            wipByWorkCenter[item.WorkCenterId] = item.WipQuantity;
        }
        
        // 計算月平均產能
        var monthlyCapacity = await CalculateMonthlyCapacityAsync();
        var wipRatio = monthlyCapacity > 0 ? (totalWipQuantity / monthlyCapacity) * 100 : 0;
        
        return new WipResultDto
        {
            TotalWipQuantity = totalWipQuantity,
            TotalWipValue = totalWipValue,
            WipRatio = Math.Round(wipRatio, 2),
            WipByWorkCenter = wipByWorkCenter,
            WipOrdersCount = wipOrders.Count
        };
    }
    
    public async Task<ScrapResultDto> CalculateScrapAsync(DateTime fromDate, DateTime toDate)
    {
        // 從報工單獲取報廢數據
        var wipReports = await _db.WipReports
            .Where(wr => wr.CreatedAt >= fromDate && wr.CreatedAt <= toDate)
            .ToListAsync();
        
        var totalScrapQuantity = wipReports.Sum(wr => wr.ScrapQty);
        
        // 按原因統計
        var scrapByReason = wipReports
            .Where(wr => !string.IsNullOrEmpty(wr.ShiftClass))
            .GroupBy(wr => wr.ShiftClass)
            .ToDictionary(g => g.Key ?? "unknown", g => g.Sum(wr => wr.ScrapQty));
        
        // 計算報廢成本（需要物料成本信息）
        decimal totalScrapAmount = 0;
        
        // 獲取前5大報廢料號（簡化版，不使用導航屬性）
        var scrapRecords = await _db.ScrapRecords
            .Where(sr => sr.CreatedAt >= fromDate && sr.CreatedAt <= toDate)
            .ToListAsync();
        
        var skuIds = scrapRecords.Select(sr => sr.SkuId).Distinct().ToList();
        var skus = await _db.Skus
            .Where(s => skuIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);
        
        var topScrapItems = scrapRecords
            .GroupBy(sr => sr.SkuId)
            .Select(g => new TopScrapItemDto
            {
                SkuId = g.Key,
                SkuNumber = skus.GetValueOrDefault(g.Key)?.SkuNumber,
                SkuName = skus.GetValueOrDefault(g.Key)?.SkuName,
                ScrapQuantity = g.Sum(sr => sr.ScrapQty),
                ScrapAmount = g.Sum(sr => sr.CostAmount)
            })
            .OrderByDescending(t => t.ScrapAmount)
            .Take(5)
            .ToList();
        
        totalScrapAmount = topScrapItems.Sum(t => t.ScrapAmount);
        
        return new ScrapResultDto
        {
            PeriodStart = fromDate,
            PeriodEnd = toDate,
            TotalScrapQuantity = totalScrapQuantity,
            TotalScrapAmount = totalScrapAmount,
            ScrapByReason = scrapByReason,
            TopScrapItems = topScrapItems
        };
    }
    
    public async Task<List<KpiTrendDto>> GetKpiTrendAsync(string kpiCode, DateTime fromDate, DateTime toDate, string periodType = "day")
    {
        var definition = await _db.KpiDefinitions
            .FirstOrDefaultAsync(k => k.KpiCode == kpiCode);
        
        if (definition == null)
            return new List<KpiTrendDto>();
        
        var values = await _db.KpiValues
            .Where(v => v.KpiDefinitionId == definition.Id
                     && v.PeriodStart >= fromDate
                     && v.PeriodEnd <= toDate
                     && v.PeriodType == periodType)
            .OrderBy(v => v.PeriodStart)
            .ToListAsync();
        
        return values.Select(v => new KpiTrendDto
        {
            PeriodStart = v.PeriodStart,
            PeriodEnd = v.PeriodEnd,
            Value = v.Value,
            TargetValue = v.TargetValue,
            AchievementRate = v.AchievementRate,
            Status = v.Status
        }).ToList();
    }
    
    public async Task<List<KpiDefinition>> GetKpiDefinitionsAsync()
    {
        return await _db.KpiDefinitions
            .Where(k => k.IsActive)
            .OrderBy(k => k.Category)
            .ThenBy(k => k.KpiName)
            .ToListAsync();
    }
    
    public async Task<KpiDefinition> CreateKpiDefinitionAsync(KpiDefinition definition)
    {
        definition.CreatedAt = DateTime.UtcNow;
        definition.UpdatedAt = DateTime.UtcNow;
        
        _db.KpiDefinitions.Add(definition);
        await _db.SaveChangesAsync();
        
        return definition;
    }
    
    private async Task<decimal> CalculateCapacityUtilizationAsync(DateTime fromDate, DateTime toDate)
    {
        var loads = await _db.CapacityLoads
            .Where(l => l.LoadDate >= fromDate && l.LoadDate <= toDate)
            .ToListAsync();
        
        if (!loads.Any())
            return 0;
        
        var totalAvailable = loads.Sum(l => l.AvailableHours);
        var totalLoaded = loads.Sum(l => l.LoadedHours);
        
        return totalAvailable > 0 ? Math.Round((totalLoaded / totalAvailable) * 100, 2) : 0;
    }
    
    private async Task<decimal> CalculateMonthlyCapacityAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        
        var tasks = await _db.ScheduleTasks
            .Where(t => t.PlannedStart >= monthStart && t.PlannedStart <= monthEnd)
            .SumAsync(t => t.Quantity);
        
        return tasks;
    }
    
    private string GetOeeStatus(decimal oeeValue)
    {
        if (oeeValue >= 85) return "excellent";
        if (oeeValue >= 70) return "good";
        if (oeeValue >= 50) return "warning";
        return "critical";
    }
}

// DTOs
public class KpiDashboardDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    // OEE
    public decimal AverageOee { get; set; }
    public List<OeeSummaryDto> OeeByWorkCenter { get; set; } = new();
    public List<KpiTrendDto> OeeTrend { get; set; } = new();
    
    // OTD
    public decimal OnTimeDeliveryRate { get; set; }
    public int TotalOrders { get; set; }
    public int OnTimeOrders { get; set; }
    public int DelayedOrders { get; set; }
    public List<KpiTrendDto> OtdTrend { get; set; } = new();
    
    // WIP
    public decimal WipQuantity { get; set; }
    public decimal WipValue { get; set; }
    public Dictionary<Guid, decimal> WipByWorkCenter { get; set; } = new();
    
    // Scrap
    public decimal ScrapQuantity { get; set; }
    public decimal ScrapAmount { get; set; }
    public Dictionary<string, decimal> ScrapByReason { get; set; } = new();
    public List<TopScrapItemDto> TopScrapItems { get; set; } = new();
    
    // Capacity
    public decimal CapacityUtilization { get; set; }
}

public class OeeSummaryDto
{
    public Guid WorkCenterId { get; set; }
    public string WorkCenterName { get; set; } = null!;
    public decimal Availability { get; set; }
    public decimal Performance { get; set; }
    public decimal Quality { get; set; }
    public decimal OeeValue { get; set; }
    public string Status { get; set; } = null!;
}

public class OeeResultDto
{
    public Guid WorkCenterId { get; set; }
    public DateTime RecordDate { get; set; }
    public int? Shift { get; set; }
    public decimal PlannedRunTime { get; set; }
    public decimal ActualRunTime { get; set; }
    public decimal Downtime { get; set; }
    public decimal SetupTime { get; set; }
    public decimal WaitTime { get; set; }
    public decimal TheoreticalOutput { get; set; }
    public decimal ActualOutput { get; set; }
    public decimal GoodOutput { get; set; }
    public decimal DefectOutput { get; set; }
    public decimal Availability { get; set; }
    public decimal Performance { get; set; }
    public decimal Quality { get; set; }
    public decimal OeeValue { get; set; }
}

public class OtdResultDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalOrders { get; set; }
    public int OnTimeOrders { get; set; }
    public int DelayedOrders { get; set; }
    public decimal OnTimeRate { get; set; }
    public decimal AverageDelayDays { get; set; }
}

public class WipResultDto
{
    public decimal TotalWipQuantity { get; set; }
    public decimal TotalWipValue { get; set; }
    public decimal WipRatio { get; set; }
    public Dictionary<Guid, decimal> WipByWorkCenter { get; set; } = new();
    public int WipOrdersCount { get; set; }
}

public class ScrapResultDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalScrapQuantity { get; set; }
    public decimal TotalScrapAmount { get; set; }
    public Dictionary<string, decimal> ScrapByReason { get; set; } = new();
    public List<TopScrapItemDto> TopScrapItems { get; set; } = new();
}

public class TopScrapItemDto
{
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public decimal ScrapQuantity { get; set; }
    public decimal ScrapAmount { get; set; }
}

public class KpiTrendDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Value { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? AchievementRate { get; set; }
    public string Status { get; set; } = null!;
}