using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.HR;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.QA;

namespace ErpBackend.Models.MES;

public class WorkCenter
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string WorkCenterCode { get; set; } = null!;
    [Required, MaxLength(100)] public string WorkCenterName { get; set; } = null!;
    [Column(TypeName = "decimal(10,2)")] public decimal Capacity { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal Efficiency { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

public class Operation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string OperationCode { get; set; } = null!;
    [Required, MaxLength(100)] public string OperationName { get; set; } = null!;
    [Column(TypeName = "decimal(8,2)")] public decimal SetupTime { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal RunTime { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal CostPerHour { get; set; }
    public bool IsOutsourced { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Routing
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string RoutingCode { get; set; } = null!;
    [Required, MaxLength(100)] public string RoutingName { get; set; } = null!;
    public Guid ProductSkuId { get; set; }
    [MaxLength(20)] public string Version { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;

    public Sku? ProductSku { get; set; }
    public ICollection<RoutingOperation> Operations { get; set; } = new List<RoutingOperation>();
}

public class RoutingOperation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoutingId { get; set; }
    public int OperationSeq { get; set; }
    public Guid OperationId { get; set; }
    public Guid WorkCenterId { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal TransferTime { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal WaitTime { get; set; }

    public Routing? Routing { get; set; }
    public Operation? Operation { get; set; }
    public WorkCenter? WorkCenter { get; set; }
}

public class Bom
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string BomCode { get; set; } = null!;
    public Guid ProductSkuId { get; set; }
    [MaxLength(20)] public string Version { get; set; } = "1.0";
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BomItem> Items { get; set; } = new List<BomItem>();
}

public class BomItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BomId { get; set; }
    public Guid ChildSkuId { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal Quantity { get; set; }
    [MaxLength(20)] public string Unit { get; set; } = null!;
    [Column(TypeName = "decimal(5,2)")] public decimal ScrapRate { get; set; }
    public int OperationSeq { get; set; }
    [MaxLength(30)] public string? SubstituteGroupId { get; set; }
    public bool IsPhantom { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    [ForeignKey(nameof(BomId))] public Bom Bom { get; set; } = null!;
}

public class BomPath
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AncestorSkuId { get; set; }
    public Guid DescendantSkuId { get; set; }
    public int Distance { get; set; }
    [MaxLength(50)] public string? PathId { get; set; }
}

public class Ecr
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string EcrNumber { get; set; } = null!;
    [Required, MaxLength(200)] public string Title { get; set; } = null!;
    public string? Description { get; set; }
    [MaxLength(500)] public string? ChangeReason { get; set; }
    [MaxLength(20)] public string Priority { get; set; } = "medium";
    public Guid? TargetBomId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public Guid? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Ecn
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string EcnNumber { get; set; } = null!;
    public Guid EcrId { get; set; }
    public string? ChangeDescription { get; set; }
    public DateTime EffectiveDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public Guid? PublishedBy { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EcrId))] public Ecr Ecr { get; set; } = null!;
    public ICollection<EcnAffectedBom> AffectedBoms { get; set; } = new List<EcnAffectedBom>();
}

public class EcnAffectedBom
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EcnId { get; set; }
    public Guid BomId { get; set; }
    [MaxLength(30)] public string ActionType { get; set; } = "update";

    [ForeignKey(nameof(EcnId))] public Ecn Ecn { get; set; } = null!;
}

public class WorkOrder
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string WoNumber { get; set; } = null!;
    [MaxLength(30)] public string WoType { get; set; } = "regular";
    public Guid ProductSkuId { get; set; }
    public Guid? BomId { get; set; }
    [MaxLength(20)] public string? BomVersion { get; set; }
    public Guid? SalesOrderId { get; set; }
    [MaxLength(20)] public string? ProductionUomCode { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PlannedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ActualQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ReceivedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ScrapQty { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    [MaxLength(20)] public string Priority { get; set; } = "normal";
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // APS Integration Hooks
    public DateTime? MrpPlannedStartDate { get; set; }
    public DateTime? MrpPlannedDueDate { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    [MaxLength(50)] public string? ScheduledResourceCode { get; set; }
    public bool IsScheduleLocked { get; set; }
    [MaxLength(30)] public string? ScheduleVersion { get; set; }
    
    // 修復 MRP→APS 數據斷層：自動排程標記
    public bool NeedsRescheduling { get; set; }
    public DateTime? RescheduleTriggeredAt { get; set; }

    [ForeignKey(nameof(ProductSkuId))] public Sku? ProductSku { get; set; }
    public ICollection<WorkOrderMaterial> Materials { get; set; } = new List<WorkOrderMaterial>();
    public ICollection<WorkOrderOperation> Operations { get; set; } = new List<WorkOrderOperation>();
}

public class WorkOrderMaterial
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; set; }
    public Guid SkuId { get; set; }
    public Guid? FqcInspectionLotId { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal BomStandardQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal RequiredQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal IssuedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ReturnedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ShortageQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ClosedShortageQty { get; set; }
    [MaxLength(20)] public string? Unit { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder WorkOrder { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Sku Sku { get; set; } = null!;
    [ForeignKey(nameof(FqcInspectionLotId))] public FqcInspectionLot? FqcInspectionLot { get; set; }
}

public class WorkOrderOperation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; set; }
    public int OperationSeq { get; set; }
    public Guid OperationId { get; set; }
    public Guid WorkCenterId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PlannedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal CompletedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ScrapQty { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    
    // APS Integration Hooks
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public Guid? ScheduledWorkCenterId { get; set; }
    public int? ScheduledSequence { get; set; }

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder WorkOrder { get; set; } = null!;
    [ForeignKey(nameof(OperationId))] public Operation Operation { get; set; } = null!;
    [ForeignKey(nameof(WorkCenterId))] public WorkCenter WorkCenter { get; set; } = null!;
}

public class WipReport
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ReportNumber { get; set; } = null!;
    public Guid WorkOrderId { get; set; }
    public int OperationSeq { get; set; }
    public Guid EmployeeId { get; set; }
    [MaxLength(20)] public string? ShiftClass { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal CompletedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DefectQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ScrapQty { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal ActualManHours { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal ActualMachineHours { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal SetupHours { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal RunHours { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    [MaxLength(50)] public string? MachineCode { get; set; }
    [MaxLength(50)] public string? MaterialLotNumber { get; set; }
    [MaxLength(50)] public string? FinishedLotNumber { get; set; }
    [MaxLength(2000)] public string? Remarks { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? EnergyConsumption { get; set; }
    public bool HasAnomaly { get; set; }
    [MaxLength(50)] public string? AnomalyType { get; set; }
    public string? AiDiagnosis { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(20)] public string ReportType { get; set; } = "individual";
    [MaxLength(20)] public string Status { get; set; } = "submitted";
    public Guid? SubmittedByUserId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    [MaxLength(1000)] public string? ReviewComments { get; set; }
    [MaxLength(1000)] public string? ProxyReason { get; set; }

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder? WorkOrder { get; set; }
    public ICollection<WipReportEquipmentHour> EquipmentHours { get; set; } = new List<WipReportEquipmentHour>();
    public ICollection<WipReportParticipant> Participants { get; set; } = new List<WipReportParticipant>();
    public ICollection<WipReportAuditEvent> AuditEvents { get; set; } = new List<WipReportAuditEvent>();
}

public class WipReportParticipant
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WipReportId { get; set; }
    public Guid EmployeeId { get; set; }
    [MaxLength(20)] public string Role { get; set; } = "participant";
    [Column(TypeName = "decimal(8,2)")] public decimal ActualManHours { get; set; }

    [ForeignKey(nameof(WipReportId))] public WipReport WipReport { get; set; } = null!;
    [ForeignKey(nameof(EmployeeId))] public Employee Employee { get; set; } = null!;
}

public class WipReportAuditEvent
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WipReportId { get; set; }
    public Guid ActorUserId { get; set; }
    [Required, MaxLength(30)] public string Action { get; set; } = null!;
    [MaxLength(1000)] public string? Reason { get; set; }
    public string? ChangesJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(WipReportId))] public WipReport WipReport { get; set; } = null!;
}

public class WipReportEquipmentHour
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WipReportId { get; set; }
    public Guid EquipmentId { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal MachineHours { get; set; }

    [ForeignKey(nameof(WipReportId))] public WipReport WipReport { get; set; } = null!;
    [ForeignKey(nameof(EquipmentId))] public Equipment Equipment { get; set; } = null!;
}

public class EquipmentUsageRecord
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; set; }
    public int OperationSeq { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid RecordedByEmployeeId { get; set; }
    public Guid RecordedByUserId { get; set; }
    [MaxLength(20)] public string? ShiftClass { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal MachineHours { get; set; }
    [MaxLength(1000)] public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder WorkOrder { get; set; } = null!;
    [ForeignKey(nameof(EquipmentId))] public Equipment Equipment { get; set; } = null!;
    [ForeignKey(nameof(RecordedByEmployeeId))] public Employee RecordedByEmployee { get; set; } = null!;
}

public class ScrapRecord
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkOrderId { get; set; }
    public Guid SkuId { get; set; }
    public Guid? FqcInspectionLotId { get; set; }
    [MaxLength(30)] public string? ScrapNumber { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ScrapQty { get; set; }
    [MaxLength(2)] public string? ScrapReasonCode { get; set; }
    [MaxLength(500)] public string? Reason { get; set; }
    [MaxLength(30)] public string? DispositionCode { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal CostAmount { get; set; }
    [MaxLength(30)] public string? ScrapStatus { get; set; }
    [MaxLength(100)] public string? DispositionBy { get; set; }
    public DateTime? DispositionAt { get; set; }
    public Guid? RequestedByUserId { get; set; }
    [MaxLength(100)] public string? RequestedByName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(FqcInspectionLotId))] public FqcInspectionLot? FqcInspectionLot { get; set; }
    public ICollection<FqcScrapApprovalEvent> ApprovalEvents { get; set; } = new List<FqcScrapApprovalEvent>();
}

public class FqcScrapApprovalEvent
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScrapRecordId { get; set; }
    [Required, MaxLength(20)] public string Stage { get; set; } = null!;
    [Required, MaxLength(20)] public string Decision { get; set; } = null!;
    public Guid ActorUserId { get; set; }
    [Required, MaxLength(100)] public string ActorName { get; set; } = null!;
    [MaxLength(1000)] public string? Comments { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ScrapRecordId))] public ScrapRecord ScrapRecord { get; set; } = null!;
}

/// <summary>
/// APS 排程結果歷史紀錄 (Audit Trail)
/// </summary>
public class ApsScheduleResult
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string OrderNo { get; set; } = null!;
    public int? OperationSeq { get; set; }
    [MaxLength(30)] public string ResourceCode { get; set; } = null!;
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public DateTime? MrpPlannedStart { get; set; }
    public int Sequence { get; set; }
    [MaxLength(30)] public string ScheduleVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(50)] public string SourceSystem { get; set; } = "APS";
}
