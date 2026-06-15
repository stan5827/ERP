using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs;

public class ApsExportDataDto
{
    public List<ApsItemDto> Items { get; set; } = new();
    public List<ApsBomDto> Boms { get; set; } = new();
    public List<ApsRoutingDto> Routings { get; set; } = new();
    public List<ApsResourceDto> Resources { get; set; } = new();
    public List<ApsWorkOrderDto> WorkOrders { get; set; } = new();
}

public class ApsItemDto
{
    public string SkuCode { get; set; } = null!;
    public string SkuName { get; set; } = null!;
    public string BaseUom { get; set; } = null!;
    public string Category { get; set; } = null!;
}

public class ApsBomDto
{
    public string ParentSkuCode { get; set; } = null!;
    public string ChildSkuCode { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = null!;
}

public class ApsRoutingDto
{
    public string SkuCode { get; set; } = null!;
    public int OperationSeq { get; set; }
    public string OperationCode { get; set; } = null!;
    public string ResourceCode { get; set; } = null!;
    public decimal SetupTime { get; set; }
    public decimal RunTime { get; set; }
}

public class ApsResourceDto
{
    public string ResourceCode { get; set; } = null!;
    public string ResourceName { get; set; } = null!;
    public decimal CapacityPerBucket { get; set; }
}

public class ApsWorkOrderDto
{
    public string WoNumber { get; set; } = null!;
    public string SkuCode { get; set; } = null!;
    public decimal PlannedQty { get; set; }
    public DateTime? RequestDate { get; set; }
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Pending";
}

public class ApsImportScheduleRequest
{
    public string ScheduleVersion { get; set; } = null!;
    public List<ApsScheduleResultDto> Results { get; set; } = new();
}

public class ApsScheduleResultDto
{
    public string OrderNo { get; set; } = null!;
    public int? OperationSeq { get; set; }
    public string ResourceCode { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Sequence { get; set; }
}
