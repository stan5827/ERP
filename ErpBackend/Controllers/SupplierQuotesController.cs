using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class SupplierQuotesController : BaseController
{
    private readonly ISupplierQuoteService _supplierQuoteService;

    public SupplierQuotesController(ISupplierQuoteService supplierQuoteService) => _supplierQuoteService = supplierQuoteService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SupplierQuoteDto>>>> GetSupplierQuotes([FromQuery] string? rfqId, [FromQuery] string? supplierId)
        => Ok(await _supplierQuoteService.GetSupplierQuotesAsync(rfqId, supplierId));

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SupplierQuoteDto>>> GetSupplierQuote(Guid id)
    {
        var dto = await _supplierQuoteService.GetSupplierQuoteAsync(id);
        return dto == null ? NotFound(ApiHelper.Fail("Quote not found")) : Ok(dto);
    }

    [HttpPatch("{id}/select")]
    public async Task<ActionResult<ApiResponse>> SelectQuote(Guid id)
    {
        var (ok, status) = await _supplierQuoteService.SelectQuoteAsync(id);
        if (status == "not_found") return NotFound(ApiHelper.Fail("Quote not found"));
        return ApiResponse.Ok("Quote selected");
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteSupplierQuote(Guid id)
    {
        var (ok, status) = await _supplierQuoteService.DeleteSupplierQuoteAsync(id);
        if (status == "not_found") return NotFound(ApiHelper.Fail("Quote not found"));
        return ApiResponse.Ok("Quote deleted");
    }
}
