using ErpBackend.Data;
using ErpBackend.Models.System;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace ErpBackend.Tests.Services;

public class AuthServiceTests
{
    private static ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ErpGeneralDbContext(options);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "local-test-key-with-at-least-32-characters",
                ["Jwt:Issuer"] = "erp-tests",
                ["Jwt:Audience"] = "erp-tests"
            })
            .Build();

    [Fact]
    public async Task LoginAsync_AllowsActiveAdminWithoutEmployeeLink()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User
        {
            Username = "admin-without-employee",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
        };
        var adminRole = new Role { RoleName = "Admin", IsActive = true };
        db.AddRange(user, adminRole);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();

        var service = new AuthService(db, CreateConfiguration());
        var result = await service.LoginAsync(new()
        {
            Username = user.Username,
            Password = "Password123"
        });

        Assert.NotNull(result);
        Assert.Null(result!.EmployeeId);
        Assert.Contains("Admin", result.Roles);
        Assert.DoesNotContain(new JwtSecurityTokenHandler().ReadJwtToken(result.Token).Claims,
            claim => claim.Type == "employee_id");
    }

    [Fact]
    public async Task LoginAsync_StillRequiresEmployeeLinkForNonAdminAccounts()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User
        {
            Username = "employee-without-link",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
        };
        var employeeRole = new Role { RoleName = "Employee", IsActive = true };
        db.AddRange(user, employeeRole);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = employeeRole.Id });
        await db.SaveChangesAsync();

        var service = new AuthService(db, CreateConfiguration());
        var result = await service.LoginAsync(new()
        {
            Username = user.Username,
            Password = "Password123"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task RecoverAdminPasswordAsync_ResetsOnlyAdminAndRequiresPasswordChange()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User
        {
            Username = "locked-admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123"),
            SecurityVersion = 4
        };
        var adminRole = new Role { RoleName = "Admin", IsActive = true };
        db.AddRange(user, adminRole);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();

        var service = new AuthService(db, CreateConfiguration());
        var result = await service.RecoverAdminPasswordAsync(
            user.Username,
            "A-New-Recovery-Password-123",
            "incident-123");

        Assert.True(result);
        Assert.True(BCrypt.Net.BCrypt.Verify("A-New-Recovery-Password-123", user.PasswordHash));
        Assert.True(user.MustChangePassword);
        Assert.Equal(5, user.SecurityVersion);
        Assert.Contains(await db.PasswordHistories.ToListAsync(), history => history.UserId == user.Id);
        Assert.Contains(await db.AuditLogs.ToListAsync(), log =>
            log.Action == "USER_PASSWORD_RECOVERY"
            && log.EntityId == user.Id
            && log.UserId == null
            && log.Details!.Contains("incident-123"));
        Assert.Contains(await db.AuditLogs.ToListAsync(), log =>
            log.Action == "LOGIN_LOCKOUT_RELEASED" && log.EntityId == user.Id);

        var login = await service.LoginAsync(new()
        {
            Username = user.Username,
            Password = "A-New-Recovery-Password-123"
        });
        Assert.NotNull(login);
        Assert.True(login!.MustChangePassword);
    }

    [Fact]
    public async Task RecoverAdminPasswordAsync_RejectsInactiveAndNonAdminAccounts()
    {
        using var db = CreateInMemoryDbContext();
        var nonAdmin = new User
        {
            Username = "operator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
        };
        var inactiveAdmin = new User
        {
            Username = "inactive-admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123"),
            IsActive = false
        };
        var adminRole = new Role { RoleName = "Admin", IsActive = true };
        var operatorRole = new Role { RoleName = "Operator", IsActive = true };
        db.AddRange(nonAdmin, inactiveAdmin, adminRole, operatorRole);
        db.UserRoles.AddRange(
            new UserRole { UserId = nonAdmin.Id, RoleId = operatorRole.Id },
            new UserRole { UserId = inactiveAdmin.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();

        var service = new AuthService(db, CreateConfiguration());

        Assert.False(await service.RecoverAdminPasswordAsync(
            nonAdmin.Username,
            "A-New-Recovery-Password-123",
            "incident-123"));
        Assert.False(await service.RecoverAdminPasswordAsync(
            inactiveAdmin.Username,
            "A-New-Recovery-Password-123",
            "incident-123"));
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task RecoverAdminPasswordAsync_RejectsWeakPassword()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
        };
        var adminRole = new Role { RoleName = "Admin", IsActive = true };
        db.AddRange(user, adminRole);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();

        var service = new AuthService(db, CreateConfiguration());

        Assert.False(await service.RecoverAdminPasswordAsync(
            user.Username,
            "Weak123",
            "incident-123"));
        Assert.False(user.MustChangePassword);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }
}
