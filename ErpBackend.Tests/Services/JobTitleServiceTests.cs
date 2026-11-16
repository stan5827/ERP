using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.HR;
using ErpBackend.Models.MES;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class JobTitleServiceTests
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
    public async Task GetAllAsync_ReturnsAllOrderedByTitleCode()
    {
        using var db = CreateInMemoryDbContext();
        db.JobTitles.AddRange(
            new JobTitle { TitleCode = "T002", TitleName = "Beta" },
            new JobTitle { TitleCode = "T001", TitleName = "Alpha" });
        await db.SaveChangesAsync();

        var service = new JobTitleService(db);
        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("T001", result[0].TitleCode);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByIsActive()
    {
        using var db = CreateInMemoryDbContext();
        db.JobTitles.AddRange(
            new JobTitle { TitleCode = "T001", TitleName = "Alpha", IsActive = true },
            new JobTitle { TitleCode = "T002", TitleName = "Beta", IsActive = false });
        await db.SaveChangesAsync();

        var service = new JobTitleService(db);
        var result = await service.GetAllAsync(isActive: true);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var grade = new JobGrade { GradeCode = "G1", GradeName = "Grade 1" };
        var title = new JobTitle { TitleCode = "T001", TitleName = "Alpha", Grade = grade };
        db.JobTitles.Add(title);
        await db.SaveChangesAsync();

        var service = new JobTitleService(db);
        var dto = await service.GetByIdAsync(title.Id);

        Assert.NotNull(dto);
        Assert.Equal("T001", dto!.TitleCode);
        Assert.Equal("G1", dto.GradeCode);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new JobTitleService(db);
        Assert.Null(await service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var service = new JobTitleService(db);
        var req = new JobTitleUpsertDto { TitleCode = "T001", TitleName = "Alpha" };

        var (dto, status) = await service.CreateAsync(req);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Equal("T001", dto!.TitleCode);
    }

    [Fact]
    public async Task CreateAsync_ReturnsDuplicate_WhenCodeExists()
    {
        using var db = CreateInMemoryDbContext();
        db.JobTitles.Add(new JobTitle { TitleCode = "T001", TitleName = "Alpha" });
        await db.SaveChangesAsync();

        var service = new JobTitleService(db);
        var req = new JobTitleUpsertDto { TitleCode = "T001", TitleName = "Other" };

        var (dto, status) = await service.CreateAsync(req);

        Assert.Null(dto);
        Assert.Equal("duplicate", status);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var grade = new JobGrade { GradeCode = "G1", GradeName = "Grade 1" };
        var title = new JobTitle { TitleCode = "T001", TitleName = "Alpha", Grade = grade };
        db.JobTitles.Add(title);
        await db.SaveChangesAsync();

        var service = new JobTitleService(db);
        var req = new JobTitleUpsertDto { TitleCode = "T001", TitleName = "Alpha Updated" };

        var (dto, status) = await service.UpdateAsync(title.Id, req);

        Assert.Equal("ok", status);
        Assert.Equal("Alpha Updated", dto!.TitleName);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsGuarded_WhenEmployeesExist()
    {
        using var db = CreateInMemoryDbContext();
        var title = new JobTitle { TitleCode = "T001", TitleName = "Alpha" };
        db.JobTitles.Add(title);
        await db.SaveChangesAsync();
        db.Employees.Add(new Employee { ChineseName = "Test", EmployeeNo = "E001", JobTitleId = title.Id });
        await db.SaveChangesAsync();

        var service = new JobTitleService(db);
        var (ok, status) = await service.DeleteAsync(title.Id);

        Assert.False(ok);
        Assert.Equal("guarded", status);
        Assert.Single(db.JobTitles);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new JobTitleService(db);

        var (ok, status) = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(ok);
        Assert.Equal("not_found", status);
    }
}
