using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/aps")]
public class ApsIntegrationController : ControllerBase
{
    private readonly ErpGeneralDbContext _context;

    public ApsIntegrationController(ErpGeneralDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 匯出 APS 引擎所需的排程基礎資料
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportData([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var data = new ApsExportDataDto();

        data.Items = await _context.Skus
            .Select(s => new ApsItemDto 
            { 
                SkuCode = s.SkuNumber ?? "", 
                SkuName = s.SkuName ?? "", 
                BaseUom = s.Unit ?? "", 
                Category = s.Category ?? "" 
            })
            .ToListAsync();

        data.Boms = await _context.BomItems
            .Include(bi => bi.Bom)
            .Select(bi => new ApsBomDto 
            { 
                ParentSkuCode = _context.Skus.Where(s => s.Id == bi.Bom.ProductSkuId).Select(s => s.SkuNumber).FirstOrDefault() ?? "",
                ChildSkuCode = _context.Skus.Where(s => s.Id == bi.ChildSkuId).Select(s => s.SkuNumber).FirstOrDefault() ?? "",
                Quantity = bi.Quantity,
                Unit = bi.Unit ?? ""
            }).ToListAsync();

        data.Resources = await _context.RccpResources
            .Select(r => new ApsResourceDto { ResourceCode = r.ResourceCode, ResourceName = r.ResourceName, CapacityPerBucket = r.CapacityPerBucket })
            .ToListAsync();

        data.WorkOrders = await _context.WorkOrders
            .Include(wo => wo.ProductSku)
            .Where(wo => wo.Status == "pending" || wo.Status == "released")
            .Select(wo => new ApsWorkOrderDto
            {
                WoNumber = wo.WoNumber ?? "",
                SkuCode = wo.ProductSku != null ? wo.ProductSku.SkuNumber : "",
                PlannedQty = wo.PlannedQty,
                RequestDate = wo.PlannedEndDate,
                Priority = wo.Priority,
                Status = wo.Status ?? ""
            }).ToListAsync();

        return Ok(new { code = 200, message = "Export success", data });
    }

    /// <summary>
    /// 接收 APS 算出的詳細排程結果並更新工單與工序
    /// </summary>
    [HttpPost("import-schedule")]
    public async Task<IActionResult> ImportSchedule([FromBody] ApsImportScheduleRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.ScheduleVersion))
            return BadRequest(new { code = 400, message = "Invalid schedule data" });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var result in request.Results)
            {
                // 1. 記錄 audit trail
                _context.ApsScheduleResults.Add(new ApsScheduleResult
                {
                    OrderNo = result.OrderNo,
                    OperationSeq = result.OperationSeq,
                    ResourceCode = result.ResourceCode,
                    ScheduledStart = result.StartTime,
                    ScheduledEnd = result.EndTime,
                    Sequence = result.Sequence,
                    ScheduleVersion = request.ScheduleVersion
                });

                // 2. 更新工單層級
                var wo = await _context.WorkOrders.FirstOrDefaultAsync(w => w.WoNumber == result.OrderNo);
                if (wo != null && !wo.IsScheduleLocked)
                {
                    // 若無工序編號，代表整張工單排程，獲取最早與最晚時間
                    if (result.OperationSeq == null)
                    {
                        wo.ScheduledStartDate = result.StartTime;
                        wo.ScheduledEndDate = result.EndTime;
                        wo.ScheduledResourceCode = result.ResourceCode;
                        wo.ScheduleVersion = request.ScheduleVersion;
                    }
                }

                // 3. 更新工序層級
                if (wo != null && result.OperationSeq != null)
                {
                    var op = await _context.WorkOrderOperations
                        .FirstOrDefaultAsync(o => o.WorkOrderId == wo.Id && o.OperationSeq == result.OperationSeq);
                    if (op != null)
                    {
                        op.ScheduledStartDate = result.StartTime;
                        op.ScheduledEndDate = result.EndTime;
                        op.ScheduledSequence = result.Sequence;
                        // 查找資源 ID (簡略做法，實際應對應 ResourceCode -> RccpResource.Id)
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { code = 200, message = $"Schedule version {request.ScheduleVersion} imported successfully" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { code = 500, message = ex.Message });
        }
    }
}
