using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;
using ErpBackend.Helpers;

namespace ErpBackend.Controllers;

public class InvoicesController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public InvoicesController(ErpGeneralDbContext db)
    {
        _db = db;
    }

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

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(i => new InvoiceDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            CustomerId = i.CustomerId,
            CustomerName = i.Customer?.CustomerName,
            InvoiceDate = i.InvoiceDate,
            DueDate = i.DueDate,
            Currency = i.Currency,
            ExchangeRate = i.ExchangeRate,
            TaxType = i.TaxType,
            SalesAmount = i.SalesAmount,
            TaxAmount = i.TaxAmount,
            TotalAmount = i.TotalAmount,
            PaidAmount = i.PaidAmount,
            Status = i.Status,
            Remarks = i.Remarks,
            CreatedAt = i.CreatedAt
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoice(Guid id)
    {
        var i = await _db.SalesInvoices
            .Include(inv => inv.Customer)
            .Include(inv => inv.Items).ThenInclude(item => item.Sku)
            .FirstOrDefaultAsync(inv => inv.Id == id);

        if (i == null) return NotFound(ApiHelper.Fail("Invoice not found"));

        var dto = new InvoiceDto
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

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<InvoiceDto>>> CreateInvoice([FromBody] CreateInvoiceRequest req)
    {
        var customer = await _db.Customers.FindAsync(req.CustomerId);
        if (customer == null) return BadRequest(ApiHelper.Fail("Customer not found"));

        var invoice = new SalesInvoice
        {
            Id = Guid.NewGuid(),
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
                    Id = Guid.NewGuid(),
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

        var dto = new InvoiceDto
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
        };

        return Ok(dto);
    }

    [HttpPatch("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> ApproveInvoice(Guid id, [FromBody] ApproveInvoiceRequest req)
    {
        var invoice = await _db.SalesInvoices.FindAsync(id);
        if (invoice == null) return NotFound(ApiHelper.Fail("Invoice not found"));

        if (invoice.Status != "draft")
            return BadRequest(ApiHelper.Fail("Only draft invoices can be approved"));

        invoice.Status = "approved";
        invoice.ApprovedBy = req.ApprovedBy;
        invoice.ApprovedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Invoice approved");
    }

    [HttpPatch("{id}/void")]
    public async Task<ActionResult<ApiResponse>> VoidInvoice(Guid id, [FromBody] VoidInvoiceRequest req)
    {
        var invoice = await _db.SalesInvoices.FindAsync(id);
        if (invoice == null) return NotFound(ApiHelper.Fail("Invoice not found"));

        if (invoice.Status == "voided")
            return BadRequest(ApiHelper.Fail("Invoice already voided"));

        invoice.Status = "voided";
        invoice.VoidedBy = req.VoidedBy;
        invoice.VoidedAt = DateTime.UtcNow;
        invoice.VoidReason = req.VoidReason;

        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Invoice voided");
    }

    [HttpGet("aging")]
    public async Task<ActionResult<ApiResponse<List<AgingReportDto>>>> GetAgingReport()
    {
        var invoices = await _db.SalesInvoices
            .Include(i => i.Customer)
            .Where(i => i.Status == "approved" && i.DueDate < DateTime.UtcNow)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var aging = invoices
            .GroupBy(i => i.Customer)
            .Select(g => new AgingReportDto
            {
                CustomerId = g.Key.Id,
                CustomerName = g.Key.CustomerName,
                Current = g.Sum(i => i.TotalAmount - i.PaidAmount),
                Days30 = g.Where(i => i.DueDate.HasValue && (now - i.DueDate.Value).TotalDays <= 30).Sum(i => i.TotalAmount - i.PaidAmount),
                Days60 = g.Where(i => i.DueDate.HasValue && (now - i.DueDate.Value).TotalDays > 30 && (now - i.DueDate.Value).TotalDays <= 60).Sum(i => i.TotalAmount - i.PaidAmount),
                Days90 = g.Where(i => i.DueDate.HasValue && (now - i.DueDate.Value).TotalDays > 60 && (now - i.DueDate.Value).TotalDays <= 90).Sum(i => i.TotalAmount - i.PaidAmount),
                Over90 = g.Where(i => i.DueDate.HasValue && (now - i.DueDate.Value).TotalDays > 90).Sum(i => i.TotalAmount - i.PaidAmount)
            }).ToList();

        return Ok(aging);
    }
}

