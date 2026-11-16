using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class ApsIntegrationServiceTests
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
    public async Task ExportDataAsync_ReturnsSkusAndBoms()
    {
        using var db = CreateInMemoryDbContext();
        db.Skus.AddRange(
            new Sku { Id = Guid.NewGuid(), SkuNumber = "SKU-001", SkuName = "Widget A", Unit = "PCS", Category = "A" },
            new Sku { Id = Guid.NewGuid(), SkuNumber = "SKU-002", SkuName = "Widget B", Unit = "PCS", Category = "B" });
        await db.SaveChangesAsync();

        var sku1 = db.Skus.First(s => s.SkuNumber == "SKU-001");
        var sku2 = db.Skus.First(s => s.SkuNumber == "SKU-002");

        db.BomItems.Add(new BomItem
        {
            Id = Guid.NewGuid(),
            Bom = new Bom { Id = Guid.NewGuid(), ProductSkuId = sku1.Id, BomCode = "BOM-001" },
            ChildSkuId = sku2.Id,
            Quantity = 2,
            Unit = "PCS"
        });
        await db.SaveChangesAsync();

        var service = new ApsIntegrationService(db);
        var result = await service.ExportDataAsync(null, null);

        var dict = Assert.IsType<Dictionary<string, object>>(result);
        Assert.Equal(200, dict["code"]);
        Assert.Equal("Export success", dict["message"]);

        var data = Assert.IsType<ApsExportDataDto>(dict["data"]);
        Assert.Equal(2, data.Items.Count);
        Assert.Single(data.Boms);
        Assert.Equal("SKU-001", data.Boms[0].ParentSkuCode);
        Assert.Equal("SKU-002", data.Boms[0].ChildSkuCode);
    }

    [Fact]
    public async Task ExportDataAsync_FiltersWorkOrdersByDateRange()
    {
        using var db = CreateInMemoryDbContext();
        var sku = new Sku { Id = Guid.NewGuid(), SkuNumber = "SKU-001", SkuName = "Widget", Unit = "PCS", Category = "A" };
        db.Skus.Add(sku);
        await db.SaveChangesAsync();

        db.WorkOrders.AddRange(
            new WorkOrder { WoNumber = "WO-001", Status = "pending", PlannedEndDate = new DateTime(2026, 1, 10), PlannedQty = 100, ProductSkuId = sku.Id },
            new WorkOrder { WoNumber = "WO-002", Status = "released", PlannedEndDate = new DateTime(2026, 2, 20), PlannedQty = 200, ProductSkuId = sku.Id },
            new WorkOrder { WoNumber = "WO-003", Status = "completed", PlannedEndDate = new DateTime(2026, 3, 30), PlannedQty = 300, ProductSkuId = sku.Id });
        await db.SaveChangesAsync();

        var service = new ApsIntegrationService(db);
        var result = await service.ExportDataAsync(new DateTime(2026, 1, 15), new DateTime(2026, 2, 25));

        var dict = Assert.IsType<Dictionary<string, object>>(result);
        var data = Assert.IsType<ApsExportDataDto>(dict["data"]);
        Assert.Single(data.WorkOrders);
        Assert.Equal("WO-002", data.WorkOrders[0].WoNumber);
    }

    [Fact]
    public async Task ImportScheduleAsync_ReturnsBadRequest_WhenRequestNull()
    {
        using var db = CreateInMemoryDbContext();
        var service = new ApsIntegrationService(db);
        var result = await service.ImportScheduleAsync(null);

        var dict = Assert.IsType<Dictionary<string, object>>(result);
        Assert.Equal(400, dict["code"]);
        Assert.Equal("Invalid schedule data", dict["message"]);
    }

    [Fact]
    public async Task ImportScheduleAsync_ReturnsBadRequest_WhenVersionEmpty()
    {
        using var db = CreateInMemoryDbContext();
        var service = new ApsIntegrationService(db);
        var result = await service.ImportScheduleAsync(new ApsImportScheduleRequest { ScheduleVersion = "" });

        var dict = Assert.IsType<Dictionary<string, object>>(result);
        Assert.Equal(400, dict["code"]);
    }

    [Fact]
    public async Task ImportScheduleAsync_ReturnsBadRequest_WhenResultsEmpty()
    {
        using var db = CreateInMemoryDbContext();
        var service = new ApsIntegrationService(db);
        var result = await service.ImportScheduleAsync(new ApsImportScheduleRequest
        {
            ScheduleVersion = "v1",
            Results = new List<ApsScheduleResultItem>()
        });

        var dict = Assert.IsType<Dictionary<string, object>>(result);
        Assert.Equal(400, dict["code"]);
        Assert.Equal("No schedule results provided", dict["message"]);
    }

    [Fact]
    public async Task ImportScheduleAsync_ImportsSchedule_Success()
    {
        using var db = CreateInMemoryDbContext();
        db.WorkOrders.Add(new WorkOrder
        {
            Id = Guid.NewGuid(),
            WoNumber = "WO-001",
            Status = "pending",
            IsScheduleLocked = false,
            PlannedQty = 100
        });
        await db.SaveChangesAsync();

        var service = new ApsIntegrationService(db);
        var result = await service.ImportScheduleAsync(new ApsImportScheduleRequest
        {
            ScheduleVersion = "v1",
            Results = new List<ApsScheduleResultItem>
            {
                new()
                {
                    OrderNo = "WO-001",
                    OperationSeq = null,
                    ResourceCode = "RES-001",
                    StartTime = new DateTime(2026, 1, 10),
                    EndTime = new DateTime(2026, 1, 12),
                    Sequence = 0
                }
            }
        });

        var dict = Assert.IsType<Dictionary<string, object>>(result);
        Assert.Equal(200, dict["code"]);
        Assert.Contains("v1", dict["message"].ToString()!);

        var audit = await db.ApsScheduleResults.FirstOrDefaultAsync();
        Assert.NotNull(audit);
        Assert.Equal("WO-001", audit.OrderNo);
        Assert.Equal("v1", audit.ScheduleVersion);
    }
}
