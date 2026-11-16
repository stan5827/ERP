using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class CostCenterDto
{
    public Guid Id { get; set; }
    public string CostCenterCode { get; set; } = null!;
    public string CostCenterName { get; set; } = null!;
    public string CostCenterType { get; set; } = "production";
    public Guid? ParentCostCenterId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public decimal BudgetLimit { get; set; }
    public decimal BudgetUsed { get; set; }
    public decimal? AreaSquareMeters { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public string? Description { get; set; }
    public List<CostCenterDto> Children { get; set; } = [];
}

public class CostCenterBudgetDto
{
    public Guid Id { get; set; }
    public decimal BudgetLimit { get; set; }
    public decimal BudgetUsed { get; set; }
    public decimal UtilizationPercent { get; set; }
}

public class SaveCostCenterRequestDto
{
    [Required, MaxLength(20)]
    public string CostCenterCode { get; set; } = null!;

    [Required, MaxLength(100)]
    public string CostCenterName { get; set; } = null!;

    [Required, MaxLength(20)]
    public string CostCenterType { get; set; } = "production";

    public Guid? ParentCostCenterId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? WorkCenterId { get; set; }
    [Range(typeof(decimal), "0", "999999999999")]
    public decimal BudgetLimit { get; set; }
    [Range(typeof(decimal), "0", "999999999999")]
    public decimal? AreaSquareMeters { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }
    public int DisplayOrder { get; set; }
    [MaxLength(200)]
    public string? Description { get; set; }
}
