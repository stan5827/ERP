using System.Security.Claims;
using ErpBackend.Controllers;
using ErpBackend.DTOs;
using ErpBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ErpBackend.Tests.Controllers;

public sealed class AgentWipControllerTests
{
    [Fact]
    public async Task GetWorkOrderAsync_SetsNoStoreAndDoesNotCallServiceForUnauthenticatedRequest()
    {
        var queryService = new Mock<IAgentWipQueryService>();
        var controller = new AgentWipController(queryService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.GetWorkOrderAsync("WO-TEST", null, CancellationToken.None);

        Assert.Equal("no-store", controller.Response.Headers["Cache-Control"]);
        Assert.False(string.IsNullOrWhiteSpace(controller.Response.Headers["X-Agent-Turn-Id"]));
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
        queryService.Verify(
            service => service.GetWorkOrderAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetWorkOrderAsync_SetsNoStoreForAuthenticatedResponse()
    {
        var queryService = new Mock<IAgentWipQueryService>();
        queryService
            .Setup(service => service.GetWorkOrderAsync(
                "WO-TEST",
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentWipQueryResultDto(AgentWipQueryStatus.FeatureDisabled));

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "test"))
        };
        httpContext.TraceIdentifier = "turn-id";
        var controller = new AgentWipController(queryService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = await controller.GetWorkOrderAsync("WO-TEST", null, CancellationToken.None);

        Assert.Equal("no-store", controller.Response.Headers["Cache-Control"]);
        Assert.True(Guid.TryParse(controller.Response.Headers["X-Agent-Session-Id"], out _));
        Assert.Equal("turn-id", controller.Response.Headers["X-Agent-Turn-Id"]);
        Assert.Equal(
            StatusCodes.Status503ServiceUnavailable,
            Assert.IsType<ObjectResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetWorkOrderAsync_UsesProvidedSessionIdAsCorrelationOnly()
    {
        var sessionId = Guid.NewGuid();
        var queryService = new Mock<IAgentWipQueryService>();
        queryService
            .Setup(service => service.GetWorkOrderAsync(
                "WO-TEST",
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentWipQueryResultDto(AgentWipQueryStatus.FeatureDisabled));

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "test"))
        };
        httpContext.TraceIdentifier = "turn-id";
        httpContext.Request.Headers["X-Agent-Session-Id"] = sessionId.ToString("D");
        var controller = new AgentWipController(queryService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        await controller.GetWorkOrderAsync(
            "WO-TEST",
            httpContext.Request.Headers["X-Agent-Session-Id"],
            CancellationToken.None);

        Assert.Equal(sessionId.ToString("D"), controller.Response.Headers["X-Agent-Session-Id"]);
        Assert.Equal("turn-id", controller.Response.Headers["X-Agent-Turn-Id"]);
        queryService.Verify(
            service => service.GetWorkOrderAsync(
                "WO-TEST",
                It.IsAny<Guid>(),
                "turn-id",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetWorkOrderAsync_RejectsMalformedSessionIdBeforeQueryingFacts()
    {
        var queryService = new Mock<IAgentWipQueryService>();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "test"))
        };
        var controller = new AgentWipController(queryService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = await controller.GetWorkOrderAsync("WO-TEST", "not-a-guid", CancellationToken.None);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            Assert.IsType<BadRequestObjectResult>(result.Result).StatusCode);
        queryService.Verify(
            service => service.GetWorkOrderAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
