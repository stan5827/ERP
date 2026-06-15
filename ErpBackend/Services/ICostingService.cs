using ErpBackend.Models.Finance;
using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface ICostingService
{
    Task<StandardCostBreakdownDto> CalculateStandardCostAsync(Guid workOrderId);
    Task<WorkOrderCostAnalysisDto> GetActualCostAnalysisAsync(Guid workOrderId);
    Task<WorkOrderCost> SettleWorkOrderAsync(Guid workOrderId);
    
    Task<CostCenterReportDto> GetCostCenterReportAsync(Guid costCenterId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<decimal> CalculateActivityCostAsync(Guid activityTypeId, decimal quantity);
    Task<Dictionary<Guid, decimal>> AllocateCostsAsync(Guid sourceCostCenterId, DateTime periodEnd);
    Task<decimal> GetWorkCenterTotalRateAsync(Guid workCenterId);
    Task<bool> LinkWorkCenterToCostCenterAsync(Guid workCenterId, Guid costCenterId);
    Task<WorkCenterCostDto> SetWorkCenterRatesAsync(CreateWorkCenterCostRequest request);
    Task UpdateBudgetUsedAsync(Guid costCenterId, decimal amount);

    Task RecordLaborActualAsync(Guid woId, Guid reportId, int opSeq, decimal hours, decimal rate, Guid? employeeId = null);
    Task RecordMachineActualAsync(Guid woId, Guid reportId, int opSeq, decimal hours, decimal rate, Guid? workCenterId = null);
    Task RecordMaterialActualAsync(Guid woId, Guid skuId, decimal qty, decimal unitCost, string? lotNo = null);

    Task<List<CategoryCostAnalysisDto>> GetCategoryAnalysisAsync(DateTime startDate, DateTime endDate);
    Task<List<DepartmentVarianceDto>> GetDepartmentVarianceAsync(DateTime startDate, DateTime endDate);
    Task<List<CostTrendPointDto>> GetCostTrendAsync(DateTime startDate, DateTime endDate);
    Task SettleAllCompletedWorkOrdersAsync(Guid? jobId = null);
}

public class StandardCostBreakdownDto
{
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal MachineCost { get; set; }
    public decimal TotalCost => MaterialCost + LaborCost + MachineCost;
}
