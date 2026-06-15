using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Sales;

namespace ErpBackend.Controllers;

public class CustomersController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public CustomersController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CustomerDto>>>> GetCustomers(
        [FromQuery] bool? isActive,
        [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.Customers.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(c => c.CustomerCode.Contains(searchTerm) || c.CustomerName.Contains(searchTerm));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.CustomerCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(c => new CustomerDto
        {
            Id = c.Id,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            IsActive = c.IsActive
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }
}
