using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ErpBackend.Data;
using ErpBackend.DTOs;
using ErpBackend.Models.Finance;
using ErpBackend.Models.MES;

namespace ErpBackend.Services;

public interface IWorkOrderCostVarianceService
{
    Task<WorkOrderCostVarianceAllocationDto> CreateAsync(
        Guid workOrderId,
        CreateWorkOrderCostVarianceAllocationRequestDto request,
        Guid actorId,
        CancellationToken cancellationToken);

    Task<WorkOrderCostVarianceAllocationDto> ApproveAsync(
        Guid workOrderId,
        Guid allocationId,
        ApproveWorkOrderCostVarianceAllocationRequestDto request,
        Guid actorId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken cancellationToken);

    Task<List<WorkOrderCostVarianceAllocationDto>> GetAllocationsAsync(
        Guid workOrderId,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<WorkOrderCostVarianceAllocation> ApprovedAllocations, bool HasPendingApproval)> GetSettlementAsync(
        Guid workOrderId,
        CancellationToken cancellationToken);
}

public sealed class WorkOrderCostVarianceService : IWorkOrderCostVarianceService
{
    private readonly ErpGeneralDbContext _db;
    private readonly AccountingPolicyOptions _policy;

    public WorkOrderCostVarianceService(
        ErpGeneralDbContext db,
        IOptions<AccountingPolicyOptions> policy)
    {
        _db = db;
        _policy = policy.Value;
        if (_policy.ApprovalThresholdTwd <= 0 ||
            string.IsNullOrWhiteSpace(_policy.DepartmentManagerRole) ||
            string.IsNullOrWhiteSpace(_policy.CfoRole) ||
            string.IsNullOrWhiteSpace(_policy.GeneralManagerRole) ||
            new[]
            {
                _policy.DepartmentManagerRole,
                _policy.CfoRole,
                _policy.GeneralManagerRole
            }.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
            throw new InvalidOperationException("工單成本差異簽核政策設定無效");
    }

    public async Task<WorkOrderCostVarianceAllocationDto> CreateAsync(
        Guid workOrderId,
        CreateWorkOrderCostVarianceAllocationRequestDto request,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorId == Guid.Empty)
            throw new ArgumentException("無效的建立者身分", nameof(actorId));
        if (workOrderId == Guid.Empty || request.SourceId == Guid.Empty)
            throw new ArgumentException("必須指定工單與實際成本來源");
        if (request.Amount <= 0 || decimal.Round(request.Amount, 2) != request.Amount)
            throw new ArgumentException("差異分攤金額必須大於零且最多兩位小數");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
            throw new ArgumentException("差異原因必須為 1 至 500 個字元");

        var sourceType = NormalizeSourceType(request.SourceType);
        var reasonCode = NormalizeReasonCode(request.ReasonCode);
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;

        var workOrder = await LockWorkOrderAsync(workOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到工單");
        if (!workOrder.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("只有已完工且尚未結算的工單可建立成本差異分攤");
        if (await _db.WorkOrderCosts.AnyAsync(
                cost => cost.WorkOrderId == workOrderId &&
                        cost.Status.ToUpper() == "SETTLED",
                cancellationToken))
            throw new InvalidOperationException("工單成本已結算，不能新增差異分攤");
        if (await _db.WorkOrderCostVarianceAllocations.AnyAsync(
                allocation => allocation.WorkOrderId == workOrderId &&
                              allocation.Status.ToUpper() == "APPROVED",
                cancellationToken))
            throw new InvalidOperationException("工單已有差異分攤完成簽核，不能再追加分攤");

        var sourceCost = await GetSourceCostAsync(workOrderId, sourceType, request.SourceId, cancellationToken);
        var allocatedAmount = await _db.WorkOrderCostVarianceAllocations
            .Where(allocation => allocation.SourceType.ToLower() == sourceType &&
                                 allocation.SourceId == request.SourceId &&
                                 allocation.Status.ToUpper() != "REJECTED")
            .SumAsync(allocation => (decimal?)allocation.Amount, cancellationToken) ?? 0m;
        if (allocatedAmount + request.Amount > sourceCost)
            throw new ArgumentException("異常成本分攤總額不可超過來源實際成本");

        var allocation = new WorkOrderCostVarianceAllocation
        {
            WorkOrderId = workOrderId,
            SourceType = sourceType,
            SourceId = request.SourceId,
            Amount = request.Amount,
            ReasonCode = reasonCode,
            Reason = request.Reason.Trim(),
            CreatedBy = actorId,
            Status = "PendingApproval",
            CreatedAt = DateTime.UtcNow
        };
        _db.WorkOrderCostVarianceAllocations.Add(allocation);
        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return Map(allocation);
    }

    public async Task<WorkOrderCostVarianceAllocationDto> ApproveAsync(
        Guid workOrderId,
        Guid allocationId,
        ApproveWorkOrderCostVarianceAllocationRequestDto request,
        Guid actorId,
        IReadOnlyCollection<string> actorRoles,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (actorId == Guid.Empty)
            throw new ArgumentException("無效的簽核者身分", nameof(actorId));
        if (request.Comments?.Length > 500)
            throw new ArgumentException("簽核備註不可超過 500 個字元", nameof(request.Comments));

        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            : null;
        var workOrder = await LockWorkOrderAsync(workOrderId, cancellationToken)
            ?? throw new KeyNotFoundException("找不到工單");
        var allocation = await _db.WorkOrderCostVarianceAllocations
            .Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == allocationId &&
                                          item.WorkOrderId == workOrderId,
                cancellationToken)
            ?? throw new KeyNotFoundException("找不到工單成本差異分攤");
        if (!workOrder.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("只有尚未結算的已完工工單可簽核差異分攤");
        if (!allocation.Status.Equals("PendingApproval", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("只有待簽核的差異分攤可以簽核");
        if (allocation.CreatedBy == actorId ||
            allocation.Approvals.Any(approval => approval.ApproverId == actorId))
            throw new InvalidOperationException("建立者不得簽核，且同一人不可完成多個簽核角色");

        var approvalAmount = await _db.WorkOrderCostVarianceAllocations
            .Where(item => item.WorkOrderId == workOrderId && item.Status.ToUpper() != "REJECTED")
            .SumAsync(item => item.Amount, cancellationToken);
        var approvalRole = ResolveApprovalRole(approvalAmount, request.ApprovalRole);
        if (approvalRole is null ||
            !actorRoles.Contains(approvalRole, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("目前帳號不具備此差異分攤所需的簽核角色");
        if (allocation.Approvals.Any(approval =>
                approval.ApprovalRole.Equals(approvalRole, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("此簽核角色已完成簽核");

        var approval = new WorkOrderCostVarianceAllocationApproval
        {
            AllocationId = allocation.Id,
            ApprovalRole = approvalRole,
            ApproverId = actorId,
            Comments = string.IsNullOrWhiteSpace(request.Comments) ? null : request.Comments.Trim(),
            ApprovedAt = DateTime.UtcNow
        };
        _db.WorkOrderCostVarianceAllocationApprovals.Add(approval);
        var approvedRoles = allocation.Approvals
            .Select(approval => approval.ApprovalRole)
            .Append(approvalRole)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isSmallAmount = approvalAmount <= _policy.ApprovalThresholdTwd;
        var isApproved = isSmallAmount
            ? approvedRoles.Contains(_policy.DepartmentManagerRole)
            : approvedRoles.Contains(_policy.CfoRole) &&
              approvedRoles.Contains(_policy.GeneralManagerRole);
        if (isApproved)
        {
            allocation.Status = "Approved";
            allocation.ApprovedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return Map(allocation);
    }

    public async Task<List<WorkOrderCostVarianceAllocationDto>> GetAllocationsAsync(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        if (!await _db.WorkOrders.AnyAsync(workOrder => workOrder.Id == workOrderId, cancellationToken))
            throw new KeyNotFoundException("找不到工單");
        var allocations = await _db.WorkOrderCostVarianceAllocations
            .AsNoTracking()
            .Include(allocation => allocation.Approvals)
            .Where(allocation => allocation.WorkOrderId == workOrderId)
            .OrderBy(allocation => allocation.CreatedAt)
            .ToListAsync(cancellationToken);
        return allocations.Select(Map).ToList();
    }

    public async Task<(IReadOnlyList<WorkOrderCostVarianceAllocation> ApprovedAllocations, bool HasPendingApproval)> GetSettlementAsync(
        Guid workOrderId,
        CancellationToken cancellationToken)
    {
        var allocations = await _db.WorkOrderCostVarianceAllocations
            .Include(allocation => allocation.Approvals)
            .Where(allocation => allocation.WorkOrderId == workOrderId)
            .ToListAsync(cancellationToken);
        foreach (var sourceGroup in allocations.GroupBy(allocation =>
                     new { SourceType = allocation.SourceType.ToLower(), allocation.SourceId }))
        {
            var sourceCost = await GetSourceCostAsync(
                workOrderId, sourceGroup.Key.SourceType, sourceGroup.Key.SourceId, cancellationToken);
            var sourceAllocations = sourceGroup
                .Where(item => !item.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                .Sum(item => item.Amount);
            if (sourceAllocations > sourceCost)
                throw new InvalidOperationException("工單差異分攤已超過來源實際成本，請先調整資料");
        }

        return (
            allocations.Where(item =>
                item.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)).ToList(),
            allocations.Any(item =>
                !item.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) &&
                !item.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase)));
    }

    private async Task<WorkOrder?> LockWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken)
    {
        if (_db.Database.IsRelational() &&
            _db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            var locked = await _db.WorkOrders
                .FromSqlInterpolated($"SELECT * FROM \"WorkOrders\" WHERE \"Id\" = {workOrderId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (locked is not null)
                await _db.Entry(locked).ReloadAsync(cancellationToken);
            return locked;
        }

        return await _db.WorkOrders
            .SingleOrDefaultAsync(workOrder => workOrder.Id == workOrderId, cancellationToken);
    }

    private async Task<decimal> GetSourceCostAsync(
        Guid workOrderId,
        string sourceType,
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        decimal? amount = sourceType switch
        {
            "material" => await _db.WorkOrderMaterialActuals
                .Where(actual => actual.Id == sourceId && actual.WorkOrderId == workOrderId)
                .Select(actual => (decimal?)actual.TotalAmount)
                .SingleOrDefaultAsync(cancellationToken),
            "labor" => await _db.WorkOrderLaborActuals
                .Where(actual => actual.Id == sourceId && actual.WorkOrderId == workOrderId)
                .Select(actual => (decimal?)actual.TotalAmount)
                .SingleOrDefaultAsync(cancellationToken),
            "machine" => await _db.WorkOrderMachineActuals
                .Where(actual => actual.Id == sourceId && actual.WorkOrderId == workOrderId)
                .Select(actual => (decimal?)actual.TotalAmount)
                .SingleOrDefaultAsync(cancellationToken),
            "overhead" => await _db.WorkOrderMachineActuals
                .Where(actual => actual.Id == sourceId && actual.WorkOrderId == workOrderId)
                .Select(actual => (decimal?)actual.OverheadAmount)
                .SingleOrDefaultAsync(cancellationToken),
            _ => throw new ArgumentException("不支援的實際成本來源類型", nameof(sourceType))
        };
        if (!amount.HasValue || amount.Value <= 0)
            throw new KeyNotFoundException("找不到有效的工單實際成本來源");
        return amount.Value;
    }

    private string? ResolveApprovalRole(decimal amount, string? requestedRole)
    {
        if (string.IsNullOrWhiteSpace(requestedRole))
            return null;
        if (amount <= _policy.ApprovalThresholdTwd &&
            requestedRole.Equals(_policy.DepartmentManagerRole, StringComparison.OrdinalIgnoreCase))
            return _policy.DepartmentManagerRole;
        if (amount > _policy.ApprovalThresholdTwd &&
            requestedRole.Equals(_policy.CfoRole, StringComparison.OrdinalIgnoreCase))
            return _policy.CfoRole;
        if (amount > _policy.ApprovalThresholdTwd &&
            requestedRole.Equals(_policy.GeneralManagerRole, StringComparison.OrdinalIgnoreCase))
            return _policy.GeneralManagerRole;
        return null;
    }

    private static string NormalizeSourceType(string? sourceType) =>
        sourceType?.Trim().ToLowerInvariant() switch
        {
            "material" => "material",
            "labor" => "labor",
            "machine" => "machine",
            "overhead" => "overhead",
            _ => throw new ArgumentException("成本來源類型必須為 material、labor、machine 或 overhead")
        };

    private static string NormalizeReasonCode(string? reasonCode) =>
        reasonCode?.Trim().ToLowerInvariant() switch
        {
            "engineering_change" => "engineering_change",
            "production_anomaly" => "production_anomaly",
            _ => throw new ArgumentException("差異原因代碼必須為 engineering_change 或 production_anomaly")
        };

    private static WorkOrderCostVarianceAllocationDto Map(
        WorkOrderCostVarianceAllocation allocation) =>
        new()
        {
            Id = allocation.Id,
            WorkOrderId = allocation.WorkOrderId,
            SourceType = allocation.SourceType,
            SourceId = allocation.SourceId,
            Amount = allocation.Amount,
            ReasonCode = allocation.ReasonCode,
            Reason = allocation.Reason,
            Status = allocation.Status,
            CreatedBy = allocation.CreatedBy,
            CreatedAt = allocation.CreatedAt,
            ApprovedAt = allocation.ApprovedAt,
            Approvals = allocation.Approvals
                .OrderBy(approval => approval.ApprovedAt)
                .Select(approval => new WorkOrderCostVarianceApprovalDto
                {
                    ApprovalRole = approval.ApprovalRole,
                    ApproverId = approval.ApproverId,
                    Comments = approval.Comments,
                    ApprovedAt = approval.ApprovedAt
                })
                .ToList()
        };
}
