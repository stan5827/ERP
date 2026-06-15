using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using ErpBackend.Helpers;

namespace ErpBackend.Controllers;

public class RfqsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public RfqsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<RfqDto>>>> GetRfqs([FromQuery] string? status, [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.Rfqs.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(r => r.RfqNumber.Contains(searchTerm));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.RfqDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(r => new RfqDto
        {
            Id = r.Id,
            RfqNumber = r.RfqNumber,
            PrId = r.PrId,
            RfqDate = r.RfqDate,
            Deadline = r.Deadline,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            ItemCount = r.Items.Count,
            ResponseCount = r.SupplierResponses.Count
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<RfqDto>>> GetRfq(Guid id)
    {
        var r = await _db.Rfqs
            .Include(r => r.Items).ThenInclude(i => i.Sku)
            .Include(r => r.SupplierResponses).ThenInclude(s => s.Supplier)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (r == null) return NotFound(ApiHelper.Fail("RFQ not found"));

        var dto = new RfqDto
        {
            Id = r.Id,
            RfqNumber = r.RfqNumber,
            PrId = r.PrId,
            RfqDate = r.RfqDate,
            Deadline = r.Deadline,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            Items = r.Items.Select(i => new RfqItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Specifications = i.Specifications
            }).ToList(),
            SupplierResponses = r.SupplierResponses.Select(s => new RfqSupplierResponseDto
            {
                Id = s.Id,
                SupplierId = s.SupplierId,
                SupplierName = s.Supplier?.SupplierName,
                UnitPrice = s.UnitPrice,
                LeadTimeDays = s.LeadTimeDays,
                Moq = s.Moq,
                TradeTerms = s.TradeTerms,
                Notes = s.Notes,
                IsSelected = s.IsSelected
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RfqDto>>> CreateRfq([FromBody] CreateRfqRequest req)
    {
        var rfq = new Rfq
        {
            Id = Guid.NewGuid(),
            RfqNumber = DocumentNumberGenerator.Generate("RFQ"),
            PrId = req.PrId,
            RfqDate = req.RfqDate ?? DateTime.UtcNow,
            Deadline = req.Deadline,
            Status = "open"
        };

        if (req.Items != null)
        {
            foreach (var item in req.Items)
            {
                rfq.Items.Add(new RfqItem
                {
                    Id = Guid.NewGuid(),
                    RfqId = rfq.Id,
                    SkuId = item.SkuId,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    Specifications = item.Specifications
                });
            }
        }

        _db.Rfqs.Add(rfq);
        await _db.SaveChangesAsync();

        var dto = new RfqDto
        {
            Id = rfq.Id,
            RfqNumber = rfq.RfqNumber,
            RfqDate = rfq.RfqDate,
            Deadline = rfq.Deadline,
            Status = rfq.Status
        };

        return Ok(dto);
    }

    [HttpPost("{id}/responses")]
    public async Task<ActionResult<ApiResponse>> AddSupplierResponse(Guid id, [FromBody] AddRfqResponseRequest req)
    {
        var response = new RfqSupplierResponse
        {
            Id = Guid.NewGuid(),
            RfqId = id,
            SupplierId = req.SupplierId,
            SkuId = req.SkuId,
            UnitPrice = req.UnitPrice,
            LeadTimeDays = req.LeadTimeDays,
            Moq = req.Moq,
            TradeTerms = req.TradeTerms,
            Notes = req.Notes
        };

        _db.RfqSupplierResponses.Add(response);
        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Response added");
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse>> UpdateStatus(Guid id, [FromBody] UpdateRfqStatusRequest req)
    {
        var rfq = await _db.Rfqs.FindAsync(id);
        if (rfq == null) return NotFound(ApiHelper.Fail("RFQ not found"));

        rfq.Status = req.Status;
        await _db.SaveChangesAsync();

        return ApiResponse.Ok("Status updated");
    }
}

