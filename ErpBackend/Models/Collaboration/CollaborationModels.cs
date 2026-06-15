using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.Collaboration;

public class Announcement
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(200)] public string Title { get; set; } = null!;
    [MaxLength(50)] public string Category { get; set; } = "general";
    public string? Content { get; set; }
    public Guid? PublishDeptId { get; set; }
    public Guid? PublishBy { get; set; }
    public DateTime PublishDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [MaxLength(50)] public string TargetAudience { get; set; } = "all";
    public bool IsPinned { get; set; }
    public bool IsUrgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AnnouncementReadLog> ReadLogs { get; set; } = new List<AnnouncementReadLog>();
}

public class AnnouncementReadLog
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnnouncementId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AnnouncementId))] public Announcement Announcement { get; set; } = null!;
}

public class CalendarEvent
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)] public string Title { get; set; } = null!;
    public string? Description { get; set; }
    [MaxLength(50)] public string EventType { get; set; } = "company";
    [MaxLength(50)] public string? CategoryCode { get; set; }
    [MaxLength(200)] public string? Location { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAllDay { get; set; }

    public string? RecurrenceRule { get; set; }
    public Guid? RecurrenceParentId { get; set; }

    public string? Participants { get; set; }
    public Guid? OrganizerId { get; set; }
    public Guid? DepartmentId { get; set; }
    [MaxLength(20)] public string Visibility { get; set; } = "department";

    [MaxLength(30)] public string? SourceModule { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? MeetingRoomId { get; set; }
    public Guid? ProjectId { get; set; }

    [MaxLength(10)] public string? Color { get; set; }
    public bool IsPinned { get; set; }

    public string? Reminders { get; set; }

    [MaxLength(20)] public string Status { get; set; } = "confirmed";
    public bool SyncToOutlook { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(CategoryCode))] public CalendarCategory? Category { get; set; }
    [ForeignKey(nameof(MeetingRoomId))] public MeetingRoom? MeetingRoom { get; set; }

    public ICollection<CalendarReminderLog> ReminderLogs { get; set; } = new List<CalendarReminderLog>();
}

public class MeetingRoom
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string RoomName { get; set; } = null!;
    [MaxLength(100)] public string? Location { get; set; }
    public int Capacity { get; set; }
    [MaxLength(500)] public string? Equipment { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MeetingReservation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    [MaxLength(200)] public string? Subject { get; set; }
    public Guid ReserverId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Attendees { get; set; }
    public bool CateringRequired { get; set; }
    public string? EquipmentRequired { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "confirmed";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(RoomId))] public MeetingRoom Room { get; set; } = null!;
}

public class CrossDeptTask
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(200)] public string TaskName { get; set; } = null!;
    public string? Description { get; set; }
    [MaxLength(20)] public string Urgency { get; set; } = "normal";
    public Guid? InitiatedDeptId { get; set; }
    public Guid? InitiatedBy { get; set; }
    public Guid? OwnerDeptId { get; set; }
    public Guid? OwnerBy { get; set; }
    public string? Assistants { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTime AssignDate { get; set; }
    public DateTime? ExpectedCompleteDate { get; set; }
    public DateTime? ActualCompleteDate { get; set; }
    [MaxLength(500)] public string? VerificationNotes { get; set; }
    public int? SatisfactionScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CrossDeptTaskReply> Replies { get; set; } = new List<CrossDeptTaskReply>();
}

public class DepartmentPage
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DepartmentId { get; set; }
    [MaxLength(100)] public string? PageTitle { get; set; }
    public string? PageContent { get; set; }
    public string? SopsJson { get; set; }
    public string? KpiDataJson { get; set; }
    public string? QuickLinksJson { get; set; }
    public Guid? PrimaryContactId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CrossDeptTaskReply
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TaskId { get; set; }
    public Guid ReplierId { get; set; }
    public string? ReplyContent { get; set; }
    [MaxLength(500)] public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TaskId))] public CrossDeptTask Task { get; set; } = null!;
}

public class CalendarCategory
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string Code { get; set; } = null!;
    [Required, MaxLength(50)] public string Name { get; set; } = null!;
    [MaxLength(10)] public string Color { get; set; } = "#3B82F6";
    [MaxLength(50)] public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CalendarShare
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CalendarOwnerId { get; set; }
    public Guid? SharedWithEmployeeId { get; set; }
    public Guid? SharedWithDeptId { get; set; }
    public Guid? SharedWithRoleId { get; set; }
    [MaxLength(20)] public string Permission { get; set; } = "view";
    public bool CanSeeDetails { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CompanyHoliday
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(100)] public string Name { get; set; } = null!;
    public DateTime Date { get; set; }
    [MaxLength(20)] public string HolidayType { get; set; } = "national";
    public bool IsRecurring { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CalendarReminderLog
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CalendarEventId { get; set; }
    public Guid EmployeeId { get; set; }
    [MaxLength(20)] public string Channel { get; set; } = "system";
    public int MinutesBefore { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTime ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    [MaxLength(500)] public string? ErrorMessage { get; set; }

    [ForeignKey(nameof(CalendarEventId))] public CalendarEvent CalendarEvent { get; set; } = null!;
}
