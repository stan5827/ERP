using Xunit;
using Moq;
using ErpBackend.Services;
using ErpBackend.Data;
using Microsoft.Extensions.Logging;

namespace ErpBackend.Tests.Services;

public class AIPlanningServiceTests
{
    private readonly Mock<ErpGeneralDbContext> _mockDb;
    private readonly Mock<ILogger<AIPlanningService>> _mockLogger;
    private readonly Mock<AgentMetricsService> _mockMetrics;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

    public AIPlanningServiceTests()
    {
        _mockDb = new Mock<ErpGeneralDbContext>(new Microsoft.EntityFrameworkCore.DbContextOptions<ErpGeneralDbContext>());
        _mockLogger = new Mock<ILogger<AIPlanningService>>();
        _mockMetrics = new Mock<AgentMetricsService>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
    }

    [Fact]
    public async Task CreatePlanAsync_WithMrpTask_ReturnsMrpPlan()
    {
        var service = new AIPlanningService(
            _mockDb.Object,
            _mockLogger.Object,
            _mockMetrics.Object,
            _mockHttpClientFactory.Object);

        var request = new PlanningRequest
        {
            TaskDescription = "執行 MRP 物料需求規劃",
            Context = "需要規劃未來3個月的物料需求"
        };

        var result = await service.CreatePlanAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.PlanId);
        Assert.Contains("mrp", result.Tasks.First().Id);
    }

    [Fact]
    public async Task CreatePlanAsync_WithSchedulingTask_ReturnsSchedulingPlan()
    {
        var service = new AIPlanningService(
            _mockDb.Object,
            _mockLogger.Object,
            _mockMetrics.Object,
            _mockHttpClientFactory.Object);

        var request = new PlanningRequest
        {
            TaskDescription = "執行生產排程",
            Context = "優化產能配置"
        };

        var result = await service.CreatePlanAsync(request);

        Assert.True(result.Success);
        Assert.Contains("sch", result.Tasks.First().Id);
    }

    [Fact]
    public async Task CreatePlanAsync_WithInventoryTask_ReturnsInventoryPlan()
    {
        var service = new AIPlanningService(
            _mockDb.Object,
            _mockLogger.Object,
            _mockMetrics.Object,
            _mockHttpClientFactory.Object);

        var request = new PlanningRequest
        {
            TaskDescription = "分析庫存狀況"
        };

        var result = await service.CreatePlanAsync(request);

        Assert.True(result.Success);
        Assert.Contains("inv", result.Tasks.First().Id);
    }

    [Fact]
    public async Task CreatePlanAsync_WithUnknownTask_ReturnsGeneralPlan()
    {
        var service = new AIPlanningService(
            _mockDb.Object,
            _mockLogger.Object,
            _mockMetrics.Object,
            _mockHttpClientFactory.Object);

        var request = new PlanningRequest
        {
            TaskDescription = "請帮我處理一些事情"
        };

        var result = await service.CreatePlanAsync(request);

        Assert.True(result.Success);
        Assert.Contains("gen", result.Tasks.First().Id);
    }

    [Fact]
    public async Task ExecutePlanAsync_WithValidPlan_ReturnsSuccess()
    {
        var service = new AIPlanningService(
            _mockDb.Object,
            _mockLogger.Object,
            _mockMetrics.Object,
            _mockHttpClientFactory.Object);

        var request = new ExecutionRequest
        {
            PlanId = "PLAN-20260101120000"
        };

        var result = await service.ExecutePlanAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.ExecutionId);
    }

    [Fact]
    public async Task ReviewResultAsync_WithFailedTasks_ReturnsIssues()
    {
        var service = new AIPlanningService(
            _mockDb.Object,
            _mockLogger.Object,
            _mockMetrics.Object,
            _mockHttpClientFactory.Object);

        var execId = "EXEC-20260101120000";
        await service.CreatePlanAsync(new PlanningRequest { TaskDescription = "test" });
        await service.ExecutePlanAsync(new ExecutionRequest { PlanId = "test" });

        var request = new ReviewRequest
        {
            ExecutionId = execId
        };

        var result = await service.ReviewResultAsync(request);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ValidateKpiAsync_ReturnsKpiResult()
    {
        var service = new AIPlanningService(
            _mockDb.Object,
            _mockLogger.Object,
            _mockMetrics.Object,
            _mockHttpClientFactory.Object);

        var result = await service.ValidateKpiAsync();

        Assert.NotNull(result);
        Assert.True(result.TargetMet);
    }

    [Fact]
    public void PlanTask_Priority_DefaultIs50()
    {
        var task = new PlanTask();
        Assert.Equal(50, task.Priority);
    }

    [Fact]
    public void ExecutionRequest_ParallelExecution_DefaultFalse()
    {
        var request = new ExecutionRequest();
        Assert.False(request.ParallelExecution);
    }

    [Fact]
    public void ReviewRequest_AutoApproval_DefaultTrue()
    {
        var request = new ReviewRequest();
        Assert.True(request.AutoApproval);
    }
}
