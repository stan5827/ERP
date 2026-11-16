using System.Collections.Generic;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface IApsIntegrationService
{
    Task<Dictionary<string, object>> ExportDataAsync(DateTime? fromDate, DateTime? toDate);
    Task<Dictionary<string, object>> ImportScheduleAsync(ApsImportScheduleRequest request);
}

public class ApsIntegrationService : IApsIntegrationService
{
    private readonly ErpGeneralDbContext _db;

    public ApsIntegrationService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<string, object>> ExportDataAsync(DateTime? fromDate, DateTime? toDate)
    {
        var skuLookup = await _db.Skus
            .ToDictionaryAsync(s => s.Id, s => s.SkuNumber ?? "");

        var data = new ApsExportDataDto();

        data.Items = await _db.Skus
            .Select(s => new ApsItemDto
            {
                SkuCode = s.SkuNumber ?? "",
                SkuName = s.SkuName ?? "",
                BaseUom = s.Unit ?? "",
                Category = s.Category ?? ""
            })
            .ToListAsync();

        var bomRows = await _db.BomItems
            .Include(bi => bi.Bom)
            .Select(bi => new { bi.Bom.ProductSkuId, bi.ChildSkuId, bi.Quantity, bi.Unit })
            .ToListAsync();

        data.Boms = bomRows.Select(bi => new ApsBomDto
        {
            ParentSkuCode = skuLookup.TryGetValue(bi.ProductSkuId, out var parentCode) ? parentCode : "",
            ChildSkuCode = skuLookup.TryGetValue(bi.ChildSkuId, out var childCode) ? childCode : "",
            Quantity = bi.Quantity,
            Unit = bi.Unit ?? ""
        }).ToList();

        data.Resources = await _db.RccpResources
            .Select(r => new ApsResourceDto { ResourceCode = r.ResourceCode, ResourceName = r.ResourceName, CapacityPerBucket = r.CapacityPerBucket })
            .ToListAsync();

        var woQuery = _db.WorkOrders
            .Include(wo => wo.ProductSku)
            .Where(wo => wo.Status == "pending" || wo.Status == "released");

        if (fromDate.HasValue)
            woQuery = woQuery.Where(wo => wo.PlannedEndDate >= fromDate.Value);
        if (toDate.HasValue)
            woQuery = woQuery.Where(wo => wo.PlannedEndDate <= toDate.Value);

        data.WorkOrders = await woQuery
            .Select(wo => new ApsWorkOrderDto
            {
                WoNumber = wo.WoNumber ?? "",
                SkuCode = wo.ProductSku != null ? wo.ProductSku.SkuNumber : "",
                PlannedQty = wo.PlannedQty,
                RequestDate = wo.PlannedEndDate ?? DateTime.Now,
                Priority = wo.Priority,
                Status = wo.Status ?? ""
            }).ToListAsync();

        return new Dictionary<string, object> { ["code"] = 200, ["message"] = "Export success", ["data"] = data };
    }

    public async Task<Dictionary<string, object>> ImportScheduleAsync(ApsImportScheduleRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.ScheduleVersion))
            return new Dictionary<string, object> { ["code"] = 400, ["message"] = "Invalid schedule data" };

        if (request.Results == null || !request.Results.Any())
            return new Dictionary<string, object> { ["code"] = 400, ["message"] = "No schedule results provided" };

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var result in request.Results)
            {
                _db.ApsScheduleResults.Add(new ApsScheduleResult
                {
                    OrderNo = result.OrderNo!,
                    OperationSeq = result.OperationSeq,
                    ResourceCode = result.ResourceCode!,
                    ScheduledStart = result.StartTime ?? DateTime.Now,
                    ScheduledEnd = result.EndTime ?? DateTime.Now,
                    Sequence = result.Sequence ?? 0,
                    ScheduleVersion = request.ScheduleVersion
                });

                var wo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.WoNumber == result.OrderNo);
                if (wo != null && !wo.IsScheduleLocked)
                {
                    if (result.OperationSeq == null)
                    {
                        wo.ScheduledStartDate = result.StartTime;
                        wo.ScheduledEndDate = result.EndTime;
                        wo.ScheduledResourceCode = result.ResourceCode;
                        wo.ScheduleVersion = request.ScheduleVersion;
                    }
                }

                if (wo != null && result.OperationSeq != null)
                {
                    var op = await _db.WorkOrderOperations
                        .FirstOrDefaultAsync(o => o.WorkOrderId == wo.Id && o.OperationSeq == result.OperationSeq);
                    if (op != null)
                    {
                        op.ScheduledStartDate = result.StartTime;
                        op.ScheduledEndDate = result.EndTime;
                        op.ScheduledSequence = result.Sequence;
                    }
                }
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new Dictionary<string, object> { ["code"] = 200, ["message"] = $"Schedule version {request.ScheduleVersion} imported successfully" };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new Dictionary<string, object> { ["code"] = 500, ["message"] = ex.Message };
        }
    }
}
