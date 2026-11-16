using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class CostCenterReportDto
{
    public Guid CostCenterId { get; set; }
    public string CostCenterCode { get; set; } = null!;
    public string CostCenterName { get; set; } = null!;
    public string CostCenterType { get; set; } = null!;
    public decimal? TotalBudget { get; set; }
    public decimal TotalUsed { get; set; }
    public Dictionary<string, decimal> CostByElement { get; set; } = new();
    public Dictionary<string, decimal> CostByActivity { get; set; } = new();
}

public class CreateWorkCenterCostRequest
{
    public Guid WorkCenterId { get; set; }
    public Guid CostCenterId { get; set; }
    public decimal LaborRate { get; set; }
    public decimal OverheadRate { get; set; }
    public decimal MachineRate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class WorkCenterCostDto
{
    public Guid Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public string? WorkCenterName { get; set; }
    public Guid CostCenterId { get; set; }
    public string? CostCenterCode { get; set; }
    public string? CostCenterName { get; set; }
    public decimal LaborRate { get; set; }
    public decimal OverheadRate { get; set; }
    public decimal MachineRate { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class CostElementDto
{
    public Guid Id { get; set; }
    public string ElementCode { get; set; } = null!;
    public string ElementName { get; set; } = null!;
    public string ElementType { get; set; } = null!;
    public string? GLAccountCode { get; set; }
    public string? GLAccountName { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateCostElementRequest
{
    public string ElementCode { get; set; } = null!;
    public string ElementName { get; set; } = null!;
    public string? ElementType { get; set; }
    public string? GLAccountCode { get; set; }
    public string? GLAccountName { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateCostElementRequest
{
    public string? ElementName { get; set; }
    public string? ElementType { get; set; }
    public string? GLAccountCode { get; set; }
    public string? GLAccountName { get; set; }
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class CreateWorkOrderCostVarianceAllocationRequestDto
{
    public string SourceType { get; set; } = null!;
    public Guid SourceId { get; set; }
    public decimal Amount { get; set; }
    public string ReasonCode { get; set; } = null!;
    public string Reason { get; set; } = null!;
}

public sealed class ApproveWorkOrderCostVarianceAllocationRequestDto
{
    public string ApprovalRole { get; set; } = null!;
    public string? Comments { get; set; }
}

public sealed class WorkOrderCostVarianceAllocationDto
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public string SourceType { get; set; } = null!;
    public Guid SourceId { get; set; }
    public decimal Amount { get; set; }
    public string ReasonCode { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public List<WorkOrderCostVarianceApprovalDto> Approvals { get; set; } = new();
}

public sealed class WorkOrderCostVarianceApprovalDto
{
    public string ApprovalRole { get; set; } = null!;
    public Guid ApproverId { get; set; }
    public string? Comments { get; set; }
    public DateTime ApprovedAt { get; set; }
}
