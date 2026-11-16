using System.Security.Claims;
using ErpBackend.Controllers;
using ErpBackend.DTOs;
using ErpBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class SupplierPortalInvitationControllerTests
{
    [Fact]
    public async Task InvitePortalUser_ReturnsActionableConflictForUnlinkedExistingIdentity()
    {
        var invitations = new Mock<ISupplierPortalInvitationService>();
        invitations
            .Setup(service => service.InviteAsync(
                It.IsAny<Guid>(),
                It.IsAny<CreateSupplierPortalInvitationRequestDto>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeycloakSupplierIdentityException("ExistingIdentityNotLinked"));

        var controller = new SuppliersController(
            Mock.Of<ISupplierService>(),
            Mock.Of<ISupplierRatingService>(),
            invitations.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                    "test"))
            }
        };

        var result = await controller.InvitePortalUser(
            Guid.NewGuid(),
            new CreateSupplierPortalInvitationRequestDto
            {
                Email = "contact@example.com",
                ContactName = "Test Contact",
                Role = "sales"
            },
            CancellationToken.None);

        var response = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, response.StatusCode);
        var body = Assert.IsType<ApiResponse>(response.Value);
        Assert.Contains("不會自動連結既有帳號", body.Message);
    }
}
