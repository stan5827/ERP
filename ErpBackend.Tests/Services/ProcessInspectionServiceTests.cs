using Microsoft.EntityFrameworkCore;
using Xunit;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Services;

namespace ErpBackend.Tests.Services;

public class ProcessInspectionServiceTests
{
    [Fact]
    public async Task CreateReportAsync_CalculatesNumericResult_AndKeepsTemplateSnapshot()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var operation = new Operation { OperationCode = "INJ", OperationName = "射出成形" };
        var workCenter = new WorkCenter { WorkCenterCode = "WC1", WorkCenterName = "射出線" };
        var sku = new Sku { SkuNumber = "FRAME-01", SkuName = "塑膠鏡框" };
        var workOrder = new WorkOrder
        {
            WoNumber = "WO-001",
            ProductSkuId = sku.Id,
            Operations = new List<WorkOrderOperation>
            {
                new()
                {
                    OperationSeq = 10,
                    OperationId = operation.Id,
                    WorkCenterId = workCenter.Id
                }
            }
        };
        db.AddRange(operation, workCenter, sku, workOrder);
        await db.SaveChangesAsync();

        var service = new ProcessInspectionService(db);
        var (template, templateStatus) = await service.CreateTemplateAsync(new SaveInspectionTemplateRequestDto
        {
            TemplateCode = "INJ-QC",
            TemplateName = "鏡框尺寸檢驗",
            OperationId = operation.Id,
            Items =
            {
                new SaveInspectionTemplateItemRequestDto
                {
                    ItemCode = "WIDTH",
                    ItemName = "鏡框總寬",
                    InputType = "number",
                    Specification = "規格 140 mm",
                    Unit = "mm",
                    LowerLimit = 135,
                    UpperLimit = 145,
                    IsRequired = true
                }
            }
        });

        Assert.Equal("ok", templateStatus);
        Assert.NotNull(template);

        var (report, reportStatus) = await service.CreateReportAsync(new CreateProcessInspectionRequestDto
        {
            WorkOrderId = workOrder.Id,
            OperationSeq = 10,
            TemplateId = template!.Id,
            LotNumber = "LOT-001",
            InspectorName = "檢驗員",
            SampleQuantity = 3,
            Items =
            {
                new CreateProcessInspectionItemRequestDto
                {
                    TemplateItemId = template.Items[0].Id,
                    Value = "150"
                }
            }
        });

        Assert.Equal("ok", reportStatus);
        Assert.NotNull(report);
        Assert.Equal("fail", report!.Result);
        Assert.Equal("fail", report.Items[0].Result);
        Assert.Equal(135, report.Items[0].LowerLimit);

        var (updatedTemplate, updateStatus) = await service.UpdateTemplateAsync(template.Id, new SaveInspectionTemplateRequestDto
        {
            TemplateCode = "INJ-QC",
            TemplateName = "鏡框尺寸檢驗",
            OperationId = operation.Id,
            Items =
            {
                new SaveInspectionTemplateItemRequestDto
                {
                    ItemCode = "WIDTH",
                    ItemName = "鏡框總寬",
                    InputType = "number",
                    Specification = "新版規格",
                    Unit = "mm",
                    LowerLimit = 130,
                    UpperLimit = 150,
                    IsRequired = true
                }
            }
        });

        Assert.Equal("ok", updateStatus);
        Assert.Equal(2, updatedTemplate!.Version);
        var reports = await service.GetReportsAsync(workOrder.Id, "LOT-001");
        Assert.Single(reports);
        Assert.Equal(1, reports[0].TemplateVersion);
        Assert.Equal("規格 140 mm", reports[0].Items[0].Specification);
        Assert.Equal(135, reports[0].Items[0].LowerLimit);
    }

    [Fact]
    public async Task CreateReportAsync_RejectsTemplateFromAnotherOperation()
    {
        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ErpGeneralDbContext(options);
        var requiredOperation = new Operation { OperationCode = "CUT", OperationName = "裁切" };
        var otherOperation = new Operation { OperationCode = "POL", OperationName = "拋光" };
        var workCenter = new WorkCenter { WorkCenterCode = "WC2", WorkCenterName = "加工線" };
        var sku = new Sku { SkuNumber = "SKU-02", SkuName = "產品" };
        var workOrder = new WorkOrder
        {
            WoNumber = "WO-002",
            ProductSkuId = sku.Id,
            Operations = new List<WorkOrderOperation>
            {
                new()
                {
                    OperationSeq = 20,
                    OperationId = requiredOperation.Id,
                    WorkCenterId = workCenter.Id
                }
            }
        };
        var template = new ErpBackend.Models.QA.InspectionTemplate
        {
            TemplateCode = "POL-QC",
            TemplateName = "拋光檢驗",
            OperationId = otherOperation.Id,
            Items =
            {
                new ErpBackend.Models.QA.InspectionTemplateItem
                {
                    ItemCode = "SURFACE",
                    ItemName = "表面外觀",
                    InputType = "passFail",
                    IsRequired = true
                }
            }
        };
        db.AddRange(requiredOperation, otherOperation, workCenter, sku, workOrder, template);
        await db.SaveChangesAsync();

        var service = new ProcessInspectionService(db);
        var (report, status) = await service.CreateReportAsync(new CreateProcessInspectionRequestDto
        {
            WorkOrderId = workOrder.Id,
            OperationSeq = 20,
            TemplateId = template.Id,
            InspectorName = "檢驗員",
            Items =
            {
                new CreateProcessInspectionItemRequestDto
                {
                    TemplateItemId = template.Items.First().Id,
                    Value = "合格",
                    Result = "pass"
                }
            }
        });

        Assert.Null(report);
        Assert.Equal("template_mismatch", status);
    }
}
