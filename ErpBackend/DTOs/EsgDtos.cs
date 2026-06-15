using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class GhgEmissionSourceDto
{
    public Guid Id { get; set; }
    public string SourceCode { get; set; } = null!;
    public string SourceName { get; set; } = null!;
    public string Scope { get; set; } = null!; // scope1, scope2, scope3
    public string? EnergyType { get; set; }
    public decimal EmissionFactor { get; set; }
    public decimal GwpValue { get; set; }
}

public class CarbonEmissionRecordDto
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public string SourceName { get; set; } = null!;
    public string Period { get; set; } = null!;
    public decimal ActivityData { get; set; }
    public string Unit { get; set; } = null!;
    public decimal CalculatedEmissions { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductCarbonFootprintDto
{
    public Guid SkuId { get; set; }
    public string SkuNumber { get; set; } = null!;
    public string SkuName { get; set; } = null!;
    public decimal TotalCo2e { get; set; }
    public string Unit { get; set; } = "kgCO2e";
    public List<CarbonFootprintComponentDto> Components { get; set; } = new();
}

public class CarbonFootprintComponentDto
{
    public string Category { get; set; } = null!; // Material, Manufacturing, Logistic
    public string Name { get; set; } = null!;
    public decimal Co2e { get; set; }
    public decimal ActivityData { get; set; }
    public string ActivityUnit { get; set; } = null!;
    public decimal Percentage { get; set; }
}

public class EsgKpiDto
{
    public string KpiName { get; set; } = null!;
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal ChangePercentage { get; set; }
    public string Status { get; set; } = "normal"; // normal, warning, critical
}

public class CarbonDashboardDto
{
    public int Year { get; set; }
    public decimal TotalEmissions { get; set; }
    public decimal Scope1Emissions { get; set; }
    public decimal Scope2Emissions { get; set; }
    public decimal Scope3Emissions { get; set; }
    public decimal ReductionTarget { get; set; }
    public decimal ReductionProgress { get; set; }
    public Dictionary<string, decimal> MonthlyEmissions { get; set; } = new();
    public Dictionary<string, decimal> ByCategory { get; set; } = new();
}

public class CarbonIntensityDto
{
    public int Year { get; set; }
    public decimal EmissionsPerRevenue { get; set; }
    public decimal EmissionsPerUnit { get; set; }
    public string Unit { get; set; } = null!;
    public decimal YoYChange { get; set; }
}

public class CarbonSummaryDto
{
    public int Year { get; set; }
    public decimal TotalEmissions { get; set; }
    public decimal Scope1Total { get; set; }
    public decimal Scope2Total { get; set; }
    public decimal Scope3Total { get; set; }
    public int RecordCount { get; set; }
    public string VerificationStatus { get; set; } = null!;
}
