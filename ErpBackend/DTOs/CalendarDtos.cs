using System;
using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class CalendarEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EventType { get; set; } = "company";
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryColor { get; set; }
    public string? Location { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public string? RecurrenceRule { get; set; }
    public Guid? RecurrenceParentId { get; set; }
    public string? Participants { get; set; }
    public Guid? OrganizerId { get; set; }
    public string? OrganizerName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string Visibility { get; set; } = "department";
    public string? SourceModule { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? MeetingRoomId { get; set; }
    public string? MeetingRoomName { get; set; }
    public Guid? ProjectId { get; set; }
    public string? Color { get; set; }
    public bool IsPinned { get; set; }
    public string? Reminders { get; set; }
    public string Status { get; set; } = "confirmed";
    public bool SyncToOutlook { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCalendarEventRequestDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [MaxLength(50)]
    public string EventType { get; set; } = "company";
    [MaxLength(50)]
    public string? CategoryCode { get; set; }
    [MaxLength(200)]
    public string? Location { get; set; }
    [Required]
    public DateTime StartDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }
    public bool IsAllDay { get; set; }
    public string? RecurrenceRule { get; set; }
    public Guid? RecurrenceParentId { get; set; }
    public string? Participants { get; set; }
    public Guid? OrganizerId { get; set; }
    public Guid? DepartmentId { get; set; }
    [MaxLength(20)]
    public string Visibility { get; set; } = "department";
    public string? SourceModule { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? MeetingRoomId { get; set; }
    public Guid? ProjectId { get; set; }
    [MaxLength(10)]
    public string? Color { get; set; }
    public bool IsPinned { get; set; }
    public string? Reminders { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "confirmed";
    public bool SyncToOutlook { get; set; }
}

public class UpdateCalendarEventRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? EventType { get; set; }
    public string? CategoryCode { get; set; }
    public string? Location { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsAllDay { get; set; }
    public string? RecurrenceRule { get; set; }
    public Guid? RecurrenceParentId { get; set; }
    public string? Participants { get; set; }
    public Guid? OrganizerId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Visibility { get; set; }
    public string? SourceModule { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? MeetingRoomId { get; set; }
    public Guid? ProjectId { get; set; }
    public string? Color { get; set; }
    public bool? IsPinned { get; set; }
    public string? Reminders { get; set; }
    public string? Status { get; set; }
    public bool? SyncToOutlook { get; set; }
}

public class UpdateEventStatusRequestDto
{
    [Required, MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}

public class CalendarCategoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCalendarCategoryRequestDto
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;
    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(10)]
    public string Color { get; set; } = "#3B82F6";
    [MaxLength(50)]
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCalendarCategoryRequestDto
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public class CompanyHolidayDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string HolidayType { get; set; } = "national";
    public bool IsRecurring { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCompanyHolidayRequestDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [Required]
    public DateTime Date { get; set; }
    [MaxLength(20)]
    public string HolidayType { get; set; } = "national";
    public bool IsRecurring { get; set; }
    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpdateCompanyHolidayRequestDto
{
    public string? Name { get; set; }
    public DateTime? Date { get; set; }
    public string? HolidayType { get; set; }
    public bool? IsRecurring { get; set; }
    public string? Description { get; set; }
}

public class CalendarShareDto
{
    public Guid Id { get; set; }
    public Guid CalendarOwnerId { get; set; }
    public string? CalendarOwnerName { get; set; }
    public Guid? SharedWithEmployeeId { get; set; }
    public string? SharedWithEmployeeName { get; set; }
    public Guid? SharedWithDeptId { get; set; }
    public string? SharedWithDeptName { get; set; }
    public Guid? SharedWithRoleId { get; set; }
    public string Permission { get; set; } = "view";
    public bool CanSeeDetails { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCalendarShareRequestDto
{
    [Required]
    public Guid CalendarOwnerId { get; set; }
    public Guid? SharedWithEmployeeId { get; set; }
    public Guid? SharedWithDeptId { get; set; }
    public Guid? SharedWithRoleId { get; set; }
    [MaxLength(20)]
    public string Permission { get; set; } = "view";
    public bool CanSeeDetails { get; set; } = true;
}
