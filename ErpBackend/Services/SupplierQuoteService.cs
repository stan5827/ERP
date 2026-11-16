using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;

namespace ErpBackend.Services;

public interface ISupplierQuoteService
{
    Task<List<SupplierQuoteDto>> GetSupplierQuotesAsync(string? rfqId = null, string? supplierId = null);
    Task<SupplierQuoteDto?> GetSupplierQuoteAsync(Guid id);
    Task<(bool ok, string status)> SelectQuoteAsync(Guid id);
    Task<(bool ok, string status)> DeleteSupplierQuoteAsync(Guid id);
}

public class SupplierQuoteService : ISupplierQuoteService
{
    private readonly ErpGeneralDbContext _db;

    public SupplierQuoteService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<SupplierQuoteDto>> GetSupplierQuotesAsync(string? rfqId = null, string? supplierId = null)
    {
        var query = _db.RfqSupplierResponses
            .Include(r => r.Supplier)
            .Include(r => r.Rfq)
            .Include(r => r.Sku)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(rfqId) && Guid.TryParse(rfqId, out var rfqGuid))
            query = query.Where(r => r.RfqId == rfqGuid);

        if (!string.IsNullOrEmpty(supplierId) && Guid.TryParse(supplierId, out var suppGuid))
            query = query.Where(r => r.SupplierId == suppGuid);

        var items = await query.OrderByDescending(r => r.UnitPrice).ToListAsync();

        return items.Select(MapToDto).ToList();
    }

    public async Task<SupplierQuoteDto?> GetSupplierQuoteAsync(Guid id)
    {
        var r = await _db.RfqSupplierResponses
            .Include(r => r.Supplier)
            .Include(r => r.Rfq)
            .Include(r => r.Sku)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        return r == null ? null : MapToDto(r);
    }

    public async Task<(bool ok, string status)> SelectQuoteAsync(Guid id)
    {
        var quote = await _db.RfqSupplierResponses.FirstOrDefaultAsync(r => r.Id == id);
        if (quote == null) return (false, "not_found");

        quote.IsSelected = true;
        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    public async Task<(bool ok, string status)> DeleteSupplierQuoteAsync(Guid id)
    {
        var quote = await _db.RfqSupplierResponses.FirstOrDefaultAsync(r => r.Id == id);
        if (quote == null) return (false, "not_found");

        _db.RfqSupplierResponses.Remove(quote);
        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    private static SupplierQuoteDto MapToDto(RfqSupplierResponse r) => new()
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
}
