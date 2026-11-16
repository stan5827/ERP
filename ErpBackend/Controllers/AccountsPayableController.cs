using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.DTOs;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

[Authorize]
public class AccountsPayableController : BaseController
{
    private readonly IAccountsPayableService _apService;

    public AccountsPayableController(IAccountsPayableService apService) => _apService = apService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ApSummaryDto>>>> GetApSummary(
        [FromQuery] string? supplierId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate)
    {
        var items = await _apService.GetApSummaryAsync(supplierId, startDate, endDate);
        return Ok(items);
    }

    [HttpGet("aging")]
    public async Task<ActionResult<ApiResponse<List<ApAgingDto>>>> GetAgingReport()
    {
        var aging = await _apService.GetAgingReportAsync();
        return Ok(aging);
    }

    [HttpGet("by-supplier/{supplierId}")]
    public async Task<ActionResult<ApiResponse<List<ApDetailDto>>>> GetApBySupplier(Guid supplierId)
    {
        var dtos = await _apService.GetApBySupplierAsync(supplierId);
        return Ok(dtos);
    }

    [HttpPost("payment")]
    public async Task<ActionResult<ApiResponse>> RecordPayment([FromBody] RecordApPaymentRequest req)
    {
        var (found, status) = await _apService.RecordPaymentAsync(req);
        if (!found) return NotFound(ApiResponse.Fail("Purchase order not found"));
        if (status == "ambiguous_ap")
            return Conflict(ApiResponse.Fail("Purchase order has multiple AP records; use the AccountsPayableId payment endpoint"));
        if (status == "no_ap_for_po")
            return Conflict(ApiResponse.Fail("No accounts payable record is linked to this purchase order"));
        if (status == "ap_po_mismatch")
            return Conflict(ApiResponse.Fail("Accounts payable supplier or purchase order does not match"));
        if (status == "exceeds_outstanding")
            return Conflict(ApiResponse.Fail("Payment exceeds the outstanding accounts payable balance after debit notes"));
        if (status == "unallocated_legacy_balance")
            return Conflict(ApiResponse.Fail("Legacy purchase-order payments must be reconciled to an AP before recording another payment"));
        if (status == "invalid_amount")
            return BadRequest(ApiResponse.Fail("Payment amount must be positive and have at most two decimal places"));
        if (status == "invalid_ap_amount")
            return Conflict(ApiResponse.Fail("Accounts payable amount is invalid"));
        return ApiResponse.Ok("Payment recorded");
    }

    [HttpPost("payment/accounts-payable")]
    public async Task<ActionResult<ApiResponse>> RecordAccountsPayablePayment(
        [FromBody] RecordAccountsPayablePaymentRequest req)
    {
        var (found, status) = await _apService.RecordAccountsPayablePaymentAsync(req);
        if (!found) return NotFound(ApiResponse.Fail("Accounts payable record not found"));
        if (status == "ap_po_mismatch")
            return Conflict(ApiResponse.Fail("Accounts payable supplier or purchase order does not match"));
        if (status == "exceeds_outstanding")
            return Conflict(ApiResponse.Fail("Payment exceeds the outstanding accounts payable balance after debit notes"));
        if (status == "unallocated_legacy_balance")
            return Conflict(ApiResponse.Fail("Legacy purchase-order payments must be reconciled to an AP before recording another payment"));
        if (status == "invalid_amount")
            return BadRequest(ApiResponse.Fail("Payment amount must be positive and have at most two decimal places"));
        if (status == "invalid_ap_amount")
            return Conflict(ApiResponse.Fail("Accounts payable amount is invalid"));
        return ApiResponse.Ok("Payment recorded");
    }
}
