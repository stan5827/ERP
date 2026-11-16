using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ErpBackend.DTOs;
using ErpBackend.Models.System;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.IntegrationTests;

public class AuthControllerIntegrationTests : IDisposable
{
    private static readonly string AdminResetSecret =
        Environment.GetEnvironmentVariable("ADMIN_RESET_SECRET") ?? "integration-test-reset-secret";
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_RejectsAnonymousAccountCreation()
    {
        var username = $"registration-{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/erp/auth/register", new CreateUserRequest
        {
            Username = username,
            Password = "Password123",
            RoleIds = [Guid.NewGuid()]
        });

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        using var db = _factory.CreateDbContext();
        Assert.False(await db.Users.AnyAsync(user => user.Username == username));
    }

    [Fact]
    public async Task AdminReset_RequiresAdminRoleAndConfiguredSecret()
    {
        var username = $"reset-{Guid.NewGuid():N}";
        var targetUserId = await AddUserAsync(username, "OldPassword123");
        var adminUserId = await AddUserAsync($"reset-admin-{Guid.NewGuid():N}", "UnusedPassword123");

        var request = new AdminResetRequest
        {
            Secret = AdminResetSecret,
            Username = username,
            NewPassword = "NewPassword456"
        };

        using var anonymousRequest = new HttpRequestMessage(HttpMethod.Post, "/api/erp/auth/admin-reset")
        {
            Content = JsonContent.Create(request)
        };
        anonymousRequest.Headers.Add("X-Test-Anonymous", "true");
        var anonymousResponse = await _client.SendAsync(anonymousRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var adminRequest = new HttpRequestMessage(HttpMethod.Post, "/api/erp/auth/admin-reset");
        adminRequest.Headers.Add("X-Test-UserId", adminUserId.ToString());
        adminRequest.Headers.Add("X-Test-Roles", "Admin");
        adminRequest.Content = JsonContent.Create(request);
        var adminResponse = await _client.SendAsync(adminRequest);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/erp/auth/login", new LoginRequest
        {
            Username = username,
            Password = "NewPassword456"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.True(login?.Success);
        Assert.True(login?.Data?.MustChangePassword);

        using var db = _factory.CreateDbContext();
        var resetAudit = await db.AuditLogs.SingleAsync(entry =>
            entry.EntityId == targetUserId && entry.Action == "USER_PASSWORD_RESET");
        Assert.Equal(adminUserId, resetAudit.UserId);
        var previousPassword = await db.PasswordHistories.SingleAsync(history => history.UserId == targetUserId);
        Assert.True(BCrypt.Net.BCrypt.Verify("OldPassword123", previousPassword.PasswordHash));
        Assert.Equal(1, await db.Users.Where(user => user.Id == targetUserId)
            .Select(user => user.SecurityVersion)
            .SingleAsync());
    }

    [Fact]
    public async Task ProtectedRequest_RejectsJwtAfterAccountSecurityVersionChanges()
    {
        var userId = await AddUserAsync($"jwt-version-{Guid.NewGuid():N}", "CorrectPassword123");
        await AssignAdminRoleAsync(userId);
        using var tokenClient = await LoginClientAsync(userId, "CorrectPassword123");

        var firstResponse = await tokenClient.GetAsync("/api/erp/users");
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using (var db = _factory.CreateDbContext())
        {
            var user = await db.Users.SingleAsync(account => account.Id == userId);
            user.SecurityVersion++;
            await db.SaveChangesAsync();
        }

        var revokedResponse = await tokenClient.GetAsync("/api/erp/users");
        Assert.Equal(HttpStatusCode.Unauthorized, revokedResponse.StatusCode);
    }

    [Fact]
    public async Task ProtectedRequest_RejectsJwtWhenCurrentRoleBindingsDiffer()
    {
        var userId = await AddUserAsync($"jwt-role-change-{Guid.NewGuid():N}", "CorrectPassword123");
        await AssignRoleAsync(userId, "Admin");
        using var tokenClient = await LoginClientAsync(userId, "CorrectPassword123");

        using (var db = _factory.CreateDbContext())
        {
            var existingRoles = await db.UserRoles.Where(role => role.UserId == userId).ToListAsync();
            db.UserRoles.RemoveRange(existingRoles);
            var hrRole = await db.Roles.SingleOrDefaultAsync(role => role.RoleName == "HR Manager");
            if (hrRole == null)
            {
                hrRole = new Role { RoleName = "HR Manager", IsSystem = true };
                db.Roles.Add(hrRole);
            }

            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = hrRole.Id });
            await db.SaveChangesAsync();
        }

        var response = await tokenClient.GetAsync("/api/erp/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HrManager_CanAccessHrEndpointsButNotAdminOrUnassignedModules()
    {
        var userId = await AddUserAsync($"hr-manager-{Guid.NewGuid():N}", "CorrectPassword123");
        await AssignRoleAsync(userId, "HR Manager");
        await AssignPermissionsAsync("HR Manager", "hr:page");
        using var tokenClient = await LoginClientAsync(userId, "CorrectPassword123");

        Assert.Equal(HttpStatusCode.OK,
            (await tokenClient.GetAsync("/api/erp/employees")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tokenClient.GetAsync("/api/erp/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tokenClient.GetAsync("/api/erp/accounts-payable")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tokenClient.GetAsync("/api/erp/roles/ping")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tokenClient.GetAsync("/api/erp/assets")).StatusCode);
    }

    [Fact]
    public async Task ProductionSupervisor_CanReviewProductionButNotFinance()
    {
        var userId = await AddUserAsync($"production-supervisor-{Guid.NewGuid():N}", "CorrectPassword123");
        await AssignRoleAsync(userId, "Production Supervisor");
        await AssignPermissionsAsync("Production Supervisor", "manufacturing:page", "manufacturing:approve");
        using var tokenClient = await LoginClientAsync(userId, "CorrectPassword123");

        Assert.Equal(HttpStatusCode.OK,
            (await tokenClient.GetAsync("/api/erp/wipreports/review-queue")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await tokenClient.GetAsync("/api/erp/work-orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tokenClient.GetAsync("/api/erp/finance/coa")).StatusCode);
    }

    [Fact]
    public async Task Operator_CanAccessOwnEssAndReportsButNotReviewQueue()
    {
        var userId = await AddUserAsync($"operator-{Guid.NewGuid():N}", "CorrectPassword123");
        await AssignRoleAsync(userId, "Operator");
        await AssignPermissionsAsync("Operator", "manufacturing:page", "manufacturing:execute");
        using var tokenClient = await LoginClientAsync(userId, "CorrectPassword123");

        Assert.Equal(HttpStatusCode.OK,
            (await tokenClient.GetAsync("/api/erp/ess/payroll")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await tokenClient.GetAsync("/api/erp/wipreports/my")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tokenClient.GetAsync("/api/erp/wipreports/review-queue")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tokenClient.GetAsync("/api/erp/users")).StatusCode);
    }

    [Fact]
    public async Task Finance_CanAccessFinancialWorkflowsButNotHr()
    {
        var userId = await AddUserAsync($"finance-{Guid.NewGuid():N}", "CorrectPassword123");
        await AssignRoleAsync(userId, "Finance");
        await AssignPermissionsAsync("Finance", "finance:page");
        using var tokenClient = await LoginClientAsync(userId, "CorrectPassword123");

        Assert.Equal(HttpStatusCode.OK,
            (await tokenClient.GetAsync("/api/erp/accounts-payable")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await tokenClient.GetAsync("/api/erp/finance/coa")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await tokenClient.GetAsync("/api/erp/employees")).StatusCode);
    }

    [Fact]
    public async Task InactiveLegacyRole_DoesNotGrantAccessOrAppearInAssignableRoleList()
    {
        var legacyUserId = await AddUserAsync($"legacy-role-user-{Guid.NewGuid():N}", "CorrectPassword123");
        using (var db = _factory.CreateDbContext())
        {
            var legacyRole = new Role
            {
                RoleName = $"Legacy-{Guid.NewGuid():N}",
                IsSystem = true,
                IsActive = false
            };
            db.Roles.Add(legacyRole);
            db.UserRoles.Add(new UserRole { UserId = legacyUserId, RoleId = legacyRole.Id });
            await db.SaveChangesAsync();
        }

        using var legacyTokenClient = await LoginClientAsync(legacyUserId, "CorrectPassword123");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await legacyTokenClient.GetAsync("/api/erp/users")).StatusCode);

        var adminUserId = await AddUserAsync($"role-list-admin-{Guid.NewGuid():N}", "CorrectPassword123");
        await AssignRoleAsync(adminUserId, "Admin");
        using var adminTokenClient = await LoginClientAsync(adminUserId, "CorrectPassword123");
        var rolesResponse = await adminTokenClient.GetAsync("/api/erp/roles");
        Assert.Equal(HttpStatusCode.OK, rolesResponse.StatusCode);
        using var rolesJson = JsonDocument.Parse(await rolesResponse.Content.ReadAsStringAsync());
        var roleNames = rolesJson.RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(role => role.GetProperty("roleName").GetString())
            .ToArray();
        Assert.Contains("Admin", roleNames);
        Assert.DoesNotContain(roleNames, roleName => roleName!.StartsWith("Legacy-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AdminRoleUpdate_RevokesOldTokenAndRecordsReason()
    {
        var targetUserId = await AddUserAsync($"role-target-{Guid.NewGuid():N}", "CorrectPassword123");
        await AssignRoleAsync(targetUserId, "Admin");
        var adminUserId = await AddUserAsync($"role-admin-{Guid.NewGuid():N}", "UnusedPassword123");
        await AssignRoleAsync(adminUserId, "Admin");
        using var oldTokenClient = await LoginClientAsync(targetUserId, "CorrectPassword123");

        Guid hrRoleId;
        using (var db = _factory.CreateDbContext())
        {
            var hrRole = await db.Roles.SingleOrDefaultAsync(role => role.RoleName == "HR Manager");
            if (hrRole == null)
            {
                hrRole = new Role { RoleName = "HR Manager", IsSystem = true };
                db.Roles.Add(hrRole);
                await db.SaveChangesAsync();
            }

            hrRoleId = hrRole.Id;
        }

        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/erp/users/{targetUserId}/roles")
        {
            Content = JsonContent.Create(new UpdateUserRolesRequest
            {
                RoleIds = [hrRoleId],
                Reason = "Role changed after approved department transfer."
            })
        };
        request.Headers.Add("X-Test-UserId", adminUserId.ToString());
        request.Headers.Add("X-Test-Roles", "Admin");
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(request)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await oldTokenClient.GetAsync("/api/erp/users")).StatusCode);

        using var verificationDb = _factory.CreateDbContext();
        var updatedUser = await verificationDb.Users.SingleAsync(user => user.Id == targetUserId);
        Assert.Equal(1, updatedUser.SecurityVersion);
        var audit = await verificationDb.AuditLogs.SingleAsync(entry =>
            entry.EntityId == targetUserId && entry.Action == "USER_ROLES_UPDATED");
        Assert.Equal(adminUserId, audit.UserId);
        Assert.Contains("approved department transfer", audit.Details);
    }


    [Fact]
    public async Task ProtectedRequest_RejectsJwtAfterAccountIsDeactivated()
    {
        var userId = await AddUserAsync($"jwt-disabled-{Guid.NewGuid():N}", "CorrectPassword123");
        await AssignAdminRoleAsync(userId);
        using var tokenClient = await LoginClientAsync(userId, "CorrectPassword123");
        var adminId = await AddUserAsync($"jwt-admin-{Guid.NewGuid():N}", "UnusedPassword123");

        var firstResponse = await tokenClient.GetAsync("/api/erp/users");
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using var deactivateRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/erp/users/{userId}");
        deactivateRequest.Headers.Add("X-Test-UserId", adminId.ToString());
        deactivateRequest.Headers.Add("X-Test-Roles", "Admin");
        var deactivateResponse = await _client.SendAsync(deactivateRequest);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var revokedResponse = await tokenClient.GetAsync("/api/erp/users");
        Assert.Equal(HttpStatusCode.Unauthorized, revokedResponse.StatusCode);
    }

    [Fact]
    public async Task ProtectedRequest_FailsClosedWhenAccountStateCannotBeRead()
    {
        var userId = await AddUserAsync($"jwt-db-error-{Guid.NewGuid():N}", "CorrectPassword123");
        await AssignAdminRoleAsync(userId);
        using var tokenClient = await LoginClientAsync(userId, "CorrectPassword123");

        using (var db = _factory.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"Users\"");

        using var response = await tokenClient.GetAsync("/api/erp/users");
        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task LeaveEmployee_IsLimitedToOwnReadOnlyEssEndpoints()
    {
        var userId = await AddUserAsync($"leave-employee-{Guid.NewGuid():N}", "CorrectPassword123");
        await AssignAdminRoleAsync(userId);
        using var oldTokenClient = await LoginClientAsync(userId, "CorrectPassword123");
        Guid employeeId;

        using (var db = _factory.CreateDbContext())
        {
            var user = await db.Users.SingleAsync(account => account.Id == userId);
            var employee = await db.Employees.SingleAsync(item => item.Id == user.EmployeeId);
            employeeId = employee.Id;
            employee.Status = "leave";
            user.SecurityVersion++;
            db.Payrolls.Add(new ErpBackend.Models.HR.Payroll
            {
                EmployeeId = employee.Id,
                PeriodStart = new DateTime(2026, 10, 1),
                PeriodEnd = new DateTime(2026, 10, 31),
                BasicSalary = 40000,
                GrossPay = 42000,
                TotalDeductions = 2000,
                NetPay = 40000,
                Status = "paid",
                PaymentBankAccount = "secret-account"
            });
            var otherEmployee = new ErpBackend.Models.HR.Employee
            {
                EmployeeNo = $"T{Guid.NewGuid():N}"[..20],
                ChineseName = "Another Employee",
                Status = "active"
            };
            db.Employees.Add(otherEmployee);
            db.Payrolls.Add(new ErpBackend.Models.HR.Payroll
            {
                EmployeeId = otherEmployee.Id,
                PeriodStart = new DateTime(2026, 10, 1),
                PeriodEnd = new DateTime(2026, 10, 31),
                BasicSalary = 90000,
                GrossPay = 90000,
                NetPay = 90000,
                Status = "paid"
            });
            db.AttendanceRecords.AddRange(
                new ErpBackend.Models.HR.AttendanceRecord
                {
                    EmployeeId = employee.Id,
                    Date = new DateTime(2026, 10, 20),
                    Status = "present"
                },
                new ErpBackend.Models.HR.AttendanceRecord
                {
                    EmployeeId = otherEmployee.Id,
                    Date = new DateTime(2026, 10, 20),
                    Status = "present"
                });
            db.LeaveRequests.AddRange(
                new ErpBackend.Models.HR.LeaveRequest
                {
                    EmployeeId = employee.Id,
                    ApplicationNo = $"LR-{Guid.NewGuid():N}"[..30],
                    LeaveType = "annual",
                    ApplicationDate = DateTime.UtcNow,
                    StartDate = new DateTime(2026, 11, 1),
                    EndDate = new DateTime(2026, 11, 1),
                    Status = "pending"
                },
                new ErpBackend.Models.HR.LeaveRequest
                {
                    EmployeeId = otherEmployee.Id,
                    ApplicationNo = $"LR-{Guid.NewGuid():N}"[..30],
                    LeaveType = "annual",
                    ApplicationDate = DateTime.UtcNow,
                    StartDate = new DateTime(2026, 11, 1),
                    EndDate = new DateTime(2026, 11, 1),
                    Status = "pending"
                });
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await oldTokenClient.GetAsync("/api/erp/ess/payroll")).StatusCode);

        using var leaveTokenClient = await LoginClientAsync(userId, "CorrectPassword123");
        var payrollResponse = await leaveTokenClient.GetAsync("/api/erp/ess/payroll");
        Assert.Equal(HttpStatusCode.OK, payrollResponse.StatusCode);
        using (var payrollJson = JsonDocument.Parse(await payrollResponse.Content.ReadAsStringAsync()))
        {
            var payrollItems = payrollJson.RootElement.GetProperty("data");
            Assert.Single(payrollItems.EnumerateArray());
            Assert.Equal(40000, payrollItems[0].GetProperty("netPay").GetDecimal());
            Assert.False(payrollItems[0].TryGetProperty("paymentBankAccount", out _));
        }
        var attendanceResponse = await leaveTokenClient.GetAsync("/api/erp/ess/attendance");
        Assert.Equal(HttpStatusCode.OK, attendanceResponse.StatusCode);
        using (var attendanceJson = JsonDocument.Parse(await attendanceResponse.Content.ReadAsStringAsync()))
        {
            var attendanceItems = attendanceJson.RootElement.GetProperty("data");
            Assert.Single(attendanceItems.EnumerateArray());
            Assert.Equal(employeeId.ToString(), attendanceItems[0].GetProperty("employeeId").GetString());
        }

        var leaveResponse = await leaveTokenClient.GetAsync("/api/erp/ess/leave-requests");
        Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);
        using (var leaveJson = JsonDocument.Parse(await leaveResponse.Content.ReadAsStringAsync()))
        {
            var leaveItems = leaveJson.RootElement.GetProperty("data");
            Assert.Single(leaveItems.EnumerateArray());
            Assert.Equal(employeeId.ToString(), leaveItems[0].GetProperty("employeeId").GetString());
        }
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await leaveTokenClient.GetAsync("/api/erp/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await leaveTokenClient.PostAsJsonAsync("/api/erp/ess/payroll", new { })).StatusCode);
    }

    [Fact]
    public async Task TerminationImmediatelyDeactivatesAccountAndRevokesJwt()
    {
        var userId = await AddUserAsync($"terminated-employee-{Guid.NewGuid():N}", "CorrectPassword123");
        using var tokenClient = await LoginClientAsync(userId, "CorrectPassword123");
        Guid employeeId;
        using (var db = _factory.CreateDbContext())
            employeeId = (await db.Users.SingleAsync(account => account.Id == userId)).EmployeeId!.Value;

        var terminationResponse = await _client.PatchAsJsonAsync(
            $"/api/erp/employees/{employeeId}/terminate",
            new { Reason = "離職完成", EffectiveDate = DateTime.UtcNow.Date });
        Assert.Equal(HttpStatusCode.OK, terminationResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await tokenClient.GetAsync("/api/erp/ess/payroll")).StatusCode);

        using var verifyDb = _factory.CreateDbContext();
        var account = await verifyDb.Users.SingleAsync(user => user.Id == userId);
        var employee = await verifyDb.Employees.SingleAsync(item => item.Id == employeeId);
        Assert.False(account.IsActive);
        Assert.Equal(1, account.SecurityVersion);
        Assert.Equal("terminated", employee.Status);
    }

    [Fact]
    public async Task NewlyProvisionedAccountMustChangePasswordBeforeUsingBusinessApis()
    {
        var userId = await AddUserAsync($"initial-password-{Guid.NewGuid():N}", "InitialPassword123");
        await AssignAdminRoleAsync(userId);
        using (var db = _factory.CreateDbContext())
        {
            var account = await db.Users.SingleAsync(user => user.Id == userId);
            account.MustChangePassword = true;
            await db.SaveChangesAsync();
        }

        using var initialTokenClient = await LoginClientAsync(userId, "InitialPassword123");
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await initialTokenClient.GetAsync("/api/erp/users")).StatusCode);

        var changeResponse = await initialTokenClient.PostAsJsonAsync(
            "/api/erp/auth/change-password",
            new { OldPassword = "InitialPassword123", NewPassword = "ChangedPassword456" });
        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await initialTokenClient.GetAsync("/api/erp/users")).StatusCode);

        using var updatedTokenClient = await LoginClientAsync(userId, "ChangedPassword456");
        Assert.Equal(HttpStatusCode.OK,
            (await updatedTokenClient.GetAsync("/api/erp/users")).StatusCode);
    }

    [Fact]
    public async Task Login_LocksAccountAfterFiveFailuresAndAdminCanUnlock()
    {
        var userId = await AddUserAsync($"locked-{Guid.NewGuid():N}", "CorrectPassword123");
        var adminId = await AddUserAsync($"unlock-admin-{Guid.NewGuid():N}", "UnusedPassword123");
        var username = await GetUsernameAsync(userId);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var failedLogin = await _client.PostAsJsonAsync("/api/erp/auth/login", new LoginRequest
            {
                Username = username,
                Password = "WrongPassword123"
            });
            Assert.Equal(HttpStatusCode.Unauthorized, failedLogin.StatusCode);
        }

        var lockedLogin = await _client.PostAsJsonAsync("/api/erp/auth/login", new LoginRequest
        {
            Username = username,
            Password = "CorrectPassword123"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, lockedLogin.StatusCode);

        using var unlockRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/erp/users/{userId}/unlock");
        unlockRequest.Headers.Add("X-Test-UserId", adminId.ToString());
        unlockRequest.Headers.Add("X-Test-Roles", "Admin");
        var unlockResponse = await _client.SendAsync(unlockRequest);
        Assert.Equal(HttpStatusCode.OK, unlockResponse.StatusCode);

        var successfulLogin = await _client.PostAsJsonAsync("/api/erp/auth/login", new LoginRequest
        {
            Username = username,
            Password = "CorrectPassword123"
        });
        Assert.Equal(HttpStatusCode.OK, successfulLogin.StatusCode);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var failedLogin = await _client.PostAsJsonAsync("/api/erp/auth/login", new LoginRequest
            {
                Username = username,
                Password = "WrongPassword123"
            });
            Assert.Equal(HttpStatusCode.Unauthorized, failedLogin.StatusCode);
        }

        var loginAfterCounterReset = await _client.PostAsJsonAsync("/api/erp/auth/login", new LoginRequest
        {
            Username = username,
            Password = "CorrectPassword123"
        });
        Assert.Equal(HttpStatusCode.OK, loginAfterCounterReset.StatusCode);

        using var db = _factory.CreateDbContext();
        var unlockAudit = await db.AuditLogs.SingleAsync(log =>
            log.EntityId == userId && log.Action == "LOGIN_LOCKOUT_RELEASED");
        Assert.Equal(adminId, unlockAudit.UserId);
    }

    [Fact]
    public async Task Login_LockoutExpiresAfterFifteenMinutes()
    {
        var userId = await AddUserAsync($"expired-lock-{Guid.NewGuid():N}", "CorrectPassword123");
        var expiredAt = DateTime.UtcNow.AddMinutes(-16);

        using (var db = _factory.CreateDbContext())
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                db.AuditLogs.Add(new AuditLog
                {
                    Action = "LOGIN_FAILED",
                    EntityType = nameof(User),
                    EntityId = userId,
                    UserId = userId,
                    Details = "Invalid credentials",
                    CreatedAt = expiredAt.AddSeconds(attempt)
                });
            }
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/erp/auth/login", new LoginRequest
        {
            Username = await GetUsernameAsync(userId),
            Password = "CorrectPassword123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_DoesNotHonorClientSuppliedSkipOldPassword()
    {
        var userId = await AddUserAsync($"change-password-{Guid.NewGuid():N}", "OldPassword123");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/erp/auth/change-password");
        request.Headers.Add("X-Test-UserId", userId.ToString());
        request.Content = JsonContent.Create(new
        {
            OldPassword = "IncorrectOldPassword",
            NewPassword = "NewPassword456",
            SkipOldPassword = true
        });

        var response = await _client.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(result?.Success);
        var oldPasswordResponse = await _client.PostAsJsonAsync("/api/erp/auth/login", new LoginRequest
        {
            Username = await GetUsernameAsync(userId),
            Password = "OldPassword123"
        });
        Assert.Equal(HttpStatusCode.OK, oldPasswordResponse.StatusCode);
        var login = await oldPasswordResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.True(login?.Success);
    }

    private async Task<Guid> AddUserAsync(string username, string password)
    {
        using var db = _factory.CreateDbContext();
        var employee = new ErpBackend.Models.HR.Employee
        {
            EmployeeNo = $"T{Guid.NewGuid():N}"[..20],
            ChineseName = username,
            Status = "active"
        };
        db.Employees.Add(employee);
        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true,
            EmployeeId = employee.Id
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<string> GetUsernameAsync(Guid userId)
    {
        using var db = _factory.CreateDbContext();
        return (await db.Users.SingleAsync(user => user.Id == userId)).Username;
    }

    private async Task<HttpClient> LoginClientAsync(Guid userId, string password)
    {
        var username = await GetUsernameAsync(userId);
        var response = await _client.PostAsJsonAsync("/api/erp/auth/login", new LoginRequest
        {
            Username = username,
            Password = password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.True(result?.Success);
        Assert.False(string.IsNullOrWhiteSpace(result?.Data?.Token));

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.Data!.Token);
        return client;
    }

    private async Task AssignAdminRoleAsync(Guid userId)
        => await AssignRoleAsync(userId, "Admin");

    private async Task AssignRoleAsync(Guid userId, string roleName)
    {
        using var db = _factory.CreateDbContext();
        var role = await db.Roles.SingleOrDefaultAsync(existing => existing.RoleName == roleName);
        if (role == null)
        {
            role = new Role { RoleName = roleName, IsSystem = true };
            db.Roles.Add(role);
        }

        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
        await db.SaveChangesAsync();
    }

    private async Task AssignPermissionsAsync(string roleName, params string[] permissionCodes)
    {
        using var db = _factory.CreateDbContext();
        var role = await db.Roles.SingleAsync(existing => existing.RoleName == roleName);
        var permissions = await db.Permissions
            .Where(permission => permissionCodes.Contains(permission.PermissionCode))
            .ToListAsync();
        if (permissions.Count != permissionCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            throw new InvalidOperationException($"One or more test permissions for {roleName} are missing.");

        var assignedIds = await db.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == role.Id)
            .Select(rolePermission => rolePermission.PermissionId)
            .ToHashSetAsync();
        db.RolePermissions.AddRange(permissions
            .Where(permission => !assignedIds.Contains(permission.Id))
            .Select(permission => new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            }));
        await db.SaveChangesAsync();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
