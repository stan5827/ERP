using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.MES;

public class BarcodeRecord
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string Barcode { get; set; } = null!;
    [MaxLength(20)] public string BarcodeType { get; set; } = "sku";
    public Guid? RefId { get; set; }
    [MaxLength(50)] public string? RefType { get; set; }
    [MaxLength(100)] public string? RefName { get; set; }
    [MaxLength(30)] public string? LocationCode { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? Qty { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "active";
    public DateTime LastScanTime { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BarcodeScanHistory
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string Barcode { get; set; } = null!;
    [MaxLength(20)] public string BarcodeType { get; set; } = "sku";
    [MaxLength(20)] public string Action { get; set; } = "lookup";
    [MaxLength(20)] public string Result { get; set; } = "success";
    public Guid? OperatorId { get; set; }
    [MaxLength(100)] public string? OperatorName { get; set; }
    [MaxLength(500)] public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
