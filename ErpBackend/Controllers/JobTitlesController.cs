using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
[Route("api/erp/job-titles")]
public class JobTitlesController : BaseController
{
    private readonly IJobTitleService _jobTitleService;

    public JobTitlesController(IJobTitleService jobTitleService) => _jobTitleService = jobTitleService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<JobTitleDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? gradeId,
        [FromQuery] bool? isActive)
        => Ok(await _jobTitleService.GetAllAsync(search, gradeId, isActive));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<JobTitleDto>>> Get(Guid id)
    {
        var dto = await _jobTitleService.GetByIdAsync(id);
        return dto == null ? NotFound(ApiHelper.Fail("Job title not found")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<JobTitleDto>>> Create([FromBody] JobTitleUpsertDto req)
    {
        var (dto, status) = await _jobTitleService.CreateAsync(req);
        if (status == "duplicate") return BadRequest(ApiHelper.Fail($"Title code {req.TitleCode} already exists"));
        if (status == "invalid") return BadRequest(ApiHelper.Fail("TitleCode is required"));
        return Ok(dto!, "Job title created");
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<JobTitleDto>>> Update(Guid id, [FromBody] JobTitleUpsertDto req)
    {
        var (dto, status) = await _jobTitleService.UpdateAsync(id, req);
        if (status == "not_found") return NotFound(ApiHelper.Fail("Job title not found"));
        if (status == "duplicate") return BadRequest(ApiHelper.Fail($"Title code {req.TitleCode} already exists"));
        return Ok(dto!, "Job title updated");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var (ok, status) = await _jobTitleService.DeleteAsync(id);
        if (status == "not_found") return NotFound(ApiHelper.Fail("Job title not found"));
        if (status == "guarded") return BadRequest(ApiHelper.Fail("Cannot delete: employees still assigned"));
        return Ok("Job title deleted");
    }
}
