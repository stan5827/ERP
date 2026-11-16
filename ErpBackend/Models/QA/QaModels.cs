using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Models.QA;

public class IqcReport
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ReportNumber { get; set; } = null!;
    public Guid GoodsReceiptId { get; set; }
    public Guid? GoodsReceiptItemId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ReceivedQty { get; set; }
    public Guid? ReleaseBinLocationId { get; set; }
    public DateTime InspectionDate { get; set; }
    public Guid? InspectorId { get; set; }
    [MaxLength(30)] public string? SamplingLevel { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? Aql { get; set; }
    public int SampleSize { get; set; }
    public int AcceptNumber { get; set; }
    public int RejectNumber { get; set; }
    [MaxLength(20)] public string Result { get; set; } = "pending";
    [MaxLength(50)] public string? LotNumber { get; set; }

    public Supplier? Supplier { get; set; }
    public GoodsReceipt? GoodsReceipt { get; set; }
    public GoodsReceiptItem? GoodsReceiptItem { get; set; }
    public Sku? Sku { get; set; }
    public BinLocation? ReleaseBinLocation { get; set; }
    public ICollection<IqcReportItem> Items { get; set; } = new List<IqcReportItem>();
}

public class IqcReportItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IqcReportId { get; set; }
    [MaxLength(100)] public string? InspectionItem { get; set; }
    [MaxLength(200)] public string? Specification { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal? LowerLimit { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal? UpperLimit { get; set; }
    [MaxLength(100)] public string? MeasuredValue { get; set; }
    [MaxLength(20)] public string Result { get; set; } = "pass";
    [MaxLength(500)] public string? AttachmentUrl { get; set; }

    [ForeignKey(nameof(IqcReportId))] public IqcReport IqcReport { get; set; } = null!;
}

public class IpqcReport
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ReportNumber { get; set; } = null!;
    public Guid WorkOrderId { get; set; }
    public int OperationSeq { get; set; }
    public Guid? WorkCenterId { get; set; }
    public Guid? MachineId { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [MaxLength(30)] public string InspectionType { get; set; } = "patrol";
    public DateTime? InspectionTime { get; set; }
    [Column(TypeName = "decimal(8,4)")] public decimal? CpkValue { get; set; }
    [MaxLength(1000)] public string? AbnormalDescription { get; set; }
    public bool IsLineStopped { get; set; }
    [MaxLength(20)] public string Result { get; set; } = "pass";

    public ICollection<IpqcReportItem> Items { get; set; } = new List<IpqcReportItem>();
}

public class IpqcReportItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IpqcReportId { get; set; }
    [MaxLength(100)] public string? KpcItem { get; set; }
    [MaxLength(200)] public string? Specification { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal? UpperLimit { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal? LowerLimit { get; set; }
    [MaxLength(100)] public string? MeasuredValue { get; set; }
    [MaxLength(20)] public string Result { get; set; } = "pass";

    [ForeignKey(nameof(IpqcReportId))] public IpqcReport IpqcReport { get; set; } = null!;
}

public class OqcReport
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ReportNumber { get; set; } = null!;
    public Guid DeliveryOrderId { get; set; }
    public DateTime InspectionDate { get; set; }
    public Guid InspectorId { get; set; }
    public bool PackagingCheck { get; set; }
    [MaxLength(200)] public string? ReliabilityTestResult { get; set; }
    [MaxLength(20)] public string FinalDecision { get; set; } = "pending";
}

public class NonConformanceReport
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string NcrNumber { get; set; } = null!;
    [MaxLength(50)] public string SourceType { get; set; } = null!;
    public Guid? SourceId { get; set; }
    public Guid SkuId { get; set; }
    [MaxLength(50)] public string? DefectType { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DefectQty { get; set; }
    [MaxLength(1000)] public string? DefectDescription { get; set; }
    [MaxLength(30)] public string? Disposition { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal CostImpact { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SupplierComplaint
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ComplaintNumber { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? SkuId { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DefectQty { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Ppm { get; set; }
    [MaxLength(50)] public string? DefectType { get; set; }
    [MaxLength(20)] public string Priority { get; set; } = "medium";
    [MaxLength(2000)] public string? Description { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Supplier? Supplier { get; set; }
    public SupplierCapa? Capa { get; set; }
}

public class SupplierCapa
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string CapaNumber { get; set; } = null!;
    public Guid ComplaintId { get; set; }
    [MaxLength(500)] public string? D1_Team { get; set; }
    public string? D2_ProblemDescription { get; set; }
    public string? D3_InterimContainment { get; set; }
    public string? D4_RootCause { get; set; }
    public string? D5_PermanentAction { get; set; }
    public string? D6_Verification { get; set; }
    public string? D7_Prevention { get; set; }
    public string? D8_Closure { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "open";
    public DateTime? SupplierResponseDue { get; set; }
    public DateTime? SupplierRespondedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ComplaintId))] public SupplierComplaint Complaint { get; set; } = null!;
    public ICollection<SupplierCapaResponseRevision> ResponseRevisions { get; set; } = new List<SupplierCapaResponseRevision>();
    public ICollection<SupplierCapaReview> Reviews { get; set; } = new List<SupplierCapaReview>();
    public ICollection<SupplierCapaAttachment> Attachments { get; set; } = new List<SupplierCapaAttachment>();
}

public class SupplierCapaResponseRevision
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierCapaId { get; set; }
    public int RevisionNumber { get; set; }
    public Guid SupplierUserId { get; set; }
    [Required, MaxLength(20)] public string Action { get; set; } = null!;
    [MaxLength(500)] public string? D1_Team { get; set; }
    public string? D2_ProblemDescription { get; set; }
    public string? D3_InterimContainment { get; set; }
    public string? D4_RootCause { get; set; }
    public string? D5_PermanentAction { get; set; }
    public string? D6_Verification { get; set; }
    public string? D7_Prevention { get; set; }
    public string? D8_Closure { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SupplierCapaId))] public SupplierCapa SupplierCapa { get; set; } = null!;
}

public class SupplierCapaReview
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierCapaId { get; set; }
    public Guid SupplierCapaResponseRevisionId { get; set; }
    public Guid ReviewerUserId { get; set; }
    [Required, MaxLength(30)] public string Decision { get; set; } = null!;
    [MaxLength(2000)] public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SupplierCapaId))] public SupplierCapa SupplierCapa { get; set; } = null!;
    [ForeignKey(nameof(SupplierCapaResponseRevisionId))]
    public SupplierCapaResponseRevision ResponseRevision { get; set; } = null!;
}

public class SupplierCapaAttachment
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierCapaId { get; set; }
    public Guid? SupplierCapaResponseRevisionId { get; set; }
    public Guid UploadedBySupplierUserId { get; set; }
    [Required, MaxLength(500)] public string StorageKey { get; set; } = null!;
    [Required, MaxLength(255)] public string OriginalFileName { get; set; } = null!;
    [Required, MaxLength(100)] public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    [Required, MaxLength(64)] public string Sha256 { get; set; } = null!;
    [Required, MaxLength(20)] public string ScanStatus { get; set; } = "pending_scan";
    [MaxLength(100)] public string? ScanResult { get; set; }
    public DateTime? ScannedAt { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    [ForeignKey(nameof(SupplierCapaId))] public SupplierCapa SupplierCapa { get; set; } = null!;
    [ForeignKey(nameof(SupplierCapaResponseRevisionId))]
    public SupplierCapaResponseRevision? ResponseRevision { get; set; }
}
