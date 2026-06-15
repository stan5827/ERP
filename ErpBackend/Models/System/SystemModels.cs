using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.System;

public class User
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string Username { get; set; } = null!;
    [Required, MaxLength(500)] public string PasswordHash { get; set; } = null!;
    [MaxLength(200)] public string? Email { get; set; }
    public Guid? EmployeeId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    [MaxLength(50)] public string? LastLoginIp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(50)] public string? ThemeName { get; set; }
    [MaxLength(10)] public string? CustomPrimaryColor { get; set; }
    [MaxLength(10)] public string? CustomAccentColor { get; set; }
    
    public bool MustChangePassword { get; set; } = false;
    public DateTime? PasswordChangedAt { get; set; }
    public int PasswordHistoryCount { get; set; } = 0;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

public class Role
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string RoleName { get; set; } = null!;
    [MaxLength(200)] public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class Permission
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    [Required, MaxLength(100)] public string PermissionCode { get; set; } = null!;
    [Required, MaxLength(50)] public string ModuleName { get; set; } = null!;
    [Required, MaxLength(50)] public string ActionType { get; set; } = null!;
    [Required, MaxLength(20)] public string PermissionType { get; set; } = "action";
    [MaxLength(200)] public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(ParentId))]
    public Permission? Parent { get; set; }
    public ICollection<Permission> Children { get; set; } = new List<Permission>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid RoleId { get; set; }
    [Required] public Guid PermissionId { get; set; }

    [ForeignKey(nameof(RoleId))] public Role Role { get; set; } = null!;
    [ForeignKey(nameof(PermissionId))] public Permission Permission { get; set; } = null!;
}

public class UserRole
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid UserId { get; set; }
    [Required] public Guid RoleId { get; set; }

    [ForeignKey(nameof(UserId))] public User User { get; set; } = null!;
    [ForeignKey(nameof(RoleId))] public Role Role { get; set; } = null!;
}

public class AuditLog
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string Action { get; set; } = null!;
    [MaxLength(200)] public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    [MaxLength(500)] public string? Details { get; set; }
    public Guid? UserId { get; set; }
    [MaxLength(50)] public string? IpAddress { get; set; }
    [MaxLength(100)] public string? TableName { get; set; }
    [MaxLength(500)] public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

public class AgentExecution
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string ExecutionId { get; set; } = null!;
    [Required, MaxLength(50)] public string PlanId { get; set; } = null!;
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public decimal SuccessRate { get; set; }
    public decimal Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Context { get; set; }
    public string? Result { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AgentTask
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgentExecutionId { get; set; }
    [Required, MaxLength(50)] public string TaskId { get; set; } = null!;
    [Required, MaxLength(200)] public string Description { get; set; } = null!;
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public string? ToolUsed { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public decimal Duration { get; set; }
    public int Priority { get; set; } = 50;
    public string? Dependencies { get; set; }

    [ForeignKey(nameof(AgentExecutionId))]
    public AgentExecution? AgentExecution { get; set; }
}

public class SystemConfig
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(100)] public string ConfigKey { get; set; } = null!;
    public string? ConfigValue { get; set; }
    [MaxLength(20)] public string DataType { get; set; } = "string";
    [MaxLength(500)] public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class NotificationQueue
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string Type { get; set; } = null!;
    public Guid? TargetUserId { get; set; }
    [MaxLength(200)] public string? Title { get; set; }
    public string? Content { get; set; }
    [MaxLength(50)] public string Channel { get; set; } = "system";
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserThemePreference
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid UserId { get; set; }
    [Required, MaxLength(50)] public string ThemeName { get; set; } = "modern-dark";
    [MaxLength(10)] public string? CustomPrimaryColor { get; set; }
    [MaxLength(10)] public string? CustomAccentColor { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}

public class PasswordHistory
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid UserId { get; set; }
    [Required, MaxLength(500)] public string PasswordHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}

public class SystemJob
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(100)] public string JobName { get; set; } = null!;
    [Required, MaxLength(50)] public string JobType { get; set; } = null!;
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public int Progress { get; set; }
    [MaxLength(200)] public string? CurrentStep { get; set; }
    public string? Parameters { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
}
