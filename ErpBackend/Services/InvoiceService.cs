using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;
using ErpBackend.Helpers;
using System.Data;

namespace ErpBackend.Services;

public interface IInvoiceService
{
    Task<List<InvoiceDto>> GetInvoicesAsync(string? status = null, string? customerId = null, string? startDate = null, string? endDate = null, string? searchTerm = null, int page = 1, int pageSize = 20);
    Task<InvoiceDto?> GetInvoiceAsync(Guid id);
    Task<(InvoiceDto? dto, string status)> CreateInvoiceAsync(CreateInvoiceRequest req);
    Task<(bool found, string status)> ApproveInvoiceAsync(Guid id, ApproveInvoiceRequest req);
    Task<(bool found, string status)> VoidInvoiceAsync(Guid id, VoidInvoiceRequest req);
    Task<List<AgingReportDto>> GetAgingReportAsync();
}

public class InvoiceService : IInvoiceService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IAccountingService _accountingService;

    public InvoiceService(ErpGeneralDbContext db, IAccountingService accountingService)
    {
        _db = db;
        _accountingService = accountingService;
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync(string? status = null, string? customerId = null, string? startDate = null, string? endDate = null, string? searchTerm = null, int page = 1, int pageSize = 20)
    {
        var query = _db.SalesInvoices
            .Include(i => i.Customer)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(i => i.Status == status);

        if (!string.IsNullOrEmpty(customerId) && Guid.TryParse(customerId, out var custGuid))
            query = query.Where(i => i.CustomerId == custGuid);

        if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            query = query.Where(i => i.InvoiceDate >= start);

        if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            query = query.Where(i => i.InvoiceDate <= end);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(i => i.InvoiceNumber.Contains(searchTerm));

        return await query
            .AsNoTracking()
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                CustomerId = i.CustomerId,
                CustomerName = i.Customer!.CustomerName,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                Currency = i.Currency,
                ExchangeRate = i.ExchangeRate,
                TaxType = i.TaxType,
                SalesAmount = i.SalesAmount,
                TaxAmount = i.TaxAmount,
                TotalAmount = i.TotalAmount,
                PaidAmount = i.PaidAmount,
                DiscountedAmount = i.DiscountedAmount,
                Outstanding = i.TotalAmount - i.PaidAmount - i.DiscountedAmount,
                Status = i.Status,
                Remarks = i.Remarks,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(Guid id)
    {
        var i = await _db.SalesInvoices
            .AsNoTracking()
            .Include(inv => inv.Customer)
            .Include(inv => inv.Items).ThenInclude(item => item.Sku)
            .FirstOrDefaultAsync(inv => inv.Id == id);

        if (i == null) return null;

        return new InvoiceDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            CustomerId = i.CustomerId,
            CustomerName = i.Customer?.CustomerName,
            SalesOrderId = i.SalesOrderId,
            DeliveryOrderId = i.DeliveryOrderId,
            InvoiceDate = i.InvoiceDate,
            DueDate = i.DueDate,
            Currency = i.Currency,
            ExchangeRate = i.ExchangeRate,
            TaxType = i.TaxType,
            TaxCode = i.TaxCode,
            SalesAmount = i.SalesAmount,
            TaxAmount = i.TaxAmount,
            TotalAmount = i.TotalAmount,
            PaidAmount = i.PaidAmount,
            DiscountedAmount = i.DiscountedAmount,
            Outstanding = i.TotalAmount - i.PaidAmount - i.DiscountedAmount,
            BaseSalesAmount = i.BaseSalesAmount,
            BaseTaxAmount = i.BaseTaxAmount,
            BaseTotalAmount = i.BaseTotalAmount,
            Status = i.Status,
            Remarks = i.Remarks,
            ApprovedBy = i.ApprovedBy,
            ApprovedAt = i.ApprovedAt,
            VoidedBy = i.VoidedBy,
            VoidedAt = i.VoidedAt,
            VoidReason = i.VoidReason,
            CreatedAt = i.CreatedAt,
            Items = i.Items.Select(item => new InvoiceItemDto
            {
                Id = item.Id,
                SkuId = item.SkuId,
                SkuNumber = item.Sku?.SkuNumber,
                SkuName = item.Sku?.SkuName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Amount
            }).ToList()
        };
    }

    public async Task<(InvoiceDto? dto, string status)> CreateInvoiceAsync(CreateInvoiceRequest req)
    {
        await using var transactionScope = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == req.CustomerId);
        if (customer == null) return (null, "customer_not_found");
        if (!req.DeliveryOrderId.HasValue || req.Items == null || req.Items.Count == 0 ||
            req.Items.Any(item => item.Quantity <= 0 || item.UnitPrice < 0))
            return (null, "invalid_invoice_items");

        var delivery = await _db.DeliveryOrders
            .Include(order => order.SalesOrder)
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == req.DeliveryOrderId.Value);
        if (delivery == null)
            return (null, "delivery_not_found");
        if (delivery.Status != "completed" ||
            req.SalesOrderId.HasValue && req.SalesOrderId.Value != delivery.SalesOrderId ||
            !await _db.SalesOrders.AnyAsync(order =>
                order.Id == delivery.SalesOrderId && order.CustomerId == req.CustomerId))
            return (null, "delivery_not_completed_or_customer_mismatch");

        var deliveredBySku = delivery.Items
            .GroupBy(item => item.SkuId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.DeliveredQty));
        var previouslyInvoicedItems = await _db.SalesInvoiceItems
            .Where(item => item.SalesInvoice.DeliveryOrderId == delivery.Id &&
                           item.SalesInvoice.Status != "voided")
            .Select(item => new { item.SkuId, item.Quantity })
            .ToListAsync();
        var invoicedBySku = previouslyInvoicedItems
            .GroupBy(item => item.SkuId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        foreach (var invoiceLine in req.Items.GroupBy(item => item.SkuId))
        {
            var invoicedQuantity = invoicedBySku.GetValueOrDefault(invoiceLine.Key);
            if (!deliveredBySku.TryGetValue(invoiceLine.Key, out var deliveredQuantity) ||
                invoicedQuantity + invoiceLine.Sum(item => item.Quantity) > deliveredQuantity)
                return (null, "invoice_quantity_exceeds_uninvoiced_delivery");
        }

        var invoice = new SalesInvoice
        {
            InvoiceNumber = DocumentNumberGenerator.Generate("INV"),
            CustomerId = req.CustomerId,
            SalesOrderId = req.SalesOrderId,
            DeliveryOrderId = req.DeliveryOrderId,
            InvoiceDate = req.InvoiceDate ?? DateTime.UtcNow,
            DueDate = req.DueDate,
            Currency = req.Currency ?? "TWD",
            ExchangeRate = req.ExchangeRate ?? 1,
            TaxType = req.TaxType ?? "taxable_5pct",
            TaxCode = req.TaxCode,
            Status = "draft"
        };

        decimal salesAmount = 0;
        if (req.Items != null)
        {
            foreach (var item in req.Items)
            {
                var amount = item.Quantity * item.UnitPrice;
                salesAmount += amount;
                invoice.Items.Add(new SalesInvoiceItem
                {
                    SalesInvoiceId = invoice.Id,
                    SkuId = item.SkuId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Amount = amount
                });
            }
        }

        invoice.SalesAmount = salesAmount;
        invoice.TaxAmount = invoice.TaxType == "taxable_5pct" ? salesAmount * 0.05m : 0;
        invoice.TotalAmount = salesAmount + invoice.TaxAmount;
        invoice.BaseSalesAmount = salesAmount * invoice.ExchangeRate;
        invoice.BaseTaxAmount = invoice.TaxAmount * invoice.ExchangeRate;
        invoice.BaseTotalAmount = invoice.TotalAmount * invoice.ExchangeRate;

        _db.SalesInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        if (transactionScope != null)
            await transactionScope.CommitAsync();

        return (new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId,
            InvoiceDate = invoice.InvoiceDate,
            SalesAmount = invoice.SalesAmount,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            Status = invoice.Status,
            CreatedAt = invoice.CreatedAt
        }, "ok");
    }

    public async Task<(bool found, string status)> ApproveInvoiceAsync(Guid id, ApproveInvoiceRequest req)
    {
        await using var transactionScope = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
        var invoice = await _db.SalesInvoices.FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null) return (false, "not_found");

        if (invoice.Status != "draft")
            return (true, "guarded");
        if (!invoice.DeliveryOrderId.HasValue ||
            !await _db.DeliveryOrders.AnyAsync(order =>
                order.Id == invoice.DeliveryOrderId.Value && order.Status == "completed"))
            return (true, "delivery_not_completed");

        if (await _accountingService.ValidateInvoiceMappingsAsync(invoice.TaxAmount > 0) is not null)
            return (true, "accounting_setup_missing");

        invoice.Status = "approved";
        invoice.ApprovedBy = req.ApprovedBy;
        invoice.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _accountingService.CreateVoucherFromInvoiceAsync(invoice.Id);
        if (transactionScope != null)
            await transactionScope.CommitAsync();

        return (true, "ok");
    }

    public async Task<(bool found, string status)> VoidInvoiceAsync(Guid id, VoidInvoiceRequest req)
    {
        var invoice = await _db.SalesInvoices.FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null) return (false, "not_found");

        if (invoice.Status == "voided")
            return (true, "guarded");

        invoice.Status = "voided";
        invoice.VoidedBy = req.VoidedBy;
        invoice.VoidedAt = DateTime.UtcNow;
        invoice.VoidReason = req.VoidReason;

        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    public async Task<List<AgingReportDto>> GetAgingReportAsync()
    {
        var invoices = await _db.SalesInvoices
            .Include(i => i.Customer)
            .AsNoTracking()
            .Where(i => i.Status == "approved" && i.DueDate < DateTime.UtcNow)
            .ToListAsync();

        var now = DateTime.UtcNow;
        return invoices
            .GroupBy(i => i.Customer)
            .Select(g => new AgingReportDto
            {
                CustomerId = g.Key.Id,
                CustomerName = g.Key.CustomerName,
                Current = g.Sum(i => i.TotalAmount - i.PaidAmount - i.DiscountedAmount),
                Days30 = g.Where(i => i.DueDate.HasValue && (now - i.DueDate.Value).TotalDays <= 30)
                    .Sum(i => i.TotalAmount - i.PaidAmount - i.DiscountedAmount),
                Days60 = g.Where(i => i.DueDate.HasValue && (now - i.DueDate.Value).TotalDays > 30 && (now - i.DueDate.Value).TotalDays <= 60)
                    .Sum(i => i.TotalAmount - i.PaidAmount - i.DiscountedAmount),
                Days90 = g.Where(i => i.DueDate.HasValue && (now - i.DueDate.Value).TotalDays > 60 && (now - i.DueDate.Value).TotalDays <= 90)
                    .Sum(i => i.TotalAmount - i.PaidAmount - i.DiscountedAmount),
                Over90 = g.Where(i => i.DueDate.HasValue && (now - i.DueDate.Value).TotalDays > 90)
                    .Sum(i => i.TotalAmount - i.PaidAmount - i.DiscountedAmount)
            })
            .ToList();
    }
}
