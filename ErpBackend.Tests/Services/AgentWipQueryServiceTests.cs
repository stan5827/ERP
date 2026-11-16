using ErpBackend.Data;
using ErpBackend.Configs;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.HR;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.System;
using ErpBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace ErpBackend.Tests.Services;

public class AgentWipQueryServiceTests
{
    [Fact]
    public async Task GetWorkOrderAsync_AggregatesApprovedFinalOperationReports_AndWritesPairedAudit()
    {
        await using var fixture = await Fixture.CreateAsync();

        var result = await fixture.QueryService.GetWorkOrderAsync(
            fixture.WorkOrder.WoNumber,
            fixture.User.Id,
            "trace-success",
            "127.0.0.1",
            "agent-wip-tests");

        Assert.Equal(AgentWipQueryStatus.Success, result.Status);
        var data = Assert.IsType<AgentWipWorkOrderDto>(result.Data);
        Assert.Equal("WO-AGENT-001", data.WorkOrderNumber.Value);
        Assert.Equal("KG", data.ProductionUnit.Value);
        Assert.Equal(new[] { 10, 30 }, data.AuthorizedOperations.Select(item => item.OperationSequence.Value));

        var finalOperation = Assert.IsType<AgentWipFinalOperationDto>(data.FinalOperation);
        Assert.Equal(50m, finalOperation.PlannedQuantity.Value);
        Assert.Equal(40m, finalOperation.GoodQuantity.Value);
        Assert.Equal(7m, finalOperation.DefectQuantity.Value);
        Assert.Equal(7m / 47m * 100m, finalOperation.DefectRatePercent.Value);
        Assert.Equal(10m, finalOperation.RemainingQuantity.Value);
        Assert.Equal("KG", finalOperation.GoodQuantity.Unit);
        Assert.Equal("40", finalOperation.GoodQuantity.DisplayValue);
        Assert.Equal("internal", finalOperation.GoodQuantity.DataClassification);
        Assert.Equal("WorkOrderOperations", finalOperation.PlannedQuantity.SourceTable);
        Assert.Equal("WipReports", finalOperation.GoodQuantity.SourceTable);
        Assert.True(AgentWipFactAlignmentValidator.TryValidateFacts(data, out _));

        var disclosure = await fixture.Db.DisclosureLogs.SingleAsync();
        var audit = await fixture.Db.AuditLogs.SingleAsync();
        Assert.Equal("trace-success", disclosure.TraceId);
        Assert.Equal("disclosed", disclosure.Outcome);
        Assert.Contains("finalOperation.goodQuantity", disclosure.DisclosedFields);
        Assert.DoesNotContain("40", disclosure.DisclosedFields);
        Assert.Equal(disclosure.CreatedAt, audit.CreatedAt);
        Assert.Equal(disclosure.ActorUserId, audit.UserId);
        Assert.Equal("agent_wip.read:disclosed", audit.Action);
        Assert.Equal("AgentWipQuery", audit.EntityType);
        Assert.Contains("policy=wip-scope-v1", audit.Details);
    }

    [Fact]
    public async Task GetWorkOrderAsync_DoesNotDiscloseFinalOperationWhenItIsOutsideScope()
    {
        await using var fixture = await Fixture.CreateAsync(finalOperationInScope: false);

        var result = await fixture.QueryService.GetWorkOrderAsync(
            fixture.WorkOrder.WoNumber,
            fixture.User.Id,
            "trace-out-of-scope",
            null,
            null);

        Assert.Equal(AgentWipQueryStatus.Success, result.Status);
        var data = Assert.IsType<AgentWipWorkOrderDto>(result.Data);
        Assert.Null(data.FinalOperation);
        Assert.Equal(new[] { 10 }, data.AuthorizedOperations.Select(item => item.OperationSequence.Value));

        var disclosure = await fixture.Db.DisclosureLogs.SingleAsync();
        Assert.Contains("final_operation_disclosed=False", disclosure.DecisionReason);
        Assert.DoesNotContain("finalOperation.goodQuantity", disclosure.DisclosedFields);
    }

    [Fact]
    public async Task GetWorkOrderAsync_UsesNotApplicableForZeroDefectRateDenominator()
    {
        await using var fixture = await Fixture.CreateAsync(includeReports: false);

        var result = await fixture.QueryService.GetWorkOrderAsync(
            fixture.WorkOrder.WoNumber,
            fixture.User.Id,
            "trace-zero-denominator",
            null,
            null);

        var finalOperation = Assert.IsType<AgentWipFinalOperationDto>(result.Data?.FinalOperation);
        Assert.Equal(0m, finalOperation.GoodQuantity.Value);
        Assert.Equal(0m, finalOperation.DefectQuantity.Value);
        Assert.Null(finalOperation.DefectRatePercent.Value);
        Assert.Equal("zero_denominator", finalOperation.DefectRatePercent.NotApplicableReason);
        Assert.Equal(50m, finalOperation.RemainingQuantity.Value);
        Assert.True(AgentWipFactAlignmentValidator.TryValidateFacts(
            Assert.IsType<AgentWipWorkOrderDto>(result.Data),
            out _));
    }

    [Fact]
    public async Task FactAlignmentValidator_RequiresFactIdForNotApplicableNumericFact()
    {
        await using var fixture = await Fixture.CreateAsync(includeReports: false);
        var result = await fixture.QueryService.GetWorkOrderAsync(
            fixture.WorkOrder.WoNumber,
            fixture.User.Id,
            "trace-n-a-fact-id",
            null,
            null);
        var facts = Assert.IsType<AgentWipWorkOrderDto>(result.Data);
        var finalOperation = Assert.IsType<AgentWipFinalOperationDto>(facts.FinalOperation);

        Assert.False(AgentWipFactAlignmentValidator.TryValidateFacts(
            facts with
            {
                FinalOperation = finalOperation with
                {
                    DefectRatePercent = finalOperation.DefectRatePercent with { FactId = string.Empty }
                }
            },
            out var failureReason));
        Assert.Equal("numeric_fact_id_missing", failureReason);
    }

    [Fact]
    public async Task AnswerClaims_MustMatchAuthorizedFactsAndCurrentTurn()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.QueryService.GetWorkOrderAsync(
            fixture.WorkOrder.WoNumber,
            fixture.User.Id,
            "trace-answer-claims",
            null,
            null);
        var facts = Assert.IsType<AgentWipWorkOrderDto>(result.Data);
        var goodQuantity = Assert.IsType<AgentWipFinalOperationDto>(facts.FinalOperation).GoodQuantity;

        var validAnswer = new AgentWipAnswerDraftDto(
            "session-current",
            "turn-current",
            [new AgentWipNumericClaimDto(goodQuantity.FactId, goodQuantity.Value, goodQuantity.Unit)]);
        Assert.True(AgentWipFactAlignmentValidator.TryValidateAnswerClaims(
            facts,
            "session-current",
            "turn-current",
            validAnswer,
            out _));

        Assert.False(AgentWipFactAlignmentValidator.TryValidateAnswerClaims(
            facts,
            "session-current",
            "turn-current",
            validAnswer with { TurnId = "turn-previous" },
            out var turnFailure));
        Assert.Equal("answer_context_mismatch", turnFailure);

        Assert.False(AgentWipFactAlignmentValidator.TryValidateAnswerClaims(
            facts,
            "session-current",
            "turn-current",
            validAnswer with
            {
                NumericClaims =
                [
                    new AgentWipNumericClaimDto(
                        goodQuantity.FactId,
                        goodQuantity.Value + 1m,
                        goodQuantity.Unit)
                ]
            },
            out var valueFailure));
        Assert.Equal("answer_numeric_claim_mismatch", valueFailure);

        Assert.False(AgentWipFactAlignmentValidator.TryValidateAnswerClaims(
            facts,
            "session-current",
            "turn-current",
            validAnswer with
            {
                NumericClaims =
                [
                    new AgentWipNumericClaimDto(
                        goodQuantity.FactId,
                        goodQuantity.Value,
                        "EA")
                ]
            },
            out var unitFailure));
        Assert.Equal("answer_numeric_claim_mismatch", unitFailure);
    }

    [Fact]
    public async Task FactAlignmentValidator_RejectsInconsistentUnitsAndDerivedValues()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.QueryService.GetWorkOrderAsync(
            fixture.WorkOrder.WoNumber,
            fixture.User.Id,
            "trace-fact-alignment",
            null,
            null);
        var facts = Assert.IsType<AgentWipWorkOrderDto>(result.Data);
        var finalOperation = Assert.IsType<AgentWipFinalOperationDto>(facts.FinalOperation);

        Assert.False(AgentWipFactAlignmentValidator.TryValidateFacts(
            facts with
            {
                FinalOperation = finalOperation with
                {
                    GoodQuantity = finalOperation.GoodQuantity with { Unit = "EA" }
                }
            },
            out var unitFailure));
        Assert.Equal("numeric_fact_unit_mismatch", unitFailure);

        Assert.False(AgentWipFactAlignmentValidator.TryValidateFacts(
            facts with
            {
                FinalOperation = finalOperation with
                {
                    RemainingQuantity = finalOperation.RemainingQuantity with { Value = 9m }
                }
            },
            out var remainingFailure));
        Assert.Equal("numeric_fact_remaining_quantity_invalid", remainingFailure);
    }

    [Fact]
    public async Task GetWorkOrderAsync_FailsClosedWhenFactUnitIsMissing()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.WorkOrder.ProductionUomCode = string.Empty;
        await fixture.Db.SaveChangesAsync();

        var result = await fixture.QueryService.GetWorkOrderAsync(
            fixture.WorkOrder.WoNumber,
            fixture.User.Id,
            "trace-unit-missing",
            null,
            null);

        Assert.Equal(AgentWipQueryStatus.DataQualityBlocked, result.Status);
        Assert.Null(result.Data);
        var disclosure = await fixture.Db.DisclosureLogs.SingleAsync();
        Assert.Equal("data_quality_blocked", disclosure.Outcome);
        Assert.Equal("production_unit_missing", disclosure.DecisionReason);
        Assert.Empty(disclosure.DisclosedFields ?? string.Empty);
    }

    [Fact]
    public async Task GetWorkOrderAsync_FailsClosedAndAuditsWhenScopeMappingIsMissing()
    {
        await using var fixture = await Fixture.CreateAsync(includeCostCenter: false);

        var result = await fixture.QueryService.GetWorkOrderAsync(
            fixture.WorkOrder.WoNumber,
            fixture.User.Id,
            "trace-no-scope",
            null,
            null);

        Assert.Equal(AgentWipQueryStatus.ScopeUnavailable, result.Status);
        Assert.Null(result.Data);
        Assert.Empty(await fixture.Db.AuditLogs.Where(log => log.Action.Contains("disclosed")).ToListAsync());
        var disclosure = await fixture.Db.DisclosureLogs.SingleAsync();
        Assert.Equal("scope_blocked", disclosure.Outcome);
        Assert.Equal("active_work_center_mapping_missing", disclosure.DecisionReason);
        Assert.Empty(disclosure.DisclosedFields ?? string.Empty);
    }

    [Fact]
    public async Task GetWorkOrderAsync_RejectsExpiredCostCenterScope()
    {
        await using var fixture = await Fixture.CreateAsync(costCenterExpired: true);

        var result = await fixture.QueryService.GetWorkOrderAsync(
            fixture.WorkOrder.WoNumber,
            fixture.User.Id,
            "trace-expired-scope",
            null,
            null);

        Assert.Equal(AgentWipQueryStatus.ScopeUnavailable, result.Status);
        Assert.Null(result.Data);
        Assert.Equal(
            "active_work_center_mapping_missing",
            (await fixture.Db.DisclosureLogs.SingleAsync()).DecisionReason);
    }

    [Fact]
    public async Task GetWorkOrderAsync_DoesNotQueryDataWhenFeatureFlagIsOff()
    {
        await using var fixture = await Fixture.CreateAsync(agentEnabled: false);

        var result = await fixture.QueryService.GetWorkOrderAsync(
            fixture.WorkOrder.WoNumber,
            fixture.User.Id,
            "trace-feature-disabled",
            null,
            null);

        Assert.Equal(AgentWipQueryStatus.FeatureDisabled, result.Status);
        Assert.Null(result.Data);
        var disclosure = await fixture.Db.DisclosureLogs.SingleAsync();
        Assert.Equal("feature_disabled", disclosure.Outcome);
        Assert.Equal("server_feature_flag_disabled", disclosure.DecisionReason);
    }

    [Fact]
    public async Task GetWorkOrderAsync_DoesNotQueryDataForUserOutsideSeedCohort()
    {
        await using var fixture = await Fixture.CreateAsync(seedUser: false);

        var result = await fixture.QueryService.GetWorkOrderAsync(
            fixture.WorkOrder.WoNumber,
            fixture.User.Id,
            "trace-seed-blocked",
            null,
            null);

        Assert.Equal(AgentWipQueryStatus.NotInSeedCohort, result.Status);
        Assert.Null(result.Data);
        var disclosure = await fixture.Db.DisclosureLogs.SingleAsync();
        Assert.Equal("seed_cohort_blocked", disclosure.Outcome);
    }

    [Fact]
    public async Task GetWorkOrderAsync_DoesNotReturnFactsWhenPairedAuditWriteFails()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Db.FailDisclosureAuditWrites = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.QueryService.GetWorkOrderAsync(
                fixture.WorkOrder.WoNumber,
                fixture.User.Id,
                "trace-audit-failure",
                null,
                null));

        Assert.Empty(await fixture.Db.DisclosureLogs.ToListAsync());
        Assert.Empty(await fixture.Db.AuditLogs.ToListAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(
            TestDbContext db,
            User user,
            WorkOrder workOrder,
            AgentWipQueryService queryService)
        {
            Db = db;
            User = user;
            WorkOrder = workOrder;
            QueryService = queryService;
        }

        public TestDbContext Db { get; }
        public User User { get; }
        public WorkOrder WorkOrder { get; }
        public AgentWipQueryService QueryService { get; }

        public static async Task<Fixture> CreateAsync(
            bool finalOperationInScope = true,
            bool includeCostCenter = true,
            bool includeReports = true,
            bool agentEnabled = true,
            bool seedUser = true,
            bool costCenterExpired = false)
        {
            var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var db = new TestDbContext(options);
            var department = new Department
            {
                DeptCode = "PROD",
                DeptName = "Production"
            };
            var user = new User
            {
                Username = "agent-wip-user",
                PasswordHash = "test-hash"
            };
            var employee = new Employee
            {
                EmployeeNo = "E-AGENT-01",
                ChineseName = "測試人員",
                DepartmentId = department.Id
            };
            user.EmployeeId = employee.Id;

            var authorizedWorkCenter = new WorkCenter
            {
                WorkCenterCode = "WC-01",
                WorkCenterName = "Authorized center"
            };
            var outOfScopeWorkCenter = new WorkCenter
            {
                WorkCenterCode = "WC-02",
                WorkCenterName = "Out-of-scope center"
            };
            var costCenter = new CostCenter
            {
                CostCenterCode = "CC-01",
                CostCenterName = "Production cost center",
                DepartmentId = department.Id,
                WorkCenterId = authorizedWorkCenter.Id,
                EffectiveFrom = DateTime.UtcNow.AddDays(-1),
                EffectiveTo = costCenterExpired ? DateTime.UtcNow.AddDays(-1) : null
            };
            if (includeCostCenter)
                db.CostCenters.Add(costCenter);

            var product = new Sku
            {
                SkuNumber = "SKU-AGENT-01",
                SkuName = "Test product"
            };
            var workOrder = new WorkOrder
            {
                WoNumber = "WO-AGENT-001",
                ProductSkuId = product.Id,
                ProductionUomCode = "KG",
                Status = "in_progress",
                UpdatedAt = DateTime.UtcNow.AddHours(-1)
            };
            var firstOperation = new WorkOrderOperation
            {
                WorkOrderId = workOrder.Id,
                OperationSeq = 10,
                WorkCenterId = authorizedWorkCenter.Id,
                PlannedQty = 100m,
                Status = "completed"
            };
            var middleOperation = new WorkOrderOperation
            {
                WorkOrderId = workOrder.Id,
                OperationSeq = 20,
                WorkCenterId = outOfScopeWorkCenter.Id,
                PlannedQty = 100m,
                Status = "completed"
            };
            var finalOperation = new WorkOrderOperation
            {
                WorkOrderId = workOrder.Id,
                OperationSeq = 30,
                WorkCenterId = finalOperationInScope
                    ? authorizedWorkCenter.Id
                    : outOfScopeWorkCenter.Id,
                PlannedQty = 50m,
                Status = "in_progress"
            };

            db.AddRange(
                department,
                employee,
                user,
                authorizedWorkCenter,
                outOfScopeWorkCenter,
                product,
                workOrder,
                firstOperation,
                middleOperation,
                finalOperation);

            if (includeReports)
            {
                var reviewedAt = DateTime.UtcNow.AddMinutes(-10);
                db.WipReports.AddRange(
                    new WipReport
                    {
                        ReportNumber = "WIP-APPROVED-1",
                        WorkOrderId = workOrder.Id,
                        OperationSeq = 30,
                        EmployeeId = employee.Id,
                        CompletedQty = 30m,
                        DefectQty = 5m,
                        ScrapQty = 80m,
                        Status = "approved",
                        ReviewedAt = reviewedAt
                    },
                    new WipReport
                    {
                        ReportNumber = "WIP-APPROVED-2",
                        WorkOrderId = workOrder.Id,
                        OperationSeq = 30,
                        EmployeeId = employee.Id,
                        CompletedQty = 10m,
                        DefectQty = 2m,
                        Status = "approved",
                        ReviewedAt = reviewedAt.AddMinutes(1)
                    },
                    new WipReport
                    {
                        ReportNumber = "WIP-SUBMITTED",
                        WorkOrderId = workOrder.Id,
                        OperationSeq = 30,
                        EmployeeId = employee.Id,
                        CompletedQty = 90m,
                        DefectQty = 90m,
                        Status = "submitted"
                    },
                    new WipReport
                    {
                        ReportNumber = "WIP-EARLIER-OPERATION",
                        WorkOrderId = workOrder.Id,
                        OperationSeq = 10,
                        EmployeeId = employee.Id,
                        CompletedQty = 99m,
                        DefectQty = 99m,
                        Status = "approved",
                        ReviewedAt = reviewedAt
                    });
            }

            await db.SaveChangesAsync();

            var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            var auditService = new AgentDisclosureAuditService(db, httpContextAccessor);
            var accessOptions = Options.Create(new AgentWipAccessOptions
            {
                Enabled = agentEnabled,
                SeedUserIds = seedUser ? [user.Id] : []
            });
            return new Fixture(
                db,
                user,
                workOrder,
                new AgentWipQueryService(db, auditService, accessOptions));
        }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class TestDbContext(DbContextOptions<ErpGeneralDbContext> options)
        : ErpGeneralDbContext(options)
    {
        public bool FailDisclosureAuditWrites { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (FailDisclosureAuditWrites
                && ChangeTracker.Entries<DisclosureLog>()
                    .Any(entry => entry.State == EntityState.Added))
                throw new InvalidOperationException("Disclosure audit persistence failed.");

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
