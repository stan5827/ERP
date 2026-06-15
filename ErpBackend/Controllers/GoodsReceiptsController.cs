using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.QA;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class GoodsReceiptsController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    private readonly IInventoryReservationService _inventoryReservationService;
    public GoodsReceiptsController(ErpGeneralDbContext db, IInventoryReservationService inventoryReservationService) { _db = db; _inventoryReservationService = inventoryReservationService; }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<GoodsReceiptDto>>>> GetGoodsReceipts()
    {
        var items = await _db.GoodsReceipts
            .Include(g => g.Supplier)
            .Include(g => g.Warehouse)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Asn)
            .Include(g => g.Items)
                .ThenInclude(i => i.Sku)
            .OrderByDescending(g => g.GrDate)
            .ToListAsync();

        return Ok(items.Select(MapGoodsReceiptDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<GoodsReceiptDto>>> GetGoodsReceipt(Guid id)
    {
        var gr = await _db.GoodsReceipts
            .Include(g => g.Supplier)
            .Include(g => g.Warehouse)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Asn)
            .Include(g => g.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (gr == null) return NotFound(ApiHelper.Fail("Goods receipt not found."));
        return Ok(MapGoodsReceiptDto(gr));
    }

    [HttpPost("from-asn/{asnId:guid}")]
    public async Task<ActionResult<ApiResponse<GoodsReceiptDto>>> ReceiveFromAsn(Guid asnId, [FromBody] ReceiveGoodsReceiptRequest request)
    {
        var asn = await _db.Asns
            .Include(a => a.Supplier)
            .Include(a => a.PurchaseOrder)
                .ThenInclude(p => p!.Items)
                    .ThenInclude(i => i.Sku)
            .Include(a => a.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(a => a.Id == asnId);

        if (asn == null) return NotFound(ApiHelper.Fail("ASN not found."));
        if (asn.Status == "received") return BadRequest(ApiHelper.Fail("ASN already received."));

        var warehouseCode = string.IsNullOrWhiteSpace(request.WarehouseCode) ? "MAIN" : request.WarehouseCode.Trim();
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.WarehouseCode == warehouseCode && w.IsActive);
        if (warehouse == null) return BadRequest(ApiHelper.Fail("Warehouse not found."));

        var now = DateTime.UtcNow;
        var po = asn.PurchaseOrder;
        var poItemsBySku = po?.Items.ToDictionary(i => i.SkuId) ?? new Dictionary<Guid, PurchaseOrderItem>();

        var gr = new GoodsReceipt
        {
            GrNumber = $"GR-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            PurchaseOrderId = asn.PurchaseOrderId,
            AsnId = asn.Id,
            SupplierId = asn.SupplierId,
            WarehouseId = warehouse.Id,
            GrDate = now,
            Status = "received",
            IqcStatus = "pending",
            SupplierInvoiceNo = request.SupplierInvoiceNo,
            PackingListNo = request.PackingListNo,
            Currency = po?.Currency ?? "TWD",
            ExchangeRate = po?.ExchangeRate ?? 1,
        };

        foreach (var asnItem in asn.Items)
        {
            var poItem = poItemsBySku.GetValueOrDefault(asnItem.SkuId);
            var conversionRate = poItem?.ConversionRate is > 0 ? poItem.ConversionRate : 1;
            var unitPrice = poItem?.UnitPrice ?? asnItem.Sku?.PurchasePrice ?? 0;
            var receivedQty = asnItem.ShippedQty;
            var inventoryQty = decimal.Round(receivedQty * conversionRate, 2);

            gr.Items.Add(new GoodsReceiptItem
            {
                SkuId = asnItem.SkuId,
                PoQty = poItem?.Quantity ?? receivedQty,
                PoUnit = poItem?.Unit ?? asnItem.Sku?.PurchaseUnit ?? asnItem.Sku?.Unit,
                ReceivedQty = receivedQty,
                AcceptedQty = receivedQty,
                RejectedQty = 0,
                InventoryUnit = poItem?.InventoryUnit ?? asnItem.Sku?.Unit,
                ConversionRate = conversionRate,
                InventoryQty = inventoryQty,
                LotNumber = asnItem.LotNumber,
                ManufactureDate = asnItem.ManufactureDate,
                ExpiryDate = asnItem.ExpiryDate,
                Currency = po?.Currency ?? "TWD",
                ExchangeRate = po?.ExchangeRate ?? 1,
                ForeignCurrencyPrice = unitPrice,
                LocalCurrencyPrice = unitPrice * (po?.ExchangeRate ?? 1),
                ForeignCurrencyAmount = receivedQty * unitPrice,
                LocalCurrencyAmount = receivedQty * unitPrice * (po?.ExchangeRate ?? 1),
                TotalLandedCost = receivedQty * unitPrice * (po?.ExchangeRate ?? 1)
            });

            if (poItem != null)
            {
                poItem.ReceivedQty += receivedQty;
                poItem.Status = poItem.ReceivedQty >= poItem.Quantity ? "received" : "partial";
            }

            var sku = asnItem.Sku;
            if (sku != null)
            {
                var qtyBefore = sku.Qty;
                sku.Qty += receivedQty;
                _db.InventoryTransactions.Add(new InventoryTransaction
                {
                    TrxId = $"IT-{now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                    SkuId = sku.Id,
                    Type = "goods_receipt",
                    QtyChange = receivedQty,
                    QtyBefore = qtyBefore,
                    QtyAfter = sku.Qty,
                    LotNumber = asnItem.LotNumber,
                    RelatedDocType = "asn",
                    RelatedDocId = asn.Id,
                    WarehouseId = warehouse.Id,
                    UnitCost = unitPrice,
                    TotalCost = receivedQty * unitPrice,
                    Notes = request.Notes ?? $"Receive ASN {asn.AsnNumber}"
                });
            }
        }

        if (po != null)
        {
            po.ReceivedAmount = po.Items.Sum(i => i.ReceivedQty * i.UnitPrice);
            po.Status = po.Items.All(i => i.ReceivedQty >= i.Quantity) ? "RECEIVED" : "PARTIAL";
            po.UpdatedAt = now;
        }

        asn.Status = "received";
        gr.TotalLandedCost = gr.Items.Sum(i => i.TotalLandedCost);

        _db.GoodsReceipts.Add(gr);
        var accountsPayable = BuildAccountsPayable(gr, po, now);
        _db.AccountsPayables.Add(accountsPayable);
        await _db.SaveChangesAsync();

        await _db.Entry(gr).Reference(x => x.Supplier).LoadAsync();
        await _db.Entry(gr).Reference(x => x.Warehouse).LoadAsync();
        await _db.Entry(gr).Reference(x => x.PurchaseOrder).LoadAsync();
        await _db.Entry(gr).Reference(x => x.Asn).LoadAsync();
        await _db.Entry(gr).Collection(x => x.Items).LoadAsync();
        foreach (var item in gr.Items)
        {
            await _db.Entry(item).Reference(x => x.Sku).LoadAsync();
        }

        return Ok(MapGoodsReceiptDto(gr), "ASN 收貨完成");
    }

    [HttpPatch("{id:guid}/iqc")]
    public async Task<ActionResult<ApiResponse<GoodsReceiptDto>>> UpdateIqc(Guid id, [FromBody] UpdateGoodsReceiptIqcRequest request)
    {
        var gr = await _db.GoodsReceipts
            .Include(g => g.Supplier)
            .Include(g => g.Warehouse)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Asn)
            .Include(g => g.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (gr == null) return NotFound(ApiHelper.Fail("Goods receipt not found."));

        var normalizedResult = NormalizeIqcResult(request.Result);
        ApplyIqcResult(gr, normalizedResult, request.DefectQty ?? 0, request.SampleQty, request.LotNumber);
        await UpsertIqcReportAsync(gr, request, normalizedResult);
        await UpsertAccountsPayableAsync(gr);
        await _db.SaveChangesAsync();
        await SyncIqcHoldReservationsAsync(gr, normalizedResult);
        if (normalizedResult == "fail" && (request.DefectQty ?? 0) > 0)
        {
            await EnsureNcrAsync(gr, request);
            await _db.SaveChangesAsync();
        }

        return Ok(MapGoodsReceiptDto(gr), "IQC 更新完成");
    }

    [HttpPost("{id:guid}/allocate-costs")]
    public async Task<ActionResult<ApiResponse<GoodsReceiptDto>>> AllocateCosts(Guid id, [FromBody] AllocateGoodsReceiptCostsRequest request)
    {
        var gr = await _db.GoodsReceipts
            .Include(g => g.Supplier)
            .Include(g => g.Warehouse)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Asn)
            .Include(g => g.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (gr == null) return NotFound(ApiHelper.Fail("Goods receipt not found."));

        var totalBase = request.AllocateByAmount
            ? gr.Items.Sum(i => i.LocalCurrencyAmount)
            : gr.Items.Sum(i => i.ReceivedQty);

        foreach (var item in gr.Items)
        {
            var basis = request.AllocateByAmount ? item.LocalCurrencyAmount : item.ReceivedQty;
            var ratio = totalBase <= 0 ? 0 : basis / totalBase;
            item.TariffAmount = decimal.Round(request.TotalTariff * ratio, 2);
            item.FreightAmount = decimal.Round(request.TotalFreight * ratio, 2);
            item.TotalLandedCost = decimal.Round(item.LocalCurrencyAmount + item.TariffAmount + item.FreightAmount, 2);
        }

        gr.TotalTariff = request.TotalTariff;
        gr.TotalFreight = request.TotalFreight;
        gr.TotalLandedCost = gr.Items.Sum(i => i.TotalLandedCost);

        await UpsertAccountsPayableAsync(gr);
        await _db.SaveChangesAsync();

        return Ok(MapGoodsReceiptDto(gr), "收貨成本分攤完成");
    }

    private static GoodsReceiptDto MapGoodsReceiptDto(GoodsReceipt gr)
        => new()
        {
            Id = gr.Id,
            GrNumber = gr.GrNumber,
            PurchaseOrderId = gr.PurchaseOrderId,
            PurchaseOrderNumber = gr.PurchaseOrder?.PoNumber,
            AsnId = gr.AsnId,
            AsnNumber = gr.Asn?.AsnNumber,
            SupplierId = gr.SupplierId,
            SupplierName = gr.Supplier?.SupplierName,
            WarehouseId = gr.WarehouseId,
            WarehouseName = gr.Warehouse?.WarehouseName,
            GrDate = gr.GrDate,
            Status = gr.Status,
            IqcStatus = gr.IqcStatus,
            SupplierInvoiceNo = gr.SupplierInvoiceNo,
            PackingListNo = gr.PackingListNo,
            TotalAmount = gr.TotalLandedCost,
            Items = gr.Items.Select(i => new GoodsReceiptItemDto
            {
                Id = i.Id,
                SkuId = i.SkuId,
                SkuNumber = i.Sku?.SkuNumber,
                ReceivedQty = i.ReceivedQty,
                AcceptedQty = i.AcceptedQty,
                RejectedQty = i.RejectedQty,
                IqcResult = i.IqcResult,
                LotNumber = i.LotNumber
            }).ToList()
        };

    private static string NormalizeIqcResult(string? result)
        => string.IsNullOrWhiteSpace(result) ? "pending" : result.Trim().ToLowerInvariant();

    private static void ApplyIqcResult(GoodsReceipt gr, string result, decimal defectQty, decimal? sampleQty, string? lotNumber)
    {
        var remainingRejectQty = Math.Max(0m, defectQty);
        var totalReceived = gr.Items.Sum(i => i.ReceivedQty);

        foreach (var item in gr.Items)
        {
            decimal itemRejectedQty;
            if (remainingRejectQty <= 0)
            {
                itemRejectedQty = 0;
            }
            else if (item == gr.Items.Last())
            {
                itemRejectedQty = remainingRejectQty;
            }
            else
            {
                var ratio = totalReceived <= 0 ? 0 : item.ReceivedQty / totalReceived;
                itemRejectedQty = decimal.Round(defectQty * ratio, 2);
                if (itemRejectedQty > remainingRejectQty)
                    itemRejectedQty = remainingRejectQty;
            }

            remainingRejectQty -= itemRejectedQty;
            item.RejectedQty = itemRejectedQty;
            item.AcceptedQty = Math.Max(0, item.ReceivedQty - itemRejectedQty);
            item.IqcResult = result;

            if (!string.IsNullOrWhiteSpace(lotNumber) && string.IsNullOrWhiteSpace(item.LotNumber))
                item.LotNumber = lotNumber.Trim();
        }

        gr.IqcStatus = result;
    }

    private async Task UpsertIqcReportAsync(GoodsReceipt gr, UpdateGoodsReceiptIqcRequest request, string result)
    {
        var existing = await _db.IqcReports.FirstOrDefaultAsync(r => r.GoodsReceiptId == gr.Id);
        var sampleSize = request.SampleQty.HasValue ? (int)Math.Round(request.SampleQty.Value) : (int)Math.Round(gr.Items.Sum(i => i.ReceivedQty));
        var rejectQty = (int)Math.Round(request.DefectQty ?? 0);

        if (existing == null)
        {
            existing = new IqcReport
            {
                ReportNumber = $"IQC-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                GoodsReceiptId = gr.Id,
                SupplierId = gr.SupplierId,
                InspectionDate = DateTime.UtcNow,
                InspectorId = request.InspectorId ?? Guid.Empty,
                SampleSize = sampleSize,
                AcceptNumber = Math.Max(0, sampleSize - rejectQty),
                RejectNumber = rejectQty,
                Result = result,
                LotNumber = request.LotNumber ?? gr.Items.Select(i => i.LotNumber).FirstOrDefault()
            };
            _db.IqcReports.Add(existing);
            return;
        }

        existing.InspectionDate = DateTime.UtcNow;
        existing.InspectorId = request.InspectorId ?? existing.InspectorId;
        existing.SampleSize = sampleSize;
        existing.AcceptNumber = Math.Max(0, sampleSize - rejectQty);
        existing.RejectNumber = rejectQty;
        existing.Result = result;
        existing.LotNumber = request.LotNumber ?? existing.LotNumber ?? gr.Items.Select(i => i.LotNumber).FirstOrDefault();
    }

    private AccountsPayable BuildAccountsPayable(GoodsReceipt gr, PurchaseOrder? po, DateTime now)
    {
        var invoiceAmount = gr.Items.Sum(i => i.LocalCurrencyAmount);
        var totalAmount = gr.TotalLandedCost > 0 ? gr.TotalLandedCost : invoiceAmount;

        return new AccountsPayable
        {
            ApNumber = $"AP-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            SupplierId = gr.SupplierId,
            PurchaseOrderId = gr.PurchaseOrderId,
            GoodsReceiptId = gr.Id,
            SupplierInvoiceNumber = gr.SupplierInvoiceNo,
            SupplierInvoiceDate = now,
            Currency = po?.Currency ?? gr.Currency,
            ExchangeRate = po?.ExchangeRate ?? gr.ExchangeRate,
            TaxType = po?.TaxType ?? "taxable",
            TaxCode = po?.TaxCode,
            InvoiceAmount = invoiceAmount,
            TaxAmount = 0,
            TotalAmount = totalAmount,
            BaseInvoiceAmount = invoiceAmount,
            BaseTaxAmount = 0,
            BaseTotalAmount = totalAmount,
            DueDate = po?.DueDate ?? now.AddDays(30),
            PaymentTerms = po?.PaymentTerms,
            VerificationStatus = gr.IqcStatus == "pass" ? "verified" : "pending",
            PaymentStatus = "unpaid",
            ThreeWayMatchResult = EvaluateThreeWayMatch(po, gr, totalAmount, gr.IqcStatus),
            Notes = $"Generated from goods receipt {gr.GrNumber}",
            Items = gr.Items.Select(i => new AccountsPayableItem
            {
                SkuId = i.SkuId,
                Quantity = i.AcceptedQty > 0 ? i.AcceptedQty : i.ReceivedQty,
                UnitPrice = i.LocalCurrencyPrice,
                Amount = i.TotalLandedCost > 0 ? i.TotalLandedCost : i.LocalCurrencyAmount
            }).ToList()
        };
    }

    private async Task UpsertAccountsPayableAsync(GoodsReceipt gr)
    {
        var po = gr.PurchaseOrder ?? await _db.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == gr.PurchaseOrderId);

        var ap = await _db.AccountsPayables
            .Include(a => a.Items)
            .FirstOrDefaultAsync(a => a.GoodsReceiptId == gr.Id);

        var invoiceAmount = gr.Items.Sum(i => i.LocalCurrencyAmount);
        var totalAmount = gr.TotalLandedCost > 0 ? gr.TotalLandedCost : invoiceAmount;

        if (ap == null)
        {
            _db.AccountsPayables.Add(BuildAccountsPayable(gr, po, DateTime.UtcNow));
            return;
        }

        ap.SupplierId = gr.SupplierId;
        ap.PurchaseOrderId = gr.PurchaseOrderId;
        ap.SupplierInvoiceNumber = gr.SupplierInvoiceNo;
        ap.Currency = po?.Currency ?? gr.Currency;
        ap.ExchangeRate = po?.ExchangeRate ?? gr.ExchangeRate;
        ap.InvoiceAmount = invoiceAmount;
        ap.TotalAmount = totalAmount;
        ap.BaseInvoiceAmount = invoiceAmount;
        ap.BaseTotalAmount = totalAmount;
        ap.DueDate = po?.DueDate ?? ap.DueDate ?? DateTime.UtcNow.AddDays(30);
        ap.PaymentTerms = po?.PaymentTerms ?? ap.PaymentTerms;
        ap.VerificationStatus = gr.IqcStatus == "pass" ? "verified" : gr.IqcStatus == "fail" ? "hold" : "pending";
        ap.ThreeWayMatchResult = EvaluateThreeWayMatch(po, gr, totalAmount, gr.IqcStatus);
        ap.Notes = $"Updated from goods receipt {gr.GrNumber}";

        _db.AccountsPayableItems.RemoveRange(ap.Items);
        ap.Items = gr.Items.Select(i => new AccountsPayableItem
        {
            SkuId = i.SkuId,
            Quantity = i.AcceptedQty > 0 ? i.AcceptedQty : i.ReceivedQty,
            UnitPrice = i.LocalCurrencyPrice,
            Amount = i.TotalLandedCost > 0 ? i.TotalLandedCost : i.LocalCurrencyAmount
        }).ToList();
    }

    private async Task EnsureNcrAsync(GoodsReceipt gr, UpdateGoodsReceiptIqcRequest request)
    {
        var primaryItem = gr.Items
            .OrderByDescending(i => i.RejectedQty)
            .ThenByDescending(i => i.ReceivedQty)
            .FirstOrDefault();

        if (primaryItem == null) return;

        var defectQty = request.DefectQty ?? primaryItem.RejectedQty;
        if (defectQty <= 0) return;

        var existing = await _db.NonConformanceReports.FirstOrDefaultAsync(n =>
            n.SourceType == "goods_receipt" &&
            n.SourceId == gr.Id &&
            n.SkuId == primaryItem.SkuId);

        if (existing == null)
        {
            existing = new NonConformanceReport
            {
                NcrNumber = $"NCR-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                SourceType = "goods_receipt",
                SourceId = gr.Id,
                SkuId = primaryItem.SkuId,
                DefectType = "IQC_FAIL",
                DefectQty = defectQty,
                DefectDescription = request.Notes ?? $"Auto-created from IQC failure for {gr.GrNumber}",
                Disposition = "hold",
                CostImpact = 0
            };
            _db.NonConformanceReports.Add(existing);
        }
        else
        {
            existing.DefectQty = defectQty;
            existing.DefectType = "IQC_FAIL";
            existing.DefectDescription = request.Notes ?? existing.DefectDescription;
            if (string.IsNullOrWhiteSpace(existing.Disposition))
                existing.Disposition = "hold";
        }
    }

    private async Task SyncIqcHoldReservationsAsync(GoodsReceipt gr, string result)
    {
        await ReleaseIqcHoldsAsync(gr);

        if (!string.Equals(result, "fail", StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var item in gr.Items.Where(i => i.RejectedQty > 0))
        {
            var skuCode = item.Sku?.SkuNumber;
            if (string.IsNullOrWhiteSpace(skuCode))
                continue;

            await _inventoryReservationService.ReserveAsync(
                skuCode,
                item.RejectedQty,
                "iqc-hold",
                gr.Id);
        }
    }

    private async Task ReleaseIqcHoldsAsync(GoodsReceipt gr)
    {
        var reservations = await _db.InventoryReservations
            .Include(r => r.Sku)
            .Where(r => r.OrderType == "iqc-hold" && r.OrderId == gr.Id && r.Status == "reserved")
            .ToListAsync();

        foreach (var reservationGroup in reservations
                     .Where(r => r.Sku != null)
                     .GroupBy(r => r.Sku!.SkuNumber))
        {
            var skuCode = reservationGroup.Key;
            if (string.IsNullOrWhiteSpace(skuCode))
                continue;

            var qty = reservationGroup.Sum(r => r.Qty);
            if (qty > 0)
                await _inventoryReservationService.ReleaseAsync(skuCode, qty, "iqc-hold", gr.Id);
        }
    }

    private static string EvaluateThreeWayMatch(PurchaseOrder? po, GoodsReceipt gr, decimal apAmount, string iqcStatus)
    {
        if (!string.Equals(iqcStatus, "pass", StringComparison.OrdinalIgnoreCase))
            return "pending";

        var poAmount = po == null
            ? apAmount
            : po.Items
                .Where(i => gr.Items.Any(gi => gi.SkuId == i.SkuId))
                .Sum(i => i.UnitPrice * i.ReceivedQty * po.ExchangeRate);

        var grAmount = gr.Items.Sum(i => i.TotalLandedCost > 0 ? i.TotalLandedCost : i.LocalCurrencyAmount);

        return Math.Abs(poAmount - grAmount) <= 0.01m && Math.Abs(grAmount - apAmount) <= 0.01m
            ? "matched"
            : "variance";
    }
}

