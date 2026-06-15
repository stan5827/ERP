using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.Procurement;

namespace ErpBackend.Models.Portal;

public class SupplierUser
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierId { get; set; }
    [Required, MaxLength(50)] public string Username { get; set; } = null!;
    [Required] public string PasswordHash { get; set; } = null!;
    [MaxLength(50)] public string? ContactName { get; set; }
    [MaxLength(200)] public string? Email { get; set; }
    [MaxLength(30)] public string Role { get; set; } = "viewer";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SupplierId))] public Supplier Supplier { get; set; } = null!;
}

public class SupplierDocument
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierId { get; set; }
    [MaxLength(50)] public string DocType { get; set; } = null!;
    [MaxLength(200)] public string? DocName { get; set; }
    [MaxLength(500)] public string? FilePath { get; set; }
    public DateTime? ExpiryDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "valid";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public class SupplierQuote
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RfqId { get; set; }
    public Guid SupplierUserId { get; set; }
    public Guid SupplierId { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal QuotedPrice { get; set; }
    public int LeadTimeDays { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Moq { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public class SupplierPerformanceScore
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierId { get; set; }
    [MaxLength(7)] public string Period { get; set; } = null!;
    [Column(TypeName = "decimal(5,2)")] public decimal OtifRate { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Ppm { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal OrderAccuracyRate { get; set; }
    [Column(TypeName = "decimal(8,2)")] public decimal ResponseTimeHours { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal OverallScore { get; set; }
    [MaxLength(5)] public string? Rating { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public class PortalMessageThread
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(200)] public string? Subject { get; set; }
    [MaxLength(50)] public string? RelatedDocType { get; set; }
    public Guid? RelatedDocId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? CreatedBy { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PortalMessage> Messages { get; set; } = new List<PortalMessage>();
}

public class PortalMessage
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ThreadId { get; set; }
    [MaxLength(20)] public string SenderType { get; set; } = null!;
    public Guid SenderId { get; set; }
    public string? Content { get; set; }
    [MaxLength(500)] public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ThreadId))] public PortalMessageThread Thread { get; set; } = null!;
}

public class SupplierEsgQuestionnaire
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierId { get; set; }
    [Required, MaxLength(30)] public string QuestionnaireNumber { get; set; } = null!;
    [MaxLength(50)] public string QuestionnaireType { get; set; } = "ESG_SelfAssessment";
    public int Year { get; set; }
    public string? ResponsesJson { get; set; }
    [MaxLength(200)] public string? EnvironmentalCertifications { get; set; }
    [MaxLength(200)] public string? SocialInitiatives { get; set; }
    [MaxLength(200)] public string? GovernancePractices { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal? TotalScore { get; set; }
    [MaxLength(20)] public string? OverallRating { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SupplierAsn
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string AsnNumber { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    [MaxLength(100)] public string? CarrierName { get; set; }
    [MaxLength(100)] public string? TrackingNumber { get; set; }
    public string? Remarks { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SupplierAsnItem> Items { get; set; } = new List<SupplierAsnItem>();
}

public class SupplierAsnItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AsnId { get; set; }
    public Guid? SkuId { get; set; }
    [Required, MaxLength(50)] public string SkuNumber { get; set; } = null!;
    [MaxLength(200)] public string SkuName { get; set; } = null!;
    public int ShippedQty { get; set; }
    [MaxLength(50)] public string? BatchNo { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    [ForeignKey(nameof(AsnId))] public SupplierAsn Asn { get; set; } = null!;
}

