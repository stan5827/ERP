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

public class JobGradeServiceTests
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
    public async Task GetAllAsync_ReturnsAllOrderedByLevelThenCode()
    {
        using var db = CreateInMemoryDbContext();
        db.JobGrades.AddRange(
            new JobGrade { GradeCode = "G3", GradeName = "Gamma", Level = 3 },
            new JobGrade { GradeCode = "G1", GradeName = "Alpha", Level = 1 },
            new JobGrade { GradeCode = "G2", GradeName = "Beta", Level = 1 });
        await db.SaveChangesAsync();

        var service = new JobGradeService(db);
        var result = await service.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal("G1", result[0].GradeCode);
        Assert.Equal("G2", result[1].GradeCode);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByIsActive()
    {
        using var db = CreateInMemoryDbContext();
        db.JobGrades.AddRange(
            new JobGrade { GradeCode = "G1", GradeName = "Alpha", IsActive = true },
            new JobGrade { GradeCode = "G2", GradeName = "Beta", IsActive = false });
        await db.SaveChangesAsync();

        var service = new JobGradeService(db);
        var result = await service.GetAllAsync(isActive: true);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllAsync_FiltersBySearch()
    {
        using var db = CreateInMemoryDbContext();
        db.JobGrades.AddRange(
            new JobGrade { GradeCode = "G1", GradeName = "Alpha" },
            new JobGrade { GradeCode = "G2", GradeName = "Beta" });
        await db.SaveChangesAsync();

        var service = new JobGradeService(db);
        var result = await service.GetAllAsync(search: "Alpha");

        Assert.Single(result);
        Assert.Equal("G1", result[0].GradeCode);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDto_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var grade = new JobGrade { GradeCode = "G1", GradeName = "Alpha", Level = 1 };
        db.JobGrades.Add(grade);
        await db.SaveChangesAsync();

        var service = new JobGradeService(db);
        var dto = await service.GetByIdAsync(grade.Id);

        Assert.NotNull(dto);
        Assert.Equal("G1", dto!.GradeCode);
        Assert.Equal("Alpha", dto.GradeName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var service = new JobGradeService(db);
        Assert.Null(await service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var service = new JobGradeService(db);
        var req = new JobGradeUpsertDto { GradeCode = "G1", GradeName = "Alpha", Level = 1 };

        var (dto, status) = await service.CreateAsync(req);

        Assert.Equal("ok", status);
        Assert.NotNull(dto);
        Assert.Equal("G1", dto!.GradeCode);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task CreateAsync_ReturnsInvalid_WhenCodeEmpty()
    {
        using var db = CreateInMemoryDbContext();
        var service = new JobGradeService(db);
        var req = new JobGradeUpsertDto { GradeCode = "", GradeName = "Alpha" };

        var (dto, status) = await service.CreateAsync(req);

        Assert.Null(dto);
        Assert.Equal("invalid", status);
    }

    [Fact]
    public async Task CreateAsync_ReturnsDuplicate_WhenCodeExists()
    {
        using var db = CreateInMemoryDbContext();
        db.JobGrades.Add(new JobGrade { GradeCode = "G1", GradeName = "Alpha" });
        await db.SaveChangesAsync();

        var service = new JobGradeService(db);
        var req = new JobGradeUpsertDto { GradeCode = "G1", GradeName = "Beta" };

        var (dto, status) = await service.CreateAsync(req);

        Assert.Null(dto);
        Assert.Equal("duplicate", status);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsOk_WhenValid()
    {
        using var db = CreateInMemoryDbContext();
        var grade = new JobGrade { GradeCode = "G1", GradeName = "Alpha", Level = 1 };
        db.JobGrades.Add(grade);
        await db.SaveChangesAsync();

        var service = new JobGradeService(db);
        var req = new JobGradeUpsertDto { GradeCode = "G1", GradeName = "Alpha Updated", Level = 2 };

        var (dto, status) = await service.UpdateAsync(grade.Id, req);

        Assert.Equal("ok", status);
        Assert.Equal("Alpha Updated", dto!.GradeName);
        Assert.Equal(2, dto.Level);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new JobGradeService(db);

        var req = new JobGradeUpsertDto { GradeCode = "G1", GradeName = "Alpha" };
        var (dto, status) = await service.UpdateAsync(Guid.NewGuid(), req);

        Assert.Null(dto);
        Assert.Equal("not_found", status);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsOk_WhenNoDependencies()
    {
        using var db = CreateInMemoryDbContext();
        var grade = new JobGrade { GradeCode = "G1", GradeName = "Alpha" };
        db.JobGrades.Add(grade);
        await db.SaveChangesAsync();

        var service = new JobGradeService(db);
        var (ok, status) = await service.DeleteAsync(grade.Id);

        Assert.True(ok);
        Assert.Equal("ok", status);
        Assert.Empty(db.JobGrades);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsGuarded_WhenJobTitlesLinked()
    {
        using var db = CreateInMemoryDbContext();
        var grade = new JobGrade { GradeCode = "G1", GradeName = "Alpha" };
        db.JobGrades.Add(grade);
        await db.SaveChangesAsync();
        db.JobTitles.Add(new JobTitle { TitleCode = "T1", TitleName = "Title 1", GradeId = grade.Id });
        await db.SaveChangesAsync();

        var service = new JobGradeService(db);
        var (ok, status) = await service.DeleteAsync(grade.Id);

        Assert.False(ok);
        Assert.Equal("guarded", status);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFound_WhenMissing()
    {
        using var db = CreateInMemoryDbContext();
        var service = new JobGradeService(db);

        var (ok, status) = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(ok);
        Assert.Equal("not_found", status);
    }
}
