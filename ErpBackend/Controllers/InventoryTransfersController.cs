using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Controllers;

[Route("api/erp/[controller]")]
public class InventoryTransfersController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public InventoryTransfersController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InventoryTransferDto>>>> GetTransfers([FromQuery] string? status)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.InventoryTransfers
            .Include(t => t.SourceWarehouse)
            .Include(t => t.TargetWarehouse)
            .Include(t => t.CreatedByEmployee)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(t => new InventoryTransferDto
        {
            Id = t.Id,
            TransferNumber = t.TransferNumber,
            SourceWarehouseId = t.SourceWarehouseId,
            SourceWarehouseName = t.SourceWarehouse?.WarehouseName,
            TargetWarehouseId = t.TargetWarehouseId,
            TargetWarehouseName = t.TargetWarehouse?.WarehouseName,
            Status = t.Status,
            TransferDate = t.TransferDate,
            CreatedByName = t.CreatedByEmployee?.ChineseName,
            CreatedAt = t.CreatedAt
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<InventoryTransferDto>>> GetTransfer(Guid id)
    {
        var t = await _db.InventoryTransfers
            .Include(t => t.SourceWarehouse)
            .Include(t => t.TargetWarehouse)
            .Include(t => t.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (t == null) return NotFound(ApiResponse.Fail("調撥單不存在"));

        var dto = new InventoryTransferDto
        {
            Id = t.Id,
            TransferNumber = t.TransferNumber,
            SourceWarehouseId = t.SourceWarehouseId,
            SourceWarehouseName = t.SourceWarehouse?.WarehouseName,
            TargetWarehouseId = t.TargetWarehouseId,
            TargetWarehouseName = t.TargetWarehouse?.WarehouseName,
            Status = t.Status,
            TransferDate = t.TransferDate,
            Items = t.Items.Select(i => new InventoryTransferItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                Qty = i.TransferQty,
                LotNumber = i.LotNumber
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InventoryTransferDto>>> CreateTransfer(CreateInventoryTransferRequest request)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var transfer = new InventoryTransfer
            {
                Id = Guid.NewGuid(),
                TransferNumber = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}",
                SourceWarehouseId = request.SourceWarehouseId,
                TargetWarehouseId = request.TargetWarehouseId,
                TransferDate = DateTime.UtcNow,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in request.Items)
            {
                transfer.Items.Add(new InventoryTransferItem
                {
                    Id = Guid.NewGuid(),
                    SkuId = item.SkuId,
                    TransferQty = item.Qty,
                    LotNumber = item.LotNumber
                });
            }

            _db.InventoryTransfers.Add(transfer);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new InventoryTransferDto { Id = transfer.Id, TransferNumber = transfer.TransferNumber });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<ApiResponse>> ConfirmTransfer(Guid id)
    {
        var transfer = await _db.InventoryTransfers
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transfer == null) return NotFound(ApiResponse.Fail("調撥單不存在"));
        if (transfer.Status != "PENDING") return BadRequest(ApiResponse.Fail("僅待處理狀態可確認"));

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in transfer.Items)
            {
                // 這裡應該呼叫 InventoryService 進行實際扣減與增加
                // 暫時簡化邏輯
                var sourceStock = await _db.InventoryTransactions
                    .Where(it => it.SkuId == item.SkuId && it.WarehouseId == transfer.SourceWarehouseId)
                    .SumAsync(it => it.Qty);

                if (sourceStock < item.TransferQty)
                    throw new Exception($"庫存不足: SKU ID {item.SkuId}");

                // 扣減來源
                _db.InventoryTransactions.Add(new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    SkuId = item.SkuId,
                    WarehouseId = transfer.SourceWarehouseId,
                    Qty = -item.TransferQty,
                    TransactionType = "TRANSFER_OUT",
                    ReferenceId = transfer.Id,
                    TransactionDate = DateTime.UtcNow
                });

                // 增加目標
                _db.InventoryTransactions.Add(new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    SkuId = item.SkuId,
                    WarehouseId = transfer.TargetWarehouseId,
                    Qty = item.TransferQty,
                    TransactionType = "TRANSFER_IN",
                    ReferenceId = transfer.Id,
                    TransactionDate = DateTime.UtcNow
                });
            }

            transfer.Status = "CONFIRMED";
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok("調撥確認成功");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }
}
