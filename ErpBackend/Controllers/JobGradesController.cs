using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Controllers;

public class JobGradesController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public JobGradesController(ErpGeneralDbContext db) { _db = db; }

    // GET /api/erp/job-grades
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<JobGradeDto>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool?   isActive)
    {
        var q = _db.JobGrades.AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(g => g.GradeName.Contains(search) || g.GradeCode.Contains(search));
        if (isActive.HasValue)
            q = q.Where(g => g.IsActive == isActive.Value);

        var list = await q.OrderBy(g => g.Level).ThenBy(g => g.GradeCode).ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    // GET /api/erp/job-grades/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<JobGradeDto>>> Get(Guid id)
    {
        var g = await _db.JobGrades.FindAsync(id);
        if (g == null) return NotFound(ApiHelper.Fail("Job grade not found"));
        return Ok(ToDto(g));
    }

    // POST /api/erp/job-grades
    [HttpPost]
    public async Task<ActionResult<ApiResponse<JobGradeDto>>> Create([FromBody] JobGradeUpsertDto req)
    {
        if (await _db.JobGrades.AnyAsync(g => g.GradeCode == req.GradeCode))
            return BadRequest(ApiHelper.Fail($"Grade code {req.GradeCode} already exists"));

        var grade = new JobGrade
        {
            GradeCode     = req.GradeCode,
            GradeName     = req.GradeName,
            Level         = req.Level ?? 1,
            MinSalary     = req.MinSalary ?? 0,
            MaxSalary     = req.MaxSalary ?? 0,
            OvertimeRate  = req.OvertimeRate ?? 1.33m,
            TravelLimit   = req.TravelLimit ?? 0,
            ApprovalLimit = req.ApprovalLimit ?? 0,
            IsActive      = req.IsActive ?? true,
            Description   = req.Description,
        };
        _db.JobGrades.Add(grade);
        await _db.SaveChangesAsync();
        return Ok(ToDto(grade), "Job grade created");
    }

    // PUT /api/erp/job-grades/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<JobGradeDto>>> Update(Guid id, [FromBody] JobGradeUpsertDto req)
    {
        var grade = await _db.JobGrades.FindAsync(id);
        if (grade == null) return NotFound(ApiHelper.Fail("Job grade not found"));

        if (await _db.JobGrades.AnyAsync(g => g.GradeCode == req.GradeCode && g.Id != id))
            return BadRequest(ApiHelper.Fail($"Grade code {req.GradeCode} already exists"));

        grade.GradeCode     = req.GradeCode;
        grade.GradeName     = req.GradeName;
        grade.Level         = req.Level ?? grade.Level;
        grade.MinSalary     = req.MinSalary ?? grade.MinSalary;
        grade.MaxSalary     = req.MaxSalary ?? grade.MaxSalary;
        grade.OvertimeRate  = req.OvertimeRate ?? grade.OvertimeRate;
        grade.TravelLimit   = req.TravelLimit ?? grade.TravelLimit;
        grade.ApprovalLimit = req.ApprovalLimit ?? grade.ApprovalLimit;
        grade.IsActive      = req.IsActive ?? grade.IsActive;
        grade.Description   = req.Description;

        await _db.SaveChangesAsync();
        return Ok(ToDto(grade), "Job grade updated");
    }

    // DELETE /api/erp/job-grades/{id}
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        var grade = await _db.JobGrades.FindAsync(id);
        if (grade == null) return NotFound(ApiHelper.Fail("Job grade not found"));
        if (await _db.JobTitles.AnyAsync(t => t.GradeId == id))
            return BadRequest(ApiHelper.Fail("Cannot delete: job titles still linked"));
        if (await _db.Set<Employee>().AnyAsync(e => e.GradeId == id))
            return BadRequest(ApiHelper.Fail("Cannot delete: employees still assigned"));
        _db.JobGrades.Remove(grade);
        await _db.SaveChangesAsync();
        return Ok("Job grade deleted");
    }

    private static JobGradeDto ToDto(JobGrade g) => new()
    {
        Id            = g.Id,
        GradeCode     = g.GradeCode,
        GradeName     = g.GradeName,
        Level         = g.Level,
        MinSalary     = g.MinSalary,
        MaxSalary     = g.MaxSalary,
        OvertimeRate  = g.OvertimeRate,
        TravelLimit   = g.TravelLimit,
        ApprovalLimit = g.ApprovalLimit,
        IsActive      = g.IsActive,
        Description   = g.Description,
        CreatedAt     = g.CreatedAt,
    };
}

public class JobGradeDto
{
    public Guid    Id            { get; set; }
    public string  GradeCode     { get; set; } = "";
    public string  GradeName     { get; set; } = "";
    public int     Level         { get; set; }
    public decimal MinSalary     { get; set; }
    public decimal MaxSalary     { get; set; }
    public decimal OvertimeRate  { get; set; }
    public decimal TravelLimit   { get; set; }
    public decimal ApprovalLimit { get; set; }
    public bool    IsActive      { get; set; }
    public string? Description   { get; set; }
    public DateTime CreatedAt    { get; set; }
}

public class JobGradeUpsertDto
{
    public string  GradeCode     { get; set; } = null!;
    public string  GradeName     { get; set; } = null!;
    public int?    Level         { get; set; }
    public decimal? MinSalary    { get; set; }
    public decimal? MaxSalary    { get; set; }
    public decimal? OvertimeRate { get; set; }
    public decimal? TravelLimit  { get; set; }
    public decimal? ApprovalLimit { get; set; }
    public bool?   IsActive      { get; set; }
    public string? Description   { get; set; }
}
