using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Helpers;
using ErpBackend.Models.Finance;
using ErpBackend.Models.MES;
using ErpBackend.Services;

namespace ErpBackend.Services;

public interface IWipReportService
{
    Task<(WipReportResultDto? result, string status)> ReportCompletionAsync(
        WipReportRequest request,
        Guid actorUserId,
        bool canProxyReport,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WipReportQueueItemDto>> GetReportsAsync(
        Guid actorUserId,
        Guid? employeeId,
        bool pendingOnly,
        CancellationToken cancellationToken);

    Task<(WipReportResultDto? result, string status)> ReviewAsync(
        Guid reportId,
        Guid reviewerUserId,
        bool approve,
        string? comments,
        CancellationToken cancellationToken);

    Task<(WipReportResultDto? result, string status)> CorrectAndResubmitAsync(
        Guid reportId,
        WipReportRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<(EquipmentUsageRecordDto? result, string status)> CreateEquipmentUsageAsync(
        EquipmentUsageRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EquipmentUsageRecordDto>> GetEquipmentUsageAsync(
        Guid workOrderId,
        CancellationToken cancellationToken);
}

public class WipReportService : IWipReportService
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ErpGeneralDbContext _db;
    private readonly ICostingService _costing;

    public WipReportService(ErpGeneralDbContext db, ICostingService costing)
    {
        _db = db;
        _costing = costing;
    }

    public async Task<(WipReportResultDto? result, string status)> ReportCompletionAsync(
        WipReportRequest request,
        Guid actorUserId,
        bool canProxyReport,
        CancellationToken cancellationToken)
    {
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var actor = await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == actorUserId && user.IsActive)
            .Select(user => new { user.EmployeeId })
            .SingleOrDefaultAsync(cancellationToken);
        if (actor == null)
            return (null, "user_not_found");

        var reportType = request.ReportType?.Trim().ToLowerInvariant();
        if (reportType is not ("individual" or "team"))
            return (null, "invalid_report_type");
        if (string.IsNullOrWhiteSpace(request.ShiftClass)
            || request.ShiftClass is not ("day" or "night" or "other"))
            return (null, "invalid_shift");

        var isTeamReport = reportType == "team";
        var requestedTeamMembers = request.TeamMemberEmployeeIds ?? new List<Guid>();
        var requestedEquipmentHours = request.EquipmentMachineHours ?? new List<WipReportEquipmentHoursDto>();
        if (isTeamReport && (requestedTeamMembers.Distinct().Count() != requestedTeamMembers.Count
                || requestedTeamMembers.Contains(request.EmployeeId.GetValueOrDefault())))
            return (null, "invalid_team_members");
        if (request.MachineHours > 0 || requestedEquipmentHours.Count > 0)
            return (null, "equipment_usage_must_be_recorded_separately");
        var employeeId = isTeamReport
            ? request.EmployeeId.GetValueOrDefault() == Guid.Empty
                ? actor.EmployeeId ?? Guid.Empty
                : request.EmployeeId.GetValueOrDefault()
            : request.EmployeeId.GetValueOrDefault() == Guid.Empty
                ? actor.EmployeeId ?? Guid.Empty
                : request.EmployeeId.GetValueOrDefault();
        if (employeeId == Guid.Empty)
            return (null, "employee_mapping_required");
        var isProxy = actor.EmployeeId != employeeId;
        if (isProxy && (!canProxyReport || string.IsNullOrWhiteSpace(request.ProxyReason)))
            return (null, "proxy_not_allowed");

        var employeeIds = isTeamReport
            ? requestedTeamMembers.Append(employeeId).Distinct().ToList()
            : new List<Guid> { employeeId };
        if (isTeamReport && (employeeIds.Count < 2 || employeeIds.Contains(Guid.Empty)))
            return (null, "team_members_required");
        if (!TryGetTeamMemberHours(request, employeeIds, out var teamMemberHours))
            return (null, "invalid_team_member_hours");
        if (isTeamReport && actor.EmployeeId is Guid actorEmployeeId && !employeeIds.Contains(actorEmployeeId))
            return (null, "reporter_must_be_team_member");
        var activeEmployeeIds = await _db.Employees
            .Where(employee => employee.Status == "active" && employeeIds.Contains(employee.Id))
            .Select(employee => employee.Id)
            .ToListAsync(cancellationToken);
        if (activeEmployeeIds.Count != employeeIds.Count)
            return (null, "employee_not_available");

        var workOrder = await _db.WorkOrders
            .Include(order => order.ProductSku)
            .Include(order => order.Operations)
            .FirstOrDefaultAsync(order => order.WoNumber == request.WoNumber, cancellationToken);
        if (workOrder == null)
            return (null, "not_found");
        if (workOrder.Operations.Count == 0)
            return (null, "operation_not_found");
        if (string.Equals(workOrder.Status, "completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(workOrder.Status, "closed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(workOrder.Status, "settled", StringComparison.OrdinalIgnoreCase))
            return (null, "work_order_closed");
        if (string.Equals(workOrder.Status, "pending_fqc", StringComparison.OrdinalIgnoreCase))
            return (null, "fqc_pending");
        if (request.CompletedQty < 0 || request.DefectQty < 0 || request.ScrapQty < 0
            || request.CompletedQty + request.DefectQty + request.ScrapQty <= 0)
            return (null, "invalid_quantities");
        if ((request.DefectQty > 0 || request.ScrapQty > 0)
            && string.IsNullOrWhiteSpace(request.DefectReasonCode))
            return (null, "defect_reason_required");
        if (request.ManHours < 0 || request.MachineHours < 0 || request.SetupHours < 0 || request.RunHours < 0)
            return (null, "invalid_hours");
        var now = DateTime.UtcNow;
        var startTime = request.StartTime ?? now.AddHours(-request.ManHours);
        var endTime = request.EndTime ?? now;
        if (endTime <= startTime || endTime - startTime > TimeSpan.FromHours(24))
            return (null, "invalid_time_range");
        if (employeeIds.Count > 0 && await _db.WipReports.AnyAsync(report =>
                report.Status != "returned"
                && report.StartTime < endTime
                && report.EndTime > startTime
                && (employeeIds.Contains(report.EmployeeId)
                    || report.Participants.Any(participant => employeeIds.Contains(participant.EmployeeId))),
                cancellationToken))
            return (null, "employee_time_overlap");

        var operation = workOrder.Operations
            .FirstOrDefault(candidate => candidate.OperationSeq == request.OperationSeq);
        if (operation == null)
            return (null, "operation_not_found");
        var finalOperationSeq = workOrder.Operations.Max(candidate => candidate.OperationSeq);
        if (operation.OperationSeq == finalOperationSeq
            && !string.IsNullOrWhiteSpace(request.FinishedLotNumber)
            && await _db.FqcInspectionLots.AnyAsync(lot =>
                lot.WorkOrderId == workOrder.Id && lot.LotNumber == request.FinishedLotNumber.Trim(),
                cancellationToken))
            return (null, "fqc_lot_locked");

        var existingReports = await _db.WipReports
            .Where(report => report.WorkOrderId == workOrder.Id
                && report.OperationSeq == request.OperationSeq
                && report.Status != "returned")
            .Select(report => new { report.CompletedQty, report.DefectQty, report.ScrapQty })
            .ToListAsync(cancellationToken);
        var reservedQty = existingReports.Sum(report =>
            report.CompletedQty + report.DefectQty + report.ScrapQty);
        var reportQty = request.CompletedQty + request.DefectQty + request.ScrapQty;
        if (reservedQty + reportQty > operation.PlannedQty)
            return (null, "quantity_exceeds_plan");

        var report = new WipReport
        {
            ReportNumber = DocumentNumberGenerator.GenerateWipReport(),
            WorkOrderId = workOrder.Id,
            OperationSeq = request.OperationSeq,
            EmployeeId = employeeId,
            ReportType = reportType,
            Status = "submitted",
            SubmittedByUserId = actorUserId,
            SubmittedAt = now,
            ProxyReason = isProxy ? request.ProxyReason!.Trim() : null,
            ShiftClass = request.ShiftClass,
            CompletedQty = request.CompletedQty,
            DefectQty = request.DefectQty,
            ScrapQty = request.ScrapQty,
            ActualManHours = (decimal)request.ManHours,
            ActualMachineHours = 0,
            SetupHours = (decimal)request.SetupHours,
            RunHours = (decimal)request.RunHours,
            StartTime = startTime,
            EndTime = endTime,
            MachineCode = request.MachineCode,
            MaterialLotNumber = request.MaterialLotNumber,
            FinishedLotNumber = string.IsNullOrWhiteSpace(request.FinishedLotNumber)
                ? null
                : request.FinishedLotNumber.Trim(),
            AnomalyType = request.DefectReasonCode,
            HasAnomaly = request.DefectQty > 0 || request.ScrapQty > 0,
            Remarks = request.Remarks,
            CreatedAt = now
        };
        if (isTeamReport)
        {
            foreach (var memberId in employeeIds)
            {
                report.Participants.Add(new WipReportParticipant
                {
                    EmployeeId = memberId,
                    Role = memberId == employeeId ? "responsible" : "participant",
                    ActualManHours = teamMemberHours[memberId]
                });
            }
        }

        report.AuditEvents.Add(new WipReportAuditEvent
        {
            ActorUserId = actorUserId,
            Action = "submitted",
            Reason = isProxy ? request.ProxyReason!.Trim() : null,
            ChangesJson = JsonSerializer.Serialize(new
            {
                report.ReportType,
                report.EmployeeId,
                report.ShiftClass,
                report.CompletedQty,
                report.DefectQty,
                report.ScrapQty,
                report.ActualManHours,
                report.StartTime,
                report.EndTime,
                TeamMemberHours = teamMemberHours
            }, AuditJsonOptions),
            CreatedAt = now
        });
        _db.WipReports.Add(report);

        await _db.SaveChangesAsync(cancellationToken);

        if (transaction != null)
            await transaction.CommitAsync(cancellationToken);

        return (MapReportResult(report, workOrder, operation), "ok");
    }

    public async Task<IReadOnlyList<WipReportQueueItemDto>> GetReportsAsync(
        Guid actorUserId,
        Guid? employeeId,
        bool pendingOnly,
        CancellationToken cancellationToken)
    {
        var actorEmployeeId = employeeId ?? await _db.Users
            .Where(user => user.Id == actorUserId && user.IsActive)
            .Select(user => user.EmployeeId)
            .SingleOrDefaultAsync(cancellationToken);
        var query = _db.WipReports.AsNoTracking().AsQueryable();
        query = pendingOnly
            ? query.Where(report => report.Status == "submitted")
            : query.Where(report => report.SubmittedByUserId == actorUserId
                || (actorEmployeeId.HasValue
                    && (report.EmployeeId == actorEmployeeId.Value
                        || report.Participants.Any(participant => participant.EmployeeId == actorEmployeeId.Value))));

        return await query
            .OrderByDescending(report => report.SubmittedAt)
            .Select(report => new WipReportQueueItemDto
            {
                Id = report.Id,
                ReportNumber = report.ReportNumber,
                WorkOrderNumber = report.WorkOrder!.WoNumber,
                EmployeeId = report.EmployeeId,
                OperationSeq = report.OperationSeq,
                ProductName = report.WorkOrder.ProductSku!.SkuName,
                OperationName = _db.WorkOrderOperations
                    .Where(operation => operation.WorkOrderId == report.WorkOrderId
                        && operation.OperationSeq == report.OperationSeq)
                    .Select(operation => operation.Operation!.OperationName)
                    .FirstOrDefault() ?? string.Empty,
                EmployeeName = _db.Employees
                    .Where(employee => employee.Id == report.EmployeeId)
                    .Select(employee => employee.ChineseName)
                    .FirstOrDefault() ?? string.Empty,
                ReportType = report.ReportType,
                ShiftClass = report.ShiftClass ?? string.Empty,
                CompletedQty = report.CompletedQty,
                DefectQty = report.DefectQty,
                ScrapQty = report.ScrapQty,
                ManHours = report.ActualManHours,
                SetupHours = report.SetupHours,
                RunHours = report.RunHours,
                StartTime = report.StartTime,
                EndTime = report.EndTime,
                SubmittedAt = report.SubmittedAt,
                Status = report.Status,
                ProxyReason = report.ProxyReason,
                ReviewComments = report.ReviewComments,
                DefectReasonCode = report.AnomalyType,
                MaterialLotNumber = report.MaterialLotNumber,
                FinishedLotNumber = report.FinishedLotNumber,
                Remarks = report.Remarks,
                TeamMemberEmployeeIds = report.Participants
                    .Where(participant => participant.Role != "responsible")
                    .Select(participant => participant.EmployeeId)
                    .ToList(),
                TeamMemberHours = report.Participants
                    .Select(participant => new WipReportParticipantHoursDto
                    {
                        EmployeeId = participant.EmployeeId,
                        ManHours = participant.ActualManHours
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(WipReportResultDto? result, string status)> ReviewAsync(
        Guid reportId,
        Guid reviewerUserId,
        bool approve,
        string? comments,
        CancellationToken cancellationToken)
    {
        if (!approve && string.IsNullOrWhiteSpace(comments))
            return (null, "review_reason_required");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var report = await _db.WipReports
            .Include(item => item.WorkOrder)
            .ThenInclude(order => order!.Operations)
            .Include(item => item.Participants)
            .FirstOrDefaultAsync(item => item.Id == reportId, cancellationToken);
        if (report == null)
            return (null, "report_not_found");
        if (report.Status != "submitted")
            return (null, "report_not_pending");
        if (report.SubmittedByUserId == reviewerUserId)
            return (null, "cannot_review_own_report");
        var claimedReport = await _db.WipReports
            .Where(item => item.Id == reportId && item.Status == "submitted")
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(item => item.Status, "reviewing"),
                cancellationToken);
        if (claimedReport != 1)
            return (null, "report_not_pending");

        var workOrder = report.WorkOrder!;
        var operation = workOrder.Operations.FirstOrDefault(item => item.OperationSeq == report.OperationSeq);
        if (operation == null)
            return (null, "operation_not_found");

        var now = DateTime.UtcNow;
        report.ReviewedByUserId = reviewerUserId;
        report.ReviewedAt = now;
        report.ReviewComments = comments?.Trim();
        report.Status = approve ? "approved" : "returned";
        _db.WipReportAuditEvents.Add(new WipReportAuditEvent
        {
            WipReportId = report.Id,
            ActorUserId = reviewerUserId,
            Action = approve ? "approved" : "returned",
            Reason = comments?.Trim(),
            CreatedAt = now
        });

        if (approve)
        {
            var finalOperationSeq = workOrder.Operations.Max(item => item.OperationSeq);
            if (report.OperationSeq == finalOperationSeq
                && !string.IsNullOrWhiteSpace(report.FinishedLotNumber)
                && await _db.FqcInspectionLots.AnyAsync(lot =>
                    lot.WorkOrderId == workOrder.Id && lot.LotNumber == report.FinishedLotNumber,
                    cancellationToken))
                return (null, "fqc_lot_locked");

            var approvedReports = await _db.WipReports
                .Where(item => item.WorkOrderId == workOrder.Id
                    && item.OperationSeq == report.OperationSeq
                    && item.Status == "approved"
                    && item.Id != report.Id)
                .Select(item => new { item.CompletedQty, item.DefectQty, item.ScrapQty })
                .ToListAsync(cancellationToken);
            var approvedCompletedQty = approvedReports.Sum(item => item.CompletedQty) + report.CompletedQty;
            var approvedScrapQty = approvedReports.Sum(item => item.ScrapQty) + report.ScrapQty;
            var approvedQty = approvedReports.Sum(item => item.CompletedQty + item.DefectQty + item.ScrapQty)
                + report.CompletedQty + report.DefectQty + report.ScrapQty;
            if (approvedQty > operation.PlannedQty)
                return (null, "quantity_exceeds_plan");

            operation.CompletedQty = approvedCompletedQty;
            operation.ScrapQty = approvedScrapQty;
            operation.Status = approvedQty >= operation.PlannedQty ? "completed" : "in_progress";
            if (workOrder.ActualStartDate == null)
            {
                workOrder.ActualStartDate = report.StartTime ?? now;
                workOrder.Status = "in_progress";
            }

            var finalOperation = workOrder.Operations.OrderByDescending(item => item.OperationSeq).First();
            if (operation.Id == finalOperation.Id)
            {
                workOrder.ActualQty = operation.CompletedQty;
                workOrder.ScrapQty = operation.ScrapQty;
                if (operation.CompletedQty >= workOrder.PlannedQty)
                {
                    var isRework = string.Equals(workOrder.WoType, "rework", StringComparison.OrdinalIgnoreCase);
                    workOrder.Status = isRework ? "pending_fqc" : "completed";
                    if (!isRework)
                        workOrder.ActualEndDate = report.EndTime ?? now;
                }
            }
            workOrder.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (approve)
            await RecordLaborCostAsync(report, operation, now, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        if (transaction != null)
            await transaction.CommitAsync(cancellationToken);

        return (MapReportResult(report, workOrder, operation), "ok");
    }

    public async Task<(WipReportResultDto? result, string status)> CorrectAndResubmitAsync(
        Guid reportId,
        WipReportRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.TeamMemberEmployeeIds == null)
            return (null, "invalid_team_members");
        if (request.MachineHours > 0 || request.EquipmentMachineHours?.Count > 0)
            return (null, "equipment_usage_must_be_recorded_separately");
        if (request.ShiftClass is not ("day" or "night" or "other"))
            return (null, "invalid_shift");
        if (request.CompletedQty < 0 || request.DefectQty < 0 || request.ScrapQty < 0
            || request.CompletedQty + request.DefectQty + request.ScrapQty <= 0)
            return (null, "invalid_quantities");
        if ((request.DefectQty > 0 || request.ScrapQty > 0)
            && string.IsNullOrWhiteSpace(request.DefectReasonCode))
            return (null, "defect_reason_required");
        if (request.ManHours < 0 || request.SetupHours < 0 || request.RunHours < 0)
            return (null, "invalid_hours");

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var report = await _db.WipReports
            .Include(item => item.WorkOrder)
            .ThenInclude(order => order!.Operations)
            .Include(item => item.Participants)
            .FirstOrDefaultAsync(item => item.Id == reportId, cancellationToken);
        if (report == null)
            return (null, "report_not_found");
        if (report.SubmittedByUserId != actorUserId)
            return (null, "report_not_owned");
        if (report.Status != "returned")
            return (null, "report_not_returned");
        if (!string.Equals(report.ReportType, request.ReportType, StringComparison.OrdinalIgnoreCase))
            return (null, "report_type_immutable");
        if (!string.Equals(report.WorkOrder?.WoNumber, request.WoNumber, StringComparison.Ordinal)
            || report.OperationSeq != request.OperationSeq)
            return (null, "report_scope_immutable");

        var workOrder = report.WorkOrder!;
        var operation = workOrder.Operations.FirstOrDefault(item => item.OperationSeq == report.OperationSeq);
        if (operation == null)
            return (null, "operation_not_found");

        var employeeIds = report.ReportType == "team"
            ? request.TeamMemberEmployeeIds.Append(report.EmployeeId).Distinct().ToList()
            : new List<Guid> { report.EmployeeId };
        if (report.ReportType == "team"
            && (request.TeamMemberEmployeeIds.Distinct().Count() != request.TeamMemberEmployeeIds.Count
                || request.TeamMemberEmployeeIds.Contains(report.EmployeeId)
                || employeeIds.Count < 2))
            return (null, "invalid_team_members");
        if (!TryGetTeamMemberHours(request, employeeIds, out var teamMemberHours))
            return (null, "invalid_team_member_hours");
        var actorEmployeeId = await _db.Users
            .Where(user => user.Id == actorUserId && user.IsActive)
            .Select(user => user.EmployeeId)
            .SingleOrDefaultAsync(cancellationToken);
        if (report.ReportType == "team" && actorEmployeeId is Guid memberId && !employeeIds.Contains(memberId))
            return (null, "reporter_must_be_team_member");
        if (report.ReportType == "team")
        {
            var activeEmployeeIds = await _db.Employees
                .Where(employee => employee.Status == "active" && employeeIds.Contains(employee.Id))
                .Select(employee => employee.Id)
                .ToListAsync(cancellationToken);
            if (activeEmployeeIds.Count != employeeIds.Count)
                return (null, "employee_not_available");
        }

        var now = DateTime.UtcNow;
        var startTime = request.StartTime ?? now.AddHours(-request.ManHours);
        var endTime = request.EndTime ?? now;
        if (endTime <= startTime || endTime - startTime > TimeSpan.FromHours(24))
            return (null, "invalid_time_range");
        if (await _db.WipReports.AnyAsync(item =>
                item.Id != report.Id
                && item.Status != "returned"
                && item.StartTime < endTime
                && item.EndTime > startTime
                && (employeeIds.Contains(item.EmployeeId)
                    || item.Participants.Any(participant => employeeIds.Contains(participant.EmployeeId))),
                cancellationToken))
            return (null, "employee_time_overlap");

        var finalOperationSeq = workOrder.Operations.Max(item => item.OperationSeq);
        var finishedLotNumber = string.IsNullOrWhiteSpace(request.FinishedLotNumber)
            ? null
            : request.FinishedLotNumber.Trim();
        if (report.OperationSeq == finalOperationSeq
            && finishedLotNumber != null
            && await _db.FqcInspectionLots.AnyAsync(lot =>
                lot.WorkOrderId == workOrder.Id && lot.LotNumber == finishedLotNumber,
                cancellationToken))
            return (null, "fqc_lot_locked");

        var reservedReports = await _db.WipReports
            .Where(item => item.WorkOrderId == workOrder.Id
                && item.OperationSeq == report.OperationSeq
                && item.Id != report.Id
                && item.Status != "returned")
            .Select(item => new { item.CompletedQty, item.DefectQty, item.ScrapQty })
            .ToListAsync(cancellationToken);
        var reservedQty = reservedReports.Sum(item => item.CompletedQty + item.DefectQty + item.ScrapQty)
            + request.CompletedQty + request.DefectQty + request.ScrapQty;
        if (reservedQty > operation.PlannedQty)
            return (null, "quantity_exceeds_plan");

        var previousValues = JsonSerializer.Serialize(new
        {
            report.ReportType,
            report.ShiftClass,
            report.CompletedQty,
            report.DefectQty,
            report.ScrapQty,
            report.ActualManHours,
            report.SetupHours,
            report.RunHours,
            report.StartTime,
            report.EndTime,
            report.MaterialLotNumber,
            report.FinishedLotNumber,
            report.AnomalyType,
            report.Remarks,
            Participants = report.Participants.Select(participant => new
            {
                participant.EmployeeId,
                participant.ActualManHours
            })
        }, AuditJsonOptions);
        report.ShiftClass = request.ShiftClass;
        report.CompletedQty = request.CompletedQty;
        report.DefectQty = request.DefectQty;
        report.ScrapQty = request.ScrapQty;
        report.ActualManHours = (decimal)request.ManHours;
        report.ActualMachineHours = 0;
        report.SetupHours = (decimal)request.SetupHours;
        report.RunHours = (decimal)request.RunHours;
        report.StartTime = startTime;
        report.EndTime = endTime;
        report.MaterialLotNumber = request.MaterialLotNumber;
        report.FinishedLotNumber = finishedLotNumber;
        report.AnomalyType = request.DefectReasonCode;
        report.HasAnomaly = request.DefectQty > 0 || request.ScrapQty > 0;
        report.Remarks = request.Remarks;
        report.Status = "submitted";
        report.SubmittedAt = now;
        report.ReviewedByUserId = null;
        report.ReviewedAt = null;
        report.ReviewComments = null;
        if (report.ReportType == "team")
        {
            _db.WipReportParticipants.RemoveRange(report.Participants);
            _db.WipReportParticipants.AddRange(employeeIds.Select(member => new WipReportParticipant
                {
                    WipReportId = report.Id,
                    EmployeeId = member,
                    Role = member == report.EmployeeId ? "responsible" : "participant",
                    ActualManHours = teamMemberHours[member]
                }));
        }
        _db.WipReportAuditEvents.Add(new WipReportAuditEvent
        {
            WipReportId = report.Id,
            ActorUserId = actorUserId,
            Action = "resubmitted",
            ChangesJson = JsonSerializer.Serialize(new
            {
                Previous = JsonSerializer.Deserialize<JsonElement>(previousValues),
                report.ShiftClass,
                report.CompletedQty,
                report.DefectQty,
                report.ScrapQty,
                report.ActualManHours,
                report.SetupHours,
                report.RunHours,
                report.StartTime,
                report.EndTime,
                report.MaterialLotNumber,
                report.FinishedLotNumber,
                report.AnomalyType,
                report.Remarks,
                Participants = teamMemberHours
            }, AuditJsonOptions),
            CreatedAt = now
        });

        await _db.SaveChangesAsync(cancellationToken);
        if (transaction != null)
            await transaction.CommitAsync(cancellationToken);
        return (MapReportResult(report, workOrder, operation), "ok");
    }

    public async Task<(EquipmentUsageRecordDto? result, string status)> CreateEquipmentUsageAsync(
        EquipmentUsageRequestDto request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (request.MachineHours <= 0 || request.StartTime >= request.EndTime)
            return (null, "invalid_equipment_usage");
        if (request.ShiftClass is not ("day" or "night" or "other"))
            return (null, "invalid_shift");

        var actor = await _db.Users
            .Where(user => user.Id == actorUserId && user.IsActive)
            .Select(user => new { user.EmployeeId })
            .SingleOrDefaultAsync(cancellationToken);
        if (actor?.EmployeeId is not Guid employeeId)
            return (null, "employee_mapping_required");

        var workOrder = await _db.WorkOrders
            .Include(order => order.Operations)
            .FirstOrDefaultAsync(order => order.Id == request.WorkOrderId, cancellationToken);
        if (workOrder == null)
            return (null, "not_found");
        var operation = workOrder.Operations.FirstOrDefault(item => item.OperationSeq == request.OperationSeq);
        if (operation == null)
            return (null, "operation_not_found");
        var equipment = await _db.Equipments.FirstOrDefaultAsync(item =>
            item.Id == request.EquipmentId && item.IsActive && item.Status == "active",
            cancellationToken);
        if (equipment == null)
            return (null, "equipment_not_available");
        if (await _db.EquipmentUsageRecords.AnyAsync(usage =>
                usage.EquipmentId == request.EquipmentId
                && usage.StartTime < request.EndTime
                && usage.EndTime > request.StartTime,
                cancellationToken))
            return (null, "equipment_time_overlap");

        var now = DateTime.UtcNow;
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var usageRecord = new EquipmentUsageRecord
        {
            WorkOrderId = workOrder.Id,
            OperationSeq = request.OperationSeq,
            EquipmentId = equipment.Id,
            RecordedByEmployeeId = employeeId,
            RecordedByUserId = actorUserId,
            ShiftClass = request.ShiftClass,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            MachineHours = request.MachineHours,
            Remarks = request.Remarks?.Trim(),
            CreatedAt = now
        };
        _db.EquipmentUsageRecords.Add(usageRecord);
        await _db.SaveChangesAsync(cancellationToken);
        await RecordMachineCostsAsync(
            operation,
            new[] { usageRecord },
            now,
            cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        if (transaction != null)
            await transaction.CommitAsync(cancellationToken);

        return (new EquipmentUsageRecordDto
        {
            Id = usageRecord.Id,
            WorkOrderId = workOrder.Id,
            WorkOrderNumber = workOrder.WoNumber,
            OperationSeq = request.OperationSeq,
            EquipmentId = equipment.Id,
            EquipmentCode = equipment.EquipmentCode,
            EquipmentName = equipment.EquipmentName,
            RecordedByEmployeeName = await _db.Employees
                .Where(item => item.Id == employeeId)
                .Select(item => item.ChineseName)
                .FirstAsync(cancellationToken),
            ShiftClass = usageRecord.ShiftClass,
            StartTime = usageRecord.StartTime,
            EndTime = usageRecord.EndTime,
            MachineHours = usageRecord.MachineHours,
            Remarks = usageRecord.Remarks
        }, "ok");
    }

    public async Task<IReadOnlyList<EquipmentUsageRecordDto>> GetEquipmentUsageAsync(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        return await _db.EquipmentUsageRecords
            .AsNoTracking()
            .Where(usage => usage.WorkOrderId == workOrderId)
            .OrderByDescending(usage => usage.StartTime)
            .Select(usage => new EquipmentUsageRecordDto
            {
                Id = usage.Id,
                WorkOrderId = usage.WorkOrderId,
                WorkOrderNumber = usage.WorkOrder.WoNumber,
                OperationSeq = usage.OperationSeq,
                EquipmentId = usage.EquipmentId,
                EquipmentCode = usage.Equipment.EquipmentCode,
                EquipmentName = usage.Equipment.EquipmentName,
                RecordedByEmployeeName = usage.RecordedByEmployee.ChineseName,
                ShiftClass = usage.ShiftClass,
                StartTime = usage.StartTime,
                EndTime = usage.EndTime,
                MachineHours = usage.MachineHours,
                Remarks = usage.Remarks
            })
            .ToListAsync(cancellationToken);
    }

    private async Task RecordLaborCostAsync(
        WipReport report,
        WorkOrderOperation operation,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (report.ActualManHours <= 0)
            return;
        var rate = await _db.WorkCenterCosts
            .Where(cost => cost.WorkCenterId == operation.WorkCenterId
                && cost.EffectiveFrom <= now
                && (cost.EffectiveTo == null || cost.EffectiveTo >= now))
            .OrderByDescending(cost => cost.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        if (rate == null)
            return;

        if (report.ReportType == "team")
        {
            foreach (var participant in report.Participants.Where(item => item.ActualManHours > 0))
            {
                await _costing.RecordLaborActualAsync(
                    report.WorkOrderId,
                    report.Id,
                    report.OperationSeq,
                    participant.ActualManHours,
                    rate.LaborRate,
                    participant.EmployeeId);
            }
            return;
        }

        await _costing.RecordLaborActualAsync(
            report.WorkOrderId,
            report.Id,
            report.OperationSeq,
            report.ActualManHours,
            rate.LaborRate,
            report.EmployeeId);
    }

    private async Task RecordMachineCostsAsync(
        WorkOrderOperation operation,
        IEnumerable<EquipmentUsageRecord> equipmentUsageRecords,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var rate = await _db.WorkCenterCosts
            .Where(cost => cost.WorkCenterId == operation.WorkCenterId
                && cost.EffectiveFrom <= now
                && (cost.EffectiveTo == null || cost.EffectiveTo >= now))
            .OrderByDescending(cost => cost.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
        if (rate == null)
            return;

        foreach (var usageRecord in equipmentUsageRecords)
        {
            _db.WorkOrderMachineActuals.Add(new WorkOrderMachineActual
            {
                WorkOrderId = usageRecord.WorkOrderId,
                EquipmentUsageRecordId = usageRecord.Id,
                OperationSeq = usageRecord.OperationSeq,
                WorkCenterId = operation.WorkCenterId,
                ActualHours = usageRecord.MachineHours,
                MachineRate = rate.MachineRate,
                OverheadRate = rate.OverheadRate,
                OverheadAmount = usageRecord.MachineHours * rate.OverheadRate,
                TotalAmount = usageRecord.MachineHours * rate.MachineRate,
                CreatedAt = now
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static WipReportResultDto MapReportResult(
        WipReport report,
        WorkOrder workOrder,
        WorkOrderOperation operation) => new()
    {
        Id = report.Id,
        ReportNumber = report.ReportNumber,
        WorkOrderNumber = workOrder.WoNumber,
        OperationSeq = operation.OperationSeq,
        OperationCompletedQty = operation.CompletedQty,
        OperationPlannedQty = operation.PlannedQty,
        WorkOrderCompletedQty = workOrder.ActualQty,
        WorkOrderPlannedQty = workOrder.PlannedQty,
        Status = report.Status
    };

    private static bool TryGetTeamMemberHours(
        WipReportRequest request,
        IReadOnlyCollection<Guid> employeeIds,
        out Dictionary<Guid, decimal> hoursByEmployeeId)
    {
        hoursByEmployeeId = new Dictionary<Guid, decimal>();
        var suppliedHours = request.TeamMemberHours ?? new List<WipReportParticipantHoursDto>();
        if (!string.Equals(request.ReportType, "team", StringComparison.OrdinalIgnoreCase))
            return suppliedHours.Count == 0;
        if (suppliedHours.Any(item => item.EmployeeId == Guid.Empty || item.ManHours < 0)
            || suppliedHours.Select(item => item.EmployeeId).Distinct().Count() != suppliedHours.Count
            || !employeeIds.ToHashSet().SetEquals(suppliedHours.Select(item => item.EmployeeId)))
            return false;

        hoursByEmployeeId = suppliedHours.ToDictionary(item => item.EmployeeId, item => item.ManHours);
        var totalMemberHours = hoursByEmployeeId.Values.Sum();
        return Math.Abs(totalMemberHours - (decimal)request.ManHours) <= 0.01m;
    }
}
