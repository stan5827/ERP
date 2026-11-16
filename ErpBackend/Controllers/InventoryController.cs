using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/erp/[controller]")]
public class InventoryController : BaseController
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("ping")]
    public object Ping() => new { message = "InventoryController operational" };

    // 前端 api.ts 呼叫: GET /inventory/balance/{sku_code}
    [HttpGet("balance/{skuCode}")]
    public async Task<object> GetSkuBalanceFrontend(string skuCode)
    {
        var result = await _inventoryService.GetSkuBalanceAsync(skuCode);
        return Ok(result);
    }

    // 舊有路由: GET /inventory/{skuCode}/balance
    [HttpGet("{skuCode}/balance")]
    public async Task<object> GetSkuBalance(string skuCode)
    {
        var result = await _inventoryService.GetSkuBalanceAsync(skuCode);
        return Ok(result);
    }

    [HttpGet("atp")]
    public async Task<object> CheckATP(
        [FromQuery] string? skuCode,
        [FromQuery] string? sku_code,
        [FromQuery] decimal? requestedQty,
        [FromQuery] decimal? requested_qty)
    {
        // 支援前端舊參數命名 (sku_code / requested_qty)
        var code = skuCode ?? sku_code ?? string.Empty;
        var qty = requestedQty ?? requested_qty ?? 0m;
        var result = await _inventoryService.CheckATPAsync(code, qty);
        return Ok(result);
    }

    // 前端 api.ts 呼叫: GET /inventory/low-stock
    [HttpGet("low-stock")]
    public async Task<object> GetLowStock()
    {
        var items = await _inventoryService.GetLowStockAsync();
        return Ok(items, totalCount: items.Count);
    }

    // 前端 api.ts 呼叫: GET /inventory/valuation
    [HttpGet("valuation")]
    public async Task<object> GetValuation()
    {
        var totalValue = await _inventoryService.GetTotalValuationAsync();
        return Ok((object)new { totalValue });
    }

    [HttpPost("adjust")]
    public async Task<object> AdjustBalance([FromBody] InventoryAdjustRequest request)
    {
        var result = await _inventoryService.AdjustBalanceAsync(request);
        return Ok(result);
    }

    [HttpGet("stocktake/balance")]
    public async Task<ActionResult<ApiResponse<InventoryStocktakeBalanceDto>>> GetStocktakeBalance(
        [FromQuery] string skuCode,
        [FromQuery] string warehouseCode,
        [FromQuery] string lotNumber)
    {
        var result = await _inventoryService.GetStocktakeBalanceAsync(skuCode, warehouseCode, lotNumber);
        return result is null
            ? NotFound(ApiResponse.Fail("找不到料號或倉庫"))
            : Ok(result);
    }

    [HttpPost("stocktake")]
    public async Task<ActionResult<ApiResponse<InventoryStocktakeResultDto>>> PostStocktake(
        [FromBody] InventoryStocktakeRequestDto request)
    {
        var actorUserId = GetUserId();
        if (actorUserId == Guid.Empty)
            return Unauthorized(ApiResponse.Fail("無法識別過帳操作人員"));

        try
        {
            return Ok(
                await _inventoryService.PostStocktakeAsync(request, actorUserId),
                "盤點差異已過帳");
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiResponse.Fail(exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(ApiResponse.Fail(exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(ApiResponse.Fail(exception.Message));
        }
    }

    [HttpPost("transaction")]
    public async Task<object> PostTransaction([FromBody] InventoryTransactionRequest request)
    {
        var result = await _inventoryService.PostTransactionAsync(request);
        return Ok(result);
    }

    [HttpGet("transactions")]
    public async Task<object> GetTransactions(
        [FromQuery] string? skuCode = null,
        [FromQuery] string? warehouseCode = null,
        [FromQuery] string? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _inventoryService.GetTransactionsAsync(skuCode, warehouseCode, type, fromDate, toDate);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<object> GetSummary([FromQuery] string? warehouseCode = null)
    {
        var result = await _inventoryService.GetSummaryAsync(warehouseCode);
        return Ok(result);
    }
}
