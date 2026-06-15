using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.Models.Inventory;
using ErpBackend.Services;
using ErpBackend.DTOs;
using System.Diagnostics;

namespace ErpBackend.Controllers;

[Route("api/erp/benchmark")]
[Authorize]
public class BenchmarkController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    private readonly IMrpEngine _mrpEngine;
    private readonly ILogger<BenchmarkController> _logger;

    public BenchmarkController(ErpGeneralDbContext db, IMrpEngine mrpEngine, ILogger<BenchmarkController> logger)
    {
        _db = db;
        _mrpEngine = mrpEngine;
        _logger = logger;
    }

    [HttpPost("seed")]
    public async Task<ActionResult<ApiResponse>> Seed([FromQuery] int targetNodes = 1000)
    {
        _logger.LogInformation($"Starting benchmark seed for {targetNodes} nodes...");
        
        await CleanupInternal();

        var sw = Stopwatch.StartNew();
        
        var benchCustId = new Guid("11111111-1111-1111-1111-111111111111");
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == benchCustId);
        if (customer == null)
        {
            customer = new Models.Sales.Customer
            {
                Id = benchCustId,
                CustomerCode = "BENCH-CUST",
                CustomerName = "Benchmark Test Customer",
                IsActive = true
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
        }

        var rootSku = new Sku 
        { 
            SkuNumber = "BENCH-ROOT", 
            SkuName = "Benchmark Root Product", 
            Unit = "PCS",
            SourceType = "manufacture",
            IsBomTop = true,
            Status = "active",
            LowLevelCode = 0
        };
        _db.Skus.Add(rootSku);
        await _db.SaveChangesAsync();

        sw.Stop();
        return OkObject(new { 
            message = "Benchmark seed completed", 
            timeTakenSeconds = sw.Elapsed.TotalSeconds 
        });
    }

    [HttpPost("run")]
    public async Task<ActionResult<ApiResponse>> Run()
    {
        _logger.LogInformation("Starting MRP benchmark run...");
        
        var rootSku = await _db.Skus.FirstOrDefaultAsync(s => s.SkuCode == "BENCH-ROOT");
        if (rootSku == null) return Fail("Benchmark data not seeded. Call /seed first.");

        var sw = Stopwatch.StartNew();
        var run = await _mrpEngine.RunMrpAsync(DateTime.UtcNow, DateTime.UtcNow.AddDays(60));
        sw.Stop();

        return OkObject(new {
            runId = run.Id,
            runNumber = run.RunNumber,
            totalItemsProcessed = run.TotalItemsProcessed,
            plannedOrdersGenerated = run.PlannedOrdersGenerated,
            timeTakenSeconds = sw.Elapsed.TotalSeconds
        });
    }

    [HttpDelete("cleanup")]
    public async Task<ActionResult<ApiResponse>> Cleanup()
    {
        await CleanupInternal();
        return Ok("Benchmark data cleaned up");
    }

    private async Task CleanupInternal()
    {
        _logger.LogInformation("Cleaning up benchmark data...");
        
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM mrp_planned_orders");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM mrp_requirements");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM mrp_runs");

        await _db.Database.ExecuteSqlRawAsync("UPDATE skus SET default_bom_id = NULL WHERE sku_code LIKE 'B-L%' OR sku_code = 'BENCH-ROOT'");
        
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM bom_items WHERE bom_id IN (SELECT id FROM boms WHERE bom_code LIKE 'BOM-B-%' OR bom_code = 'BOM-BENCH-ROOT')");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM boms WHERE bom_code LIKE 'BOM-B-%' OR bom_code = 'BOM-BENCH-ROOT'");
        
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM skus WHERE sku_code LIKE 'B-L%' OR sku_code = 'BENCH-ROOT'");
        
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM sales_order_items WHERE sales_order_id IN (SELECT id FROM sales_orders WHERE order_number LIKE 'SO-BENCH%')");
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM sales_orders WHERE order_number LIKE 'SO-BENCH%'");
        
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM customers WHERE customer_code = 'BENCH-CUST'");
        
        await _db.SaveChangesAsync();
    }
}
