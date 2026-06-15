using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.QA;
using ErpBackend.Services;

namespace ErpBackend.Controllers;

public class QualityController : BaseController
{
    private readonly ErpGeneralDbContext _db;
    private readonly IInventoryReservationService _inventoryReservationService;

    public QualityController(ErpGeneralDbContext db, IInventoryReservationService inventoryReservationService)
    {
        _db = db;
        _inventoryReservationService = inventoryReservationService;
    }

    [HttpGet("iqc-reports")]
    public async Task<ActionResult<ApiResponse<List<IqcReportDto>>>> GetIqcReports([FromQuery] string? result)
    {
        var query = _db.IqcReports
            .Include(r => r.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(result))
            query = query.Where(r => r.Result == result);

        var reports = await query
            .OrderByDescending(r => r.InspectionDate)
            .ToListAsync();

        var dtos = reports.Select(r => new IqcReportDto
        {
            Id = r.Id,
            ReportNumber = r.ReportNumber,
            BatchNumber = r.LotNumber,
            SampleQty = r.SampleSize,
            DefectQty = r.RejectNumber,
            ReportDate = r.InspectionDate,
            Result = r.Result,
            SupplierName = r.Supplier?.SupplierName
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("iqc-pending")]
    public async Task<ActionResult<ApiResponse<List<GoodsReceiptDto>>>> GetPendingIqcReceipts()
    {
        var receipts = await _db.GoodsReceipts
            .Include(g => g.Supplier)
            .Include(g => g.Warehouse)
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Asn)
            .Include(g => g.Items)
                .ThenInclude(i => i.Sku)
            .Where(g => g.IqcStatus == "pending")
            .OrderByDescending(g => g.GrDate)
            .ToListAsync();

        return Ok(receipts.Select(MapGoodsReceiptDto).ToList());
    }

    [HttpGet("ncrs")]
    public async Task<ActionResult<ApiResponse<List<NonConformanceReportDto>>>> GetNcrs([FromQuery] string? disposition)
    {
        var query = _db.NonConformanceReports.AsQueryable();
        if (!string.IsNullOrWhiteSpace(disposition))
            query = query.Where(n => n.Disposition == disposition);

        var ncrs = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var sourceIds = ncrs.Where(n => n.SourceId.HasValue).Select(n => n.SourceId!.Value).Distinct().ToList();
        var receipts = await _db.GoodsReceipts
            .Include(g => g.Supplier)
            .Include(g => g.Items)
                .ThenInclude(i => i.Sku)
            .Where(g => sourceIds.Contains(g.Id))
            .ToListAsync();

        var receiptMap = receipts.ToDictionary(g => g.Id, g => g);

        var logs = await LoadReviewLogsAsync("ncr", ncrs.Select(n => n.Id).ToList());

        var dtos = ncrs.Select(n =>
        {
            receiptMap.TryGetValue(n.SourceId ?? Guid.Empty, out var receipt);
            var primaryItem = receipt?.Items
                .OrderByDescending(i => i.RejectedQty)
                .ThenByDescending(i => i.ReceivedQty)
                .FirstOrDefault();

            return new NonConformanceReportDto
            {
                Id = n.Id,
                NcrNumber = n.NcrNumber,
                SourceType = n.SourceType,
                SourceId = n.SourceId,
                SourceNumber = receipt?.GrNumber,
                SupplierName = receipt?.Supplier?.SupplierName,
                SkuName = primaryItem?.Sku?.SkuName,
                DefectType = n.DefectType,
                DefectQty = n.DefectQty,
                DefectDescription = n.DefectDescription,
                Disposition = n.Disposition,
                CostImpact = n.CostImpact,
                ResponsiblePerson = n.ResponsiblePerson,
                AttachmentUrl = n.AttachmentUrl,
                ClosureReason = n.ClosureReason,
                ClosedAt = n.ClosedAt,
                CreatedAt = n.CreatedAt
                ,
                Timeline = logs.TryGetValue(n.Id, out var timeline) ? timeline : new List<QaReviewLogDto>()
            };
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("iqc-reports")]
    public async Task<ActionResult<ApiResponse<IqcReportDto>>> CreateIqcReport([FromBody] CreateIqcReportRequest request)
    {
        var receipt = await _db.GoodsReceipts
            .Include(g => g.Supplier)
            .Include(g => g.PurchaseOrder)
                .ThenInclude(p => p!.Items)
            .Include(g => g.Asn)
            .Include(g => g.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(g => g.Id == request.GoodsReceiptId);

        if (receipt == null) return NotFound(ApiHelper.Fail("Goods receipt not found."));

        var result = NormalizeResult(request.Result);
        ApplyIqcResult(receipt, result, request.DefectQty, request.LotNumber);

        var report = await _db.IqcReports.FirstOrDefaultAsync(r => r.GoodsReceiptId == receipt.Id);
        if (report == null)
        {
            report = new IqcReport
            {
                ReportNumber = $"IQC-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                GoodsReceiptId = receipt.Id,
                SupplierId = receipt.SupplierId,
                InspectionDate = DateTime.UtcNow,
                InspectorId = request.InspectorId ?? Guid.Empty,
                SampleSize = (int)Math.Round(request.SampleQty),
                AcceptNumber = Math.Max(0, (int)Math.Round(request.SampleQty - request.DefectQty)),
                RejectNumber = (int)Math.Round(request.DefectQty),
                Result = result,
                LotNumber = request.LotNumber ?? receipt.Items.Select(i => i.LotNumber).FirstOrDefault()
            };
            _db.IqcReports.Add(report);
        }
        else
        {
            report.InspectionDate = DateTime.UtcNow;
            report.InspectorId = request.InspectorId ?? report.InspectorId;
            report.SampleSize = (int)Math.Round(request.SampleQty);
            report.AcceptNumber = Math.Max(0, (int)Math.Round(request.SampleQty - request.DefectQty));
            report.RejectNumber = (int)Math.Round(request.DefectQty);
            report.Result = result;
            report.LotNumber = request.LotNumber ?? report.LotNumber ?? receipt.Items.Select(i => i.LotNumber).FirstOrDefault();
        }

        if (result == "fail" && request.DefectQty > 0)
        {
            await EnsureComplaintAsync(receipt, request);
            await EnsureNcrAsync(receipt, request);
        }

        await UpsertAccountsPayableAsync(receipt);
        await _db.SaveChangesAsync();
        await SyncIqcHoldReservationsAsync(receipt, result);
        await _db.SaveChangesAsync();

        var dto = new IqcReportDto
        {
            Id = report.Id,
            ReportNumber = report.ReportNumber,
            BatchNumber = report.LotNumber,
            SampleQty = report.SampleSize,
            DefectQty = report.RejectNumber,
            ReportDate = report.InspectionDate,
            Result = report.Result,
            SupplierName = receipt.Supplier?.SupplierName
        };

        return Ok(dto, "IQC 報告已建立");
    }

    [HttpPost("ncrs")]
    public async Task<ActionResult<ApiResponse<NonConformanceReportDto>>> CreateNcr([FromBody] CreateNonConformanceReportRequest request)
    {
        if (request.GoodsReceiptId == Guid.Empty)
            return BadRequest(ApiHelper.Fail("Goods receipt is required."));
        if (request.DefectQty <= 0)
            return BadRequest(ApiHelper.Fail("Defect quantity must be greater than zero."));

        var receipt = await _db.GoodsReceipts
            .Include(g => g.Supplier)
            .Include(g => g.Items)
                .ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(g => g.Id == request.GoodsReceiptId);

        if (receipt == null)
            return NotFound(ApiHelper.Fail("Goods receipt not found."));

        var primaryItem = request.SkuId.HasValue
            ? receipt.Items.FirstOrDefault(i => i.SkuId == request.SkuId.Value)
            : receipt.Items.OrderByDescending(i => i.RejectedQty).ThenByDescending(i => i.ReceivedQty).FirstOrDefault();

        if (primaryItem == null)
            return BadRequest(ApiHelper.Fail("Unable to determine NCR item."));

        var ncr = new NonConformanceReport
        {
            NcrNumber = $"NCR-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            SourceType = "goods_receipt",
            SourceId = receipt.Id,
            SkuId = primaryItem.SkuId,
            DefectType = request.DefectType ?? "IQC_FAIL",
            DefectQty = request.DefectQty,
            DefectDescription = request.DefectDescription ?? $"Auto-created from IQC failure for {receipt.GrNumber}",
            Disposition = string.IsNullOrWhiteSpace(request.Disposition) ? "hold" : request.Disposition.Trim().ToLowerInvariant(),
            CostImpact = request.CostImpact
        };

        _db.NonConformanceReports.Add(ncr);
        await LogReviewAsync("ncr", ncr.Id, "created", "system", ncr.ResponsiblePerson, request.DefectDescription, ncr.AttachmentUrl);
        await _db.SaveChangesAsync();

        var ncrCreatedTimeline = await LoadReviewLogsAsync("ncr", new List<Guid> { ncr.Id });
        return Ok(MapNcrDto(ncr, receipt, ncrCreatedTimeline.TryGetValue(ncr.Id, out var ncrTimeline) ? ncrTimeline : new List<QaReviewLogDto>()), "NCR 已建立");
    }

    [HttpGet("complaints")]
    public async Task<ActionResult<ApiResponse<List<SupplierComplaintDto>>>> GetComplaints()
    {
        var complaints = await _db.SupplierComplaints
            .Include(c => c.Supplier)
            .Include(c => c.Capa)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var skuIds = complaints.Where(c => c.SkuId.HasValue).Select(c => c.SkuId!.Value).Distinct().ToList();
        var skuMap = await _db.Skus
            .Where(s => skuIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.SkuName);

        var logs = await LoadReviewLogsAsync("complaint", complaints.Select(c => c.Id).ToList());
        var capaIds = complaints.Where(c => c.Capa != null).Select(c => c.Capa!.Id).ToList();
        var capaLogs = await LoadReviewLogsAsync("capa", capaIds);
        var dtos = complaints.Select(c => new SupplierComplaintDto
        {
            Id = c.Id,
            ComplaintNumber = c.ComplaintNumber,
            SupplierName = c.Supplier?.SupplierName,
            SkuName = c.SkuId.HasValue && skuMap.TryGetValue(c.SkuId.Value, out var skuName) ? skuName : null,
            DefectType = c.DefectType,
            Ppm = c.Ppm,
            Priority = c.Priority,
            Status = c.Status,
            ResponsiblePerson = c.ResponsiblePerson,
            AttachmentUrl = c.AttachmentUrl,
            ClosureReason = c.ClosureReason,
            ClosedAt = c.ClosedAt,
            CreatedAt = c.CreatedAt,
            CapaNumber = c.Capa?.CapaNumber,
            CapaStatus = c.Capa?.Status,
            CapaResponsiblePerson = c.Capa?.ResponsiblePerson,
            CapaAttachmentUrl = c.Capa?.AttachmentUrl,
            CapaClosureReason = c.Capa?.ClosureReason,
            CapaClosedAt = c.Capa?.ClosedAt,
            Timeline = logs.TryGetValue(c.Id, out var timeline) ? timeline : new List<QaReviewLogDto>(),
            CapaTimeline = c.Capa != null && capaLogs.TryGetValue(c.Capa.Id, out var capaTimeline) ? capaTimeline : new List<QaReviewLogDto>()
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost("complaints")]
    public async Task<ActionResult<ApiResponse<SupplierComplaintDto>>> CreateComplaint([FromBody] CreateSupplierComplaintRequest request)
    {
        if (request.SupplierId == Guid.Empty)
            return BadRequest(ApiHelper.Fail("Supplier is required."));
        if (request.DefectQty <= 0)
            return BadRequest(ApiHelper.Fail("Defect quantity must be greater than zero."));

        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId);
        if (supplier == null) return BadRequest(ApiHelper.Fail("Supplier not found."));

        string? skuName = null;
        if (request.SkuId.HasValue)
        {
            var sku = await _db.Skus.FirstOrDefaultAsync(s => s.Id == request.SkuId.Value);
            skuName = sku?.SkuName;
        }

        var complaint = new SupplierComplaint
        {
            ComplaintNumber = $"SC-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            SupplierId = request.SupplierId,
            PurchaseOrderId = request.PurchaseOrderId,
            SkuId = request.SkuId,
            LotNumber = request.LotNumber,
            DefectQty = request.DefectQty,
            Ppm = request.DefectQty * 1000,
            DefectType = request.DefectType,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "medium" : request.Priority,
            Description = request.Description,
            Status = "open"
        };

        _db.SupplierComplaints.Add(complaint);
        await LogReviewAsync("complaint", complaint.Id, "created", "system", null, request.Description, null);
        await _db.SaveChangesAsync();

        return Ok(new SupplierComplaintDto
        {
            Id = complaint.Id,
            ComplaintNumber = complaint.ComplaintNumber,
            SupplierName = supplier.SupplierName,
            SkuName = skuName,
            DefectType = complaint.DefectType,
            Ppm = complaint.Ppm,
            Priority = complaint.Priority,
            Status = complaint.Status,
            ResponsiblePerson = complaint.ResponsiblePerson,
            AttachmentUrl = complaint.AttachmentUrl,
            ClosureReason = complaint.ClosureReason,
            ClosedAt = complaint.ClosedAt,
            CreatedAt = complaint.CreatedAt,
            CapaNumber = complaint.Capa?.CapaNumber,
            CapaStatus = complaint.Capa?.Status,
            CapaResponsiblePerson = complaint.Capa?.ResponsiblePerson,
            CapaAttachmentUrl = complaint.Capa?.AttachmentUrl,
            CapaClosureReason = complaint.Capa?.ClosureReason,
            CapaClosedAt = complaint.Capa?.ClosedAt
        }, "供應商客訴已建立");
    }

    [HttpPatch("complaints/{complaintId:guid}/ownership")]
    public async Task<ActionResult<ApiResponse<SupplierComplaintDto>>> UpdateComplaintOwnership(Guid complaintId, [FromBody] UpdateQualityOwnershipRequest request)
    {
        var complaint = await _db.SupplierComplaints.Include(c => c.Supplier).Include(c => c.Capa).FirstOrDefaultAsync(c => c.Id == complaintId);
        if (complaint == null) return NotFound(ApiHelper.Fail("Complaint not found."));

        complaint.ResponsiblePerson = request.ResponsiblePerson ?? complaint.ResponsiblePerson;
        complaint.AttachmentUrl = request.AttachmentUrl ?? complaint.AttachmentUrl;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            complaint.ClosureReason = string.IsNullOrWhiteSpace(complaint.ClosureReason) ? request.Notes : $"{complaint.ClosureReason} | {request.Notes}";

        await LogReviewAsync("complaint", complaint.Id, "ownership-updated", "system", complaint.ResponsiblePerson, request.Notes, complaint.AttachmentUrl);
        await _db.SaveChangesAsync();

        var complaintTimeline = await LoadReviewLogsAsync("complaint", new List<Guid> { complaint.Id });
        return Ok(MapComplaintDto(complaint, null, complaintTimeline.TryGetValue(complaint.Id, out var logs) ? logs : new List<QaReviewLogDto>()), "客訴資訊已更新");
    }

    [HttpPatch("ncrs/{ncrId:guid}/close")]
    public async Task<ActionResult<ApiResponse<NonConformanceReportDto>>> CloseNcr(Guid ncrId, [FromBody] CloseNonConformanceReportRequest request)
    {
        var ncr = await _db.NonConformanceReports.FirstOrDefaultAsync(n => n.Id == ncrId);
        if (ncr == null) return NotFound(ApiHelper.Fail("NCR not found."));

        var disposition = NormalizeDisposition(request.Disposition);
        if (string.IsNullOrWhiteSpace(disposition))
            disposition = "released";

        GoodsReceipt? receipt = null;
        if (string.Equals(ncr.SourceType, "goods_receipt", StringComparison.OrdinalIgnoreCase) && ncr.SourceId.HasValue)
        {
            receipt = await _db.GoodsReceipts
                .Include(g => g.Supplier)
                .Include(g => g.Items)
                    .ThenInclude(i => i.Sku)
                .FirstOrDefaultAsync(g => g.Id == ncr.SourceId.Value);
        }

        if (receipt != null && request.ReleaseStock)
        {
            if (string.Equals(disposition, "scrapped", StringComparison.OrdinalIgnoreCase))
            {
                await ScrapRejectedStockAsync(receipt, ncr, request.Notes);
            }
            else
            {
                await ReleaseIqcHoldsAsync(receipt);
            }
        }

        ncr.Disposition = disposition;
        ncr.ClosedAt = DateTime.UtcNow;
        ncr.ClosureReason = request.Notes ?? ncr.ClosureReason;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            ncr.DefectDescription = string.IsNullOrWhiteSpace(ncr.DefectDescription) ? request.Notes : $"{ncr.DefectDescription} | {request.Notes}";

        await LogReviewAsync("ncr", ncr.Id, "closed", "system", ncr.ResponsiblePerson, request.Notes, ncr.AttachmentUrl);
        await _db.SaveChangesAsync();

        var ncrTimeline = await LoadReviewLogsAsync("ncr", new List<Guid> { ncr.Id });
        return Ok(MapNcrDto(ncr, receipt, ncrTimeline.TryGetValue(ncr.Id, out var logs) ? logs : new List<QaReviewLogDto>()), "NCR 已結案");
    }

    [HttpPatch("ncrs/{ncrId:guid}/ownership")]
    public async Task<ActionResult<ApiResponse<NonConformanceReportDto>>> UpdateNcrOwnership(Guid ncrId, [FromBody] UpdateQualityOwnershipRequest request)
    {
        var ncr = await _db.NonConformanceReports.FirstOrDefaultAsync(n => n.Id == ncrId);
        if (ncr == null) return NotFound(ApiHelper.Fail("NCR not found."));

        ncr.ResponsiblePerson = request.ResponsiblePerson ?? ncr.ResponsiblePerson;
        ncr.AttachmentUrl = request.AttachmentUrl ?? ncr.AttachmentUrl;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            ncr.ClosureReason = string.IsNullOrWhiteSpace(ncr.ClosureReason) ? request.Notes : $"{ncr.ClosureReason} | {request.Notes}";

        await LogReviewAsync("ncr", ncr.Id, "ownership-updated", "system", ncr.ResponsiblePerson, request.Notes, ncr.AttachmentUrl);
        await _db.SaveChangesAsync();

        var receipt = ncr.SourceId.HasValue
            ? await _db.GoodsReceipts.Include(g => g.Supplier).Include(g => g.Items).ThenInclude(i => i.Sku).FirstOrDefaultAsync(g => g.Id == ncr.SourceId.Value)
            : null;
        var ncrTimeline = await LoadReviewLogsAsync("ncr", new List<Guid> { ncr.Id });
        return Ok(MapNcrDto(ncr, receipt, ncrTimeline.TryGetValue(ncr.Id, out var logs) ? logs : new List<QaReviewLogDto>()), "NCR 資訊已更新");
    }

    [HttpPost("complaints/{complaintId:guid}/capa")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object>>>> CreateCapa(Guid complaintId, [FromBody] CreateSupplierCapaRequest request)
    {
        var complaint = await _db.SupplierComplaints.FirstOrDefaultAsync(c => c.Id == complaintId);
        if (complaint == null) return NotFound(ApiHelper.Fail("Complaint not found."));

        var existing = await _db.SupplierCapas.FirstOrDefaultAsync(c => c.ComplaintId == complaintId);
        if (existing == null)
        {
            existing = new SupplierCapa
            {
                CapaNumber = $"CAPA-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                ComplaintId = complaintId
            };
            _db.SupplierCapas.Add(existing);
        }

        existing.D1_Team = request.D1Team;
        existing.D2_ProblemDescription = request.ProblemDescription;
        existing.D3_InterimContainment = request.InterimContainment;
        existing.D4_RootCause = request.RootCause;
        existing.D5_PermanentAction = request.PermanentAction;
        existing.D6_Verification = request.Verification;
        existing.D7_Prevention = request.Prevention;
        existing.D8_Closure = request.Closure;
        existing.SupplierResponseDue = request.SupplierResponseDue;
        existing.Status = "open";
        complaint.Status = "investigating";
        complaint.ResponsiblePerson = existing.D1_Team ?? complaint.ResponsiblePerson;
        complaint.ClosureReason = request.Closure ?? complaint.ClosureReason;

        await LogReviewAsync("capa", existing.Id, "capa-opened", "system", complaint.ResponsiblePerson, request.ProblemDescription, existing.AttachmentUrl);
        await _db.SaveChangesAsync();
        return Ok(new Dictionary<string, object>
        {
            ["id"] = existing.Id,
            ["capaNumber"] = existing.CapaNumber
        }, "CAPA 已建立");
    }

    [HttpPatch("complaints/{complaintId:guid}/capa/ownership")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, object>>>> UpdateCapaOwnership(Guid complaintId, [FromBody] UpdateQualityOwnershipRequest request)
    {
        var complaint = await _db.SupplierComplaints.Include(c => c.Capa).FirstOrDefaultAsync(c => c.Id == complaintId);
        if (complaint == null) return NotFound(ApiHelper.Fail("Complaint not found."));
        if (complaint.Capa == null) return BadRequest(ApiHelper.Fail("CAPA not found."));

        complaint.Capa.ResponsiblePerson = request.ResponsiblePerson ?? complaint.Capa.ResponsiblePerson;
        complaint.Capa.AttachmentUrl = request.AttachmentUrl ?? complaint.Capa.AttachmentUrl;
        if (!string.IsNullOrWhiteSpace(request.Notes))
            complaint.Capa.ClosureReason = string.IsNullOrWhiteSpace(complaint.Capa.ClosureReason) ? request.Notes : $"{complaint.Capa.ClosureReason} | {request.Notes}";

        await LogReviewAsync("capa", complaint.Capa.Id, "ownership-updated", "system", complaint.Capa.ResponsiblePerson, request.Notes, complaint.Capa.AttachmentUrl);
        await _db.SaveChangesAsync();

        return Ok(new Dictionary<string, object>
        {
            ["id"] = complaint.Capa.Id,
            ["capaNumber"] = complaint.Capa.CapaNumber
        }, "CAPA 資訊已更新");
    }

    [HttpPatch("complaints/{complaintId:guid}/capa/close")]
    public async Task<ActionResult<ApiResponse<SupplierComplaintDto>>> CloseCapa(Guid complaintId, [FromBody] CloseSupplierCapaRequest request)
    {
        var complaint = await _db.SupplierComplaints
            .Include(c => c.Supplier)
            .Include(c => c.Capa)
            .FirstOrDefaultAsync(c => c.Id == complaintId);

        if (complaint == null) return NotFound(ApiHelper.Fail("Complaint not found."));
        if (complaint.Capa == null) return BadRequest(ApiHelper.Fail("CAPA not found."));

        complaint.Capa.Status = "closed";
        complaint.Capa.ClosedAt = DateTime.UtcNow;
        complaint.Capa.ClosureReason = request.Closure ?? complaint.Capa.ClosureReason;
        if (!string.IsNullOrWhiteSpace(request.Closure))
            complaint.Capa.D8_Closure = request.Closure;
        if (!string.IsNullOrWhiteSpace(request.Verification))
            complaint.Capa.D6_Verification = request.Verification;

        complaint.Status = "closed";
        complaint.ClosedAt = DateTime.UtcNow;
        complaint.ClosureReason = request.Closure ?? complaint.ClosureReason;

        await LogReviewAsync("capa", complaint.Capa.Id, "closed", "system", complaint.Capa.ResponsiblePerson, request.Closure ?? request.Verification, complaint.Capa.AttachmentUrl);
        await _db.SaveChangesAsync();

        var complaintTimeline = await LoadReviewLogsAsync("complaint", new List<Guid> { complaint.Id });
        return Ok(MapComplaintDto(complaint, null, complaintTimeline.TryGetValue(complaint.Id, out var logs) ? logs : new List<QaReviewLogDto>()), "CAPA 已結案");
    }

    private static string NormalizeResult(string? result)
        => string.IsNullOrWhiteSpace(result) ? "pending" : result.Trim().ToLowerInvariant();

    private static void ApplyIqcResult(GoodsReceipt receipt, string result, decimal defectQty, string? lotNumber)
    {
        var totalReceived = receipt.Items.Sum(i => i.ReceivedQty);
        var remainingRejected = Math.Max(0m, defectQty);

        foreach (var item in receipt.Items)
        {
            decimal rejectedQty;
            if (remainingRejected <= 0)
            {
                rejectedQty = 0;
            }
            else if (item == receipt.Items.Last())
            {
                rejectedQty = remainingRejected;
            }
            else
            {
                var ratio = totalReceived <= 0 ? 0 : item.ReceivedQty / totalReceived;
                rejectedQty = decimal.Round(defectQty * ratio, 2);
                if (rejectedQty > remainingRejected)
                    rejectedQty = remainingRejected;
            }

            remainingRejected -= rejectedQty;
            item.RejectedQty = rejectedQty;
            item.AcceptedQty = Math.Max(0, item.ReceivedQty - rejectedQty);
            item.IqcResult = result;

            if (!string.IsNullOrWhiteSpace(lotNumber) && string.IsNullOrWhiteSpace(item.LotNumber))
                item.LotNumber = lotNumber.Trim();
        }

        receipt.IqcStatus = result;
    }

    private async Task EnsureComplaintAsync(GoodsReceipt receipt, CreateIqcReportRequest request)
    {
        var lotNumber = request.LotNumber ?? receipt.Items.Select(i => i.LotNumber).FirstOrDefault();
        var existing = await _db.SupplierComplaints.FirstOrDefaultAsync(c =>
            c.PurchaseOrderId == receipt.PurchaseOrderId &&
            c.SupplierId == receipt.SupplierId &&
            c.LotNumber == lotNumber &&
            c.Status != "closed");

        if (existing != null) return;

        var primaryItem = receipt.Items
            .OrderByDescending(i => i.RejectedQty)
            .ThenByDescending(i => i.ReceivedQty)
            .FirstOrDefault();

        var totalReceived = receipt.Items.Sum(i => i.ReceivedQty);
        var ppm = totalReceived <= 0 ? 0 : decimal.Round(request.DefectQty / totalReceived * 1000000m, 2);

        _db.SupplierComplaints.Add(new SupplierComplaint
        {
            ComplaintNumber = $"SC-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            SupplierId = receipt.SupplierId,
            PurchaseOrderId = receipt.PurchaseOrderId,
            SkuId = primaryItem?.SkuId,
            LotNumber = lotNumber,
            DefectQty = request.DefectQty,
            Ppm = ppm,
            DefectType = "IQC_FAIL",
            Priority = request.DefectQty > 0 ? "high" : "medium",
            Description = request.Notes ?? $"Auto-created from IQC failure for {receipt.GrNumber}",
            Status = "open"
        });
    }

    private async Task UpsertAccountsPayableAsync(GoodsReceipt receipt)
    {
        var po = receipt.PurchaseOrder ?? await _db.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == receipt.PurchaseOrderId);

        var ap = await _db.AccountsPayables
            .Include(a => a.Items)
            .FirstOrDefaultAsync(a => a.GoodsReceiptId == receipt.Id);

        if (ap == null) return;

        var invoiceAmount = receipt.Items.Sum(i => i.LocalCurrencyAmount);
        var totalAmount = receipt.TotalLandedCost > 0 ? receipt.TotalLandedCost : invoiceAmount;

        ap.InvoiceAmount = invoiceAmount;
        ap.TotalAmount = totalAmount;
        ap.BaseInvoiceAmount = invoiceAmount;
        ap.BaseTotalAmount = totalAmount;
        ap.VerificationStatus = receipt.IqcStatus == "pass" ? "verified" : receipt.IqcStatus == "fail" ? "hold" : "pending";
        ap.ThreeWayMatchResult = EvaluateThreeWayMatch(po, receipt, totalAmount);
        ap.Notes = $"Updated from IQC for {receipt.GrNumber}";

        _db.AccountsPayableItems.RemoveRange(ap.Items);
        ap.Items = receipt.Items.Select(i => new AccountsPayableItem
        {
            SkuId = i.SkuId,
            Quantity = i.AcceptedQty > 0 ? i.AcceptedQty : i.ReceivedQty,
            UnitPrice = i.LocalCurrencyPrice,
            Amount = i.TotalLandedCost > 0 ? i.TotalLandedCost : i.LocalCurrencyAmount
        }).ToList();
    }

    private static string EvaluateThreeWayMatch(PurchaseOrder? po, GoodsReceipt receipt, decimal apAmount)
    {
        if (!string.Equals(receipt.IqcStatus, "pass", StringComparison.OrdinalIgnoreCase))
            return "pending";

        var poAmount = po == null
            ? apAmount
            : po.Items
                .Where(i => receipt.Items.Any(gi => gi.SkuId == i.SkuId))
                .Sum(i => i.UnitPrice * i.ReceivedQty * po.ExchangeRate);

        var grAmount = receipt.Items.Sum(i => i.TotalLandedCost > 0 ? i.TotalLandedCost : i.LocalCurrencyAmount);

        return Math.Abs(poAmount - grAmount) <= 0.01m && Math.Abs(grAmount - apAmount) <= 0.01m
            ? "matched"
            : "variance";
    }

    private async Task EnsureNcrAsync(GoodsReceipt receipt, CreateIqcReportRequest request)
    {
        var primaryItem = receipt.Items
            .OrderByDescending(i => i.RejectedQty)
            .ThenByDescending(i => i.ReceivedQty)
            .FirstOrDefault();

        if (primaryItem == null) return;

        var existing = await _db.NonConformanceReports.FirstOrDefaultAsync(n =>
            n.SourceType == "goods_receipt" &&
            n.SourceId == receipt.Id &&
            n.SkuId == primaryItem.SkuId);

        var defectQty = request.DefectQty > 0 ? request.DefectQty : primaryItem.RejectedQty;
        if (defectQty <= 0) return;

        if (existing == null)
        {
            existing = new NonConformanceReport
            {
                NcrNumber = $"NCR-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                SourceType = "goods_receipt",
                SourceId = receipt.Id,
                SkuId = primaryItem.SkuId,
                DefectType = "IQC_FAIL",
                DefectQty = defectQty,
                DefectDescription = request.Notes ?? $"Auto-created from IQC failure for {receipt.GrNumber}",
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

    private async Task SyncIqcHoldReservationsAsync(GoodsReceipt receipt, string result)
    {
        await ReleaseIqcHoldsAsync(receipt);

        if (!string.Equals(result, "fail", StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var item in receipt.Items.Where(i => i.RejectedQty > 0))
        {
            var skuCode = item.Sku?.SkuNumber;
            if (string.IsNullOrWhiteSpace(skuCode))
                continue;

            await _inventoryReservationService.ReserveAsync(
                skuCode,
                item.RejectedQty,
                "iqc-hold",
                receipt.Id);
        }
    }

    private async Task ReleaseIqcHoldsAsync(GoodsReceipt receipt)
    {
        var reservations = await _db.InventoryReservations
            .Include(r => r.Sku)
            .Where(r => r.OrderType == "iqc-hold" && r.OrderId == receipt.Id && r.Status == "reserved")
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
                await _inventoryReservationService.ReleaseAsync(skuCode, qty, "iqc-hold", receipt.Id);
        }
    }

    private async Task ScrapRejectedStockAsync(GoodsReceipt receipt, NonConformanceReport ncr, string? notes)
    {
        var sku = receipt.Items
            .Select(i => i.Sku)
            .FirstOrDefault(s => s != null && s.Id == ncr.SkuId);

        if (sku == null)
        {
            await ReleaseIqcHoldsAsync(receipt);
            return;
        }

        var qty = Math.Min(ncr.DefectQty, sku.Qty);
        if (qty > 0)
        {
            var qtyBefore = sku.Qty;
            sku.Qty = Math.Max(0, sku.Qty - qty);

            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                TrxId = $"IT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                SkuId = sku.Id,
                Type = "quality_scrap",
                QtyChange = -qty,
                QtyBefore = qtyBefore,
                QtyAfter = sku.Qty,
                RelatedDocType = "ncr",
                RelatedDocId = ncr.Id,
                Notes = notes ?? $"Scrapped from NCR {ncr.NcrNumber}"
            });
        }

        await ReleaseIqcHoldsAsync(receipt);
    }

    private static string NormalizeDisposition(string? disposition)
        => string.IsNullOrWhiteSpace(disposition) ? string.Empty : disposition.Trim().ToLowerInvariant();

    private static NonConformanceReportDto MapNcrDto(NonConformanceReport ncr, GoodsReceipt? receipt = null, List<QaReviewLogDto>? timeline = null)
    {
        var primaryItem = receipt?.Items
            .OrderByDescending(i => i.RejectedQty)
            .ThenByDescending(i => i.ReceivedQty)
            .FirstOrDefault();

        return new NonConformanceReportDto
        {
            Id = ncr.Id,
            NcrNumber = ncr.NcrNumber,
            SourceType = ncr.SourceType,
            SourceId = ncr.SourceId,
            SourceNumber = receipt?.GrNumber,
            SupplierName = receipt?.Supplier?.SupplierName,
            SkuName = primaryItem?.Sku?.SkuName,
            DefectType = ncr.DefectType,
            DefectQty = ncr.DefectQty,
            DefectDescription = ncr.DefectDescription,
            Disposition = ncr.Disposition,
            CostImpact = ncr.CostImpact,
            ResponsiblePerson = ncr.ResponsiblePerson,
            AttachmentUrl = ncr.AttachmentUrl,
            ClosureReason = ncr.ClosureReason,
            ClosedAt = ncr.ClosedAt,
            CreatedAt = ncr.CreatedAt,
            Timeline = timeline ?? new List<QaReviewLogDto>()
        };
    }

    private static SupplierComplaintDto MapComplaintDto(SupplierComplaint complaint, string? skuName = null, List<QaReviewLogDto>? timeline = null)
        => new()
        {
            Id = complaint.Id,
            ComplaintNumber = complaint.ComplaintNumber,
            SupplierName = complaint.Supplier?.SupplierName,
            SkuName = skuName,
            DefectType = complaint.DefectType,
            Ppm = complaint.Ppm,
            Priority = complaint.Priority,
            Status = complaint.Status,
            ResponsiblePerson = complaint.ResponsiblePerson,
            AttachmentUrl = complaint.AttachmentUrl,
            ClosureReason = complaint.ClosureReason,
            ClosedAt = complaint.ClosedAt,
            CreatedAt = complaint.CreatedAt,
            CapaNumber = complaint.Capa?.CapaNumber,
            CapaStatus = complaint.Capa?.Status,
            CapaResponsiblePerson = complaint.Capa?.ResponsiblePerson,
            CapaAttachmentUrl = complaint.Capa?.AttachmentUrl,
            CapaClosureReason = complaint.Capa?.ClosureReason,
            CapaClosedAt = complaint.Capa?.ClosedAt,
            Timeline = timeline ?? new List<QaReviewLogDto>()
        };

    private async Task<Dictionary<Guid, List<QaReviewLogDto>>> LoadReviewLogsAsync(string targetType, List<Guid> targetIds)
    {
        if (targetIds.Count == 0)
            return new Dictionary<Guid, List<QaReviewLogDto>>();

        var logs = await _db.QaReviewLogs
            .Where(l => l.TargetType == targetType && targetIds.Contains(l.TargetId))
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

        return logs
            .GroupBy(l => l.TargetId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(l => new QaReviewLogDto
                {
                    Id = l.Id,
                    TargetType = l.TargetType,
                    TargetId = l.TargetId,
                    ActionType = l.ActionType,
                    ActorName = l.ActorName,
                    ResponsiblePerson = l.ResponsiblePerson,
                    Notes = l.Notes,
                    AttachmentUrl = l.AttachmentUrl,
                    CreatedAt = l.CreatedAt
                }).ToList());
    }

    private async Task LogReviewAsync(string targetType, Guid targetId, string actionType, string? actorName, string? responsiblePerson, string? notes, string? attachmentUrl)
    {
        _db.QaReviewLogs.Add(new QaReviewLog
        {
            TargetType = targetType,
            TargetId = targetId,
            ActionType = actionType,
            ActorName = actorName,
            ResponsiblePerson = responsiblePerson,
            Notes = notes,
            AttachmentUrl = attachmentUrl
        });
        await Task.CompletedTask;
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
}
