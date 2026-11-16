using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;

namespace ErpBackend.Services;

public interface IBudgetControlService
{
    Task<BudgetOverrunCheck> CheckOverrunAsync(string costCenterCode, decimal amount);
    Task<List<BudgetAlertDto>> GetActiveAlertsAsync();
}

public class BudgetControlService : IBudgetControlService
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<BudgetControlService> _logger;

    public BudgetControlService(ErpGeneralDbContext db, ILogger<BudgetControlService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 修復 COST-004: 檢查預算是否會超支
    /// </summary>
    public async Task<BudgetOverrunCheck> CheckOverrunAsync(string costCenterCode, decimal amount)
    {
        var result = new BudgetOverrunCheck { Amount = amount, CostCenterCode = costCenterCode };

        var costCenter = await _db.CostCenters
            .FirstOrDefaultAsync(c => c.CostCenterCode == costCenterCode && c.IsActive);

        if (costCenter == null)
        {
            result.Status = "no_budget";
            return result;
        }

        result.BudgetAmount = costCenter.BudgetLimit;
        result.PreviousUsed = costCenter.BudgetUsed;
        result.ProjectedTotal = costCenter.BudgetUsed + amount;
        result.RemainingAfter = costCenter.BudgetLimit - (costCenter.BudgetUsed + amount);

        var utilizationPercent = costCenter.BudgetLimit > 0 
            ? (costCenter.BudgetUsed + amount) / costCenter.BudgetLimit * 100 
            : 0;

        if (utilizationPercent >= 100)
        {
            result.Status = "exceeded";
            result.WarningLevel = "error";
            result.Message = $"預算已超支！使用後將達 {utilizationPercent:F1}%";
        }
        else if (utilizationPercent >= 90)
        {
            result.Status = "warning";
            result.WarningLevel = "high";
            result.Message = $"預算使用達 {utilizationPercent:F1}%，接近上限";
        }
        else if (utilizationPercent >= 75)
        {
            result.Status = "caution";
            result.WarningLevel = "medium";
            result.Message = $"預算使用達 {utilizationPercent:F1}%";
        }
        else
        {
            result.Status = "ok";
            result.WarningLevel = "none";
            result.Message = $"預算使用正常 ({utilizationPercent:F1}%)";
        }

        if (result.WarningLevel != "none")
        {
            _logger.LogWarning("預算警示：{CostCenter} {Status} - {Message}", 
                costCenterCode, result.Status, result.Message);
        }

        return result;
    }

    public async Task<List<BudgetAlertDto>> GetActiveAlertsAsync()
    {
        var alerts = new List<BudgetAlertDto>();

        var costCenters = await _db.CostCenters
            .Where(c => c.IsActive && c.BudgetLimit > 0)
            .ToListAsync();

        foreach (var cc in costCenters)
        {
            var utilization = cc.BudgetLimit > 0 
                ? cc.BudgetUsed / cc.BudgetLimit * 100 
                : 0;

            if (utilization >= 75)
            {
                alerts.Add(new BudgetAlertDto
                {
                    CostCenterCode = cc.CostCenterCode,
                    CostCenterName = cc.CostCenterName,
                    BudgetAmount = cc.BudgetLimit,
                    UsedAmount = cc.BudgetUsed,
                    UtilizationPercent = utilization,
                    WarningLevel = utilization >= 100 ? "error" : 
                                  utilization >= 90 ? "high" : "medium",
                    Message = utilization >= 100 
                        ? "預算已超支" 
                        : $"預算使用達 {utilization:F1}%"
                });
            }
        }

        return alerts.OrderByDescending(a => a.UtilizationPercent).ToList();
    }
}