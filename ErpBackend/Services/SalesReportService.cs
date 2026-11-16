using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace ErpBackend.Services;

public interface ISalesReportService
{
    Task<object> GetOrderToCashAsync(DateTime? startDate, DateTime? endDate, bool includeAll = false);
    Task<object> GetArAgingAsync();
    Task<object> GetMonthlySummaryAsync(int? year);
    Task<object> GetCreditUsageAsync();
    Task<List<MonthlyPeriodStatusDto>> GetMonthlyPeriodsAsync(int? year);
}

public class SalesReportService : ISalesReportService
{
    private readonly ErpGeneralDbContext _db;

    public SalesReportService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<object> GetOrderToCashAsync(DateTime? startDate, DateTime? endDate, bool includeAll = false)
    {
        var now = DateTime.UtcNow;
        DateTime? sDate = includeAll
            ? null
            : startDate.HasValue
                ? DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc)
                : now.AddMonths(-3);
        DateTime? requestedEndDate = includeAll
            ? null
            : endDate.HasValue
                ? DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc)
                : DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
        var eDateExclusive = includeAll || !endDate.HasValue
            ? now
            : DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc).AddDays(1);

        var query = _db.SalesOrders
            .Include(so => so.Customer)
            .AsQueryable();

        if (!includeAll)
            query = query.Where(so => so.OrderDate < eDateExclusive);

        if (!includeAll && sDate.HasValue)
            query = query.Where(so => so.OrderDate >= sDate.Value);

        query = query
            .AsNoTracking();

        var totalOrders = await query.CountAsync();
        var totalAmount = await query.SumAsync(so => (decimal?)so.TotalAmount) ?? 0m;
        var pendingOrders = await query.CountAsync(so => so.Status == "pending" || so.Status == "draft");
        var confirmedOrders = await query.CountAsync(so => so.Status == "confirmed");
        var deliveredOrders = await query.CountAsync(so => so.Status == "delivered");

        var orders = await query
            .OrderByDescending(so => so.OrderDate)
            .Take(200)
            .ToListAsync();

        var rows = new List<object>();
        foreach (var order in orders)
        {
            var hasDelivery = await _db.DeliveryOrders.AnyAsync(d => d.SalesOrderId == order.Id);
            var invoices = await _db.SalesInvoices
                .Where(invoice => invoice.SalesOrderId == order.Id
                    && invoice.Status != "void"
                    && invoice.Status != "voided")
                .Select(invoice => new { invoice.PaidAmount })
                .ToListAsync();
            var hasInvoice = invoices.Count > 0;
            var collectedAmount = invoices.Sum(invoice => invoice.PaidAmount);
            var collectionRate = order.TotalAmount <= 0
                ? 0
                : Math.Min(100, decimal.Round(collectedAmount / order.TotalAmount * 100, 1));
            var hasPayment = collectedAmount > 0;

            var stage = order.Status == "draft" || order.Status == "pending" ? "order"
                      : hasDelivery && !hasInvoice ? "delivery"
                      : hasInvoice && collectionRate < 100 ? (hasPayment ? "partial_payment" : "invoice")
                      : hasPayment ? "payment"
                      : "order";

            rows.Add(new
            {
                orderId = order.Id,
                orderNumber = order.OrderNumber,
                customerName = order.Customer?.CustomerName ?? "",
                orderDate = order.OrderDate,
                orderAmount = order.TotalAmount,
                stage,
                pipelineStatus = stage switch
                {
                    "order" => order.Status == "confirmed" ? "in_progress" : "pending",
                    "delivery" => "shipped",
                    "invoice" => "invoiced",
                    "partial_payment" => "partial_collection",
                    "payment" => "completed",
                    _ => "pending"
                },
                collectedAmount,
                collectionRate,
                deliveryCount = hasDelivery ? 1 : 0,
                invoiceCount = invoices.Count,
                paymentCount = hasPayment ? 1 : 0
            });
        }

        var summary = new
        {
            totalOrders,
            totalAmount,
            pendingOrders,
            confirmedOrders,
            deliveredOrders,
            invoicedOrders = rows.Count(r => ((dynamic)r).stage == "invoice"),
            paidOrders = rows.Count(r => ((dynamic)r).stage == "payment"),
            startDate = includeAll ? (DateTime?)null : sDate,
            endDate = requestedEndDate
        };

        return new { rows, summary, rowLimit = 200 };
    }

    public async Task<object> GetArAgingAsync()
    {
        var invoices = await _db.SalesInvoices
            .Include(si => si.Customer)
            .Where(si => si.Status != "void" && si.Status != "paid")
            .ToListAsync();

        var aging = invoices.Select(i =>
        {
            var dueDate = i.DueDate ?? i.InvoiceDate.AddDays(30);
            var daysOverdue = (DateTime.UtcNow - dueDate).Days;
            var bucket = daysOverdue <= 0 ? "current"
                       : daysOverdue <= 30 ? "1-30"
                       : daysOverdue <= 60 ? "31-60"
                       : daysOverdue <= 90 ? "61-90"
                       : "over90";

            return new
            {
                invoiceId = i.Id,
                invoiceNumber = i.InvoiceNumber,
                customerName = i.Customer?.CustomerName ?? "",
                invoiceDate = i.InvoiceDate,
                dueDate = dueDate,
                daysOverdue = Math.Max(0, daysOverdue),
                totalAmount = i.TotalAmount,
                paidAmount = i.PaidAmount,
                discountedAmount = i.DiscountedAmount,
                outstanding = i.TotalAmount - i.PaidAmount - i.DiscountedAmount,
                bucket
            };
        }).ToList();

        var summary = new
        {
            current = aging.Where(a => a.bucket == "current").Sum(a => a.outstanding),
            days30 = aging.Where(a => a.bucket == "1-30").Sum(a => a.outstanding),
            days60 = aging.Where(a => a.bucket == "31-60").Sum(a => a.outstanding),
            days90 = aging.Where(a => a.bucket == "61-90").Sum(a => a.outstanding),
            over90 = aging.Where(a => a.bucket == "over90").Sum(a => a.outstanding),
            totalOutstanding = aging.Sum(a => a.outstanding)
        };

        return new { aging, summary };
    }

    public async Task<object> GetMonthlySummaryAsync(int? year)
    {
        var y = year ?? DateTime.UtcNow.Year;

        var monthlyData = await _db.SalesOrders
            .Where(so => so.OrderDate.Year == y)
            .GroupBy(so => so.OrderDate.Month)
            .Select(g => new
            {
                month = g.Key,
                orderCount = g.Count(),
                totalAmount = g.Sum(so => so.TotalAmount),
                confirmedCount = g.Count(so => so.Status == "confirmed" || so.Status == "delivered" || so.Status == "closed")
            })
            .OrderBy(g => g.month)
            .ToListAsync();

        var months = Enumerable.Range(1, 12).Select(m => new
        {
            month = m,
            orderCount = monthlyData.FirstOrDefault(d => d.month == m)?.orderCount ?? 0,
            totalAmount = monthlyData.FirstOrDefault(d => d.month == m)?.totalAmount ?? 0,
            confirmedCount = monthlyData.FirstOrDefault(d => d.month == m)?.confirmedCount ?? 0
        }).ToList();

        var summary = new
        {
            year = y,
            totalOrders = months.Sum(m => m.orderCount),
            totalAmount = months.Sum(m => m.totalAmount),
            totalConfirmed = months.Sum(m => m.confirmedCount)
        };

        return new { months, summary };
    }

    public async Task<object> GetCreditUsageAsync()
    {
        var customers = await _db.Customers
            .Where(c => c.IsActive)
            .Select(c => new
            {
                customerId = c.Id,
                customerCode = c.CustomerCode,
                customerName = c.CustomerName,
                creditLimit = c.CreditLimit,
                totalOutstanding = _db.SalesInvoices
                    .Where(si => si.CustomerId == c.Id && si.Status != "paid" && si.Status != "void")
                    .Sum(si => (decimal?)(si.TotalAmount - si.PaidAmount - si.DiscountedAmount)) ?? 0
            })
            .ToListAsync();

        var usage = customers.Select(c => new
        {
            c.customerId,
            c.customerCode,
            c.customerName,
            c.creditLimit,
            c.totalOutstanding,
            availableCredit = c.creditLimit - c.totalOutstanding,
            usageRate = c.creditLimit > 0 ? Math.Round(c.totalOutstanding / c.creditLimit * 100, 1) : 0
        }).ToList();

        var summary = new
        {
            totalCreditLimit = customers.Sum(c => c.creditLimit),
            totalOutstanding = customers.Sum(c => c.totalOutstanding),
            totalAvailable = customers.Sum(c => c.creditLimit - c.totalOutstanding),
            overallUsageRate = customers.Sum(c => c.creditLimit) > 0
                ? Math.Round(customers.Sum(c => c.totalOutstanding) / customers.Sum(c => c.creditLimit) * 100, 1)
                : 0
        };

        return new { usage, summary };
    }

    public async Task<List<MonthlyPeriodStatusDto>> GetMonthlyPeriodsAsync(int? year)
    {
        var query = _db.MonthlyPeriods.AsQueryable();
        if (year.HasValue) query = query.Where(p => p.Year == year.Value);

        var items = await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync();
        return items.Select(p => new MonthlyPeriodStatusDto
        {
            Id = p.Id,
            Year = p.Year,
            Month = p.Month,
            IsClosed = p.Status == "closed",
            ClosedAt = p.ClosedAt,
            ClosedBy = p.ClosedBy?.ToString(),
            Notes = p.Notes
        }).ToList();
    }
}
