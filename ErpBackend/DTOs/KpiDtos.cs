using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class KpiDashboardDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // OEE
    public decimal AverageOee { get; set; }
    public List<OeeSummaryDto> OeeByWorkCenter { get; set; } = new();
    public List<KpiTrendDto> OeeTrend { get; set; } = new();

    // OTD
    public decimal OnTimeDeliveryRate { get; set; }
    public int TotalOrders { get; set; }
    public int OnTimeOrders { get; set; }
    public int DelayedOrders { get; set; }
    public List<KpiTrendDto> OtdTrend { get; set; } = new();

    // WIP
    public decimal WipQuantity { get; set; }
    public decimal WipValue { get; set; }
    public Dictionary<Guid, decimal> WipByWorkCenter { get; set; } = new();

    // Scrap
    public decimal ScrapQuantity { get; set; }
    public decimal ScrapAmount { get; set; }
    public Dictionary<string, decimal> ScrapByReason { get; set; } = new();
    public List<TopScrapItemDto> TopScrapItems { get; set; } = new();

    // Capacity
    public decimal CapacityUtilization { get; set; }
}

public class OeeSummaryDto
{
    public Guid WorkCenterId { get; set; }
    public string WorkCenterName { get; set; } = null!;
    public decimal Availability { get; set; }
    public decimal Performance { get; set; }
    public decimal Quality { get; set; }
    public decimal OeeValue { get; set; }
    public string Status { get; set; } = null!;
}

public class OeeResultDto
{
    public Guid WorkCenterId { get; set; }
    public DateTime RecordDate { get; set; }
    public int? Shift { get; set; }
    public decimal PlannedRunTime { get; set; }
    public decimal ActualRunTime { get; set; }
    public decimal Downtime { get; set; }
    public decimal SetupTime { get; set; }
    public decimal WaitTime { get; set; }
    public decimal TheoreticalOutput { get; set; }
    public decimal ActualOutput { get; set; }
    public decimal GoodOutput { get; set; }
    public decimal DefectOutput { get; set; }
    public decimal Availability { get; set; }
    public decimal Performance { get; set; }
    public decimal Quality { get; set; }
    public decimal OeeValue { get; set; }
}

public class OtdResultDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalOrders { get; set; }
    public int OnTimeOrders { get; set; }
    public int DelayedOrders { get; set; }
    public decimal OnTimeRate { get; set; }
    public decimal AverageDelayDays { get; set; }
}

public class WipResultDto
{
    public decimal TotalWipQuantity { get; set; }
    public decimal TotalWipValue { get; set; }
    public decimal WipRatio { get; set; }
    public Dictionary<Guid, decimal> WipByWorkCenter { get; set; } = new();
    public int WipOrdersCount { get; set; }
}

public class ScrapResultDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalScrapQuantity { get; set; }
    public decimal TotalScrapAmount { get; set; }
    public Dictionary<string, decimal> ScrapByReason { get; set; } = new();
    public List<TopScrapItemDto> TopScrapItems { get; set; } = new();
}

public class TopScrapItemDto
{
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public decimal ScrapQuantity { get; set; }
    public decimal ScrapAmount { get; set; }
}

public class KpiTrendDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Value { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? AchievementRate { get; set; }
    public string Status { get; set; } = null!;
}

public class EquipmentListDto
{
    public Guid Id { get; set; }
    public string EquipmentCode { get; set; } = null!;
    public string EquipmentName { get; set; } = null!;
    public string? EquipmentType { get; set; }
    public string? WorkCenterName { get; set; }
    public string? SupplierName { get; set; }
    public string Status { get; set; } = "active";
    public string? MaintenanceStatus { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
}

public class KpiDefinitionDto
{
    public Guid Id { get; set; }
    public string KpiCode { get; set; } = null!;
    public string KpiName { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = "efficiency";
    public string? Formula { get; set; }
    public string Unit { get; set; } = "%";
    public decimal? TargetValue { get; set; }
    public decimal? WarningThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    public string CalculationFrequency { get; set; } = "daily";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class KpiDefinitionUpsertDto
{
    public string KpiCode { get; set; } = null!;
    public string KpiName { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Formula { get; set; }
    public string? Unit { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? WarningThreshold { get; set; }
    public decimal? CriticalThreshold { get; set; }
    public string? CalculationFrequency { get; set; }
    public bool? IsActive { get; set; }
}
