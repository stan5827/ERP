using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.Procurement;

namespace ErpBackend.Models.MES;

public class Equipment
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string EquipmentCode { get; set; } = null!;
    [Required, MaxLength(100)] public string EquipmentName { get; set; } = null!;
    [MaxLength(50)] public string? EquipmentType { get; set; }
    public Guid? WorkCenterId { get; set; }
    [MaxLength(50)] public string? SerialNumber { get; set; }
    [MaxLength(50)] public string? Model { get; set; }
    [MaxLength(50)] public string? Manufacturer { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime? PurchaseDate { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PurchasePrice { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "active";
    [MaxLength(20)] public string MaintenanceStatus { get; set; } = "normal";
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public bool IsActive { get; set; } = true;

    public Supplier? Supplier { get; set; }
    public WorkCenter? WorkCenter { get; set; }
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

public class MaintenancePlan
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string PlanNumber { get; set; } = null!;
    public Guid EquipmentId { get; set; }
    [MaxLength(50)] public string MaintenanceType { get; set; } = "preventive";
    [MaxLength(20)] public string Frequency { get; set; } = "monthly";
    public int? FrequencyDays { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(100)] public string? CheckpointList { get; set; }
    public DateTime? NextDueDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "active";
    public bool IsActive { get; set; } = true;
}

public class MaintenanceRecord
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string RecordNumber { get; set; } = null!;
    public Guid EquipmentId { get; set; }
    public Guid? MaintenancePlanId { get; set; }
    [MaxLength(50)] public string MaintenanceType { get; set; } = "preventive";
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public Guid? PerformedBy { get; set; }
    [MaxLength(1000)] public string? WorkDescription { get; set; }
    [MaxLength(500)] public string? PartsReplaced { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal LaborHours { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PartsCost { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalCost { get; set; }
    [MaxLength(20)] public string Result { get; set; } = "completed";
    [MaxLength(500)] public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EquipmentDowntime
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string DowntimeNumber { get; set; } = null!;
    public Guid EquipmentId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    [MaxLength(50)] public string DowntimeType { get; set; } = "breakdown";
    [MaxLength(50)] public string? Cause { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "open";
    public Guid? ReportedBy { get; set; }
    public Guid? AssignedTo { get; set; }
    [MaxLength(500)] public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Equipment? Equipment { get; set; }
}

public class EquipmentHistory
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EquipmentId { get; set; }
    [MaxLength(50)] public string EventType { get; set; } = null!;
    [MaxLength(500)] public string? Description { get; set; }
    public Guid? RelatedRecordId { get; set; }
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
}
