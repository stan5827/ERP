using System.Text.Json;
using ErpBackend.Data;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Services;

public interface IErpIntegrationService
{
    Task<ToolResult> CreateWorkOrderAsync(string skuId, decimal quantity, string? priority = null);
    Task<ToolResult> UpdateWorkOrderStatusAsync(string woId, string status);
    Task<ToolResult> CompleteWorkOrderAsync(string woId, decimal quantity);
    Task<ToolResult> CreatePurchaseRequisitionAsync(string supplierId, List<PrItemDto> items);
    Task<ToolResult> AdjustInventoryAsync(string skuId, string warehouseId, decimal quantity, string reason);
    Task<ToolResult> SearchInventoryAsync(string? skuCode, string? warehouseId);
    Task<ToolResult> SearchWorkOrdersAsync(string? woNumber, string? status);
    Task<ToolResult> SearchPurchaseOrdersAsync(string? poNumber, string? status);
    Task<ToolResult> GenerateInventoryReportAsync(string? warehouseId, DateTime? dateFrom, DateTime? dateTo);
}

public class PrItemDto
{
    public string SkuId { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Notes { get; set; }
}

public class ErpIntegrationService : IErpIntegrationService
{
    private readonly ErpGeneralDbContext _db;
    private readonly ILogger<ErpIntegrationService> _logger;

    public ErpIntegrationService(ErpGeneralDbContext db, ILogger<ErpIntegrationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ToolResult> CreateWorkOrderAsync(string skuId, decimal quantity, string? priority = null)
    {
        try
        {
            var sku = await _db.Skus.FindAsync(Guid.Parse(skuId));
            if (sku == null)
                return new ToolResult { Success = false, Error = $"SKU {skuId} not found" };

            var woNumber = $"WO-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            var workOrder = new ErpBackend.Models.MES.WorkOrder
            {
                WoNumber = woNumber,
                ProductSkuId = Guid.Parse(skuId),
                PlannedQty = quantity,
                Priority = priority ?? "normal",
                Status = "pending",
                PlannedStartDate = DateTime.Today,
                PlannedEndDate = DateTime.Today.AddDays(7)
            };

            _db.WorkOrders.Add(workOrder);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created WorkOrder: {WoNumber}", woNumber);

            return new ToolResult
            {
                Success = true,
                Data = JsonSerializer.Serialize(new
                {
                    WorkOrderId = workOrder.Id,
                    WoNumber = woNumber,
                    Status = "created"
                })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create work order");
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ToolResult> UpdateWorkOrderStatusAsync(string woId, string status)
    {
        try
        {
            var wo = await _db.WorkOrders.FindAsync(Guid.Parse(woId));
            if (wo == null)
                return new ToolResult { Success = false, Error = $"WorkOrder {woId} not found" };

            wo.Status = status;
            wo.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new ToolResult
            {
                Success = true,
                Data = JsonSerializer.Serialize(new { WorkOrderId = woId, Status = status, UpdatedAt = DateTime.UtcNow })
            };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ToolResult> CompleteWorkOrderAsync(string woId, decimal quantity)
    {
        try
        {
            var wo = await _db.WorkOrders.FindAsync(Guid.Parse(woId));
            if (wo == null)
                return new ToolResult { Success = false, Error = $"WorkOrder {woId} not found" };

            wo.Status = "completed";
            wo.ActualQty = quantity;
            wo.ActualStartDate = wo.PlannedStartDate;
            wo.ActualEndDate = DateTime.UtcNow;
            wo.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new ToolResult
            {
                Success = true,
                Data = JsonSerializer.Serialize(new { WorkOrderId = woId, Status = "completed", CompletedAt = DateTime.UtcNow })
            };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ToolResult> CreatePurchaseRequisitionAsync(string supplierId, List<PrItemDto> items)
    {
        try
        {
            var prNumber = $"PR-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            var totalAmount = items.Sum(i => i.Quantity * 100);

            var pr = new ErpBackend.Models.Procurement.PurchaseRequisition
            {
                PrNumber = prNumber,
                PrDate = DateTime.Today,
                Status = "draft",
                TotalEstimatedAmount = totalAmount
            };

            _db.PurchaseRequisitions.Add(pr);
            await _db.SaveChangesAsync();

            return new ToolResult
            {
                Success = true,
                Data = JsonSerializer.Serialize(new { PrNumber = prNumber, Status = "draft", ItemsCount = items.Count })
            };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ToolResult> AdjustInventoryAsync(string skuId, string warehouseId, decimal quantity, string reason)
    {
        try
        {
            var sku = await _db.Skus.FindAsync(Guid.Parse(skuId));
            if (sku == null)
                return new ToolResult { Success = false, Error = $"SKU {skuId} not found" };

            sku.Qty += quantity;
            sku.UpdatedAt = DateTime.UtcNow;

            var transaction = new ErpBackend.Models.Inventory.InventoryTransaction
            {
                TrxId = $"ADJ-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Type = "adjustment",
                SkuId = Guid.Parse(skuId),
                QtyChange = quantity,
                QtyBefore = sku.Qty,
                QtyAfter = sku.Qty + quantity,
                RelatedDocType = reason ?? "manual_adjustment"
            };

            _db.InventoryTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            return new ToolResult
            {
                Success = true,
                Data = JsonSerializer.Serialize(new
                {
                    AdjustmentId = transaction.Id,
                    NewQuantity = sku.Qty,
                    AdjustedAt = DateTime.UtcNow
                })
            };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ToolResult> SearchInventoryAsync(string? skuCode, string? warehouseId)
    {
        try
        {
            var query = _db.Skus.AsQueryable();

            if (!string.IsNullOrEmpty(skuCode))
                query = query.Where(s => s.SkuNumber.Contains(skuCode));

            var results = await query.Take(50).Select(s => new
            {
                s.SkuNumber,
                s.SkuName,
                s.Qty,
                s.Unit,
                s.StandardCost,
                s.SafetyStock
            }).ToListAsync();

            return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ToolResult> SearchWorkOrdersAsync(string? woNumber, string? status)
    {
        try
        {
            var query = _db.WorkOrders.AsQueryable();

            if (!string.IsNullOrEmpty(woNumber))
                query = query.Where(w => w.WoNumber.Contains(woNumber));
            if (!string.IsNullOrEmpty(status))
                query = query.Where(w => w.Status == status);

            var results = await query.Take(50).Select(w => new
            {
                w.WoNumber,
                w.PlannedQty,
                w.ActualQty,
                w.Status,
                w.Priority,
                w.PlannedStartDate,
                w.PlannedEndDate
            }).ToListAsync();

            return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ToolResult> SearchPurchaseOrdersAsync(string? poNumber, string? status)
    {
        try
        {
            var query = _db.PurchaseOrders.AsQueryable();

            if (!string.IsNullOrEmpty(poNumber))
                query = query.Where(p => p.PoNumber.Contains(poNumber));
            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var results = await query.Take(50).Select(p => new
            {
                p.PoNumber,
                p.PoDate,
                p.Status,
                p.TotalAmount
            }).ToListAsync();

            return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<ToolResult> GenerateInventoryReportAsync(string? warehouseId, DateTime? dateFrom, DateTime? dateTo)
    {
        try
        {
            var query = _db.Skus.AsQueryable();

            var totalSkus = await query.CountAsync();
            var lowStockSkus = await query.CountAsync(s => s.Qty <= s.SafetyStock);
            var totalValue = await query.SumAsync(s => s.Qty * s.StandardCost);

            var report = new
            {
                ReportId = Guid.NewGuid(),
                ReportType = "inventory",
                GeneratedAt = DateTime.UtcNow,
                Summary = new
                {
                    TotalSKUs = totalSkus,
                    LowStockItems = lowStockSkus,
                    TotalInventoryValue = totalValue
                },
                Format = "PDF"
            };

            return new ToolResult { Success = true, Data = JsonSerializer.Serialize(report) };
        }
        catch (Exception ex)
        {
            return new ToolResult { Success = false, Error = ex.Message };
        }
    }
}