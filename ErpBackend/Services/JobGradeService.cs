using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Services;

public interface IJobGradeService
{
    Task<List<JobGradeDto>> GetAllAsync(string? search = null, bool? isActive = null);
    Task<JobGradeDto?> GetByIdAsync(Guid id);
    Task<(JobGradeDto? dto, string status)> CreateAsync(JobGradeUpsertDto request);
    Task<(JobGradeDto? dto, string status)> UpdateAsync(Guid id, JobGradeUpsertDto request);
    Task<(bool ok, string status)> DeleteAsync(Guid id);
}

public class JobGradeService : IJobGradeService
{
    private readonly ErpGeneralDbContext _db;

    public JobGradeService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<JobGradeDto>> GetAllAsync(string? search = null, bool? isActive = null)
    {
        var q = _db.JobGrades.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(g => g.GradeName.Contains(search) || g.GradeCode.Contains(search));
        if (isActive.HasValue)
            q = q.Where(g => g.IsActive == isActive.Value);

        var items = await q.OrderBy(g => g.Level).ThenBy(g => g.GradeCode).ToListAsync();
        return items.Select(MapToDto).ToList();
    }

    public async Task<JobGradeDto?> GetByIdAsync(Guid id)
    {
        var g = await _db.JobGrades.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
        return g == null ? null : MapToDto(g);
    }

    public async Task<(JobGradeDto? dto, string status)> CreateAsync(JobGradeUpsertDto request)
    {
        if (string.IsNullOrWhiteSpace(request.GradeCode) || string.IsNullOrWhiteSpace(request.GradeName))
            return (null, "invalid");

        var duplicate = await _db.JobGrades.AnyAsync(g => g.GradeCode == request.GradeCode);
        if (duplicate) return (null, "duplicate");

        var grade = new JobGrade
        {
            GradeCode = request.GradeCode,
            GradeName = request.GradeName,
            Level = request.Level ?? 1,
            MinSalary = request.MinSalary ?? 0,
            MaxSalary = request.MaxSalary ?? 0,
            OvertimeRate = request.OvertimeRate ?? 1.33m,
            TravelLimit = request.TravelLimit ?? 0,
            ApprovalLimit = request.ApprovalLimit ?? 0,
            IsActive = request.IsActive ?? true,
            Description = request.Description
        };

        _db.JobGrades.Add(grade);
        await _db.SaveChangesAsync();

        return (MapToDto(grade), "ok");
    }

    public async Task<(JobGradeDto? dto, string status)> UpdateAsync(Guid id, JobGradeUpsertDto request)
    {
        var grade = await _db.JobGrades.FirstOrDefaultAsync(g => g.Id == id);
        if (grade == null) return (null, "not_found");

        if (await _db.JobGrades.AnyAsync(g => g.GradeCode == request.GradeCode && g.Id != id))
            return (null, "duplicate");

        grade.GradeCode     = request.GradeCode;
        grade.GradeName     = request.GradeName;
        grade.Level         = request.Level ?? grade.Level;
        grade.MinSalary     = request.MinSalary ?? grade.MinSalary;
        grade.MaxSalary     = request.MaxSalary ?? grade.MaxSalary;
        grade.OvertimeRate  = request.OvertimeRate ?? grade.OvertimeRate;
        grade.TravelLimit   = request.TravelLimit ?? grade.TravelLimit;
        grade.ApprovalLimit = request.ApprovalLimit ?? grade.ApprovalLimit;
        grade.IsActive      = request.IsActive ?? grade.IsActive;
        grade.Description   = request.Description;

        await _db.SaveChangesAsync();

        return (MapToDto(grade), "ok");
    }

    public async Task<(bool ok, string status)> DeleteAsync(Guid id)
    {
        var grade = await _db.JobGrades.FirstOrDefaultAsync(g => g.Id == id);
        if (grade == null) return (false, "not_found");

        var hasJobTitles = await _db.JobTitles.AnyAsync(t => t.GradeId == id);
        if (hasJobTitles) return (false, "guarded");

        var hasEmployees = await _db.Employees.AnyAsync(e => e.GradeId == id);
        if (hasEmployees) return (false, "guarded");

        _db.JobGrades.Remove(grade);
        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    private static JobGradeDto MapToDto(JobGrade g) => new()
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
