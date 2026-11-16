using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class MpsController : BaseController
{
    private readonly IMpsService _mpsService;

    public MpsController(IMpsService mpsService)
    {
        _mpsService = mpsService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MpsHeaderDto>>>> GetHeaders()
    {
        var headers = await _mpsService.GetMpsHeadersAsync();
        return Ok(headers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MpsDetailDto>>> GetDetail(Guid id)
    {
        var detail = await _mpsService.GetMpsDetailAsync(id);
        if (detail == null) return NotFound(ApiResponse<MpsDetailDto>.Fail("MPS version not found"));
        return Ok(detail);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MpsHeaderDto>>> Create(CreateMpsRequest request)
    {
        var userId = GetUserId();
        var mps = await _mpsService.CreateMpsVersionAsync(request, userId);
        var dto = new MpsHeaderDto
        {
            Id = mps.Id,
            MpsNo = mps.MpsNo,
            PlanName = mps.PlanName,
            Status = mps.Status,
            VersionNo = mps.VersionNo
        };
        return Ok(dto);
    }

    [HttpPost("{id}/run-consumption")]
    public async Task<ActionResult<ApiResponse>> RunConsumption(Guid id)
    {
        var success = await _mpsService.RunConsumptionAsync(id);
        if (!success) return NotFound(ApiResponse.Fail("MPS version not found"));
        return Ok("Consumption triggered successfully");
    }

    [HttpGet("{id}/exceptions")]
    public ActionResult<ApiResponse<List<MpsExceptionDto>>> GetExceptions(Guid id)
    {
        // For now returning mock/empty list as implementation is pending
        var dtos = new List<MpsExceptionDto>();
        return Ok(dtos);
    }

    [HttpGet("{id}/rccp")]
    public async Task<ActionResult<ApiResponse<RccpStatusDto>>> GetRccpStatus(Guid id)
    {
        var status = await _mpsService.GetRccpStatusAsync(id);
        return Ok(status);
    }

    [HttpGet("aggregate-rccp")]
    public async Task<ActionResult<ApiResponse<AggregateRccpDto>>> GetAggregateRccpStatus([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var startDate = start ?? DateTime.Today;
        var endDate = end ?? startDate.AddDays(12 * 7); // Default 12 weeks
        
        var status = await _mpsService.GetAggregateRccpStatusAsync(startDate, endDate);
        return Ok(status);
    }
}
