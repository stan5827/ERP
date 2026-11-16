using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.HR;

namespace ErpBackend.IntegrationTests;

public class LeaveRequestsJobTitlesJobGradesIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();
    private Guid _deptId;
    private Guid _gradeId;
    private Guid _jobTitleId;
    private Guid _empId;
    private Guid _emp2Id;

    public LeaveRequestsJobTitlesJobGradesIntegrationTests()
    {
        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.LeaveRequests.RemoveRange(db.LeaveRequests);
        db.Employees.RemoveRange(db.Employees);
        db.JobTitles.RemoveRange(db.JobTitles);
        db.JobGrades.RemoveRange(db.JobGrades);
        db.Departments.RemoveRange(db.Departments);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var dept = new Department
        {
            DeptCode = "DEPT-HR2",
            DeptName = "人資部 B",
            DeptType = "admin",
            IsActive = true
        };
        db.Departments.Add(dept);
        db.SaveChanges();
        _deptId = dept.Id;

        var grade = new JobGrade
        {
            GradeCode = "G10",
            GradeName = "職等 10",
            Level = 10,
            MinSalary = 40000,
            MaxSalary = 80000,
            IsActive = true
        };
        db.JobGrades.Add(grade);
        db.SaveChanges();
        _gradeId = grade.Id;

        var jobTitle = new JobTitle
        {
            TitleCode = "MGR",
            TitleName = "經理",
            GradeId = grade.Id,
            IsActive = true
        };
        db.JobTitles.Add(jobTitle);
        db.SaveChanges();
        _jobTitleId = jobTitle.Id;

        var emp = new Employee
        {
            EmployeeNo = "EMP-HR-001",
            ChineseName = "請假員工",
            DepartmentId = dept.Id,
            JobTitleId = jobTitle.Id,
            GradeId = grade.Id,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Employees.Add(emp);
        db.SaveChanges();
        _empId = emp.Id;

        var emp2 = new Employee
        {
            EmployeeNo = "EMP-HR-002",
            ChineseName = "主管",
            DepartmentId = dept.Id,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Employees.Add(emp2);
        db.SaveChanges();
        _emp2Id = emp2.Id;
    }

    #region LeaveRequests

    [Fact]
    public async Task GetAllLeaveRequests_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/leave-requests");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateLeaveRequest_WhenScheduleIsNotConfigured_ReturnsUnavailableWithoutPersisting()
    {
        var start = DateTime.UtcNow.AddDays(2);
        var end = start.AddDays(2);
        var request = new
        {
            EmployeeId = _empId,
            LeaveType = "special",
            StartDate = start,
            EndDate = end,
            Reason = "結婚假",
            IncludesSpecialPeriod = true
        };

        var response = await _client.PostAsJsonAsync("/api/erp/leave-requests", request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        using var db = _factory.CreateDbContext();
        Assert.Empty(await db.LeaveRequests.ToListAsync());
    }

    [Fact]
    public async Task CreateLeaveRequest_UnknownEmployee_ReturnsUnavailableUntilScheduleIsConfigured()
    {
        var request = new
        {
            EmployeeId = Guid.NewGuid().ToString(),
            LeaveType = "sick",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2)
        };

        var response = await _client.PostAsJsonAsync("/api/erp/leave-requests", request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CreateLeaveRequest_NonGuidEmployeeId_ReturnsBadRequest()
    {
        var request = new
        {
            EmployeeId = "not-a-guid",
            LeaveType = "sick",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(2)
        };

        var response = await _client.PostAsJsonAsync("/api/erp/leave-requests", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLeaveRequest_ExistingId_ReturnsDto()
    {
        using var db = _factory.CreateDbContext();
        var lr = new LeaveRequest
        {
            ApplicationNo = "LR-TEST-001",
            EmployeeId = _empId,
            LeaveType = "sick",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1),
            TotalHours = 8,
            Status = "pending",
            ApplicationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        db.LeaveRequests.Add(lr);
        db.SaveChanges();

        var response = await _client.GetAsync($"/api/erp/leave-requests/{lr.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("LR-TEST-001", data.GetProperty("applicationNo").GetString());
    }

    [Fact]
    public async Task GetLeaveRequest_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/erp/leave-requests/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApproveLeaveRequest_WhenWorkflowIsNotConfigured_ReturnsUnavailableWithoutChangingRequest()
    {
        using var db = _factory.CreateDbContext();
        var lr = new LeaveRequest
        {
            ApplicationNo = "LR-APPROVE-001",
            EmployeeId = _empId,
            LeaveType = "sick",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1),
            TotalHours = 8,
            Status = "pending",
            ApplicationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        db.LeaveRequests.Add(lr);
        db.SaveChanges();

        var request = new { ApproverId = _emp2Id, Remarks = "核准" };
        var response = await _client.PatchAsJsonAsync($"/api/erp/leave-requests/{lr.Id}/approve", request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        using var verifyDb = _factory.CreateDbContext();
        Assert.Equal("pending", (await verifyDb.LeaveRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task ApproveLeaveRequest_WhenWorkflowIsNotConfigured_PreservesExistingApprovedStatus()
    {
        using var db = _factory.CreateDbContext();
        var lr = new LeaveRequest
        {
            ApplicationNo = "LR-APPROVE-GUARD",
            EmployeeId = _empId,
            LeaveType = "sick",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1),
            TotalHours = 8,
            Status = "approved",
            ApplicationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        db.LeaveRequests.Add(lr);
        db.SaveChanges();

        var request = new { ApproverId = _emp2Id };
        var response = await _client.PatchAsJsonAsync($"/api/erp/leave-requests/{lr.Id}/approve", request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        using var verifyDb = _factory.CreateDbContext();
        Assert.Equal("approved", (await verifyDb.LeaveRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task RejectLeaveRequest_ValidRequest_Rejects()
    {
        using var db = _factory.CreateDbContext();
        var lr = new LeaveRequest
        {
            ApplicationNo = "LR-REJECT-001",
            EmployeeId = _empId,
            LeaveType = "sick",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1),
            TotalHours = 8,
            Status = "pending",
            ApplicationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        db.LeaveRequests.Add(lr);
        db.SaveChanges();

        var request = new { ApproverId = _emp2Id, RejectReason = "原因不充分" };
        var response = await _client.PatchAsJsonAsync($"/api/erp/leave-requests/{lr.Id}/reject", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetLeaveRequests_ByEmployee_ReturnsFiltered()
    {
        var response = await _client.GetAsync($"/api/erp/leave-requests?employeeId={_empId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    #endregion

    #region JobTitles

    [Fact]
    public async Task GetAllJobTitles_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/job-titles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(1, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task GetJobTitle_ExistingId_ReturnsDto()
    {
        var response = await _client.GetAsync($"/api/erp/job-titles/{_jobTitleId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("MGR", data.GetProperty("titleCode").GetString());
    }

    [Fact]
    public async Task GetJobTitle_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/erp/job-titles/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateJobTitle_ValidRequest_Creates()
    {
        var request = new
        {
            TitleCode = "ENG",
            TitleName = "工程師",
            GradeId = _gradeId,
            IsActive = true
        };

        var response = await _client.PostAsJsonAsync("/api/erp/job-titles", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateJobTitle_DuplicateCode_ReturnsBadRequest()
    {
        var request = new
        {
            TitleCode = "MGR",
            TitleName = "重複職稱"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/job-titles", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateJobTitle_EmptyCode_ReturnsBadRequest()
    {
        var request = new
        {
            TitleCode = "",
            TitleName = "空代碼"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/job-titles", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateJobTitle_ValidRequest_Updates()
    {
        var request = new
        {
            TitleCode = "MGR",
            TitleName = "高級經理",
            GradeId = _gradeId
        };

        var response = await _client.PutAsJsonAsync($"/api/erp/job-titles/{_jobTitleId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateJobTitle_UnknownId_ReturnsNotFound()
    {
        var request = new { TitleCode = "NEW", TitleName = "測試" };
        var response = await _client.PutAsJsonAsync($"/api/erp/job-titles/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteJobTitle_GuardedByEmployee_ReturnsBadRequest()
    {
        // _jobTitleId 有員工關聯 (_empId)
        var response = await _client.DeleteAsync($"/api/erp/job-titles/{_jobTitleId}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region JobGrades

    [Fact]
    public async Task GetAllJobGrades_ReturnsList()
    {
        var response = await _client.GetAsync("/api/erp/job-grades");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(1, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task GetJobGrade_ExistingId_ReturnsDto()
    {
        var response = await _client.GetAsync($"/api/erp/job-grades/{_gradeId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal("G10", data.GetProperty("gradeCode").GetString());
    }

    [Fact]
    public async Task GetJobGrade_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/erp/job-grades/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateJobGrade_ValidRequest_Creates()
    {
        var request = new
        {
            GradeCode = "G20",
            GradeName = "職等 20",
            Level = 20,
            MinSalary = 60000,
            MaxSalary = 120000,
            IsActive = true
        };

        var response = await _client.PostAsJsonAsync("/api/erp/job-grades", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CreateJobGrade_DuplicateCode_ReturnsBadRequest()
    {
        var request = new
        {
            GradeCode = "G10",
            GradeName = "重複職等"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/job-grades", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateJobGrade_EmptyCode_ReturnsBadRequest()
    {
        var request = new
        {
            GradeCode = "",
            GradeName = "空代碼"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/job-grades", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateJobGrade_ValidRequest_Updates()
    {
        var request = new
        {
            GradeCode = "G10",
            GradeName = "職等 10 更新",
            MinSalary = 45000
        };

        var response = await _client.PutAsJsonAsync($"/api/erp/job-grades/{_gradeId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateJobGrade_UnknownId_ReturnsNotFound()
    {
        var request = new { GradeCode = "G-NEW", GradeName = "測試" };
        var response = await _client.PutAsJsonAsync($"/api/erp/job-grades/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteJobGrade_GuardedByJobTitle_ReturnsBadRequest()
    {
        // _gradeId 有 JobTitle 關聯 (_jobTitleId)
        var response = await _client.DeleteAsync($"/api/erp/job-grades/{_gradeId}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion
}
