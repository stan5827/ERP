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
}
