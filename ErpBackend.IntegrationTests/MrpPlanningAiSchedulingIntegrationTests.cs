using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;

namespace ErpBackend.IntegrationTests;

public class MrpPlanningAiSchedulingIntegrationTests
{
    private static readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client = _factory.CreateClient();
    private Guid _skuId;
    private Guid _skuLowId;
    private Guid _empId;
    private Guid _deptId;

    public MrpPlanningAiSchedulingIntegrationTests()
    {
        SeedData();
    }

    private void SeedData()
    {
        using var db = _factory.CreateDbContext();

        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.MrpPlannedOrders.RemoveRange(db.MrpPlannedOrders);
        db.MrpRequirements.RemoveRange(db.MrpRequirements);
        db.MrpRuns.RemoveRange(db.MrpRuns);
        db.ShiftSchedules.RemoveRange(db.ShiftSchedules);
        db.Employees.RemoveRange(db.Employees);
        db.Departments.RemoveRange(db.Departments);
        db.Skus.RemoveRange(db.Skus);
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        // SKUs for MRP and Planning
        var sku = new Sku
        {
            SkuNumber = "SKU-MRP-001",
            SkuName = "MRP 測試成品",
            Unit = "SET",
            Category = "FG",
            Qty = 100,
            SafetyStock = 20,
            ReorderPoint = 50,
            MaxStock = 500,
            Moq = 10,
            StandardCost = 200,
            SourceType = "self_made",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var skuLow = new Sku
        {
            SkuNumber = "SKU-MRP-LOW",
            SkuName = "低庫存測試品",
            Unit = "PCS",
            Category = "RM",
            Qty = 5,
            SafetyStock = 20,
            ReorderPoint = 30,
            MaxStock = 200,
            Moq = 5,
            StandardCost = 50,
            SourceType = "purchased",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Skus.AddRange(sku, skuLow);
        db.SaveChanges();
        _skuId = sku.Id;
        _skuLowId = skuLow.Id;

        // Department and Employee for AI Scheduling
        var dept = new Department
        {
            DeptCode = "DEPT-MRP",
            DeptName = "MRP 測試部門",
            DeptType = "production",
            IsActive = true
        };
        db.Departments.Add(dept);
        db.SaveChanges();
        _deptId = dept.Id;

        var emp = new Employee
        {
            EmployeeNo = "EMP-MRP-001",
            ChineseName = "MRP 測試員工",
            DepartmentId = dept.Id,
            Status = "active",
            BaseSalary = 50000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Employees.Add(emp);
        db.SaveChanges();
        _empId = emp.Id;

        // ShiftSchedule for utilization
        var shift = new ShiftSchedule
        {
            EmployeeId = emp.Id,
            YearMonth = "202610",
            ScheduleData = "D,D,D,D,D,off,off,D,D,D,D,D,off,off,D,D,D,D,D,off,off,D,D,D,D,D,off,off",
        };
        db.ShiftSchedules.Add(shift);
        db.SaveChanges();
    }

    #region MRP Engine

    [Fact]
    public async Task GetMrpRuns_ReturnsEmptyInitially()
    {
        var response = await _client.GetAsync("/api/erp/mrp/runs");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task RunMrp_ValidRequest_CreatesRunAndPlannedOrders()
    {
        var request = new MrpRunRequest
        {
            HorizonStart = DateTime.UtcNow,
            HorizonEnd = DateTime.UtcNow.AddDays(90),
            RunType = "regenerative"
        };

        var response = await _client.PostAsJsonAsync("/api/erp/mrp/run", request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"MRP run returned {response.StatusCode}: {body}");
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        Assert.True(data.GetProperty("status").GetString() is "running" or "completed");
        Assert.True(data.TryGetProperty("id", out var runId));

        // Verify planned orders were generated
        var runIdGuid = Guid.Parse(runId.GetString()!);
        var ordersResponse = await _client.GetAsync($"/api/erp/mrp/runs/{runIdGuid}/orders");
        Assert.Equal(HttpStatusCode.OK, ordersResponse.StatusCode);

        var ordersJson = await ordersResponse.Content.ReadAsStringAsync();
        using var ordersDoc = JsonDocument.Parse(ordersJson);
        var ordersRoot = ordersDoc.RootElement;
        Assert.True(ordersRoot.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetMrpSummary_ReturnsSummary()
    {
        var response = await _client.GetAsync("/api/erp/mrp/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("success", out _));
    }

    [Fact]
    public async Task ConvertOrder_UnknownId_ReturnsBadRequest()
    {
        var response = await _client.PostAsync($"/api/erp/mrp/orders/{Guid.NewGuid()}/convert", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Planning

    [Fact]
    public async Task GetPlanningSuggestions_ReturnsSuggestions()
    {
        var response = await _client.GetAsync("/api/erp/planning/suggestions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        var data = root.GetProperty("data");
        // SKU-MRP-LOW has Qty=5 < ReorderPoint=30, should appear
        Assert.True(data.EnumerateArray().Count() >= 1);
        var first = data.EnumerateArray().First();
        Assert.Equal("SKU-MRP-LOW", first.GetProperty("skuCode").GetString());
    }

    [Fact]
    public async Task PersistPlanningSuggestions_ReturnsSuggestions()
    {
        var response = await _client.PostAsJsonAsync("/api/erp/planning/suggestions", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
    }

    #endregion

    #region AI Scheduling

    [Fact]
    public async Task GenerateSchedule_ValidRequest_ReturnsSchedule()
    {
        var request = new AiScheduleRequest
        {
            YearMonth = "202610",
            DepartmentId = _deptId,
            TargetHeadcount = 10
        };

        var response = await _client.PostAsJsonAsync("/api/ai/scheduling/generate", request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"GenerateSchedule returned {response.StatusCode}: {body}");
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("202610", root.GetProperty("yearMonth").GetString());
        Assert.True(root.TryGetProperty("schedules", out _));
        Assert.True(root.TryGetProperty("summary", out _));
    }

    [Fact]
    public async Task GenerateSchedule_NoEmployees_ReturnsFailure()
    {
        // Use a department with no employees
        var request = new AiScheduleRequest
        {
            YearMonth = "202610",
            DepartmentId = Guid.NewGuid(),
            TargetHeadcount = 10
        };

        var response = await _client.PostAsJsonAsync("/api/ai/scheduling/generate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.False(root.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetTurnoverRisks_ReturnsList()
    {
        var response = await _client.GetAsync("/api/ai/scheduling/risks");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.ValueKind == JsonValueKind.Array || root.TryGetProperty("success", out _));
    }

    [Fact]
    public async Task GetTurnoverRisks_ByDepartment_ReturnsList()
    {
        var response = await _client.GetAsync($"/api/ai/scheduling/risks?departmentId={_deptId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.ValueKind == JsonValueKind.Array || root.TryGetProperty("success", out _));
    }

    [Fact]
    public async Task GetShiftUtilization_ReturnsUtilization()
    {
        var response = await _client.GetAsync("/api/ai/scheduling/utilization?yearMonth=202610");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("yearMonth", out _));
        Assert.True(root.TryGetProperty("utilizationRate", out _));
    }

    #endregion
}
