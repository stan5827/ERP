using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ErpBackend.DTOs;
using ErpBackend.Services;
using ErpBackend.Models.Portal;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/erp/supplier-portal")]
public class SupplierPortalController : BaseController
{
    private readonly ISupplierPortalService _portalService;

    public SupplierPortalController(ISupplierPortalService portalService)
    {
        _portalService = portalService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<SupplierPortalLoginResponse>>> Login([FromBody] SupplierPortalLoginRequest request)
    {
        var response = await _portalService.LoginAsync(request);
        if (response == null)
            return Unauthorized(ApiResponse.Fail("Invalid credentials"));

        return Ok(response);
    }

    [HttpGet("rfqs")]
    [Authorize(Roles = "Supplier")]
    public async Task<ActionResult<ApiResponse<IEnumerable<RfqDto>>>> GetRfqs()
    {
        var supplierId = GetSupplierId();
        var rfqs = await _portalService.GetActiveRfqsAsync(supplierId);
        return Ok(rfqs);
    }

    [HttpPost("quotes")]
    [Authorize(Roles = "Supplier")]
    public async Task<ActionResult<ApiResponse>> SubmitQuote([FromBody] SubmitSupplierQuoteRequest request)
    {
        var supplierId = GetSupplierId();
        var supplierUserId = GetUserId(); // Assuming sub claim is the user id
        
        var success = await _portalService.SubmitQuoteAsync(supplierUserId, supplierId, request);
        if (!success)
            return Fail("Failed to submit quote");

        return Ok("Quote submitted successfully");
    }

    [HttpGet("purchase-orders")]
    [Authorize(Roles = "Supplier")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PurchaseOrderDto>>>> GetPurchaseOrders()
    {
        var supplierId = GetSupplierId();
        var pos = await _portalService.GetActivePurchaseOrdersAsync(supplierId);
        return Ok(pos);
    }

    [HttpPost("purchase-orders/{id}/confirm")]
    [Authorize(Roles = "Supplier")]
    public async Task<ActionResult<ApiResponse>> ConfirmPo(Guid id)
    {
        var supplierId = GetSupplierId();
        var success = await _portalService.ConfirmPurchaseOrderAsync(id, supplierId);
        if (!success)
            return Fail("Failed to confirm purchase order");

        return Ok("Purchase order confirmed successfully");
    }

    [HttpGet("asns")]
    [Authorize(Roles = "Supplier")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SupplierAsnDto>>>> GetAsns()
    {
        var supplierId = GetSupplierId();
        var asns = await _portalService.GetAsnsAsync(supplierId);
        return Ok(asns);
    }

    [HttpPost("asns")]
    [Authorize(Roles = "Supplier")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateAsn([FromBody] CreateSupplierAsnRequest request)
    {
        var supplierId = GetSupplierId();
        var asnId = await _portalService.CreateAsnAsync(supplierId, request);
        return Ok(asnId);
    }

    [HttpGet("performance")]
    [Authorize(Roles = "Supplier")]
    public async Task<ActionResult<ApiResponse<SupplierPerformanceReportDto>>> GetPerformance([FromQuery] string period)
    {
        var supplierId = GetSupplierId();
        var score = await _portalService.GetPerformanceScoreAsync(supplierId, period);
        if (score == null)
            return Fail<SupplierPerformanceReportDto>("Performance data not found");

        return Ok(score);
    }

    private Guid GetSupplierId()
    {
        var claim = User.FindFirst("SupplierId")?.Value ?? User.FindFirst("supplier_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public class SupplierAsnLegacyCreateRequest : CreateSupplierAsnRequest
{
    public Guid SupplierId { get; set; }
    public string? Notes { get; set; }
}
