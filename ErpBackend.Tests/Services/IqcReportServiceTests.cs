using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.QA;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class IqcReportServiceTests
{
    private ErpGeneralDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ErpGeneralDbContext(options);
    }

    [Fact]
    public async Task GetReportsAsync_ReturnsEmpty_WhenNoData()
    {
        using var db = CreateInMemoryDbContext();
        var service = new IqcReportService(db);

        var (items, totalCount) = await service.GetReportsAsync(null, 1, 50);

        Assert.Empty(items);
        Assert.Equal(0, totalCount);
    }

    [Fact]
    public async Task GetReportsAsync_ReturnsAllOrdered_WhenNoFilter()
    {
        using var db = CreateInMemoryDbContext();
        var (supplier, sku) = CreateRelatedEntities();
        db.IqcReports.AddRange(
            CreateReport("IQC-001", supplier, sku, DateTime.Today.AddDays(-2), "pass", 10, 0),
            CreateReport("IQC-002", supplier, sku, DateTime.Today, "fail", 20, 2),
            CreateReport("IQC-003", supplier, sku, DateTime.Today.AddDays(-1), "pass", 30, 1));
        db.AddRange(supplier, sku);
        await db.SaveChangesAsync();

        var service = new IqcReportService(db);

        var (items, totalCount) = await service.GetReportsAsync(null, 1, 50);

        Assert.Equal(3, totalCount);
        Assert.Equal(3, items.Count);
        Assert.Equal("IQC-002", items[0].ReportNumber);
        Assert.Equal("IQC-003", items[1].ReportNumber);
        Assert.Equal("IQC-001", items[2].ReportNumber);
    }

    [Fact]
    public async Task GetReportsAsync_FiltersByResult()
    {
        using var db = CreateInMemoryDbContext();
        var (supplier, sku) = CreateRelatedEntities();
        db.IqcReports.AddRange(
            CreateReport("IQC-001", supplier, sku, DateTime.Today, "pass", 10, 0),
            CreateReport("IQC-002", supplier, sku, DateTime.Today.AddDays(-1), "fail", 20, 2));
        db.AddRange(supplier, sku);
        await db.SaveChangesAsync();

        var service = new IqcReportService(db);

        var (items, totalCount) = await service.GetReportsAsync("fail", 1, 50);

        Assert.Single(items);
        Assert.Equal(1, totalCount);
        Assert.Equal("IQC-002", items[0].ReportNumber);
    }

    [Fact]
    public async Task GetReportsAsync_PaginatesResults()
    {
        using var db = CreateInMemoryDbContext();
        var (supplier, sku) = CreateRelatedEntities();
        db.IqcReports.AddRange(
            CreateReport("IQC-001", supplier, sku, DateTime.Today, "pass", 10, 0),
            CreateReport("IQC-002", supplier, sku, DateTime.Today.AddDays(-1), "pass", 10, 0),
            CreateReport("IQC-003", supplier, sku, DateTime.Today.AddDays(-2), "pass", 10, 0));
        db.AddRange(supplier, sku);
        await db.SaveChangesAsync();

        var service = new IqcReportService(db);

        var (items, totalCount) = await service.GetReportsAsync(null, 1, 2);

        Assert.Equal(3, totalCount);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task GetReportsAsync_MapsAllDtoFields()
    {
        using var db = CreateInMemoryDbContext();
        var (supplier, sku) = CreateRelatedEntities();
        var inspectionDate = new DateTime(2026, 10, 14);
        db.IqcReports.Add(CreateReport("IQC-TEST", supplier, sku, inspectionDate, "fail", 100, 5, "LOT-001"));
        db.AddRange(supplier, sku);
        await db.SaveChangesAsync();

        var service = new IqcReportService(db);

        var (items, _) = await service.GetReportsAsync(null, 1, 50);

        var dto = items[0];
        Assert.Equal("IQC-TEST", dto.ReportNumber);
        Assert.Equal("LOT-001", dto.BatchNumber);
        Assert.Equal(100, dto.SampleQty);
        Assert.Equal(5, dto.DefectQty);
        Assert.Equal("fail", dto.Result);
        Assert.Equal(inspectionDate, dto.ReportDate);
    }

    private static (Supplier Supplier, Sku Sku) CreateRelatedEntities() =>
        (
            new Supplier { SupplierCode = "SUP-IQC", SupplierName = "IQC Supplier" },
            new Sku { SkuNumber = "SKU-IQC", SkuName = "IQC Material" }
        );

    private static IqcReport CreateReport(
        string reportNumber,
        Supplier supplier,
        Sku sku,
        DateTime inspectionDate,
        string result,
        int sampleSize,
        int rejectNumber,
        string? lotNumber = null) =>
        new()
        {
            ReportNumber = reportNumber,
            SupplierId = supplier.Id,
            Supplier = supplier,
            SkuId = sku.Id,
            Sku = sku,
            InspectionDate = inspectionDate,
            Result = result,
            SampleSize = sampleSize,
            RejectNumber = rejectNumber,
            LotNumber = lotNumber
        };
}
