using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Collaboration;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface ICalendarService
{
    Task<List<CalendarEventDto>> GetEventsAsync(Guid userId, Dictionary<string, string>? parameters);
    Task<List<CalendarEventDto>> GetRangeAsync(DateTime startDate, DateTime endDate, Guid userId);
    Task<CalendarEventDto?> GetEventAsync(Guid id, Guid userId);
    Task<CalendarEventDto> CreateEventAsync(CreateCalendarEventRequestDto request, Guid userId);
    Task<CalendarEventDto?> UpdateEventAsync(Guid id, UpdateCalendarEventRequestDto request, Guid userId);
    Task<bool> DeleteEventAsync(Guid id, Guid userId);
    Task<bool> UpdateEventStatusAsync(Guid id, string status, Guid userId);
    Task<List<CalendarEventDto>> GetConflictsAsync(Guid userId, DateTime startDate, DateTime endDate, Guid? excludeEventId = null);
    Task<List<CalendarCategoryDto>> GetCategoriesAsync();
    Task<CalendarCategoryDto> CreateCategoryAsync(CreateCalendarCategoryRequestDto request);
    Task<bool> UpdateCategoryAsync(Guid id, UpdateCalendarCategoryRequestDto request);
    Task<bool> DeleteCategoryAsync(Guid id);
    Task<List<CompanyHolidayDto>> GetHolidaysAsync(Dictionary<string, string>? parameters);
    Task<CompanyHolidayDto> CreateHolidayAsync(CreateCompanyHolidayRequestDto request);
    Task<bool> UpdateHolidayAsync(Guid id, UpdateCompanyHolidayRequestDto request);
    Task<bool> DeleteHolidayAsync(Guid id);
    Task<List<CalendarShareDto>> GetSharesAsync(Dictionary<string, string>? parameters);
    Task<CalendarShareDto> CreateShareAsync(CreateCalendarShareRequestDto request, Guid userId);
    Task<bool> DeleteShareAsync(Guid id, Guid userId);
}

public class CalendarService : ICalendarService
{
    private readonly ErpGeneralDbContext _db;

    public CalendarService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<CalendarEventDto>> GetEventsAsync(Guid userId, Dictionary<string, string>? parameters)
    {
        var query = _db.CalendarEvents
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.MeetingRoom)
            .Where(e => e.StartDate <= DateTime.UtcNow.AddDays(365) && e.EndDate >= DateTime.UtcNow.AddDays(-365));

        if (parameters != null)
        {
            if (parameters.TryGetValue("startDate", out var startStr) && DateTime.TryParse(startStr, out var startDate))
                query = query.Where(e => e.EndDate >= startDate);
            if (parameters.TryGetValue("endDate", out var endStr) && DateTime.TryParse(endStr, out var endDate))
                query = query.Where(e => e.StartDate <= endDate);
            if (parameters.TryGetValue("categoryCode", out var categoryCode) && !string.IsNullOrWhiteSpace(categoryCode))
                query = query.Where(e => e.CategoryCode == categoryCode);
            if (parameters.TryGetValue("eventType", out var eventType) && !string.IsNullOrWhiteSpace(eventType))
                query = query.Where(e => e.EventType == eventType);
            if (parameters.TryGetValue("departmentId", out var deptStr) && Guid.TryParse(deptStr, out var deptId))
                query = query.Where(e => e.DepartmentId == deptId);
            if (parameters.TryGetValue("visibility", out var visibility) && !string.IsNullOrWhiteSpace(visibility))
                query = query.Where(e => e.Visibility == visibility || e.Visibility == "all" || e.OrganizerId == userId);
        }

        return await query
            .OrderBy(e => e.StartDate)
            .Select(e => MapEventToDto(e, _db))
            .ToListAsync();
    }

    public async Task<List<CalendarEventDto>> GetRangeAsync(DateTime startDate, DateTime endDate, Guid userId)
    {
        return await _db.CalendarEvents
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.MeetingRoom)
            .Where(e => e.StartDate <= endDate && e.EndDate >= startDate)
            .Where(e => e.Visibility == "all" || e.Visibility == "department" || e.OrganizerId == userId)
            .OrderBy(e => e.StartDate)
            .Select(e => MapEventToDto(e, _db))
            .ToListAsync();
    }

    public async Task<CalendarEventDto?> GetEventAsync(Guid id, Guid userId)
    {
        var e = await _db.CalendarEvents
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.MeetingRoom)
            .FirstOrDefaultAsync(e => e.Id == id);
        return e == null ? null : MapEventToDto(e, _db);
    }

    public async Task<CalendarEventDto> CreateEventAsync(CreateCalendarEventRequestDto request, Guid userId)
    {
        var entity = new CalendarEvent
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            EventType = request.EventType,
            CategoryCode = request.CategoryCode,
            Location = request.Location,
            StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc),
            IsAllDay = request.IsAllDay,
            RecurrenceRule = request.RecurrenceRule,
            RecurrenceParentId = request.RecurrenceParentId,
            Participants = request.Participants,
            OrganizerId = request.OrganizerId ?? userId,
            DepartmentId = request.DepartmentId,
            Visibility = request.Visibility,
            SourceModule = request.SourceModule,
            SourceId = request.SourceId,
            MeetingRoomId = request.MeetingRoomId,
            ProjectId = request.ProjectId,
            Color = request.Color,
            IsPinned = request.IsPinned,
            Reminders = request.Reminders,
            Status = request.Status,
            SyncToOutlook = request.SyncToOutlook,
            CreatedAt = DateTime.UtcNow
        };

        _db.CalendarEvents.Add(entity);
        await _db.SaveChangesAsync();
        return await GetEventAsync(entity.Id, userId) ?? MapEventToDto(entity, _db);
    }

    public async Task<CalendarEventDto?> UpdateEventAsync(Guid id, UpdateCalendarEventRequestDto request, Guid userId)
    {
        var entity = await _db.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null) return null;

        if (request.Title != null) entity.Title = request.Title.Trim();
        if (request.Description != null) entity.Description = request.Description;
        if (request.EventType != null) entity.EventType = request.EventType;
        if (request.CategoryCode != null) entity.CategoryCode = request.CategoryCode;
        if (request.Location != null) entity.Location = request.Location;
        if (request.StartDate.HasValue) entity.StartDate = DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc);
        if (request.EndDate.HasValue) entity.EndDate = DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc);
        if (request.IsAllDay.HasValue) entity.IsAllDay = request.IsAllDay.Value;
        if (request.RecurrenceRule != null) entity.RecurrenceRule = request.RecurrenceRule;
        if (request.RecurrenceParentId.HasValue) entity.RecurrenceParentId = request.RecurrenceParentId;
        if (request.Participants != null) entity.Participants = request.Participants;
        if (request.OrganizerId.HasValue) entity.OrganizerId = request.OrganizerId;
        if (request.DepartmentId.HasValue) entity.DepartmentId = request.DepartmentId;
        if (request.Visibility != null) entity.Visibility = request.Visibility;
        if (request.SourceModule != null) entity.SourceModule = request.SourceModule;
        if (request.SourceId.HasValue) entity.SourceId = request.SourceId;
        if (request.MeetingRoomId.HasValue) entity.MeetingRoomId = request.MeetingRoomId;
        if (request.ProjectId.HasValue) entity.ProjectId = request.ProjectId;
        if (request.Color != null) entity.Color = request.Color;
        if (request.IsPinned.HasValue) entity.IsPinned = request.IsPinned.Value;
        if (request.Reminders != null) entity.Reminders = request.Reminders;
        if (request.Status != null) entity.Status = request.Status;
        if (request.SyncToOutlook.HasValue) entity.SyncToOutlook = request.SyncToOutlook.Value;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return await GetEventAsync(id, userId);
    }

    public async Task<bool> DeleteEventAsync(Guid id, Guid userId)
    {
        var entity = await _db.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null) return false;
        _db.CalendarEvents.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateEventStatusAsync(Guid id, string status, Guid userId)
    {
        var entity = await _db.CalendarEvents.FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null) return false;
        entity.Status = status;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<CalendarEventDto>> GetConflictsAsync(Guid userId, DateTime startDate, DateTime endDate, Guid? excludeEventId = null)
    {
        var query = _db.CalendarEvents
            .AsNoTracking()
            .Include(e => e.Category)
            .Where(e => e.StartDate < endDate && e.EndDate > startDate)
            .Where(e => e.Visibility == "all" || e.Visibility == "department" || e.OrganizerId == userId);

        if (excludeEventId.HasValue)
            query = query.Where(e => e.Id != excludeEventId.Value);

        return await query
            .OrderBy(e => e.StartDate)
            .Select(e => MapEventToDto(e, _db))
            .ToListAsync();
    }

    public async Task<List<CalendarCategoryDto>> GetCategoriesAsync()
    {
        return await _db.CalendarCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CalendarCategoryDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Color = c.Color,
                Icon = c.Icon,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CalendarCategoryDto> CreateCategoryAsync(CreateCalendarCategoryRequestDto request)
    {
        if (await _db.CalendarCategories.AnyAsync(c => c.Code == request.Code.Trim()))
            throw new ArgumentException("????????");

        var entity = new CalendarCategory
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Color = request.Color,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.CalendarCategories.Add(entity);
        await _db.SaveChangesAsync();

        return new CalendarCategoryDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Color = entity.Color,
            Icon = entity.Icon,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<bool> UpdateCategoryAsync(Guid id, UpdateCalendarCategoryRequestDto request)
    {
        var entity = await _db.CalendarCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null) return false;

        if (request.Code != null)
        {
            if (await _db.CalendarCategories.AnyAsync(c => c.Code == request.Code.Trim() && c.Id != id))
                throw new ArgumentException("????????");
            entity.Code = request.Code.Trim();
        }
        if (request.Name != null) entity.Name = request.Name.Trim();
        if (request.Color != null) entity.Color = request.Color;
        if (request.Icon != null) entity.Icon = request.Icon;
        if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        var entity = await _db.CalendarCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null) return false;
        _db.CalendarCategories.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<CompanyHolidayDto>> GetHolidaysAsync(Dictionary<string, string>? parameters)
    {
        var query = _db.CompanyHolidays.AsNoTracking();

        if (parameters != null)
        {
            if (parameters.TryGetValue("year", out var yearStr) && int.TryParse(yearStr, out var year))
            {
                var start = new DateTime(year, 1, 1);
                var end = new DateTime(year, 12, 31);
                query = query.Where(h => h.Date >= start && h.Date <= end);
            }
        }

        return await query
            .OrderBy(h => h.Date)
            .Select(h => new CompanyHolidayDto
            {
                Id = h.Id,
                Name = h.Name,
                Date = h.Date,
                HolidayType = h.HolidayType,
                IsRecurring = h.IsRecurring,
                Description = h.Description,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CompanyHolidayDto> CreateHolidayAsync(CreateCompanyHolidayRequestDto request)
    {
        var entity = new CompanyHoliday
        {
            Name = request.Name.Trim(),
            Date = DateTime.SpecifyKind(request.Date, DateTimeKind.Utc),
            HolidayType = request.HolidayType,
            IsRecurring = request.IsRecurring,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _db.CompanyHolidays.Add(entity);
        await _db.SaveChangesAsync();

        return new CompanyHolidayDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Date = entity.Date,
            HolidayType = entity.HolidayType,
            IsRecurring = entity.IsRecurring,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<bool> UpdateHolidayAsync(Guid id, UpdateCompanyHolidayRequestDto request)
    {
        var entity = await _db.CompanyHolidays.FirstOrDefaultAsync(h => h.Id == id);
        if (entity == null) return false;

        if (request.Name != null) entity.Name = request.Name.Trim();
        if (request.Date.HasValue) entity.Date = DateTime.SpecifyKind(request.Date.Value, DateTimeKind.Utc);
        if (request.HolidayType != null) entity.HolidayType = request.HolidayType;
        if (request.IsRecurring.HasValue) entity.IsRecurring = request.IsRecurring.Value;
        if (request.Description != null) entity.Description = request.Description;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteHolidayAsync(Guid id)
    {
        var entity = await _db.CompanyHolidays.FirstOrDefaultAsync(h => h.Id == id);
        if (entity == null) return false;
        _db.CompanyHolidays.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<CalendarShareDto>> GetSharesAsync(Dictionary<string, string>? parameters)
    {
        var query = _db.CalendarShares.AsNoTracking();

        if (parameters != null && parameters.TryGetValue("ownerId", out var ownerIdStr) && Guid.TryParse(ownerIdStr, out var ownerId))
            query = query.Where(s => s.CalendarOwnerId == ownerId);

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new CalendarShareDto
            {
                Id = s.Id,
                CalendarOwnerId = s.CalendarOwnerId,
                SharedWithEmployeeId = s.SharedWithEmployeeId,
                SharedWithDeptId = s.SharedWithDeptId,
                SharedWithRoleId = s.SharedWithRoleId,
                Permission = s.Permission,
                CanSeeDetails = s.CanSeeDetails,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<CalendarShareDto> CreateShareAsync(CreateCalendarShareRequestDto request, Guid userId)
    {
        var entity = new CalendarShare
        {
            CalendarOwnerId = request.CalendarOwnerId,
            SharedWithEmployeeId = request.SharedWithEmployeeId,
            SharedWithDeptId = request.SharedWithDeptId,
            SharedWithRoleId = request.SharedWithRoleId,
            Permission = request.Permission,
            CanSeeDetails = request.CanSeeDetails,
            CreatedAt = DateTime.UtcNow
        };

        _db.CalendarShares.Add(entity);
        await _db.SaveChangesAsync();

        return new CalendarShareDto
        {
            Id = entity.Id,
            CalendarOwnerId = entity.CalendarOwnerId,
            SharedWithEmployeeId = entity.SharedWithEmployeeId,
            SharedWithDeptId = entity.SharedWithDeptId,
            SharedWithRoleId = entity.SharedWithRoleId,
            Permission = entity.Permission,
            CanSeeDetails = entity.CanSeeDetails,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<bool> DeleteShareAsync(Guid id, Guid userId)
    {
        var entity = await _db.CalendarShares.FirstOrDefaultAsync(s => s.Id == id);
        if (entity == null) return false;
        _db.CalendarShares.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    private static CalendarEventDto MapEventToDto(CalendarEvent e, ErpGeneralDbContext db)
    {
        return new CalendarEventDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            EventType = e.EventType,
            CategoryCode = e.CategoryCode,
            CategoryName = e.Category?.Name,
            CategoryColor = e.Category?.Color,
            Location = e.Location,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            IsAllDay = e.IsAllDay,
            RecurrenceRule = e.RecurrenceRule,
            RecurrenceParentId = e.RecurrenceParentId,
            Participants = e.Participants,
            OrganizerId = e.OrganizerId,
            DepartmentId = e.DepartmentId,
            Visibility = e.Visibility,
            SourceModule = e.SourceModule,
            SourceId = e.SourceId,
            MeetingRoomId = e.MeetingRoomId,
            MeetingRoomName = e.MeetingRoom?.RoomName,
            ProjectId = e.ProjectId,
            Color = e.Color,
            IsPinned = e.IsPinned,
            Reminders = e.Reminders,
            Status = e.Status,
            SyncToOutlook = e.SyncToOutlook,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
