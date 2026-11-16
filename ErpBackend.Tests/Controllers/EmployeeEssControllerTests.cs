using System.Security.Claims;
using ErpBackend.Controllers;
using ErpBackend.DTOs;
using ErpBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ErpBackend.Tests.Controllers;

public sealed class EmployeeEssControllerTests
{
    [Fact]
    public async Task GetPayroll_RequestsOnlyPaidRecords()
    {
        var employeeId = Guid.NewGuid();
        var payrollService = new Mock<IPayrollService>();
        payrollService
            .Setup(service => service.GetPayrollRecordsAsync(
                employeeId.ToString(),
                null,
                null,
                "paid",
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<PayrollDto>(), 0));
        var controller = CreateController(
            new Mock<IAttendanceRecordService>(),
            employeeId,
            payrollService: payrollService);

        var result = await controller.GetPayroll(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        payrollService.Verify(
            service => service.GetPayrollRecordsAsync(
                employeeId.ToString(),
                null,
                null,
                "paid",
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ClockIn_UsesEmployeeIdFromToken()
    {
        var employeeId = Guid.NewGuid();
        var attendanceService = new Mock<IAttendanceRecordService>();
        attendanceService
            .Setup(service => service.ClockInForEmployeeAsync(
                employeeId, 25.03, 121.56, It.IsAny<CancellationToken>()))
            .ReturnsAsync("ok");
        var controller = CreateController(attendanceService, employeeId);

        var result = await controller.ClockIn(
            new EmployeeEssClockRequestDto { Latitude = 25.03, Longitude = 121.56 },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        attendanceService.Verify(
            service => service.ClockInForEmployeeAsync(
                employeeId, 25.03, 121.56, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ClockIn_RejectsRequestWithoutEmployeeClaim()
    {
        var attendanceService = new Mock<IAttendanceRecordService>();
        var controller = CreateController(attendanceService);

        var result = await controller.ClockIn(
            new EmployeeEssClockRequestDto { Latitude = 25.03, Longitude = 121.56 },
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
        attendanceService.Verify(
            service => service.ClockInForEmployeeAsync(
                It.IsAny<Guid>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateLeaveRequest_UsesEmployeeIdFromToken()
    {
        var employeeId = Guid.NewGuid();
        var leaveService = new Mock<ILeaveRequestService>();
        leaveService
            .Setup(service => service.CreateAsync(It.Is<CreateLeaveRequestRequest>(
                request => request.EmployeeId == employeeId.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new LeaveRequestDto
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                ApplicationNo = "LR-ESS-001",
                LeaveType = "annual",
                Status = "pending"
            }, "ok"));
        var controller = CreateController(new Mock<IAttendanceRecordService>(), employeeId, leaveService);

        var result = await controller.CreateLeaveRequest(new EmployeeEssCreateLeaveRequestDto
        {
            LeaveType = "annual",
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 2)
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        leaveService.Verify(
            service => service.CreateAsync(It.Is<CreateLeaveRequestRequest>(
                request => request.EmployeeId == employeeId.ToString()), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateLeaveRequest_WhenScheduleIsNotConfigured_ReturnsUnavailable()
    {
        var employeeId = Guid.NewGuid();
        var leaveService = new Mock<ILeaveRequestService>();
        leaveService
            .Setup(service => service.CreateAsync(
                It.IsAny<CreateLeaveRequestRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((LeaveRequestDto?)null, "schedule_not_configured"));
        var controller = CreateController(
            new Mock<IAttendanceRecordService>(),
            employeeId,
            leaveService);

        var result = await controller.CreateLeaveRequest(new EmployeeEssCreateLeaveRequestDto
        {
            LeaveType = "annual",
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 2)
        }, CancellationToken.None);

        Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, ((ObjectResult)result.Result!).StatusCode);
    }

    [Fact]
    public async Task CreateLeaveRequest_AlwaysUsesTokenEmployeeId()
    {
        var tokenEmployeeId = Guid.NewGuid();
        var leaveService = new Mock<ILeaveRequestService>();
        leaveService
            .Setup(service => service.CreateAsync(
                It.Is<CreateLeaveRequestRequest>(request =>
                    request.EmployeeId == tokenEmployeeId.ToString()),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new LeaveRequestDto
            {
                Id = Guid.NewGuid(),
                EmployeeId = tokenEmployeeId,
                Status = "pending"
            }, "ok"));
        var controller = CreateController(
            new Mock<IAttendanceRecordService>(),
            tokenEmployeeId,
            leaveService);

        var result = await controller.CreateLeaveRequest(new EmployeeEssCreateLeaveRequestDto
        {
            LeaveType = "annual",
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 1)
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        leaveService.Verify(
            service => service.CreateAsync(
                It.Is<CreateLeaveRequestRequest>(request =>
                    request.EmployeeId == tokenEmployeeId.ToString()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static EmployeeEssController CreateController(
        Mock<IAttendanceRecordService> attendanceService,
        Guid? employeeId = null,
        Mock<ILeaveRequestService>? leaveService = null,
        Mock<IPayrollService>? payrollService = null)
    {
        var claims = employeeId.HasValue
            ? new[] { new Claim("employee_id", employeeId.Value.ToString()) }
            : Array.Empty<Claim>();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        var controller = new EmployeeEssController(
            (payrollService ?? new Mock<IPayrollService>()).Object,
            attendanceService.Object,
            (leaveService ?? new Mock<ILeaveRequestService>()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        return controller;
    }
}
