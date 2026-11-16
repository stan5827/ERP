using Microsoft.AspNetCore.Mvc;
using ErpBackend.Services;
using ErpBackend.Models.MES;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/erp/[controller]")]
public class ApsController : Controller
{
    private readonly IApsScheduler _scheduler;
    private readonly ILogger<ApsController> _logger;

    public ApsController(IApsScheduler scheduler, ILogger<ApsController> logger)
    {
        _scheduler = scheduler;
        _logger = logger;
    }

    [HttpGet("ping")]
    public object Ping() => new { message = "ApsController operational", timestamp = DateTime.UtcNow };

    [HttpPost("generate")]
    public async Task<object> GenerateSchedule([FromBody] GenerateScheduleRequest request)
    {
        try
        {
            var horizonStart = request.HorizonStart ?? DateTime.UtcNow.Date;
            var horizonEnd = request.HorizonEnd ?? DateTime.UtcNow.Date.AddDays(30);

            var schedule = await _scheduler.GenerateScheduleAsync(horizonStart, horizonEnd, request.ScheduleType ?? "firm");

            return Ok(new
            {
                schedule_id = schedule.Id,
                schedule_number = schedule.ScheduleNumber,
                status = schedule.Status,
                task_count = schedule.Tasks?.Count ?? 0,
                message = "排程生成成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "排程生成失敗");
            return StatusCode(500, new { error = $"排程生成失敗: {ex.Message}" });
        }
    }

    [HttpGet("schedules")]
    public async Task<object> GetSchedules([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var schedules = await _scheduler.GetSchedulesAsync(page, pageSize);
            return Ok(new { data = schedules });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "獲取排程列表失敗");
            return StatusCode(500, new { error = $"獲取失敗: {ex.Message}" });
        }
    }

    [HttpGet("{scheduleId}")]
    public async Task<object> GetSchedule(Guid scheduleId)
    {
        try
        {
            var schedule = await _scheduler.GetScheduleAsync(scheduleId);
            if (schedule == null)
                return NotFound(new { error = "排程不存在" });

            return Ok(new
            {
                id = schedule.Id,
                schedule_number = schedule.ScheduleNumber,
                status = schedule.Status,
                horizon_start = schedule.HorizonStart,
                horizon_end = schedule.HorizonEnd,
                tasks = schedule.Tasks?.Select(t => new
                {
                    id = t.Id,
                    work_order_id = t.WorkOrderId,
                    work_center_id = t.WorkCenterId,
                    planned_start = t.PlannedStart,
                    planned_end = t.PlannedEnd,
                    quantity = t.Quantity,
                    status = t.Status
                }),
                capacity_loads = schedule.CapacityLoads?.Select(l => new
                {
                    work_center_id = l.WorkCenterId,
                    load_date = l.LoadDate,
                    load_percentage = l.LoadPercentage,
                    status = l.LoadStatus
                }),
                constraints = schedule.Constraints?.Where(c => !c.IsResolved).Select(c => new
                {
                    type = c.ConstraintType,
                    description = c.Description,
                    severity = c.Severity
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "獲取排程詳情失敗");
            return StatusCode(500, new { error = $"獲取失敗: {ex.Message}" });
        }
    }

    [HttpPost("{scheduleId}/optimize")]
    public async Task<object> OptimizeSchedule(Guid scheduleId, [FromBody] OptimizationRequest request)
    {
        try
        {
            var criteria = new OptimizationCriteria
            {
                PrimaryGoal = request.PrimaryGoal ?? "meet_deadlines"
            };

            var schedule = await _scheduler.OptimizeScheduleAsync(scheduleId, criteria);

            return Ok(new
            {
                schedule_id = schedule.Id,
                status = schedule.Status,
                message = "排程優化完成"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "排程優化失敗");
            return StatusCode(500, new { error = $"優化失敗: {ex.Message}" });
        }
    }

    [HttpPost("tasks/{taskId}/reschedule")]
    public async Task<object> RescheduleTask(Guid taskId, [FromBody] RescheduleRequest request)
    {
        try
        {
            var tasks = await _scheduler.RescheduleTaskAsync(taskId, request.NewStart, request.NewEnd);

            return Ok(new
            {
                task_id = taskId,
                affected_tasks = tasks.Count,
                message = "任務重新排程成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "任務重新排程失敗");
            return StatusCode(500, new { error = $"重新排程失敗: {ex.Message}" });
        }
    }

    [HttpGet("capacity/{workCenterId}")]
    public async Task<object> GetCapacityLoad(Guid workCenterId, [FromQuery] DateTime? date = null)
    {
        try
        {
            var loadDate = date ?? DateTime.UtcNow.Date;
            var load = await _scheduler.GetCapacityLoadAsync(workCenterId, loadDate);

            if (load == null)
                return Ok(new
                {
                    work_center_id = workCenterId,
                    load_date = loadDate,
                    status = "no_data",
                    message = "無產能負荷資料"
                });

            return Ok(new
            {
                work_center_id = load.WorkCenterId,
                load_date = load.LoadDate,
                available_hours = load.AvailableHours,
                scheduled_hours = load.ScheduledHours,
                loaded_hours = load.LoadedHours,
                load_percentage = load.LoadPercentage,
                status = load.LoadStatus
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "獲取產能負荷失敗");
            return StatusCode(500, new { error = $"獲取失敗: {ex.Message}" });
        }
    }

    [HttpGet("{scheduleId}/constraints")]
    public async Task<object> GetConstraints(Guid scheduleId)
    {
        try
        {
            var constraints = await _scheduler.CheckConstraintsAsync(scheduleId);
            return Ok(new { data = constraints });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "獲取約束失敗");
            return StatusCode(500, new { error = $"獲取失敗: {ex.Message}" });
        }
    }
}