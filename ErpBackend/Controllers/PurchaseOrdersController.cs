using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;
using System.Text.Json.Serialization;

namespace ErpBackend.Controllers;

public class PurchaseOrdersController : BaseController
{
    private readonly ErpGeneralDbContext _db;

    public PurchaseOrdersController(ErpGeneralDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
    {
        if (request.SupplierId == Guid.Empty)
            return BadRequest(ApiHelper.Fail("SupplierId is required."));
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(ApiHelper.Fail("At least one item is required."));

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId);
        if (supplier == null)
            return BadRequest(ApiHelper.Fail("Supplier not found."));

        var skuIds = request.Items.Select(i => i.SkuId).Distinct().ToList();
        var skus = await _db.Skus.Where(s => skuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id);
        if (skus.Count != skuIds.Count)
            return BadRequest(ApiHelper.Fail("One or more SKUs were not found."));

        var now = DateTime.UtcNow;
        var po = new PurchaseOrder
        {
            PoNumber = $"PO-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            SupplierId = request.SupplierId,
            PoDate = request.PoDate == default ? now : request.PoDate,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            DeliveryWarehouseId = request.DeliveryWarehouseId,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "TWD" : request.Currency,
            ExchangeRate = request.ExchangeRate <= 0 ? 1 : request.ExchangeRate,
            Status = "DRAFT",
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var item in request.Items)
        {
            var sku = skus[item.SkuId];
            var qty = item.Quantity <= 0 ? 0 : item.Quantity;
            var unitPrice = item.UnitPrice < 0 ? 0 : item.UnitPrice;
            var subtotal = decimal.Round(qty * unitPrice, 2);
            po.Items.Add(new PurchaseOrderItem
            {
                SkuId = sku.Id,
                Quantity = qty,
                Unit = item.Unit ?? sku.PurchaseUnit ?? sku.Unit ?? "PCS",
                UnitPrice = unitPrice,
                Subtotal = subtotal,
                ReceivedQty = 0,
                Status = "pending",
                InventoryUnit = sku.Unit,
                ConversionRate = sku.ConversionRate <= 0 ? 1 : sku.ConversionRate,
                InventoryQty = decimal.Round(qty * (sku.ConversionRate <= 0 ? 1 : sku.ConversionRate), 2),
                InventoryUnitPrice = unitPrice,
                InventorySubtotal = subtotal
            });
        }

        po.Subtotal = po.Items.Sum(i => i.Subtotal);
        po.TaxAmount = 0;
        po.TotalAmount = po.Subtotal;
        po.BaseSubtotal = po.Subtotal;
        po.BaseTaxAmount = 0;
        po.BaseTotalAmount = po.Subtotal;

        _db.PurchaseOrders.Add(po);
        await _db.SaveChangesAsync();

        await _db.Entry(po).Reference(x => x.Supplier).LoadAsync();
        await _db.Entry(po).Collection(x => x.Items).LoadAsync();
        foreach (var item in po.Items)
        {
            await _db.Entry(item).Reference(x => x.Sku).LoadAsync();
        }

        return Ok(MapPurchaseOrderDto(po), "採購單建立成功");
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PurchaseOrderDto>>>> GetPurchaseOrders(
        [FromQuery] string? status,
        [FromQuery] string? searchTerm)
    {
        var page = GetPage();
        var pageSize = GetPageSize();

        var query = _db.PurchaseOrders
            .Include(po => po.Supplier)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(po => po.Status == status);

        if (!string.IsNullOrEmpty(searchTerm))
            query = query.Where(po => po.PoNumber.Contains(searchTerm) || po.Supplier.SupplierName.Contains(searchTerm));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(po => po.PoDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(po => new PurchaseOrderDto
        {
            Id = po.Id,
            PoNumber = po.PoNumber,
            SupplierId = po.SupplierId,
            SupplierCode = po.Supplier?.SupplierCode,
            SupplierName = po.Supplier?.SupplierName,
            PoDate = po.PoDate,
            TotalAmount = po.TotalAmount,
            Status = po.Status,
            CreatedAt = po.CreatedAt
        }).ToList();

        return Ok(dtos, totalCount: totalCount);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> GetPurchaseOrder(Guid id)
    {
        var po = await _db.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(po => po.Id == id);

        if (po == null) return NotFound(ApiHelper.Fail("Error"));

        var dto = new PurchaseOrderDto
        {
            Id = po.Id,
            PoNumber = po.PoNumber,
            SupplierId = po.SupplierId,
            SupplierName = po.Supplier?.SupplierName,
            PoDate = po.PoDate,
            TotalAmount = po.TotalAmount,
            Status = po.Status,
            CreatedAt = po.CreatedAt,
            Items = po.Items.Select(i => new PurchaseOrderItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                ReceivedQty = i.ReceivedQty
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost("{poNumber}/confirm")]
    public async Task<ActionResult<ApiResponse<PurchaseOrderDto>>> ConfirmPurchaseOrder(string poNumber)
    {
        var po = await _db.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.Items)
                .ThenInclude(x => x.Sku)
            .FirstOrDefaultAsync(x => x.PoNumber == poNumber);

        if (po == null) return NotFound(ApiHelper.Fail("Purchase order not found."));

        po.Status = "CONFIRMED";
        po.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(MapPurchaseOrderDto(po), "採購單已確認");
    }

    [HttpPost("{poNumber}/receive")]
    public async Task<ActionResult<ApiResponse<GoodsReceiptDto>>> ReceivePurchaseOrder(string poNumber, [FromBody] LegacyReceivePurchaseOrderRequest? request)
    {
        var po = await _db.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.Items)
                .ThenInclude(x => x.Sku)
            .FirstOrDefaultAsync(x => x.PoNumber == poNumber);

        if (po == null) return NotFound(ApiHelper.Fail("Purchase order not found."));

        var warehouseCode = string.IsNullOrWhiteSpace(request?.WarehouseCode) ? "MAIN" : request!.WarehouseCode!;
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseCode == warehouseCode && w.IsActive);
        if (warehouse == null) return BadRequest(ApiHelper.Fail("Warehouse not found."));

        var now = DateTime.UtcNow;
        var gr = new GoodsReceipt
        {
            GrNumber = $"GR-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            PurchaseOrderId = po.Id,
            SupplierId = po.SupplierId,
            WarehouseId = warehouse.Id,
            GrDate = now,
            Status = "received",
            IqcStatus = "pending",
            SupplierInvoiceNo = request?.SupplierInvoiceNo,
            PackingListNo = request?.PackingListNo,
            Currency = po.Currency,
            ExchangeRate = po.ExchangeRate,
        };

        foreach (var poItem in po.Items)
        {
            var receiptQty = Math.Max(0, poItem.Quantity - poItem.ReceivedQty);
            if (receiptQty <= 0) continue;

            gr.Items.Add(new GoodsReceiptItem
            {
                SkuId = poItem.SkuId,
                PoQty = poItem.Quantity,
                PoUnit = poItem.Unit,
                ReceivedQty = receiptQty,
                AcceptedQty = receiptQty,
                RejectedQty = 0,
                InventoryUnit = poItem.InventoryUnit,
                ConversionRate = poItem.ConversionRate <= 0 ? 1 : poItem.ConversionRate,
                InventoryQty = decimal.Round(receiptQty * (poItem.ConversionRate <= 0 ? 1 : poItem.ConversionRate), 2),
                Currency = po.Currency,
                ExchangeRate = po.ExchangeRate,
                ForeignCurrencyPrice = poItem.UnitPrice,
                LocalCurrencyPrice = poItem.UnitPrice * po.ExchangeRate,
                ForeignCurrencyAmount = receiptQty * poItem.UnitPrice,
                LocalCurrencyAmount = receiptQty * poItem.UnitPrice * po.ExchangeRate,
                TotalLandedCost = receiptQty * poItem.UnitPrice * po.ExchangeRate
            });

            poItem.ReceivedQty += receiptQty;
            poItem.Status = poItem.ReceivedQty >= poItem.Quantity ? "received" : "partial";

            var sku = poItem.Sku;
            if (sku != null)
            {
                var qtyBefore = sku.Qty;
                sku.Qty += receiptQty;
                _db.InventoryTransactions.Add(new InventoryTransaction
                {
                    TrxId = $"IT-{now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                    SkuId = sku.Id,
                    Type = "goods_receipt",
                    QtyChange = receiptQty,
                    QtyBefore = qtyBefore,
                    QtyAfter = sku.Qty,
                    RelatedDocType = "purchase_order",
                    RelatedDocId = po.Id,
                    WarehouseId = warehouse.Id,
                    UnitCost = poItem.UnitPrice,
                    TotalCost = receiptQty * poItem.UnitPrice,
                    Notes = $"Receive PO {po.PoNumber}"
                });
            }
        }

        po.ReceivedAmount = po.Items.Sum(i => i.ReceivedQty * i.UnitPrice);
        po.Status = po.Items.All(i => i.ReceivedQty >= i.Quantity) ? "RECEIVED" : "PARTIAL";
        po.UpdatedAt = now;
        gr.TotalLandedCost = gr.Items.Sum(i => i.TotalLandedCost);

        _db.GoodsReceipts.Add(gr);
        await _db.SaveChangesAsync();

        await _db.Entry(gr).Reference(x => x.Supplier).LoadAsync();
        await _db.Entry(gr).Reference(x => x.Warehouse).LoadAsync();
        await _db.Entry(gr).Collection(x => x.Items).LoadAsync();
        foreach (var item in gr.Items)
        {
            await _db.Entry(item).Reference(x => x.Sku).LoadAsync();
        }

        return Ok(MapGoodsReceiptDto(gr), "收貨入庫完成");
    }

    private static PurchaseOrderDto MapPurchaseOrderDto(PurchaseOrder po)
        => new()
        {
            Id = po.Id,
            PoNumber = po.PoNumber,
            SupplierId = po.SupplierId,
            SupplierCode = po.Supplier?.SupplierCode,
            SupplierName = po.Supplier?.SupplierName,
            PoDate = po.PoDate,
            TotalAmount = po.TotalAmount,
            Status = po.Status,
            CreatedAt = po.CreatedAt,
            Items = po.Items.Select(i => new PurchaseOrderItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                ReceivedQty = i.ReceivedQty
            }).ToList()
        };

    private static GoodsReceiptDto MapGoodsReceiptDto(GoodsReceipt gr)
        => new()
        {
            Id = gr.Id,
            GrNumber = gr.GrNumber,
            PurchaseOrderId = gr.PurchaseOrderId,
            AsnId = gr.AsnId,
            SupplierId = gr.SupplierId,
            SupplierName = gr.Supplier?.SupplierName,
            WarehouseId = gr.WarehouseId,
            WarehouseName = gr.Warehouse?.WarehouseName,
            GrDate = gr.GrDate,
            Status = gr.Status,
            SupplierInvoiceNo = gr.SupplierInvoiceNo,
            PackingListNo = gr.PackingListNo,
            Items = gr.Items.Select(i => new GoodsReceiptItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                ReceivedQty = i.ReceivedQty,
                AcceptedQty = i.AcceptedQty
            }).ToList()
        };
}

public class LegacyReceivePurchaseOrderRequest
{
    [JsonPropertyName("warehouse_code")]
    public string? WarehouseCode { get; set; }
    [JsonPropertyName("supplierInvoiceNo")]
    public string? SupplierInvoiceNo { get; set; }
    [JsonPropertyName("packingListNo")]
    public string? PackingListNo { get; set; }
}
