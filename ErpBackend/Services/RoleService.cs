using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;

namespace ErpBackend.Services;

public interface IRoleService
{
    Task<List<RoleSummaryDto>> GetActiveRolesAsync();
}

public class RoleService : IRoleService
{
    private readonly ErpGeneralDbContext _db;

    public RoleService(ErpGeneralDbContext db) => _db = db;

    public Task<List<RoleSummaryDto>> GetActiveRolesAsync() =>
        _db.Roles
            .AsNoTracking()
            .Where(role => role.IsActive)
            .OrderBy(role => role.RoleName)
            .Select(role => new RoleSummaryDto
            {
                Id = role.Id,
                RoleName = role.RoleName,
                Description = role.Description,
                IsSystem = role.IsSystem
            })
            .ToListAsync();
}
