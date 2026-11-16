using System.Security.Claims;
using ErpBackend.Controllers;
using ErpBackend.DTOs;
using ErpBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ErpBackend.Tests.Services;

public sealed class SupplierApprovalControllerTests
{
    [Fact]
    public async Task ApproveSupplier_UsesAuthenticatedEmployeeClaim()
    {
        var supplierId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var supplierService = new Mock<ISupplierService>();
        SupplierApprovalRequest? forwardedRequest = null;
        supplierService
            .Setup(service => service.ApproveSupplierAsync(supplierId, It.IsAny<SupplierApprovalRequest>()))
            .Callback<Guid, SupplierApprovalRequest>((_, request) => forwardedRequest = request)
            .Returns(Task.CompletedTask);
        var controller = CreateController(
            supplierService,
            new Claim("employee_id", employeeId.ToString()));

        await controller.ApproveSupplier(supplierId, new SupplierApprovalRequest
        {
            EmployeeId = Guid.NewGuid(),
            Comments = "Approved"
        });

        Assert.NotNull(forwardedRequest);
        Assert.Equal(employeeId, forwardedRequest.EmployeeId);
        Assert.Equal("Approved", forwardedRequest.Comments);
    }

    [Fact]
    public async Task ApproveSupplier_AllowsAdminWithoutEmployeeLink()
    {
        var supplierId = Guid.NewGuid();
        var supplierService = new Mock<ISupplierService>();
        SupplierApprovalRequest? forwardedRequest = null;
        supplierService
            .Setup(service => service.ApproveSupplierAsync(supplierId, It.IsAny<SupplierApprovalRequest>()))
            .Callback<Guid, SupplierApprovalRequest>((_, request) => forwardedRequest = request)
            .Returns(Task.CompletedTask);
        var controller = CreateController(
            supplierService,
            new Claim(ClaimTypes.Role, "Admin"));

        var result = await controller.ApproveSupplier(supplierId, new SupplierApprovalRequest
        {
            EmployeeId = Guid.NewGuid()
        });

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(forwardedRequest);
        Assert.Null(forwardedRequest.EmployeeId);
    }

    private static SuppliersController CreateController(Mock<ISupplierService> supplierService, params Claim[] claims)
    {
        var controller = new SuppliersController(
            supplierService.Object,
            Mock.Of<ISupplierRatingService>(),
            Mock.Of<ISupplierPortalInvitationService>());
        var identityClaims = new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }
            .Concat(claims);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(identityClaims, "test"))
            }
        };
        return controller;
    }
}
