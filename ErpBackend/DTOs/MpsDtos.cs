using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class MpsHeaderDto
{
    public Guid Id { get; set; }
    public string MpsNo { get; set; } = null!;
    public string PlanName { get; set; } = null!;
    public DateTime? HorizonStart { get; set; }
    public DateTime? HorizonEnd { get; set; }
    public string Status { get; set; } = null!;
    public int VersionNo { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MpsDetailDto
{
    public Guid Id { get; set; }
    public string MpsNo { get; set; } = null!;
    public string PlanName { get; set; } = null!;
    public DateTime? HorizonStart { get; set; }
    public DateTime? HorizonEnd { get; set; }
    public string Status { get; set; } = null!;
    public int VersionNo { get; set; }
    public List<MpsLineDto> Lines { get; set; } = new();
}

public class MpsDetailItemDto
{
    public Guid ProductSkuId { get; set; }
    public string ProductSkuName { get; set; } = null!;
    public DateTime? DemandDate { get; set; }
    public decimal DemandQty { get; set; }
    public decimal AvailableQty { get; set; }
    public decimal NetRequirement { get; set; }
}

public class MpsExceptionDto
{
    public Guid Id { get; set; }
    public string Severity { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class MpsLineDto
{
    public Guid Id { get; set; }
    public Guid MpsHeaderId { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public DateTime BucketStart { get; set; }
    public DateTime BucketEnd { get; set; }
    public string? BucketCode { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal PlanQty { get; set; }
    public decimal CompletedQty { get; set; }
    public decimal ConsumedQty { get; set; }
    public decimal AdjustedQty { get; set; }
    public bool? OverrideFlag { get; set; }
    public string? Notes { get; set; }
}

public class RccpStatusDto
{
    public Guid MpsHeaderId { get; set; }
    public bool IsRccpDone { get; set; }
    public DateTime? LastRunDate { get; set; }
    public string Status { get; set; } = null!;
    public List<ResourceLoadDto> Loads { get; set; } = new();
}

public class AggregateRccpDto
{
    public List<RccpStatusByWeekDto> WeeklyStatus { get; set; } = new();
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<ResourceAggregateLoadDto> Resources { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class RccpStatusByWeekDto
{
    public DateTime WeekStart { get; set; }
    public string Status { get; set; } = null!;
    public decimal CapacityUtilization { get; set; }
}

public class ResourceLoadDto
{
    public Guid ResourceId { get; set; }
    public string? ResourceCode { get; set; }
    public string? ResourceName { get; set; }
    public string? ResourceType { get; set; }
    public string? Uom { get; set; }
    public decimal RequiredCapacity { get; set; }
    public decimal AvailableCapacity { get; set; }
    public decimal UtilizationRate { get; set; }
    public DateTime? BucketStart { get; set; }
    public DateTime? BucketEnd { get; set; }
    public string? BucketCode { get; set; }
}

public class ResourceAggregateLoadDto
{
    public Guid ResourceId { get; set; }
    public string? ResourceCode { get; set; }
    public string? ResourceName { get; set; }
    public string? ResourceType { get; set; }
    public string? Uom { get; set; }
    public decimal TotalRequiredCapacity { get; set; }
    public decimal TotalAvailableCapacity { get; set; }
    public decimal UtilizationRate { get; set; }
    public List<WeeklyLoadDto> WeeklyLoads { get; set; } = new();
    public decimal MaxLoadPercentage { get; set; }
}

public class WeeklyLoadDto
{
    public string WeekCode { get; set; } = null!;
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public decimal Required { get; set; }
    public decimal Available { get; set; }
    public decimal Surplus { get; set; }
    public decimal LoadPercentage { get; set; }
}

public class CreateMpsRequest
{
    public string PlanName { get; set; } = null!;
    public DateTime PlanStartDate { get; set; }
    public DateTime PlanEndDate { get; set; }
    public string? Description { get; set; }
    public DateTime? HorizonStart { get; set; }
    public DateTime? HorizonEnd { get; set; }
    public string? BaseOn { get; set; } = "MIXED";
    public string? ConsumptionStrategy { get; set; } = "BACKWARD_THEN_FORWARD";
    public int? BackwardConsumptionDays { get; set; }
    public int? ForwardConsumptionDays { get; set; }
    public string? TimeBucketType { get; set; } = "WEEK";
    public List<MpsLineDto>? Lines { get; set; } = new();
}
