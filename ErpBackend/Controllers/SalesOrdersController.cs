using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class SalesOrdersController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    private readonly IStateMachineService _stateMachine;

    public SalesOrdersController(ErpGeneralDbContext db, IStateMachineService stateMachine)
    {
        _db = db;
        _stateMachine = stateMachine;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SalesOrderDto>>>> GetSalesOrders(
        [FromQuery] string? status,
        [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.Salesperson)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(so => so.Status == status);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(so => so.OrderNumber.Contains(searchTerm) || so.Customer.CustomerName.Contains(searchTerm));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(so => so.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(so => new SalesOrderDto
        {
            Id = so.Id,
            OrderNumber = so.OrderNumber,
            CustomerId = so.CustomerId,
            CustomerName = so.Customer?.CustomerName,
            OrderDate = so.OrderDate,
            TotalAmount = so.TotalAmount,
            Status = so.Status,
            CustomerPoNumber = so.CustomerPoNumber,
            SalespersonId = so.SalespersonId,
            SalespersonName = so.Salesperson?.ChineseName,
            CreatedAt = so.CreatedAt
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SalesOrderDto>>> GetSalesOrder(Guid id)
    {
        var so = await _db.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.Salesperson)
            .Include(so => so.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(so => so.Id == id);

        if (so == null) return NotFound(ApiHelper.Fail("Error"));

        var dto = new SalesOrderDto
        {
            Id = so.Id,
            OrderNumber = so.OrderNumber,
            CustomerId = so.CustomerId,
            CustomerName = so.Customer?.CustomerName,
            OrderDate = so.OrderDate,
            TotalAmount = so.TotalAmount,
            Status = so.Status,
            CustomerPoNumber = so.CustomerPoNumber,
            SalespersonId = so.SalespersonId,
            SalespersonName = so.Salesperson?.ChineseName,
            CreatedAt = so.CreatedAt,
            Items = so.Items.Select(i => new SalesOrderItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                LineNumber = i.LineNumber,
                Status = i.Status,
                PlannedFinishDate = i.PlannedFinishDate
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost("{id}/status")]
    public async Task<object> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var so = await _db.SalesOrders.FindAsync(id);
        if (so == null) return NotFound(new { error = "Error" });

        // 狀態機鎖定：防止狀態逆流
        var error = _stateMachine.ValidateTransition("sales_order", so.Status, request.NewStatus);
        if (error != null)
            return BadRequest(new { error });

        var oldStatus = so.Status;
        so.Status = request.NewStatus;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"狀態從 {oldStatus} 變更為 {request.NewStatus}" });
    }
}

public class UpdateStatusRequest
{
    public string NewStatus { get; set; } = null!;
}
