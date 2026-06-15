using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;

namespace ErpBackend.Controllers;

public class SupplierAsnController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    public SupplierAsnController(ErpGeneralDbContext db) { _db = db; }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SupplierAsnDto>>>> GetAsns([FromQuery] string? status, [FromQuery] Guid? supplierId)
    {
        var query = _db.Asns
            .Include(a => a.Supplier)
            .Include(a => a.PurchaseOrder)
            .Include(a => a.Items)
                .ThenInclude(i => i.Sku)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status == status);
        if (supplierId.HasValue && supplierId.Value != Guid.Empty)
            query = query.Where(a => a.SupplierId == supplierId.Value);

        var items = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        return Ok(items.Select(MapAsnDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SupplierAsnDto>>> GetAsn(Guid id)
    {
        var asn = await _db.Asns
            .Include(a => a.Supplier)
            .Include(a => a.PurchaseOrder)
            .Include(a => a.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (asn == null) return NotFound(ApiHelper.Fail("ASN not found."));
        return Ok(MapAsnDto(asn));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupplierAsnDto>>> CreateAsn([FromBody] CreateSupplierAsnRequest request)
    {
        if (request.PurchaseOrderId == Guid.Empty)
            return BadRequest(ApiHelper.Fail("Purchase order is required."));
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(ApiHelper.Fail("ASN items are required."));

        var po = await _db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId);

        if (po == null) return BadRequest(ApiHelper.Fail("Purchase order not found."));
        if (po.Items.Count == 0) return BadRequest(ApiHelper.Fail("Purchase order has no items."));
        if (!string.Equals(po.Status, "CONFIRMED", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(po.Status, "PARTIAL", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiHelper.Fail("Purchase order is not ready for ASN."));
        if (request.Items.Any(i => i.ShippedQty <= 0))
            return BadRequest(ApiHelper.Fail("ASN shipped quantity must be greater than zero."));

        var shippedSkuIds = request.Items.Select(i => i.SkuId).Distinct().ToList();
        var poSkuIds = po.Items.Select(i => i.SkuId).Distinct().ToHashSet();
        if (shippedSkuIds.Any(id => !poSkuIds.Contains(id)))
            return BadRequest(ApiHelper.Fail("ASN contains SKU not in purchase order."));
        if (request.Items.Any(i =>
        {
            var poItem = po.Items.FirstOrDefault(x => x.SkuId == i.SkuId);
            var remainingQty = poItem == null ? 0 : poItem.Quantity - poItem.ReceivedQty;
            return i.ShippedQty > remainingQty;
        }))
            return BadRequest(ApiHelper.Fail("ASN shipped quantity exceeds remaining PO quantity."));

        var now = DateTime.UtcNow;
        var asn = new Asn
        {
            AsnNumber = string.IsNullOrWhiteSpace(request.AsnNumber) ? $"ASN-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}" : request.AsnNumber.Trim(),
            PurchaseOrderId = po.Id,
            SupplierId = po.SupplierId,
            ShipDate = now,
            ExpectedArrival = request.ExpectedArrivalDate,
            Carrier = request.CarrierName,
            TrackingNumber = request.TrackingNumber,
            Status = "pending",
            CreatedAt = now,
            Items = request.Items.Select(i => new AsnItem
            {
                SkuId = i.SkuId,
                ShippedQty = i.ShippedQty,
                LotNumber = i.LotNumber,
                ManufactureDate = i.ManufactureDate,
                ExpiryDate = i.ExpiryDate
            }).ToList()
        };

        _db.Asns.Add(asn);
        await _db.SaveChangesAsync();

        await _db.Entry(asn).Reference(x => x.Supplier).LoadAsync();
        await _db.Entry(asn).Reference(x => x.PurchaseOrder).LoadAsync();
        await _db.Entry(asn).Collection(x => x.Items).LoadAsync();
        foreach (var item in asn.Items)
        {
            await _db.Entry(item).Reference(x => x.Sku).LoadAsync();
        }

        return Ok(MapAsnDto(asn), "ASN 建立成功");
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<SupplierAsnDto>>> UpdateStatus(Guid id, [FromBody] UpdateAsnStatusRequest request)
    {
        var asn = await _db.Asns
            .Include(a => a.Supplier)
            .Include(a => a.PurchaseOrder)
            .Include(a => a.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (asn == null) return NotFound(ApiHelper.Fail("ASN not found."));

        asn.Status = request.Status;
        await _db.SaveChangesAsync();
        return Ok(MapAsnDto(asn), "ASN 狀態已更新");
    }

    private static SupplierAsnDto MapAsnDto(Asn asn)
        => new()
        {
            AsnId = asn.Id,
            AsnNumber = asn.AsnNumber,
            SupplierId = asn.SupplierId,
            SupplierName = asn.Supplier?.SupplierName,
            PurchaseOrderId = asn.PurchaseOrderId ?? Guid.Empty,
            PurchaseOrderNumber = asn.PurchaseOrder?.PoNumber,
            ShipDate = asn.ShipDate,
            ExpectedArrivalDate = asn.ExpectedArrival ?? asn.ShipDate,
            CarrierName = asn.Carrier,
            TrackingNumber = asn.TrackingNumber,
            Status = asn.Status,
            CreatedAt = asn.CreatedAt,
            ItemCount = asn.Items.Count,
            TotalShippedQty = asn.Items.Sum(i => i.ShippedQty),
            Items = asn.Items.Select(i => new SupplierAsnItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                SkuName = i.Sku?.SkuName,
                ShippedQty = i.ShippedQty,
                LotNumber = i.LotNumber,
                ManufactureDate = i.ManufactureDate,
                ExpiryDate = i.ExpiryDate
            }).ToList()
        };
}

public class UpdateAsnStatusRequest
{
    public string Status { get; set; } = "pending";
}

