using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using ErpBackend.Models.System;
using BCryptNet = BCrypt.Net.BCrypt;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace ErpBackend.IntegrationTests;

public class PostgreSqlJwtLatencyIntegrationTests
{
    private const string TestJwtSecret = "postgresql-latency-test-secret-key-1234567890";
    private readonly ITestOutputHelper _output;

    public PostgreSqlJwtLatencyIntegrationTests(ITestOutputHelper output) => _output = output;

    [PostgreSqlFact]
    public async Task MeasuresSuccessfulLoginAndProtectedApiLatency()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable("ERP_TEST_POSTGRES_CONNECTION_STRING")!;
        await using var database = await PostgreSqlTestDatabase.CreateAsync(baseConnectionString);
        await SeedAdminAsync(database.Options);

        var previousConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        var previousJwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        try
        {
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", database.ConnectionString);
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", TestJwtSecret);

            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
            using var client = factory.CreateClient();

            var loginSamples = new List<double>();
            string? token = null;
            for (var sample = 0; sample < 15; sample++)
            {
                var stopwatch = Stopwatch.StartNew();
                using var response = await client.PostAsJsonAsync("/api/erp/auth/login", new LoginRequest
                {
                    Username = "jwt-latency-test",
                    Password = "LatencyPassword123"
                });
                stopwatch.Stop();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
                Assert.True(result?.Success);
                token = result?.Data?.Token;
                Assert.False(string.IsNullOrWhiteSpace(token));
                if (sample >= 3)
                    loginSamples.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            for (var warmup = 0; warmup < 5; warmup++)
            {
                using var response = await client.GetAsync("/api/erp/users");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            var apiSamples = new List<double>();
            for (var sample = 0; sample < 40; sample++)
            {
                var stopwatch = Stopwatch.StartNew();
                using var response = await client.GetAsync("/api/erp/users");
                stopwatch.Stop();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                apiSamples.Add(stopwatch.Elapsed.TotalMilliseconds);
            }

            _output.WriteLine(
                $"Local isolated PostgreSQL (n={loginSamples.Count} login, n={apiSamples.Count} protected API): " +
                $"login p50={Percentile(loginSamples, 0.50):F2} ms, p95={Percentile(loginSamples, 0.95):F2} ms; " +
                $"GET /api/erp/users p50={Percentile(apiSamples, 0.50):F2} ms, " +
                $"p95={Percentile(apiSamples, 0.95):F2} ms.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", previousConnectionString);
            Environment.SetEnvironmentVariable("JWT_SECRET_KEY", previousJwtSecret);
        }
    }

    private static async Task SeedAdminAsync(DbContextOptions<ErpGeneralDbContext> options)
    {
        await using var db = new ErpGeneralDbContext(options);
        var role = new Role { RoleName = "Admin", IsSystem = true };
        var employee = new Employee
        {
            EmployeeNo = "JWT-LATENCY-TEST",
            ChineseName = "JWT 延遲測試",
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
        var user = new User
        {
            Username = "jwt-latency-test",
            PasswordHash = BCryptNet.HashPassword("LatencyPassword123"),
            IsActive = true,
            EmployeeId = employee.Id
        };
        db.Roles.Add(role);
        db.Employees.Add(employee);
        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
    }

    private static double Percentile(List<double> samples, double percentile)
    {
        var orderedSamples = samples.OrderBy(sample => sample).ToArray();
        var index = Math.Clamp((int)Math.Ceiling(percentile * orderedSamples.Length) - 1, 0, orderedSamples.Length - 1);
        return orderedSamples[index];
    }
}
