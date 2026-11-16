using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.MES;
using ErpBackend.Models.Sales;

namespace ErpBackend.Models.QA;

public class InspectionTemplateSku
{
    public Guid TemplateId { get; set; }
    public Guid ProductSkuId { get; set; }

    [ForeignKey(nameof(TemplateId))] public InspectionTemplate Template { get; set; } = null!;
    [ForeignKey(nameof(ProductSkuId))] public Sku ProductSku { get; set; } = null!;
}

public class InspectionTemplate
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string TemplateCode { get; set; } = null!;
    [Required, MaxLength(150)] public string TemplateName { get; set; } = null!;
    public Guid OperationId { get; set; }
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    [MaxLength(1000)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(OperationId))] public Operation? Operation { get; set; }
    public ICollection<InspectionTemplateSku> ProductSkus { get; set; } = new List<InspectionTemplateSku>();
    public ICollection<InspectionTemplateItem> Items { get; set; } = new List<InspectionTemplateItem>();
}

public class InspectionTemplateItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    [Required, MaxLength(30)] public string ItemCode { get; set; } = null!;
    [Required, MaxLength(150)] public string ItemName { get; set; } = null!;
    [Required, MaxLength(20)] public string InputType { get; set; } = "passFail";
    [MaxLength(500)] public string? Specification { get; set; }
    [MaxLength(30)] public string? Unit { get; set; }
    [Column(TypeName = "decimal(20,6)")] public decimal? LowerLimit { get; set; }
    [Column(TypeName = "decimal(20,6)")] public decimal? UpperLimit { get; set; }
    public string? OptionsJson { get; set; }
    [MaxLength(200)] public string? InspectionMethod { get; set; }
    [MaxLength(100)] public string? Instrument { get; set; }
    [MaxLength(200)] public string? SamplingPlan { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsCritical { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    [ForeignKey(nameof(TemplateId))] public InspectionTemplate Template { get; set; } = null!;
}

public class ProcessInspectionReport
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ReportNumber { get; set; } = null!;
    public Guid WorkOrderId { get; set; }
    public int OperationSeq { get; set; }
    public Guid TemplateId { get; set; }
    public int TemplateVersion { get; set; }
    public Guid ProductSkuId { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [MaxLength(100)] public string InspectorName { get; set; } = null!;
    public DateTime InspectedAt { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(14,2)")] public decimal SampleQuantity { get; set; }
    [Required, MaxLength(20)] public string Result { get; set; } = "pending";
    [MaxLength(1000)] public string? Notes { get; set; }

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder WorkOrder { get; set; } = null!;
    [ForeignKey(nameof(TemplateId))] public InspectionTemplate Template { get; set; } = null!;
    [ForeignKey(nameof(ProductSkuId))] public Sku ProductSku { get; set; } = null!;
    public ICollection<ProcessInspectionReportItem> Items { get; set; } = new List<ProcessInspectionReportItem>();
}

public class ProcessInspectionReportItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReportId { get; set; }
    public Guid? TemplateItemId { get; set; }
    [Required, MaxLength(150)] public string ItemName { get; set; } = null!;
    [Required, MaxLength(20)] public string InputType { get; set; } = "passFail";
    [MaxLength(500)] public string? Specification { get; set; }
    [MaxLength(30)] public string? Unit { get; set; }
    [Column(TypeName = "decimal(20,6)")] public decimal? LowerLimit { get; set; }
    [Column(TypeName = "decimal(20,6)")] public decimal? UpperLimit { get; set; }
    [MaxLength(200)] public string? InspectionMethod { get; set; }
    [MaxLength(100)] public string? Instrument { get; set; }
    [MaxLength(200)] public string? SamplingPlan { get; set; }
    public bool IsCritical { get; set; }
    public string? Value { get; set; }
    [Required, MaxLength(20)] public string Result { get; set; } = "pending";
    [MaxLength(500)] public string? Notes { get; set; }

    [ForeignKey(nameof(ReportId))] public ProcessInspectionReport Report { get; set; } = null!;
    [ForeignKey(nameof(TemplateItemId))] public InspectionTemplateItem? TemplateItem { get; set; }
}

public class FqcInspectionLot
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string InspectionNumber { get; set; } = null!;
    [Required] public Guid WorkOrderId { get; set; }
    [Required] public Guid ProductSkuId { get; set; }
    [Required, MaxLength(50)] public string LotNumber { get; set; } = null!;
    [Column(TypeName = "date")] public DateTime? ExpiryDate { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ProducedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal SampledQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DefectiveQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ApprovedQty { get; set; }
    public bool CriticalDefectFound { get; set; }
    [Required, MaxLength(20)] public string InspectionStatus { get; set; } = "Pending";
    [Required, MaxLength(20)] public string DispositionStatus { get; set; } = "Pending";
    [MaxLength(50)] public string? DefectCode { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    [Required, MaxLength(100)] public string InspectorName { get; set; } = null!;
    public DateTime InspectedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string? DecisionBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public Guid? QuarantineWarehouseId { get; set; }
    public Guid? QuarantineBinLocationId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal QuarantinedQty { get; set; }
    public DateTime? QuarantinedAt { get; set; }
    public Guid? ReworkWorkOrderId { get; set; }
    public Guid? ReworkSourceInspectionLotId { get; set; }
    [MaxLength(100)] public string? DispositionBy { get; set; }
    public DateTime? DispositionAt { get; set; }

    [ForeignKey(nameof(WorkOrderId))] public WorkOrder WorkOrder { get; set; } = null!;
    [ForeignKey(nameof(ProductSkuId))] public Sku ProductSku { get; set; } = null!;
    [ForeignKey(nameof(QuarantineWarehouseId))] public Warehouse? QuarantineWarehouse { get; set; }
    [ForeignKey(nameof(QuarantineBinLocationId))] public BinLocation? QuarantineBinLocation { get; set; }
    [ForeignKey(nameof(ReworkSourceInspectionLotId))] public FqcInspectionLot? ReworkSourceInspectionLot { get; set; }
    public FqcInspectionLot? ReworkInspectionLot { get; set; }
    public ICollection<ProductionReceiptItem> ReceiptItems { get; set; } = new List<ProductionReceiptItem>();
    public ICollection<FqcInspectionFinding> Findings { get; set; } = new List<FqcInspectionFinding>();
    public ICollection<ScrapRecord> ScrapRecords { get; set; } = new List<ScrapRecord>();
}

public class FqcInspectionFinding
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid FqcInspectionLotId { get; set; }
    [Required, MaxLength(150)] public string FindingName { get; set; } = null!;
    [Required, MaxLength(50)] public string DefectCode { get; set; } = null!;
    [Column(TypeName = "decimal(14,2)")] public decimal DefectQty { get; set; }
    public bool IsCritical { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }

    [ForeignKey(nameof(FqcInspectionLotId))] public FqcInspectionLot FqcInspectionLot { get; set; } = null!;
}
