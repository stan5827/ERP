using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Models.Procurement;
using ErpBackend.Helpers;

namespace ErpBackend.Services;

public interface IMrpProcessLinkageService
{
    Task<(bool Success, string Message, string? DocumentNumber)> ConvertToExecutionAsync(Guid plannedOrderId);
    Task TriggerApsRescheduleAsync(Guid workOrderId);
}

public class MrpProcessLinkageService : IMrpProcessLinkageService
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<MrpProcessLinkageService> _logger;

    public MrpProcessLinkageService(ErpGeneralDbContext db, ILogger<MrpProcessLinkageService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? DocumentNumber)> ConvertToExecutionAsync(Guid plannedOrderId)
    {
        var plannedOrder = await _db.MrpPlannedOrders
            .Include(o => o.Sku)
            .FirstOrDefaultAsync(o => o.Id == plannedOrderId);

        if (plannedOrder == null)
            return (false, "找不到指定的計畫訂單", null);

        if (plannedOrder.Status != "suggested")
            return (false, $"計畫訂單狀態為 {plannedOrder.Status}，無法重覆轉換", null);

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            (bool Success, string Message, string? DocumentNumber) result;
            if (plannedOrder.OrderType == "work_order")
            {
                result = await CreateWorkOrderAsync(plannedOrder);
            }
            else
            {
                result = await CreatePurchaseRequisitionAsync(plannedOrder);
            }
            
            if (result.Success)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "轉換計畫訂單時發生錯誤：{Id}", plannedOrderId);
            return (false, $"系統錯誤：{ex.Message}", null);
        }
    }

    private async Task<(bool Success, string Message, string? DocumentNumber)> CreateWorkOrderAsync(MrpPlannedOrder order)
    {
        // 1. 產生工單單號（使用安全的單號產生器，防止重複）
        var woNumber = DocumentNumberGenerator.GenerateWorkOrder();
        
        // 2. 獲取產品 BOM
        var bom = await _db.Boms
            .Include(b => b.Items)
            .Where(b => b.ProductSkuId == order.SkuId && (b.Status == "active" || b.Status == "approved"))
            .OrderByDescending(b => b.Version)
            .FirstOrDefaultAsync();

        var wo = new WorkOrder
        {
            WoNumber = woNumber,
            ProductSkuId = order.SkuId,
            BomId = bom?.Id,
            BomVersion = bom?.Version,
            PlannedQty = order.OrderQty,
            PlannedStartDate = order.PlannedStartDate,
            PlannedEndDate = order.PlannedEndDate,
            MrpPlannedStartDate = order.PlannedStartDate,
            MrpPlannedDueDate = order.PlannedEndDate,
            Status = "pending",
            Notes = $"由 MRP 計畫 {order.MrpRunId} 轉換產生"
        };

        _db.WorkOrders.Add(wo);

        // 3.5. 修復 MRP→APS 數據斷層：工單建立後自動觸發排程鉤子
        await TriggerApsRescheduleAsync(wo.Id);

        // 4. 展開 BOM 建立發料需求
        if (bom != null)
        {
            foreach (var item in bom.Items)
            {
                var material = new WorkOrderMaterial
                {
                    WorkOrderId = wo.Id,
                    SkuId = item.ChildSkuId,
                    BomStandardQty = item.Quantity,
                    RequiredQty = item.Quantity * order.OrderQty,
                    IssuedQty = 0,
                    Unit = item.Unit,
                    Status = "pending"
                };
                _db.WorkOrderMaterials.Add(material);
            }
        }

        // 4. 更新計畫訂單狀態
        order.Status = "confirmed";
        order.ConvertedDocumentId = wo.Id;
        order.ConvertedDocumentNumber = woNumber;

        await _db.SaveChangesAsync();
        return (true, "工單建立成功", woNumber);
    }

    private async Task<(bool Success, string Message, string? DocumentNumber)> CreatePurchaseRequisitionAsync(MrpPlannedOrder order)
    {
        // 1. 產生 PR 單號（使用安全的單號產生器，防止重複）
        var prNumber = DocumentNumberGenerator.GeneratePurchaseRequisition();
        
        var pr = new PurchaseRequisition
        {
            PrNumber = prNumber,
            PrDate = DateTime.UtcNow,
            Purpose = $"由 MRP 計畫 {order.MrpRunId} 轉換產生",
            Status = "pending",
            TotalEstimatedAmount = 0
        };

        _db.PurchaseRequisitions.Add(pr);

        var prItem = new PurchaseRequisitionItem
        {
            PurchaseRequisitionId = pr.Id,
            SkuId = order.SkuId,
            Quantity = order.OrderQty,
            Unit = order.Sku?.Unit,
            RequiredDate = order.PlannedEndDate,
            Notes = $"對應計畫日期：{order.PlannedEndDate:yyyy-MM-dd}"
        };
        _db.PurchaseRequisitionItems.Add(prItem);

        // 2. 更新計畫訂單狀態
        order.Status = "confirmed";
        order.ConvertedDocumentId = pr.Id;
        order.ConvertedDocumentNumber = prNumber;

        await _db.SaveChangesAsync();
        return (true, "採購申請建立成功", prNumber);
    }
    
    /// <summary>
    /// 修復 MRP→APS 數據斷層：工單建立後自動觸發 APS 排程更新
    /// </summary>
    public async Task TriggerApsRescheduleAsync(Guid workOrderId)
    {
        try
        {
            var workOrder = await _db.WorkOrders.FindAsync(workOrderId);
            if (workOrder == null) return;
            
            // 標記工單為需要重新排程
            workOrder.NeedsRescheduling = true;
            workOrder.RescheduleTriggeredAt = DateTime.UtcNow;
            
            await _db.SaveChangesAsync();
            _logger.LogInformation("工單 {WoNumber} 已標記為需要重新排程 (MRP→APS 鉤子觸發)", workOrder.WoNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "觸發 APS 重新排程失敗：{WorkOrderId}", workOrderId);
        }
    }
}
