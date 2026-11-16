using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.Finance;

public class CostCenter
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
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
    [Column(TypeName = "decimal(14,2)")] public decimal BudgetLimit { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BudgetUsed { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? AreaSquareMeters { get; set; }
    [MaxLength(200)] public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ParentCostCenterId))] public CostCenter? ParentCostCenter { get; set; }
    public ICollection<CostCenter> ChildCostCenters { get; set; } = new List<CostCenter>();
    [ForeignKey(nameof(DepartmentId))] public HR.Department? Department { get; set; }
    [ForeignKey(nameof(WorkCenterId))] public MES.WorkCenter? WorkCenter { get; set; }
    [ForeignKey(nameof(ManagerEmployeeId))] public HR.Employee? ManagerEmployee { get; set; }
    public ICollection<CostElementAssignment> CostElementAssignments { get; set; } = new List<CostElementAssignment>();
    public ICollection<ActivityType> ActivityTypes { get; set; } = new List<ActivityType>();
    public ICollection<AllocationRule> SourceAllocationRules { get; set; } = new List<AllocationRule>();
    public ICollection<AllocationRule> TargetAllocationRules { get; set; } = new List<AllocationRule>();
    public ICollection<CostCenterPosting> Postings { get; set; } = new List<CostCenterPosting>();
}

public class CostElement
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string ElementCode { get; set; } = null!;
    [Required, MaxLength(100)] public string ElementName { get; set; } = null!;
    [MaxLength(20)] public string ElementType { get; set; } = "overhead";
    [MaxLength(50)] public string? GLAccountCode { get; set; }
    [MaxLength(200)] public string? GLAccountName { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(200)] public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CostElementAssignment> Assignments { get; set; } = new List<CostElementAssignment>();
    public ICollection<CostCenterPosting> Postings { get; set; } = new List<CostCenterPosting>();
}

public class CostElementAssignment
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CostCenterId { get; set; }
    public Guid CostElementId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CostCenterId))] public CostCenter CostCenter { get; set; } = null!;
    [ForeignKey(nameof(CostElementId))] public CostElement CostElement { get; set; } = null!;
}

public class ActivityType
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string ActivityCode { get; set; } = null!;
    [Required, MaxLength(100)] public string ActivityName { get; set; } = null!;
    public Guid CostCenterId { get; set; }
    [MaxLength(10)] public string UnitOfMeasure { get; set; } = "小時";
    [Column(TypeName = "decimal(14,4)")] public decimal StandardRate { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal? ActualRate { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(200)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CostCenterId))] public CostCenter CostCenter { get; set; } = null!;
}

public class AllocationRule
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceCostCenterId { get; set; }
    public Guid TargetCostCenterId { get; set; }
    [MaxLength(20)] public string AllocationBasis { get; set; } = "percentage";
    [Column(TypeName = "decimal(8,4)")] public decimal Percentage { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    [MaxLength(200)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SourceCostCenterId))] public CostCenter SourceCostCenter { get; set; } = null!;
    [ForeignKey(nameof(TargetCostCenterId))] public CostCenter TargetCostCenter { get; set; } = null!;
}

public class CostCenterPosting
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CostCenterId { get; set; }
    public Guid CostElementId { get; set; }
    public DateTime PostingDate { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(14,2)")] public decimal Amount { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal Quantity { get; set; }
    [MaxLength(50)] public string ReferenceType { get; set; } = null!;
    public Guid? ReferenceId { get; set; }
    [MaxLength(200)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CostCenterId))] public CostCenter CostCenter { get; set; } = null!;
    [ForeignKey(nameof(CostElementId))] public CostElement CostElement { get; set; } = null!;
}

public class WorkCenterCost
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkCenterId { get; set; }
    public Guid CostCenterId { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal LaborRate { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal OverheadRate { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal MachineRate { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(WorkCenterId))] public MES.WorkCenter WorkCenter { get; set; } = null!;
    [ForeignKey(nameof(CostCenterId))] public CostCenter CostCenter { get; set; } = null!;
}
