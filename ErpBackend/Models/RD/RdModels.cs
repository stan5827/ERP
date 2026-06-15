using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.MES;

namespace ErpBackend.Models.RD;

public class RndProject
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ProjectCode { get; set; } = null!;
    [Required, MaxLength(200)] public string ProjectName { get; set; } = null!;
    public Guid? ProjectManagerId { get; set; }
    [MaxLength(30)] public string ProjectType { get; set; } = "new_product";
    public Guid? CustomerId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? TargetPrice { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? TargetSampleDate { get; set; }
    public DateTime? TargetProductionDate { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal EstimatedBudget { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "planning";
    [MaxLength(200)] public string? TechnicalFeasibility { get; set; }
    [MaxLength(500)] public string? ComplianceStandards { get; set; }
    [MaxLength(50)] public string? MarketResearchNo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RndMilestone> Milestones { get; set; } = new List<RndMilestone>();
    public ICollection<RndBudget> Budgets { get; set; } = new List<RndBudget>();
}

public class RndMilestone
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RndProjectId { get; set; }
    [Required, MaxLength(100)] public string MilestoneName { get; set; } = null!;
    public DateTime? TargetDate { get; set; }
    public DateTime? ActualDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";

    [ForeignKey(nameof(RndProjectId))] public RndProject RndProject { get; set; } = null!;
}

public class RndBudget
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RndProjectId { get; set; }
    [MaxLength(50)] public string BudgetCategory { get; set; } = null!;
    [Column(TypeName = "decimal(14,2)")] public decimal BudgetAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ActualAmount { get; set; }

    [ForeignKey(nameof(RndProjectId))] public RndProject RndProject { get; set; } = null!;
}

public class ProductSpecification
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RndProjectId { get; set; }
    public Guid? TempSkuId { get; set; }
    [MaxLength(200)] public string? ProductName { get; set; }
    [MaxLength(100)] public string? ProductSeries { get; set; }
    public string? TechnicalParams { get; set; }
    [MaxLength(500)] public string? PackagingSpec { get; set; }
    [MaxLength(500)] public string? CertificationRequirements { get; set; }
}

public class RndLog
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RndProjectId { get; set; }
    [MaxLength(30)] public string? ExperimentNumber { get; set; }
    public Guid ResearcherId { get; set; }
    public DateTime LogDate { get; set; }
    [MaxLength(100)] public string? Location { get; set; }
    public string? TestConditions { get; set; }
    public string? Results { get; set; }
    public string? Conclusion { get; set; }
    [MaxLength(30)] public string ConfidentialityLevel { get; set; } = "internal";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Document
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string DocNumber { get; set; } = null!;
    [Required, MaxLength(200)] public string DocName { get; set; } = null!;
    [MaxLength(50)] public string DocType { get; set; } = "pdf";
    [MaxLength(20)] public string CurrentVersion { get; set; } = "1.0";
    public Guid? ProjectId { get; set; }
    public Guid? RelatedSkuId { get; set; }
    public Guid? RelatedEcnId { get; set; }
    [MaxLength(20)] public string LifecycleStatus { get; set; } = "draft";
    [MaxLength(30)] public string ConfidentialityLevel { get; set; } = "internal";
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
}

public class DocumentVersion
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    [MaxLength(20)] public string VersionNumber { get; set; } = null!;
    [MaxLength(500)] public string? FilePath { get; set; }
    public long FileSize { get; set; }
    [MaxLength(100)] public string? Checksum { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string? ChangeDescription { get; set; }

    [ForeignKey(nameof(DocumentId))] public Document Document { get; set; } = null!;
}

public class DocumentPermission
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Guid RoleId { get; set; }
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDownload { get; set; }
    public bool CanPrint { get; set; }
    public bool WatermarkEnabled { get; set; }
}

public class SampleRequest
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string RequestNumber { get; set; } = null!;
    public Guid RndProjectId { get; set; }
    [MaxLength(50)] public string Purpose { get; set; } = "testing";
    public int RequiredQty { get; set; }
    [MaxLength(50)] public string? MaterialSource { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? PilotRunYield { get; set; }
    [MaxLength(500)] public string? DfmFeedback { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "requested";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ========== Trial Production ==========
public class TrialProduction
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RndProjectId { get; set; }
    [Required, MaxLength(30)] public string TrialNumber { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int QuantityProduced { get; set; }
    public int GoodQuantity { get; set; }
    public int DefectiveQuantity { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal YieldRate { get; set; }
    [MaxLength(500)] public string? DfmFeedback { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "planning"; // planning, in_progress, completed, cancelled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(RndProjectId))] public RndProject RndProject { get; set; } = null!;
}

// ========== Mass Production Transition ==========
public class MassProductionTransition
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RndProjectId { get; set; }
    public Guid ProductionBomId { get; set; }
    public DateTime TransitionDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending"; // pending, in_progress, completed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(RndProjectId))] public RndProject RndProject { get; set; } = null!;
    [ForeignKey(nameof(ProductionBomId))] public Bom ProductionBom { get; set; } = null!;
}
