using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.System;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public class SystemSettingsSecurityVersionTests
{
    private static ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    [Fact]
    public async Task UpdatePermissionAsync_IncrementsVersionsForUsersWithPermission()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "permission-user", PasswordHash = "hash" };
        var role = new Role { RoleName = "permission-role" };
        var permission = new Permission
        {
            PermissionCode = "inventory:read",
            ModuleName = "inventory",
            ActionType = "read"
        };
        db.AddRange(user, role, permission);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await db.SaveChangesAsync();

        var service = new SystemSettingsService(db);
        var updated = await service.UpdatePermissionAsync(permission.Id, new UpdatePermissionRequest
        {
            PermissionCode = "inventory:view",
            ModuleName = "inventory",
            ActionType = "view"
        });

        Assert.NotNull(updated);
        Assert.Equal(1, user.SecurityVersion);
    }

    [Fact]
    public async Task DeletePermissionAsync_IncrementsVersionsForUsersWithPermission()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "permission-user", PasswordHash = "hash" };
        var role = new Role { RoleName = "permission-role" };
        var permission = new Permission
        {
            PermissionCode = "inventory:read",
            ModuleName = "inventory",
            ActionType = "read"
        };
        db.AddRange(user, role, permission);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await db.SaveChangesAsync();

        var service = new SystemSettingsService(db);
        Assert.True(await service.DeletePermissionAsync(permission.Id));

        Assert.Equal(1, user.SecurityVersion);
    }

    [Fact]
    public async Task DeleteRoleAsync_IncrementsVersionsForUsersAssignedToRole()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "role-user", PasswordHash = "hash" };
        var role = new Role { RoleName = "temporary-role" };
        db.AddRange(user, role);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();

        var service = new SystemSettingsService(db);
        Assert.True(await service.DeleteRoleAsync(role.Id));

        Assert.Equal(1, user.SecurityVersion);
    }

    [Fact]
    public async Task UserAndRoleLists_IncludeEmployeeLinkAndRoleStatus()
    {
        using var db = CreateInMemoryDbContext();
        var employeeId = Guid.NewGuid();
        var user = new User
        {
            Username = "linked-user",
            PasswordHash = "hash",
            EmployeeId = employeeId
        };
        var role = new Role { RoleName = "inactive-role", IsActive = false };
        db.AddRange(user, role);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();

        var service = new SystemSettingsService(db);
        var users = await service.GetUsersAsync();
        var roles = await service.GetRolesAsync();

        Assert.Equal(employeeId, Assert.Single(users).EmployeeId);
        Assert.False(Assert.Single(Assert.Single(users).Roles).IsActive);
        Assert.False(Assert.Single(roles).IsActive);
    }

    [Fact]
    public async Task GetLinkedEmployeeIdsAsync_IncludesInactiveAccountBindings()
    {
        using var db = CreateInMemoryDbContext();
        var employeeId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Username = "inactive-linked-user",
            PasswordHash = "hash",
            EmployeeId = employeeId,
            IsActive = false
        });
        await db.SaveChangesAsync();

        var linkedEmployeeIds = await new SystemSettingsService(db).GetLinkedEmployeeIdsAsync();

        Assert.Equal(employeeId, Assert.Single(linkedEmployeeIds));
    }
}
