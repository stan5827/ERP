using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.HR;
using ErpBackend.Models.Inventory;

namespace ErpBackend.Models.Procurement;

public class Supplier
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string SupplierCode { get; set; } = null!;
    [Required, MaxLength(200)] public string SupplierName { get; set; } = null!;
    [MaxLength(10)] public string? TaxId { get; set; }
    [MaxLength(50)] public string? ContactPerson { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(200)] public string? Email { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
    [MaxLength(500)] public string? InvoiceAddress { get; set; }
    [MaxLength(500)] public string? DeliveryAddress { get; set; }
    [MaxLength(50)] public string? PaymentTerms { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [MaxLength(10)] public string? Rating { get; set; }
    [MaxLength(20)] public string ApprovalStatus { get; set; } = "PENDING";
    [MaxLength(20)] public string Status { get; set; } = "INACTIVE";
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    [MaxLength(500)] public string? ApprovalNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SupplierContact> Contacts { get; set; } = new List<SupplierContact>();
    public ICollection<SupplierBankAccount> BankAccounts { get; set; } = new List<SupplierBankAccount>();
}

public class SupplierContact
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierId { get; set; }
    [Required, MaxLength(50)] public string ContactName { get; set; } = null!;
    [MaxLength(50)] public string? Title { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(200)] public string? Email { get; set; }
    public bool IsPrimary { get; set; }

    [ForeignKey(nameof(SupplierId))] public Supplier Supplier { get; set; } = null!;
}

public class SupplierBankAccount
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierId { get; set; }
    [MaxLength(10)] public string? BankCode { get; set; }
    [MaxLength(20)] public string? BankBranch { get; set; }
    [MaxLength(30)] public string? AccountNumber { get; set; }
    [MaxLength(100)] public string? AccountName { get; set; }
    public bool IsPrimary { get; set; }

    [ForeignKey(nameof(SupplierId))] public Supplier Supplier { get; set; } = null!;
}

public class PurchaseRequisition
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string PrNumber { get; set; } = null!;
    public DateTime PrDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? RequesterId { get; set; }
    [MaxLength(50)] public string? ProjectCode { get; set; }
    public Guid? WorkOrderId { get; set; }
    [MaxLength(500)] public string? Purpose { get; set; }
    [MaxLength(20)] public string Priority { get; set; } = "normal";
    [MaxLength(100)] public string? BudgetAccount { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "DRAFT";
    [MaxLength(20)] public string? WorkflowStatus { get; set; } = "DRAFT";
    public string? CurrentApprovalLevel { get; set; }
    public int CurrentLevel { get; set; }
    public Guid? CurrentApproverId { get; set; }
    public bool IsLocked { get; set; } = false;
    [Column(TypeName = "decimal(14,2)")] public decimal TotalEstimatedAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DepartmentId))] public Department? Department { get; set; }
    [ForeignKey(nameof(RequesterId))] public Employee? Requester { get; set; }
    public ICollection<PurchaseRequisitionItem> Items { get; set; } = new List<PurchaseRequisitionItem>();
}

public class PurchaseRequisitionItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseRequisitionId { get; set; }
    public Guid SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Quantity { get; set; }
    [MaxLength(20)] public string? Unit { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal EstimatedUnitPrice { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    public DateTime? RequiredDate { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }

    [ForeignKey(nameof(PurchaseRequisitionId))] public PurchaseRequisition PurchaseRequisition { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Inventory.Sku? Sku { get; set; }
}

public class PurchaseRequisitionApproval
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseRequisitionId { get; set; }
    public int ApprovalLevel { get; set; }
    public Guid ApproverId { get; set; }
    public string Action { get; set; } = null!;
    public string? Comments { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(PurchaseRequisitionId))] public PurchaseRequisition? PurchaseRequisition { get; set; }
    [ForeignKey(nameof(ApproverId))] public Employee? Approver { get; set; }
}

public class Rfq
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string RfqNumber { get; set; } = null!;
    public Guid? PrId { get; set; }
    public DateTime RfqDate { get; set; }
    public DateTime? Deadline { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(PrId))] public PurchaseRequisition? Pr { get; set; }
    public ICollection<RfqItem> Items { get; set; } = new List<RfqItem>();
    public ICollection<RfqSupplierResponse> SupplierResponses { get; set; } = new List<RfqSupplierResponse>();
}

public class RfqItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RfqId { get; set; }
    public Guid SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Quantity { get; set; }
    [MaxLength(20)] public string? Unit { get; set; }
    [MaxLength(500)] public string? Specifications { get; set; }

    [ForeignKey(nameof(RfqId))] public Rfq Rfq { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Inventory.Sku? Sku { get; set; }
}

public class RfqSupplierResponse
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RfqId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid SkuId { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal UnitPrice { get; set; }
    public int LeadTimeDays { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Moq { get; set; }
    [MaxLength(50)] public string? TradeTerms { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public bool IsSelected { get; set; }

    [ForeignKey(nameof(RfqId))] public Rfq Rfq { get; set; } = null!;
    [ForeignKey(nameof(SupplierId))] public Supplier? Supplier { get; set; }
    [ForeignKey(nameof(SkuId))] public Inventory.Sku? Sku { get; set; }
}

public class PurchaseOrder
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string PoNumber { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public Guid? RfqId { get; set; }
    public Guid? PrId { get; set; }
    public DateTime PoDate { get; set; }
    [MaxLength(50)] public string? PaymentTerms { get; set; }
    [MaxLength(50)] public string? TradeTerms { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(12,6)")] public decimal ExchangeRate { get; set; } = 1;
    [MaxLength(20)] public string TaxType { get; set; } = "taxable";
    [MaxLength(20)] public string? TaxCode { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Subtotal { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalAmount { get; set; }
    
    [Column(TypeName = "decimal(14,2)")] public decimal BaseSubtotal { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseTaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseTotalAmount { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public bool IsLocked { get; set; } = false;
    public int Version { get; set; } = 1;
    public Guid? BlanketOrderId { get; set; }
    [MaxLength(500)] public string? CancellationReason { get; set; }
    [MaxLength(2000)] public string? TermsAndConditions { get; set; }
    [MaxLength(500)] public string? SpecialPackaging { get; set; }
    public Guid? DeliveryWarehouseId { get; set; }
    public DateTime? DueDate { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? ReceivedAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SupplierId))] public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}

public class PurchaseOrderItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseOrderId { get; set; }
    public Guid SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Quantity { get; set; }
    [MaxLength(20)] public string? Unit { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Subtotal { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ReceivedQty { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    [MaxLength(20)] public string? InventoryUnit { get; set; }
    [Column(TypeName = "decimal(14,6)")] public decimal ConversionRate { get; set; } = 1;
    [Column(TypeName = "decimal(14,2)")] public decimal InventoryQty { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal InventoryUnitPrice { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal InventorySubtotal { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))] public PurchaseOrder PurchaseOrder { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Models.Inventory.Sku? Sku { get; set; }
}

public class Asn
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string AsnNumber { get; set; } = null!;
    public Guid? PurchaseOrderId { get; set; }
    public Guid SupplierId { get; set; }
    public DateTime ShipDate { get; set; }
    public DateTime? ExpectedArrival { get; set; }
    [MaxLength(100)] public string? Carrier { get; set; }
    [MaxLength(100)] public string? TrackingNumber { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(PurchaseOrderId))] public PurchaseOrder? PurchaseOrder { get; set; }
    [ForeignKey(nameof(SupplierId))] public Supplier? Supplier { get; set; }
    public ICollection<AsnItem> Items { get; set; } = new List<AsnItem>();
}

public class AsnItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AsnId { get; set; }
    public Guid SkuId { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ShippedQty { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    [ForeignKey(nameof(AsnId))] public Asn Asn { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Inventory.Sku? Sku { get; set; }
}

public class GoodsReceipt
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string GrNumber { get; set; } = null!;
    public Guid? PurchaseOrderId { get; set; }
    public Guid? AsnId { get; set; }
    public Guid SupplierId { get; set; }
    public DateTime GrDate { get; set; }
    public Guid WarehouseId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "received";
    [MaxLength(20)] public string IqcStatus { get; set; } = "pending";
    [MaxLength(50)] public string? SupplierInvoiceNo { get; set; }
    [MaxLength(50)] public string? PackingListNo { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(12,6)")] public decimal ExchangeRate { get; set; } = 1;
    [Column(TypeName = "decimal(14,2)")] public decimal TotalTariff { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalFreight { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalLandedCost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(PurchaseOrderId))] public PurchaseOrder? PurchaseOrder { get; set; }
    [ForeignKey(nameof(AsnId))] public Asn? Asn { get; set; }
    [ForeignKey(nameof(SupplierId))] public Supplier? Supplier { get; set; }
    [ForeignKey(nameof(WarehouseId))] public Inventory.Warehouse? Warehouse { get; set; }
    public ICollection<GoodsReceiptItem> Items { get; set; } = new List<GoodsReceiptItem>();
}

public class GoodsReceiptItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GoodsReceiptId { get; set; }
    public Guid SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PoQty { get; set; }
    [MaxLength(20)] public string? PoUnit { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ReceivedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal AcceptedQty { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal RejectedQty { get; set; }
    [MaxLength(20)] public string? InventoryUnit { get; set; }
    [Column(TypeName = "decimal(14,6)")] public decimal ConversionRate { get; set; } = 1;
    [Column(TypeName = "decimal(14,2)")] public decimal InventoryQty { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? BinLocationId { get; set; }
    [MaxLength(20)] public string IqcResult { get; set; } = "pending";
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(12,6)")] public decimal ExchangeRate { get; set; } = 1;
    [Column(TypeName = "decimal(14,4)")] public decimal ForeignCurrencyPrice { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal LocalCurrencyPrice { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ForeignCurrencyAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal LocalCurrencyAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TariffAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal FreightAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalLandedCost { get; set; }

    [ForeignKey(nameof(GoodsReceiptId))] public GoodsReceipt GoodsReceipt { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Inventory.Sku? Sku { get; set; }
    [ForeignKey(nameof(BinLocationId))] public Inventory.BinLocation? BinLocation { get; set; }
}

public class AccountsPayable
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ApNumber { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    [MaxLength(50)] public string? SupplierInvoiceNumber { get; set; }
    public DateTime? SupplierInvoiceDate { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(12,6)")] public decimal ExchangeRate { get; set; } = 1;
    [MaxLength(20)] public string TaxType { get; set; } = "taxable";
    [MaxLength(20)] public string? TaxCode { get; set; }

    [Column(TypeName = "decimal(14,2)")] public decimal InvoiceAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(14,2)")] public decimal BaseInvoiceAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseTaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseTotalAmount { get; set; }
    public DateTime? DueDate { get; set; }
    [MaxLength(50)] public string? PaymentTerms { get; set; }
    public DateTime? LastAgingDate { get; set; }
    [MaxLength(20)] public string VerificationStatus { get; set; } = "pending";
    public Guid? DepartmentId { get; set; }
    [MaxLength(50)] public string? CostCenter { get; set; }
    [MaxLength(20)] public string PaymentStatus { get; set; } = "unpaid";
    [MaxLength(20)] public string ThreeWayMatchResult { get; set; } = "pending";
    [Column(TypeName = "decimal(14,2)")] public decimal? AllowanceAmount { get; set; }
    [MaxLength(500)] public string? DeductionReason { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SupplierId))] public Supplier? Supplier { get; set; }
    [ForeignKey(nameof(PurchaseOrderId))] public PurchaseOrder? PurchaseOrder { get; set; }
    [ForeignKey(nameof(GoodsReceiptId))] public GoodsReceipt? GoodsReceipt { get; set; }
    public ICollection<AccountsPayableItem> Items { get; set; } = new List<AccountsPayableItem>();
}

public class AccountsPayableItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountsPayableId { get; set; }
    public Guid SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(14,4)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Amount { get; set; }

    [ForeignKey(nameof(AccountsPayableId))] public AccountsPayable AccountsPayable { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Inventory.Sku? Sku { get; set; }
}

public class PaymentBatch
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string BatchNumber { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    [MaxLength(30)] public string PaymentMethod { get; set; } = "wire";
    [Column(TypeName = "decimal(14,2)")] public decimal TotalAmount { get; set; }
    public Guid? BankAccountId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PaymentRecord> Records { get; set; } = new List<PaymentRecord>();
}

public class PaymentRecord
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentBatchId { get; set; }
    public Guid AccountsPayableId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PaidAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DiscountAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ExchangeGainLoss { get; set; }
    [MaxLength(100)] public string? BankReference { get; set; }

    [ForeignKey(nameof(PaymentBatchId))] public PaymentBatch PaymentBatch { get; set; } = null!;
}
