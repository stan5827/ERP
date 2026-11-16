using System.Net;
using System.Net.Http.Json;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using ErpBackend.Models.System;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.IntegrationTests;

public class UserManagementAuthorizationIntegrationTests : IDisposable
{
    private static readonly string[] ManagementEndpoints =
    [
        "/api/erp/users",
        "/api/erp/system-settings/users",
        "/api/erp/systemsettings/users",
        "/api/erp/system-settings/roles",
        "/api/erp/system-settings/permissions/tree",
        "/api/erp/system-settings/role-modules/Admin"
    ];

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserManagementAuthorizationIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ManagementEndpoints_RequireAdminRole()
    {
        foreach (var endpoint in ManagementEndpoints)
        {
            using var anonymousRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
            anonymousRequest.Headers.Add("X-Test-Anonymous", "true");
            var anonymousResponse = await _client.SendAsync(anonymousRequest);
            Assert.True(anonymousResponse.StatusCode == HttpStatusCode.Unauthorized,
                $"{endpoint} returned {anonymousResponse.StatusCode} for an anonymous request.");

            using var userRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
            userRequest.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
            var userResponse = await _client.SendAsync(userRequest);
            Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);

            using var adminRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
            adminRequest.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
            adminRequest.Headers.Add("X-Test-Roles", "Admin");
            var adminResponse = await _client.SendAsync(adminRequest);
            Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        }
    }

    [Fact]
    public async Task CreateUser_AdminCanCreateAccountForActiveEmployee()
    {
        var department = new Department
        {
            DeptCode = $"D{Guid.NewGuid():N}"[..20],
            DeptName = "Integration Test Department"
        };
        var jobTitle = new JobTitle
        {
            TitleCode = $"J{Guid.NewGuid():N}"[..20],
            TitleName = "Integration Test Title"
        };
        var employee = new Employee
        {
            EmployeeNo = $"T{Guid.NewGuid():N}"[..20],
            ChineseName = "Integration Test Employee",
            Status = "active",
            NationalId = $"ID{Guid.NewGuid():N}"[..20],
            HireDate = DateTime.UtcNow.Date,
            DepartmentId = department.Id,
            JobTitleId = jobTitle.Id
        };
        var role = new Role { RoleName = $"Role-{Guid.NewGuid():N}" };
        var actor = new User
        {
            Username = $"admin-{Guid.NewGuid():N}",
            PasswordHash = "unused",
            IsActive = true
        };
        using (var db = _factory.CreateDbContext())
        {
            db.Departments.Add(department);
            db.JobTitles.Add(jobTitle);
            db.Employees.Add(employee);
            db.Roles.Add(role);
            db.Users.Add(actor);
            await db.SaveChangesAsync();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/erp/users")
        {
            Content = JsonContent.Create(new CreateUserRequest
            {
                Username = $"user-{Guid.NewGuid():N}",
                Password = "Password123",
                EmployeeId = employee.Id,
                RoleIds = [role.Id]
            })
        };
        request.Headers.Add("X-Test-UserId", actor.Id.ToString());
        request.Headers.Add("X-Test-Roles", "Admin");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        Assert.True(result?.Success);
        Assert.Equal(employee.Id, result?.Data?.EmployeeId);
        Assert.Contains(role.RoleName, result?.Data?.Roles ?? []);

        using var dbAfter = _factory.CreateDbContext();
        Assert.True(await dbAfter.Users.AnyAsync(user => user.EmployeeId == employee.Id));
        Assert.True(await dbAfter.Users.Where(user => user.EmployeeId == employee.Id)
            .Select(user => user.MustChangePassword).SingleAsync());
        var audit = await dbAfter.AuditLogs.SingleAsync(log => log.EntityId == result!.Data!.Id);
        Assert.Equal(actor.Id, audit.UserId);
    }

    [Fact]
    public async Task UnlockUser_RequiresAdminRoleAndRecordsActor()
    {
        var target = new User
        {
            Username = $"locked-user-{Guid.NewGuid():N}",
            PasswordHash = "unused"
        };
        var admin = new User
        {
            Username = $"unlock-admin-{Guid.NewGuid():N}",
            PasswordHash = "unused"
        };
        using (var db = _factory.CreateDbContext())
        {
            db.Users.AddRange(target, admin);
            await db.SaveChangesAsync();
        }

        var endpoint = $"/api/erp/users/{target.Id}/unlock";
        using var anonymousRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        anonymousRequest.Headers.Add("X-Test-Anonymous", "true");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(anonymousRequest)).StatusCode);

        using var userRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        userRequest.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
        var userResponse = await _client.SendAsync(userRequest);
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);

        using var adminRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        adminRequest.Headers.Add("X-Test-UserId", admin.Id.ToString());
        adminRequest.Headers.Add("X-Test-Roles", "Admin");
        var adminResponse = await _client.SendAsync(adminRequest);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        using var dbAfter = _factory.CreateDbContext();
        var audit = await dbAfter.AuditLogs.SingleAsync(log =>
            log.EntityId == target.Id && log.Action == "LOGIN_LOCKOUT_RELEASED");
        Assert.Equal(admin.Id, audit.UserId);
    }

    [Fact]
    public async Task ThemeEndpoints_AllowSelfAndForbidChangesToOtherUsers()
    {
        var user = new User
        {
            Username = $"theme-user-{Guid.NewGuid():N}",
            PasswordHash = "unused"
        };
        var otherUser = new User
        {
            Username = $"theme-other-{Guid.NewGuid():N}",
            PasswordHash = "unused"
        };
        using (var db = _factory.CreateDbContext())
        {
            db.Users.AddRange(user, otherUser);
            await db.SaveChangesAsync();
        }

        using var selfRead = new HttpRequestMessage(HttpMethod.Get, $"/api/erp/system-settings/users/{user.Id}/theme");
        selfRead.Headers.Add("X-Test-UserId", user.Id.ToString());
        selfRead.Headers.Add("X-Test-Roles", "Operator");
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(selfRead)).StatusCode);

        using var otherRead = new HttpRequestMessage(HttpMethod.Get, $"/api/erp/system-settings/users/{otherUser.Id}/theme");
        otherRead.Headers.Add("X-Test-UserId", user.Id.ToString());
        otherRead.Headers.Add("X-Test-Roles", "Operator");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(otherRead)).StatusCode);

        using var selfUpdate = new HttpRequestMessage(HttpMethod.Put, $"/api/erp/system-settings/users/{user.Id}/theme")
        {
            Content = JsonContent.Create(new UpdateThemeRequest { ThemeName = "modern-light" })
        };
        selfUpdate.Headers.Add("X-Test-UserId", user.Id.ToString());
        selfUpdate.Headers.Add("X-Test-Roles", "Operator");
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(selfUpdate)).StatusCode);

        using var otherUpdate = new HttpRequestMessage(HttpMethod.Put, $"/api/erp/system-settings/users/{otherUser.Id}/theme")
        {
            Content = JsonContent.Create(new UpdateThemeRequest { ThemeName = "modern-light" })
        };
        otherUpdate.Headers.Add("X-Test-UserId", user.Id.ToString());
        otherUpdate.Headers.Add("X-Test-Roles", "Operator");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(otherUpdate)).StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
