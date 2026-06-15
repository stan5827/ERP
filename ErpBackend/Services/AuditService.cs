using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.System;

namespace ErpBackend.Services;

public interface IAuditService
{
    Task LogAsync(AuditEntry entry);
    Task LogLoginAsync(Guid userId, string username, string ipAddress, bool success, string? reason = null);
    Task LogLogoutAsync(Guid userId, string username, string ipAddress);
    Task LogSecurityEventAsync(string eventType, string message, Guid? userId = null, string? ipAddress = null, string? details = null);
    Task LogDataChangeAsync(Guid userId, string action, string tableName, Guid entityId, string? oldValue = null, string? newValue = null, string? ipAddress = null);
    Task LogSensitiveOperationAsync(Guid userId, string operation, string details, string? ipAddress = null);
    Task<List<AuditLog>> GetLogsAsync(DateTime from, DateTime to, string? action = null, Guid? userId = null);
}

public class AuditEntry
{
    public string Action { get; set; } = null!;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
    public string? TableName { get; set; }
    public string? Details { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? UserAgent { get; set; }
}

public class AuditService : IAuditService
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ErpGeneralDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(AuditEntry entry)
    {
        try
        {
            var log = new AuditLog
            {
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                UserId = entry.UserId,
                IpAddress = entry.IpAddress,
                TableName = entry.TableName,
                Details = entry.Details,
                UserAgent = entry.UserAgent,
                CreatedAt = DateTime.UtcNow
            };
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log: {Action}", entry.Action);
        }
    }

    public async Task LogLoginAsync(Guid userId, string username, string ipAddress, bool success, string? reason = null)
    {
        await LogAsync(new AuditEntry
        {
            Action = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED",
            UserId = userId,
            Username = username,
            IpAddress = ipAddress,
            TableName = "Users",
            Details = reason ?? (success ? "User logged in successfully" : "Invalid credentials")
        });
    }

    public async Task LogLogoutAsync(Guid userId, string username, string ipAddress)
    {
        await LogAsync(new AuditEntry
        {
            Action = "LOGOUT",
            UserId = userId,
            Username = username,
            IpAddress = ipAddress,
            TableName = "Users",
            Details = "User logged out"
        });
    }

    public async Task LogSecurityEventAsync(string eventType, string message, Guid? userId = null, string? ipAddress = null, string? details = null)
    {
        await LogAsync(new AuditEntry
        {
            Action = $"SECURITY_{eventType}",
            UserId = userId,
            IpAddress = ipAddress,
            TableName = "System",
            Details = details ?? message
        });
    }

    public async Task LogDataChangeAsync(Guid userId, string action, string tableName, Guid entityId, string? oldValue = null, string? newValue = null, string? ipAddress = null)
    {
        var details = $"Changed {tableName} record";
        if (!string.IsNullOrEmpty(oldValue) || !string.IsNullOrEmpty(newValue))
        {
            details += $": {oldValue ?? "null"} -> {newValue ?? "null"}";
        }

        await LogAsync(new AuditEntry
        {
            Action = action,
            UserId = userId,
            TableName = tableName,
            EntityId = entityId,
            IpAddress = ipAddress,
            OldValue = oldValue,
            NewValue = newValue,
            Details = details
        });
    }

    public async Task LogSensitiveOperationAsync(Guid userId, string operation, string details, string? ipAddress = null)
    {
        await LogAsync(new AuditEntry
        {
            Action = "SENSITIVE_OPERATION",
            UserId = userId,
            TableName = "System",
            IpAddress = ipAddress,
            Details = $"{operation}: {details}"
        });
    }

    public async Task<List<AuditLog>> GetLogsAsync(DateTime from, DateTime to, string? action = null, Guid? userId = null)
    {
        var query = _db.AuditLogs
            .Where(l => l.CreatedAt >= from && l.CreatedAt <= to);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(l => l.Action == action);

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        return await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
    }
}