using Microsoft.AspNetCore.Mvc;
using ErpBackend.Services;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/ai/scheduling")]
public class AiSchedulingController : ControllerBase
{
    private readonly IAiSchedulingService _aiScheduling;

    public AiSchedulingController(IAiSchedulingService aiScheduling)
    {
        _aiScheduling = aiScheduling;
    }

    [HttpPost("generate")]
    public async Task<ActionResult> GenerateSchedule([FromBody] AiScheduleRequest request)
    {
        var result = await _aiScheduling.GenerateScheduleAsync(request);
        return Ok(result);
    }

    [HttpGet("risks")]
    public async Task<ActionResult> GetTurnoverRisks([FromQuery] Guid? departmentId)
    {
        var risks = await _aiScheduling.PredictTurnoverRisksAsync(departmentId);
        return Ok(risks);
    }

    [HttpGet("utilization")]
    public async Task<ActionResult> GetShiftUtilization([FromQuery] string yearMonth)
    {
        var result = await _aiScheduling.GetShiftUtilizationAsync(yearMonth);
        return Ok(result);
    }
}