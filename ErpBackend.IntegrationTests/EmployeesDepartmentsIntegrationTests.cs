using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.HR;
using ErpBackend.Models.System;

namespace ErpBackend.IntegrationTests;

[CollectionDefinition("EmployeesDepartments", DisableParallelization = true)]
public sealed class EmployeesDepartmentsCollection { }

[Collection("EmployeesDepartments")]
public class EmployeesDepartmentsIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();

    public EmployeesDepartmentsIntegrationTests()
    {
        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.Employees.RemoveRange(db.Employees);
        db.Departments.RemoveRange(db.Departments);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var dept = new Department
        {
            DeptCode = "DEPT-HR",
            DeptName = "人資部",
            DeptType = "admin",
            IsActive = true,
            BudgetLimit = 500000
        };
        db.Departments.Add(dept);
        db.SaveChanges();

        var emp = new Employee
        {
            EmployeeNo = "EMP-001",
            ChineseName = "張三",
            DepartmentId = dept.Id,
            Status = "active",
            BaseSalary = 50000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Employees.Add(emp);
        db.SaveChanges();
    }

    #region Employees

    [Fact]
    public async Task GetAllEmployees_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/employees");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(1, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task GetAllEmployees_WithStatusFilter_ReturnsFiltered()
    {
        var response = await _client.GetAsync("/api/erp/employees?status=active");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("data").EnumerateArray().Count());
    }

    [Fact]
    public async Task GetAllEmployees_WithDepartmentFilter_ReturnsFiltered()
    {
        using var db = _factory.CreateDbContext();
        var deptId = db.Departments.First().Id;

        var response = await _client.GetAsync($"/api/erp/employees?departmentId={deptId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, root.GetProperty("data").EnumerateArray().Count());
    }

    [Fact]
    public async Task GetEmployee_ExistingId_ReturnsDetail()
    {
        using var db = _factory.CreateDbContext();
        var empId = db.Employees.First().Id;

        var response = await _client.GetAsync($"/api/erp/employees/{empId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("EMP-001", data.GetProperty("employeeNo").GetString());
        Assert.Equal("張三", data.GetProperty("chineseName").GetString());
    }

    [Fact]
    public async Task GetEmployee_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/erp/employees/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEmployee_ValidRequest_Creates()
    {
        using var db = _factory.CreateDbContext();
        var deptId = db.Departments.First().Id;

        var request = new
        {
            ChineseName = "李四",
            DepartmentId = deptId,
            Mobile = "0912345678",
            Email = "lisi@example.com",
            BaseSalary = 45000,
            Status = "active"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/employees", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Contains("Employee created", root.GetProperty("message").GetString()!);
    }

    [Fact]
    public async Task CreateEmployee_DuplicateEmployeeNo_ReturnsBadRequest()
    {
        using var db = _factory.CreateDbContext();
        var deptId = db.Departments.First().Id;

        var request = new
        {
            EmployeeNo = "EMP-001",
            ChineseName = "重複",
            DepartmentId = deptId
        };

        var response = await _client.PostAsJsonAsync("/api/erp/employees", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_ValidRequest_Updates()
    {
        using var db = _factory.CreateDbContext();
        var empId = db.Employees.First().Id;

        var request = new
        {
            ChineseName = "張三更新",
            Mobile = "0987654321"
        };

        var response = await _client.PutAsJsonAsync($"/api/erp/employees/{empId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Contains("Employee updated", root.GetProperty("message").GetString()!);
    }

    [Fact]
    public async Task UpdateEmployee_PersonalDetails_ArePersistedAndReturnedByDetailEndpoint()
    {
        using var db = _factory.CreateDbContext();
        var employeeId = db.Employees.First().Id;
        var birthDate = new DateTime(1990, 4, 12);
        var hireDate = new DateTime(2020, 6, 15);
        var probationEndDate = new DateTime(2020, 9, 15);
        var request = new
        {
            NationalId = "A123456789",
            BirthDate = birthDate,
            HireDate = hireDate,
            ProbationEndDate = probationEndDate,
            Gender = "F",
            Phone = "02-12345678",
            RegisteredAddress = "測試戶籍地址",
            MailingAddress = "測試通訊地址"
        };

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/erp/employees/{employeeId}",
            request);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var verifyDb = _factory.CreateDbContext();
        var updatedEmployee = await verifyDb.Employees.FindAsync(employeeId);
        Assert.Equal(request.NationalId, updatedEmployee!.NationalId);
        Assert.Equal(birthDate, updatedEmployee.BirthDate!.Value.Date);
        Assert.Equal(hireDate, updatedEmployee.HireDate!.Value.Date);
        Assert.Equal(probationEndDate, updatedEmployee.ProbationEndDate!.Value.Date);
        Assert.Equal(request.Gender, updatedEmployee.Gender);
        Assert.Equal(request.Phone, updatedEmployee.Phone);
        Assert.Equal(request.RegisteredAddress, updatedEmployee.RegisteredAddress);
        Assert.Equal(request.MailingAddress, updatedEmployee.MailingAddress);

        var detailResponse = await _client.GetAsync($"/api/erp/employees/{employeeId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        using var detailJson = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
        var detail = detailJson.RootElement.GetProperty("data");
        Assert.Equal(request.NationalId, detail.GetProperty("nationalId").GetString());
        Assert.Equal("1990-04-12", detail.GetProperty("birthDate").GetString()![..10]);
        Assert.Equal("2020-06-15", detail.GetProperty("hireDate").GetString()![..10]);
        Assert.Equal("2020-09-15", detail.GetProperty("probationEndDate").GetString()![..10]);
        Assert.Equal(request.Gender, detail.GetProperty("gender").GetString());
        Assert.Equal(request.Phone, detail.GetProperty("phone").GetString());
        Assert.Equal(request.RegisteredAddress, detail.GetProperty("registeredAddress").GetString());
        Assert.Equal(request.MailingAddress, detail.GetProperty("mailingAddress").GetString());
    }

    [Fact]
    public async Task UpdateEmployee_UnknownId_ReturnsNotFound()
    {
        var request = new { ChineseName = "測試" };
        var response = await _client.PutAsJsonAsync($"/api/erp/employees/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmployee_ToLeave_RecordsActorAndKeepsAccountEnabled()
    {
        using var db = _factory.CreateDbContext();
        var employee = db.Employees.First();
        var account = new User
        {
            Username = $"leave-{Guid.NewGuid():N}",
            PasswordHash = "unused",
            EmployeeId = employee.Id
        };
        db.Users.Add(account);
        await db.SaveChangesAsync();

        var response = await _client.PutAsJsonAsync($"/api/erp/employees/{employee.Id}", new
        {
            Status = "leave",
            StatusChangeReason = "留職停薪核准"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var verifyDb = _factory.CreateDbContext();
        Assert.Equal("leave", (await verifyDb.Employees.FindAsync(employee.Id))!.Status);
        var updatedAccount = await verifyDb.Users.FindAsync(account.Id);
        Assert.True(updatedAccount!.IsActive);
        Assert.Equal(1, updatedAccount.SecurityVersion);
        var audit = await verifyDb.AuditLogs.SingleAsync(log =>
            log.EntityId == employee.Id && log.Action == "EMPLOYEE_STATUS_CHANGED");
        Assert.Equal(CustomWebApplicationFactory.DefaultTestUserId, audit.UserId);
        Assert.Contains("'active' to 'leave'", audit.Details);
        Assert.Contains("留職停薪核准", audit.Details);
    }

    [Fact]
    public async Task UpdateEmployee_OrganizationChangeRequiresReasonAndRevokesTokenVersion()
    {
        using var db = _factory.CreateDbContext();
        var employee = db.Employees.First();
        var originalDepartmentId = employee.DepartmentId;
        var newDepartment = new Department
        {
            DeptCode = $"D{Guid.NewGuid():N}"[..20],
            DeptName = "新部門"
        };
        db.Departments.Add(newDepartment);
        var account = new User
        {
            Username = $"transfer-{Guid.NewGuid():N}",
            PasswordHash = "unused",
            EmployeeId = employee.Id
        };
        await db.SaveChangesAsync();
        db.Users.Add(account);
        await db.SaveChangesAsync();

        var missingReasonResponse = await _client.PutAsJsonAsync($"/api/erp/employees/{employee.Id}", new
        {
            DepartmentId = newDepartment.Id
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingReasonResponse.StatusCode);
        using (var afterRejectedChange = _factory.CreateDbContext())
        {
            Assert.Equal(originalDepartmentId,
                (await afterRejectedChange.Employees.FindAsync(employee.Id))!.DepartmentId);
            Assert.Equal(0, (await afterRejectedChange.Users.FindAsync(account.Id))!.SecurityVersion);
        }

        var response = await _client.PutAsJsonAsync($"/api/erp/employees/{employee.Id}", new
        {
            DepartmentId = newDepartment.Id,
            ChangeReason = "組織調動"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyDb = _factory.CreateDbContext();
        Assert.Equal(newDepartment.Id, (await verifyDb.Employees.FindAsync(employee.Id))!.DepartmentId);
        Assert.Equal(1, (await verifyDb.Users.FindAsync(account.Id))!.SecurityVersion);
        var audit = await verifyDb.AuditLogs.SingleAsync(log =>
            log.EntityId == employee.Id && log.Action == "EMPLOYEE_ORGANIZATION_CHANGED");
        Assert.Equal(CustomWebApplicationFactory.DefaultTestUserId, audit.UserId);
        Assert.Contains($"DepartmentId {originalDepartmentId} -> {newDepartment.Id}", audit.Details);
        Assert.Contains("組織調動", audit.Details);
    }

    [Fact]
    public async Task TerminateEmployee_ValidRequest_Terminates()
    {
        using var db = _factory.CreateDbContext();
        var empId = db.Employees.First().Id;
        var account = new User
        {
            Username = $"termination-{Guid.NewGuid():N}",
            PasswordHash = "unused",
            EmployeeId = empId
        };
        db.Users.Add(account);
        await db.SaveChangesAsync();

        var request = new { Reason = "個人原因", EffectiveDate = DateTime.UtcNow.Date };
        var response = await _client.PatchAsync($"/api/erp/employees/{empId}/terminate", JsonContent.Create(request));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Contains("Employee terminated", root.GetProperty("message").GetString()!);

        using var verifyDb = _factory.CreateDbContext();
        var updated = verifyDb.Employees.Find(empId);
        Assert.Equal("terminated", updated!.Status);
        var updatedAccount = verifyDb.Users.Find(account.Id);
        Assert.False(updatedAccount!.IsActive);
        Assert.Equal(1, updatedAccount.SecurityVersion);
        var audit = verifyDb.AuditLogs.Single(log =>
            log.EntityId == empId && log.Action == "EMPLOYEE_STATUS_CHANGED");
        Assert.Equal(CustomWebApplicationFactory.DefaultTestUserId, audit.UserId);
        Assert.Contains("'active' to 'terminated'", audit.Details);
        Assert.Contains("個人原因", audit.Details);
    }

    [Fact]
    public async Task TerminateEmployee_UnknownId_ReturnsNotFound()
    {
        var request = new { Reason = "測試", EffectiveDate = DateTime.UtcNow.Date };
        var response = await _client.PatchAsync($"/api/erp/employees/{Guid.NewGuid()}/terminate", JsonContent.Create(request));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RehireEmployee_TerminatedEmployee_Rehires()
    {
        using var db = _factory.CreateDbContext();
        var employee = db.Employees.First(e => e.Status == "active");
        var empId = employee.Id;
        var account = new User
        {
            Username = $"rehire-{Guid.NewGuid():N}",
            PasswordHash = "unused",
            EmployeeId = empId
        };
        db.Users.Add(account);
        await db.SaveChangesAsync();

        // 先 terminate
        await _client.PatchAsync($"/api/erp/employees/{empId}/terminate", JsonContent.Create(new
        {
            Reason = "測試",
            EffectiveDate = DateTime.UtcNow.Date
        }));

        // 再 rehire
        var response = await _client.PatchAsync(
            $"/api/erp/employees/{empId}/rehire", JsonContent.Create(new { Reason = "重新聘用" }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Contains("Employee rehired", root.GetProperty("message").GetString()!);

        using var verifyDb = _factory.CreateDbContext();
        var updated = verifyDb.Employees.Find(empId);
        Assert.Equal("active", updated!.Status);
        Assert.False((await verifyDb.Users.FindAsync(account.Id))!.IsActive);
    }

    [Fact]
    public async Task RehireEmployee_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PatchAsync(
            $"/api/erp/employees/{Guid.NewGuid()}/rehire", JsonContent.Create(new { Reason = "測試" }));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Departments

    [Fact]
    public async Task GetAllDepartments_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/departments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(1, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task GetDepartmentsFlat_ReturnsFlatList()
    {
        var response = await _client.GetAsync("/api/erp/departments/flat");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetDepartmentsTree_ReturnsTree()
    {
        var response = await _client.GetAsync("/api/erp/departments/tree");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetDepartment_ExistingId_ReturnsDto()
    {
        using var db = _factory.CreateDbContext();
        var deptId = db.Departments.First().Id;

        var response = await _client.GetAsync($"/api/erp/departments/{deptId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("DEPT-HR", data.GetProperty("deptCode").GetString());
    }

    [Fact]
    public async Task GetDepartment_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/erp/departments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateDepartment_ValidRequest_Creates()
    {
        var request = new
        {
            DeptCode = "DEPT-IT",
            DeptName = "資訊部",
            DeptType = "admin",
            BudgetLimit = 300000
        };

        var response = await _client.PostAsJsonAsync("/api/erp/departments", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Contains("Department created", root.GetProperty("message").GetString()!);
    }

    [Fact]
    public async Task CreateDepartment_DuplicateCode_ReturnsBadRequest()
    {
        var request = new
        {
            DeptCode = "DEPT-HR",
            DeptName = "重複部門"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/departments", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDepartment_ValidRequest_Updates()
    {
        using var db = _factory.CreateDbContext();
        var deptId = db.Departments.First().Id;

        var request = new
        {
            DeptCode = "DEPT-HR",
            DeptName = "人資部更新"
        };

        var response = await _client.PutAsJsonAsync($"/api/erp/departments/{deptId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDepartment_UnknownId_ReturnsNotFound()
    {
        var request = new { DeptCode = "DEPT-TEST", DeptName = "測試" };
        var response = await _client.PutAsJsonAsync($"/api/erp/departments/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDepartment_DuplicateCode_ReturnsBadRequest()
    {
        using var db = _factory.CreateDbContext();
        // 建立第二個部門
        var dept2 = new Department { DeptCode = "DEPT-TEST", DeptName = "測試", IsActive = true };
        db.Departments.Add(dept2);
        db.SaveChanges();

        var request = new { DeptCode = "DEPT-HR", DeptName = "重複代碼" };
        var response = await _client.PutAsJsonAsync($"/api/erp/departments/{dept2.Id}", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDepartment_EmptyDepartment_Deletes()
    {
        using var db = _factory.CreateDbContext();
        var emptyDept = new Department { DeptCode = "DEPT-DELETE", DeptName = "可刪除", IsActive = true };
        db.Departments.Add(emptyDept);
        db.SaveChanges();
        var deptId = emptyDept.Id;

        var response = await _client.DeleteAsync($"/api/erp/departments/{deptId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDepartment_Guarded_DeletesFails()
    {
        using var db = _factory.CreateDbContext();
        var deptId = db.Departments.First().Id; // 有員工關聯

        var response = await _client.DeleteAsync($"/api/erp/departments/{deptId}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDepartment_HasSubDepartment_DeletesFails()
    {
        using var db = _factory.CreateDbContext();
        var parentId = db.Departments.First().Id;
        var child = new Department { DeptCode = "DEPT-CHILD", DeptName = "子部門", ParentDeptId = parentId, IsActive = true };
        db.Departments.Add(child);
        db.SaveChanges();

        var response = await _client.DeleteAsync($"/api/erp/departments/{parentId}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion
}
