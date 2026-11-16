using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Services;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.DTOs;
using Microsoft.Extensions.Logging;

namespace ErpBackend.Tests.Services;

public class MpsServiceTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    [Fact]
    public async Task GetMpsHeadersAsync_ReturnsEmptyList_WhenNoData()
    {
        using var db = CreateInMemoryDbContext();
        var mockLogger = new Mock<ILogger<MpsService>>();
        var service = new MpsService(db, mockLogger.Object);

        var result = await service.GetMpsHeadersAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateMpsVersionAsync_UsesDefaultConsumptionStrategy_WhenRequestOmitsIt()
    {
        using var db = CreateInMemoryDbContext();
        var mockLogger = new Mock<ILogger<MpsService>>();
        var service = new MpsService(db, mockLogger.Object);
        var horizonStart = new DateTime(2026, 10, 23);
        var horizonEnd = new DateTime(2026, 12, 22);
        var bucketStart = new DateTime(2026, 10, 26);
        var bucketEnd = new DateTime(2026, 11, 1);
        var request = new CreateMpsRequest
        {
            PlanName = "Test Plan",
            HorizonStart = horizonStart,
            HorizonEnd = horizonEnd,
            TimeBucketType = "WEEK",
            BaseOn = "MIXED",
            Lines = new List<MpsLineDto>
            {
                new()
                {
                    SkuId = Guid.NewGuid(),
                    BucketStart = bucketStart,
                    BucketEnd = bucketEnd,
                    PlanQty = 10
                }
            }
        };

        var result = await service.CreateMpsVersionAsync(request, Guid.NewGuid());

        Assert.Equal("BACKWARD_THEN_FORWARD", result.ConsumptionStrategy);
        Assert.Equal("WEEK", result.TimeBucketType);
        Assert.Equal("MIXED", result.BaseOn);
        Assert.Equal(DateTimeKind.Utc, result.HorizonStart.Kind);
        Assert.Equal(DateTimeKind.Utc, result.HorizonEnd.Kind);
        Assert.Equal(DateTimeKind.Utc, Assert.Single(result.Lines).BucketStart.Kind);
        Assert.Equal(DateTimeKind.Utc, result.Lines.Single().BucketEnd.Kind);
        Assert.Equal(horizonStart, result.HorizonStart);
        Assert.Equal(horizonEnd, result.HorizonEnd);
        Assert.Equal(bucketStart, result.Lines.Single().BucketStart);
        Assert.Equal(bucketEnd, result.Lines.Single().BucketEnd);
    }

    [Fact]
    public async Task GetMpsHeadersAsync_ReturnsMpsHeaders()
    {
        using var db = CreateInMemoryDbContext();
        var mockLogger = new Mock<ILogger<MpsService>>();
        
        db.MpsHeaders.Add(new MpsHeader
        {
            MpsNo = "MPS-20260101-001",
            PlanName = "Test Plan",
            PlantCode = "HQ",
            HorizonStart = DateTime.UtcNow,
            HorizonEnd = DateTime.UtcNow.AddMonths(3),
            TimeBucketType = "WEEKLY",
            VersionNo = 1,
            Status = "DRAFT"
        });
        await db.SaveChangesAsync();
        
        var service = new MpsService(db, mockLogger.Object);

        var result = await service.GetMpsHeadersAsync();

        Assert.Single(result);
        Assert.Equal("Test Plan", result[0].PlanName);
    }

    [Fact]
    public async Task GetMpsVersionsAsync_ReturnsVersionsForPlanName()
    {
        using var db = CreateInMemoryDbContext();
        var mockLogger = new Mock<ILogger<MpsService>>();
        
        db.MpsHeaders.AddRange(
            new MpsHeader { MpsNo = "MPS-001", PlanName = "Plan A", VersionNo = 1, Status = "DRAFT" },
            new MpsHeader { MpsNo = "MPS-002", PlanName = "Plan A", VersionNo = 2, Status = "DRAFT" },
            new MpsHeader { MpsNo = "MPS-003", PlanName = "Plan B", VersionNo = 1, Status = "DRAFT" }
        );
        await db.SaveChangesAsync();
        
        var service = new MpsService(db, mockLogger.Object);

        var result = await service.GetMpsVersionsAsync("Plan A");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetLatestApprovedMpsAsync_ReturnsNull_WhenNoApproved()
    {
        using var db = CreateInMemoryDbContext();
        var mockLogger = new Mock<ILogger<MpsService>>();
        
        db.MpsHeaders.Add(new MpsHeader { MpsNo = "MPS-001", PlanName = "Plan A", VersionNo = 1, Status = "DRAFT" });
        await db.SaveChangesAsync();
        
        var service = new MpsService(db, mockLogger.Object);

        var result = await service.GetLatestApprovedMpsAsync("Plan A");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestApprovedMpsAsync_ReturnsApprovedVersion()
    {
        using var db = CreateInMemoryDbContext();
        var mockLogger = new Mock<ILogger<MpsService>>();
        
        db.MpsHeaders.AddRange(
            new MpsHeader { MpsNo = "MPS-001", PlanName = "Plan A", VersionNo = 1, Status = "DRAFT" },
            new MpsHeader { MpsNo = "MPS-002", PlanName = "Plan A", VersionNo = 2, Status = "APPROVED" }
        );
        await db.SaveChangesAsync();
        
        var service = new MpsService(db, mockLogger.Object);

        var result = await service.GetLatestApprovedMpsAsync("Plan A");

        Assert.NotNull(result);
        Assert.Equal("APPROVED", result.Status);
    }

    [Fact]
    public async Task GetMpsDetailAsync_ReturnsNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var mockLogger = new Mock<ILogger<MpsService>>();
        var service = new MpsService(db, mockLogger.Object);

        var result = await service.GetMpsDetailAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}

public class MpsDtoTests
{
    [Fact]
    public void MpsHeaderDto_CanBeCreated()
    {
        var dto = new MpsHeaderDto
        {
            Id = Guid.NewGuid(),
            MpsNo = "MPS-001",
            PlanName = "Test Plan",
            Status = "DRAFT"
        };
        
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("MPS-001", dto.MpsNo);
    }

    [Fact]
    public void MpsDetailDto_CanContainLines()
    {
        var dto = new MpsDetailDto();
        
        Assert.NotNull(dto.Lines);
    }
}