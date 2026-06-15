using System;
using System.ComponentModel.DataAnnotations;

namespace ErpBackend.Data;

public class SystemNotification
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(100)] public string Title { get; set; } = null!;
    [Required] public string Content { get; set; } = null!;
    [MaxLength(50)] public string Type { get; set; } = "info"; // info, warning, success, error
    [MaxLength(50)] public string TargetRole { get; set; } = "all"; // procurement, sales, engineering, finance
    public string? Link { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
