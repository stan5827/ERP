using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;

namespace ErpBackend.Controllers;

public class SupplierQuotesController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public SupplierQuotesController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SupplierQuoteDto>>>> GetSupplierQuotes([FromQuery] string? rfqId, [FromQuery] string? supplierId)
    {
        var query = _db.RfqSupplierResponses
            .Include(r => r.Supplier)
            .Include(r => r.Rfq)
            .Include(r => r.Sku)
            .AsQueryable();

        if (!string.IsNullOrEmpty(rfqId))
        {
            if (Guid.TryParse(rfqId, out var rfqGuid))
                query = query.Where(r => r.RfqId == rfqGuid);
        }

        if (!string.IsNullOrEmpty(supplierId))
        {
            if (Guid.TryParse(supplierId, out var suppGuid))
                query = query.Where(r => r.SupplierId == suppGuid);
        }

        var items = await query
            .OrderByDescending(r => r.UnitPrice)
            .ToListAsync();

        var dtos = items.Select(r => new SupplierQuoteDto
        {
            Id = r.Id,
            RfqId = r.RfqId,
            RfqNumber = r.Rfq?.RfqNumber,
            SupplierId = r.SupplierId,
            SupplierName = r.Supplier?.SupplierName,
            SkuId = r.SkuId,
            SkuNumber = r.Sku?.SkuNumber,
            SkuName = r.Sku?.SkuName,
            UnitPrice = r.UnitPrice,
            LeadTimeDays = r.LeadTimeDays,
            Moq = r.Moq,
            TradeTerms = r.TradeTerms,
            Notes = r.Notes,
            IsSelected = r.IsSelected
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SupplierQuoteDto>>> GetSupplierQuote(Guid id)
    {
        var r = await _db.RfqSupplierResponses
            .Include(r => r.Supplier)
            .Include(r => r.Rfq)
            .Include(r => r.Sku)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (r == null) return NotFound(ApiHelper.Fail("Quote not found"));

        var dto = new SupplierQuoteDto
        {
            Id = r.Id,
            RfqId = r.RfqId,
            RfqNumber = r.Rfq?.RfqNumber,
            SupplierId = r.SupplierId,
            SupplierName = r.Supplier?.SupplierName,
            SkuId = r.SkuId,
            SkuNumber = r.Sku?.SkuNumber,
            SkuName = r.Sku?.SkuName,
            UnitPrice = r.UnitPrice,
            LeadTimeDays = r.LeadTimeDays,
            Moq = r.Moq,
            TradeTerms = r.TradeTerms,
            Notes = r.Notes,
            IsSelected = r.IsSelected
        };

        return Ok(dto);
    }

    [HttpPatch("{id}/select")]
    public async Task<ActionResult<ApiResponse>> SelectQuote(Guid id)
    {
        var quote = await _db.RfqSupplierResponses.FindAsync(id);
        if (quote == null) return NotFound(ApiHelper.Fail("Quote not found"));

        quote.IsSelected = true;
        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Quote selected");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteSupplierQuote(Guid id)
    {
        var quote = await _db.RfqSupplierResponses.FindAsync(id);
        if (quote == null) return NotFound(ApiHelper.Fail("Quote not found"));

        _db.RfqSupplierResponses.Remove(quote);
        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Quote deleted");
    }
}

