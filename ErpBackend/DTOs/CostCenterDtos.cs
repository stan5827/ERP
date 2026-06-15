using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class CostCenterDto
{
    public Guid? Id { get; set; }
    [Required, MaxLength(20)] public string CostCenterCode { get; set; } = null!;
    [Required, MaxLength(100)] public string CostCenterName { get; set; } = null!;
    [MaxLength(20)] public string CostCenterType { get; set; } = "production";
    public Guid? ParentCostCenterId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal BudgetLimit { get; set; }
    public decimal BudgetUsed { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;

    public string? ParentCostCenterName { get; set; }
    public string? DepartmentName { get; set; }
    public string? WorkCenterName { get; set; }
    public string? ManagerEmployeeName { get; set; }
    public List<CostCenterDto> Children { get; set; } = new();
}

public class CreateCostCenterRequest
{
    [Required, MaxLength(20)] public string CostCenterCode { get; set; } = null!;
    [Required, MaxLength(100)] public string CostCenterName { get; set; } = null!;
    [MaxLength(20)] public string CostCenterType { get; set; } = "production";
    public Guid? ParentCostCenterId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal BudgetLimit { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
}

public class UpdateCostCenterRequest
{
    [Required, MaxLength(100)] public string CostCenterName { get; set; } = null!;
    [MaxLength(20)] public string CostCenterType { get; set; } = "production";
    public Guid? ParentCostCenterId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal BudgetLimit { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
}

public class CostCenterTreeDto
{
    public Guid Id { get; set; }
    public string CostCenterCode { get; set; } = null!;
    public string CostCenterName { get; set; } = null!;
    public string CostCenterType { get; set; } = null!;
    public decimal BudgetLimit { get; set; }
    public decimal BudgetUsed { get; set; }
    public bool IsActive { get; set; }
    public string? DepartmentName { get; set; }
    public List<CostCenterTreeDto> Children { get; set; } = new();
}

public class CostCenterBudgetDto
{
    public Guid Id { get; set; }
    public string CostCenterCode { get; set; } = null!;
    public string CostCenterName { get; set; } = null!;
    public decimal BudgetLimit { get; set; }
    public decimal BudgetUsed { get; set; }
    public decimal BudgetRemaining => BudgetLimit - BudgetUsed;
    public decimal UtilizationPercent => BudgetLimit > 0 ? (BudgetUsed / BudgetLimit) * 100 : 0;
}

public class CostElementDto
{
    public Guid? Id { get; set; }
    [Required, MaxLength(20)] public string ElementCode { get; set; } = null!;
    [Required, MaxLength(100)] public string ElementName { get; set; } = null!;
    [MaxLength(20)] public string ElementType { get; set; } = "overhead";
    public string? GLAccountCode { get; set; }
    public string? GLAccountName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
}

public class CreateCostElementRequest
{
    [Required, MaxLength(20)] public string ElementCode { get; set; } = null!;
    [Required, MaxLength(100)] public string ElementName { get; set; } = null!;
    [MaxLength(20)] public string ElementType { get; set; } = "overhead";
    public string? GLAccountCode { get; set; }
    public string? GLAccountName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
}

public class UpdateCostElementRequest
{
    [Required, MaxLength(100)] public string ElementName { get; set; } = null!;
    [MaxLength(20)] public string ElementType { get; set; } = "overhead";
    public string? GLAccountCode { get; set; }
    public string? GLAccountName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
}

public class CostElementAssignmentDto
{
    public Guid Id { get; set; }
    public Guid CostCenterId { get; set; }
    public string CostCenterCode { get; set; } = null!;
    public string CostCenterName { get; set; } = null!;
    public Guid CostElementId { get; set; }
    public string ElementCode { get; set; } = null!;
    public string ElementName { get; set; } = null!;
    public string ElementType { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class AssignCostElementRequest
{
    [Required] public Guid CostCenterId { get; set; }
    [Required] public Guid CostElementId { get; set; }
}

public class ActivityTypeDto
{
    public Guid? Id { get; set; }
    [Required, MaxLength(20)] public string ActivityCode { get; set; } = null!;
    [Required, MaxLength(100)] public string ActivityName { get; set; } = null!;
    public Guid CostCenterId { get; set; }
    public string? CostCenterName { get; set; }
    [MaxLength(10)] public string UnitOfMeasure { get; set; } = "小時";
    public decimal StandardRate { get; set; }
    public decimal? ActualRate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class CreateActivityTypeRequest
{
    [Required, MaxLength(20)] public string ActivityCode { get; set; } = null!;
    [Required, MaxLength(100)] public string ActivityName { get; set; } = null!;
    [Required] public Guid CostCenterId { get; set; }
    [MaxLength(10)] public string UnitOfMeasure { get; set; } = "小時";
    public decimal StandardRate { get; set; }
    public decimal? ActualRate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateActivityTypeRequest
{
    [Required, MaxLength(100)] public string ActivityName { get; set; } = null!;
    [MaxLength(10)] public string UnitOfMeasure { get; set; } = "小時";
    public decimal StandardRate { get; set; }
    public decimal? ActualRate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class AllocationRuleDto
{
    public Guid? Id { get; set; }
    public Guid SourceCostCenterId { get; set; }
    public string SourceCostCenterCode { get; set; } = null!;
    public string SourceCostCenterName { get; set; } = null!;
    public Guid TargetCostCenterId { get; set; }
    public string TargetCostCenterCode { get; set; } = null!;
    public string TargetCostCenterName { get; set; } = null!;
    [MaxLength(20)] public string AllocationBasis { get; set; } = "percentage";
    public decimal Percentage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class CreateAllocationRuleRequest
{
    [Required] public Guid SourceCostCenterId { get; set; }
    [Required] public Guid TargetCostCenterId { get; set; }
    [MaxLength(20)] public string AllocationBasis { get; set; } = "percentage";
    public decimal Percentage { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateAllocationRuleRequest
{
    [MaxLength(20)] public string AllocationBasis { get; set; } = "percentage";
    public decimal Percentage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class CostCenterPostingDto
{
    public Guid? Id { get; set; }
    public Guid CostCenterId { get; set; }
    public string CostCenterCode { get; set; } = null!;
    public string CostCenterName { get; set; } = null!;
    public Guid CostElementId { get; set; }
    public string ElementCode { get; set; } = null!;
    public string ElementName { get; set; } = null!;
    public DateTime PostingDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; }
    [MaxLength(50)] public string ReferenceType { get; set; } = null!;
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
}

public class CreateCostCenterPostingRequest
{
    [Required] public Guid CostCenterId { get; set; }
    [Required] public Guid CostElementId { get; set; }
    public DateTime PostingDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public decimal Quantity { get; set; }
    [Required, MaxLength(50)] public string ReferenceType { get; set; } = null!;
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
}

public class WorkCenterCostDto
{
    public Guid? Id { get; set; }
    public Guid WorkCenterId { get; set; }
    public string? WorkCenterName { get; set; }
    public Guid CostCenterId { get; set; }
    public string? CostCenterCode { get; set; }
    public string? CostCenterName { get; set; }
    public decimal LaborRate { get; set; }
    public decimal OverheadRate { get; set; }
    public decimal MachineRate { get; set; }
    public decimal TotalRate => LaborRate + OverheadRate + MachineRate;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class CreateWorkCenterCostRequest
{
    [Required] public Guid WorkCenterId { get; set; }
    [Required] public Guid CostCenterId { get; set; }
    public decimal LaborRate { get; set; }
    public decimal OverheadRate { get; set; }
    public decimal MachineRate { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
}

public class CostCenterReportDto
{
    public Guid CostCenterId { get; set; }
    public string CostCenterCode { get; set; } = null!;
    public string CostCenterName { get; set; } = null!;
    public string CostCenterType { get; set; } = null!;
    public decimal TotalBudget { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal TotalRemaining => TotalBudget - TotalUsed;
    public Dictionary<string, decimal> CostByElement { get; set; } = new();
    public Dictionary<string, decimal> CostByActivity { get; set; } = new();
}

public class LinkWorkCenterRequest
{
    [Required] public Guid WorkCenterId { get; set; }
    [Required] public Guid CostCenterId { get; set; }
}
