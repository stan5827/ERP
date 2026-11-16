using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class DepartmentServiceTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoData()
    {
        using var db = CreateInMemoryDbContext();
        var service = new DepartmentService(db);

        var result = await service.GetAllAsync(null, null, null);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByDisplayOrderThenDeptCode()
    {
        using var db = CreateInMemoryDbContext();
        db.Departments.AddRange(
            new Department { DeptCode = "D002", DeptName = "Beta", DisplayOrder = 1, IsActive = true },
            new Department { DeptCode = "D001", DeptName = "Alpha", DisplayOrder = 0, IsActive = true },
            new Department { DeptCode = "D003", DeptName = "Gamma", DisplayOrder = 0, IsActive = true });
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var result = await service.GetAllAsync(null, null, null);

        Assert.Equal(3, result.Count);
        Assert.Equal("D001", result[0].DeptCode);
        Assert.Equal("D003", result[1].DeptCode);
        Assert.Equal("D002", result[2].DeptCode);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByIsActive()
    {
        using var db = CreateInMemoryDbContext();
        db.Departments.AddRange(
            new Department { DeptCode = "D001", DeptName = "Alpha", IsActive = true },
            new Department { DeptCode = "D002", DeptName = "Beta", IsActive = false });
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var result = await service.GetAllAsync(null, null, true);

        Assert.Single(result);
        Assert.Equal("D001", result[0].DeptCode);
    }

    [Fact]
    public async Task GetAllAsync_FiltersBySearchTerm()
    {
        using var db = CreateInMemoryDbContext();
        db.Departments.AddRange(
            new Department { DeptCode = "D001", DeptName = "HR Department", IsActive = true },
            new Department { DeptCode = "D002", DeptName = "Engineering", IsActive = true });
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var result = await service.GetAllAsync("HR", null, null);

        Assert.Single(result);
        Assert.Equal("D001", result[0].DeptCode);
    }

    [Fact]
    public async Task GetFlatAsync_ReturnsOnlyActiveDepartments()
    {
        using var db = CreateInMemoryDbContext();
        db.Departments.AddRange(
            new Department { DeptCode = "D001", DeptName = "Alpha", IsActive = true },
            new Department { DeptCode = "D002", DeptName = "Beta", IsActive = false });
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var result = await service.GetFlatAsync();

        Assert.Single(result);
        Assert.Equal("D001", result[0].DeptCode);
    }

    [Fact]
    public async Task GetTreeAsync_BuildsRootAndChildren()
    {
        using var db = CreateInMemoryDbContext();
        var parent = new Department { DeptCode = "D001", DeptName = "Parent", IsActive = true };
        var child  = new Department { DeptCode = "D002", DeptName = "Child", IsActive = true };
        child.ParentDeptId = parent.Id;
        db.Departments.AddRange(parent, child);
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var roots = await service.GetTreeAsync();

        Assert.Single(roots);
        Assert.Equal("D001", roots[0].DeptCode);
        Assert.Single(roots[0].Children);
        Assert.Equal("D002", roots[0].Children[0].DeptCode);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var dept = new Department { DeptCode = "D001", DeptName = "Alpha", IsActive = true };
        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var result = await service.GetByIdAsync(dept.Id);

        Assert.NotNull(result);
        Assert.Equal("D001", result!.DeptCode);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();

        var service = new DepartmentService(db);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ReturnsDto_WhenCreated()
    {
        using var db = CreateInMemoryDbContext();
        var service = new DepartmentService(db);

        var req = new DepartmentUpsertDto { DeptCode = "D001", DeptName = "Alpha" };
        var (dto, status) = await service.CreateAsync(req);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Equal("D001", dto!.DeptCode);
    }

    [Fact]
    public async Task CreateAsync_ReturnsDuplicate_WhenCodeExists()
    {
        using var db = CreateInMemoryDbContext();
        db.Departments.Add(new Department { DeptCode = "D001", DeptName = "Alpha" });
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var req = new DepartmentUpsertDto { DeptCode = "D001", DeptName = "Other" };
        var (dto, status) = await service.CreateAsync(req);

        Assert.Equal("duplicate", status);
        Assert.Null(dto);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsDto_WhenUpdated()
    {
        using var db = CreateInMemoryDbContext();
        var dept = new Department { DeptCode = "D001", DeptName = "Alpha" };
        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var req = new DepartmentUpsertDto { DeptCode = "D001", DeptName = "Alpha Updated" };
        var (dto, status) = await service.UpdateAsync(dept.Id, req);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Equal("Alpha Updated", dto!.DeptName);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsDuplicate_WhenCodeTakenByAnother()
    {
        using var db = CreateInMemoryDbContext();
        db.Departments.AddRange(
            new Department { DeptCode = "D001", DeptName = "Alpha" },
            new Department { DeptCode = "D002", DeptName = "Beta" });
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var req = new DepartmentUpsertDto { DeptCode = "D002", DeptName = "Alpha" };
        var (dto, status) = await service.UpdateAsync(db.Departments.First(d => d.DeptCode == "D001").Id, req);

        Assert.Equal("duplicate", status);
        Assert.Null(dto);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenIdMissing()
    {
        using var db = CreateInMemoryDbContext();

        var service = new DepartmentService(db);

        var req = new DepartmentUpsertDto { DeptCode = "D001", DeptName = "Alpha" };
        var (dto, status) = await service.UpdateAsync(Guid.NewGuid(), req);

        Assert.Equal("not_found", status);
        Assert.Null(dto);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsOk_WhenDeleted()
    {
        using var db = CreateInMemoryDbContext();
        var dept = new Department { DeptCode = "D001", DeptName = "Alpha" };
        db.Departments.Add(dept);
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var (ok, status) = await service.DeleteAsync(dept.Id);

        Assert.True(ok);
        Assert.Equal("ok", status);
        Assert.Empty(db.Departments);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenIdMissing()
    {
        using var db = CreateInMemoryDbContext();

        var service = new DepartmentService(db);

        var (ok, status) = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(ok);
        Assert.Equal("not_found", status);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsGuarded_WhenSubDepartmentsExist()
    {
        using var db = CreateInMemoryDbContext();
        var parent = new Department { DeptCode = "D001", DeptName = "Parent" };
        var child  = new Department { DeptCode = "D002", DeptName = "Child" };
        child.ParentDeptId = parent.Id;
        db.Departments.AddRange(parent, child);
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var (ok, status) = await service.DeleteAsync(parent.Id);

        Assert.False(ok);
        Assert.Equal("guarded", status);
        Assert.Equal(2, db.Departments.Count());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsGuarded_WhenEmployeesExist()
    {
        using var db = CreateInMemoryDbContext();
        var dept = new Department { DeptCode = "D001", DeptName = "Alpha" };
        db.Departments.Add(dept);
        db.Employees.Add(new Employee { DepartmentId = dept.Id, ChineseName = "Test", EmployeeNo = "E001" });
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var (ok, status) = await service.DeleteAsync(dept.Id);

        Assert.False(ok);
        Assert.Equal("guarded", status);
        Assert.Single(db.Departments);
    }

    [Fact]
    public async Task GetAllAsync_MapsAllDtoFields()
    {
        using var db = CreateInMemoryDbContext();
        var parent = new Department { DeptCode = "D001", DeptName = "Parent" };
        var dept = new Department
        {
            DeptCode = "D002",
            DeptName = "Child",
            DeptType = "ops",
            ParentDeptId = parent.Id,
            CostCenterCode = "CC1",
            Description = "desc",
            IsActive = true,
            IsCostCenter = false,
            BudgetLimit = 1000,
            BudgetUsed = 250,
            DisplayOrder = 5
        };
        db.Departments.AddRange(parent, dept);
        await db.SaveChangesAsync();

        var service = new DepartmentService(db);

        var result = await service.GetAllAsync(null, null, null);

        var dto = result.Single(d => d.DeptCode == "D002");
        Assert.Equal("ops", dto.DeptType);
        Assert.Equal(parent.Id, dto.ParentDeptId);
        Assert.Equal("Parent", dto.ParentDeptName);
        Assert.Equal("CC1", dto.CostCenterCode);
        Assert.Equal("desc", dto.Description);
        Assert.True(dto.IsActive);
        Assert.False(dto.IsCostCenter);
        Assert.Equal(1000, dto.BudgetLimit);
        Assert.Equal(250, dto.BudgetUsed);
        Assert.Equal(5, dto.DisplayOrder);
    }
}
