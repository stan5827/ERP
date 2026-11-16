using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;
using ErpBackend.Services;
using ErpBackend.Helpers;

namespace ErpBackend.Services;

public interface ISalesOrderService
{
    Task<List<SalesOrderDto>> GetSalesOrdersAsync(string? status = null, string? searchTerm = null, int page = 1, int pageSize = 20);
    Task<SalesOrderDto?> GetSalesOrderAsync(Guid id);
    Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderRequest request);
    Task<(bool found, string status, SalesOrderConfirmationDto? result)> ConfirmSalesOrderAsync(string idOrNumber);
    Task<(bool found, string status)> UpdateManufacturingStatusAsync(Guid id, UpdateManufacturingStatusRequest request);
    Task<(bool found, string error, string status)> UpdateStatusAsync(Guid id, UpdateStatusRequest request);
}

public class SalesOrderService : ISalesOrderService
{
    private readonly ErpGeneralDbContext _db;
    private readonly IStateMachineService _stateMachine;

    public SalesOrderService(ErpGeneralDbContext db, IStateMachineService stateMachine)
    {
        _db = db;
        _stateMachine = stateMachine;
    }

    public async Task<List<SalesOrderDto>> GetSalesOrdersAsync(string? status = null, string? searchTerm = null, int page = 1, int pageSize = 20)
    {
        var query = _db.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.Salesperson)
            .Include(so => so.Items)
                .ThenInclude(item => item.Sku)
                    .ThenInclude(sku => sku!.SalesUom)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(so => so.Status == status);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(so => so.OrderNumber.Contains(searchTerm) || so.Customer.CustomerName.Contains(searchTerm));

        var items = await query
            .AsNoTracking()
            .OrderByDescending(so => so.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return items.Select(so => MapToDto(so)).ToList();
    }

    public async Task<SalesOrderDto?> GetSalesOrderAsync(Guid id)
    {
        var so = await _db.SalesOrders
            .AsNoTracking()
            .Include(so => so.Customer)
            .Include(so => so.Salesperson)
            .Include(so => so.Items).ThenInclude(i => i.Sku).ThenInclude(sku => sku!.SalesUom)
            .FirstOrDefaultAsync(so => so.Id == id);

        return so == null ? null : MapToDto(so);
    }

    public async Task<SalesOrderDto> CreateSalesOrderAsync(CreateSalesOrderRequest request)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CustomerId);
        if (customer == null) throw new KeyNotFoundException("Customer not found");

        var orderNumber = DocumentNumberGenerator.GenerateSalesOrder();
        var subtotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
        var taxAmount = subtotal * 0.05m;
        var totalAmount = subtotal + taxAmount;

        var order = new SalesOrder
        {
            OrderNumber = orderNumber,
            CustomerId = request.CustomerId,
            OrderDate = request.OrderDate ?? DateTime.UtcNow,
            Currency = customer.Currency,
            Status = "pending",
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            CustomerPoNumber = request.CustomerPoNumber,
            PaymentTerms = request.PaymentTerms,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            OrderRemarks = request.OrderRemarks,
            CreatedAt = DateTime.UtcNow
        };

        int lineNo = 1;
        foreach (var item in request.Items)
        {
            order.Items.Add(new SalesOrderItem
            {
                LineNumber = lineNo++,
                ProductId = item.SkuId,
                SkuId = item.SkuId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountRate = 1.0m,
                Subtotal = item.Quantity * item.UnitPrice,
                Status = "pending",
                NeedManufacturing = item.NeedManufacturing,
                NeedOutsourcing = item.NeedOutsourcing
            });
        }

        _db.SalesOrders.Add(order);
        await _db.SaveChangesAsync();

        await _db.Entry(order).Reference(so => so.Customer).LoadAsync();
        await _db.Entry(order).Collection(so => so.Items).LoadAsync();
        foreach (var item in order.Items)
            await _db.Entry(item).Reference(i => i.Sku).LoadAsync();

        return MapToDto(order);
    }

    public async Task<(bool found, string status, SalesOrderConfirmationDto? result)> ConfirmSalesOrderAsync(string idOrNumber)
    {
        var so = await FindSalesOrderAsync(idOrNumber);
        if (so == null) return (false, "not_found", null);

        if (so.Status != "pending")
            return (true, "invalid_state", null);

        var unpaidInvoices = await _db.SalesInvoices
            .Where(invoice => invoice.CustomerId == so.CustomerId
                && invoice.Status != "paid"
                && invoice.Status != "void"
                && invoice.Status != "voided")
            .Select(invoice => new { invoice.TotalAmount, invoice.PaidAmount, invoice.DiscountedAmount })
            .ToListAsync();
        var invoiceOutstanding = unpaidInvoices.Sum(invoice =>
            invoice.TotalAmount - invoice.PaidAmount - invoice.DiscountedAmount);
        var openOrders = await _db.SalesOrders
            .Where(order => order.CustomerId == so.CustomerId
                && order.Id != so.Id
                && (order.Status == "pending" || order.Status == "confirmed" || order.Status == "partial")
                && !_db.SalesInvoices.Any(invoice => invoice.SalesOrderId == order.Id))
            .Select(order => order.TotalAmount)
            .ToListAsync();
        var openOrderCommitments = openOrders.Sum();
        var creditUsageBeforeOrder = invoiceOutstanding + openOrderCommitments;
        var creditUsageAfterOrder = creditUsageBeforeOrder + so.TotalAmount;

        var inventoryWarnings = new List<SalesOrderInventoryWarningDto>();
        foreach (var item in so.Items)
        {
            var sku = item.Sku;
            if (sku == null)
            {
                inventoryWarnings.Add(new SalesOrderInventoryWarningDto
                {
                    LineNumber = item.LineNumber,
                    RequiredQty = item.Quantity,
                    AvailableQty = 0,
                    ShortageQty = item.Quantity
                });
                continue;
            }

            var reservations = await _db.InventoryReservations
                .Where(reservation => reservation.SkuId == sku.Id && reservation.Status == "reserved")
                .Select(reservation => reservation.Qty)
                .ToListAsync();
            var reserved = reservations.Sum();
            var quarantinedReports = await _db.IqcReports
                .Where(report => report.SkuId == sku.Id
                    && report.GoodsReceiptItemId != null
                    && report.Result != "pass"
                    && report.Result != "not_required")
                .Select(report => report.ReceivedQty)
                .ToListAsync();
            var quarantined = quarantinedReports.Sum();
            var available = sku.Qty - reserved - quarantined;
            if (available < item.Quantity)
            {
                inventoryWarnings.Add(new SalesOrderInventoryWarningDto
                {
                    LineNumber = item.LineNumber,
                    SkuNumber = sku.SkuNumber,
                    SkuName = sku.SkuName,
                    RequiredQty = item.Quantity,
                    AvailableQty = available,
                    ShortageQty = item.Quantity - available
                });
            }
        }

        so.Status = "confirmed";
        so.CreditChecked = true;
        so.ConfirmedAt = DateTime.UtcNow;
        so.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, "ok", new SalesOrderConfirmationDto
        {
            OrderNumber = so.OrderNumber,
            CreditLimit = so.Customer.CreditLimit,
            CreditUsageBeforeOrder = creditUsageBeforeOrder,
            CreditUsageAfterOrder = creditUsageAfterOrder,
            CreditLimitExceeded = so.Customer.CreditLimit > 0 && creditUsageAfterOrder > so.Customer.CreditLimit,
            InventoryWarnings = inventoryWarnings
        });
    }

    public async Task<(bool found, string status)> UpdateManufacturingStatusAsync(Guid id, UpdateManufacturingStatusRequest request)
    {
        var so = await _db.SalesOrders.FirstOrDefaultAsync(so => so.Id == id);
        if (so == null) return (false, "not_found");

        var newStatus = request.NewStatus ?? request.Status;
        if (string.IsNullOrEmpty(newStatus))
            return (true, "invalid");

        so.ManufacturingStatus = newStatus;
        so.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, "ok");
    }

    public async Task<(bool found, string error, string status)> UpdateStatusAsync(Guid id, UpdateStatusRequest request)
    {
        var so = await _db.SalesOrders.FirstOrDefaultAsync(so => so.Id == id);
        if (so == null) return (false, "not_found", "");

        var error = _stateMachine.ValidateTransition("sales_order", so.Status, request.NewStatus);
        if (error != null)
            return (true, error, "blocked");

        so.Status = request.NewStatus;
        so.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, "", "ok");
    }

    private async Task<SalesOrder?> FindSalesOrderAsync(string idOrNumber)
    {
        if (Guid.TryParse(idOrNumber, out var gid))
            return await _db.SalesOrders
                .Include(x => x.Customer)
                .Include(x => x.Items).ThenInclude(i => i.Sku).ThenInclude(sku => sku!.SalesUom)
                .FirstOrDefaultAsync(x => x.Id == gid);

        return await _db.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.Items).ThenInclude(i => i.Sku).ThenInclude(sku => sku!.SalesUom)
            .FirstOrDefaultAsync(x => x.OrderNumber == idOrNumber);
    }

    private static SalesOrderDto MapToDto(SalesOrder so) => new()
    {
        Id = so.Id,
        OrderNumber = so.OrderNumber,
        CustomerId = so.CustomerId,
        CustomerName = so.Customer?.CustomerName,
        OrderDate = so.OrderDate,
        TotalAmount = so.TotalAmount,
        Status = so.Status,
        CreditChecked = so.CreditChecked,
        CustomerPoNumber = so.CustomerPoNumber,
        SalespersonId = so.SalespersonId,
        SalespersonName = so.Salesperson?.ChineseName,
        CreatedAt = so.CreatedAt,
        Items = so.Items.Select(i => new SalesOrderItemDto
        {
            Id = i.Id,
            OrderId = i.SalesOrderId,
            SkuId = i.SkuId ?? Guid.Empty,
            SkuNumber = i.Sku?.SkuNumber,
            SkuName = i.Sku?.SkuName,
            Unit = i.Sku?.SalesUom?.UomCode ?? i.Sku?.Unit,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            DiscountRate = i.DiscountRate,
            Subtotal = i.Subtotal,
            LineNumber = i.LineNumber,
            Status = i.Status,
            PlannedFinishDate = i.PlannedFinishDate,
            NeedManufacturing = i.NeedManufacturing,
            NeedOutsourcing = i.NeedOutsourcing
        }).ToList()
    };
}
