using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class EquipmentDto
{
    public Guid Id { get; set; }
    public string EquipmentCode { get; set; } = null!;
    public string EquipmentName { get; set; } = null!;
    public string? EquipmentType { get; set; }
    public Guid? WorkCenterId { get; set; }
    public string? WorkCenterName { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public string Status { get; set; } = "active";
    public string MaintenanceStatus { get; set; } = "normal";
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
}

public class CreateEquipmentRequestDto
{
    [Required, MaxLength(30)]
    public string EquipmentCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string EquipmentName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? EquipmentType { get; set; }

    public Guid? WorkCenterId { get; set; }
    public Guid? SupplierId { get; set; }

    [MaxLength(50)]
    public string? SerialNumber { get; set; }

    [MaxLength(50)]
    public string? Model { get; set; }

    [MaxLength(50)]
    public string? Manufacturer { get; set; }

    public DateTime? PurchaseDate { get; set; }
    [Range(typeof(decimal), "0", "999999999999.99")]
    public decimal PurchasePrice { get; set; }
}

public class UpdateEquipmentRequestDto
{
    [MaxLength(100)]
    public string? EquipmentName { get; set; }

    [MaxLength(50)]
    public string? EquipmentType { get; set; }

    public Guid? WorkCenterId { get; set; }
    public Guid? SupplierId { get; set; }
    [MaxLength(50)] public string? SerialNumber { get; set; }
    [MaxLength(50)] public string? Model { get; set; }
    [MaxLength(50)] public string? Manufacturer { get; set; }
    public DateTime? PurchaseDate { get; set; }
    [Range(typeof(decimal), "0", "999999999999.99")] public decimal? PurchasePrice { get; set; }
    [RegularExpression("^(active|maintenance|repair|inactive)$")]
    public string? Status { get; set; }
}

public class EquipmentStatsDto
{
    public int TotalCount { get; set; }
    public int RepairCount { get; set; }
    public int WarningCount { get; set; }
    public decimal MonthlyMaintenanceCost { get; set; }
}
