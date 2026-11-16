using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.HR;

namespace ErpBackend.IntegrationTests;

public class PayrollControllerIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();
    private Guid _employeeId;

    public PayrollControllerIntegrationTests()
    {
        SeedPayrollData();
    }

    private void SeedPayrollData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.Payrolls.RemoveRange(db.Payrolls);
        db.AttendanceRecords.RemoveRange(db.AttendanceRecords);
        db.LeaveRequests.RemoveRange(db.LeaveRequests);
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
            IsCostCenter = true
        };
        db.Departments.Add(dept);
        db.SaveChanges();

        var emp = new Employee
        {
            EmployeeNo = "EMP-001",
            ChineseName = "測試員工",
            DepartmentId = dept.Id,
            Status = "active",
            BaseSalary = 50000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Employees.Add(emp);
        db.SaveChanges();
        _employeeId = emp.Id;
    }

    [Fact]
    public async Task GetPayrollRecords_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/erp/payroll");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.Equal(0, data.EnumerateArray().Count());
    }

    [Fact]
    public async Task CalculatePayroll_WhenRulesAreNotConfigured_ReturnsUnavailableWithoutPersistingPayroll()
    {
        var request = new
        {
            EmployeeId = _employeeId,
            PeriodStart = new DateTime(2026, 10, 1),
            PeriodEnd = new DateTime(2026, 10, 31)
        };

        var response = await _client.PostAsJsonAsync("/api/erp/payroll/calculate", request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        using var db = _factory.CreateDbContext();
        Assert.Empty(await db.Payrolls.ToListAsync());
    }

    [Fact]
    public async Task CalculatePayroll_WhenRulesAreNotConfigured_ReturnsUnavailableForUnknownEmployee()
    {
        var request = new
        {
            EmployeeId = Guid.NewGuid(),
            PeriodStart = new DateTime(2026, 10, 1),
            PeriodEnd = new DateTime(2026, 10, 31)
        };

        var response = await _client.PostAsJsonAsync("/api/erp/payroll/calculate", request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task ApprovePayroll_ValidId_ReturnsOk()
    {
        using var db = _factory.CreateDbContext();
        var payroll = new Payroll
        {
            EmployeeId = _employeeId,
            PeriodStart = new DateTime(2026, 10, 1),
            PeriodEnd = new DateTime(2026, 10, 31),
            BasicSalary = 50000,
            GrossPay = 50000,
            TotalDeductions = 0,
            NetPay = 50000,
            Status = "calculated",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Payrolls.Add(payroll);
        db.SaveChanges();

        var response = await _client.PatchAsync($"/api/erp/payroll/{payroll.Id}/approve", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyDb = _factory.CreateDbContext();
        var updated = await verifyDb.Payrolls.FindAsync(payroll.Id);
        Assert.Equal("approved", updated!.Status);
    }

    [Fact]
    public async Task ApprovePayroll_UnknownId_ReturnsNotFound()
    {
        var response = await _client.PatchAsync($"/api/erp/payroll/{Guid.NewGuid()}/approve", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPayrollSummary_ReturnsSummary()
    {
        var start = new DateTime(2026, 10, 1);
        var end = new DateTime(2026, 10, 31);
        var response = await _client.GetAsync($"/api/erp/payroll/summary?periodStart={start:yyyy-MM-dd}&periodEnd={end:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }
}
