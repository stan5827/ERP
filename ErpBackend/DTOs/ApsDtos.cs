using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class ApsBomDto
{
    public Guid Id { get; set; }
    public string BomCode { get; set; } = null!;
    public string? ParentSkuCode { get; set; }
    public string? ChildSkuCode { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
}

public class ApsItemDto
{
    public Guid SkuId { get; set; }
    public string SkuCode { get; set; } = null!;
    public string SkuName { get; set; } = null!;
    public string BaseUom { get; set; } = null!;
    public string? Category { get; set; }
    public decimal QtyPer { get; set; }
}

public class ApsScheduleResultItem
{
    public string? OrderNo { get; set; }
    public int? OperationSeq { get; set; }
    public string? ResourceCode { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? Sequence { get; set; }
}

public class ApsWorkOrderDto
{
    public string? WoNumber { get; set; }
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = null!;
    public Guid ProductSkuId { get; set; }
    public string SkuCode { get; set; } = null!;
    public decimal PlannedQty { get; set; }
    public DateTime RequestDate { get; set; }
    public string Priority { get; set; } = null!;
    public DateTime PlannedStartDate { get; set; }
    public DateTime PlannedEndDate { get; set; }
    public string Status { get; set; } = null!;
}

public class ApsResourceDto
{
    public Guid Id { get; set; }
    public string ResourceCode { get; set; } = null!;
    public string ResourceName { get; set; } = null!;
    public decimal CapacityPerBucket { get; set; }
}

public class ApsExportDataDto
{
    public List<ApsBomDto> Boms { get; set; } = new();
    public List<ApsItemDto> Items { get; set; } = new();
    public List<ApsWorkOrderDto> WorkOrders { get; set; } = new();
    public List<ApsResourceDto> Resources { get; set; } = new();
}

public class ApsImportScheduleRequest
{
    public string ScheduleData { get; set; } = null!;
    public string ScheduleVersion { get; set; } = null!;
    public List<ApsScheduleResultItem> Results { get; set; } = new();
}

public class GenerateScheduleRequest
{
    public DateTime? HorizonStart { get; set; }
    public DateTime? HorizonEnd { get; set; }
    public string? ScheduleType { get; set; }
}

public class OptimizationRequest
{
    public string? PrimaryGoal { get; set; }
}

public class RescheduleRequest
{
    public DateTime NewStart { get; set; }
    public DateTime NewEnd { get; set; }
}
