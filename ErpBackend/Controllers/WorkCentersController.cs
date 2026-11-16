using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Route("api/erp/work-centers")]
public class WorkCentersController : BaseController
{
    private readonly IWorkCenterService _workCenterService;

    public WorkCentersController(IWorkCenterService workCenterService) => _workCenterService = workCenterService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WorkCenterDto>>>> GetWorkCenters()
        => Ok(await _workCenterService.GetWorkCentersAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkCenterDto>>> GetWorkCenter(Guid id)
    {
        var dto = await _workCenterService.GetWorkCenterAsync(id);
        return dto == null ? NotFound(ApiHelper.FailOf<WorkCenterDto>("找不到工作中心")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WorkCenterDto>>> CreateWorkCenter([FromBody] CreateWorkCenterRequest request)
    {
        var (dto, status) = await _workCenterService.CreateWorkCenterAsync(request);
        if (status == "invalid") return BadRequest(ApiHelper.FailOf<WorkCenterDto>("WorkCenterCode is required"));
        if (status == "duplicate") return BadRequest(ApiHelper.FailOf<WorkCenterDto>("工作中心代碼已存在"));
        return Ok(dto!);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkCenterDto>>> UpdateWorkCenter(Guid id, [FromBody] UpdateWorkCenterRequest request)
    {
        var (dto, status) = await _workCenterService.UpdateWorkCenterAsync(id, request);
        if (status == "not_found") return NotFound(ApiHelper.FailOf<WorkCenterDto>("找不到工作中心"));
        if (status == "duplicate") return BadRequest(ApiHelper.FailOf<WorkCenterDto>("工作中心代碼已存在"));
        return Ok(dto!);
    }
}
