using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class MpsHeaderDto
{
    public Guid Id { get; set; }
    public string MpsNo { get; set; } = null!;
    public string PlantCode { get; set; } = null!;
    public string PlanName { get; set; } = null!;
    public DateTime HorizonStart { get; set; }
    public DateTime HorizonEnd { get; set; }
    public string TimeBucketType { get; set; } = null!;
    public int VersionNo { get; set; }
    public string Status { get; set; } = null!;
    public string BaseOn { get; set; } = null!;
    public int BackwardConsumptionDays { get; set; }
    public int ForwardConsumptionDays { get; set; }
    public string ConsumptionStrategy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? CreatorName { get; set; }
}

public class MpsLineDto
{
    public Guid Id { get; set; }
    public Guid SkuId { get; set; }
    public string SkuNumber { get; set; } = null!;
    public string SkuName { get; set; } = null!;
    public string UomName { get; set; } = null!;
    public DateTime BucketStart { get; set; }
    public DateTime BucketEnd { get; set; }
    public string? BucketCode { get; set; }
    public decimal PlanQty { get; set; }
    public decimal ConsumedQty { get; set; }
    public decimal AdjustedQty { get; set; }
    public bool OverrideFlag { get; set; }
    public string? Notes { get; set; }
}

public class RccpStatusDto
{
    public Guid MpsHeaderId { get; set; }
    public List<ResourceLoadDto> Loads { get; set; } = new();
}

public class ResourceLoadDto
{
    public Guid ResourceId { get; set; }
    public string ResourceCode { get; set; } = null!;
    public string ResourceName { get; set; } = null!;
    public string BucketCode { get; set; } = null!;
    public DateTime BucketStart { get; set; }
    public DateTime BucketEnd { get; set; }
    public decimal RequiredCapacity { get; set; }
    public decimal AvailableCapacity { get; set; }
    public decimal LoadPercentage => AvailableCapacity > 0 ? (RequiredCapacity / AvailableCapacity) * 100 : 0;
    public string Status => LoadPercentage > 100 ? "OVERLOAD" : LoadPercentage > 90 ? "CRITICAL" : "OK";
}

public class MpsDetailDto : MpsHeaderDto
{
    public List<MpsLineDto> Lines { get; set; } = new();
}

public class CreateMpsRequest
{
    [Required] public string PlanName { get; set; } = null!;
    public DateTime HorizonStart { get; set; }
    public DateTime HorizonEnd { get; set; }
    public string TimeBucketType { get; set; } = "WEEK";
    public string BaseOn { get; set; } = "MIXED";
    public int BackwardConsumptionDays { get; set; } = 7;
    public int ForwardConsumptionDays { get; set; } = 7;
    public string ConsumptionStrategy { get; set; } = "BACKWARD_THEN_FORWARD";
    
    public List<CreateMpsLineRequest> Lines { get; set; } = new();
}

public class CreateMpsLineRequest
{
    public Guid SkuId { get; set; }
    public DateTime BucketStart { get; set; }
    public DateTime BucketEnd { get; set; }
    public string? BucketCode { get; set; }
    public decimal PlanQty { get; set; }
    public string? Notes { get; set; }
}

public class ForecastConsumptionResultDto
{
    public Guid MpsLineId { get; set; }
    public string SkuNumber { get; set; } = null!;
    public decimal OriginalPlanQty { get; set; }
    public decimal ConsumedQty { get; set; }
    public decimal RemainingQty => Math.Max(0, OriginalPlanQty - ConsumedQty);
    public List<ConsumptionDetailDto> Details { get; set; } = new();
}

public class ConsumptionDetailDto
{
    public string SoNo { get; set; } = null!;
    public decimal ConsumedQty { get; set; }
    public DateTime ConsumedAt { get; set; }
}

public class AggregateRccpDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<ResourceAggregateLoadDto> Resources { get; set; } = new();
    
    // 修復 MPS-005: 分頁欄位
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class ResourceAggregateLoadDto
{
    public Guid ResourceId { get; set; }
    public string ResourceCode { get; set; } = null!;
    public string ResourceName { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public string Uom { get; set; } = null!;
    public List<WeeklyLoadDto> WeeklyLoads { get; set; } = new();
    public decimal MaxLoadPercentage { get; set; }
}

public class WeeklyLoadDto
{
    public string WeekCode { get; set; } = null!;
    public DateTime WeekStart { get; set; }
    public decimal Required { get; set; }
    public decimal Available { get; set; }
    public decimal LoadPercentage => Available > 0 ? (Required / Available) * 100 : 0;
}
