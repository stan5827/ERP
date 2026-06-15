using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;

namespace ErpBackend.Controllers;

public class CreateCashReceiptRequest
{
    public DateTime ReceiptDate { get; set; }
    public Guid CustomerId { get; set; }
    public string PaymentMethod { get; set; } = "wire";
    public decimal ReceivedAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public List<CreateCashReceiptAllocationRequest> Allocations { get; set; } = new();
}

public class CreateCashReceiptAllocationRequest
{
    public Guid SalesInvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
}

public class CashReceiptsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public CashReceiptsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CashReceipt>>>> GetCashReceipts(
        [FromQuery] Guid? customerId)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.CashReceipts.Include(cr => cr.Allocations).AsQueryable();

        if (customerId.HasValue)
            query = query.Where(cr => cr.CustomerId == customerId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(cr => cr.ReceiptDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(items, totalCount: totalCount);
    }

    /// <summary>
    /// 建立收款單並進行發票核銷
    /// 當累積已付款額達發票總額時，將發票狀態更換為 paid
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CashReceipt>>> CreateCashReceipt([FromBody] CreateCashReceiptRequest req)
    {
        if (req.Allocations.Count == 0)
            return BadRequest(ApiHelper.FailOf<CashReceipt>("需要至少一筆發票核銷明細"));

        var totalAllocated = req.Allocations.Sum(a => a.AllocatedAmount);
        if (totalAllocated > req.ReceivedAmount)
            return BadRequest(ApiHelper.FailOf<CashReceipt>("核銷總金額超過本次收款金額"));

        // 驗證並準備核銷發票
        var invoicesToUpdate = new List<(SalesInvoice invoice, decimal amount)>();
        foreach (var alloc in req.Allocations)
        {
            var invoice = await _db.SalesInvoices.FindAsync(alloc.SalesInvoiceId);
            if (invoice == null)
                return BadRequest(ApiHelper.FailOf<CashReceipt>($"找不到發票 {alloc.SalesInvoiceId}"));
            if (invoice.CustomerId != req.CustomerId)
                return BadRequest(ApiHelper.FailOf<CashReceipt>($"發票 {invoice.InvoiceNumber} 不屬於此客戶"));
            if (invoice.Status != "approved")
                return BadRequest(ApiHelper.FailOf<CashReceipt>($"發票 {invoice.InvoiceNumber} 狀態為 {invoice.Status} (非已核准)，無法收款"));

            var outstanding = invoice.TotalAmount - invoice.PaidAmount;
            if (alloc.AllocatedAmount > outstanding)
                return BadRequest(ApiHelper.FailOf<CashReceipt>($"發票 {invoice.InvoiceNumber} 待收餘額 {outstanding}，核銷金額不應超過此金額"));

            invoicesToUpdate.Add((invoice, alloc.AllocatedAmount));
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var receiptNumber = $"CR-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var entity = new CashReceipt
            {
                ReceiptNumber = receiptNumber,
                ReceiptDate = req.ReceiptDate,
                CustomerId = req.CustomerId,
                PaymentMethod = req.PaymentMethod,
                ReceivedAmount = req.ReceivedAmount,
                DiscountAmount = req.DiscountAmount,
                BankAccountId = req.BankAccountId,
                ReferenceNumber = req.ReferenceNumber,
                Notes = req.Notes,
            };

            foreach (var alloc in req.Allocations)
            {
                entity.Allocations.Add(new CashReceiptAllocation
                {
                    SalesInvoiceId = alloc.SalesInvoiceId,
                    AllocatedAmount = alloc.AllocatedAmount,
                });
            }

            _db.CashReceipts.Add(entity);

            // 更新發票已付額，若達總額則標記已支付
            var paidInvoices = new List<string>();
            foreach (var (invoice, amount) in invoicesToUpdate)
            {
                invoice.PaidAmount += amount;
                if (invoice.PaidAmount >= invoice.TotalAmount)
                {
                    invoice.Status = "paid";
                    paidInvoices.Add(invoice.InvoiceNumber);
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var msg = paidInvoices.Any()
                ? $"收款完成，且以下發票已結清：{string.Join(", ", paidInvoices)}"
                : "收款完成，已更新帳款";

            return Ok(entity, msg);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, ApiHelper.Fail($"收款作業失敗：{ex.Message}"));
        }
    }

    /// <summary>
    /// 獲取待收款發票，供建立收款單時選擇
    /// </summary>
    [HttpGet("pending-invoices/{customerId}")]
    public async Task<ActionResult<ApiResponse<object>>> GetPendingInvoices(Guid customerId)
    {
        var invoices = await _db.SalesInvoices
            .Where(i => i.CustomerId == customerId && i.Status == "approved")
            .Select(i => new
            {
                i.Id,
                i.InvoiceNumber,
                i.InvoiceDate,
                i.DueDate,
                i.TotalAmount,
                i.PaidAmount,
                Outstanding = i.TotalAmount - i.PaidAmount,
            })
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync();

        return Ok<object>(invoices);
    }
}
