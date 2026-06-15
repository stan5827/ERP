using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.MES;

namespace ErpBackend.Controllers;

[Route("api/erp")]
public class WorkOrdersCostingController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public WorkOrdersCostingController(ErpGeneralDbContext db) => _db = db;

    [HttpGet("work-orders-costing/summary")]
    public async Task<IActionResult> GetCostingSummary()
    {
        try
        {
            var wos = await _db.WorkOrders
                .Include(w => w.ProductSku)
                .Where(w => w.Status == "Completed" || w.Status == "Settled")
                .Select(w => new WorkOrderCostSummaryDto {
                    Id = w.Id,
                    WoNumber = w.WoNumber,
                    ProductSkuName = w.ProductSku != null ? w.ProductSku.SkuName : "",
                    ProductSkuNumber = w.ProductSku != null ? w.ProductSku.SkuNumber : "",
                    ActualQty = w.ActualQty,
                    Status = w.Status,
                    ActualMaterialCost = _db.WorkOrderMaterialActuals.Where(m => m.WorkOrderId == w.Id).Sum(m => m.TotalAmount),
                    ActualLaborCost = _db.WorkOrderLaborActuals.Where(l => l.WorkOrderId == w.Id).Sum(l => l.TotalAmount),
                    ActualOverheadCost = _db.WorkOrderMachineActuals.Where(m => m.WorkOrderId == w.Id).Sum(m => m.TotalAmount), 
                    TotalStandardCost = w.PlannedQty * 100 
                })
                .ToListAsync();

            return StatusCode(200, ApiResponse<List<WorkOrderCostSummaryDto>>.Ok(wos));
        }
        catch (Exception ex)
        {
            return StatusCode(400, ApiResponse.Fail(ex.Message));
        }
    }

    [HttpGet("workorders/{id}/costing/analysis")]
    public async Task<IActionResult> GetCostAnalysis(Guid id)
    {
        try
        {
            var data = await GetCostAnalysisInternal(id);
            if (data == null) return NotFound(ApiHelper.Fail("Error"));
            return StatusCode(200, ApiResponse<WorkOrderCostAnalysisDto>.Ok(data));
        }
        catch (Exception ex)
        {
            return StatusCode(400, ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("workorders/{id}/costing/settle")]
    public async Task<IActionResult> SettleWorkOrderCost(Guid id)
    {
        try
        {
            var wo = await _db.WorkOrders.FindAsync(id);
            if (wo == null) return NotFound(ApiHelper.Fail("Error"));

            wo.Status = "Settled";
            await _db.SaveChangesAsync();

            return StatusCode(200, ApiResponse.Ok("çµè??å?"));
        }
        catch (Exception ex)
        {
            return StatusCode(400, ApiResponse.Fail(ex.Message));
        }
    }

    [HttpPost("work-orders-costing/batch-settle")]
    public async Task<IActionResult> BatchSettle()
    {
        try
        {
            var wos = await _db.WorkOrders.Where(w => w.Status == "Completed").ToListAsync();
            foreach (var wo in wos)
            {
                wo.Status = "Settled";
            }
            await _db.SaveChangesAsync();
            return StatusCode(200, ApiResponse.Ok("Success"));
        }
        catch (Exception ex)
        {
            return StatusCode(400, ApiResponse.Fail(ex.Message));
        }
    }

    private async Task<WorkOrderCostAnalysisDto?> GetCostAnalysisInternal(Guid id)
    {
        var wo = await _db.WorkOrders.FirstOrDefaultAsync(o => o.Id == id);
        if (wo == null) return null;

        var actualMat = await _db.WorkOrderMaterialActuals.Where(m => m.WorkOrderId == id).SumAsync(m => m.TotalAmount);
        var actualLab = await _db.WorkOrderLaborActuals.Where(l => l.WorkOrderId == id).SumAsync(l => l.TotalAmount);
        var actualMac = await _db.WorkOrderMachineActuals.Where(m => m.WorkOrderId == id).SumAsync(m => m.TotalAmount);

        decimal stdMat = wo.PlannedQty * 80; 
        decimal stdLab = wo.PlannedQty * 20; 

        return new WorkOrderCostAnalysisDto
        {
            WorkOrderId = wo.Id,
            WoNumber = wo.WoNumber,
            ActualMaterialCost = actualMat,
            ActualLaborCost = actualLab,
            ActualOverheadCost = actualMac,
            TotalActualCost = actualMat + actualLab + actualMac,
            StandardMaterialCost = stdMat,
            StandardLaborCost = stdLab,
            TotalStandardCost = stdMat + stdLab,
            VarianceAmount = (actualMat + actualLab + actualMac) - (stdMat + stdLab),
            VariancePercentage = (stdMat + stdLab) > 0 ? Math.Round(((actualMat + actualLab + actualMac) - (stdMat + stdLab)) / (stdMat + stdLab) * 100, 2) : 0,
            UnitCost = wo.ActualQty > 0 ? (actualMat + actualLab + actualMac) / wo.ActualQty : 0,
            ActualQty = wo.ActualQty
        };
    }
}
