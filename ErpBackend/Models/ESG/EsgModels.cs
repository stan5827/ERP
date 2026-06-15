using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErpBackend.Models.ESG;

public class EmissionSource
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string SourceCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string SourceName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Scope { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}

public class EmissionFactor
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string FactorCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string FactorName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;
    
    public decimal FactorValue { get; set; }
    
    [MaxLength(50)]
    public string UnitOfMeasure { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Scope { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Region { get; set; } = string.Empty;
    
    public int Year { get; set; }
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime EffectiveFrom { get; set; }
    
    public DateTime? EffectiveTo { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EmissionRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid SourceId { get; set; }
    
    [MaxLength(100)]
    public string SourceCode { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string SourceName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Scope { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Period { get; set; } = string.Empty;
    
    public DateTime PeriodStart { get; set; }
    
    public DateTime PeriodEnd { get; set; }
    
    public decimal ActivityData { get; set; }
    
    [MaxLength(50)]
    public string ActivityUnit { get; set; } = string.Empty;
    
    public Guid? FactorId { get; set; }
    
    public decimal? EmissionFactor { get; set; }
    
    public decimal CalculatedEmissions { get; set; }
    
    [MaxLength(50)]
    public string EmissionUnit { get; set; } = "tCO2e";
    
    public decimal? VerifiedEmissions { get; set; }
    
    [MaxLength(500)]
    public string Remarks { get; set; } = string.Empty;
    
    public string Status { get; set; } = "draft";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public string CreatedBy { get; set; } = string.Empty;
}

public class CarbonReductionTarget
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string TargetCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string TargetName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Scope { get; set; } = string.Empty;
    
    public decimal BaselineEmissions { get; set; }
    
    public decimal TargetReductionPercent { get; set; }
    
    public decimal TargetEmissions { get; set; }
    
    public int BaseYear { get; set; }
    
    public int TargetYear { get; set; }
    
    public decimal CurrentEmissions { get; set; }
    
    public decimal AchievedReductionPercent { get; set; }
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    public string Status { get; set; } = "active";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}

public class SupplierSustainability
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid SupplierId { get; set; }
    
    [MaxLength(100)]
    public string SupplierCode { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;
    
    public decimal CarbonFootprint { get; set; }
    
    [MaxLength(50)]
    public string CarbonUnit { get; set; } = "tCO2e";
    
    public decimal RenewableEnergyPercent { get; set; }
    
    public int SustainabilityScore { get; set; }
    
    [MaxLength(10)]
    public string Rating { get; set; } = "C";
    
    public DateTime LastAssessmentDate { get; set; }
    
    public DateTime? NextAssessmentDate { get; set; }
    
    [MaxLength(500)]
    public string Certification { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string ImprovementPlan { get; set; } = string.Empty;
    
    public bool HasIso14001 { get; set; }
    
    public bool HasIso14064 { get; set; }
    
    public string Status { get; set; } = "active";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}

public class EsgReport
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string ReportCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string ReportName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string ReportType { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Framework { get; set; } = string.Empty;
    
    public DateTime ReportPeriodStart { get; set; }
    
    public DateTime ReportPeriodEnd { get; set; }
    
    public decimal TotalScope1Emissions { get; set; }
    
    public decimal TotalScope2Emissions { get; set; }
    
    public decimal TotalScope3Emissions { get; set; }
    
    public decimal TotalEmissions { get; set; }
    
    public decimal EmissionIntensity { get; set; }
    
    [MaxLength(50)]
    public string IntensityUnit { get; set; } = "tCO2e/MWh";
    
    public string Status { get; set; } = "draft";
    
    public bool IsVerified { get; set; }
    
    [MaxLength(200)]
    public string Verifier { get; set; } = string.Empty;
    
    public DateTime? VerifiedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? PublishedAt { get; set; }
}

public class EnergyConsumption
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string EnergyType { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Scope { get; set; } = string.Empty;
    
    public decimal Consumption { get; set; }
    
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;
    
    public decimal CarbonFactor { get; set; }
    
    public decimal CalculatedEmissions { get; set; }
    
    [MaxLength(20)]
    public string Period { get; set; } = string.Empty;
    
    public DateTime PeriodStart { get; set; }
    
    public DateTime PeriodEnd { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
