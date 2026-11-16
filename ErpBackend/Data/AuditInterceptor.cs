using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.System;

namespace ErpBackend.Data;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context == null) return;

        var currentUserId = GetCurrentUserId();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.State == EntityState.Added)
            {
                if (currentUserId.HasValue)
                    SetAuditUser(entry, "CreatedBy", currentUserId.Value, onlyIfUnset: true);

                var createdAtProp = entry.Metadata.FindProperty("CreatedAt");
                if (createdAtProp != null)
                {
                    var createdAtValue = entry.Property("CreatedAt").CurrentValue;
                    var createdAt = createdAtValue is DateTime value && value != default
                        ? value
                        : DateTime.UtcNow;
                    entry.Property("CreatedAt").CurrentValue = NormalizeToUtc(createdAt);
                }

                var modifiedAtProp = entry.Metadata.FindProperty("ModifiedAt");
                if (modifiedAtProp != null)
                    entry.Property("ModifiedAt").CurrentValue = null;

                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                {
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }

                if (entry.Entity is InventoryTransaction inventoryTransaction && currentUserId.HasValue)
                {
                    if (!inventoryTransaction.HandlerId.HasValue || inventoryTransaction.HandlerId == Guid.Empty)
                        inventoryTransaction.HandlerId = currentUserId.Value;

                    var hasLinkedAuditLog = context.ChangeTracker.Entries<AuditLog>().Any(auditEntry =>
                        auditEntry.State != EntityState.Deleted &&
                        auditEntry.Entity.EntityType == nameof(InventoryTransaction) &&
                        auditEntry.Entity.EntityId == inventoryTransaction.Id);
                    if (!hasLinkedAuditLog)
                    {
                        var auditLog = new AuditLog
                        {
                            UserId = currentUserId.Value,
                            Action = "inventory.transaction.created",
                            EntityType = nameof(InventoryTransaction),
                            EntityId = inventoryTransaction.Id,
                            Details = $"交易 {inventoryTransaction.TrxId}；類型 {inventoryTransaction.Type}；SKU {inventoryTransaction.SkuId}；批號 {inventoryTransaction.LotNumber}；數量變更 {inventoryTransaction.QtyChange}",
                            TableName = nameof(ErpGeneralDbContext.InventoryTransactions),
                            CreatedAt = inventoryTransaction.CreatedAt
                        };
                        context.Add(auditLog);
                        SetAuditUser(context.Entry(auditLog), "CreatedBy", currentUserId.Value, onlyIfUnset: true);
                    }
                }
            }

            if (entry.State == EntityState.Modified)
            {
                if (currentUserId.HasValue)
                    SetAuditUser(entry, "ModifiedBy", currentUserId.Value, onlyIfUnset: false);

                var modifiedAtProp = entry.Metadata.FindProperty("ModifiedAt");
                if (modifiedAtProp != null)
                {
                    entry.Property("ModifiedAt").CurrentValue = DateTime.UtcNow;
                }

                // Normal properties if defined in class
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                {
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }

            }
        }
    }

    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private Guid? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value
            ?? user?.FindFirst("user_id")?.Value
            ?? user?.FindFirst("nameid")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) && userId != Guid.Empty
            ? userId
            : null;
    }

    private static void SetAuditUser(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        string propertyName,
        Guid userId,
        bool onlyIfUnset)
    {
        var property = entry.Metadata.FindProperty(propertyName);
        if (property == null ||
            (property.ClrType != typeof(Guid) && Nullable.GetUnderlyingType(property.ClrType) != typeof(Guid)))
            return;

        var propertyEntry = entry.Property(propertyName);
        if (!onlyIfUnset || propertyEntry.CurrentValue == null ||
            propertyEntry.CurrentValue is Guid existingUserId && existingUserId == Guid.Empty)
            propertyEntry.CurrentValue = userId;
    }
}
