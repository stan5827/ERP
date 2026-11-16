using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

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

public class CarbonEmissionRecordDto
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public string Period { get; set; } = null!;
    public decimal ActivityData { get; set; }
    public string? Unit { get; set; }
    public decimal CalculatedEmissions { get; set; }
    public DateTime CreatedAt { get; set; }
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

public class CarbonIntensityDto
{
    public int Year { get; set; }
    public decimal EmissionsPerRevenue { get; set; }
    public decimal EmissionsPerUnit { get; set; }
    public string Unit { get; set; } = null!;
    public decimal YoYChange { get; set; }
}
