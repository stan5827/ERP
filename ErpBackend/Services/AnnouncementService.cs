using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Collaboration;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface IAnnouncementService
{
    Task<List<AnnouncementDto>> GetAnnouncementsAsync(Guid userId);
    Task<AnnouncementDto> CreateAnnouncementAsync(CreateAnnouncementRequestDto request, Guid userId);
    Task<bool> MarkReadAsync(Guid announcementId, Guid userId);
}

public class AnnouncementService : IAnnouncementService
{
    private readonly ErpGeneralDbContext _db;

    public AnnouncementService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<AnnouncementDto>> GetAnnouncementsAsync(Guid userId)
    {
        var employeeId = await GetEmployeeIdAsync(userId);
        var departmentId = employeeId.HasValue
            ? await _db.Employees.Where(employee => employee.Id == employeeId.Value)
                .Select(employee => employee.DepartmentId).FirstOrDefaultAsync()
            : null;
        var now = DateTime.UtcNow;

        var query = _db.Announcements
            .AsNoTracking()
            .Where(announcement => announcement.PublishDate <= now
                && (!announcement.StartDate.HasValue || announcement.StartDate <= now)
                && (!announcement.EndDate.HasValue || announcement.EndDate >= now));

        if (employeeId.HasValue)
        {
            var currentEmployeeId = employeeId.Value;
            query = query.Where(announcement =>
                announcement.PublishBy == userId
                || announcement.TargetAudience == "all"
                || (announcement.TargetAudience == "department" && departmentId.HasValue
                    && announcement.PublishDeptId == departmentId.Value)
                || (announcement.TargetAudience == "custom"
                    && announcement.Recipients.Any(recipient => recipient.EmployeeId == currentEmployeeId)));
        }
        else
        {
            query = query.Where(announcement =>
                announcement.PublishBy == userId || announcement.TargetAudience == "all");
        }

        return await query
            .OrderByDescending(announcement => announcement.IsPinned)
            .ThenByDescending(announcement => announcement.PublishDate)
            .Select(announcement => new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Category = announcement.Category,
                Content = announcement.Content,
                IsPinned = announcement.IsPinned,
                IsUrgent = announcement.IsUrgent,
                TargetAudience = announcement.TargetAudience,
                PublishDeptId = announcement.PublishDeptId,
                PublishDeptName = _db.Departments
                    .Where(department => department.Id == announcement.PublishDeptId)
                    .Select(department => department.DeptName)
                    .FirstOrDefault(),
                PublishDate = announcement.PublishDate,
                IsRead = employeeId.HasValue && _db.AnnouncementReadLogs.Any(log =>
                    log.AnnouncementId == announcement.Id && log.EmployeeId == employeeId.Value)
            })
            .ToListAsync();
    }

    public async Task<AnnouncementDto> CreateAnnouncementAsync(CreateAnnouncementRequestDto request, Guid userId)
    {
        var audience = request.TargetAudience.Trim().ToLowerInvariant();
        if (audience is not ("all" or "department" or "custom"))
            throw new ArgumentException("發布對象必須選擇全體、部門或自訂。");

        Guid? publishDeptId = null;
        var recipientEmployeeIds = request.RecipientEmployeeIds.Distinct().ToList();
        if (audience == "department")
        {
            if (!request.PublishDeptId.HasValue)
                throw new ArgumentException("選擇部門發布時，必須指定部門。");
            if (!await _db.Departments.AnyAsync(department =>
                    department.Id == request.PublishDeptId.Value && department.IsActive))
                throw new ArgumentException("指定的部門不存在或已停用。");
            publishDeptId = request.PublishDeptId;
        }
        else if (audience == "custom")
        {
            if (recipientEmployeeIds.Count == 0)
                throw new ArgumentException("選擇自訂發布時，至少要指定一位收件人。");

            var activeEmployeeCount = await _db.Employees.CountAsync(employee =>
                recipientEmployeeIds.Contains(employee.Id) && employee.Status == "active");
            if (activeEmployeeCount != recipientEmployeeIds.Count)
                throw new ArgumentException("部分指定人員不存在或已非在職狀態，請重新選擇。");
        }

        var announcement = new Announcement
        {
            Title = request.Title.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "general" : request.Category.Trim(),
            Content = request.Content,
            IsPinned = request.IsPinned,
            IsUrgent = request.IsUrgent,
            TargetAudience = audience,
            PublishDeptId = publishDeptId,
            PublishBy = userId == Guid.Empty ? null : userId,
            PublishDate = DateTime.SpecifyKind(request.PublishDate.Date, DateTimeKind.Utc)
        };

        foreach (var employeeId in recipientEmployeeIds)
        {
            announcement.Recipients.Add(new AnnouncementRecipient
            {
                AnnouncementId = announcement.Id,
                EmployeeId = employeeId
            });
        }

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync();

        return new AnnouncementDto
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Category = announcement.Category,
            Content = announcement.Content,
            IsPinned = announcement.IsPinned,
            IsUrgent = announcement.IsUrgent,
            TargetAudience = announcement.TargetAudience,
            PublishDeptId = announcement.PublishDeptId,
            PublishDeptName = publishDeptId.HasValue
                ? await _db.Departments.Where(department => department.Id == publishDeptId.Value)
                    .Select(department => department.DeptName).FirstOrDefaultAsync()
                : null,
            PublishDate = announcement.PublishDate
        };
    }

    public async Task<bool> MarkReadAsync(Guid announcementId, Guid userId)
    {
        var employeeId = await GetEmployeeIdAsync(userId);
        if (!employeeId.HasValue)
            return false;

        var announcement = await _db.Announcements
            .AsNoTracking()
            .Where(item => item.Id == announcementId && item.PublishDate <= DateTime.UtcNow)
            .Select(item => new { item.Id, item.TargetAudience, item.PublishDeptId })
            .FirstOrDefaultAsync();
        if (announcement is null)
            return false;

        var employeeDepartmentId = await _db.Employees
            .Where(employee => employee.Id == employeeId.Value)
            .Select(employee => employee.DepartmentId)
            .FirstOrDefaultAsync();
        var isRecipient = announcement.TargetAudience == "all"
            || (announcement.TargetAudience == "department"
                && announcement.PublishDeptId == employeeDepartmentId)
            || (announcement.TargetAudience == "custom"
                && await _db.AnnouncementRecipients.AnyAsync(recipient =>
                    recipient.AnnouncementId == announcementId && recipient.EmployeeId == employeeId.Value));
        if (!isRecipient)
            return false;

        if (!await _db.AnnouncementReadLogs.AnyAsync(log =>
                log.AnnouncementId == announcementId && log.EmployeeId == employeeId.Value))
        {
            _db.AnnouncementReadLogs.Add(new AnnouncementReadLog
            {
                AnnouncementId = announcementId,
                EmployeeId = employeeId.Value,
                ReadAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return true;
    }

    private Task<Guid?> GetEmployeeIdAsync(Guid userId) =>
        _db.Users.Where(user => user.Id == userId).Select(user => user.EmployeeId).FirstOrDefaultAsync();
}
