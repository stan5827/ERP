using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Services;
using ErpBackend.DTOs;
using ErpBackend.Helpers;

namespace ErpBackend.Controllers;

public class WipReportsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    private readonly Services.IAccountingService _accounting;
    private readonly Services.ICostingService _costing;

    public WipReportsController(ErpGeneralDbContext db, Services.IAccountingService accounting, Services.ICostingService costing)
    {
        _db = db;
        _accounting = accounting;
        _costing = costing;
    }

    [HttpGet("ping")]
    public dynamic Ping() => Ok(new { Message = "WipReportsController is currently being repaired." });

    [HttpPost("submit")]
    public async Task<dynamic> ReportCompletion([FromBody] WipReportRequest request)
    {
        var wo = await _db.WorkOrders
            .Include(w => w.ProductSku) // ç¢ºä??½å???SKU
            .FirstOrDefaultAsync(w => w.WoNumber == request.WoNumber);
        
        if (wo == null) return NotFound("Error");

        // 1. å»ºç? WIP ?±å ±è¨é?
        var report = new WipReport
        {
            ReportNumber = DocumentNumberGenerator.GenerateWipReport(),
            WorkOrderId = wo.Id,
            OperationSeq = request.OperationSeq,
            CompletedQty = request.CompletedQty,
            EmployeeId = Guid.Empty, // ç°¡å??ç?
            StartTime = DateTime.UtcNow.AddHours(-request.ManHours),
            EndTime = DateTime.UtcNow,
            ActualManHours = (decimal)request.ManHours
        };

        _db.WipReports.Add(report);

        // 4. è¨é?å¯¦é??æ¬ (Actual Costing - Labor & Machine)
        try
        {
            // å°æ¾æ­¤å·¥åºå??ç?å·¥ç?è²»ç?
            var op = await _db.WorkOrderOperations
                .FirstOrDefaultAsync(o => o.WorkOrderId == wo.Id && o.OperationSeq == request.OperationSeq);
                
            if (op != null)
            {
                // ?²å??æ?ä¸­ç?è²»ç? (Labor, Machine)
                var rate = await _db.WorkCenterCosts
                    .Where(r => r.WorkCenterId == op.WorkCenterId && r.EffectiveFrom <= DateTime.UtcNow && (r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow))
                    .OrderByDescending(r => r.EffectiveFrom)
                    .FirstOrDefaultAsync();
                
                if (rate != null)
                {
                    // ä½¿ç¨ CostingService è¨é?äººå·¥?æ??¨æ// 
                    await _costing.RecordLaborActualAsync(
                        wo.Id, 
                        report.Id, 
                        request.OperationSeq, 
                        (decimal)request.ManHours, 
                        rate.LaborRate, 
                        request.EmployeeId != Guid.Empty ? request.EmployeeId : null
                    );
                    
                    await _costing.RecordMachineActualAsync(
                        wo.Id, 
                        report.Id, 
                        request.OperationSeq, 
                        (decimal)request.MachineHours, 
                        rate.MachineRate, 
                        op.WorkCenterId
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Actual cost collection failed: {ex.Message}");
        }

        // 2. ?´æ°å·¥å®?²åº¦
        wo.ActualQty += request.CompletedQty;
        if (wo.ActualQty >= wo.PlannedQty) 
        {
            wo.Status = "completed";
            wo.ActualEndDate = DateTime.UtcNow;
        }

        // 3. è§¸ç¼?ªå??è??é? (TICKET-003: ?æ¬çµè?)
        try 
        {
            // å¾?SKU ?²å?æ¨æ??æ¬
            decimal unitCost = wo.ProductSku?.StandardCost ?? 0m;
            
            if (unitCost > 0)
            {
                await _accounting.CreateVoucherFromProductionReceiptAsync(wo.Id, request.CompletedQty, request.CompletedQty * unitCost);
            }
        }
        catch (Exception ex)
        {
            // ?è´?½é¯èª¤ï?è¨é? Log
            Console.WriteLine($"Accounting entry failed: {ex.Message}");
        }

        await _db.SaveChangesAsync();

        return Ok($"?±å·¥?å?ï¼å®?ï?{report.ReportNumber}");
    }

    public class WipReportRequest
    {
        public string WoNumber { get; set; } = null!;
        public int OperationSeq { get; set; }
        public decimal CompletedQty { get; set; }
        public double ManHours { get; set; }
        public double MachineHours { get; set; }
        public Guid EmployeeId { get; set; }
    }
}
