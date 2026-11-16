using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class ApSummaryDto
{
    public Guid SupplierId { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public decimal TotalPayable { get; set; }
    public int Count { get; set; }
}

public class ApAgingDto
{
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal Current { get; set; }
    public decimal Days30 { get; set; }
    public decimal Days60 { get; set; }
    public decimal Days90 { get; set; }
    public decimal Over90 { get; set; }
}

public class ApDetailDto
{
    public Guid Id { get; set; }
    public string? ApNumber { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public DateTime? SupplierInvoiceDate { get; set; }
    public string? Currency { get; set; }
    public string? PoNumber { get; set; }
    public DateTime PoDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DebitNoteAmount { get; set; }
    public decimal Outstanding { get; set; }
    public string? Status { get; set; }
}

public class RecordApPaymentRequest
{
    public Guid PurchaseOrderId { get; set; }
    public decimal PaymentAmount { get; set; }
}

public sealed record RecordAccountsPayablePaymentRequest(
    Guid AccountsPayableId,
    decimal PaymentAmount);

public sealed class CreateSupplierDebitNoteRequest
{
    public Guid AccountsPayableId { get; set; }
    public Guid IqcReportId { get; set; }
    public decimal ReturnedQty { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed record ApproveSupplierDebitNoteRequest(string ApprovalRole, string? Comments);

public sealed record SupplierDebitNoteDto(
    Guid Id,
    string DebitNoteNumber,
    Guid AccountsPayableId,
    string ApNumber,
    Guid GoodsReceiptId,
    string GrNumber,
    Guid GoodsReceiptItemId,
    Guid IqcReportId,
    string IqcReportNumber,
    Guid SupplierId,
    string SupplierName,
    Guid SkuId,
    string SkuNumber,
    string LotNumber,
    decimal ReturnedQty,
    decimal NetAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal BaseNetAmount,
    decimal BaseTaxAmount,
    decimal BaseTotalAmount,
    decimal InventoryAllocation,
    decimal CogsAllocation,
    decimal BaseInventoryAllocation,
    decimal BaseCogsAllocation,
    string Currency,
    decimal ExchangeRate,
    string Reason,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<SupplierDebitNoteApprovalDto> Approvals);

public sealed record SupplierDebitNoteApprovalDto(
    string ApprovalRole,
    Guid ApproverId,
    string? Comments,
    DateTime ApprovedAt);

public class DeliveryOrderDto
{
    public Guid Id { get; set; }
    public string? DeliveryNumber { get; set; }
    public Guid SalesOrderId { get; set; }
    public string? SalesOrderNumber { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? ShippingMethod { get; set; }
    public string? TrackingNumber { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? SignedBy { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public List<DeliveryOrderItemDto> Items { get; set; } = new();
}

public class DeliveryOrderItemDto
{
    public Guid Id { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? BinLocationId { get; set; }
    public string? LotNumber { get; set; }
    public decimal DeliveredQty { get; set; }
    public string? SerialNumber { get; set; }
}

public class CreateDeliveryOrderRequest
{
    public Guid SalesOrderId { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? ShippingMethod { get; set; }
    public string? TrackingNumber { get; set; }
    public string? DeliveryAddress { get; set; }
    public List<CreateDeliveryOrderItemRequest>? Items { get; set; }
}

public class CreateDeliveryOrderItemRequest
{
    public Guid SkuId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? BinLocationId { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
}

public class UpdateDeliveryStatusRequest
{
    public string Status { get; set; } = null!;
}

public class SignDeliveryRequest
{
    public string SignedBy { get; set; } = null!;
}

public class RfqDto
{
    public Guid Id { get; set; }
    public string? RfqNumber { get; set; }
    public Guid? PrId { get; set; }
    public DateTime RfqDate { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public int ResponseCount { get; set; }
    public List<RfqItemDto> Items { get; set; } = new();
    public List<RfqSupplierResponseDto> SupplierResponses { get; set; } = new();
}

public class RfqItemDto
{
    public Guid Id { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Specifications { get; set; }
}

public class RfqSupplierResponseDto
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid SkuId { get; set; }
    public decimal UnitPrice { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? Moq { get; set; }
    public string? TradeTerms { get; set; }
    public string? Notes { get; set; }
    public bool? IsSelected { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRfqItemRequest
{
    public Guid SkuId { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Specifications { get; set; }
}

public class UpdateRfqStatusRequest
{
    public string Status { get; set; } = null!;
}

public class AddRfqResponseRequest
{
    public Guid SupplierId { get; set; }
    public Guid SkuId { get; set; }
    public decimal UnitPrice { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? Moq { get; set; }
    public string? TradeTerms { get; set; }
    public string? Notes { get; set; }
}

public class SupplierQuoteDto
{
    public Guid Id { get; set; }
    public Guid RfqId { get; set; }
    public string? RfqNumber { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public decimal UnitPrice { get; set; }
    public int? LeadTimeDays { get; set; }
    public decimal? Moq { get; set; }
    public string? TradeTerms { get; set; }
    public string? Notes { get; set; }
    public bool? IsSelected { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string? PoNumber { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public DateTime PoDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public string? Status { get; set; }
    public string? Currency { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}

public class PurchaseOrderItemDto
{
    public Guid Id { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ReceivedQty { get; set; }
    public string? Status { get; set; }
    public bool LotControlled { get; set; }
}

public class GoodsReceiptDto
{
    public Guid Id { get; set; }
    public string? GrNumber { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Status { get; set; }
    public string? IqcStatus { get; set; }
}

/// <summary>Reason required to reverse a pending goods receipt.</summary>
public sealed record ReverseGoodsReceiptDto
{
    [Required, MaxLength(400)]
    public string Reason { get; init; } = string.Empty;
}

/// <summary>Audit details and compensating transactions created for a goods receipt reversal.</summary>
public sealed record GoodsReceiptReversalDto(
    Guid GoodsReceiptId,
    string GoodsReceiptNumber,
    Guid PurchaseOrderId,
    string PurchaseOrderStatus,
    Guid ReversedBy,
    DateTimeOffset ReversedAt,
    string Reason,
    IReadOnlyList<string> ReversalTransactionIds);

public class CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; set; }
    public Guid? PrId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();
}

public class CreatePurchaseOrderItemRequest
{
    public Guid SkuId { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
}

public class ReceivePurchaseOrderRequest
{
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? AsnId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public Guid? BinLocationId { get; set; }
    public List<ReceivePurchaseOrderItemRequestDto>? Items { get; set; }
}

public class ReceivePurchaseOrderItemRequestDto
{
    public Guid PurchaseOrderItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? LotNumber { get; set; }
}

public class PurchaseRequisitionDto
{
    public Guid Id { get; set; }
    public string? PrNumber { get; set; }
    public DateTime RequestDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? RequesterId { get; set; }
    public string? RequesterName { get; set; }
    public string? ProjectCode { get; set; }
    public string? Purpose { get; set; }
    public string? Priority { get; set; }
    public string? BudgetAccount { get; set; }
    public string? Status { get; set; }
    public bool IsLocked { get; set; }
    public decimal TotalEstimatedAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public List<PurchaseRequisitionItemDto> Items { get; set; } = new();
}

public class PurchaseRequisitionItemDto
{
    public Guid Id { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal EstimatedUnitPrice { get; set; }
    public string? Currency { get; set; }
    public DateTime? RequiredDate { get; set; }
    public string? Notes { get; set; }
}

public class CreatePurchaseRequisitionRequest
{
    public Guid? DepartmentId { get; set; }
    public Guid? RequesterId { get; set; }
    public string? ProjectCode { get; set; }
    public string? Purpose { get; set; }
    public string? Priority { get; set; } = "normal";
    public string? BudgetAccount { get; set; }
    public DateTime? PrDate { get; set; }
    public List<CreatePurchaseRequisitionItemRequest> Items { get; set; } = new();
}

public class CreatePurchaseRequisitionItemRequest
{
    public Guid SkuId { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal EstimatedUnitPrice { get; set; }
    public string? Currency { get; set; } = "TWD";
    public DateTime? RequiredDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePurchaseRequisitionRequest
{
    public string? Purpose { get; set; }
    public string? Priority { get; set; }
    public string? BudgetAccount { get; set; }
    public DateTime? PrDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? RequesterId { get; set; }
    public string? ProjectCode { get; set; }
    public List<UpdatePurchaseRequisitionItemRequest>? Items { get; set; }
}

public class UpdatePurchaseRequisitionItemRequest
{
    public Guid SkuId { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal EstimatedUnitPrice { get; set; }
    public string? Currency { get; set; } = "TWD";
    public DateTime? RequiredDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdatePrStatusRequest
{
    public string Status { get; set; } = null!;
}

public class ApprovalActionRequest
{
    public Guid EmployeeId { get; set; }
    public string? Comments { get; set; }
}

public class ApprovalHistoryDto
{
    public Guid Id { get; set; }
    public int Level { get; set; }
    public string? ApproverName { get; set; }
    public string? Action { get; set; }
    public string? Comments { get; set; }
    public DateTime ActionDate { get; set; }
}

public class ConvertToPoRequest
{
    public Guid SupplierId { get; set; }
}

public class CreateRfqRequest
{
    public Guid? PrId { get; set; }
    public DateTime? RfqDate { get; set; }
    public DateTime? Deadline { get; set; }
    public List<CreateRfqItemRequest> Items { get; set; } = new();
}
