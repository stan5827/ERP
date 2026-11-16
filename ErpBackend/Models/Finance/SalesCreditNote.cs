using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.Sales;

namespace ErpBackend.Models.Finance;

public class SalesCreditNote
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string CreditNoteNumber { get; set; } = null!;
    [Required, MaxLength(100)] public string IdempotencyKey { get; set; } = null!;
    public Guid SalesInvoiceId { get; set; }
    public Guid DeliveryOrderId { get; set; }
    [Required, MaxLength(500)] public string Reason { get; set; } = null!;
    [Column(TypeName = "decimal(14,2)")] public decimal NetAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseTotalAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ReceivableAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal CustomerAdvanceAmount { get; set; }
    [Required, MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(12,6)")] public decimal ExchangeRate { get; set; } = 1;
    [Required, MaxLength(30)] public string Status { get; set; } = "PendingApproval";
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? PostedBy { get; set; }
    public DateTime? PostedAt { get; set; }

    [ForeignKey(nameof(SalesInvoiceId))] public SalesInvoice SalesInvoice { get; set; } = null!;
    [ForeignKey(nameof(DeliveryOrderId))] public DeliveryOrder DeliveryOrder { get; set; } = null!;
    public ICollection<SalesCreditNoteApproval> Approvals { get; set; } = new List<SalesCreditNoteApproval>();
}

public class SalesCreditNoteApproval
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesCreditNoteId { get; set; }
    [Required, MaxLength(30)] public string ApprovalRole { get; set; } = null!;
    public Guid ApproverId { get; set; }
    [MaxLength(500)] public string? Comments { get; set; }
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SalesCreditNoteId))] public SalesCreditNote SalesCreditNote { get; set; } = null!;
}
