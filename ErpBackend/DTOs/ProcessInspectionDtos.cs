namespace ErpBackend.DTOs;

public class InspectionTemplateDto
{
    public Guid Id { get; set; }
    public string TemplateCode { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public Guid OperationId { get; set; }
    public string? OperationCode { get; set; }
    public string? OperationName { get; set; }
    public Guid? ProductSkuId { get; set; }
    public string? ProductSkuNumber { get; set; }
    public string? ProductSkuName { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public List<InspectionTemplateItemDto> Items { get; set; } = new();
}

public class InspectionTemplateItemDto
{
    public Guid Id { get; set; }
    public string ItemCode { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public string InputType { get; set; } = "passFail";
    public string? Specification { get; set; }
    public string? Unit { get; set; }
    public decimal? LowerLimit { get; set; }
    public decimal? UpperLimit { get; set; }
    public List<string> Options { get; set; } = new();
    public string? InspectionMethod { get; set; }
    public string? Instrument { get; set; }
    public string? SamplingPlan { get; set; }
    public bool IsRequired { get; set; }
    public bool IsCritical { get; set; }
    public int SortOrder { get; set; }
}

public class SaveInspectionTemplateRequestDto
{
    public string TemplateCode { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public Guid OperationId { get; set; }
    public Guid? ProductSkuId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public List<SaveInspectionTemplateItemRequestDto> Items { get; set; } = new();
}

public class SaveInspectionTemplateItemRequestDto
{
    public string ItemCode { get; set; } = null!;
    public string ItemName { get; set; } = null!;
    public string InputType { get; set; } = "passFail";
    public string? Specification { get; set; }
    public string? Unit { get; set; }
    public decimal? LowerLimit { get; set; }
    public decimal? UpperLimit { get; set; }
    public List<string> Options { get; set; } = new();
    public string? InspectionMethod { get; set; }
    public string? Instrument { get; set; }
    public string? SamplingPlan { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsCritical { get; set; }
    public int SortOrder { get; set; }
}

public class WorkOrderInspectionOperationDto
{
    public int OperationSeq { get; set; }
    public Guid OperationId { get; set; }
    public string OperationCode { get; set; } = null!;
    public string OperationName { get; set; } = null!;
    public Guid WorkCenterId { get; set; }
    public string? WorkCenterName { get; set; }
    public List<InspectionTemplateDto> Templates { get; set; } = new();
}

public class CreateProcessInspectionRequestDto
{
    public Guid WorkOrderId { get; set; }
    public int OperationSeq { get; set; }
    public Guid TemplateId { get; set; }
    public string? LotNumber { get; set; }
    public string InspectorName { get; set; } = null!;
    public decimal SampleQuantity { get; set; } = 1;
    public string? Notes { get; set; }
    public List<CreateProcessInspectionItemRequestDto> Items { get; set; } = new();
}

public class CreateProcessInspectionItemRequestDto
{
    public Guid TemplateItemId { get; set; }
    public string? Value { get; set; }
    public string Result { get; set; } = "pass";
    public string? Notes { get; set; }
}

public class ProcessInspectionReportDto
{
    public Guid Id { get; set; }
    public string ReportNumber { get; set; } = null!;
    public Guid WorkOrderId { get; set; }
    public string? WorkOrderNumber { get; set; }
    public int OperationSeq { get; set; }
    public string? OperationCode { get; set; }
    public string? OperationName { get; set; }
    public string? WorkCenterName { get; set; }
    public Guid TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public int TemplateVersion { get; set; }
    public Guid ProductSkuId { get; set; }
    public string? ProductSkuNumber { get; set; }
    public string? ProductSkuName { get; set; }
    public string? LotNumber { get; set; }
    public string InspectorName { get; set; } = null!;
    public DateTime InspectedAt { get; set; }
    public decimal SampleQuantity { get; set; }
    public string Result { get; set; } = null!;
    public string? Notes { get; set; }
    public List<ProcessInspectionReportItemDto> Items { get; set; } = new();
}

public class ProcessInspectionReportItemDto
{
    public string ItemName { get; set; } = null!;
    public string InputType { get; set; } = null!;
    public string? Specification { get; set; }
    public string? Unit { get; set; }
    public decimal? LowerLimit { get; set; }
    public decimal? UpperLimit { get; set; }
    public string? InspectionMethod { get; set; }
    public string? Instrument { get; set; }
    public string? SamplingPlan { get; set; }
    public bool IsCritical { get; set; }
    public string? Value { get; set; }
    public string Result { get; set; } = null!;
    public string? Notes { get; set; }
}

public class DeliveryInspectionReportDto
{
    public Guid DeliveryOrderId { get; set; }
    public string DeliveryNumber { get; set; } = null!;
    public DateTime DeliveryDate { get; set; }
    public string? SalesOrderNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? DeliveryAddress { get; set; }
    public string OverallResult { get; set; } = "pending";
    public List<string> Warnings { get; set; } = new();
    public List<DeliveryInspectionItemDto> Items { get; set; } = new();
}

public class DeliveryInspectionItemDto
{
    public Guid DeliveryOrderItemId { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public string? LotNumber { get; set; }
    public decimal Quantity { get; set; }
    public string Result { get; set; } = "pending";
    public List<string> Warnings { get; set; } = new();
    public List<ProcessInspectionReportDto> Reports { get; set; } = new();
}
