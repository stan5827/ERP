using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/workcenters")]
public class WorkCentersController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public WorkCentersController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WorkCenterDto>>>> GetWorkCenters()
    {
        var items = await _db.WorkCenters
            .Where(w => w.IsActive)
            .OrderBy(w => w.WorkCenterCode)
            .Select(w => new WorkCenterDto
            {
                Id = w.Id,
                WorkCenterCode = w.WorkCenterCode,
                WorkCenterName = w.WorkCenterName,
                Capacity = w.Capacity,
                Efficiency = w.Efficiency,
                IsActive = w.IsActive
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<WorkCenterDto>>> GetWorkCenter(Guid id)
    {
        var w = await _db.WorkCenters.FindAsync(id);
        if (w == null) return NotFound(ApiHelper.FailOf<WorkCenterDto>("找不到工作中心"));

        return Ok(new WorkCenterDto
        {
            Id = w.Id,
            WorkCenterCode = w.WorkCenterCode,
            WorkCenterName = w.WorkCenterName,
            Capacity = w.Capacity,
            Efficiency = w.Efficiency,
            IsActive = w.IsActive
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WorkCenterDto>>> CreateWorkCenter([FromBody] CreateWorkCenterRequest request)
    {
        var w = new WorkCenter
        {
            WorkCenterCode = request.WorkCenterCode,
            WorkCenterName = request.WorkCenterName,
            Capacity = request.Capacity,
            Efficiency = request.Efficiency,
            IsActive = request.IsActive
        };

        _db.WorkCenters.Add(w);
        await _db.SaveChangesAsync();

        return Ok(new WorkCenterDto
        {
            Id = w.Id,
            WorkCenterCode = w.WorkCenterCode,
            WorkCenterName = w.WorkCenterName,
            Capacity = w.Capacity,
            Efficiency = w.Efficiency,
            IsActive = w.IsActive
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<WorkCenterDto>>> UpdateWorkCenter(Guid id, [FromBody] UpdateWorkCenterRequest request)
    {
        var w = await _db.WorkCenters.FindAsync(id);
        if (w == null) return NotFound(ApiHelper.FailOf<WorkCenterDto>("找不到工作中心"));

        if (request.WorkCenterCode != null) w.WorkCenterCode = request.WorkCenterCode;
        if (request.WorkCenterName != null) w.WorkCenterName = request.WorkCenterName;
        if (request.Capacity.HasValue) w.Capacity = request.Capacity.Value;
        if (request.Efficiency.HasValue) w.Efficiency = request.Efficiency.Value;
        if (request.IsActive.HasValue) w.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();

        return Ok(new WorkCenterDto
        {
            Id = w.Id,
            WorkCenterCode = w.WorkCenterCode,
            WorkCenterName = w.WorkCenterName,
            Capacity = w.Capacity,
            Efficiency = w.Efficiency,
            IsActive = w.IsActive
        });
    }
}
