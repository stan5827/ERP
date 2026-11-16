using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Helpers;
using ErpBackend.Models.QA;

namespace ErpBackend.Services;

public interface IProcessInspectionService
{
    Task<List<InspectionTemplateDto>> GetTemplatesAsync(Guid? operationId, Guid? productSkuId);
    Task<InspectionTemplateDto?> GetTemplateAsync(Guid id);
    Task<(InspectionTemplateDto? dto, string status)> CreateTemplateAsync(SaveInspectionTemplateRequestDto request);
    Task<(InspectionTemplateDto? dto, string status)> UpdateTemplateAsync(Guid id, SaveInspectionTemplateRequestDto request);
    Task<List<WorkOrderInspectionOperationDto>?> GetWorkOrderOperationsAsync(Guid workOrderId);
    Task<List<ProcessInspectionReportDto>> GetReportsAsync(Guid? workOrderId, string? lotNumber);
    Task<(ProcessInspectionReportDto? dto, string status)> CreateReportAsync(CreateProcessInspectionRequestDto request);
    Task<DeliveryInspectionReportDto?> GetDeliveryReportAsync(Guid deliveryOrderId);
}

public class ProcessInspectionService : IProcessInspectionService
{
    private static readonly HashSet<string> InputTypes = new(StringComparer.OrdinalIgnoreCase)
        { "number", "text", "boolean", "choice", "passFail" };
    private static readonly HashSet<string> Results = new(StringComparer.OrdinalIgnoreCase)
        { "pass", "fail", "na" };

    private readonly ErpGeneralDbContext _db;

    public ProcessInspectionService(ErpGeneralDbContext db) => _db = db;

    public async Task<List<InspectionTemplateDto>> GetTemplatesAsync(Guid? operationId, Guid? productSkuId)
    {
        var query = _db.InspectionTemplates
            .AsNoTracking()
            .Include(t => t.Operation)
            .Include(t => t.ProductSkus).ThenInclude(ts => ts.ProductSku)
            .Include(t => t.Items)
            .AsQueryable();
        if (operationId.HasValue) query = query.Where(t => t.OperationId == operationId.Value);
        if (productSkuId.HasValue)
            query = query.Where(t => !t.ProductSkus.Any() ||
                t.ProductSkus.Any(ts => ts.ProductSkuId == productSkuId.Value));

        var templates = await query
            .OrderBy(t => t.Operation!.OperationCode)
            .ThenBy(t => !t.ProductSkus.Any())
            .ThenBy(t => t.TemplateCode)
            .ToListAsync();
        return templates.Select(ToTemplateDto).ToList();
    }

    public async Task<InspectionTemplateDto?> GetTemplateAsync(Guid id)
    {
        var template = await _db.InspectionTemplates
            .AsNoTracking()
            .Include(t => t.Operation)
            .Include(t => t.ProductSkus).ThenInclude(ts => ts.ProductSku)
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id);
        return template == null ? null : ToTemplateDto(template);
    }

    public async Task<(InspectionTemplateDto? dto, string status)> CreateTemplateAsync(SaveInspectionTemplateRequestDto request)
    {
        var validation = await ValidateTemplateAsync(request);
        if (validation != "ok") return (null, validation);

        var duplicate = await _db.InspectionTemplates.AnyAsync(t =>
            t.TemplateCode == request.TemplateCode && t.Version == 1);
        if (duplicate) return (null, "duplicate");

        var template = new InspectionTemplate
        {
            TemplateCode = request.TemplateCode.Trim(),
            TemplateName = request.TemplateName.Trim(),
            OperationId = request.OperationId,
            ProductSkus = ToTemplateSkus(request.ProductSkuIds),
            IsActive = request.IsActive,
            Notes = request.Notes?.Trim(),
            Items = request.Items.OrderBy(i => i.SortOrder).Select(ToTemplateItem).ToList()
        };

        _db.InspectionTemplates.Add(template);
        await _db.SaveChangesAsync();
        return (await GetTemplateAsync(template.Id), "ok");
    }

    public async Task<(InspectionTemplateDto? dto, string status)> UpdateTemplateAsync(Guid id, SaveInspectionTemplateRequestDto request)
    {
        var template = await _db.InspectionTemplates
            .Include(t => t.Items)
            .Include(t => t.ProductSkus)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return (null, "not_found");

        var validation = await ValidateTemplateAsync(request, id);
        if (validation != "ok") return (null, validation);
        if (await _db.InspectionTemplates.AnyAsync(t => t.Id != id &&
            t.TemplateCode == request.TemplateCode && t.Version == template.Version + 1))
            return (null, "duplicate");

        template.TemplateCode = request.TemplateCode.Trim();
        template.TemplateName = request.TemplateName.Trim();
        template.OperationId = request.OperationId;
        var requestedSkuIds = request.ProductSkuIds.Distinct().ToHashSet();
        foreach (var existingSku in template.ProductSkus.Where(ts => !requestedSkuIds.Contains(ts.ProductSkuId)).ToList())
            _db.InspectionTemplateSkus.Remove(existingSku);
        var existingSkuIds = template.ProductSkus.Select(ts => ts.ProductSkuId).ToHashSet();
        foreach (var skuId in requestedSkuIds.Where(skuId => !existingSkuIds.Contains(skuId)))
            template.ProductSkus.Add(new InspectionTemplateSku { TemplateId = template.Id, ProductSkuId = skuId });
        template.Version++;
        template.IsActive = request.IsActive;
        template.Notes = request.Notes?.Trim();
        template.UpdatedAt = DateTime.UtcNow;
        foreach (var existing in template.Items)
            existing.IsActive = false;
        foreach (var item in request.Items.OrderBy(i => i.SortOrder))
        {
            var existing = template.Items.FirstOrDefault(i =>
                string.Equals(i.ItemCode, item.ItemCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                template.Items.Add(ToTemplateItem(item));
                continue;
            }

            UpdateTemplateItem(existing, item);
        }

        await _db.SaveChangesAsync();
        return (await GetTemplateAsync(template.Id), "ok");
    }

    public async Task<List<WorkOrderInspectionOperationDto>?> GetWorkOrderOperationsAsync(Guid workOrderId)
    {
        var workOrder = await _db.WorkOrders
            .AsNoTracking()
            .Include(w => w.Operations).ThenInclude(o => o.Operation)
            .Include(w => w.Operations).ThenInclude(o => o.WorkCenter)
            .FirstOrDefaultAsync(w => w.Id == workOrderId);
        if (workOrder == null) return null;

        var operationIds = workOrder.Operations.Select(o => o.OperationId).Distinct().ToList();
        var templates = await _db.InspectionTemplates
            .AsNoTracking()
            .Where(t => t.IsActive && operationIds.Contains(t.OperationId) &&
                (!t.ProductSkus.Any() || t.ProductSkus.Any(ts => ts.ProductSkuId == workOrder.ProductSkuId)))
            .Include(t => t.Operation)
            .Include(t => t.ProductSkus).ThenInclude(ts => ts.ProductSku)
            .Include(t => t.Items)
            .ToListAsync();

        return workOrder.Operations
            .OrderBy(o => o.OperationSeq)
            .Select(operation =>
            {
                var eligible = templates.Where(t => t.OperationId == operation.OperationId);
                var productTemplates = eligible
                    .Where(t => t.ProductSkus.Any(ts => ts.ProductSkuId == workOrder.ProductSkuId))
                    .ToList();
                var selected = productTemplates.Count > 0
                    ? productTemplates
                    : eligible.Where(t => t.ProductSkus.Count == 0).ToList();
                return new WorkOrderInspectionOperationDto
                {
                    OperationSeq = operation.OperationSeq,
                    OperationId = operation.OperationId,
                    OperationCode = operation.Operation?.OperationCode ?? "",
                    OperationName = operation.Operation?.OperationName ?? "",
                    WorkCenterId = operation.WorkCenterId,
                    WorkCenterName = operation.WorkCenter?.WorkCenterName,
                    Templates = selected.OrderBy(t => t.TemplateCode).Select(ToTemplateDto).ToList()
                };
            }).ToList();
    }

    public async Task<List<ProcessInspectionReportDto>> GetReportsAsync(Guid? workOrderId, string? lotNumber)
    {
        var query = _db.ProcessInspectionReports
            .AsNoTracking()
            .Include(r => r.WorkOrder).ThenInclude(w => w!.ProductSku)
            .Include(r => r.WorkOrder).ThenInclude(w => w!.Operations).ThenInclude(o => o.Operation)
            .Include(r => r.WorkOrder).ThenInclude(w => w!.Operations).ThenInclude(o => o.WorkCenter)
            .Include(r => r.Template)
            .Include(r => r.Items)
            .AsQueryable();
        if (workOrderId.HasValue) query = query.Where(r => r.WorkOrderId == workOrderId.Value);
        if (!string.IsNullOrWhiteSpace(lotNumber)) query = query.Where(r => r.LotNumber == lotNumber);

        var reports = await query.OrderByDescending(r => r.InspectedAt).Take(500).ToListAsync();
        return reports.Select(report => ToReportDto(report)).ToList();
    }

    public async Task<(ProcessInspectionReportDto? dto, string status)> CreateReportAsync(CreateProcessInspectionRequestDto request)
    {
        if (request.SampleQuantity <= 0 || string.IsNullOrWhiteSpace(request.InspectorName))
            return (null, "invalid");

        var workOrder = await _db.WorkOrders
            .Include(w => w.ProductSku)
            .Include(w => w.Operations)
            .FirstOrDefaultAsync(w => w.Id == request.WorkOrderId);
        if (workOrder == null) return (null, "work_order_not_found");

        var operation = workOrder.Operations.FirstOrDefault(o => o.OperationSeq == request.OperationSeq);
        if (operation == null) return (null, "operation_not_found");

        var template = await _db.InspectionTemplates
            .Include(t => t.Items)
            .Include(t => t.ProductSkus)
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId);
        if (template == null || !template.IsActive) return (null, "template_not_found");
        if (template.OperationId != operation.OperationId ||
            (template.ProductSkus.Count > 0 &&
             !template.ProductSkus.Any(ts => ts.ProductSkuId == workOrder.ProductSkuId)))
            return (null, "template_mismatch");

        var responses = request.Items ?? new List<CreateProcessInspectionItemRequestDto>();
        if (responses.Select(i => i.TemplateItemId).Distinct().Count() != responses.Count)
            return (null, "invalid");

        var itemsById = template.Items.ToDictionary(i => i.Id);
        if (responses.Any(i => !itemsById.ContainsKey(i.TemplateItemId)))
            return (null, "invalid");
        if (template.Items.Any(i => i.IsRequired &&
            !responses.Any(r => r.TemplateItemId == i.Id && !string.IsNullOrWhiteSpace(r.Value))))
            return (null, "required_item_missing");

        var report = new ProcessInspectionReport
        {
            ReportNumber = $"PI-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..27],
            WorkOrderId = workOrder.Id,
            OperationSeq = operation.OperationSeq,
            TemplateId = template.Id,
            TemplateVersion = template.Version,
            ProductSkuId = workOrder.ProductSkuId,
            LotNumber = string.IsNullOrWhiteSpace(request.LotNumber) ? null : request.LotNumber.Trim(),
            InspectorName = request.InspectorName.Trim(),
            SampleQuantity = request.SampleQuantity,
            Notes = request.Notes?.Trim(),
            InspectedAt = DateTime.UtcNow
        };

        foreach (var response in responses)
        {
            var templateItem = itemsById[response.TemplateItemId];
            if (templateItem.IsRequired && string.IsNullOrWhiteSpace(response.Value))
                return (null, "required_item_missing");

            var value = response.Value?.Trim();
            var result = string.IsNullOrWhiteSpace(value) && !templateItem.IsRequired
                ? "na"
                : response.Result.ToLowerInvariant();
            if (templateItem.InputType == "number" && !string.IsNullOrWhiteSpace(value))
            {
                if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var measured))
                    return (null, "invalid_numeric_value");
                result = !templateItem.LowerLimit.HasValue && !templateItem.UpperLimit.HasValue ||
                    (measured >= (templateItem.LowerLimit ?? decimal.MinValue) &&
                     measured <= (templateItem.UpperLimit ?? decimal.MaxValue)) ? "pass" : "fail";
            }
            else if (!(result == "na" && !templateItem.IsRequired) && !Results.Contains(result))
            {
                return (null, "invalid_result");
            }

            if (templateItem.InputType == "choice" && !string.IsNullOrWhiteSpace(value))
            {
                var options = DeserializeOptions(templateItem.OptionsJson);
                if (!options.Contains(value, StringComparer.Ordinal))
                    return (null, "invalid_choice");
            }

            report.Items.Add(new ProcessInspectionReportItem
            {
                TemplateItemId = templateItem.Id,
                ItemName = templateItem.ItemName,
                InputType = templateItem.InputType,
                Specification = templateItem.Specification,
                Unit = templateItem.Unit,
                LowerLimit = templateItem.LowerLimit,
                UpperLimit = templateItem.UpperLimit,
                InspectionMethod = templateItem.InspectionMethod,
                Instrument = templateItem.Instrument,
                SamplingPlan = templateItem.SamplingPlan,
                IsCritical = templateItem.IsCritical,
                Value = value,
                Result = result,
                Notes = response.Notes?.Trim()
            });
        }

        report.Result = report.Items.Any(i => i.Result == "fail") ? "fail" : "pass";
        _db.ProcessInspectionReports.Add(report);
        await _db.SaveChangesAsync();
        var savedReports = await GetReportsAsync(report.WorkOrderId, report.LotNumber);
        return (savedReports.FirstOrDefault(r => r.Id == report.Id), "ok");
    }

    public async Task<DeliveryInspectionReportDto?> GetDeliveryReportAsync(Guid deliveryOrderId)
    {
        var delivery = await _db.DeliveryOrders
            .AsNoTracking()
            .Include(d => d.SalesOrder).ThenInclude(s => s!.Customer)
            .Include(d => d.Items).ThenInclude(i => i.Sku)
            .FirstOrDefaultAsync(d => d.Id == deliveryOrderId);
        if (delivery == null) return null;

        var skuIds = delivery.Items.Select(i => i.SkuId).Distinct().ToList();
        var workOrders = await _db.WorkOrders
            .AsNoTracking()
            .Where(w => w.SalesOrderId == delivery.SalesOrderId && skuIds.Contains(w.ProductSkuId))
            .Include(w => w.ProductSku)
            .Include(w => w.Operations).ThenInclude(o => o.Operation)
            .Include(w => w.Operations).ThenInclude(o => o.WorkCenter)
            .ToListAsync();
        var workOrderIds = workOrders.Select(w => w.Id).ToList();
        var operationIds = workOrders.SelectMany(w => w.Operations).Select(o => o.OperationId).Distinct().ToList();

        var templates = await _db.InspectionTemplates
            .AsNoTracking()
            .Where(t => t.IsActive && operationIds.Contains(t.OperationId) &&
                (!t.ProductSkus.Any() || t.ProductSkus.Any(ts => skuIds.Contains(ts.ProductSkuId))))
            .Include(t => t.Operation)
            .Include(t => t.ProductSkus).ThenInclude(ts => ts.ProductSku)
            .Include(t => t.Items)
            .ToListAsync();
        var reports = await _db.ProcessInspectionReports
            .AsNoTracking()
            .Where(r => workOrderIds.Contains(r.WorkOrderId))
            .Include(r => r.Template)
            .Include(r => r.Items)
            .ToListAsync();

        var output = new DeliveryInspectionReportDto
        {
            DeliveryOrderId = delivery.Id,
            DeliveryNumber = delivery.DeliveryNumber,
            DeliveryDate = delivery.DeliveryDate,
            SalesOrderNumber = delivery.SalesOrder?.OrderNumber,
            CustomerName = delivery.SalesOrder?.Customer?.CustomerName,
            DeliveryAddress = delivery.DeliveryAddress
        };

        foreach (var item in delivery.Items)
        {
            var itemWorkOrders = workOrders.Where(w => w.ProductSkuId == item.SkuId).ToList();
            var expected = itemWorkOrders.SelectMany(w => w.Operations.Select(operation =>
            {
                var eligible = templates.Where(t => t.OperationId == operation.OperationId &&
                    (t.ProductSkus.Count == 0 || t.ProductSkus.Any(ts => ts.ProductSkuId == w.ProductSkuId)));
                var exact = eligible.Where(t => t.ProductSkus.Any(ts => ts.ProductSkuId == w.ProductSkuId)).ToList();
                var template = (exact.Count > 0 ? exact : eligible.Where(t => t.ProductSkus.Count == 0).ToList())
                    .OrderByDescending(t => t.Version).FirstOrDefault();
                return (WorkOrder: w, Operation: operation, Template: template);
            })).ToList();

            var selectedReports = reports.Where(r => itemWorkOrders.Any(w => w.Id == r.WorkOrderId) &&
                (string.IsNullOrWhiteSpace(item.LotNumber) || r.LotNumber == item.LotNumber)).ToList();
            var row = new DeliveryInspectionItemDto
            {
                DeliveryOrderItemId = item.Id,
                SkuId = item.SkuId,
                SkuNumber = item.Sku?.SkuNumber,
                SkuName = item.Sku?.SkuName,
                LotNumber = item.LotNumber,
                Quantity = item.DeliveredQty
            };

            if (itemWorkOrders.Count == 0)
                row.Warnings.Add("找不到此銷售訂單與料號對應的工單，無法追溯工藝檢驗。");
            if (string.IsNullOrWhiteSpace(item.LotNumber))
                row.Warnings.Add("出貨明細未填批號，報告會彙整此銷售訂單同料號的工單檢驗紀錄。");

            foreach (var expectedOperation in expected)
            {
                if (expectedOperation.Template == null)
                {
                    row.Warnings.Add($"工單 {expectedOperation.WorkOrder.WoNumber} 的第 {expectedOperation.Operation.OperationSeq} 道工藝尚未設定檢驗範本。");
                    continue;
                }

                var matching = selectedReports.Where(r => r.WorkOrderId == expectedOperation.WorkOrder.Id &&
                    r.OperationSeq == expectedOperation.Operation.OperationSeq &&
                    r.TemplateId == expectedOperation.Template.Id).ToList();
                if (matching.Count == 0)
                {
                    row.Warnings.Add($"工單 {expectedOperation.WorkOrder.WoNumber} 的工藝「{expectedOperation.Operation.Operation?.OperationName}」尚無檢驗紀錄。");
                    continue;
                }
            }

            row.Reports = selectedReports.Select(report =>
            {
                var workOrder = itemWorkOrders.First(w => w.Id == report.WorkOrderId);
                return ToReportDto(report, workOrder);
            }).OrderBy(r => r.OperationSeq).ThenBy(r => r.InspectedAt).ToList();

            var failed = row.Reports.Any(r => r.Result == "fail");
            var missing = row.Warnings.Any(w => w.Contains("尚未設定") || w.Contains("尚無檢驗紀錄") || w.Contains("找不到"));
            row.Result = failed ? "fail" : missing ? "incomplete" : row.Reports.Count == 0 ? "pending" : "pass";
            output.Items.Add(row);
        }

        var allWarnings = output.Items.SelectMany(i => i.Warnings).Distinct().ToList();
        output.Warnings.AddRange(allWarnings);
        output.OverallResult = output.Items.Any(i => i.Result == "fail") ? "fail" :
            output.Items.Any(i => i.Result != "pass") ? "incomplete" : "pass";
        return output;
    }

    private async Task<string> ValidateTemplateAsync(SaveInspectionTemplateRequestDto request, Guid? currentId = null)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateCode) || string.IsNullOrWhiteSpace(request.TemplateName) ||
            request.OperationId == Guid.Empty || request.Items.Count == 0)
            return "invalid";
        if (!await _db.Operations.AnyAsync(o => o.Id == request.OperationId)) return "operation_not_found";
        var productSkuIds = request.ProductSkuIds.Distinct().ToList();
        if (productSkuIds.Count > 0 &&
            await _db.Skus.CountAsync(s => productSkuIds.Contains(s.Id)) != productSkuIds.Count)
            return "sku_not_found";
        if (request.Items.Any(i => string.IsNullOrWhiteSpace(i.ItemCode) ||
            string.IsNullOrWhiteSpace(i.ItemName) || !InputTypes.Contains(i.InputType) ||
            i.LowerLimit.HasValue && i.UpperLimit.HasValue && i.LowerLimit > i.UpperLimit))
            return "invalid";
        if (request.Items.Any(i => i.InputType == "choice" && i.Options.Count == 0)) return "invalid";
        if (request.Items.Select(i => i.ItemCode.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Items.Count)
            return "invalid";

        if (request.IsActive)
        {
            var activeTemplates = _db.InspectionTemplates.Where(t =>
                t.Id != currentId && t.IsActive && t.OperationId == request.OperationId);
            var hasConflict = productSkuIds.Count == 0
                ? await activeTemplates.AnyAsync(t => !t.ProductSkus.Any())
                : await activeTemplates.AnyAsync(t =>
                    t.ProductSkus.Any(ts => productSkuIds.Contains(ts.ProductSkuId)));
            if (hasConflict) return "active_template_exists";
        }
        return "ok";
    }

    private static List<InspectionTemplateSku> ToTemplateSkus(IEnumerable<Guid> productSkuIds) =>
        productSkuIds.Distinct()
            .Select(productSkuId => new InspectionTemplateSku { ProductSkuId = productSkuId })
            .ToList();

    private static InspectionTemplateItem ToTemplateItem(SaveInspectionTemplateItemRequestDto item) => new()
    {
        ItemCode = item.ItemCode.Trim(),
        ItemName = item.ItemName.Trim(),
        InputType = item.InputType.Trim().ToLowerInvariant(),
        Specification = item.Specification?.Trim(),
        Unit = item.Unit?.Trim(),
        LowerLimit = item.LowerLimit,
        UpperLimit = item.UpperLimit,
        OptionsJson = JsonSerializer.Serialize(item.Options),
        InspectionMethod = item.InspectionMethod?.Trim(),
        Instrument = item.Instrument?.Trim(),
        SamplingPlan = item.SamplingPlan?.Trim(),
        IsRequired = item.IsRequired,
        IsCritical = item.IsCritical,
        SortOrder = item.SortOrder
    };

    private static void UpdateTemplateItem(
        InspectionTemplateItem existing,
        SaveInspectionTemplateItemRequestDto item)
    {
        existing.ItemName = item.ItemName.Trim();
        existing.InputType = item.InputType.Trim().ToLowerInvariant();
        existing.Specification = item.Specification?.Trim();
        existing.Unit = item.Unit?.Trim();
        existing.LowerLimit = item.LowerLimit;
        existing.UpperLimit = item.UpperLimit;
        existing.OptionsJson = JsonSerializer.Serialize(item.Options);
        existing.InspectionMethod = item.InspectionMethod?.Trim();
        existing.Instrument = item.Instrument?.Trim();
        existing.SamplingPlan = item.SamplingPlan?.Trim();
        existing.IsRequired = item.IsRequired;
        existing.IsCritical = item.IsCritical;
        existing.IsActive = true;
        existing.SortOrder = item.SortOrder;
    }

    private static InspectionTemplateDto ToTemplateDto(InspectionTemplate template) => new()
    {
        Id = template.Id,
        TemplateCode = template.TemplateCode,
        TemplateName = template.TemplateName,
        OperationId = template.OperationId,
        OperationCode = template.Operation?.OperationCode,
        OperationName = template.Operation?.OperationName,
        ProductSkus = template.ProductSkus
            .OrderBy(ts => ts.ProductSku.SkuNumber)
            .Select(ts => new InspectionTemplateProductDto
            {
                Id = ts.ProductSkuId,
                SkuNumber = ts.ProductSku.SkuNumber,
                SkuName = ts.ProductSku.SkuName
            }).ToList(),
        Version = template.Version,
        IsActive = template.IsActive,
        Notes = template.Notes,
        Items = template.Items.Where(i => i.IsActive).OrderBy(i => i.SortOrder).Select(i => new InspectionTemplateItemDto
        {
            Id = i.Id,
            ItemCode = i.ItemCode,
            ItemName = i.ItemName,
            InputType = i.InputType,
            Specification = i.Specification,
            Unit = i.Unit,
            LowerLimit = i.LowerLimit,
            UpperLimit = i.UpperLimit,
            Options = DeserializeOptions(i.OptionsJson),
            InspectionMethod = i.InspectionMethod,
            Instrument = i.Instrument,
            SamplingPlan = i.SamplingPlan,
            IsRequired = i.IsRequired,
            IsCritical = i.IsCritical,
            SortOrder = i.SortOrder
        }).ToList()
    };

    private static List<string> DeserializeOptions(string? json) =>
        string.IsNullOrWhiteSpace(json) ? new List<string>() :
            JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

    private static ProcessInspectionReportDto ToReportDto(ProcessInspectionReport report, ErpBackend.Models.MES.WorkOrder? workOrder = null)
    {
        workOrder ??= report.WorkOrder;
        var operation = workOrder?.Operations.FirstOrDefault(o => o.OperationSeq == report.OperationSeq);
        return new ProcessInspectionReportDto
        {
            Id = report.Id,
            ReportNumber = report.ReportNumber,
            WorkOrderId = report.WorkOrderId,
            WorkOrderNumber = workOrder?.WoNumber,
            OperationSeq = report.OperationSeq,
            OperationCode = operation?.Operation?.OperationCode,
            OperationName = operation?.Operation?.OperationName,
            WorkCenterName = operation?.WorkCenter?.WorkCenterName,
            TemplateId = report.TemplateId,
            TemplateName = report.Template?.TemplateName,
            TemplateVersion = report.TemplateVersion,
            ProductSkuId = report.ProductSkuId,
            ProductSkuNumber = workOrder?.ProductSku?.SkuNumber,
            ProductSkuName = workOrder?.ProductSku?.SkuName,
            LotNumber = report.LotNumber,
            InspectorName = report.InspectorName,
            InspectedAt = report.InspectedAt,
            SampleQuantity = report.SampleQuantity,
            Result = report.Result,
            Notes = report.Notes,
            Items = report.Items.Select(i => new ProcessInspectionReportItemDto
            {
                ItemName = i.ItemName,
                InputType = i.InputType,
                Specification = i.Specification,
                Unit = i.Unit,
                LowerLimit = i.LowerLimit,
                UpperLimit = i.UpperLimit,
                InspectionMethod = i.InspectionMethod,
                Instrument = i.Instrument,
                SamplingPlan = i.SamplingPlan,
                IsCritical = i.IsCritical,
                Value = i.Value,
                Result = i.Result,
                Notes = i.Notes
            }).ToList()
        };
    }
}
