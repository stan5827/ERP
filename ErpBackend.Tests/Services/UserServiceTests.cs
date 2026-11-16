using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using ErpBackend.Models.System;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class UserServiceTests
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
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        using var db = CreateInMemoryDbContext();
        db.Users.AddRange(
            new User { Username = "alice", Email = "alice@test.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1), PasswordHash = "hash1" },
            new User { Username = "bob", Email = "bob@test.com", IsActive = true, CreatedAt = DateTime.UtcNow, PasswordHash = "hash2" });
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("bob", result[0].Username);
    }

    [Fact]
    public async Task GetAllAsync_FiltersBySearch()
    {
        using var db = CreateInMemoryDbContext();
        db.Users.AddRange(
            new User { Username = "alice", Email = "alice@test.com", IsActive = true, CreatedAt = DateTime.UtcNow, PasswordHash = "hash1" },
            new User { Username = "bob", Email = "bob@test.com", IsActive = true, CreatedAt = DateTime.UtcNow, PasswordHash = "hash2" });
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var result = await service.GetAllAsync(search: "ali");

        Assert.Single(result);
        Assert.Equal("alice", result[0].Username);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "alice", Email = "alice@test.com", IsActive = true, CreatedAt = DateTime.UtcNow, PasswordHash = "hash1" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var dto = await service.GetByIdAsync(user.Id);

        Assert.NotNull(dto);
        Assert.Equal("alice", dto!.Username);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new UserService(db);
        Assert.Null(await service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var department = new Department { DeptCode = "D001", DeptName = "Test Department" };
        var jobTitle = new JobTitle { TitleCode = "J001", TitleName = "Test Job Title" };
        var employee = new Employee
        {
            EmployeeNo = "E001",
            ChineseName = "Test Employee",
            Status = "active",
            NationalId = "A123456789",
            HireDate = DateTime.UtcNow.Date,
            DepartmentId = department.Id,
            JobTitleId = jobTitle.Id
        };
        var role = new Role { RoleName = "Operator" };
        db.Departments.Add(department);
        db.JobTitles.Add(jobTitle);
        db.Employees.Add(employee);
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var actorId = Guid.NewGuid();

        var req = new CreateUserRequest
        {
            Username = "charlie",
            Password = "Password123",
            Email = "charlie@test.com",
            EmployeeId = employee.Id,
            RoleIds = [role.Id]
        };

        var (dto, status) = await service.CreateAsync(req, actorId);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Equal("charlie", dto!.Username);
        Assert.Equal(employee.Id, dto.EmployeeId);
        Assert.Contains("Operator", dto.Roles);
        Assert.True(await db.Users.Where(user => user.EmployeeId == employee.Id)
            .Select(user => user.MustChangePassword).SingleAsync());
        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal("USER_CREATED", audit.Action);
        Assert.Equal(actorId, audit.UserId);
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingEmployeeBinding()
    {
        using var db = CreateInMemoryDbContext();
        var service = new UserService(db);

        var (dto, status) = await service.CreateAsync(new CreateUserRequest
        {
            Username = "unbound",
            Password = "Password123"
        }, Guid.NewGuid());

        Assert.Null(dto);
        Assert.Equal("employee_required", status);
    }

    [Fact]
    public async Task CreateAsync_RejectsInactiveEmployee()
    {
        using var db = CreateInMemoryDbContext();
        var employee = new Employee { EmployeeNo = "E002", ChineseName = "Former Employee", Status = "resigned" };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var (dto, status) = await service.CreateAsync(new CreateUserRequest
        {
            Username = "inactive",
            Password = "Password123",
            EmployeeId = employee.Id
        }, Guid.NewGuid());

        Assert.Null(dto);
        Assert.Equal("employee_inactive", status);
    }

    [Fact]
    public async Task CreateAsync_RejectsEmployeeWithIncompleteHrProfile()
    {
        using var db = CreateInMemoryDbContext();
        var employee = new Employee
        {
            EmployeeNo = "E005",
            ChineseName = "Incomplete Employee",
            Status = "active"
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var (dto, status) = await service.CreateAsync(new CreateUserRequest
        {
            Username = "incomplete-profile",
            Password = "Password123",
            EmployeeId = employee.Id
        }, Guid.NewGuid());

        Assert.Null(dto);
        Assert.Equal("employee_profile_incomplete", status);
    }

    [Fact]
    public async Task CreateAsync_RejectsEmployeeAlreadyBoundToAnotherUser()
    {
        using var db = CreateInMemoryDbContext();
        var employee = new Employee { EmployeeNo = "E003", ChineseName = "Bound Employee", Status = "active" };
        db.Employees.Add(employee);
        db.Users.Add(new User
        {
            Username = "existing",
            PasswordHash = "hash",
            EmployeeId = employee.Id
        });
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var (dto, status) = await service.CreateAsync(new CreateUserRequest
        {
            Username = "duplicate-employee",
            Password = "Password123",
            EmployeeId = employee.Id
        }, Guid.NewGuid());

        Assert.Null(dto);
        Assert.Equal("employee_already_linked", status);
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownRoleId()
    {
        using var db = CreateInMemoryDbContext();
        var employee = new Employee { EmployeeNo = "E004", ChineseName = "Role Employee", Status = "active" };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var (dto, status) = await service.CreateAsync(new CreateUserRequest
        {
            Username = "invalid-role",
            Password = "Password123",
            EmployeeId = employee.Id,
            RoleIds = [Guid.NewGuid()]
        }, Guid.NewGuid());

        Assert.Null(dto);
        Assert.Equal("invalid_roles", status);
    }

    [Fact]
    public async Task CreateAsync_ReturnsDuplicate_WhenUsernameExists()
    {
        using var db = CreateInMemoryDbContext();
            db.Users.Add(new User { Username = "alice", Email = "alice@test.com", IsActive = true, CreatedAt = DateTime.UtcNow, PasswordHash = "hash1" });
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var req = new CreateUserRequest { Username = "alice", Password = "Password123", Email = "alice2@test.com" };

        var (dto, status) = await service.CreateAsync(req, Guid.NewGuid());

        Assert.Null(dto);
        Assert.Equal("duplicate", status);
    }

    [Fact]
    public async Task CreateAsync_ReturnsInvalid_WhenPasswordTooShort()
    {
        using var db = CreateInMemoryDbContext();
        var service = new UserService(db);
        var req = new CreateUserRequest { Username = "bob", Password = "abc", Email = "bob@test.com" };

        var (dto, status) = await service.CreateAsync(req, Guid.NewGuid());

        Assert.Null(dto);
        Assert.Equal("invalid", status);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsOk_WhenUserExists()
    {
        using var db = CreateInMemoryDbContext();
        var user = new User { Username = "alice", Email = "alice@test.com", IsActive = true, CreatedAt = DateTime.UtcNow, PasswordHash = "hash1" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new UserService(db);
        var actorId = Guid.NewGuid();
        var (found, status) = await service.DeleteAsync(user.Id, actorId);

        Assert.True(found);
        Assert.Equal("ok", status);
        Assert.False(user.IsActive);
        Assert.Equal(1, user.SecurityVersion);
        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal("USER_DEACTIVATED", audit.Action);
        Assert.Equal(actorId, audit.UserId);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenUserDoesNotExist()
    {
        using var db = CreateInMemoryDbContext();
        var service = new UserService(db);
        var (found, status) = await service.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(found);
        Assert.Equal("not_found", status);
    }
}
