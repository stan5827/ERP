using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.MES;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

[Route("api/erp/currencies")]
public class CurrenciesController : BaseController
{
    private readonly ErpGeneralDbContext _context;

    public CurrenciesController(ErpGeneralDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CurrencyMaster>>>> List()
    {
        var list = await _context.Currencies
            .OrderByDescending(c => c.IsBaseCurrency)
            .ThenBy(c => c.CurrencyCode)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CurrencyMaster>>> Get(Guid id)
    {
        var item = await _context.Currencies.FindAsync(id);
        if (item == null) return NotFound(ApiResponse.Fail("Currency not found"));
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CurrencyMaster>>> Create(CurrencyMaster model)
    {
        if (model.IsBaseCurrency)
        {
            // Reset other base currencies
            var bases = await _context.Currencies.Where(c => c.IsBaseCurrency).ToListAsync();
            foreach (var b in bases) b.IsBaseCurrency = false;
        }

        _context.Currencies.Add(model);
        await _context.SaveChangesAsync();
        return Ok(model);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CurrencyMaster>>> Update(Guid id, CurrencyMaster model)
    {
        if (id != model.Id) return BadRequest();
        
        if (model.IsBaseCurrency)
        {
            var bases = await _context.Currencies.Where(c => c.Id != id && c.IsBaseCurrency).ToListAsync();
            foreach (var b in bases) b.IsBaseCurrency = false;
        }

        _context.Entry(model).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return Ok(model);
    }
}
