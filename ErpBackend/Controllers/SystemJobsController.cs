using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.System;
using ErpBackend.Services;
using ErpBackend.Helpers;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/system/jobs")]
public class SystemJobsController : BaseController
{
    private readonly ISystemJobService _jobService;

    public SystemJobsController(ISystemJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SystemJob>>>> GetRecentJobs([FromQuery] int count = 50)
    {
        var jobs = await _jobService.GetRecentJobsAsync(count);
        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SystemJob>>> GetJob(Guid id)
    {
        var job = await _jobService.GetJobAsync(id);
        if (job == null) return NotFound(ApiResponse.Fail("Job not found"));
        return Ok(job);
    }
}

