using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;

namespace ErpBackend.Controllers;

public class EcnsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public EcnsController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<EcnDto>>>> GetEcns([FromQuery] string? status)
    {
        var query = _db.Ecns.Include(e => e.AffectedBoms).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(e => e.Status == status);
        
        var list = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        var dtos = list.Select(e => new EcnDto
        {
            Id = e.Id,
            EcnNumber = e.EcnNumber,
            EcrId = e.EcrId,
            ChangeDescription = e.ChangeDescription,
            EffectiveDate = e.EffectiveDate,
            Status = e.Status,
            CreatedAt = e.CreatedAt,
            AffectedBoms = e.AffectedBoms.Select(ab => new EcnAffectedBomDto
            {
                Id = ab.Id,
                BomId = ab.BomId,
                ActionType = ab.ActionType
            }).ToList()
        }).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<EcnDto>>> GetEcn(Guid id)
    {
        var ecn = await _db.Ecns.Include(e => e.AffectedBoms).FirstOrDefaultAsync(e => e.Id == id);
        if (ecn == null) return NotFound(ApiResponse<string>.Fail("ECN not found"));
        var dto = new EcnDto
        {
            Id = ecn.Id,
            EcnNumber = ecn.EcnNumber,
            EcrId = ecn.EcrId,
            ChangeDescription = ecn.ChangeDescription,
            EffectiveDate = ecn.EffectiveDate,
            Status = ecn.Status,
            CreatedAt = ecn.CreatedAt,
            AffectedBoms = ecn.AffectedBoms.Select(ab => new EcnAffectedBomDto
            {
                Id = ab.Id,
                BomId = ab.BomId,
                ActionType = ab.ActionType
            }).ToList()
        };
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateEcn([FromBody] CreateEcnRequest req)
    {
        var ecn = new Ecn
        {
            Id = Guid.NewGuid(),
            EcnNumber = string.IsNullOrEmpty(req.EcnNumber) ? $"ECN{DateTime.Now:yyyyMMddHHmmss}" : req.EcnNumber,
            EcrId = req.EcrId != Guid.Empty ? req.EcrId : Guid.Empty,
            ChangeDescription = req.ChangeDescription,
            EffectiveDate = req.EffectiveDate == default ? DateTime.Now : req.EffectiveDate,
            Status = "draft",
            CreatedAt = DateTime.UtcNow
        };
        foreach (var b in req.AffectedBoms)
        {
            ecn.AffectedBoms.Add(new EcnAffectedBom
            {
                Id = Guid.NewGuid(),
                EcnId = ecn.Id,
                BomId = b.BomId,
                ActionType = b.ActionType
            });
        }
        _db.Ecns.Add(ecn);
        await _db.SaveChangesAsync();
        return Ok(ecn.Id);
    }

    [HttpGet("{id}/impact")]
    public ActionResult<ApiResponse<EcnImpactDto>> GetEcnImpact(Guid id)
    {
        // Mock impact analysis for UI demo
        var impact = new EcnImpactDto
        {
            EcnId = id,
            TotalLossAmount = 150000m,
            AffectedItemCount = 3,
            AffectedWorkOrderCount = 2,
            AffectedPurchaseOrderCount = 1,
            SuggestedEffectiveDate = DateTime.Now.AddDays(7),
            Details = new List<EcnImpactItemDto>
            {
                new EcnImpactItemDto { SkuNumber = "SKU-OLD-01", SkuName = "Old Component A", CurrentStock = 100, UnitCost = 500, PotentialLoss = 50000, ImpactType = "obsolete" },
                new EcnImpactItemDto { SkuNumber = "SKU-OLD-02", SkuName = "Old Component B", CurrentStock = 50, UnitCost = 2000, PotentialLoss = 100000, ImpactType = "slow_moving" }
            },
            AffectedWorkOrders = new List<EcnImpactWorkOrderDto>
            {
                new EcnImpactWorkOrderDto { WorkOrderId = Guid.NewGuid(), WoNumber = "WO-20260401-01", Status = "in_progress", PlannedQty = 500, ProductSkuLabel = "Product A" },
                new EcnImpactWorkOrderDto { WorkOrderId = Guid.NewGuid(), WoNumber = "WO-20260402-02", Status = "planned", PlannedQty = 300, ProductSkuLabel = "Product B" }
            }
        };
        return Ok(impact);
    }
}
