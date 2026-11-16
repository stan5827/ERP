using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpBackend.Tests.Services;

public class CostCenterServiceTests
{
    [Fact]
    public async Task CreateAndUpdateAsync_PersistsAreaSquareMeters()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CostCenterService(db);
        var request = new SaveCostCenterRequestDto
        {
            CostCenterCode = "CC-AREA",
            CostCenterName = "產線",
            CostCenterType = "production",
            AreaSquareMeters = 120
        };

        var (created, createStatus, costCenter) = await service.CreateAsync(request);
        Assert.True(created, createStatus);
        Assert.Equal(120m, costCenter!.AreaSquareMeters);

        request.ParentCostCenterId = costCenter.Id;
        var (selfParentUpdated, selfParentStatus, _) =
            await service.UpdateAsync(costCenter.Id, request);
        Assert.False(selfParentUpdated);
        Assert.Equal("invalid_parent", selfParentStatus);

        request.ParentCostCenterId = null;
        request.AreaSquareMeters = 185.5m;
        var (updated, updateStatus, updatedCenter) = await service.UpdateAsync(costCenter.Id, request);

        Assert.True(updated, updateStatus);
        Assert.Equal(185.5m, updatedCenter!.AreaSquareMeters);
        Assert.Equal(185.5m, (await db.CostCenters.SingleAsync()).AreaSquareMeters);
    }

    [Fact]
    public async Task CreateAsync_RejectsNegativeAreaSquareMeters()
    {
        using var db = CreateInMemoryDbContext();
        var service = new CostCenterService(db);

        var (success, status, costCenter) = await service.CreateAsync(new SaveCostCenterRequestDto
        {
            CostCenterCode = "CC-NEG",
            CostCenterName = "無效成本中心",
            CostCenterType = "production",
            AreaSquareMeters = -1
        });

        Assert.False(success);
        Assert.Equal("invalid_amount", status);
        Assert.Null(costCenter);
        Assert.Empty(await db.CostCenters.ToListAsync());
    }

    private static ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }
}
