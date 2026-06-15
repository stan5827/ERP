using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Services;

public interface IApsScheduler
{
    Task<ProductionSchedule> GenerateScheduleAsync(DateTime horizonStart, DateTime horizonEnd, string scheduleType = "firm");
    Task<ProductionSchedule> OptimizeScheduleAsync(Guid scheduleId, OptimizationCriteria criteria);
    Task<List<ScheduleTask>> RescheduleTaskAsync(Guid taskId, DateTime newStart, DateTime newEnd);
    Task<CapacityLoad?> GetCapacityLoadAsync(Guid workCenterId, DateTime date, int shift = 1);
    Task<List<ScheduleConstraint>> CheckConstraintsAsync(Guid scheduleId);
    Task<List<ProductionSchedule>> GetSchedulesAsync(int page = 1, int pageSize = 20);
    Task<ProductionSchedule?> GetScheduleAsync(Guid scheduleId);
}

public class ApsScheduler : IApsScheduler
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<ApsScheduler> _logger;
    
    public ApsScheduler(ErpGeneralDbContext db, ILogger<ApsScheduler> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public async Task<ProductionSchedule> GenerateScheduleAsync(DateTime horizonStart, DateTime horizonEnd, string scheduleType = "firm")
    {
        // 1. 創建排程記錄
        var schedule = new ProductionSchedule
        {
            ScheduleNumber = $"SCH-{DateTime.UtcNow:yyyyMMddHHmmss}",
            ScheduleType = scheduleType,
            ScheduleDate = DateTime.UtcNow,
            HorizonStart = horizonStart,
            HorizonEnd = horizonEnd,
            Status = "draft"
        };
        
        _db.ProductionSchedules.Add(schedule);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation($"開始生成排程：{schedule.ScheduleNumber}");
        
        // 2. 獲取所有待排程工單
        var pendingOrders = await GetPendingWorkOrdersAsync(horizonStart, horizonEnd);
        
        _logger.LogInformation($"找到 {pendingOrders.Count} 個待排程工單");
        
        // 3. 為每個工單的每個工序生成排程任務
        foreach (var order in pendingOrders)
        {
            await ScheduleWorkOrderAsync(schedule, order);
        }
        
        // 4. 計算產能負荷
        await CalculateCapacityLoadsAsync(schedule);
        
        // 5. 檢查約束條件
        await CheckAndRecordConstraintsAsync(schedule);
        
        await _db.SaveChangesAsync();
        
        _logger.LogInformation($"排程生成完成：{schedule.ScheduleNumber}，共 {schedule.Tasks.Count} 個任務");
        
        return schedule;
    }
    
    public async Task<List<WorkOrder>> GetPendingWorkOrdersAsync(DateTime horizonStart, DateTime horizonEnd)
    {
        return await _db.WorkOrders
            .Include(wo => wo.Operations)
            .ThenInclude(op => op.Operation)
            .Include(wo => wo.Operations)
            .ThenInclude(op => op.WorkCenter)
            .Where(wo => wo.Status == "released" || wo.Status == "pending")
            .Where(wo => wo.PlannedStartDate >= horizonStart && wo.PlannedEndDate <= horizonEnd)
            .OrderBy(wo => wo.Priority == "urgent" ? 1 : 
                          wo.Priority == "high" ? 2 : 
                          wo.Priority == "normal" ? 3 : 4)
            .ThenBy(wo => wo.PlannedEndDate)
            .ToListAsync();
    }
    
    private async Task ScheduleWorkOrderAsync(ProductionSchedule schedule, WorkOrder workOrder)
    {
        // 如果工單沒有製程，直接創建一個任務
        if (!workOrder.Operations.Any())
        {
            var defaultTask = new ScheduleTask
            {
                ScheduleId = schedule.Id,
                WorkOrderId = workOrder.Id,
                OperationSeq = 1,
                WorkCenterId = workOrder.Operations.FirstOrDefault()?.WorkCenterId ?? Guid.Empty,
                PlannedStart = workOrder.PlannedStartDate ?? schedule.HorizonStart,
                PlannedEnd = workOrder.PlannedEndDate ?? schedule.HorizonEnd,
                Quantity = workOrder.PlannedQty,
                Status = "planned",
                Priority = GetPriorityValue(workOrder.Priority)
            };
            
            _db.ScheduleTasks.Add(defaultTask);
            return;
        }
        
        // 按工序順序排程，起始時間受 MRP 計畫開始日約束 (Earliest Start Constraint)
        var mrpConstraint = workOrder.MrpPlannedStartDate ?? workOrder.PlannedStartDate ?? schedule.HorizonStart;
        var currentDate = mrpConstraint;
        
        foreach (var operation in workOrder.Operations.OrderBy(op => op.OperationSeq))
        {
            var workCenter = operation.WorkCenter;
            
            // 計算工序時間
            var setupHours = operation.Operation.SetupTime;
            var runHours = operation.Operation.RunTime * workOrder.PlannedQty;
            var totalHours = (setupHours + runHours) / (workCenter.Efficiency / 100);
            
            // 找到可用的開始時間（考慮產能限制）
            var availableStart = await FindAvailableSlotAsync(
                workCenter.Id, currentDate, totalHours, schedule.Id);
            
            var task = new ScheduleTask
            {
                ScheduleId = schedule.Id,
                WorkOrderId = workOrder.Id,
                OperationSeq = operation.OperationSeq,
                WorkCenterId = workCenter.Id,
                PlannedStart = availableStart,
                PlannedEnd = availableStart.AddHours((double)totalHours),
                SetupHours = (decimal)setupHours,
                RunHours = (decimal)runHours,
                TotalHours = totalHours,
                Quantity = workOrder.PlannedQty,
                Status = "planned",
                Priority = GetPriorityValue(workOrder.Priority),
                SequenceType = GetSequenceType(workOrder.Priority)
            };
            
            _db.ScheduleTasks.Add(task);
            
            // 下一個工序的開始時間 = 當前工序結束時間（簡化處理，不考慮轉移時間）
            currentDate = task.PlannedEnd;
        }
    }
    
    private async Task<DateTime> FindAvailableSlotAsync(Guid workCenterId, DateTime startDate, decimal requiredHours, Guid scheduleId)
    {
        var currentDate = startDate;
        var maxCheckDate = startDate.AddDays(30); // 最多向後查找30天
        
        while (currentDate < maxCheckDate)
        {
            // 獲取當天的產能日曆
            var calendarEntry = await GetCalendarEntryAsync(workCenterId, currentDate);
            
            if (calendarEntry != null && !calendarEntry.IsHoliday)
            {
                // 計算當天已排程時間
                var scheduledHours = await _db.ScheduleTasks
                    .Where(t => t.WorkCenterId == workCenterId 
                             && t.ScheduleId == scheduleId
                             && t.PlannedStart.Date == currentDate.Date)
                    .SumAsync(t => t.TotalHours);
                
                var availableHours = calendarEntry.AvailableHours * calendarEntry.Efficiency / 100;
                var remainingHours = availableHours - scheduledHours;
                
                if (remainingHours >= requiredHours)
                {
                    return currentDate;
                }
            }
            
            // 移到下一天
            currentDate = currentDate.AddDays(1).Date.AddHours(8); // 假設早上8點開始
        }
        
        return startDate; // 找不到則返回原始日期
    }
    
    private async Task<CapacityCalendarEntry?> GetCalendarEntryAsync(Guid workCenterId, DateTime date)
    {
        var calendar = await _db.CapacityCalendars
            .Include(c => c.Entries)
            .FirstOrDefaultAsync(c => c.WorkCenterId == workCenterId && c.IsDefault);
        
        return calendar?.Entries.FirstOrDefault(e => e.Date.Date == date.Date);
    }
    
    private async Task CalculateCapacityLoadsAsync(ProductionSchedule schedule)
    {
        var workCenters = await _db.WorkCenters.ToListAsync();
        var startDate = schedule.HorizonStart.Date;
        var endDate = schedule.HorizonEnd.Date;
        
        foreach (var wc in workCenters)
        {
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var calendarEntry = await GetCalendarEntryAsync(wc.Id, date);
                if (calendarEntry == null) continue;
                
                var scheduledHours = await _db.ScheduleTasks
                    .Where(t => t.WorkCenterId == wc.Id 
                             && t.ScheduleId == schedule.Id
                             && t.PlannedStart.Date == date.Date)
                    .SumAsync(t => t.TotalHours);
                
                var availableHours = calendarEntry.AvailableHours * calendarEntry.Efficiency / 100;
                var loadPercentage = availableHours > 0 ? (scheduledHours / availableHours) * 100 : 0;
                
                var load = new CapacityLoad
                {
                    ScheduleId = schedule.Id,
                    WorkCenterId = wc.Id,
                    LoadDate = date,
                    ShiftNumber = calendarEntry.ShiftNumber,
                    AvailableHours = availableHours,
                    ScheduledHours = scheduledHours,
                    LoadedHours = scheduledHours,
                    LoadPercentage = loadPercentage,
                    LoadStatus = loadPercentage > 100 ? "overloaded" : 
                               loadPercentage < 60 ? "underutilized" : "normal",
                    CapacityUtilization = loadPercentage
                };
                
                _db.CapacityLoads.Add(load);
            }
        }
    }
    
    private async Task CheckAndRecordConstraintsAsync(ProductionSchedule schedule)
    {
        // 檢查產能過載
        var overloadedLoads = await _db.CapacityLoads
            .Where(l => l.ScheduleId == schedule.Id && l.LoadStatus == "overloaded")
            .Include(l => l.WorkCenter)
            .ToListAsync();
        
        foreach (var load in overloadedLoads)
        {
            var constraint = new ScheduleConstraint
            {
                ScheduleId = schedule.Id,
                ConstraintType = "resource",
                Description = $"工作中心 {load.WorkCenter.WorkCenterName} 在 {load.LoadDate:yyyy/MM/dd} 產能過載 ({load.LoadPercentage:F1}%)",
                ResourceId = load.WorkCenterId,
                ResourceType = "work_center",
                StartDate = load.LoadDate,
                EndDate = load.LoadDate,
                Severity = load.LoadPercentage > 120 ? "error" : "warning"
            };
            
            _db.ScheduleConstraints.Add(constraint);
        }

        // 檢查物料邊界衝突 (排程早於 MRP 建議日)
        var tasks = await _db.ScheduleTasks
            .Include(t => t.WorkOrder)
            .Where(t => t.ScheduleId == schedule.Id && t.WorkOrder.MrpPlannedStartDate != null)
            .ToListAsync();

        foreach (var task in tasks)
        {
            if (task.PlannedStart < task.WorkOrder.MrpPlannedStartDate)
            {
                var constraint = new ScheduleConstraint
                {
                    ScheduleId = schedule.Id,
                    ConstraintType = "material",
                    Description = $"工單 {task.WorkOrder.WoNumber} 排程早於 MRP 建議開始日 {task.WorkOrder.MrpPlannedStartDate:yyyy/MM/dd}",
                    ResourceId = task.WorkOrderId,
                    ResourceType = "work_order",
                    Severity = "error",
                    StartDate = task.PlannedStart,
                    EndDate = task.WorkOrder.MrpPlannedStartDate.Value
                };
                _db.ScheduleConstraints.Add(constraint);
            }
        }
    }
    
    public async Task<ProductionSchedule> OptimizeScheduleAsync(Guid scheduleId, OptimizationCriteria criteria)
    {
        var schedule = await _db.ProductionSchedules
            .Include(s => s.Tasks)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
        
        if (schedule == null)
            throw new ArgumentException("排程不存在");
        
        // 根據優化目標重新排序任務
        var tasks = schedule.Tasks.ToList();
        
        switch (criteria.PrimaryGoal)
        {
            case "minimize_setup":
                tasks = OptimizeForMinSetup(tasks);
                break;
            case "maximize_utilization":
                tasks = OptimizeForMaxUtilization(tasks);
                break;
            case "meet_deadlines":
                tasks = OptimizeForDeadlines(tasks);
                break;
            case "balance_workload":
                tasks = OptimizeForBalance(tasks);
                break;
        }
        
        // 重新計算時間
        await RecalculateScheduleTimesAsync(schedule, tasks);
        
        schedule.Status = "optimized";
        await _db.SaveChangesAsync();
        
        return schedule;
    }
    
    private List<ScheduleTask> OptimizeForMinSetup(List<ScheduleTask> tasks)
    {
        // 按工作中心分組，然後按設置時間最小化排序
        return tasks
            .GroupBy(t => t.WorkCenterId)
            .SelectMany(g => g.OrderBy(t => t.SetupHours).ThenBy(t => t.PlannedStart))
            .ToList();
    }
    
    private List<ScheduleTask> OptimizeForMaxUtilization(List<ScheduleTask> tasks)
    {
        // 優先排緊急工單，填滿產能
        return tasks
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.WorkCenterId)
            .ThenBy(t => t.PlannedStart)
            .ToList();
    }
    
    private List<ScheduleTask> OptimizeForDeadlines(List<ScheduleTask> tasks)
    {
        // 按交期排序
        return tasks
            .OrderBy(t => t.WorkOrder.PlannedEndDate)
            .ThenByDescending(t => t.Priority)
            .ToList();
    }
    
    private List<ScheduleTask> OptimizeForBalance(List<ScheduleTask> tasks)
    {
        // 平衡各工作中心的負荷（簡化版：輪流分配）
        var result = new List<ScheduleTask>();
        var groups = tasks.GroupBy(t => t.WorkCenterId).ToList();
        var maxCount = groups.Max(g => g.Count());
        
        for (int i = 0; i < maxCount; i++)
        {
            foreach (var group in groups)
            {
                var taskList = group.ToList();
                if (i < taskList.Count)
                {
                    result.Add(taskList[i]);
                }
            }
        }
        
        return result;
    }
    
    private async Task RecalculateScheduleTimesAsync(ProductionSchedule schedule, List<ScheduleTask> tasks)
    {
        // 清除現有任務
        _db.ScheduleTasks.RemoveRange(schedule.Tasks);
        
        // 按工作中心分組重新排程
        var workCenterGroups = tasks.GroupBy(t => t.WorkCenterId);
        
        foreach (var group in workCenterGroups)
        {
            var currentDate = schedule.HorizonStart;
            
            foreach (var task in group.OrderBy(t => t.Priority))
            {
                var availableStart = await FindAvailableSlotAsync(
                    task.WorkCenterId, currentDate, task.TotalHours, schedule.Id);
                
                task.PlannedStart = availableStart;
                task.PlannedEnd = availableStart.AddHours((double)task.TotalHours);
                
                _db.ScheduleTasks.Add(task);
                
                currentDate = task.PlannedEnd;
            }
        }
        
        // 重新計算產能負荷
        await CalculateCapacityLoadsAsync(schedule);
    }
    
    public async Task<List<ScheduleTask>> RescheduleTaskAsync(Guid taskId, DateTime newStart, DateTime newEnd)
    {
        var task = await _db.ScheduleTasks
            .Include(t => t.WorkOrder)
            .FirstOrDefaultAsync(t => t.Id == taskId);
        
        if (task == null)
            throw new ArgumentException("排程任務不存在");
        
        task.PlannedStart = newStart;
        task.PlannedEnd = newEnd;
        task.Status = "rescheduled";
        
        await _db.SaveChangesAsync();
        
        return await _db.ScheduleTasks
            .Where(t => t.ScheduleId == task.ScheduleId && t.WorkCenterId == task.WorkCenterId)
            .OrderBy(t => t.PlannedStart)
            .ToListAsync();
    }
    
    public async Task<CapacityLoad?> GetCapacityLoadAsync(Guid workCenterId, DateTime date, int shift = 1)
    {
        return await _db.CapacityLoads
            .FirstOrDefaultAsync(l => l.WorkCenterId == workCenterId 
                                   && l.LoadDate.Date == date.Date 
                                   && l.ShiftNumber == shift);
    }
    
    public async Task<List<ScheduleConstraint>> CheckConstraintsAsync(Guid scheduleId)
    {
        return await _db.ScheduleConstraints
            .Where(c => c.ScheduleId == scheduleId && !c.IsResolved)
            .OrderByDescending(c => c.Severity)
            .ToListAsync();
    }
    
    public async Task<List<ProductionSchedule>> GetSchedulesAsync(int page = 1, int pageSize = 20)
    {
        return await _db.ProductionSchedules
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
    
    public async Task<ProductionSchedule?> GetScheduleAsync(Guid scheduleId)
    {
        return await _db.ProductionSchedules
            .Include(s => s.Tasks)
            .ThenInclude(t => t.WorkOrder)
            .Include(s => s.Tasks)
            .ThenInclude(t => t.WorkCenter)
            .Include(s => s.CapacityLoads)
            .ThenInclude(l => l.WorkCenter)
            .Include(s => s.Constraints)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
    }
    
    private int GetPriorityValue(string? priority)
    {
        return priority switch
        {
            "urgent" => 100,
            "high" => 75,
            "normal" => 50,
            "low" => 25,
            _ => 50
        };
    }
    
    private string GetSequenceType(string? priority)
    {
        return priority switch
        {
            "urgent" => "urgent",
            "high" => "setup_min",
            _ => "normal"
        };
    }
}