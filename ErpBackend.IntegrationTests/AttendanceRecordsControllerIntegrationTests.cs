using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.HR;

namespace ErpBackend.IntegrationTests;

public class AttendanceRecordsControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();
    private Guid _employeeId;

    public AttendanceRecordsControllerIntegrationTests()
    {
        SeedAttendanceData();
    }

    private void SeedAttendanceData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.AttendanceRecords.RemoveRange(db.AttendanceRecords);
        db.Employees.RemoveRange(db.Employees);
        db.Departments.RemoveRange(db.Departments);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        var dept = new Department
        {
            DeptCode = "DEPT-ATT",
            DeptName = "出勤測試部",
            IsActive = true
        };
        db.Departments.Add(dept);
        db.SaveChanges();

        var emp = new Employee
        {
            EmployeeNo = "EMP-ATT-001",
            ChineseName = "出勤測試員",
            DepartmentId = dept.Id,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Employees.Add(emp);
        db.SaveChanges();
        _employeeId = emp.Id;
    }

    [Fact]
    public async Task GetAttendanceRecords_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/erp/attendance-records");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task ClockIn_ValidEmployee_ReturnsOk()
    {
        var request = new { EmployeeId = _employeeId.ToString() };
        var response = await _client.PostAsJsonAsync("/api/erp/attendance-records/clock-in", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task ClockIn_InvalidEmployeeId_ReturnsBadRequest()
    {
        var request = new { EmployeeId = "not-a-guid" };
        var response = await _client.PostAsJsonAsync("/api/erp/attendance-records/clock-in", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ClockOut_AfterClockIn_ReturnsOk()
    {
        using var db = _factory.CreateDbContext();
        var today = DateTime.UtcNow.Date;
        var record = new AttendanceRecord
        {
            EmployeeId = _employeeId,
            Date = today,
            ClockIn = today.AddHours(9),
            Status = "present",
            ApprovalStatus = "approved"
        };
        db.AttendanceRecords.Add(record);
        db.SaveChanges();

        var request = new { EmployeeId = _employeeId.ToString() };
        var response = await _client.PostAsJsonAsync("/api/erp/attendance-records/clock-out", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ClockOut_NoClockIn_ReturnsNotFound()
    {
        using var db = _factory.CreateDbContext();
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var record = new AttendanceRecord
        {
            EmployeeId = _employeeId,
            Date = yesterday,
            ClockIn = yesterday.AddHours(9),
            Status = "present"
        };
        db.AttendanceRecords.Add(record);
        db.SaveChanges();

        var request = new { EmployeeId = _employeeId.ToString() };
        var response = await _client.PostAsJsonAsync("/api/erp/attendance-records/clock-out", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BatchImport_ValidRecords_ReturnsOk()
    {
        var today = DateTime.UtcNow.Date;
        var request = new
        {
            Records = new[]
            {
                new
                {
                    EmployeeId = _employeeId,
                    Date = today,
                    ShiftType = "day",
                    ClockIn = today.AddHours(8),
                    ClockOut = today.AddHours(17),
                    Status = "present"
                },
                new
                {
                    EmployeeId = _employeeId,
                    Date = today.AddDays(1),
                    ShiftType = "day",
                    ClockIn = today.AddDays(1).AddHours(8),
                    ClockOut = today.AddDays(1).AddHours(17),
                    Status = "present"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/erp/attendance-records/batch-import", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Contains("2 records imported", root.GetProperty("message").GetString()!);
    }
}
