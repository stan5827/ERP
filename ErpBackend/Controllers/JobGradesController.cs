using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
[Route("api/erp/job-grades")]
public class JobGradesController : BaseController
{
    private readonly IJobGradeService _jobGradeService;

    public JobGradesController(IJobGradeService jobGradeService) => _jobGradeService = jobGradeService;

    // GET /api/erp/job-grades
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<JobGradeDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive)
        => Ok(await _jobGradeService.GetAllAsync(search, isActive));

    // GET /api/erp/job-grades/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<JobGradeDto>>> Get(Guid id)
    {
        var dto = await _jobGradeService.GetByIdAsync(id);
        return dto == null ? NotFound(ApiHelper.Fail("Job grade not found")) : Ok(dto);
    }

    // POST /api/erp/job-grades
    [HttpPost]
    public async Task<ActionResult<ApiResponse<JobGradeDto>>> Create([FromBody] JobGradeUpsertDto req)
    {
        var (dto, status) = await _jobGradeService.CreateAsync(req);
        if (status == "invalid") return BadRequest(ApiHelper.Fail("GradeCode and GradeName are required"));
        if (status == "duplicate") return BadRequest(ApiHelper.Fail($"Grade code {req.GradeCode} already exists"));
        return Ok(dto!, "Job grade created");
    }

    // PUT /api/erp/job-grades/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<JobGradeDto>>> Update(Guid id, [FromBody] JobGradeUpsertDto req)
    {
        var (dto, status) = await _jobGradeService.UpdateAsync(id, req);
        if (status == "not_found") return NotFound(ApiHelper.Fail("Job grade not found"));
        if (status == "duplicate") return BadRequest(ApiHelper.Fail($"Grade code {req.GradeCode} already exists"));
        return Ok(dto!, "Job grade updated");
    }

    // DELETE /api/erp/job-grades/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var (ok, status) = await _jobGradeService.DeleteAsync(id);
        if (status == "not_found") return NotFound(ApiHelper.Fail("Job grade not found"));
        if (status == "guarded") return BadRequest(ApiHelper.Fail("Cannot delete: job titles or employees still linked"));
        return Ok("Job grade deleted");
    }
}
