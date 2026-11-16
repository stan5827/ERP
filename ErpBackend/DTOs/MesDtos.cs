using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ErpBackend.DTOs;

public class WorkCenterDto
{
    public Guid Id { get; set; }
    public string? WorkCenterCode { get; set; }
    public string? WorkCenterName { get; set; }
    public decimal Capacity { get; set; }
    public decimal Efficiency { get; set; }
    public bool IsActive { get; set; }
}

public class CreateWorkCenterRequest
{
    public string WorkCenterCode { get; set; } = null!;
    public string WorkCenterName { get; set; } = null!;
    public decimal Capacity { get; set; }
    public decimal Efficiency { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

public class UpdateWorkCenterRequest
{
    public string? WorkCenterCode { get; set; }
    public string? WorkCenterName { get; set; }
    public decimal? Capacity { get; set; }
    public decimal? Efficiency { get; set; }
    public bool? IsActive { get; set; }
}

public class OperationDto
{
    public Guid Id { get; set; }
    public string? OperationCode { get; set; }
    public string? OperationName { get; set; }
    public decimal SetupTime { get; set; }
    public decimal RunTime { get; set; }
    public decimal CostPerHour { get; set; }
    public bool IsOutsourced { get; set; }
    public bool IsActive { get; set; }
}

public class CreateOperationRequest
{
    public string OperationCode { get; set; } = null!;
    public string OperationName { get; set; } = null!;
    public decimal SetupTime { get; set; }
    public decimal RunTime { get; set; }
    public decimal CostPerHour { get; set; }
    public bool IsOutsourced { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateOperationRequest
{
    public string? OperationCode { get; set; }
    public string? OperationName { get; set; }
    public decimal? SetupTime { get; set; }
    public decimal? RunTime { get; set; }
    public decimal? CostPerHour { get; set; }
    public bool? IsOutsourced { get; set; }
    public bool? IsActive { get; set; }
}

public class BomDto
{
    public Guid Id { get; set; }
    public string? BomCode { get; set; }
    public Guid ProductSkuId { get; set; }
    public string? ProductSkuNumber { get; set; }
    public string? ProductSkuName { get; set; }
    public string? Version { get; set; }
    public string? Status { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BomItemDto> Items { get; set; } = new();
}

public class BomItemDto
{
    public Guid Id { get; set; }
    public Guid ChildSkuId { get; set; }
    public string? ChildSkuNumber { get; set; }
    public string? ChildSkuName { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal ScrapRate { get; set; }
    public int OperationSeq { get; set; }
}

public class CreateBomRequest
{
    public string BomCode { get; set; } = null!;
    public Guid ProductSkuId { get; set; }
    public string? Version { get; set; }
    public string? Status { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? Notes { get; set; }
    public List<CreateBomItemRequest> Items { get; set; } = new();
}

public class CreateBomItemRequest
{
    public Guid ChildSkuId { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal ScrapRate { get; set; }
    public int OperationSeq { get; set; }
}

public class UpdateBomRequest
{
    public string? BomCode { get; set; }
    public string? Version { get; set; }
    public string? Status { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? Notes { get; set; }
    public List<CreateBomItemRequest>? Items { get; set; }
}

public class BomCostRollupDto
{
    [JsonPropertyName("materialCost")] public decimal MaterialCost { get; set; }
    [JsonPropertyName("laborCost")] public decimal LaborCost { get; set; }
    [JsonPropertyName("overheadCost")] public decimal OverheadCost { get; set; }
    [JsonPropertyName("totalCost")] public decimal TotalCost { get; set; }
}

/// <summary>Confirms the previewed total before applying a BOM standard-cost rollup.</summary>
public sealed record ApplyBomStandardCostRollupRequestDto
{
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public required decimal ExpectedTotalCost { get; init; }
}

/// <summary>Reports the standard cost approved and applied from a BOM rollup.</summary>
public sealed record BomStandardCostRollupApprovalDto(
    Guid BomId,
    string BomCode,
    Guid ProductSkuId,
    string ProductSkuNumber,
    decimal PreviousStandardCost,
    decimal AppliedStandardCost,
    decimal MaterialCost,
    decimal LaborCost,
    decimal OverheadCost,
    DateTimeOffset ApprovedAt);

public class BomTreeNodeDto
{
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public List<BomTreeNodeDto> Children { get; set; } = new();
}

public class CurrencyDto
{
    public Guid Id { get; set; }
    public string? CurrencyCode { get; set; }
    public string? CurrencyName { get; set; }
    public string? Symbol { get; set; }
    public int DecimalPlaces { get; set; }
    public bool IsBaseCurrency { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class JobTitleDto
{
    public Guid Id { get; set; }
    public string TitleCode { get; set; } = "";
    public string TitleName { get; set; } = "";
    public Guid? GradeId { get; set; }
    public string? GradeCode { get; set; }
    public string? GradeName { get; set; }
    public string? ApplicableDeptType { get; set; }
    public bool IsSupervisor { get; set; }
    public bool IsActive { get; set; }
    public int EmployeeCount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JobTitleUpsertDto
{
    public string TitleCode { get; set; } = null!;
    public string TitleName { get; set; } = null!;
    public Guid? GradeId { get; set; }
    public string? ApplicableDeptType { get; set; }
    public bool? IsSupervisor { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}

public class WorkOrderDto
{
    public Guid Id { get; set; }
    public string? WoNumber { get; set; }
    public Guid ProductSkuId { get; set; }
    public string? ProductSkuName { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string? SalesOrderNumber { get; set; }
    public Guid? BomId { get; set; }
    public string? BomVersion { get; set; }
    public string? ProductionUomCode { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal ProductionQty { get; set; }
    public decimal ActualQty { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WorkOrderOperationDto> Operations { get; set; } = new();
    public List<WorkOrderMaterialDto> Materials { get; set; } = new();
}

public class WorkOrderTransferCandidateDto
{
    public Guid SalesOrderId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public string? CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? OrderRemarks { get; set; }
    public List<WorkOrderTransferCandidateItemDto> Items { get; set; } = new();
}

public class WorkOrderTransferCandidateItemDto
{
    public int LineNumber { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public string? Specification { get; set; }
    public string? CustomerItemCode { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public bool NeedManufacturing { get; set; }
    public bool NeedOutsourcing { get; set; }
    public bool IsOutsourced { get; set; }
    public bool IsAlreadyTransferred { get; set; }
    public bool WillCreateWorkOrder { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedFinishDate { get; set; }
    public string? LineRemarks { get; set; }
}

public class CreateSupplyOrdersResultDto
{
    public int PrCount { get; set; }
    public int SubWoCount { get; set; }
}

public class WorkOrderMaterialDto
{
    public Guid Id { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public string? Unit { get; set; }
    public string? InventoryUnit { get; set; }
    public decimal BomStandardQty { get; set; }
    public decimal RequiredQty { get; set; }
    public decimal IssuedQty { get; set; }
    public decimal UnissuedQty { get; set; }
    public decimal ClosedShortageQty { get; set; }
    public decimal OnHandQty { get; set; }
    public decimal AvailableQty { get; set; }
    public decimal ShortageQty { get; set; }
    public string Status { get; set; } = null!;
}

public class WorkOrderOperationDto
{
    public Guid Id { get; set; }
    public int OperationSeq { get; set; }
    public Guid OperationId { get; set; }
    public string? OperationName { get; set; }
    public Guid WorkCenterId { get; set; }
    public string? WorkCenterName { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal CompletedQty { get; set; }
    public decimal ScrapQty { get; set; }
    public string Status { get; set; } = null!;
}

public class CreateWorkOrderRequest
{
    public Guid ProductSkuId { get; set; }
    public decimal PlannedQty { get; set; }
    public string? Priority { get; set; } = "normal";
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
}

public class UpdateWorkOrderRequest
{
    public Guid? ProductSkuId { get; set; }
    public decimal? PlannedQty { get; set; }
    public string? Priority { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
}

public class RecalculateWorkOrderMaterialsRequestDto
{
    public List<Guid> WorkOrderIds { get; set; } = new();
    public decimal PlannedQty { get; set; }
}

public class UpdateWorkOrderStatusRequestDto
{
    public string? Status { get; set; }
}

public class ReorderWorkOrderOperationsRequestDto
{
    public List<Guid> OperationIds { get; set; } = new();
}

public class WorkOrderCostAnalysisDto
{
    public Guid WorkOrderId { get; set; }
    public string? WoNumber { get; set; }
    public decimal ActualMaterialCost { get; set; }
    public decimal ActualLaborCost { get; set; }
    public decimal ActualMachineCost { get; set; }
    public decimal ActualOverheadCost { get; set; }
    public decimal TotalActualCost { get; set; }
    public decimal AbnormalCost { get; set; }
    public decimal CapitalizedActualCost { get; set; }
    public decimal StandardMaterialCost { get; set; }
    public decimal StandardLaborCost { get; set; }
    public decimal StandardOverheadCost { get; set; }
    public decimal TotalStandardCost { get; set; }
    public decimal? VarianceAmount { get; set; }
    public decimal? VariancePercentage { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal ActualQty { get; set; }
    public bool HasActualCost { get; set; }
    public string? Status { get; set; }
}

public class WorkOrderCostSummaryDto
{
    public Guid Id { get; set; }
    public string? WoNumber { get; set; }
    public string? ProductSkuName { get; set; }
    public string? ProductSkuNumber { get; set; }
    public decimal ActualQty { get; set; }
    public decimal ActualMaterialCost { get; set; }
    public decimal ActualLaborCost { get; set; }
    public decimal ActualOverheadCost { get; set; }
    public decimal AbnormalCost { get; set; }
    public decimal CapitalizedActualCost { get; set; }
    public decimal TotalStandardCost { get; set; }
    public string? Status { get; set; }
}

public class RoutingDto
{
    public Guid Id { get; set; }
    public string? RoutingCode { get; set; }
    public string? RoutingName { get; set; }
    public Guid ProductSkuId { get; set; }
    public string? ProductSkuNumber { get; set; }
    public string? ProductSkuName { get; set; }
    public string? Version { get; set; }
    public bool IsActive { get; set; }
    public List<RoutingOperationDto> Operations { get; set; } = new();
}

public class RoutingOperationDto
{
    public Guid Id { get; set; }
    public Guid RoutingId { get; set; }
    public int OperationSeq { get; set; }
    public Guid OperationId { get; set; }
    public string? OperationCode { get; set; }
    public string? OperationName { get; set; }
    public Guid? WorkCenterId { get; set; }
    public string? WorkCenterCode { get; set; }
    public string? WorkCenterName { get; set; }
    public decimal SetupTime { get; set; }
    public decimal RunTime { get; set; }
    public decimal TransferTime { get; set; }
    public decimal? WaitTime { get; set; }
    public decimal CostPerHour { get; set; }
}

public class CreateRoutingRequest
{
    public string RoutingCode { get; set; } = null!;
    public string RoutingName { get; set; } = null!;
    public string? Version { get; set; }
    public Guid ProductSkuId { get; set; }
    public bool IsActive { get; set; }
    public List<CreateRoutingOperationRequest> Operations { get; set; } = new();
}

public class CreateRoutingOperationRequest
{
    public Guid OperationId { get; set; }
    public int OperationSeq { get; set; }
    public Guid? WorkCenterId { get; set; }
    public decimal? TransferTime { get; set; }
    public decimal? WaitTime { get; set; }
}

public class UpdateRoutingRequest
{
    public string? RoutingCode { get; set; }
    public string? RoutingName { get; set; }
    public string? Version { get; set; }
    public Guid? ProductSkuId { get; set; }
    public bool? IsActive { get; set; }
    public List<CreateRoutingOperationRequest>? Operations { get; set; }
}

public class WipReportRequest
{
    [Required, MaxLength(30)]
    public string WoNumber { get; set; } = null!;
    [Range(1, int.MaxValue)]
    public int OperationSeq { get; set; }
    [Range(typeof(decimal), "0", "999999999")]
    public decimal CompletedQty { get; set; }
    [Range(typeof(decimal), "0", "999999999")]
    public decimal DefectQty { get; set; }
    [Range(typeof(decimal), "0", "999999999")]
    public decimal ScrapQty { get; set; }
    [Range(0, 999999)]
    public double ManHours { get; set; }
    [Range(0, 999999)]
    public double MachineHours { get; set; }
    [Range(0, 999999)]
    public double SetupHours { get; set; }
    [Range(0, 999999)]
    public double RunHours { get; set; }
    public Guid? EmployeeId { get; set; }
    [MaxLength(20)]
    public string ReportType { get; set; } = "individual";
    public List<Guid> TeamMemberEmployeeIds { get; set; } = new();
    public List<WipReportParticipantHoursDto> TeamMemberHours { get; set; } = new();
    [MaxLength(1000)]
    public string? ProxyReason { get; set; }
    [MaxLength(20)]
    public string? ShiftClass { get; set; }
    [MaxLength(50)]
    public string? MachineCode { get; set; }
    public List<WipReportEquipmentHoursDto> EquipmentMachineHours { get; set; } = new();
    [MaxLength(50)]
    public string? MaterialLotNumber { get; set; }
    [MaxLength(50)]
    public string? FinishedLotNumber { get; set; }
    [MaxLength(50)]
    public string? DefectReasonCode { get; set; }
    [MaxLength(2000)]
    public string? Remarks { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class WipReportParticipantHoursDto
{
    public Guid EmployeeId { get; set; }
    [Range(typeof(decimal), "0", "999999.99")]
    public decimal ManHours { get; set; }
}

public class WipReportEquipmentHoursDto
{
    public Guid EquipmentId { get; set; }
    [Range(typeof(decimal), "0", "999999.99")]
    public decimal MachineHours { get; set; }
}

public class WipReportResultDto
{
    public Guid Id { get; set; }
    public string ReportNumber { get; set; } = null!;
    public string WorkOrderNumber { get; set; } = null!;
    public int OperationSeq { get; set; }
    public decimal OperationCompletedQty { get; set; }
    public decimal OperationPlannedQty { get; set; }
    public decimal WorkOrderCompletedQty { get; set; }
    public decimal WorkOrderPlannedQty { get; set; }
    public string Status { get; set; } = null!;
}

public class WipReportReviewRequestDto
{
    [MaxLength(1000)]
    public string? Comments { get; set; }
}

public class EquipmentUsageRequestDto
{
    public Guid WorkOrderId { get; set; }
    [Range(1, int.MaxValue)]
    public int OperationSeq { get; set; }
    public Guid EquipmentId { get; set; }
    [Range(typeof(decimal), "0", "999999.99")]
    public decimal MachineHours { get; set; }
    [MaxLength(20)]
    public string? ShiftClass { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    [MaxLength(1000)]
    public string? Remarks { get; set; }
}

public class EquipmentUsageRecordDto
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public string WorkOrderNumber { get; set; } = null!;
    public int OperationSeq { get; set; }
    public Guid EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = null!;
    public string EquipmentName { get; set; } = null!;
    public string RecordedByEmployeeName { get; set; } = null!;
    public string? ShiftClass { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal MachineHours { get; set; }
    public string? Remarks { get; set; }
}

public class WipReportQueueItemDto
{
    public Guid Id { get; set; }
    public string ReportNumber { get; set; } = null!;
    public string WorkOrderNumber { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public int OperationSeq { get; set; }
    public string ProductName { get; set; } = null!;
    public string OperationName { get; set; } = null!;
    public string EmployeeName { get; set; } = null!;
    public string ReportType { get; set; } = null!;
    public string ShiftClass { get; set; } = null!;
    public decimal CompletedQty { get; set; }
    public decimal DefectQty { get; set; }
    public decimal ScrapQty { get; set; }
    public decimal ManHours { get; set; }
    public decimal SetupHours { get; set; }
    public decimal RunHours { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string Status { get; set; } = null!;
    public string? ProxyReason { get; set; }
    public string? ReviewComments { get; set; }
    public string? DefectReasonCode { get; set; }
    public string? MaterialLotNumber { get; set; }
    public string? FinishedLotNumber { get; set; }
    public string? Remarks { get; set; }
    public List<Guid> TeamMemberEmployeeIds { get; set; } = new();
    public List<WipReportParticipantHoursDto> TeamMemberHours { get; set; } = new();
}
