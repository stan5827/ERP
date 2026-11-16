using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.MES;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Models.Finance;

/// <summary>
/// 實際領料記錄 (Material Actuals)
/// 記錄工單每筆領料/退料的實際成本金額
/// </summary>
[Table("work_order_material_actuals")]
public class WorkOrderMaterialActual
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid WorkOrderId { get; set; }
    
    [Required] public Guid SkuId { get; set; }
    
    public Guid? InventoryTransactionId { get; set; }
    
    [Column(TypeName = "decimal(18, 4)")] public decimal Quantity { get; set; }
    
    [Column(TypeName = "decimal(18, 4)")] public decimal UnitCost { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")] public decimal TotalAmount { get; set; }
    
    [MaxLength(50)] public string? LotNumber { get; set; }
    
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder? WorkOrder { get; set; }
    [ForeignKey(nameof(SkuId))] public Sku? Sku { get; set; }
    [ForeignKey(nameof(InventoryTransactionId))] public InventoryTransaction? InventoryTransaction { get; set; }
}

/// <summary>
/// 實際人工成本記錄 (Labor Actuals)
/// 依報工工時與員工費率計算
/// </summary>
[Table("work_order_labor_actuals")]
public class WorkOrderLaborActual
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid WorkOrderId { get; set; }
    
    public Guid? WipReportId { get; set; }

    public Guid? EmployeeId { get; set; }
    
    public int OperationSeq { get; set; }
    
    [Column(TypeName = "decimal(18, 4)")] public decimal ActualHours { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")] public decimal HourlyRate { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")] public decimal TotalAmount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder? WorkOrder { get; set; }
    [ForeignKey(nameof(WipReportId))] public WipReport? WipReport { get; set; }
}

/// <summary>
/// 實際機器成本記錄 (Machine Actuals)
/// 依機台工時與機台費率計算
/// </summary>
[Table("work_order_machine_actuals")]
public class WorkOrderMachineActual
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid WorkOrderId { get; set; }
    
    public Guid? WipReportId { get; set; }

    public Guid? EquipmentUsageRecordId { get; set; }
    
    public Guid? WorkCenterId { get; set; }
    
    public int OperationSeq { get; set; }
    
    [Column(TypeName = "decimal(18, 4)")] public decimal ActualHours { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")] public decimal MachineRate { get; set; }

    [Column(TypeName = "decimal(18, 2)")] public decimal OverheadRate { get; set; }

    [Column(TypeName = "decimal(18, 2)")] public decimal OverheadAmount { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")] public decimal TotalAmount { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder? WorkOrder { get; set; }
    [ForeignKey(nameof(WipReportId))] public WipReport? WipReport { get; set; }
    [ForeignKey(nameof(WorkCenterId))] public WorkCenter? WorkCenter { get; set; }
}

/// <summary>
/// 工單實際成本總表 (Work Order Cost Summary)
/// 彙總工單結案時的實際料工費與差異
/// </summary>
[Table("work_order_costs")]
public class WorkOrderCost
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required] public Guid WorkOrderId { get; set; }
    
    [Column(TypeName = "decimal(18, 4)")] public decimal TotalOutputQty { get; set; }
    
    // 實際成本
    [Column(TypeName = "decimal(18, 2)")] public decimal MaterialActualCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal LaborActualCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal MachineActualCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal OverheadActualCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal TotalActualCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal AbnormalCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal CapitalizedActualCost { get; set; }
    
    // 標準成本 (結案時快照)
    [Column(TypeName = "decimal(18, 2)")] public decimal StandardMaterialCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal StandardLaborCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal StandardOverheadCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal StandardUnitCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal TotalStandardCost { get; set; }
    
    // 差異分析
    [Column(TypeName = "decimal(18, 2)")] public decimal VarianceAmount { get; set; }
    
    [MaxLength(20)] public string Status { get; set; } = "open"; // open, settled
    
    public DateTime? SettledAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder? WorkOrder { get; set; }
}

[Table("work_order_cost_variance_allocations")]
public class WorkOrderCostVarianceAllocation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid WorkOrderId { get; set; }
    [Required, MaxLength(20)] public string SourceType { get; set; } = null!;
    [Required] public Guid SourceId { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal Amount { get; set; }
    [Required, MaxLength(30)] public string ReasonCode { get; set; } = null!;
    [Required, MaxLength(500)] public string Reason { get; set; } = null!;
    [Required, MaxLength(30)] public string Status { get; set; } = "PendingApproval";
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder? WorkOrder { get; set; }
    public ICollection<WorkOrderCostVarianceAllocationApproval> Approvals { get; set; } =
        new List<WorkOrderCostVarianceAllocationApproval>();
}

[Table("work_order_cost_variance_allocation_approvals")]
public class WorkOrderCostVarianceAllocationApproval
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid AllocationId { get; set; }
    [Required, MaxLength(30)] public string ApprovalRole { get; set; } = null!;
    [Required] public Guid ApproverId { get; set; }
    [MaxLength(500)] public string? Comments { get; set; }
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AllocationId))]
    public WorkOrderCostVarianceAllocation Allocation { get; set; } = null!;
}
