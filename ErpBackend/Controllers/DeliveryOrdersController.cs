using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;
using ErpBackend.Helpers;

namespace ErpBackend.Controllers;

public class DeliveryOrdersController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public DeliveryOrdersController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DeliveryOrderDto>>>> GetDeliveryOrders([FromQuery] string? status, [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.DeliveryOrders
            .Include(d => d.SalesOrder)
                .ThenInclude(s => s!.Customer)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == status);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(d => d.DeliveryNumber.Contains(searchTerm) || 
                (d.SalesOrder != null && d.SalesOrder.Customer != null && d.SalesOrder.Customer.CustomerName.Contains(searchTerm)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.DeliveryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(d => new DeliveryOrderDto
        {
            Id = d.Id,
            DeliveryNumber = d.DeliveryNumber,
            SalesOrderId = d.SalesOrderId,
            SalesOrderNumber = d.SalesOrder?.OrderNumber,
            CustomerName = d.SalesOrder?.Customer?.CustomerName,
            DeliveryDate = d.DeliveryDate,
            ShippingMethod = d.ShippingMethod,
            TrackingNumber = d.TrackingNumber,
            DeliveryAddress = d.DeliveryAddress,
            SignedBy = d.SignedBy,
            SignedAt = d.SignedAt,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            ItemCount = d.Items.Count
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DeliveryOrderDto>>> GetDeliveryOrder(Guid id)
    {
        var d = await _db.DeliveryOrders
            .Include(d => d.SalesOrder)
                .ThenInclude(s => s!.Customer)
            .Include(d => d.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (d == null) return NotFound(ApiHelper.Fail("Delivery order not found"));

        var dto = new DeliveryOrderDto
        {
            Id = d.Id,
            DeliveryNumber = d.DeliveryNumber,
            SalesOrderId = d.SalesOrderId,
            SalesOrderNumber = d.SalesOrder?.OrderNumber,
            CustomerName = d.SalesOrder?.Customer?.CustomerName,
            DeliveryDate = d.DeliveryDate,
            ShippingMethod = d.ShippingMethod,
            TrackingNumber = d.TrackingNumber,
            DeliveryAddress = d.DeliveryAddress,
            SignedBy = d.SignedBy,
            SignedAt = d.SignedAt,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            Items = d.Items.Select(i => new DeliveryOrderItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                LotNumber = i.LotNumber,
                DeliveredQty = i.DeliveredQty,
                SerialNumber = i.SerialNumber
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DeliveryOrderDto>>> CreateDeliveryOrder([FromBody] CreateDeliveryOrderRequest req)
    {
        var so = await _db.SalesOrders.FindAsync(req.SalesOrderId);
        if (so == null) return BadRequest(ApiHelper.Fail("Sales order not found"));

        var delivery = new DeliveryOrder
        {
            Id = Guid.NewGuid(),
            DeliveryNumber = DocumentNumberGenerator.Generate("DEL"),
            SalesOrderId = req.SalesOrderId,
            DeliveryDate = req.DeliveryDate ?? DateTime.UtcNow,
            ShippingMethod = req.ShippingMethod,
            TrackingNumber = req.TrackingNumber,
            DeliveryAddress = req.DeliveryAddress,
            Status = "pending"
        };

        if (req.Items != null)
        {
            foreach (var item in req.Items)
            {
                delivery.Items.Add(new DeliveryOrderItem
                {
                    Id = Guid.NewGuid(),
                    DeliveryOrderId = delivery.Id,
                    SkuId = item.SkuId,
                    LotNumber = item.LotNumber,
                    DeliveredQty = item.Quantity,
                    SerialNumber = item.SerialNumber
                });
            }
        }

        _db.DeliveryOrders.Add(delivery);
        await _db.SaveChangesAsync();

        var dto = new DeliveryOrderDto
        {
            Id = delivery.Id,
            DeliveryNumber = delivery.DeliveryNumber,
            SalesOrderId = delivery.SalesOrderId,
            DeliveryDate = delivery.DeliveryDate,
            Status = delivery.Status,
            CreatedAt = delivery.CreatedAt
        };

        return Ok(dto);
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdateDeliveryStatusRequest req)
    {
        var delivery = await _db.DeliveryOrders.FindAsync(id);
        if (delivery == null) return NotFound(ApiHelper.Fail("Delivery order not found"));

        delivery.Status = req.Status;
        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Status updated");
    }

    [HttpPatch("{id}/sign")]
    public async Task<ActionResult<ApiResponse>> SignDelivery(Guid id, [FromBody] SignDeliveryRequest req)
    {
        var delivery = await _db.DeliveryOrders.FindAsync(id);
        if (delivery == null) return NotFound(ApiHelper.Fail("Delivery order not found"));

        delivery.SignedBy = req.SignedBy;
        delivery.SignedAt = DateTime.UtcNow;
        delivery.Status = "completed";
        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Delivery signed");
    }
}

