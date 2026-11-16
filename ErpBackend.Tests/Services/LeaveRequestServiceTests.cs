using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class LeaveRequestServiceTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ErpGeneralDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrderedByDate()
    {
        using var db = CreateInMemoryDbContext();
        var emp = new Employee { ChineseName = "Test", EmployeeNo = "E001" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        db.LeaveRequests.AddRange(
            new LeaveRequest { EmployeeId = emp.Id, ApplicationNo = "LR-001", LeaveType = "annual", ApplicationDate = DateTime.UtcNow.AddDays(-1), Status = "pending" },
            new LeaveRequest { EmployeeId = emp.Id, ApplicationNo = "LR-002", LeaveType = "sick", ApplicationDate = DateTime.UtcNow, Status = "approved" });
        await db.SaveChangesAsync();

        var service = new LeaveRequestService(db);
        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("LR-002", result[0].ApplicationNo);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByStatus()
    {
        using var db = CreateInMemoryDbContext();
        var emp = new Employee { ChineseName = "Test", EmployeeNo = "E001" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        db.LeaveRequests.AddRange(
            new LeaveRequest { EmployeeId = emp.Id, ApplicationNo = "LR-001", LeaveType = "annual", Status = "pending" },
            new LeaveRequest { EmployeeId = emp.Id, ApplicationNo = "LR-002", LeaveType = "annual", Status = "approved" });
        await db.SaveChangesAsync();

        var service = new LeaveRequestService(db);
        var result = await service.GetAllAsync(status: "pending");

        Assert.Single(result);
        Assert.Equal("LR-001", result[0].ApplicationNo);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var emp = new Employee { ChineseName = "Alice", EmployeeNo = "E001" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var lr = new LeaveRequest { EmployeeId = emp.Id, ApplicationNo = "LR-001", LeaveType = "annual", Status = "pending" };
        db.LeaveRequests.Add(lr);
        await db.SaveChangesAsync();

        var service = new LeaveRequestService(db);
        var dto = await service.GetByIdAsync(lr.Id);

        Assert.NotNull(dto);
        Assert.Equal("Alice", dto!.EmployeeName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new LeaveRequestService(db);
        Assert.Null(await service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetLeaveBalancesAsync_FiltersByEmployeeAndYear()
    {
        using var db = CreateInMemoryDbContext();
        var employee = new Employee { ChineseName = "Test", EmployeeNo = "E001" };
        var otherEmployee = new Employee { ChineseName = "Other", EmployeeNo = "E002" };
        db.Employees.AddRange(employee, otherEmployee);
        db.LeaveBalances.AddRange(
            new LeaveBalance
            {
                EmployeeId = employee.Id,
                LeaveType = "annual",
                Year = 2026,
                TotalEntitled = 15,
                Used = 3,
                Remaining = 12
            },
            new LeaveBalance
            {
                EmployeeId = employee.Id,
                LeaveType = "annual",
                Year = 2025,
                TotalEntitled = 15,
                Used = 10,
                Remaining = 5
            },
            new LeaveBalance
            {
                EmployeeId = otherEmployee.Id,
                LeaveType = "annual",
                Year = 2026,
                TotalEntitled = 15,
                Used = 1,
                Remaining = 14
            });
        await db.SaveChangesAsync();
        var service = new LeaveRequestService(db);

        var balances = await service.GetLeaveBalancesAsync(employee.Id, 2026);

        var balance = Assert.Single(balances);
        Assert.Equal("annual", balance.LeaveType);
        Assert.Equal(15, balance.TotalDays);
        Assert.Equal(3, balance.UsedDays);
        Assert.Equal(12, balance.RemainingDays);
    }

    [Fact]
    public async Task CreateAsync_WhenScheduleIsNotConfigured_ReturnsUnavailableWithoutPersisting()
    {
        using var db = CreateInMemoryDbContext();
        var emp = new Employee { ChineseName = "Bob", EmployeeNo = "E001" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var service = new LeaveRequestService(db);
        var req = new CreateLeaveRequestRequest
        {
            EmployeeId = emp.Id.ToString(),
            LeaveType = "annual",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var (dto, status) = await service.CreateAsync(req);

        Assert.Null(dto);
        Assert.Equal("schedule_not_configured", status);
        Assert.Empty(await db.LeaveRequests.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_ReturnsInvalid_WhenEmployeeIdInvalid()
    {
        using var db = CreateInMemoryDbContext();
        var service = new LeaveRequestService(db);

        var req = new CreateLeaveRequestRequest
        {
            EmployeeId = "invalid-guid",
            LeaveType = "annual",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        var (dto, status) = await service.CreateAsync(req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);
    }

    [Fact]
    public async Task ApproveAsync_WhenWorkflowIsNotConfigured_PreservesPendingRequest()
    {
        using var db = CreateInMemoryDbContext();
        var emp = new Employee { ChineseName = "Test", EmployeeNo = "E001" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var lr = new LeaveRequest { EmployeeId = emp.Id, ApplicationNo = "LR-001", LeaveType = "annual", Status = "pending" };
        db.LeaveRequests.Add(lr);
        await db.SaveChangesAsync();

        var service = new LeaveRequestService(db);
        var req = new ApproveLeaveRequestRequest { ApproverId = Guid.NewGuid() };

        var (ok, status) = await service.ApproveAsync(lr.Id, req);

        Assert.False(ok);
        Assert.Equal("approval_not_configured", status);
        Assert.Equal("pending", lr.Status);
    }

    [Fact]
    public async Task ApproveAsync_WhenWorkflowIsNotConfigured_DoesNotChangeExistingStatus()
    {
        using var db = CreateInMemoryDbContext();
        var emp = new Employee { ChineseName = "Test", EmployeeNo = "E001" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var lr = new LeaveRequest { EmployeeId = emp.Id, ApplicationNo = "LR-001", LeaveType = "annual", Status = "approved" };
        db.LeaveRequests.Add(lr);
        await db.SaveChangesAsync();

        var service = new LeaveRequestService(db);
        var req = new ApproveLeaveRequestRequest { ApproverId = Guid.NewGuid() };

        var (ok, status) = await service.ApproveAsync(lr.Id, req);

        Assert.False(ok);
        Assert.Equal("approval_not_configured", status);
        Assert.Equal("approved", lr.Status);
    }

    [Fact]
    public async Task RejectAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var emp = new Employee { ChineseName = "Test", EmployeeNo = "E001" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var lr = new LeaveRequest { EmployeeId = emp.Id, ApplicationNo = "LR-001", LeaveType = "annual", Status = "pending" };
        db.LeaveRequests.Add(lr);
        await db.SaveChangesAsync();

        var service = new LeaveRequestService(db);
        var req = new RejectLeaveRequestRequest { ApproverId = Guid.NewGuid(), RejectReason = "Not enough notice" };

        var (ok, status) = await service.RejectAsync(lr.Id, req);

        Assert.True(ok);
        Assert.Equal("ok", status);
        Assert.Equal("rejected", lr.Status);
        Assert.Equal("Not enough notice", lr.RejectReason);
    }
}
