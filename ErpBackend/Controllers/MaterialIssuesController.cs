using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Inventory;
using ErpBackend.Services;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;
using ErpBackend.Helpers;

namespace ErpBackend.Controllers;

public class MaterialIssuesController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    private readonly ICostingService _costing;

    public MaterialIssuesController(ErpGeneralDbContext db, ICostingService costing)
    {
        _db = db;
        _costing = costing;
    }

    /// <summary>
    /// 提交工單領料 (Submit Material Issue)
    /// 扣減庫存並同步更新實際成本
    /// </summary>
    [HttpPost("submit")]
    public async Task<ActionResult<ApiResponse>> SubmitIssue([FromBody] MaterialIssueRequest request)
    {
        var wo = await _db.WorkOrders.FirstOrDefaultAsync(w => w.WoNumber == request.WoNumber);
        if (wo == null) return NotFound(ApiHelper.Fail("Error"));

        var sku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuNumber == request.SkuCode);
        if (sku == null) return NotFound(ApiHelper.Fail("Error"));

        if (sku.Qty < request.Quantity)
            return BadRequest(ApiResponse.Fail($"庫存不足，目前庫存 {sku.Qty}"));

        // 1. 建立領料實體記錄
        var issue = new MaterialIssue
        {
            IssueNumber = DocumentNumberGenerator.GenerateMaterialIssue(),
            WorkOrderId = wo.Id,
            Status = "issued",
            IssuedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _db.MaterialIssues.Add(issue);
        
        var issueItem = new MaterialIssueItem
        {
            IssueId = issue.Id,
            SkuId = sku.Id,
            IssuedQty = request.Quantity,
            Status = "completed",
            LotNumber = request.LotNumber
        };
        _db.MaterialIssueItems.Add(issueItem);

        // 2. 更新庫存 (同步扣減 SKU 總量)
        sku.Qty -= request.Quantity;
        sku.UpdatedAt = DateTime.UtcNow;

        // 3. 記錄實際成本 (Actual Costing - Material)
        // 優先使用最新進貨成本，若無則使用標準成本
        decimal actualUnitCost = sku.LatestPurchaseCost > 0 ? sku.LatestPurchaseCost : sku.StandardCost;
        if (actualUnitCost == 0) actualUnitCost = 10.0m; // 保守設計
        await _costing.RecordMaterialActualAsync(
            wo.Id, 
            sku.Id, 
            request.Quantity, 
            actualUnitCost, 
            request.LotNumber
        );

        await _db.SaveChangesAsync();

        return OkObject(new { 
            Message = "領料成功並已同步記錄實際成本", 
            IssueNumber = issue.IssueNumber,
            Sku = sku.SkuNumber,
            Quantity = request.Quantity,
            ActualUnitCost = actualUnitCost,
            TotalActualCost = request.Quantity * actualUnitCost
        });
    }
}

public class MaterialIssueRequest
{
    public string WoNumber { get; set; } = null!;
    public string SkuCode { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string? LotNumber { get; set; }
}
