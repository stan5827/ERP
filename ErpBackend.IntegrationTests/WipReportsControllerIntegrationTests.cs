using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using ErpBackend.Data;
using ErpBackend.Models.Finance;
using ErpBackend.Models.HR;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.System;

namespace ErpBackend.IntegrationTests;

[CollectionDefinition("WipReports", DisableParallelization = true)]
public sealed class WipReportsCollection { }

[Collection("WipReports")]
public class WipReportsControllerIntegrationTests
{
    private static readonly Guid ActorUserId = Guid.Parse("2a7c4048-a16d-4d08-ae03-1d07eb3ef001");
    private static readonly Guid ActorEmployeeId = Guid.Parse("2a7c4048-a16d-4d08-ae03-1d07eb3ef002");
    private static readonly Guid ReviewerUserId = Guid.Parse("2a7c4048-a16d-4d08-ae03-1d07eb3ef003");
    private static readonly Guid ReviewerEmployeeId = Guid.Parse("2a7c4048-a16d-4d08-ae03-1d07eb3ef004");
    private static readonly CustomWebApplicationFactory Factory = new();
    private readonly HttpClient _client = Factory.CreateClient();

    public WipReportsControllerIntegrationTests()
    {
        SeedWipData();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", ActorUserId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Permissions", "manufacturing.write");
        _client.DefaultRequestHeaders.Add("X-Test-Roles", "Operator");
    }

    [Fact]
    public async Task ReportCompletion_IsSubmittedButOnlyApprovalUpdatesProgress()
    {
        var response = await _client.PostAsJsonAsync("/api/erp/wipreports/submit", BuildReportRequest(45));
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<ApiResponseData>();
        Assert.Equal("submitted", result!.Data!.Status);

        using (var db = Factory.CreateDbContext())
        {
            Assert.Equal(0m, db.WorkOrders.Single(item => item.WoNumber == "WO-WIP-001").ActualQty);
            Assert.Equal(0m, db.WorkOrderOperations.Single().CompletedQty);
            Assert.Equal("submitted", db.WipReports.Single().Status);
        }
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/erp/wipreports/my")).StatusCode);
        using (var queueRequest = new HttpRequestMessage(HttpMethod.Get, "/api/erp/wipreports/review-queue"))
        {
            queueRequest.Headers.Add("X-Test-UserId", ReviewerUserId.ToString());
            queueRequest.Headers.Add("X-Test-Permissions", "manufacturing.approve");
            queueRequest.Headers.Add("X-Test-Roles", "Production Supervisor");
            var queueResponse = await _client.SendAsync(queueRequest);
            Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
            Assert.Contains("WO-WIP-001", await queueResponse.Content.ReadAsStringAsync());
        }

        using var approveRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/erp/wipreports/{result.Data.Id}/approve")
        {
            Content = JsonContent.Create(new { comments = "數量確認無誤" })
        };
        approveRequest.Headers.Add("X-Test-UserId", ReviewerUserId.ToString());
        approveRequest.Headers.Add("X-Test-Permissions", "manufacturing.approve");
        approveRequest.Headers.Add("X-Test-Roles", "Production Supervisor");
        var approveResponse = await _client.SendAsync(approveRequest);

        Assert.True(approveResponse.StatusCode == HttpStatusCode.OK, await approveResponse.Content.ReadAsStringAsync());
        using var verifyDb = Factory.CreateDbContext();
        Assert.Equal(45m, verifyDb.WorkOrders.Single(item => item.WoNumber == "WO-WIP-001").ActualQty);
        Assert.Equal(45m, verifyDb.WorkOrderOperations.Single().CompletedQty);
        Assert.Equal("approved", verifyDb.WipReports.Single().Status);
        Assert.Contains(verifyDb.WipReportAuditEvents, item => item.Action == "approved");
    }

    [Fact]
    public async Task ReturnedReport_CanBeCorrectedAndResubmittedWithAuditTrail()
    {
        var response = await _client.PostAsJsonAsync("/api/erp/wipreports/submit", BuildReportRequest(20));
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<ApiResponseData>();

        using var returnRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/erp/wipreports/{result!.Data!.Id}/return")
        {
            Content = JsonContent.Create(new { comments = "請重新確認數量" })
        };
        returnRequest.Headers.Add("X-Test-UserId", ReviewerUserId.ToString());
        returnRequest.Headers.Add("X-Test-Permissions", "manufacturing.approve");
        returnRequest.Headers.Add("X-Test-Roles", "Production Supervisor");
        var returnResponse = await _client.SendAsync(returnRequest);
        Assert.True(returnResponse.StatusCode == HttpStatusCode.OK, await returnResponse.Content.ReadAsStringAsync());

        var correctedRequest = BuildReportRequest(18);
        var resubmitResponse = await _client.PostAsJsonAsync(
            $"/api/erp/wipreports/{result.Data.Id}/resubmit",
            correctedRequest);

        Assert.True(resubmitResponse.StatusCode == HttpStatusCode.OK, await resubmitResponse.Content.ReadAsStringAsync());
        using var db = Factory.CreateDbContext();
        var report = db.WipReports.Single();
        Assert.Equal("submitted", report.Status);
        Assert.Equal(18m, report.CompletedQty);
        Assert.Contains(db.WipReportAuditEvents, item => item.Action == "returned" && item.Reason == "請重新確認數量");
        Assert.Contains(db.WipReportAuditEvents, item => item.Action == "resubmitted" && item.ChangesJson!.Contains("previous"));
        Assert.Equal(0m, db.WorkOrders.Single(item => item.WoNumber == "WO-WIP-001").ActualQty);
    }

    [Fact]
    public async Task TeamReportCountsOutputOnceAndEquipmentUsageIsRecordedSeparately()
    {
        using var db = Factory.CreateDbContext();
        var workOrder = db.WorkOrders.Single(item => item.WoNumber == "WO-WIP-001");
        var equipment = db.Equipments.Single(item => item.EquipmentCode == "EQ-WIP-01");
        var teamRequest = new Dictionary<string, object?>
        {
            ["woNumber"] = "WO-WIP-001",
            ["operationSeq"] = 10,
            ["completedQty"] = 25,
            ["reportType"] = "team",
            ["employeeId"] = ActorEmployeeId,
            ["teamMemberEmployeeIds"] = new[] { ReviewerEmployeeId },
            ["teamMemberHours"] = new[]
            {
                new { employeeId = ActorEmployeeId, manHours = 0.5m },
                new { employeeId = ReviewerEmployeeId, manHours = 0.5m }
            },
            ["shiftClass"] = "day",
            ["manHours"] = 1,
            ["machineHours"] = 0,
            ["startTime"] = DateTime.UtcNow.AddHours(-1),
            ["endTime"] = DateTime.UtcNow
        };
        var reportResponse = await _client.PostAsJsonAsync("/api/erp/wipreports/submit", teamRequest);
        Assert.True(reportResponse.StatusCode == HttpStatusCode.OK, await reportResponse.Content.ReadAsStringAsync());
        var myReportsResponse = await _client.GetAsync("/api/erp/wipreports/my");
        Assert.Contains("team", await myReportsResponse.Content.ReadAsStringAsync());

        using var equipmentRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/erp/wipreports/equipment-usage")
        {
            Content = JsonContent.Create(new
            {
                workOrderId = workOrder.Id,
                operationSeq = 10,
                equipmentId = equipment.Id,
                machineHours = 2.5m,
                shiftClass = "day",
                startTime = DateTime.UtcNow.AddHours(-1),
                endTime = DateTime.UtcNow,
                remarks = "獨立設備紀錄"
            })
        };
        equipmentRequest.Headers.Add("X-Test-UserId", ReviewerUserId.ToString());
        equipmentRequest.Headers.Add("X-Test-Roles", "Production Supervisor");
        var equipmentResponse = await _client.SendAsync(equipmentRequest);
        Assert.Equal(HttpStatusCode.Created, equipmentResponse.StatusCode);

        Guid reportId;
        using (var reportDb = Factory.CreateDbContext())
            reportId = reportDb.WipReports.Single().Id;
        using var approveRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/erp/wipreports/{reportId}/approve")
        {
            Content = JsonContent.Create(new { comments = "班組產量確認" })
        };
        approveRequest.Headers.Add("X-Test-UserId", ReviewerUserId.ToString());
        approveRequest.Headers.Add("X-Test-Permissions", "manufacturing.approve");
        approveRequest.Headers.Add("X-Test-Roles", "Production Supervisor");
        var approveResponse = await _client.SendAsync(approveRequest);
        Assert.True(approveResponse.StatusCode == HttpStatusCode.OK, await approveResponse.Content.ReadAsStringAsync());

        using var verifyDb = Factory.CreateDbContext();
        var report = verifyDb.WipReports.Single();
        Assert.Equal(25m, report.CompletedQty);
        Assert.Equal("team", report.ReportType);
        Assert.Equal(2, verifyDb.WipReportParticipants.Count(item => item.WipReportId == report.Id));
        Assert.Equal(25m, verifyDb.WorkOrderOperations.Single().CompletedQty);
        Assert.Equal(25m, verifyDb.WorkOrders.Single(item => item.Id == workOrder.Id).ActualQty);
        Assert.Equal(
            new[] { ActorEmployeeId, ReviewerEmployeeId }.OrderBy(id => id),
            verifyDb.WorkOrderLaborActuals.Select(item => item.EmployeeId!.Value).OrderBy(id => id));
        var usage = verifyDb.EquipmentUsageRecords.Single();
        Assert.Equal(2.5m, usage.MachineHours);
        var machineCost = verifyDb.WorkOrderMachineActuals.Single();
        Assert.Equal(usage.Id, machineCost.EquipmentUsageRecordId);
        Assert.Null(machineCost.WipReportId);
    }

    [Fact]
    public async Task UnauthenticatedUser_CannotSubmitReport()
    {
        using var anonymousClient = Factory.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        var response = await anonymousClient.PostAsJsonAsync(
            "/api/erp/wipreports/submit",
            BuildReportRequest(5));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static object BuildReportRequest(decimal completedQty)
    {
        var end = DateTime.UtcNow;
        return new
        {
            WoNumber = "WO-WIP-001",
            OperationSeq = 10,
            CompletedQty = completedQty,
            DefectQty = 0,
            ScrapQty = 0,
            ManHours = 1.0,
            MachineHours = 0,
            SetupHours = 0,
            RunHours = 1.0,
            EmployeeId = ActorEmployeeId,
            ReportType = "individual",
            TeamMemberEmployeeIds = Array.Empty<Guid>(),
            ShiftClass = "day",
            StartTime = end.AddHours(-1),
            EndTime = end
        };
    }

    private static void SeedWipData()
    {
        using var db = Factory.CreateDbContext();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF");
        db.WipReportAuditEvents.RemoveRange(db.WipReportAuditEvents);
        db.WipReportParticipants.RemoveRange(db.WipReportParticipants);
        db.EquipmentUsageRecords.RemoveRange(db.EquipmentUsageRecords);
        db.WipReportEquipmentHours.RemoveRange(db.WipReportEquipmentHours);
        db.WorkOrderMachineActuals.RemoveRange(db.WorkOrderMachineActuals);
        db.WorkOrderLaborActuals.RemoveRange(db.WorkOrderLaborActuals);
        db.WipReports.RemoveRange(db.WipReports);
        db.Equipments.RemoveRange(db.Equipments);
        db.WorkOrderOperations.RemoveRange(db.WorkOrderOperations);
        db.WorkOrders.RemoveRange(db.WorkOrders);
        db.WorkCenterCosts.RemoveRange(db.WorkCenterCosts);
        db.Operations.RemoveRange(db.Operations);
        db.WorkCenters.RemoveRange(db.WorkCenters);
        db.Skus.RemoveRange(db.Skus);
        db.CostCenters.RemoveRange(db.CostCenters);
        db.UserRoles.RemoveRange(db.UserRoles.Where(userRole =>
            userRole.UserId == ActorUserId || userRole.UserId == ReviewerUserId));
        db.Users.RemoveRange(db.Users.Where(user => user.Id == ActorUserId || user.Id == ReviewerUserId));
        db.Employees.RemoveRange(db.Employees.Where(employee =>
            employee.Id == ActorEmployeeId || employee.Id == ReviewerEmployeeId));
        db.SaveChanges();
        db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON");

        db.Employees.AddRange(
            new Employee { Id = ActorEmployeeId, EmployeeNo = "E-WIP-001", ChineseName = "報工人員", Status = "active" },
            new Employee { Id = ReviewerEmployeeId, EmployeeNo = "E-WIP-002", ChineseName = "審核主管", Status = "active" });
        db.Users.AddRange(
            new User { Id = ActorUserId, Username = "wip-reporter", PasswordHash = "unused", EmployeeId = ActorEmployeeId },
            new User { Id = ReviewerUserId, Username = "wip-reviewer", PasswordHash = "unused", EmployeeId = ReviewerEmployeeId });
        var operatorRole = db.Roles.SingleOrDefault(role => role.RoleName == "Operator");
        if (operatorRole == null)
        {
            operatorRole = new Role { RoleName = "Operator", IsSystem = true };
            db.Roles.Add(operatorRole);
        }

        var supervisorRole = db.Roles.SingleOrDefault(role => role.RoleName == "Production Supervisor");
        if (supervisorRole == null)
        {
            supervisorRole = new Role { RoleName = "Production Supervisor", IsSystem = true };
            db.Roles.Add(supervisorRole);
        }
        db.SaveChanges();

        db.UserRoles.AddRange(
            new UserRole { UserId = ActorUserId, RoleId = operatorRole.Id },
            new UserRole { UserId = ReviewerUserId, RoleId = supervisorRole.Id });
        var permissionCodes = new[]
        {
            "manufacturing:page",
            "manufacturing:execute",
            "manufacturing:create",
            "manufacturing:approve"
        };
        var permissions = db.Permissions
            .Where(permission => permissionCodes.Contains(permission.PermissionCode))
            .ToDictionary(permission => permission.PermissionCode);
        if (permissions.Count != permissionCodes.Length)
            throw new InvalidOperationException("WIP integration-test permissions have not been seeded.");

        var rolePermissions = new[]
        {
            (RoleId: operatorRole.Id, PermissionCode: "manufacturing:page"),
            (RoleId: operatorRole.Id, PermissionCode: "manufacturing:execute"),
            (RoleId: supervisorRole.Id, PermissionCode: "manufacturing:page"),
            (RoleId: supervisorRole.Id, PermissionCode: "manufacturing:create"),
            (RoleId: supervisorRole.Id, PermissionCode: "manufacturing:approve")
        };
        foreach (var rolePermission in rolePermissions)
        {
            var permissionId = permissions[rolePermission.PermissionCode].Id;
            if (!db.RolePermissions.Any(existing =>
                    existing.RoleId == rolePermission.RoleId && existing.PermissionId == permissionId))
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = rolePermission.RoleId,
                    PermissionId = permissionId
                });
            }
        }
        db.SaveChanges();

        var sku = new Sku
        {
            SkuNumber = "SKU-WIP-001",
            SkuName = "WIP 測試品",
            Unit = "PCS",
            Category = "FG",
            StandardCost = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Skus.Add(sku);
        db.SaveChanges();

        var workCenter = new WorkCenter
        {
            WorkCenterCode = "WC-WIP",
            WorkCenterName = "WIP 工作中心",
            Capacity = 100,
            Efficiency = 90,
            IsActive = true
        };
        db.WorkCenters.Add(workCenter);
        db.SaveChanges();
        db.Equipments.Add(new Equipment
        {
            EquipmentCode = "EQ-WIP-01",
            EquipmentName = "WIP 設備一號",
            Status = "active",
            IsActive = true
        });
        db.SaveChanges();

        var costCenter = new CostCenter
        {
            CostCenterCode = "CC-WIP",
            CostCenterName = "WIP 成本中心",
            BudgetLimit = 10000,
            IsActive = true
        };
        db.CostCenters.Add(costCenter);
        db.SaveChanges();

        var operation = new Operation
        {
            OperationCode = "OP-ASM",
            OperationName = "組裝作業",
            SetupTime = 0.5m,
            RunTime = 2,
            CostPerHour = 50,
            IsActive = true
        };
        db.Operations.Add(operation);
        db.SaveChanges();

        db.WorkCenterCosts.Add(new WorkCenterCost
        {
            Id = Guid.NewGuid(),
            WorkCenterId = workCenter.Id,
            CostCenterId = costCenter.Id,
            LaborRate = 150,
            MachineRate = 200,
            OverheadRate = 50,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        });
        var workOrder = new WorkOrder
        {
            WoNumber = "WO-WIP-001",
            WoType = "regular",
            ProductSkuId = sku.Id,
            PlannedQty = 100,
            ActualQty = 0,
            Status = "pending",
            Priority = "normal",
            PlannedStartDate = DateTime.UtcNow.AddDays(-1),
            PlannedEndDate = DateTime.UtcNow.AddDays(5),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.WorkOrders.Add(workOrder);
        db.SaveChanges();

        db.WorkOrderOperations.Add(new WorkOrderOperation
        {
            WorkOrderId = workOrder.Id,
            OperationSeq = 10,
            OperationId = operation.Id,
            WorkCenterId = workCenter.Id,
            PlannedQty = 100,
            CompletedQty = 0,
            Status = "pending"
        });
        db.SaveChanges();
    }

    private sealed class ApiResponseData
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public WipResult? Data { get; set; }
    }

    private sealed class WipResult
    {
        public Guid Id { get; set; }
        public string? Status { get; set; }
    }
}

internal sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.ContainsKey("X-Test-Anonymous"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var isDefaultTestUser = !Request.Headers.TryGetValue("X-Test-UserId", out var userId);
        var resolvedUserId = isDefaultTestUser
            ? CustomWebApplicationFactory.DefaultTestUserId.ToString()
            : userId.ToString();

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, resolvedUserId) };
        claims.AddRange(Request.Headers["X-Test-Permissions"]
            .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(permission => new Claim("Permission", permission)));
        claims.AddRange(Request.Headers["X-Test-Roles"]
            .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(role => new Claim(ClaimTypes.Role, role)));
        if (isDefaultTestUser && !Request.Headers.ContainsKey("X-Test-Roles"))
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
