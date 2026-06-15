namespace ErpBackend.DTOs;

public class CategoryCostAnalysisDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalActualCost { get; set; }
    public decimal TotalStandardCost { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal VariancePercentage { get; set; }
}

public class DepartmentVarianceDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public decimal MaterialVariance { get; set; }
    public decimal LaborVariance { get; set; }
    public decimal MachineVariance { get; set; }
    public decimal TotalVariance => MaterialVariance + LaborVariance + MachineVariance;
}

public class CostTrendPointDto
{
    public string Period { get; set; } = string.Empty; // e.g., "2024-01"
    public decimal ActualCost { get; set; }
    public decimal StandardCost { get; set; }
}
