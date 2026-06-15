using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;

namespace ErpBackend.Controllers;

public class AccountsPayableController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public AccountsPayableController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AccountsPayableDto>>>> GetAccountsPayable(
        [FromQuery] string? supplierId,
        [FromQuery] string? matchResult,
        [FromQuery] string? paymentStatus)
    {
        var query = _db.AccountsPayables
            .Include(ap => ap.Supplier)
            .Include(ap => ap.PurchaseOrder)
            .Include(ap => ap.GoodsReceipt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(supplierId) && Guid.TryParse(supplierId, out var supplierGuid))
            query = query.Where(ap => ap.SupplierId == supplierGuid);
        if (!string.IsNullOrWhiteSpace(matchResult))
            query = query.Where(ap => ap.ThreeWayMatchResult == matchResult);
        if (!string.IsNullOrWhiteSpace(paymentStatus))
            query = query.Where(ap => ap.PaymentStatus == paymentStatus);

        var items = await query
            .OrderByDescending(ap => ap.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(MapAccountsPayableDto).ToList());
    }

    [HttpGet("three-way-match/{id:guid}")]
    public async Task<ActionResult<ApiResponse<AccountsPayableDto>>> ReviewThreeWayMatch(Guid id)
    {
        var ap = await _db.AccountsPayables
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.GoodsReceipt)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ap == null) return NotFound(ApiHelper.Fail("Accounts payable not found."));
        return Ok(MapAccountsPayableDto(ap));
    }

    [HttpPatch("{id:guid}/pay")]
    public async Task<ActionResult<ApiResponse<AccountsPayableDto>>> MarkAsPaid(Guid id)
    {
        var ap = await _db.AccountsPayables
            .Include(x => x.Supplier)
            .Include(x => x.PurchaseOrder)
            .Include(x => x.GoodsReceipt)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ap == null) return NotFound(ApiHelper.Fail("Accounts payable not found."));

        ap.PaymentStatus = "paid";
        if (string.IsNullOrWhiteSpace(ap.VerificationStatus) || ap.VerificationStatus == "pending")
            ap.VerificationStatus = "verified";

        await _db.SaveChangesAsync();
        return Ok(MapAccountsPayableDto(ap), "應付帳款已標記付款");
    }

    [HttpGet("aging")]
    public async Task<ActionResult<ApiResponse<List<ApAgingDto>>>> GetAgingReport()
    {
        var aps = await _db.AccountsPayables
            .Include(ap => ap.Supplier)
            .Where(ap => ap.PaymentStatus != "paid")
            .ToListAsync();

        var now = DateTime.UtcNow.Date;
        var aging = aps
            .GroupBy(ap => ap.Supplier)
            .Select(g => new ApAgingDto
            {
                SupplierId = g.Key!.Id,
                SupplierName = g.Key.SupplierName,
                Current = g.Where(ap => !ap.DueDate.HasValue || (now - ap.DueDate.Value.Date).Days <= 0).Sum(ap => ap.TotalAmount),
                Days30 = g.Where(ap => ap.DueDate.HasValue && (now - ap.DueDate.Value.Date).Days is >= 1 and <= 30).Sum(ap => ap.TotalAmount),
                Days60 = g.Where(ap => ap.DueDate.HasValue && (now - ap.DueDate.Value.Date).Days is >= 31 and <= 60).Sum(ap => ap.TotalAmount),
                Days90 = g.Where(ap => ap.DueDate.HasValue && (now - ap.DueDate.Value.Date).Days is >= 61 and <= 90).Sum(ap => ap.TotalAmount),
                Over90 = g.Where(ap => ap.DueDate.HasValue && (now - ap.DueDate.Value.Date).Days > 90).Sum(ap => ap.TotalAmount)
            })
            .ToList();

        return Ok(aging);
    }

    private static AccountsPayableDto MapAccountsPayableDto(Models.Procurement.AccountsPayable ap)
        => new()
        {
            Id = ap.Id,
            ApNumber = ap.ApNumber,
            SupplierId = ap.SupplierId,
            SupplierName = ap.Supplier?.SupplierName,
            PurchaseOrderId = ap.PurchaseOrderId,
            PurchaseOrderNumber = ap.PurchaseOrder?.PoNumber,
            GoodsReceiptId = ap.GoodsReceiptId,
            GoodsReceiptNumber = ap.GoodsReceipt?.GrNumber,
            SupplierInvoiceNumber = ap.SupplierInvoiceNumber,
            SupplierInvoiceDate = ap.SupplierInvoiceDate,
            Currency = ap.Currency,
            InvoiceAmount = ap.InvoiceAmount,
            TaxAmount = ap.TaxAmount,
            TotalAmount = ap.TotalAmount,
            DueDate = ap.DueDate,
            PaymentStatus = ap.PaymentStatus,
            VerificationStatus = ap.VerificationStatus,
            ThreeWayMatchResult = ap.ThreeWayMatchResult,
            Notes = ap.Notes,
            CreatedAt = ap.CreatedAt
        };
}
