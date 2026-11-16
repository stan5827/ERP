using ErpBackend.Controllers;
using ErpBackend.DTOs;
using ErpBackend.Services;
using ErpBackend.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ErpBackend.Tests.Controllers;

public class LlmControllerTests
{
    [Fact]
    public void Complete_IsUnavailableUntilEgressPolicyIsApproved()
    {
        var llmService = new Mock<ILlmService>();
        var controller = new LlmController(llmService.Object);

        var result = controller.Complete();

        var response = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.StatusCode);
        llmService.Verify(
            service => service.CompleteAsync(It.IsAny<LlmRequest>()),
            Times.Never);

        var permission = Assert.Single(
            typeof(LlmController)
                .GetMethod(nameof(LlmController.Complete))!
                .GetCustomAttributes(typeof(PermissionAuthorizeAttribute), inherit: false)
                .Cast<PermissionAuthorizeAttribute>());
        Assert.Equal("ai", permission.Module);
        Assert.Equal("execute", permission.Action);
    }
}
