using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Text.Json.Serialization;

namespace ErpBackend.Models.Budget;

public class BudgetYear
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public Guid? CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BudgetVersion> Versions { get; set; } = new List<BudgetVersion>();
}

public class BudgetVersion
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetYearId { get; set; }
    [MaxLength(20)] public string VersionNumber { get; set; } = null!;
    [MaxLength(100)] public string? VersionName { get; set; }
    public bool IsActive { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }

    [JsonIgnore]
    [ForeignKey(nameof(BudgetYearId))] public BudgetYear BudgetYear { get; set; } = null!;
}

public class SalesForecast
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetVersionId { get; set; }
    public int Year { get; set; }
    [MaxLength(10)] public string? Quarter { get; set; }
    [MaxLength(50)] public string? Region { get; set; }
    public Guid? SalesPersonId { get; set; }
    [MaxLength(30)] public string? CustomerTier { get; set; }
    public Guid? SalesTeamId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SalesForecastItem> Items { get; set; } = new List<SalesForecastItem>();
}

public class SalesForecastItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesForecastId { get; set; }
    public Guid ProductId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PriorYearQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ForecastQty { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal GrowthRate { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EstimatedUnitPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal EstimatedAmount { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal Probability { get; set; }

    [ForeignKey(nameof(SalesForecastId))]    [JsonIgnore]
    public SalesForecast SalesForecast { get; set; } = null!;
}

public class MaterialCostForecast
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetVersionId { get; set; }
    public int Year { get; set; }
    [MaxLength(10)] public string? Quarter { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MaterialCostForecastItem> Items { get; set; } = new List<MaterialCostForecastItem>();
}

public class MaterialCostForecastItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MaterialCostForecastId { get; set; }
    public Guid SkuId { get; set; }
    public Guid? MainSupplierId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ForecastDemandQty { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal CurrentPrice { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal ExpectedPriceChange { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal EstimatedUnitPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal EstimatedTotalAmount { get; set; }
    [MaxLength(500)] public string? RiskNotes { get; set; }

    [ForeignKey(nameof(MaterialCostForecastId))]    [JsonIgnore]
    public MaterialCostForecast MaterialCostForecast { get; set; } = null!;
}

public class LaborBudget
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetVersionId { get; set; }
    public int Year { get; set; }
    public Guid DepartmentId { get; set; }
    public int CurrentHeadcount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal CurrentSalaryTotal { get; set; }

    public ICollection<LaborBudgetItem> Items { get; set; } = new List<LaborBudgetItem>();
}

public class LaborBudgetItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LaborBudgetId { get; set; }
    [MaxLength(100)] public string? PositionTitle { get; set; }
    [MaxLength(20)] public string? JobGrade { get; set; }
    public int AdditionalHeadcount { get; set; }
    public int? PlannedHireMonth { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EstimatedStartingSalary { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EstimatedOvertimeBudget { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EstimatedInsuranceBudget { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal RecruitmentBudget { get; set; }
    [MaxLength(500)] public string? Justification { get; set; }

    [ForeignKey(nameof(LaborBudgetId))]    [JsonIgnore]
    public LaborBudget LaborBudget { get; set; } = null!;
}

public class ExpenseBudget
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetVersionId { get; set; }
    public int Year { get; set; }
    public Guid DepartmentId { get; set; }

    public ICollection<ExpenseBudgetItem> Items { get; set; } = new List<ExpenseBudgetItem>();
}

public class ExpenseBudgetItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExpenseBudgetId { get; set; }
    [MaxLength(20)] public string? AccountCode { get; set; }
    [MaxLength(100)] public string? AccountName { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal AnnualBudget { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month01 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month02 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month03 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month04 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month05 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month06 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month07 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month08 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month09 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month10 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month11 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Month12 { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PriorYearActual { get; set; }
    [MaxLength(20)] public string ControlLevel { get; set; } = "warning";
    [MaxLength(50)] public string? ProjectCode { get; set; }

    [ForeignKey(nameof(ExpenseBudgetId))]    [JsonIgnore]
    public ExpenseBudget ExpenseBudget { get; set; } = null!;
}

public class CapexBudget
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetVersionId { get; set; }
    [Required, MaxLength(200)] public string ProjectName { get; set; } = null!;
    public DateTime? PlannedPurchaseDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal EstimatedAmount { get; set; }
    public int UsefulLife { get; set; }
    [MaxLength(50)] public string? FundingSource { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EstimatedMonthlyDepreciation { get; set; }
    public DateTime? PlannedCommissionDate { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal? Roi { get; set; }
    [Column(TypeName = "decimal(5,1)")] public decimal? PaybackPeriodYears { get; set; }
}
