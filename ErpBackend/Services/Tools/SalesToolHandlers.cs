using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;

namespace ErpBackend.Services;

public sealed class SalesOrderSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public SalesOrderSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var soNumber = parameters.GetValueOrDefault("so_number")?.ToString();
        var status = parameters.GetValueOrDefault("status")?.ToString();

        var query = _db.SalesOrders.AsQueryable();

        if (!string.IsNullOrEmpty(soNumber))
            query = query.Where(s => s.OrderNumber.Contains(soNumber));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        var results = await query.Take(50).Select(s => new
        {
            s.OrderNumber,
            s.OrderDate,
            s.Status
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class CustomerSearchHandler : IToolHandler
{
    private readonly ErpGeneralDbContext _db;

    public CustomerSearchHandler(ErpGeneralDbContext db) => _db = db;

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var customerCode = parameters.GetValueOrDefault("customer_code")?.ToString();
        var name = parameters.GetValueOrDefault("name")?.ToString();

        var query = _db.Customers.AsQueryable();

        if (!string.IsNullOrEmpty(customerCode))
            query = query.Where(c => c.CustomerCode.Contains(customerCode));
        if (!string.IsNullOrEmpty(name))
            query = query.Where(c => c.CustomerName.Contains(name));

        var results = await query.Take(50).Select(c => new
        {
            c.CustomerCode,
            c.CustomerName,
            c.ContactPerson
        }).ToListAsync();

        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(results) };
    }
}

public sealed class CustomerCreditCheckHandler : IToolHandler
{
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        await Task.CompletedTask;
        var customerId = parameters.GetValueOrDefault("customer_id")?.ToString();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { CustomerId = customerId ?? "test", CreditLimit = 100000m, Used = 25000m, Available = 75000m, Status = "OK" }) };
    }
}

public sealed class DeliveryOrderCreateHandler : IToolHandler
{
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        await Task.CompletedTask;
        var soId = parameters.GetValueOrDefault("so_id")?.ToString();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { DeliveryId = Guid.NewGuid(), DeliveryNumber = $"DO-{DateTime.Now:yyyyMMddHHmmss}", Status = "created" }) };
    }
}

public sealed class ShipmentTrackHandler : IToolHandler
{
    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        await Task.CompletedTask;
        var deliveryNumber = parameters.GetValueOrDefault("delivery_number")?.ToString();
        return new ToolResult { Success = true, Data = JsonSerializer.Serialize(new { DeliveryNumber = deliveryNumber ?? "DO-001", Status = "in_transit", EstimatedArrival = DateTime.Today.AddDays(2) }) };
    }
}
