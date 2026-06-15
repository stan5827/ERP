using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Controllers;

public class JobTitlesController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public JobTitlesController(ErpGeneralDbContext db) { _db = db; }

    // GET /api/erp/job-titles
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<JobTitleDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid?   gradeId,
        [FromQuery] bool?   isActive)
    {
        var q = _db.JobTitles.Include(t => t.Grade).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(t => t.TitleName.Contains(search) || t.TitleCode.Contains(search));
        if (gradeId.HasValue)
            q = q.Where(t => t.GradeId == gradeId.Value);
        if (isActive.HasValue)
            q = q.Where(t => t.IsActive == isActive.Value);

        var list = await q.OrderBy(t => t.TitleCode).ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    // GET /api/erp/job-titles/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<JobTitleDto>>> Get(Guid id)
    {
        var t = await _db.JobTitles.Include(x => x.Grade).FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound(ApiHelper.Fail("Job title not found"));
        return Ok(ToDto(t));
    }

    // POST /api/erp/job-titles
    [HttpPost]
    public async Task<ActionResult<ApiResponse<JobTitleDto>>> Create([FromBody] JobTitleUpsertDto req)
    {
        if (await _db.JobTitles.AnyAsync(t => t.TitleCode == req.TitleCode))
            return BadRequest(ApiHelper.Fail($"Title code {req.TitleCode} already exists"));

        var title = new JobTitle
        {
            TitleCode          = req.TitleCode,
            TitleName          = req.TitleName,
            GradeId            = req.GradeId,
            ApplicableDeptType = req.ApplicableDeptType,
            IsSupervisor       = req.IsSupervisor ?? false,
            IsActive           = req.IsActive ?? true,
            Description        = req.Description,
        };
        _db.JobTitles.Add(title);
        await _db.SaveChangesAsync();
        await _db.Entry(title).Reference(t => t.Grade).LoadAsync();
        return Ok(ToDto(title), "Job title created");
    }

    // PUT /api/erp/job-titles/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<JobTitleDto>>> Update(Guid id, [FromBody] JobTitleUpsertDto req)
    {
        var title = await _db.JobTitles.Include(t => t.Grade).FirstOrDefaultAsync(t => t.Id == id);
        if (title == null) return NotFound(ApiHelper.Fail("Job title not found"));

        if (await _db.JobTitles.AnyAsync(t => t.TitleCode == req.TitleCode && t.Id != id))
            return BadRequest(ApiHelper.Fail($"Title code {req.TitleCode} already exists"));

        title.TitleCode          = req.TitleCode;
        title.TitleName          = req.TitleName;
        title.GradeId            = req.GradeId;
        title.ApplicableDeptType = req.ApplicableDeptType;
        title.IsSupervisor       = req.IsSupervisor ?? title.IsSupervisor;
        title.IsActive           = req.IsActive ?? title.IsActive;
        title.Description        = req.Description;

        await _db.SaveChangesAsync();
        await _db.Entry(title).Reference(t => t.Grade).LoadAsync();
        return Ok(ToDto(title), "Job title updated");
    }

    // DELETE /api/erp/job-titles/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var title = await _db.JobTitles.FindAsync(id);
        if (title == null) return NotFound(ApiHelper.Fail("Job title not found"));
        if (await _db.Set<Employee>().AnyAsync(e => e.JobTitleId == id))
            return BadRequest(ApiHelper.Fail("Cannot delete: employees still assigned"));
        _db.JobTitles.Remove(title);
        await _db.SaveChangesAsync();
        return Ok("Job title deleted");
    }

    private static JobTitleDto ToDto(JobTitle t) => new()
    {
        Id                 = t.Id,
        TitleCode          = t.TitleCode,
        TitleName          = t.TitleName,
        GradeId            = t.GradeId,
        GradeCode          = t.Grade?.GradeCode,
        GradeName          = t.Grade?.GradeName,
        ApplicableDeptType = t.ApplicableDeptType,
        IsSupervisor       = t.IsSupervisor,
        IsActive           = t.IsActive,
        Description        = t.Description,
        CreatedAt          = t.CreatedAt,
    };
}

public class JobTitleDto
{
    public Guid    Id                 { get; set; }
    public string  TitleCode          { get; set; } = "";
    public string  TitleName          { get; set; } = "";
    public Guid?   GradeId            { get; set; }
    public string? GradeCode          { get; set; }
    public string? GradeName          { get; set; }
    public string? ApplicableDeptType { get; set; }
    public bool    IsSupervisor       { get; set; }
    public bool    IsActive           { get; set; }
    public string? Description        { get; set; }
    public DateTime CreatedAt         { get; set; }
}

public class JobTitleUpsertDto
{
    public string  TitleCode          { get; set; } = null!;
    public string  TitleName          { get; set; } = null!;
    public Guid?   GradeId            { get; set; }
    public string? ApplicableDeptType { get; set; }
    public bool?   IsSupervisor       { get; set; }
    public bool?   IsActive           { get; set; }
    public string? Description        { get; set; }
}
