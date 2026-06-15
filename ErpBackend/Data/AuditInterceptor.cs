using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ErpBackend.Data;

public class AuditInterceptor : SaveChangesInterceptor
{
    // Ideally inject IHttpContextAccessor to get the real User ID
    // For now, we use a system user ID or parse from context if available.
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

        // In a real application, inject IHttpContextAccessor to get this ID
        // For demonstration, if we don't have it, we leave it null or set a mock ID.
        var currentUserId = Guid.Empty; // Mock System User

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                // Shadow properties
                var createdByProp = entry.Metadata.FindProperty("CreatedBy");
                if (createdByProp != null && entry.Property("CreatedBy").CurrentValue == null && currentUserId != Guid.Empty)
                {
                    entry.Property("CreatedBy").CurrentValue = currentUserId;
                }

                var createdAtProp = entry.Metadata.FindProperty("CreatedAt");
                if (createdAtProp != null && (DateTime)entry.Property("CreatedAt").CurrentValue! == default)
                {
                    entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                }

                // Normal properties if defined in class
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                {
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                var modifiedByProp = entry.Metadata.FindProperty("ModifiedBy");
                if (modifiedByProp != null && currentUserId != Guid.Empty)
                {
                    entry.Property("ModifiedBy").CurrentValue = currentUserId;
                }

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
}
