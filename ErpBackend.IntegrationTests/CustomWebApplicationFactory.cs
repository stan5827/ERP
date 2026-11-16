using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ErpBackend.Data;
using ErpBackend.Models.System;

namespace ErpBackend.IntegrationTests;

internal class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    internal static readonly Guid DefaultTestUserId =
        Guid.Parse("c78fa4b8-3b90-49b0-a6b8-20bf2d800001");

    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var initDb = new ErpGeneralDbContext(new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseSqlite(_connection)
            .Options);
        initDb.Database.EnsureCreated();
        var testAdminRole = new Role
        {
            Id = Guid.Parse("c78fa4b8-3b90-49b0-a6b8-20bf2d800002"),
            RoleName = "Admin",
            IsSystem = true
        };
        initDb.Roles.Add(testAdminRole);
        initDb.Users.Add(new User
        {
            Id = DefaultTestUserId,
            Username = "integration-test-admin",
            PasswordHash = "unused"
        });
        initDb.UserRoles.Add(new UserRole
        {
            UserId = DefaultTestUserId,
            RoleId = testAdminRole.Id
        });
        initDb.SaveChanges();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminReset:Secret"] = "integration-test-reset-secret"
            }));

        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "Host=localhost;Database=erp_test;Username=test;Password=test");
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "test-secret-key-at-least-32-chars-long!!");
        Environment.SetEnvironmentVariable("LLM_TIMEOUT_SECONDS", "5");

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestOrJwt";
                    options.DefaultChallengeScheme = "TestOrJwt";
                })
                .AddPolicyScheme("TestOrJwt", "Test or JWT", options =>
                {
                    options.ForwardDefaultSelector = context =>
                        context.Request.Headers.ContainsKey("Authorization")
                            ? JwtBearerDefaults.AuthenticationScheme
                            : "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });

            var descriptors = services
                .Where(d => d.ServiceType == typeof(ErpGeneralDbContext)
                         || d.ServiceType == typeof(DbContextOptions<ErpGeneralDbContext>)
                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<ErpGeneralDbContext>))
                .ToList();
            foreach (var d in descriptors)
                services.Remove(d);

            services.AddDbContext<ErpGeneralDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
        });
    }

    public ErpGeneralDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpGeneralDbContext>();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        return db;
    }
}
