using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Helpers;
using ErpBackend.Models.Sales;
using System.Data;

namespace ErpBackend.Services;

public interface ICashReceiptService
{
    Task<List<CashReceiptDto>> GetCashReceiptsAsync(Guid? customerId = null, int page = 1, int pageSize = 20);
    Task<(CashReceiptDto? dto, string status)> CreateAsync(CreateCashReceiptRequest req);
    Task<(CashReceiptDto? dto, string status)> ApplyUnappliedAsync(
        Guid receiptId,
        ApplyCashReceiptUnappliedRequestDto req);
    Task<List<PendingInvoiceDto>> GetPendingInvoicesAsync(Guid customerId);
}

public class CashReceiptService : ICashReceiptService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IAccountingService _accountingService;

    public CashReceiptService(ErpGeneralDbContext db, IAccountingService accountingService)
    {
        _db = db;
        _accountingService = accountingService;
    }

    public async Task<List<CashReceiptDto>> GetCashReceiptsAsync(Guid? customerId = null, int page = 1, int pageSize = 20)
    {
        var query = _db.CashReceipts
            .Include(cr => cr.Allocations)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(cr => cr.CustomerId == customerId.Value);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(cr => cr.ReceiptDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return items.Select(cr => new CashReceiptDto
        {
            Id = cr.Id,
            ReceiptNumber = cr.ReceiptNumber,
            ReceiptDate = cr.ReceiptDate,
            CustomerId = cr.CustomerId,
            PaymentMethod = cr.PaymentMethod,
            ReceivedAmount = cr.ReceivedAmount,
            DiscountAmount = cr.DiscountAmount,
            Currency = cr.Currency,
            UnappliedAmount = cr.ReceivedAmount - cr.Allocations.Sum(allocation => allocation.AllocatedAmount),
            BankAccountId = cr.BankAccountId,
            ReferenceNumber = cr.ReferenceNumber,
            Notes = cr.Notes,
            CreatedAt = cr.CreatedAt,
            Allocations = cr.Allocations.Select(a => new CashReceiptAllocationDto
            {
                Id = a.Id,
                SalesInvoiceId = a.SalesInvoiceId,
                AllocatedAmount = a.AllocatedAmount,
                DiscountAmount = a.DiscountAmount,
                ExchangeGainLoss = a.ExchangeGainLoss
            }).ToList()
        }).ToList();
    }

    public async Task<(CashReceiptDto? dto, string status)> CreateAsync(CreateCashReceiptRequest req)
    {
        if (req.Allocations is null || req.ReceivedAmount <= 0 || req.DiscountAmount < 0 ||
            string.IsNullOrWhiteSpace(req.Currency) || req.Currency.Trim().Length > 10 ||
            req.Allocations.Any(allocation =>
                allocation is null ||
                allocation.AllocatedAmount < 0 ||
                allocation.DiscountAmount < 0 ||
                allocation.AllocatedAmount + allocation.DiscountAmount <= 0))
            return (null, "invalid");

        var allocations = req.Allocations
            .GroupBy(allocation => allocation.SalesInvoiceId)
            .Select(group => new
            {
                SalesInvoiceId = group.Key,
                AllocatedAmount = group.Sum(item => item.AllocatedAmount),
                DiscountAmount = group.Sum(item => item.DiscountAmount)
            })
            .ToList();
        var totalAllocated = allocations.Sum(allocation => allocation.AllocatedAmount);
        var totalDiscount = allocations.Sum(allocation => allocation.DiscountAmount);
        if (totalAllocated > req.ReceivedAmount || totalDiscount != req.DiscountAmount)
            return (null, "receipt_amount_mismatch");
        if (totalDiscount > 0)
            return (null, "discount_policy_pending");
        var unappliedAmount = req.ReceivedAmount - totalAllocated;

        if (!req.BankAccountId.HasValue ||
            !await _db.BankAccounts.AnyAsync(account =>
                account.Id == req.BankAccountId.Value && account.IsActive &&
                account.GLAccountId != null &&
                account.GLAccount!.IsActive && account.GLAccount.IsCashAccount))
            return (null, "cash_account_mapping_required");
        var mappingStatus = await _accountingService.ValidateCashReceiptMappingsAsync(
            unappliedAmount > 0);
        if (mappingStatus is not null)
            return (null, mappingStatus);

        await using var transactionScope = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var bankAccount = await _db.BankAccounts.FirstAsync(account =>
            account.Id == req.BankAccountId!.Value);
        if (!bankAccount.Currency.Equals(req.Currency, StringComparison.OrdinalIgnoreCase))
            return (null, "currency_mismatch");

        var invoicesToUpdate = new List<SalesInvoice>();
        foreach (var alloc in allocations)
        {
            var invoice = await _db.SalesInvoices.FirstOrDefaultAsync(i => i.Id == alloc.SalesInvoiceId);
            if (invoice == null)
                return (null, "not_found_invoice");
            if (invoice.CustomerId != req.CustomerId)
                return (null, "customer_mismatch");
            if (invoice.Status != "approved")
                return (null, "invoice_not_approved");
            if (!invoice.Currency.Equals(req.Currency, StringComparison.OrdinalIgnoreCase))
                return (null, "currency_mismatch");

            var outstanding = invoice.TotalAmount - invoice.PaidAmount - invoice.DiscountedAmount;
            if (alloc.AllocatedAmount + alloc.DiscountAmount > outstanding)
                return (null, "over_allocation");

            invoicesToUpdate.Add(invoice);
        }

        var receiptNumber = DocumentNumberGenerator.GenerateCashReceipt();
        var entity = new CashReceipt
        {
            ReceiptNumber = receiptNumber,
            ReceiptDate = req.ReceiptDate,
            CustomerId = req.CustomerId,
            PaymentMethod = req.PaymentMethod,
            ReceivedAmount = req.ReceivedAmount,
            DiscountAmount = req.DiscountAmount,
            Currency = req.Currency.Trim().ToUpperInvariant(),
            BankAccountId = req.BankAccountId,
            ReferenceNumber = req.ReferenceNumber,
            Notes = req.Notes,
        };

        foreach (var alloc in allocations)
        {
            entity.Allocations.Add(new CashReceiptAllocation
            {
                SalesInvoiceId = alloc.SalesInvoiceId,
                AllocatedAmount = alloc.AllocatedAmount,
                DiscountAmount = alloc.DiscountAmount,
            });
        }

        _db.CashReceipts.Add(entity);
        bankAccount.CurrentBalance += req.ReceivedAmount;

        var settledInvoiceCount = 0;
        foreach (var invoice in invoicesToUpdate)
        {
            var allocation = allocations.Single(item => item.SalesInvoiceId == invoice.Id);
            invoice.PaidAmount += allocation.AllocatedAmount;
            invoice.DiscountedAmount += allocation.DiscountAmount;
            if (invoice.PaidAmount + invoice.DiscountedAmount >= invoice.TotalAmount)
            {
                invoice.Status = "paid";
                settledInvoiceCount++;
            }
        }

        await _db.SaveChangesAsync();
        await _accountingService.CreateVoucherFromCashReceiptAsync(entity.Id);
        if (transactionScope != null)
            await transactionScope.CommitAsync();

        var dto = MapToDto(entity);
        return (dto, unappliedAmount > 0 ? "unapplied" : settledInvoiceCount > 0 ? "paid" : "ok");
    }

    public async Task<(CashReceiptDto? dto, string status)> ApplyUnappliedAsync(
        Guid receiptId,
        ApplyCashReceiptUnappliedRequestDto req)
    {
        if (req is null || req.SalesInvoiceId == Guid.Empty || req.Amount <= 0)
            return (null, "invalid");

        var mappingStatus = await _accountingService.ValidateCashReceiptMappingsAsync(
            requiresUnapplied: true);
        if (mappingStatus is not null)
            return (null, mappingStatus);
        if (!await _db.GLAccounts.AnyAsync(account =>
                account.AccountCode == "1131" && account.IsActive))
            return (null, "ar_account_missing");

        await using var transactionScope = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var receipt = await _db.CashReceipts
            .Include(item => item.Allocations)
            .FirstOrDefaultAsync(item => item.Id == receiptId);
        if (receipt is null)
            return (null, "not_found_receipt");

        var unappliedAmount = receipt.ReceivedAmount -
                              receipt.Allocations.Sum(allocation => allocation.AllocatedAmount);
        if (req.Amount > unappliedAmount)
            return (null, "over_unapplied");

        var invoice = await _db.SalesInvoices
            .FirstOrDefaultAsync(item => item.Id == req.SalesInvoiceId);
        if (invoice is null)
            return (null, "not_found_invoice");
        if (invoice.CustomerId != receipt.CustomerId)
            return (null, "customer_mismatch");
        if (invoice.Status != "approved")
            return (null, "invoice_not_approved");
        if (!invoice.Currency.Equals(receipt.Currency, StringComparison.OrdinalIgnoreCase))
            return (null, "currency_mismatch");
        if (req.Amount > invoice.TotalAmount - invoice.PaidAmount - invoice.DiscountedAmount)
            return (null, "over_allocation");

        var allocation = new CashReceiptAllocation
        {
            CashReceiptId = receipt.Id,
            CashReceipt = receipt,
            SalesInvoiceId = invoice.Id,
            AllocatedAmount = req.Amount
        };
        _db.CashReceiptAllocations.Add(allocation);
        invoice.PaidAmount += req.Amount;
        if (invoice.PaidAmount + invoice.DiscountedAmount >= invoice.TotalAmount)
            invoice.Status = "paid";

        await _accountingService.CreateVoucherFromCashReceiptApplicationAsync(
            receipt.Id,
            allocation.Id,
            req.Amount);
        if (transactionScope is not null)
            await transactionScope.CommitAsync();

        return (MapToDto(receipt), invoice.Status == "paid" ? "paid" : "ok");
    }

    public async Task<List<PendingInvoiceDto>> GetPendingInvoicesAsync(Guid customerId)
    {
        return await _db.SalesInvoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.Status == "approved")
            .Select(i => new PendingInvoiceDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                TotalAmount = i.TotalAmount,
                PaidAmount = i.PaidAmount,
                DiscountedAmount = i.DiscountedAmount,
                Outstanding = i.TotalAmount - i.PaidAmount - i.DiscountedAmount,
                Currency = i.Currency,
            })
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync();
    }

    private static CashReceiptDto MapToDto(CashReceipt cr) => new()
    {
        Id = cr.Id,
        ReceiptNumber = cr.ReceiptNumber,
        ReceiptDate = cr.ReceiptDate,
        CustomerId = cr.CustomerId,
        PaymentMethod = cr.PaymentMethod,
        ReceivedAmount = cr.ReceivedAmount,
        DiscountAmount = cr.DiscountAmount,
        Currency = cr.Currency,
        UnappliedAmount = cr.ReceivedAmount - cr.Allocations.Sum(allocation => allocation.AllocatedAmount),
        BankAccountId = cr.BankAccountId,
        ReferenceNumber = cr.ReferenceNumber,
        Notes = cr.Notes,
        CreatedAt = cr.CreatedAt,
        Allocations = cr.Allocations.Select(a => new CashReceiptAllocationDto
        {
            Id = a.Id,
            SalesInvoiceId = a.SalesInvoiceId,
            AllocatedAmount = a.AllocatedAmount,
            DiscountAmount = a.DiscountAmount,
            ExchangeGainLoss = a.ExchangeGainLoss
        }).ToList()
    };
}
