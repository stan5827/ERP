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
    
    public Guid? WorkCenterId { get; set; }
    
    public int OperationSeq { get; set; }
    
    [Column(TypeName = "decimal(18, 4)")] public decimal ActualHours { get; set; }
    
    [Column(TypeName = "decimal(18, 2)")] public decimal MachineRate { get; set; }
    
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
    [NotMapped] public decimal MaterialCost { get => MaterialActualCost; set => MaterialActualCost = value; }
    [NotMapped] public decimal LaborCost { get => LaborActualCost; set => LaborActualCost = value; }
    [NotMapped] public decimal OverheadCost { get => OverheadActualCost; set => OverheadActualCost = value; }
    [NotMapped] public decimal TotalCost { get => TotalActualCost; set => TotalActualCost = value; }
    [NotMapped] public DateTime SettlementDate { get => SettledAt ?? DateTime.MinValue; set => SettledAt = value; }
    
    // 標準成本 (結案時快照)
    [Column(TypeName = "decimal(18, 2)")] public decimal StandardUnitCost { get; set; }
    [Column(TypeName = "decimal(18, 2)")] public decimal TotalStandardCost { get; set; }
    
    // 差異分析
    [Column(TypeName = "decimal(18, 2)")] public decimal VarianceAmount { get; set; }
    
    [MaxLength(20)] public string Status { get; set; } = "open"; // open, settled
    
    public DateTime? SettledAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder? WorkOrder { get; set; }
}
