using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.MES;

namespace ErpBackend.Controllers;

[Authorize]
public class BarcodeController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public BarcodeController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpGet("ping")]
    public IActionResult Ping() => new OkObjectResult(new { Message = "Barcode service is available." });

    [HttpGet("records")]
    public async Task<IActionResult> GetRecords([FromQuery] string? type, [FromQuery] string? status)
    {
        var query = _db.BarcodeRecords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(record => record.BarcodeType == type);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(record => record.Status == status);

        var records = await query
            .OrderByDescending(record => record.CreatedAt)
            .Take(500)
            .ToListAsync();
        return new OkObjectResult(ApiResponse<List<BarcodeRecord>>.Ok(records));
    }

    [HttpGet("lookup/{barcode}")]
    public async Task<IActionResult> Lookup(string barcode)
    {
        var record = await _db.BarcodeRecords.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Barcode == barcode);
        await AddHistoryAsync(barcode, record?.BarcodeType ?? "sku", "lookup",
            record == null ? "not_found" : "success", record == null ? "Barcode not found" : null);
        return new OkObjectResult(ApiResponse<BarcodeRecord?>.Ok(record));
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanBarcodeRequestDto request)
    {
        var barcode = request.Barcode.Trim();
        if (string.IsNullOrEmpty(barcode))
            return BadRequest();

        var record = await _db.BarcodeRecords.FirstOrDefaultAsync(item => item.Barcode == barcode);
        if (record == null)
        {
            await AddHistoryAsync(barcode, request.BarcodeType ?? "sku", request.Action ?? "scan",
                "not_found", request.Details);
            return NotFound();
        }

        record.LastScanTime = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.LocationCode))
            record.LocationCode = request.LocationCode;
        if (request.Qty.HasValue)
            record.Qty = request.Qty;
        await _db.SaveChangesAsync();
        await AddHistoryAsync(barcode, record.BarcodeType, request.Action ?? "scan",
            "success", request.Details);
        return new OkObjectResult(ApiResponse<BarcodeRecord>.Ok(record));
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateBarcodeRequestDto request)
    {
        var record = await CreateRecordAsync(request);
        await AddHistoryAsync(record.Barcode, record.BarcodeType, "generate", "success",
            "Barcode generated", save: true);
        return new OkObjectResult(ApiResponse<object>.Ok(new { barcode = record.Barcode, type = record.BarcodeType }));
    }

    [HttpPost("generate/batch")]
    public async Task<IActionResult> GenerateBatch([FromBody] BatchBarcodeRequestDto request)
    {
        var quantity = Math.Clamp(request.Quantity, 1, 500);
        var barcodes = new List<string>();
        for (var index = 0; index < quantity; index++)
        {
            var record = await CreateRecordAsync(request, request.QtyPerBarcode ?? request.Qty);
            barcodes.Add(record.Barcode);
        }
        await AddHistoryAsync(string.Join(",", barcodes.Take(5)), request.BarcodeType, "generate_batch",
            "success", $"Generated {barcodes.Count} barcodes", save: true);
        return new OkObjectResult(ApiResponse<object>.Ok(new { generated = barcodes.Count, barcodes }));
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.BarcodeScanHistories.AsNoTracking();
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.Timestamp)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new OkObjectResult(ApiResponse<object>.Ok(new
        {
            data = items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        }));
    }

    private async Task<BarcodeRecord> CreateRecordAsync(GenerateBarcodeRequestDto request, decimal? qty = null)
    {
        var type = string.IsNullOrWhiteSpace(request.BarcodeType) ? "sku" : request.BarcodeType.Trim();
        var prefix = string.IsNullOrWhiteSpace(request.Prefix)
            ? GetPrefix(type)
            : request.Prefix.Trim().TrimEnd('-');
        string barcode;
        do
        {
            barcode = $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..50];
        } while (await _db.BarcodeRecords.AnyAsync(item => item.Barcode == barcode));

        var record = new BarcodeRecord
        {
            Barcode = barcode,
            BarcodeType = type,
            RefId = request.RefId,
            RefType = request.RefType,
            RefName = request.RefName,
            LocationCode = request.LocationCode,
            Qty = qty ?? request.Qty,
            Status = "active"
        };
        _db.BarcodeRecords.Add(record);
        await _db.SaveChangesAsync();
        return record;
    }

    private async Task AddHistoryAsync(string barcode, string type, string action, string result,
        string? details, bool save = true)
    {
        var userId = GetUserId();
        _db.BarcodeScanHistories.Add(new BarcodeScanHistory
        {
            Barcode = barcode[..Math.Min(barcode.Length, 50)],
            BarcodeType = type,
            Action = action,
            Result = result,
            OperatorId = userId == Guid.Empty ? null : userId,
            OperatorName = User.Identity?.Name,
            Details = details
        });
        if (save)
            await _db.SaveChangesAsync();
    }

    private static string GetPrefix(string type) => type.ToLowerInvariant() switch
    {
        "lot" => "LOT",
        "location" => "LOC",
        "box" => "BOX",
        "pallet" => "PAL",
        _ => "SKU"
    };
}
