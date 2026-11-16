using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class CreateAnnouncementRequestDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(50)]
    public string Category { get; set; } = "general";
    public string? Content { get; set; }
    public bool IsPinned { get; set; }
    public bool IsUrgent { get; set; }
    [Required, MaxLength(20)]
    public string TargetAudience { get; set; } = "all";
    public Guid? PublishDeptId { get; set; }
    public List<Guid> RecipientEmployeeIds { get; set; } = [];
    [Required]
    public DateTime PublishDate { get; set; }
}

public class AnnouncementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Content { get; set; }
    public bool IsPinned { get; set; }
    public bool IsUrgent { get; set; }
    public string TargetAudience { get; set; } = string.Empty;
    public Guid? PublishDeptId { get; set; }
    public string? PublishDeptName { get; set; }
    public DateTime PublishDate { get; set; }
    public bool IsRead { get; set; }
}
