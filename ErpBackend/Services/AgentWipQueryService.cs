using ErpBackend.Data;
using ErpBackend.Configs;
using ErpBackend.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ErpBackend.Services;

public interface IAgentWipQueryService
{
    Task<AgentWipQueryResultDto> GetWorkOrderAsync(
        string workOrderNumber,
        Guid actorUserId,
        string traceId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}

public sealed class AgentWipQueryService : IAgentWipQueryService
{
    private static readonly string[] WorkOrderFields =
    [
        "workOrder.number",
        "workOrder.product.number",
        "workOrder.product.name",
        "workOrder.productionUnit",
        "workOrder.status"
    ];

    private static readonly string[] OperationFields =
    [
        "authorizedOperations[].sequence",
        "authorizedOperations[].status"
    ];

    private readonly ErpGeneralDbContext _db;
    private readonly IAgentDisclosureAuditService _disclosureAudit;
    private readonly AgentWipAccessOptions _accessOptions;

    public AgentWipQueryService(
        ErpGeneralDbContext db,
        IAgentDisclosureAuditService disclosureAudit,
        IOptions<AgentWipAccessOptions> accessOptions)
    {
        _db = db;
        _disclosureAudit = disclosureAudit;
        _accessOptions = accessOptions.Value;
    }

    public async Task<AgentWipQueryResultDto> GetWorkOrderAsync(
        string workOrderNumber,
        Guid actorUserId,
        string traceId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (!_accessOptions.Enabled)
            return await BlockAsync(
                AgentWipQueryStatus.FeatureDisabled,
                actorUserId,
                traceId,
                ipAddress,
                userAgent,
                "server_feature_flag_disabled",
                cancellationToken);

        if (!_accessOptions.SeedUserIds.Contains(actorUserId))
            return await BlockAsync(
                AgentWipQueryStatus.NotInSeedCohort,
                actorUserId,
                traceId,
                ipAddress,
                userAgent,
                "user_not_in_seed_cohort",
                cancellationToken);

        var employeeId = await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == actorUserId && user.IsActive)
            .Select(user => user.EmployeeId)
            .SingleOrDefaultAsync(cancellationToken);

        if (employeeId is null)
            return await BlockAsync(
                AgentWipQueryStatus.ScopeUnavailable,
                actorUserId,
                traceId,
                ipAddress,
                userAgent,
                "active_employee_mapping_missing",
                cancellationToken);

        var departmentId = await _db.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == employeeId && employee.Status.ToLower() == "active")
            .Select(employee => employee.DepartmentId)
            .SingleOrDefaultAsync(cancellationToken);

        if (departmentId is null || !await _db.Departments
                .AsNoTracking()
                .AnyAsync(department => department.Id == departmentId && department.IsActive, cancellationToken))
            return await BlockAsync(
                AgentWipQueryStatus.ScopeUnavailable,
                actorUserId,
                traceId,
                ipAddress,
                userAgent,
                "active_department_mapping_missing",
                cancellationToken);

        var now = DateTime.UtcNow;
        var workCenterIds = await (
                from costCenter in _db.CostCenters.AsNoTracking()
                join workCenter in _db.WorkCenters.AsNoTracking()
                    on costCenter.WorkCenterId equals (Guid?)workCenter.Id
                where costCenter.DepartmentId == departmentId
                      && costCenter.IsActive
                      && costCenter.EffectiveFrom <= now
                      && (costCenter.EffectiveTo == null || costCenter.EffectiveTo >= now)
                      && workCenter.IsActive
                select workCenter.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (workCenterIds.Count == 0)
            return await BlockAsync(
                AgentWipQueryStatus.ScopeUnavailable,
                actorUserId,
                traceId,
                ipAddress,
                userAgent,
                "active_work_center_mapping_missing",
                cancellationToken);

        var workOrder = await _db.WorkOrders
            .AsNoTracking()
            .Where(order => order.WoNumber == workOrderNumber)
            .Select(order => new
            {
                order.Id,
                order.WoNumber,
                order.ProductSkuId,
                order.ProductionUomCode,
                order.Status,
                order.UpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (workOrder is null)
            return await BlockAsync(
                AgentWipQueryStatus.NotFound,
                actorUserId,
                traceId,
                ipAddress,
                userAgent,
                "work_order_missing_or_out_of_scope",
                cancellationToken);

        var authorizedOperations = await _db.WorkOrderOperations
            .AsNoTracking()
            .Where(operation =>
                operation.WorkOrderId == workOrder.Id &&
                workCenterIds.Contains(operation.WorkCenterId))
            .OrderBy(operation => operation.OperationSeq)
            .Select(operation => new
            {
                operation.OperationSeq,
                operation.WorkCenterId,
                operation.PlannedQty,
                operation.Status
            })
            .ToListAsync(cancellationToken);

        var totalOperationCount = await _db.WorkOrderOperations
            .AsNoTracking()
            .CountAsync(operation => operation.WorkOrderId == workOrder.Id, cancellationToken);

        if (authorizedOperations.Count == 0)
            return await BlockAsync(
                AgentWipQueryStatus.NotFound,
                actorUserId,
                traceId,
                ipAddress,
                userAgent,
                $"work_order_missing_or_out_of_scope;filtered_operations={totalOperationCount}",
                cancellationToken,
                workOrder.Id);

        var finalOperationSequence = await _db.WorkOrderOperations
            .AsNoTracking()
            .Where(operation => operation.WorkOrderId == workOrder.Id)
            .MaxAsync(operation => (int?)operation.OperationSeq, cancellationToken);

        var finalSequenceCount = finalOperationSequence is null
            ? 0
            : await _db.WorkOrderOperations
                .AsNoTracking()
                .CountAsync(
                    operation => operation.WorkOrderId == workOrder.Id
                        && operation.OperationSeq == finalOperationSequence,
                    cancellationToken);

        if (finalSequenceCount != 1)
            return await BlockAsync(
                AgentWipQueryStatus.DataQualityBlocked,
                actorUserId,
                traceId,
                ipAddress,
                userAgent,
                "final_operation_missing_or_ambiguous",
                cancellationToken,
                workOrder.Id);

        var finalOperation = authorizedOperations
            .SingleOrDefault(operation => operation.OperationSeq == finalOperationSequence);

        var product = await _db.Skus
            .AsNoTracking()
            .Where(sku => sku.Id == workOrder.ProductSkuId)
            .Select(sku => new { sku.SkuNumber, sku.SkuName, sku.UpdatedAt })
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null || string.IsNullOrWhiteSpace(workOrder.ProductionUomCode))
            return await BlockAsync(
                AgentWipQueryStatus.DataQualityBlocked,
                actorUserId,
                traceId,
                ipAddress,
                userAgent,
                product is null ? "product_mapping_missing" : "production_unit_missing",
                cancellationToken,
                workOrder.Id);

        var orderPrefix = $"work-order/{workOrder.Id:N}";
        var authorizedOperationFacts = authorizedOperations
            .Select(operation => new AgentWipOperationDto(
                IntFact(
                    $"{orderPrefix}/operation/{operation.OperationSeq}/sequence",
                    operation.OperationSeq,
                    "WorkOrderOperations",
                    "OperationSeq",
                    workOrder.UpdatedAt),
                StringFact(
                    $"{orderPrefix}/operation/{operation.OperationSeq}/status",
                    operation.Status,
                    "WorkOrderOperations",
                    "Status",
                    workOrder.UpdatedAt)))
            .ToList();

        AgentWipFinalOperationDto? finalOperationFacts = null;
        var disclosedFields = WorkOrderFields.Concat(OperationFields).ToList();

        if (finalOperation is not null)
        {
            var approvedReports = await _db.WipReports
                .AsNoTracking()
                .Where(report =>
                    report.WorkOrderId == workOrder.Id
                    && report.OperationSeq == finalOperation.OperationSeq
                    && report.Status.ToLower() == "approved")
                .Select(report => new
                {
                    report.CompletedQty,
                    report.DefectQty,
                    report.ReviewedAt
                })
                .ToListAsync(cancellationToken);

            var goodQuantity = approvedReports.Sum(report => report.CompletedQty);
            var defectQuantity = approvedReports.Sum(report => report.DefectQty);
            var denominator = goodQuantity + defectQuantity;
            var defectRate = denominator == 0
                ? (decimal?)null
                : defectQuantity / denominator * 100m;
            var latestReview = approvedReports
                .Where(report => report.ReviewedAt.HasValue)
                .Select(report => report.ReviewedAt)
                .Max();
            var aggregateAsOf = latestReview.HasValue && latestReview > workOrder.UpdatedAt
                ? latestReview
                : workOrder.UpdatedAt;
            var remainingQuantity = Math.Max(finalOperation.PlannedQty - goodQuantity, 0m);
            var finalPrefix = $"{orderPrefix}/operation/{finalOperation.OperationSeq}";
            var unit = workOrder.ProductionUomCode;

            finalOperationFacts = new AgentWipFinalOperationDto(
                IntFact(
                    $"{finalPrefix}/sequence",
                    finalOperation.OperationSeq,
                    "WorkOrderOperations",
                    "OperationSeq",
                    workOrder.UpdatedAt),
                StringFact(
                    $"{finalPrefix}/status",
                    finalOperation.Status,
                    "WorkOrderOperations",
                    "Status",
                    workOrder.UpdatedAt),
                DecimalFact(
                    $"{finalPrefix}/planned-quantity",
                    finalOperation.PlannedQty,
                    "WorkOrderOperations",
                    "PlannedQty",
                    workOrder.UpdatedAt,
                    unit),
                DecimalFact(
                    $"{finalPrefix}/good-quantity",
                    goodQuantity,
                    "WipReports",
                    "CompletedQty (approved final-operation reports)",
                    latestReview,
                    unit),
                DecimalFact(
                    $"{finalPrefix}/defect-quantity",
                    defectQuantity,
                    "WipReports",
                    "DefectQty (approved final-operation reports)",
                    latestReview,
                    unit),
                NullableDecimalFact(
                    $"{finalPrefix}/defect-rate-percent",
                    defectRate,
                    "WipReports",
                    "DefectQty / (CompletedQty + DefectQty) * 100",
                    latestReview,
                    "%",
                    denominator == 0 ? "zero_denominator" : null),
                DecimalFact(
                    $"{finalPrefix}/remaining-quantity",
                    remainingQuantity,
                    "WorkOrderOperations,WipReports",
                    "max(PlannedQty - CompletedQty, 0)",
                    aggregateAsOf,
                    unit));

            disclosedFields.AddRange(
            [
                "finalOperation.sequence",
                "finalOperation.status",
                "finalOperation.plannedQuantity",
                "finalOperation.goodQuantity",
                "finalOperation.defectQuantity",
                "finalOperation.defectRatePercent",
                "finalOperation.remainingQuantity"
            ]);
        }

        var response = new AgentWipWorkOrderDto(
            StringFact(
                $"{orderPrefix}/number",
                workOrder.WoNumber,
                "WorkOrders",
                "WoNumber",
                workOrder.UpdatedAt),
            StringFact(
                $"{orderPrefix}/product-number",
                product.SkuNumber,
                "Skus",
                "SkuNumber",
                product.UpdatedAt),
            StringFact(
                $"{orderPrefix}/product-name",
                product.SkuName,
                "Skus",
                "SkuName",
                product.UpdatedAt),
            StringFact(
                $"{orderPrefix}/production-unit",
                workOrder.ProductionUomCode,
                "WorkOrders",
                "ProductionUomCode",
                workOrder.UpdatedAt),
            StringFact(
                $"{orderPrefix}/status",
                workOrder.Status,
                "WorkOrders",
                "Status",
                workOrder.UpdatedAt),
            authorizedOperationFacts,
            finalOperationFacts);

        if (!AgentWipFactAlignmentValidator.TryValidateFacts(response, out var alignmentFailure))
            return await BlockAsync(
                AgentWipQueryStatus.DataQualityBlocked,
                actorUserId,
                traceId,
                ipAddress,
                userAgent,
                alignmentFailure,
                cancellationToken,
                workOrder.Id);

        await _disclosureAudit.RecordAsync(
            new AgentDisclosureAuditEntryDto(
                actorUserId,
                traceId,
                "disclosed",
                $"authorized_operations={authorizedOperations.Count};filtered_operations={totalOperationCount - authorizedOperations.Count};final_operation_disclosed={finalOperation is not null}",
                workOrder.Id,
                disclosedFields,
                ipAddress,
                userAgent),
            cancellationToken);

        return new AgentWipQueryResultDto(AgentWipQueryStatus.Success, response);
    }

    private async Task<AgentWipQueryResultDto> BlockAsync(
        AgentWipQueryStatus status,
        Guid actorUserId,
        string traceId,
        string? ipAddress,
        string? userAgent,
        string reason,
        CancellationToken cancellationToken,
        Guid? workOrderId = null)
    {
        await _disclosureAudit.RecordAsync(
            new AgentDisclosureAuditEntryDto(
                actorUserId,
                traceId,
                status switch
                {
                    AgentWipQueryStatus.FeatureDisabled => "feature_disabled",
                    AgentWipQueryStatus.NotInSeedCohort => "seed_cohort_blocked",
                    AgentWipQueryStatus.ScopeUnavailable => "scope_blocked",
                    AgentWipQueryStatus.NotFound => "not_found",
                    _ => "data_quality_blocked"
                },
                reason,
                workOrderId,
                [],
                ipAddress,
                userAgent),
            cancellationToken);

        return new AgentWipQueryResultDto(status);
    }

    private static AgentWipStringFactDto StringFact(
        string factId,
        string value,
        string sourceTable,
        string sourceField,
        DateTime? asOf)
        => new(factId, value, sourceTable, sourceField, asOf);

    private static AgentWipIntFactDto IntFact(
        string factId,
        int value,
        string sourceTable,
        string sourceField,
        DateTime? asOf)
        => new(factId, value, sourceTable, sourceField, asOf);

    private static AgentWipDecimalFactDto DecimalFact(
        string factId,
        decimal value,
        string sourceTable,
        string sourceField,
        DateTime? asOf,
        string? unit = null)
        => new(factId, value, sourceTable, sourceField, asOf, unit);

    private static AgentWipNullableDecimalFactDto NullableDecimalFact(
        string factId,
        decimal? value,
        string sourceTable,
        string sourceField,
        DateTime? asOf,
        string? unit,
        string? notApplicableReason)
        => new(factId, value, sourceTable, sourceField, asOf, unit, notApplicableReason);
}
