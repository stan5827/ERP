using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace ErpBackend.Controllers;

public class SalesReportsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public SalesReportsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet("order-to-cash")]
    public async Task<ActionResult<ApiResponse>> GetOrderToCash(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        // Mock data for Order-to-Cash summary
        var data = new
        {
            TotalOrders = await _db.SalesOrders.CountAsync(),
            TotalAmount = await _db.SalesOrders.SumAsync(so => (decimal?)so.TotalAmount) ?? 0,
            PendingOrders = await _db.SalesOrders.CountAsync(so => so.Status == "pending"),
            ClosedOrders = await _db.SalesOrders.CountAsync(so => so.Status == "closed"),
            MonthlyTrend = new List<dynamic>
            {
                new { Month = "1", Orders = 45, Amount = 1200000 },
                new { Month = "2", Orders = 52, Amount = 1450000 },
                new { Month = "3", Orders = 61, Amount = 1890000 }
            }
        };

        return OkObject(data);
    }

    [HttpGet("monthly-periods")]
    public async Task<ActionResult<ApiResponse<List<MonthlyPeriodDto>>>> GetMonthlyPeriods([FromQuery] int? year)
    {
        var query = _db.MonthlyPeriods.AsQueryable();
        if (year.HasValue) query = query.Where(p => p.Year == year.Value);

        var items = await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync();
        var dtos = items.Select(p => new MonthlyPeriodDto
        {
            Id = p.Id,
            Year = p.Year,
            Month = p.Month,
            IsClosed = p.Status == "closed",
            ClosedAt = p.ClosedAt,
            ClosedBy = p.ClosedBy?.ToString(),
            Notes = p.Notes
        }).ToList();

        return Ok(dtos);
    }
}

public class MonthlyPeriodDto
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
    public string? Notes { get; set; }
}
