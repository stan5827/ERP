using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;

namespace ErpBackend.Services;

public interface IJobTitleService
{
    Task<List<JobTitleDto>> GetAllAsync(string? search = null, Guid? gradeId = null, bool? isActive = null);
    Task<JobTitleDto?> GetByIdAsync(Guid id);
    Task<(JobTitleDto? dto, string status)> CreateAsync(JobTitleUpsertDto request);
    Task<(JobTitleDto? dto, string status)> UpdateAsync(Guid id, JobTitleUpsertDto request);
    Task<(bool ok, string status)> DeleteAsync(Guid id);
}

public class JobTitleService : IJobTitleService
{
    private readonly ErpGeneralDbContext _db;

    public JobTitleService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<JobTitleDto>> GetAllAsync(string? search = null, Guid? gradeId = null, bool? isActive = null)
    {
        var q = _db.JobTitles.Include(t => t.Grade).AsQueryable();

        if (!string.IsNullOrEmpty(search))
            q = q.Where(t => t.TitleName.Contains(search) || t.TitleCode.Contains(search));
        if (gradeId.HasValue)
            q = q.Where(t => t.GradeId == gradeId.Value);
        if (isActive.HasValue)
            q = q.Where(t => t.IsActive == isActive.Value);

        var titles = await q.OrderBy(t => t.TitleCode).Include(t => t.Grade).ToListAsync();
        var counts = await _db.Employees
            .Where(e => e.JobTitleId.HasValue)
            .GroupBy(e => e.JobTitleId!.Value)
            .Select(group => new { JobTitleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.JobTitleId, item => item.Count);

        foreach (var title in titles.Where(title => counts.GetValueOrDefault(title.Id) == 0))
        {
            counts[title.Id] = await _db.Employees.CountAsync(e =>
                e.PositionTitle == title.TitleName ||
                e.PositionTitle == title.TitleCode ||
                e.PositionTitle == $"{title.TitleCode} {title.TitleName}" ||
                e.PositionTitle == $"{title.TitleCode}-{title.TitleName}");
        }

        return titles.Select(title => ToDto(title, counts.GetValueOrDefault(title.Id))).ToList();
    }

    public async Task<JobTitleDto?> GetByIdAsync(Guid id)
    {
        var t = await _db.JobTitles.Include(t => t.Grade).AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (t == null) return null;
        var titleForCount = await _db.JobTitles.AsNoTracking().FirstAsync(item => item.Id == id);
        var employeeCount = await _db.Employees.CountAsync(e =>
            e.JobTitleId == id ||
            e.PositionTitle == titleForCount.TitleName ||
            e.PositionTitle == titleForCount.TitleCode ||
            e.PositionTitle == $"{titleForCount.TitleCode} {titleForCount.TitleName}" ||
            e.PositionTitle == $"{titleForCount.TitleCode}-{titleForCount.TitleName}");
        return ToDto(t, employeeCount);
    }

    public async Task<(JobTitleDto? dto, string status)> CreateAsync(JobTitleUpsertDto request)
    {
        if (string.IsNullOrWhiteSpace(request.TitleCode)) return (null, "invalid");
        if (await _db.JobTitles.AnyAsync(t => t.TitleCode == request.TitleCode)) return (null, "duplicate");

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var title = new JobTitle
            {
                TitleCode = request.TitleCode,
                TitleName = request.TitleName,
                GradeId = request.GradeId,
                ApplicableDeptType = request.ApplicableDeptType,
                IsSupervisor = request.IsSupervisor ?? false,
                IsActive = request.IsActive ?? true,
                Description = request.Description
            };

            _db.JobTitles.Add(title);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (await ProjectDtoAsync(title.Id), "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(JobTitleDto? dto, string status)> UpdateAsync(Guid id, JobTitleUpsertDto request)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var title = await _db.JobTitles.FirstOrDefaultAsync(t => t.Id == id);
            if (title == null) return (null, "not_found");

            if (!string.IsNullOrWhiteSpace(request.TitleCode) && request.TitleCode != title.TitleCode
                && await _db.JobTitles.AnyAsync(t => t.TitleCode == request.TitleCode && t.Id != id))
                return (null, "duplicate");

            title.TitleCode = request.TitleCode;
            title.TitleName = request.TitleName;
            title.GradeId = request.GradeId;
            title.ApplicableDeptType = request.ApplicableDeptType;
            title.IsSupervisor = request.IsSupervisor ?? title.IsSupervisor;
            title.IsActive = request.IsActive ?? title.IsActive;
            title.Description = request.Description;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return (await ProjectDtoAsync(id), "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<(bool ok, string status)> DeleteAsync(Guid id)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var title = await _db.JobTitles.FirstOrDefaultAsync(t => t.Id == id);
            if (title == null) return (false, "not_found");

            if (await _db.Employees.AnyAsync(e => e.JobTitleId == id)) return (false, "guarded");

            _db.JobTitles.Remove(title);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, "ok");
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<JobTitleDto> ProjectDtoAsync(Guid id)
    {
        var title = await _db.JobTitles.AsNoTracking().Include(t => t.Grade)
            .Where(t => t.Id == id)
            .FirstAsync();
        var employeeCount = await _db.Employees.CountAsync(e =>
            e.JobTitleId == id ||
            e.PositionTitle == title.TitleName ||
            e.PositionTitle == title.TitleCode ||
            e.PositionTitle == $"{title.TitleCode} {title.TitleName}" ||
            e.PositionTitle == $"{title.TitleCode}-{title.TitleName}");
        return ToDto(title, employeeCount);
    }

    private static JobTitleDto ToDto(JobTitle t, int employeeCount = 0) => new()
    {
        Id = t.Id,
        TitleCode = t.TitleCode,
        TitleName = t.TitleName,
        GradeId = t.GradeId,
        GradeCode = t.Grade?.GradeCode,
        GradeName = t.Grade?.GradeName,
        ApplicableDeptType = t.ApplicableDeptType,
        IsSupervisor = t.IsSupervisor,
        IsActive = t.IsActive,
        EmployeeCount = employeeCount,
        Description = t.Description,
        CreatedAt = t.CreatedAt
    };
}
