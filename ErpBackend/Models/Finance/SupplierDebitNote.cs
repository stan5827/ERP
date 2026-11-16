using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.Inventory;
using ErpBackend.Models.Procurement;
using ErpBackend.Models.QA;

namespace ErpBackend.Models.Finance;

public class SupplierDebitNote
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string DebitNoteNumber { get; set; } = null!;
    [Required, MaxLength(100)] public string IdempotencyKey { get; set; } = null!;
    public Guid AccountsPayableId { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid GoodsReceiptItemId { get; set; }
    public Guid IqcReportId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid SkuId { get; set; }
    [Required, MaxLength(50)] public string LotNumber { get; set; } = null!;
    [Column(TypeName = "decimal(14,2)")] public decimal ReturnedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal NetAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseNetAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseTaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseTotalAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal InventoryAllocation { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal CogsAllocation { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseInventoryAllocation { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseCogsAllocation { get; set; }
    [Required, MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(12,6)")] public decimal ExchangeRate { get; set; } = 1;
    [Required, MaxLength(500)] public string Reason { get; set; } = null!;
    [Required, MaxLength(30)] public string Status { get; set; } = "PendingApproval";
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? PostedBy { get; set; }
    public DateTime? PostedAt { get; set; }

    [ForeignKey(nameof(AccountsPayableId))] public AccountsPayable AccountsPayable { get; set; } = null!;
    [ForeignKey(nameof(GoodsReceiptId))] public GoodsReceipt GoodsReceipt { get; set; } = null!;
    [ForeignKey(nameof(GoodsReceiptItemId))] public GoodsReceiptItem GoodsReceiptItem { get; set; } = null!;
    [ForeignKey(nameof(IqcReportId))] public IqcReport IqcReport { get; set; } = null!;
    [ForeignKey(nameof(SupplierId))] public Supplier Supplier { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Sku Sku { get; set; } = null!;
    public ICollection<SupplierDebitNoteApproval> Approvals { get; set; } = new List<SupplierDebitNoteApproval>();
}

public class SupplierDebitNoteApproval
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierDebitNoteId { get; set; }
    [Required, MaxLength(30)] public string ApprovalRole { get; set; } = null!;
    public Guid ApproverId { get; set; }
    [MaxLength(500)] public string? Comments { get; set; }
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SupplierDebitNoteId))] public SupplierDebitNote SupplierDebitNote { get; set; } = null!;
}

