using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.QA;

namespace ErpBackend.Controllers;

[Route("api/erp/quality/[controller]")]
public class IqcReportsController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public IqcReportsController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<IqcReportDto>>>> GetReports([FromQuery] string? result)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.IqcReports.AsQueryable();

        if (!string.IsNullOrEmpty(result))
            query = query.Where(r => r.Result == result);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.InspectionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(r => new IqcReportDto
        {
            Id = r.Id,
            ReportNumber = r.ReportNumber,
            // SkuId = r.SkuId,
            // SkuName = r.Sku?.SkuName,
            BatchNumber = r.LotNumber,
            SampleQty = r.SampleSize,
            DefectQty = r.RejectNumber,
            Result = r.Result,
            ReportDate = r.InspectionDate
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }
}



