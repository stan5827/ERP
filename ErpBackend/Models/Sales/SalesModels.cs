using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ErpBackend.Models.HR;

namespace ErpBackend.Models.Sales;

public class Customer
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(20)] public string CustomerCode { get; set; } = null!;
    [Required, MaxLength(200)] public string CustomerName { get; set; } = null!;
    [MaxLength(50)] public string? CustomerShortName { get; set; }
    [MaxLength(10)] public string? TaxId { get; set; }
    [MaxLength(50)] public string? ContactPerson { get; set; }
    [MaxLength(50)] public string? ContactTitle { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(30)] public string? Mobile { get; set; }
    [MaxLength(200)] public string? Email { get; set; }
    [Column(TypeName = "decimal(5,4)")] public decimal FixedDiscount { get; set; } = 1.0m;
    public int BillingDay { get; set; } = 25;
    public int CollectionDay { get; set; } = 10;
    [MaxLength(20)] public string InvoiceMethod { get; set; } = "email";
    [Column(TypeName = "decimal(14,2)")] public decimal CreditLimit { get; set; }
    [MaxLength(20)] public string CreditStatus { get; set; } = "normal";
    [MaxLength(20)] public string CompanyType { get; set; } = "customer"; 
    [MaxLength(30)] public string? Fax { get; set; }
    [MaxLength(50)] public string? PaymentTerms { get; set; }
    [MaxLength(20)] public string? TaxCategory { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [MaxLength(20)] public string? PriceLevel { get; set; }
    [MaxLength(100)] public string? Industry { get; set; }
    [MaxLength(100)] public string? Region { get; set; }
    [MaxLength(100)] public string? SalesTerritory { get; set; }
    [MaxLength(20)] public string ApprovalStatus { get; set; } = "approved"; // draft, pending, approved
    public Guid? SalesPersonId { get; set; }
    [MaxLength(500)] public string? Address { get; set; }
    [MaxLength(500)] public string? InvoiceAddress { get; set; }
    [MaxLength(50)] public string? LeadSource { get; set; }
    public bool? ChurnRisk { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
    public int? SatisfactionScore { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CustomerContact> Contacts { get; set; } = new List<CustomerContact>();
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}

public class CustomerContact
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    [Required, MaxLength(50)] public string ContactName { get; set; } = null!;
    [MaxLength(50)] public string? Title { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(200)] public string? Email { get; set; }
    public bool IsPrimary { get; set; }

    [ForeignKey(nameof(CustomerId))] public Customer Customer { get; set; } = null!;
}

public class Product
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ProductCode { get; set; } = null!;
    [Required, MaxLength(200)] public string ProductName { get; set; } = null!;
    [MaxLength(100)] public string? Category { get; set; }
    [MaxLength(200)] public string? Specification { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal StandardPrice { get; set; }
    public Guid? SkuId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Quote
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string QuoteNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public DateTime QuoteDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(5,4)")] public decimal DiscountRate { get; set; } = 1.0m;
    [Column(TypeName = "decimal(14,2)")] public decimal FinalAmount { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [MaxLength(50)] public string? Status { get; set; } = "draft";
    [MaxLength(50)] public string? CustomerPoNumber { get; set; }
    public DateTime? CustomerDueDate { get; set; }
    public DateTime? ConfirmedDeliveryDate { get; set; }
    public Guid? SalesPersonId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CustomerId))] public Customer Customer { get; set; } = null!;
    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}

public class QuoteItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuoteId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(5,4)")] public decimal DiscountRate { get; set; } = 1.0m;
    [Column(TypeName = "decimal(14,2)")] public decimal Subtotal { get; set; }

    [ForeignKey(nameof(QuoteId))] public Quote Quote { get; set; } = null!;
    [ForeignKey(nameof(ProductId))] public Models.Inventory.Sku? Product { get; set; }
    [ForeignKey(nameof(SkuId))] public Models.Inventory.Sku? Sku { get; set; }
}

public class SalesOrder
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string OrderNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Guid? QuoteId { get; set; }
    public DateTime OrderDate { get; set; }
    [MaxLength(50)] public string? PaymentTerms { get; set; }
    [MaxLength(50)] public string? TradeTerms { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(12,6)")] public decimal ExchangeRate { get; set; } = 1;
    [MaxLength(20)] public string TaxType { get; set; } = "taxable";
    [MaxLength(50)] public string? CustomerPoNumber { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    [MaxLength(20)] public string ManufacturingStatus { get; set; } = "pending";
    public bool CreditChecked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CustomerId))] public Customer Customer { get; set; } = null!;
    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();

    [Column(TypeName = "decimal(14,2)")] public decimal Subtotal { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalAmount { get; set; }
    
    [MaxLength(20)] public string? TaxCode { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseSubtotal { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseTaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseTotalAmount { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    public Guid? SalespersonId { get; set; }
    public Guid? DepartmentId { get; set; }
    [MaxLength(50)] public string? SalesChannel { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? AdvanceDepositAmount { get; set; }
    [MaxLength(20)] public string? OrderType { get; set; } = "standard";
    [MaxLength(30)] public string? ProductionGroupCode { get; set; }
    [MaxLength(20)] public string? HsCode { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? Freight { get; set; }
    [MaxLength(50)] public string? ShippingMethod { get; set; }
    [MaxLength(10)] public string? Incoterms { get; set; }
    public DateTime? ConfirmedDeliveryDate { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CustomerDueDate { get; set; }
    [MaxLength(500)] public string? OrderRemarks { get; set; }
    [ForeignKey(nameof(SalespersonId))] public Employee? Salesperson { get; set; }
    [ForeignKey(nameof(DepartmentId))] public Department? Department { get; set; }
}

public class SalesOrderItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesOrderId { get; set; }
    public int LineNumber { get; set; }
    public Guid ProductId { get; set; }
    public Guid? SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(5,4)")] public decimal DiscountRate { get; set; } = 1.0m;
    [Column(TypeName = "decimal(14,2)")] public decimal Subtotal { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DeliveredQty { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";

    [MaxLength(50)] public string? CustomerItemCode { get; set; }
    public Guid? WarehouseId { get; set; }
    [MaxLength(20)] public string? TaxCode { get; set; }
    [MaxLength(20)] public string? TaxRate { get; set; }
    public Guid? BomId { get; set; }
    [MaxLength(20)] public string? BomVersion { get; set; }
    public Guid? RoutingId { get; set; }
    public Guid? WorkCenterId { get; set; }
    public bool NeedManufacturing { get; set; }
    public bool NeedOutsourcing { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedFinishDate { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BackorderQty { get; set; }
    [MaxLength(500)] public string? LineRemarks { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    [MaxLength(20)] public string? ProductionStatus { get; set; } = "pending";

    [ForeignKey(nameof(SalesOrderId))] public SalesOrder SalesOrder { get; set; } = null!;
    [ForeignKey(nameof(ProductId))] public Models.Inventory.Sku? Product { get; set; }
    [ForeignKey(nameof(SkuId))] public Models.Inventory.Sku? Sku { get; set; }
    [ForeignKey(nameof(WarehouseId))] public Models.Inventory.Warehouse? Warehouse { get; set; }
    [ForeignKey(nameof(BomId))] public Models.MES.Bom? Bom { get; set; }
    [ForeignKey(nameof(RoutingId))] public Models.MES.Routing? Routing { get; set; }
    [ForeignKey(nameof(WorkCenterId))] public Models.MES.WorkCenter? WorkCenter { get; set; }

    public ICollection<SalesOrderProductionLink> ProductionLinks { get; set; } = new List<SalesOrderProductionLink>();
}

public class DeliveryOrder
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string DeliveryNumber { get; set; } = null!;
    public Guid SalesOrderId { get; set; }
    public DateTime DeliveryDate { get; set; }
    [MaxLength(100)] public string? ShippingMethod { get; set; }
    [MaxLength(100)] public string? TrackingNumber { get; set; }
    [MaxLength(500)] public string? DeliveryAddress { get; set; }
    [MaxLength(100)] public string? SignedBy { get; set; }
    public DateTime? SignedAt { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SalesOrderId))] public SalesOrder? SalesOrder { get; set; }
    public ICollection<DeliveryOrderItem> Items { get; set; } = new List<DeliveryOrderItem>();
}

public class DeliveryOrderItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeliveryOrderId { get; set; }
    public Guid SkuId { get; set; }
    [MaxLength(50)] public string? LotNumber { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DeliveredQty { get; set; }
    public Guid? BinLocationId { get; set; }
    [MaxLength(50)] public string? SerialNumber { get; set; }

    [ForeignKey(nameof(DeliveryOrderId))] public DeliveryOrder DeliveryOrder { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Inventory.Sku? Sku { get; set; }
}

public class SalesInvoice
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string InvoiceNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? SalesOrderId { get; set; }       // 關聯銷售訂單
    public Guid? DeliveryOrderId { get; set; }    // 關聯出貨單
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(12,6)")] public decimal ExchangeRate { get; set; } = 1;
    [MaxLength(20)] public string TaxType { get; set; } = "taxable_5pct";
    [MaxLength(20)] public string? TaxCode { get; set; }
    
    [Column(TypeName = "decimal(14,2)")] public decimal SalesAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal PaidAmount { get; set; } = 0;
    
    [Column(TypeName = "decimal(14,2)")] public decimal BaseSalesAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseTaxAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal BaseTotalAmount { get; set; }

    [MaxLength(20)] public string Status { get; set; } = "draft";  // draft → approved → paid / voided
    [MaxLength(500)] public string? Remarks { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? VoidedBy { get; set; }
    public DateTime? VoidedAt { get; set; }
    [MaxLength(500)] public string? VoidReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CustomerId))] public Customer Customer { get; set; } = null!;
    public ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
}

public class SalesInvoiceItem
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesInvoiceId { get; set; }
    public Guid SkuId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Amount { get; set; }

    [ForeignKey(nameof(SalesInvoiceId))] public SalesInvoice SalesInvoice { get; set; } = null!;
    [ForeignKey(nameof(SkuId))] public Inventory.Sku? Sku { get; set; }
}

public class CashReceipt
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string ReceiptNumber { get; set; } = null!;
    public DateTime ReceiptDate { get; set; }
    public Guid CustomerId { get; set; }
    [MaxLength(30)] public string PaymentMethod { get; set; } = "wire";
    [Column(TypeName = "decimal(14,2)")] public decimal ReceivedAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal DiscountAmount { get; set; }
    public Guid? BankAccountId { get; set; }
    [MaxLength(100)] public string? ReferenceNumber { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal? HandlingFee { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CustomerId))] public Customer Customer { get; set; } = null!;
    public ICollection<CashReceiptAllocation> Allocations { get; set; } = new List<CashReceiptAllocation>();
}

public class CashReceiptAllocation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CashReceiptId { get; set; }
    public Guid SalesInvoiceId { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal AllocatedAmount { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal ExchangeGainLoss { get; set; }

    [ForeignKey(nameof(CashReceiptId))] public CashReceipt CashReceipt { get; set; } = null!;
}

public class SalesOrderProductionLink
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesOrderItemId { get; set; }
    [Required, MaxLength(20)] public string ProductionDocType { get; set; } = null!;
    public Guid ProductionDocId { get; set; }
    [MaxLength(30)] public string? ProductionDocNo { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal LinkedQty { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string? Remarks { get; set; }

    [ForeignKey(nameof(SalesOrderItemId))] public SalesOrderItem SalesOrderItem { get; set; } = null!;
}

/// <summary>
/// 月結控制檔：記錄銷售模組每個會計年月的開帳/結帳/鎖定狀態
/// </summary>
public class MonthlyPeriod
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    public int Month { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "open";   // open / closing / closed / locked
    [MaxLength(500)] public string? Notes { get; set; }
    public Guid? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? LockedBy { get; set; }
    public DateTime? LockedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 快照：月結時凍結的統計數字
    [Column(TypeName = "decimal(18,2)")] public decimal OrderAmount { get; set; }      // 月訂單總額
    [Column(TypeName = "decimal(18,2)")] public decimal InvoiceAmount { get; set; }    // 月發票總額
    [Column(TypeName = "decimal(18,2)")] public decimal CollectedAmount { get; set; }  // 月收款總額
    [Column(TypeName = "decimal(18,2)")] public decimal OutstandingAR { get; set; }    // 月末未收應收
}

