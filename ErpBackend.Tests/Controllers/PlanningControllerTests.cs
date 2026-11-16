using ErpBackend.Controllers;
using ErpBackend.DTOs;
using ErpBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ErpBackend.Tests.Controllers;

public class PlanningControllerTests
{
    [Fact]
    public async Task CreatePlan_ReturnsPlanningServiceResult()
    {
        var expected = new PlanningResponse
        {
            Success = true,
            PlanId = "PLAN-TEST"
        };
        var aiPlanningService = new Mock<IAIPlanningService>();
        aiPlanningService
            .Setup(service => service.CreatePlanAsync(It.IsAny<PlanningRequest>()))
            .ReturnsAsync(expected);
        var controller = new PlanningController(
            Mock.Of<IPlanningService>(),
            aiPlanningService.Object,
            Mock.Of<IToolFactory>());

        var result = await controller.CreatePlan(new PlanningRequest
        {
            TaskDescription = "規劃物料需求"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<PlanningResponse>>(ok.Value);
        Assert.Same(expected, response.Data);
    }

    [Fact]
    public async Task CreatePlan_RejectsEmptyTaskDescription()
    {
        var aiPlanningService = new Mock<IAIPlanningService>();
        var controller = new PlanningController(
            Mock.Of<IPlanningService>(),
            aiPlanningService.Object,
            Mock.Of<IToolFactory>());

        var result = await controller.CreatePlan(new PlanningRequest
        {
            TaskDescription = " "
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        aiPlanningService.Verify(
            service => service.CreatePlanAsync(It.IsAny<PlanningRequest>()),
            Times.Never);
    }
}
