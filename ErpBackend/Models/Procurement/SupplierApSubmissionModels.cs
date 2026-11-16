using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Portal;
using ErpBackend.Models.System;

namespace ErpBackend.Models.Procurement;

public class SupplierApSubmission
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string SubmissionNumber { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public Guid SubmittedBySupplierUserId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "draft";
    public int CurrentRevisionNumber { get; set; }
    public Guid? GeneratedAccountsPayableId { get; set; }
    [Required, MaxLength(100)] public string IdempotencyKey { get; set; } = null!;
    [Required, MaxLength(64)] public string IdempotencyRequestHash { get; set; } = null!;
    [MaxLength(100)] public string? SupplierInvoiceNumberKey { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SupplierId))] public Supplier Supplier { get; set; } = null!;
    [ForeignKey(nameof(SubmittedBySupplierUserId))]
    public SupplierUser SubmittedBySupplierUser { get; set; } = null!;
    [ForeignKey(nameof(GeneratedAccountsPayableId))]
    public AccountsPayable? GeneratedAccountsPayable { get; set; }
    public ICollection<SupplierApSubmissionRevision> Revisions { get; set; } = new List<SupplierApSubmissionRevision>();
    public ICollection<SupplierApSubmissionAttachment> Attachments { get; set; } = new List<SupplierApSubmissionAttachment>();
    public ICollection<SupplierApSubmissionReview> Reviews { get; set; } = new List<SupplierApSubmissionReview>();
}

public class SupplierApSubmissionRevision
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public int RevisionNumber { get; set; }
    [Required, MaxLength(50)] public string SupplierInvoiceNumber { get; set; } = null!;
    public DateOnly SupplierInvoiceDate { get; set; }
    [Required, MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(14,2)")] public decimal InvoiceAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalAmount { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid SubmittedBySupplierUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    [MaxLength(64)] public string? ContentHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SubmissionId))] public SupplierApSubmission Submission { get; set; } = null!;
    [ForeignKey(nameof(PurchaseOrderId))] public PurchaseOrder PurchaseOrder { get; set; } = null!;
    [ForeignKey(nameof(GoodsReceiptId))] public GoodsReceipt GoodsReceipt { get; set; } = null!;
    [ForeignKey(nameof(SubmittedBySupplierUserId))]
    public SupplierUser SubmittedBySupplierUser { get; set; } = null!;
    public ICollection<SupplierApSubmissionRevisionLine> Lines { get; set; } = new List<SupplierApSubmissionRevisionLine>();
}

public class SupplierApSubmissionRevisionLine
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RevisionId { get; set; }
    public Guid GoodsReceiptItemId { get; set; }
    public Guid? PurchaseOrderItemId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal InvoicedQuantity { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal NetAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalAmount { get; set; }

    [ForeignKey(nameof(RevisionId))]
    public SupplierApSubmissionRevision Revision { get; set; } = null!;
    [ForeignKey(nameof(GoodsReceiptItemId))]
    public GoodsReceiptItem GoodsReceiptItem { get; set; } = null!;
    [ForeignKey(nameof(PurchaseOrderItemId))]
    public PurchaseOrderItem? PurchaseOrderItem { get; set; }
}

public class SupplierApSubmissionAttachment
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public Guid RevisionId { get; set; }
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

    [ForeignKey(nameof(SubmissionId))] public SupplierApSubmission Submission { get; set; } = null!;
    [ForeignKey(nameof(RevisionId))] public SupplierApSubmissionRevision Revision { get; set; } = null!;
    [ForeignKey(nameof(UploadedBySupplierUserId))]
    public SupplierUser UploadedBySupplierUser { get; set; } = null!;
}

public class SupplierApSubmissionReview
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public Guid RevisionId { get; set; }
    public Guid ReviewerUserId { get; set; }
    [Required, MaxLength(30)] public string Action { get; set; } = null!;
    [MaxLength(30)] public string? ReviewerRole { get; set; }
    [MaxLength(2000)] public string? Reason { get; set; }
    [MaxLength(20)] public string MatchResult { get; set; } = "pending";
    [MaxLength(50)] public string? MatchRuleVersion { get; set; }
    [Column(TypeName = "jsonb")] public string? MatchSnapshot { get; set; }
    public Guid? AccountsPayableId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SubmissionId))] public SupplierApSubmission Submission { get; set; } = null!;
    [ForeignKey(nameof(RevisionId))] public SupplierApSubmissionRevision Revision { get; set; } = null!;
    [ForeignKey(nameof(ReviewerUserId))] public User Reviewer { get; set; } = null!;
    [ForeignKey(nameof(AccountsPayableId))] public AccountsPayable? AccountsPayable { get; set; }
}
