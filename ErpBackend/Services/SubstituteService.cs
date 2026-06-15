using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface ISubstituteService
{
    Task<List<SubstituteItem>> GetAvailableSubstitutes(Guid skuId, decimal requiredQty, DateTime? requiredDate = null);
    Task<SubstituteItem?> FindBestSubstitute(Guid skuId, decimal requiredQty, Dictionary<string, object>? conditions = null);
    Task<bool> ValidateSubstitution(Guid originalSkuId, Guid substituteSkuId, decimal qty);
    Task LogSubstitution(Guid originalSkuId, Guid substituteSkuId, Guid workOrderId, decimal qty, string? reason, decimal costVariance = 0);
    Task<List<SubstituteUsageLog>> GetSubstitutionHistory(Guid skuId, DateTime? fromDate = null);
    Task<decimal> CalculateCostVariance(Guid originalSkuId, Guid substituteSkuId, decimal qty);
}

public class SubstituteService : ISubstituteService
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<SubstituteService> _logger;
    
    public SubstituteService(ErpGeneralDbContext db, ILogger<SubstituteService> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public async Task<List<SubstituteItem>> GetAvailableSubstitutes(Guid skuId, decimal requiredQty, DateTime? requiredDate = null)
    {
        var now = DateTime.UtcNow;
        var date = requiredDate ?? now;
        
        // 查找該料號所屬的所有替代料組
        var substituteGroupIds = await _db.SubstituteItems
            .Where(si => si.SkuId == skuId && si.Status == "active")
            .Select(si => si.GroupId)
            .Distinct()
            .ToListAsync();
        
        if (!substituteGroupIds.Any())
            return new List<SubstituteItem>();
        
        // 獲取組內所有替代料，排除自己
        var substitutes = await _db.SubstituteItems
            .Include(si => si.Sku)
            .Include(si => si.Group)
            .Where(si => substituteGroupIds.Contains(si.GroupId) 
                        && si.SkuId != skuId
                        && si.Status == "active"
                        && (si.EffectiveFrom == null || si.EffectiveFrom <= date)
                        && (si.EffectiveTo == null || si.EffectiveTo >= date)
                        && si.Group.IsActive
                        && si.Group.AllowAutoSubstitute)
            .OrderBy(si => si.Priority)
            .ToListAsync();
        
        // 過濾庫存足夠的替代料
        var availableSubstitutes = new List<SubstituteItem>();
        foreach (var sub in substitutes)
        {
            var availableQty = await GetAvailableQuantity(sub.SkuId);
            var requiredSubQty = requiredQty * (sub.ConversionRate ?? 1);
            
            if (availableQty >= requiredSubQty)
            {
                availableSubstitutes.Add(sub);
            }
        }
        
        return availableSubstitutes;
    }
    
    public async Task<SubstituteItem?> FindBestSubstitute(Guid skuId, decimal requiredQty, Dictionary<string, object>? conditions = null)
    {
        var availableSubstitutes = await GetAvailableSubstitutes(skuId, requiredQty);
        
        if (!availableSubstitutes.Any())
            return null;
        
        // 根據優先級返回最佳替代料
        return availableSubstitutes.OrderBy(s => s.Priority).First();
    }
    
    public async Task<bool> ValidateSubstitution(Guid originalSkuId, Guid substituteSkuId, decimal qty)
    {
        // 檢查替代料是否存在於同一個群組中
        var originalGroups = await _db.SubstituteItems
            .Where(si => si.SkuId == originalSkuId && si.Status == "active")
            .Select(si => si.GroupId)
            .ToListAsync();
        
        var substituteItem = await _db.SubstituteItems
            .Include(si => si.Group)
            .Where(si => si.SkuId == substituteSkuId 
                      && si.Status == "active"
                      && originalGroups.Contains(si.GroupId))
            .FirstOrDefaultAsync();
        
        if (substituteItem == null)
            return false;
        
        // 檢查庫存是否足夠
        var availableQty = await GetAvailableQuantity(substituteSkuId);
        var requiredSubQty = qty * (substituteItem.ConversionRate ?? 1);
        
        return availableQty >= requiredSubQty;
    }
    
    public async Task LogSubstitution(Guid originalSkuId, Guid substituteSkuId, Guid workOrderId, decimal qty, string? reason, decimal costVariance = 0)
    {
        var originalSku = await _db.Skus.FindAsync(originalSkuId);
        var substituteSku = await _db.Skus.FindAsync(substituteSkuId);
        
        if (originalSku == null || substituteSku == null)
            throw new ArgumentException("找不到原始料號或替代料號");
        
        // 計算成本差異
        if (costVariance == 0)
        {
            costVariance = await CalculateCostVariance(originalSkuId, substituteSkuId, qty);
        }
        
        var usageLog = new SubstituteUsageLog
        {
            OriginalSkuId = originalSkuId,
            SubstituteSkuId = substituteSkuId,
            WorkOrderId = workOrderId,
            OriginalQty = qty,
            SubstituteQty = qty,
            CostVariance = costVariance,
            Reason = reason ?? "shortage",
            Notes = $"替代料使用紀錄：{originalSku.SkuNumber} -> {substituteSku.SkuNumber}"
        };
        
        _db.SubstituteUsageLogs.Add(usageLog);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation($"替代料使用記錄已創建：{originalSku.SkuNumber} -> {substituteSku.SkuNumber}，數量：{qty}");
    }
    
    public async Task<List<SubstituteUsageLog>> GetSubstitutionHistory(Guid skuId, DateTime? fromDate = null)
    {
        var query = _db.SubstituteUsageLogs
            .Include(log => log.OriginalSku)
            .Include(log => log.SubstituteSku)
            .Include(log => log.WorkOrder)
            .Where(log => log.OriginalSkuId == skuId || log.SubstituteSkuId == skuId);
        
        if (fromDate.HasValue)
            query = query.Where(log => log.CreatedAt >= fromDate.Value);
        
        return await query.OrderByDescending(log => log.CreatedAt).ToListAsync();
    }
    
    public async Task<decimal> CalculateCostVariance(Guid originalSkuId, Guid substituteSkuId, decimal qty)
    {
        var originalSku = await _db.Skus.FindAsync(originalSkuId);
        var substituteSku = await _db.Skus.FindAsync(substituteSkuId);
        
        if (originalSku == null || substituteSku == null)
            return 0;
        
        var originalCost = originalSku.StandardCost * qty;
        var substituteCost = substituteSku.StandardCost * qty;
        
        return substituteCost - originalCost;
    }
    
    private async Task<decimal> GetAvailableQuantity(Guid skuId)
    {
        var sku = await _db.Skus.FindAsync(skuId);
        if (sku == null) return 0;
        
        var reservedQty = 0m;
        try
        {
            reservedQty = await _db.WorkOrderMaterials
                .Where(wom => wom.SkuId == skuId && wom.Status == "pending")
                .SumAsync(wom => wom.RequiredQty - wom.IssuedQty);
        }
        catch
        {
            reservedQty = 0;
        }
        
        return Math.Max(0, sku.Qty - sku.SafetyStock - reservedQty);
    }
}