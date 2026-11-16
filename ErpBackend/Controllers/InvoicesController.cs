using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
[Route("api/erp/invoices")]
public class InvoicesController : BaseController
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService) => _invoiceService = invoiceService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InvoiceDto>>>> GetInvoices(
        [FromQuery] string? status,
        [FromQuery] string? customerId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var dtos = await _invoiceService.GetInvoicesAsync(status, customerId, startDate, endDate, searchTerm, page, pageSize);
        return Ok(dtos, totalCount: dtos.Count);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoice(Guid id)
    {
        var dto = await _invoiceService.GetInvoiceAsync(id);
        return dto == null ? NotFound(ApiHelper.Fail("Invoice not found")) : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> CreateInvoice([FromBody] CreateInvoiceRequest req)
    {
        var (dto, status) = await _invoiceService.CreateInvoiceAsync(req);
        if (status != "ok")
            return status == "customer_not_found"
                ? BadRequest(ApiHelper.Fail("Customer not found"))
                : BadRequest(ApiHelper.Fail($"Invoice rejected: {status}"));
        return Ok(dto!);
    }

    [HttpPatch("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> ApproveInvoice(Guid id, [FromBody] ApproveInvoiceRequest req)
    {
        var (found, status) = await _invoiceService.ApproveInvoiceAsync(id, req);
        if (!found) return NotFound(ApiHelper.Fail("Invoice not found"));
        if (status == "guarded") return BadRequest(ApiHelper.Fail("Only draft invoices can be approved"));
        if (status == "delivery_not_completed") return BadRequest(ApiHelper.Fail("Invoice requires a completed delivery"));
        if (status == "accounting_setup_missing")
            return BadRequest(ApiHelper.Fail("Required active GL account mappings for receivables, revenue, and applicable output tax must be configured"));
        return ApiResponse.Ok("Invoice approved");
    }

    [HttpPatch("{id}/void")]
    public async Task<ActionResult<ApiResponse>> VoidInvoice(Guid id, [FromBody] VoidInvoiceRequest req)
    {
        var (found, status) = await _invoiceService.VoidInvoiceAsync(id, req);
        if (!found) return NotFound(ApiHelper.Fail("Invoice not found"));
        if (status == "guarded") return BadRequest(ApiHelper.Fail("Invoice already voided"));
        return ApiResponse.Ok("Invoice voided");
    }

    [HttpGet("aging")]
    public async Task<ActionResult<ApiResponse<List<AgingReportDto>>>> GetAgingReport()
    {
        var aging = await _invoiceService.GetAgingReportAsync();
        return Ok(aging);
    }
}
