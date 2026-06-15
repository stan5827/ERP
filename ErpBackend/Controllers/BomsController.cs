using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/boms")]
public class BomsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public BomsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BomDto>>>> GetBoms([FromQuery] Guid? productSkuId)
    {
        var query = _db.Boms
            .Include(b => b.Items)
            .AsQueryable();

        if (productSkuId.HasValue)
            query = query.Where(b => b.ProductSkuId == productSkuId.Value);

        var items = await query
            .OrderBy(b => b.BomCode)
            .Select(b => new BomDto
            {
                Id = b.Id,
                BomCode = b.BomCode,
                ProductSkuId = b.ProductSkuId,
                Items = b.Items.Select(i => new BomItemDto
                {
                    Id = i.Id,
                    ChildSkuId = i.ChildSkuId,
                    Quantity = i.Quantity,
                    Unit = i.Unit
                }).ToList()
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BomDto>>> GetBom(Guid id)
    {
        var b = await _db.Boms
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (b == null) return NotFound(ApiHelper.FailOf<BomDto>("找不到物料清單 (BOM)"));

        // Fetch child SKU details
        var itemDtos = new List<BomItemDto>();
        foreach (var item in b.Items)
        {
            var sku = await _db.Skus.FindAsync(item.ChildSkuId);
            itemDtos.Add(new BomItemDto
            {
                Id = item.Id,
                ChildSkuId = item.ChildSkuId,
                ChildSkuNumber = sku?.SkuNumber,
                ChildSkuName = sku?.SkuName,
                Quantity = item.Quantity,
                Unit = item.Unit
            });
        }

        return Ok(new BomDto
        {
            Id = b.Id,
            BomCode = b.BomCode,
            ProductSkuId = b.ProductSkuId,
            Items = itemDtos
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BomDto>>> CreateBom([FromBody] CreateBomRequest request)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var bom = new Bom
            {
                BomCode = request.BomCode,
                ProductSkuId = request.ProductSkuId,
                Version = request.Version,
                Status = request.Status,
                EffectiveDate = request.EffectiveDate,
                Notes = request.Notes
            };

            _db.Boms.Add(bom);
            await _db.SaveChangesAsync();

            if (request.Items != null)
            {
                foreach (var itemReq in request.Items)
                {
                    _db.BomItems.Add(new BomItem
                    {
                        BomId = bom.Id,
                        ChildSkuId = itemReq.ChildSkuId,
                        Quantity = itemReq.Quantity,
                        Unit = itemReq.Unit,
                        ScrapRate = itemReq.ScrapRate,
                        OperationSeq = itemReq.OperationSeq
                    });
                }
                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            return await GetBom(bom.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiHelper.FailOf<BomDto>($"建立 BOM 失敗: {ex.Message}"));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<BomDto>>> UpdateBom(Guid id, [FromBody] UpdateBomRequest request)
    {
        var bom = await _db.Boms
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bom == null) return NotFound(ApiHelper.FailOf<BomDto>("找不到 BOM"));

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (request.BomCode != null) bom.BomCode = request.BomCode;
            if (request.Version != null) bom.Version = request.Version;
            if (request.Status != null) bom.Status = request.Status;
            if (request.EffectiveDate.HasValue) bom.EffectiveDate = request.EffectiveDate.Value;
            if (request.Notes != null) bom.Notes = request.Notes;

            if (request.Items != null)
            {
                _db.BomItems.RemoveRange(bom.Items);
                foreach (var itemReq in request.Items)
                {
                    _db.BomItems.Add(new BomItem
                    {
                        BomId = bom.Id,
                        ChildSkuId = itemReq.ChildSkuId,
                        Quantity = itemReq.Quantity,
                        Unit = itemReq.Unit,
                        ScrapRate = itemReq.ScrapRate,
                        OperationSeq = itemReq.OperationSeq
                    });
                }
            }

            bom.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetBom(id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, ApiHelper.FailOf<BomDto>($"更新 BOM 失敗: {ex.Message}"));
        }
    }
}
