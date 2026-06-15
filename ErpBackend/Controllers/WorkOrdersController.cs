using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;
using ErpBackend.Helpers;

namespace ErpBackend.Controllers;

public class WorkOrdersController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public WorkOrdersController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WorkOrderDto>>>> GetWorkOrders([FromQuery] string? status)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.WorkOrders.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(w => w.Status == status);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var skus = await _db.Skus.ToDictionaryAsync(s => s.Id);

        var dtos = items.Select(w => new WorkOrderDto
        {
            Id = w.Id,
            WoNumber = w.WoNumber,
            ProductSkuId = w.ProductSkuId,
            ProductSkuName = skus.GetValueOrDefault(w.ProductSkuId)?.SkuName,
            PlannedQty = w.PlannedQty,
            ActualQty = w.ActualQty,
            Status = w.Status,
            Priority = w.Priority,
            PlannedStartDate = w.PlannedStartDate,
            PlannedEndDate = w.PlannedEndDate,
            CreatedAt = w.CreatedAt
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> GetWorkOrder(Guid id)
    {
        var w = await _db.WorkOrders.FindAsync(id);
        if (w == null) return NotFound(ApiResponse.Fail("工單不存在"));

        var sku = await _db.Skus.FindAsync(w.ProductSkuId);
        return Ok(new WorkOrderDto
        {
            Id = w.Id,
            WoNumber = w.WoNumber,
            ProductSkuId = w.ProductSkuId,
            ProductSkuName = sku?.SkuName,
            PlannedQty = w.PlannedQty,
            ActualQty = w.ActualQty,
            Status = w.Status,
            Priority = w.Priority,
            PlannedStartDate = w.PlannedStartDate,
            PlannedEndDate = w.PlannedEndDate,
            ActualStartDate = w.ActualStartDate,
            ActualEndDate = w.ActualEndDate,
            CreatedAt = w.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<WorkOrderDto>>> CreateWorkOrder([FromBody] CreateWorkOrderRequest req)
    {
        var sku = await _db.Skus.FindAsync(req.ProductSkuId);
        if (sku == null) return BadRequest(ApiResponse.Fail("品號不存在"));

        var wo = new WorkOrder
        {
            Id = Guid.NewGuid(),
            WoNumber = DocumentNumberGenerator.GenerateWorkOrder(),
            ProductSkuId = req.ProductSkuId,
            PlannedQty = req.PlannedQty,
            Priority = req.Priority ?? "normal",
            PlannedStartDate = req.PlannedStartDate,
            PlannedEndDate = req.PlannedEndDate,
            Status = "planned"
        };

        _db.WorkOrders.Add(wo);
        await _db.SaveChangesAsync();

        return Ok(new WorkOrderDto
        {
            Id = wo.Id,
            WoNumber = wo.WoNumber,
            ProductSkuId = wo.ProductSkuId,
            ProductSkuName = sku.SkuName,
            PlannedQty = wo.PlannedQty,
            Status = wo.Status,
            Priority = wo.Priority,
            PlannedStartDate = wo.PlannedStartDate,
            PlannedEndDate = wo.PlannedEndDate,
            CreatedAt = wo.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdateWorkOrder(Guid id, [FromBody] UpdateWorkOrderRequest req)
    {
        var wo = await _db.WorkOrders.FindAsync(id);
        if (wo == null) return NotFound(ApiResponse.Fail("工單不存在"));

        if (req.ProductSkuId != null) wo.ProductSkuId = req.ProductSkuId.Value;
        if (req.PlannedQty != null) wo.PlannedQty = req.PlannedQty.Value;
        if (req.Priority != null) wo.Priority = req.Priority;
        if (req.PlannedStartDate != null) wo.PlannedStartDate = req.PlannedStartDate;
        if (req.PlannedEndDate != null) wo.PlannedEndDate = req.PlannedEndDate;
        wo.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok("工單已更新");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteWorkOrder(Guid id)
    {
        var wo = await _db.WorkOrders.FindAsync(id);
        if (wo == null) return NotFound(ApiResponse.Fail("工單不存在"));

        if (wo.Status != "planned")
            return BadRequest(ApiResponse.Fail("只能刪除草稿狀態的工單"));

        _db.WorkOrders.Remove(wo);
        await _db.SaveChangesAsync();
        return Ok("工單已刪除");
    }
}
