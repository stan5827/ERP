namespace ErpBackend.DTOs;

public class BudgetAlertDto
{
    public string CostCenterCode { get; set; } = null!;
    public string CostCenterName { get; set; } = null!;
    public decimal BudgetAmount { get; set; }
    public decimal UsedAmount { get; set; }
    public decimal UtilizationPercent { get; set; }
    public string WarningLevel { get; set; } = "none";
    public string Message { get; set; } = null!;
}

public class BudgetOverrunCheck
{
    public decimal Amount { get; set; }
    public string CostCenterCode { get; set; } = null!;
    public decimal BudgetAmount { get; set; }
    public decimal PreviousUsed { get; set; }
    public decimal ProjectedTotal { get; set; }
    public decimal RemainingAfter { get; set; }
    public string Status { get; set; } = "ok";
    public string WarningLevel { get; set; } = "none";
    public string Message { get; set; } = "ok";
}
