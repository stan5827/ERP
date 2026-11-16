using ErpBackend.Data;
using ErpBackend.Models.HR;
using ErpBackend.Models.System;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ErpBackend.IntegrationTests;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("ERP_TEST_POSTGRES_CONNECTION_STRING")))
            Skip = "Set ERP_TEST_POSTGRES_CONNECTION_STRING to run PostgreSQL integration tests.";
    }
}

public class PostgreSqlLoginLockoutIntegrationTests
{
    [PostgreSqlFact]
    public async Task ConcurrentFailedLogins_AreSerializedByPostgreSqlUserRowLock()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ERP_TEST_POSTGRES_CONNECTION_STRING")!;
        await using var database = await PostgreSqlTestDatabase.CreateAsync(baseConnectionString);
        var options = database.Options;
        await using (var setupDb = new ErpGeneralDbContext(options))
        {
            var employee = new Employee
            {
                EmployeeNo = "LOGIN-LOCKOUT-TEST",
                ChineseName = "登入鎖定測試",
                Status = "active",
                BaseSalary = 0,
                FoodAllowance = 0,
                PositionAllowance = 0,
                FullAttendanceBonus = 0,
                LaborInsuranceGrade = 0,
                HealthInsuranceGrade = 0,
                PensionContributionRate = 0,
                TaxDependents = 0,
                OvertimePermission = false
            };
            setupDb.Employees.Add(employee);
            setupDb.Users.Add(new User
            {
                Username = "parallel-login",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123"),
                IsActive = true,
                EmployeeId = employee.Id
            });
            await setupDb.SaveChangesAsync();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "postgresql-integration-test-secret-key-12345",
                ["Jwt:Issuer"] = "erp-tests",
                ["Jwt:Audience"] = "erp-tests",
                ["Jwt:ExpireMinutes"] = "30",
                ["Auth:Lockout:MaxFailedAttempts"] = "5",
                ["Auth:Lockout:DurationMinutes"] = "15"
            })
            .Build();
        var attempts = Enumerable.Range(0, 5).Select(async _ =>
        {
            await using var db = new ErpGeneralDbContext(options);
            var authService = new AuthService(db, configuration);
            return await authService.LoginAsync(new DTOs.LoginRequest
            {
                Username = "parallel-login",
                Password = "IncorrectPassword123"
            });
        });

        var failedResults = await Task.WhenAll(attempts);
        Assert.All(failedResults, result => Assert.Null(result));

        await using var verificationDb = new ErpGeneralDbContext(options);
        var verificationAuthService = new AuthService(verificationDb, configuration);
        var lockedResult = await verificationAuthService.LoginAsync(new DTOs.LoginRequest
        {
            Username = "parallel-login",
            Password = "CorrectPassword123"
        });

        Assert.Null(lockedResult);
        Assert.Equal(1, await verificationDb.AuditLogs.CountAsync(log =>
            log.Action == "LOGIN_LOCKOUT_STARTED"));
    }
}
