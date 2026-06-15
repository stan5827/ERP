namespace ErpBackend.DTOs;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public int? TotalCount { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public static ApiResponse<T> Ok(T data, string? message = null, int? totalCount = null, int? page = null, int? pageSize = null)
        => new() { Success = true, Data = data, Message = message, TotalCount = totalCount, Page = page, PageSize = pageSize };
    public static ApiResponse<T> Fail(string message)
        => new() { Success = false, Message = message };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string? message = null)
        => new() { Success = true, Message = message };
    public static ApiResponse Ok(object data, string? message = null)
        => new() { Success = true, Data = data, Message = message };
    public static new ApiResponse Fail(string message)
        => new() { Success = false, Message = message };
}

public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = true;
}

// ========== Auth DTOs ==========
public class LoginRequest { public string Username { get; set; } = null!; public string Password { get; set; } = null!; }
public class LoginResponse { 
    public Guid UserId { get; set; } 
    public string Username { get; set; } = null!; 
    public string? Email { get; set; } 
    public string Token { get; set; } = null!; 
    public DateTime ExpiresAt { get; set; } 
    public List<string> Roles { get; set; } = new(); 
    public List<string> Permissions { get; set; } = new(); 
    public bool MustChangePassword { get; set; }
}

// ========== System DTOs ==========
public class UserDto { public Guid Id { get; set; } public string Username { get; set; } = null!; public string? Email { get; set; } public Guid? EmployeeId { get; set; } public bool IsActive { get; set; } public DateTime? LastLoginAt { get; set; } public DateTime CreatedAt { get; set; } public List<string> Roles { get; set; } = new(); }
public class CreateUserRequest { public string Username { get; set; } = null!; public string Password { get; set; } = null!; public string? Email { get; set; } public Guid? EmployeeId { get; set; } public List<Guid> RoleIds { get; set; } = new(); }
public class UpdateUserRequest { public string? Email { get; set; } public Guid? EmployeeId { get; set; } public bool? IsActive { get; set; } public List<Guid>? RoleIds { get; set; } }
public class RoleDto { public Guid Id { get; set; } public string RoleName { get; set; } = null!; public string? Description { get; set; } public bool IsSystem { get; set; } public List<string> Permissions { get; set; } = new(); }
public class CreateRoleRequest { public string RoleName { get; set; } = null!; public string? Description { get; set; } public List<Guid> PermissionIds { get; set; } = new(); }

// ========== Inventory DTOs ==========
public class SkuDto {
    public Guid Id { get; set; }
    public string SkuNumber { get; set; } = null!;
    public string SkuName { get; set; } = null!;
    public string? Unit { get; set; }
    public string Status { get; set; } = "active";
    public decimal PurchasePrice { get; set; }
    public decimal StandardCost { get; set; }
    public decimal Qty { get; set; }
    public string ItemType { get; set; } = "purchased";
    public Guid? DefaultWarehouseId { get; set; }
    public string? DefaultWarehouseName { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal MaxStock { get; set; }
    public int LeadTimeDays { get; set; }
    public string? Category { get; set; }
    public bool LotControlled { get; set; }
    public bool SerialControlled { get; set; }
    public string? Specification { get; set; }
    public string? Barcode { get; set; }
    public string? Model { get; set; }
    public string? Brand { get; set; }
    public string SourceType { get; set; } = "buy";
    public string? AbcClassification { get; set; }
    public Guid? DefaultBinLocationId { get; set; }
    public string? DefaultBinLocationCode { get; set; }
    public bool IsSellable { get; set; }
    public bool IsPurchasable { get; set; }
    public bool IsManufacturable { get; set; }
    public bool IsOutsourced { get; set; }
    public decimal? PriceA { get; set; }
    public decimal? PriceB { get; set; }
    public decimal? PriceC { get; set; }
    public decimal? PriceD { get; set; }
    public decimal? PriceE { get; set; }
    public string? Currency { get; set; }
    public string? CostMethod { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsMto { get; set; }
    public int? SupplierLeadTime { get; set; }
    public int? ShelfLifeDays { get; set; }
    public Guid? DefaultBomId { get; set; }
    public Guid? DefaultRoutingId { get; set; }
    public bool IsBomTop { get; set; }
    public bool IsBomComponent { get; set; }
    public bool IsPhantom { get; set; }
    public bool IsMts { get; set; }
    public bool IsCustom { get; set; }
    public string? MakeOrBuy { get; set; }
    public Guid? DefaultSupplierId { get; set; }
    public string? DefaultSupplierName { get; set; }
    public string? SupplierItemCode { get; set; }
    public string? DrawingNo { get; set; }
    public string? DrawingRevision { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? QualitySpec { get; set; }
    public string? ProductionDept { get; set; }
    public string? MachineCode { get; set; }
    public string? MoldCode { get; set; }
    public string? CostUnit { get; set; }
    public Guid? CostUomId { get; set; }
    public decimal LatestPurchaseCost { get; set; }
    public string? TaxCode { get; set; }
    public string? TaxRate { get; set; }
    public bool IsSubstitute { get; set; }
    public bool IsAlternate { get; set; }
    public bool IqcRequired { get; set; }
    public bool OqcRequired { get; set; }
    public string? Color { get; set; }
    public string? PackingMethod { get; set; }
    public int? MinPackingQty { get; set; }
    public bool IsStocked { get; set; }
    public string? MaterialType { get; set; }
    public string? DesignOwner { get; set; }
    public decimal AvgDailyConsumption { get; set; }
    public decimal? PurchasePriceCeiling { get; set; }
    public decimal? FloorPrice { get; set; }
    public decimal? DiscountRate { get; set; }
    public string? CommissionAccount { get; set; }
    public string? RelatedSkuId { get; set; }
    public string? CustomerItemCode { get; set; }
    public string? BomType { get; set; }
    public string? RoutingId { get; set; }
    public string? SubstituteSkuId { get; set; }
    public Guid? OutsourceSupplierId { get; set; }
    public string? ProcessingFeeAccount { get; set; }
    public Guid? OutsourceWarehouseId { get; set; }
    public bool IsSupplierProvided { get; set; }
    public Guid? PurchaseUomId { get; set; }
    public Guid? SalesUomId { get; set; }
    public Guid? ProductionUomId { get; set; }
    public decimal? PurchaseToBaseFactor { get; set; }
    public decimal? SalesToBaseFactor { get; set; }
    public decimal? ProductionToBaseFactor { get; set; }
}
public class CreateSkuRequest { public string SkuNumber { get; set; } = null!; public string SkuName { get; set; } = null!; public string? Unit { get; set; } = "PCS"; }
public class WarehouseDto { public Guid Id { get; set; } public string WarehouseCode { get; set; } = null!; public string WarehouseName { get; set; } = null!; public bool IsActive { get; set; } }
public class BinLocationDto { public Guid Id { get; set; } public Guid WarehouseId { get; set; } public string LocationCode { get; set; } = null!; public bool IsActive { get; set; } }
public class InventoryTransactionDto { public Guid Id { get; set; } public string TransactionNumber { get; set; } = null!; public Guid SkuId { get; set; } public string Type { get; set; } = null!; public decimal QtyChange { get; set; } public DateTime CreatedAt { get; set; } }

// ========== Sales DTOs ==========
public class CustomerDto { public Guid Id { get; set; } public string CustomerCode { get; set; } = null!; public string CustomerName { get; set; } = null!; public bool IsActive { get; set; } }
public class SalesOrderDto {
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "draft";
    public string? CustomerPoNumber { get; set; }
    public Guid? SalespersonId { get; set; }
    public string? SalespersonName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SalesOrderItemDto> Items { get; set; } = new();
}
public class SalesOrderItemDto { public Guid Id { get; set; } public Guid? SkuId { get; set; } public string? SkuNumber { get; set; } public string? SkuName { get; set; } public decimal Quantity { get; set; } public decimal UnitPrice { get; set; } public decimal Subtotal { get; set; } public int LineNumber { get; set; } public string Status { get; set; } = "pending"; public DateTime? PlannedFinishDate { get; set; } }
public class CreateQuoteRequest { public Guid CustomerId { get; set; } public DateTime QuoteDate { get; set; } public DateTime? ValidUntil { get; set; } public decimal DiscountRate { get; set; } = 0; public string Currency { get; set; } = "TWD"; public string? CustomerPoNumber { get; set; } public DateTime? CustomerDueDate { get; set; } public DateTime? ConfirmedDeliveryDate { get; set; } public List<CreateQuoteItemRequest> Items { get; set; } = new(); }
public class CreateQuoteItemRequest { public Guid? ProductId { get; set; } public Guid? SkuId { get; set; } public decimal Quantity { get; set; } public decimal UnitPrice { get; set; } public decimal DiscountRate { get; set; } = 0; }
public class QuoteDto { public Guid Id { get; set; } public string QuoteNumber { get; set; } = null!; public Guid CustomerId { get; set; } public string? CustomerName { get; set; } public DateTime QuoteDate { get; set; } public DateTime? ValidUntil { get; set; } public decimal TotalAmount { get; set; } public decimal Subtotal { get; set; } public decimal TaxAmount { get; set; } public decimal DiscountRate { get; set; } public decimal FinalAmount { get; set; } public string Currency { get; set; } = "TWD"; public string Status { get; set; } = "draft"; public string? CustomerPoNumber { get; set; } public DateTime? CustomerDueDate { get; set; } public DateTime? ConfirmedDeliveryDate { get; set; } public string? Notes { get; set; } public DateTime CreatedAt { get; set; } public List<QuoteItemDto> Items { get; set; } = new(); }
public class QuoteItemDto { public Guid Id { get; set; } public Guid QuoteId { get; set; } public Guid? ProductId { get; set; } public Guid? SkuId { get; set; } public string? SkuNumber { get; set; } public decimal Quantity { get; set; } public decimal UnitPrice { get; set; } public decimal DiscountRate { get; set; } public decimal Subtotal { get; set; } }

// ========== Delivery Order DTOs ==========
public class DeliveryOrderDto { public Guid Id { get; set; } public string DeliveryNumber { get; set; } = null!; public Guid SalesOrderId { get; set; } public string? SalesOrderNumber { get; set; } public string? CustomerName { get; set; } public DateTime DeliveryDate { get; set; } public string? ShippingMethod { get; set; } public string? TrackingNumber { get; set; } public string? DeliveryAddress { get; set; } public string? SignedBy { get; set; } public DateTime? SignedAt { get; set; } public string Status { get; set; } = "pending"; public DateTime CreatedAt { get; set; } public int ItemCount { get; set; } public List<DeliveryOrderItemDto> Items { get; set; } = new(); }
public class DeliveryOrderItemDto { public Guid Id { get; set; } public Guid? SkuId { get; set; } public string? SkuNumber { get; set; } public string? SkuName { get; set; } public string? LotNumber { get; set; } public decimal DeliveredQty { get; set; } public string? SerialNumber { get; set; } }
public class CreateDeliveryOrderRequest { public Guid SalesOrderId { get; set; } public DateTime? DeliveryDate { get; set; } public string? ShippingMethod { get; set; } public string? TrackingNumber { get; set; } public string? DeliveryAddress { get; set; } public List<CreateDeliveryOrderItemRequest>? Items { get; set; } }
public class CreateDeliveryOrderItemRequest { public Guid SkuId { get; set; } public decimal Quantity { get; set; } public string? LotNumber { get; set; } public string? SerialNumber { get; set; } }
public class UpdateStatusRequest { public string Status { get; set; } = null!; public string? Notes { get; set; } }
public class SignDeliveryRequest { public string SignedBy { get; set; } = null!; }
public class UpdateDeliveryStatusRequest { public string Status { get; set; } = null!; }
public class UpdatePrStatusRequest { public string Status { get; set; } = null!; }
public class UpdateRfqStatusRequest { public string Status { get; set; } = null!; }
public class ApprovalActionRequest { public Guid EmployeeId { get; set; } public string? Comments { get; set; } }
public class ApprovalHistoryDto { public Guid Id { get; set; } public int Level { get; set; } public string? ApproverName { get; set; } public string Action { get; set; } = null!; public string? Comments { get; set; } public DateTime ActionDate { get; set; } }

// ========== RFQ DTOs ==========
public class RfqDto { public Guid Id { get; set; } public string RfqNumber { get; set; } = null!; public Guid? PrId { get; set; } public DateTime RfqDate { get; set; } public DateTime? Deadline { get; set; } public string Status { get; set; } = "open"; public DateTime CreatedAt { get; set; } public int ItemCount { get; set; } public int ResponseCount { get; set; } public List<RfqItemDto> Items { get; set; } = new(); public List<RfqSupplierResponseDto> SupplierResponses { get; set; } = new(); }
public class RfqItemDto { public Guid Id { get; set; } public Guid SkuId { get; set; } public string? SkuNumber { get; set; } public string? SkuName { get; set; } public decimal Quantity { get; set; } public string? Unit { get; set; } public string? Specifications { get; set; } }
public class RfqSupplierResponseDto { public Guid Id { get; set; } public Guid SupplierId { get; set; } public string? SupplierName { get; set; } public decimal UnitPrice { get; set; } public int LeadTimeDays { get; set; } public decimal Moq { get; set; } public string? TradeTerms { get; set; } public string? Notes { get; set; } public bool IsSelected { get; set; } }
public class CreateRfqRequest { public Guid? PrId { get; set; } public DateTime? RfqDate { get; set; } public DateTime? Deadline { get; set; } public List<CreateRfqItemRequest>? Items { get; set; } }
public class CreateRfqItemRequest { public Guid SkuId { get; set; } public decimal Quantity { get; set; } public string? Unit { get; set; } public string? Specifications { get; set; } }
public class AddRfqResponseRequest { public Guid SupplierId { get; set; } public Guid SkuId { get; set; } public decimal UnitPrice { get; set; } public int LeadTimeDays { get; set; } public decimal Moq { get; set; } public string? TradeTerms { get; set; } public string? Notes { get; set; } }

// ========== Supplier Quote DTOs ==========
public class SupplierQuoteDto { public Guid Id { get; set; } public Guid RfqId { get; set; } public string? RfqNumber { get; set; } public Guid SupplierId { get; set; } public string? SupplierName { get; set; } public Guid SkuId { get; set; } public string? SkuNumber { get; set; } public string? SkuName { get; set; } public decimal UnitPrice { get; set; } public int LeadTimeDays { get; set; } public decimal Moq { get; set; } public string? TradeTerms { get; set; } public string? Notes { get; set; } public bool IsSelected { get; set; } }

// ========== Procurement DTOs ==========
public class SupplierDto { 
    public Guid Id { get; set; } 
    public string SupplierCode { get; set; } = null!; 
    public string SupplierName { get; set; } = null!; 
    public string? TaxId { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Status { get; set; }
    public string? Currency { get; set; }
    public string? Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSupplierRequest
{
    [Required] public string SupplierCode { get; set; } = null!;
    [Required] public string SupplierName { get; set; } = null!;
    public string? TaxId { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? InvoiceAddress { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Currency { get; set; } = "TWD";
}

public class SupplierPortalUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = "viewer";
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSupplierPortalUserRequest
{
    [Required] public string Username { get; set; } = null!;
    [Required] public string Password { get; set; } = null!;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string Role { get; set; } = "viewer";
}
public class PurchaseOrderDto { public Guid Id { get; set; } public string PoNumber { get; set; } = null!; public Guid SupplierId { get; set; } public string? SupplierCode { get; set; } public string? SupplierName { get; set; } public DateTime PoDate { get; set; } public decimal TotalAmount { get; set; } public string Status { get; set; } = "draft"; public DateTime CreatedAt { get; set; } public List<PurchaseOrderItemDto> Items { get; set; } = new(); }
public class PurchaseOrderItemDto { public Guid Id { get; set; } public Guid SkuId { get; set; } public string? SkuNumber { get; set; } public decimal Quantity { get; set; } public decimal UnitPrice { get; set; } public decimal Subtotal { get; set; } public decimal ReceivedQty { get; set; } }
public class CreatePurchaseOrderRequest { public Guid SupplierId { get; set; } public DateTime PoDate { get; set; } public DateTime? ExpectedDeliveryDate { get; set; } public Guid? DeliveryWarehouseId { get; set; } public string Currency { get; set; } = "TWD"; public decimal ExchangeRate { get; set; } = 1; public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new(); }
public class CreatePurchaseOrderItemRequest { public Guid SkuId { get; set; } public decimal Quantity { get; set; } public decimal UnitPrice { get; set; } public string? Unit { get; set; } }
public class PurchaseRequisitionDto { public Guid Id { get; set; } public string PrNumber { get; set; } = null!; public DateTime RequestDate { get; set; } public DateTime PrDate => RequestDate; public Guid? DepartmentId { get; set; } public string? DepartmentName { get; set; } public Guid? RequesterId { get; set; } public string? RequesterName { get; set; } public string? ProjectCode { get; set; } public string? Purpose { get; set; } public string Priority { get; set; } = "normal"; public string? BudgetAccount { get; set; } public string Status { get; set; } = "pending"; public bool IsLocked { get; set; } public decimal TotalEstimatedAmount { get; set; } public DateTime CreatedAt { get; set; } public int ItemCount { get; set; } public List<PurchaseRequisitionItemDto> Items { get; set; } = new(); }
public class PurchaseRequisitionItemDto { public Guid Id { get; set; } public Guid SkuId { get; set; } public string? SkuNumber { get; set; } public string? SkuName { get; set; } public decimal Quantity { get; set; } public string Unit { get; set; } = "PCS"; public decimal EstimatedUnitPrice { get; set; } public string Currency { get; set; } = "TWD"; public DateTime? RequiredDate { get; set; } public string? Notes { get; set; } }
public class CreatePurchaseRequisitionRequest { public DateTime? PrDate { get; set; } public Guid? DepartmentId { get; set; } public Guid? RequesterId { get; set; } public string? ProjectCode { get; set; } public string? Purpose { get; set; } public string Priority { get; set; } = "normal"; public string? BudgetAccount { get; set; } public List<CreatePurchaseRequisitionItemRequest>? Items { get; set; } }
public class CreatePurchaseRequisitionItemRequest { public Guid SkuId { get; set; } public decimal Quantity { get; set; } public string? Unit { get; set; } public decimal EstimatedUnitPrice { get; set; } public string Currency { get; set; } = "TWD"; public DateTime? RequiredDate { get; set; } public string? Notes { get; set; } }
public class UpdatePurchaseRequisitionRequest { public DateTime? PrDate { get; set; } public Guid? DepartmentId { get; set; } public Guid? RequesterId { get; set; } public string? ProjectCode { get; set; } public string? Purpose { get; set; } public string? Priority { get; set; } public string? BudgetAccount { get; set; } public List<CreatePurchaseRequisitionItemRequest>? Items { get; set; } }

public class GoodsReceiptDto { public Guid Id { get; set; } public string GrNumber { get; set; } = null!; public Guid? PurchaseOrderId { get; set; } public string? PurchaseOrderNumber { get; set; } public Guid? AsnId { get; set; } public string? AsnNumber { get; set; } public Guid SupplierId { get; set; } public string? SupplierName { get; set; } public Guid WarehouseId { get; set; } public string? WarehouseName { get; set; } public DateTime GrDate { get; set; } public string Status { get; set; } = "received"; public string IqcStatus { get; set; } = "pending"; public string? SupplierInvoiceNo { get; set; } public string? PackingListNo { get; set; } public decimal TotalAmount { get; set; } public List<GoodsReceiptItemDto> Items { get; set; } = new(); }
public class GoodsReceiptItemDto { public Guid Id { get; set; } public Guid SkuId { get; set; } public string? SkuNumber { get; set; } public decimal ReceivedQty { get; set; } public decimal AcceptedQty { get; set; } public decimal RejectedQty { get; set; } public string IqcResult { get; set; } = "pending"; public string? LotNumber { get; set; } }
public class ReceiveGoodsReceiptRequest { public string? WarehouseCode { get; set; } public string? SupplierInvoiceNo { get; set; } public string? PackingListNo { get; set; } public string? Notes { get; set; } }
public class UpdateGoodsReceiptIqcRequest { public string Result { get; set; } = "pass"; public decimal? DefectQty { get; set; } public decimal? SampleQty { get; set; } public string? LotNumber { get; set; } public string? Notes { get; set; } public Guid? InspectorId { get; set; } }
public class AllocateGoodsReceiptCostsRequest { public decimal TotalTariff { get; set; } public decimal TotalFreight { get; set; } public bool AllocateByAmount { get; set; } = true; }
public class CreateIqcReportRequest { public Guid GoodsReceiptId { get; set; } public string Result { get; set; } = "pass"; public decimal SampleQty { get; set; } public decimal DefectQty { get; set; } public string? LotNumber { get; set; } public string? Notes { get; set; } public Guid? InspectorId { get; set; } }
public class AccountsPayableDto { public Guid Id { get; set; } public string ApNumber { get; set; } = null!; public Guid SupplierId { get; set; } public string? SupplierName { get; set; } public Guid? PurchaseOrderId { get; set; } public string? PurchaseOrderNumber { get; set; } public Guid? GoodsReceiptId { get; set; } public string? GoodsReceiptNumber { get; set; } public string? SupplierInvoiceNumber { get; set; } public DateTime? SupplierInvoiceDate { get; set; } public string Currency { get; set; } = "TWD"; public decimal InvoiceAmount { get; set; } public decimal TaxAmount { get; set; } public decimal TotalAmount { get; set; } public DateTime? DueDate { get; set; } public string PaymentStatus { get; set; } = "unpaid"; public string VerificationStatus { get; set; } = "pending"; public string ThreeWayMatchResult { get; set; } = "pending"; public string? Notes { get; set; } public DateTime CreatedAt { get; set; } }
public class CreateSupplierComplaintRequest { public Guid SupplierId { get; set; } public Guid? PurchaseOrderId { get; set; } public Guid? SkuId { get; set; } public string? LotNumber { get; set; } public decimal DefectQty { get; set; } public string? DefectType { get; set; } public string Priority { get; set; } = "medium"; public string? Description { get; set; } }
public class CreateSupplierCapaRequest { public string? D1Team { get; set; } public string? ProblemDescription { get; set; } public string? InterimContainment { get; set; } public string? RootCause { get; set; } public string? PermanentAction { get; set; } public string? Verification { get; set; } public string? Prevention { get; set; } public string? Closure { get; set; } public DateTime? SupplierResponseDue { get; set; } }
public class CreateNonConformanceReportRequest { public Guid GoodsReceiptId { get; set; } public Guid? SkuId { get; set; } public decimal DefectQty { get; set; } public string? DefectType { get; set; } public string? DefectDescription { get; set; } public string? Disposition { get; set; } public decimal CostImpact { get; set; } }
public class CloseNonConformanceReportRequest { public string? Disposition { get; set; } public bool ReleaseStock { get; set; } = true; public string? Notes { get; set; } }
public class CloseSupplierCapaRequest { public string? Closure { get; set; } public string? Verification { get; set; } }
public class UpdateQualityOwnershipRequest { public string? ResponsiblePerson { get; set; } public string? AttachmentUrl { get; set; } public string? Notes { get; set; } }
public class QaReviewLogDto { public Guid Id { get; set; } public string TargetType { get; set; } = null!; public Guid TargetId { get; set; } public string ActionType { get; set; } = null!; public string? ActorName { get; set; } public string? ResponsiblePerson { get; set; } public string? Notes { get; set; } public string? AttachmentUrl { get; set; } public DateTime CreatedAt { get; set; } }

// ========== MES DTOs ==========
public class WorkOrderDto { public Guid Id { get; set; } public string WoNumber { get; set; } = null!; public Guid ProductSkuId { get; set; } public string? ProductSkuName { get; set; } public string Status { get; set; } = "pending"; public decimal PlannedQty { get; set; } public decimal ActualQty { get; set; } public DateTime? PlannedStartDate { get; set; } public DateTime? PlannedEndDate { get; set; } public DateTime? ActualStartDate { get; set; } public DateTime? ActualEndDate { get; set; } public decimal ScrapQty { get; set; } public string Priority { get; set; } = "normal"; public DateTime CreatedAt { get; set; } }
public class BomDto { public Guid Id { get; set; } public string BomCode { get; set; } = null!; public Guid ProductSkuId { get; set; } public string? ProductSkuNumber { get; set; } public List<BomItemDto> Items { get; set; } = new(); }
public class BomItemDto { public Guid Id { get; set; } public Guid ChildSkuId { get; set; } public string? ChildSkuNumber { get; set; } public string? ChildSkuName { get; set; } public decimal Quantity { get; set; } public string Unit { get; set; } = "PCS"; }

public class CreateBomItemRequest
{
    public Guid ChildSkuId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "PCS";
    public decimal ScrapRate { get; set; }
    public int OperationSeq { get; set; }
}

public class CreateBomRequest
{
    public string BomCode { get; set; } = null!;
    public Guid ProductSkuId { get; set; }
    public string Version { get; set; } = "1.0";
    public string Status { get; set; } = "active";
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public List<CreateBomItemRequest> Items { get; set; } = new();
}

public class UpdateBomRequest
{
    public string? BomCode { get; set; }
    public string? Version { get; set; }
    public string? Status { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? Notes { get; set; }
    public List<CreateBomItemRequest>? Items { get; set; }
}

public class WorkOrderCostAnalysisDto
{
    public Guid WorkOrderId { get; set; }
    public string WoNumber { get; set; } = null!;
    public decimal ActualMaterialCost { get; set; }
    public decimal ActualLaborCost { get; set; }
    public decimal ActualOverheadCost { get; set; }
    public decimal TotalActualCost { get; set; }
    public decimal StandardMaterialCost { get; set; }
    public decimal StandardLaborCost { get; set; }
    public decimal TotalStandardCost { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal VariancePercentage { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ActualQty { get; set; }
}

public class WorkOrderCostSummaryDto
{
    public Guid Id { get; set; }
    public string WoNumber { get; set; } = null!;
    public string ProductSkuName { get; set; } = "";
    public string ProductSkuNumber { get; set; } = "";
    public decimal ActualQty { get; set; }
    public string Status { get; set; } = "";
    public decimal ActualMaterialCost { get; set; }
    public decimal ActualLaborCost { get; set; }
    public decimal ActualOverheadCost { get; set; }
    public decimal TotalStandardCost { get; set; }
}

// ========== Dashboard DTOs ==========
public class DashboardDto { 
    public List<KpiCardDto> KpiCards { get; set; } = new(); 
    public List<AlertDto> Alerts { get; set; } = new(); 
    public List<WorkOrderSummaryDto> RecentWorkOrders { get; set; } = new(); 
    public List<SalesOrderDto> RecentSalesOrders { get; set; } = new(); 
    public List<ProductionTrendDto> ProductionTrend { get; set; } = new();
    public List<AttendanceTrendDto> AttendanceTrend { get; set; } = new();
    public SidebarBadgeDto SidebarBadges { get; set; } = new(); 
    public int TodayAttendanceCount { get; set; }
    public int PendingApprovals { get; set; }
    public int TotalEmployees { get; set; }
    public int ActiveAlerts { get; set; }
}
public class KpiCardDto { public string Title { get; set; } = null!; public string Value { get; set; } = null!; public string? Unit { get; set; } public decimal? ChangePercent { get; set; } public string? ChangeDirection { get; set; } public string? Color { get; set; } }
public class AlertDto { public Guid Id { get; set; } public string Title { get; set; } = null!; public string Description { get; set; } = null!; public string Severity { get; set; } = "info"; public DateTime CreatedAt { get; set; } }
public class WorkOrderSummaryDto { public Guid Id { get; set; } public string WoNumber { get; set; } = null!; public string ProductName { get; set; } = null!; public decimal PlannedQty { get; set; } public decimal CompletedQty { get; set; } public decimal Progress => PlannedQty > 0 ? (CompletedQty / PlannedQty) * 100 : 0; public string Status { get; set; } = null!; public string Priority { get; set; } = "normal"; public DateTime? DueDate { get; set; } }
public class SidebarBadgeDto { public int SalesCount { get; set; } public int ProcurementCount { get; set; } public int HrCount { get; set; } public int InventoryCount { get; set; } public int ManufacturingCount { get; set; } public int QualityCount { get; set; } }
public class ProductionTrendDto { public string Date { get; set; } = null!; public decimal Planned { get; set; } public decimal Actual { get; set; } public decimal Efficiency { get; set; } }
public class AttendanceTrendDto { public string Date { get; set; } = null!; public int Present { get; set; } public int Late { get; set; } public int Absent { get; set; } public int Leave { get; set; } }

// ========== System Settings DTOs ==========
public class UserThemePreferenceDto { public string ThemeName { get; set; } = "modern-dark"; public string? CustomPrimaryColor { get; set; } public string? CustomAccentColor { get; set; } }
public class UpdateThemeRequest { public string ThemeName { get; set; } = null!; public string? CustomPrimaryColor { get; set; } public string? CustomAccentColor { get; set; } }
public class ModuleAccessDto { public string ModuleId { get; set; } = null!; public List<string> Permissions { get; set; } = new(); }
public class PermissionTreeDto { public Guid Id { get; set; } public string PermissionCode { get; set; } = null!; public string ModuleName { get; set; } = null!; public string ActionType { get; set; } = null!; public string PermissionType { get; set; } = null!; public string? Description { get; set; } public int SortOrder { get; set; } public List<PermissionTreeDto> Children { get; set; } = new(); }
public class UserWithRolesDto { public Guid Id { get; set; } public string Username { get; set; } = null!; public string? Email { get; set; } public bool IsActive { get; set; } public DateTime CreatedAt { get; set; } public List<RoleDto> Roles { get; set; } = new(); }
public class RoleWithPermissionsDto { public Guid Id { get; set; } public string RoleName { get; set; } = null!; public string? Description { get; set; } public bool IsSystem { get; set; } public DateTime CreatedAt { get; set; } public List<Guid> PermissionIds { get; set; } = new(); }
public class UpdateUserRolesRequest { public List<Guid> RoleIds { get; set; } = new(); }
public class UpdateRolePermissionsRequest { public List<Guid> PermissionIds { get; set; } = new(); }
public class ThemeDto { public string Name { get; set; } = "custom"; public string DisplayName { get; set; } = null!; public string? Description { get; set; } public bool IsDefault { get; set; } public ThemeColorsDto? Colors { get; set; } }
public class ThemeColorsDto { public string Primary { get; set; } = "#2d6ef5"; public string Accent { get; set; } = "#2d6ef5"; public string Background { get; set; } = "#04080f"; public string Surface { get; set; } = "#0d1829"; public string Text { get; set; } = "#e8eef8"; public string Border { get; set; } = "#1a3050"; }
public class CreatePermissionRequest { public string PermissionCode { get; set; } = null!; public string ModuleName { get; set; } = null!; public string ActionType { get; set; } = null!; public string PermissionType { get; set; } = "action"; public string? Description { get; set; } public Guid? ParentId { get; set; } public int SortOrder { get; set; } }
public class UpdatePermissionRequest { public string PermissionCode { get; set; } = null!; public string ModuleName { get; set; } = null!; public string ActionType { get; set; } = null!; public string PermissionType { get; set; } = "action"; public string? Description { get; set; } public Guid? ParentId { get; set; } public int SortOrder { get; set; } }

// ========== HR DTOs ==========
public class EmployeeDto { public Guid Id { get; set; } public string EmployeeNo { get; set; } = null!; public string ChineseName { get; set; } = null!; public string Status { get; set; } = "active"; }
public class DepartmentDto { public Guid Id { get; set; } public string DeptCode { get; set; } = null!; public string DeptName { get; set; } = null!; public bool IsActive { get; set; } }
public class AttendanceRecordDto { public Guid Id { get; set; } public Guid EmployeeId { get; set; } public string? EmployeeName { get; set; } public Guid? DepartmentId { get; set; } public string? DepartmentName { get; set; } public DateTime Date { get; set; } public string? ShiftType { get; set; } public string? ClockIn { get; set; } public string? ClockOut { get; set; } public string Status { get; set; } = "normal"; public bool LateFlag { get; set; } public int LateMinutes { get; set; } public bool EarlyLeaveFlag { get; set; } public int EarlyLeaveMinutes { get; set; } public decimal Ot1Hours { get; set; } public decimal Ot2Hours { get; set; } public decimal Ot3Hours { get; set; } public string ApprovalStatus { get; set; } = "pending"; public string? Notes { get; set; } }
public class ClockInRequest { public string EmployeeId { get; set; } = null!; }
public class ClockOutRequest { public string EmployeeId { get; set; } = null!; }
public class BatchAttendanceRequest { public List<BatchAttendanceRecord> Records { get; set; } = new(); }
public class BatchAttendanceRecord { public Guid EmployeeId { get; set; } public Guid? DepartmentId { get; set; } public DateTime Date { get; set; } public string? ShiftType { get; set; } public DateTime? ClockIn { get; set; } public DateTime? ClockOut { get; set; } public string? Status { get; set; } }

// ========== Leave Request DTOs ==========
public class LeaveRequestDto { public Guid Id { get; set; } public string ApplicationNo { get; set; } = null!; public Guid EmployeeId { get; set; } public string? EmployeeName { get; set; } public string LeaveType { get; set; } = null!; public DateTime ApplicationDate { get; set; } public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public string? StartTime { get; set; } public string? EndTime { get; set; } public decimal TotalHours { get; set; } public string? Reason { get; set; } public string? AttachmentUrl { get; set; } public bool IncludesSpecialPeriod { get; set; } public bool ShiftConflictFlag { get; set; } public bool AutoDeductPayFlag { get; set; } public bool ImpactOtOrBonus { get; set; } public int ApprovalLevel { get; set; } public string Status { get; set; } = "pending"; public Guid? ApproverId { get; set; } public DateTime? ApprovedAt { get; set; } public string? RejectReason { get; set; } public string? Remarks { get; set; } public DateTime CreatedAt { get; set; } }
public class CreateLeaveRequestRequest { public string EmployeeId { get; set; } = null!; public string LeaveType { get; set; } = null!; public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } public TimeOnly? StartTime { get; set; } public TimeOnly? EndTime { get; set; } public string? Reason { get; set; } public string? AttachmentUrl { get; set; } public bool IncludesSpecialPeriod { get; set; } public bool ImpactOtOrBonus { get; set; } }
public class ApproveLeaveRequestRequest { public Guid ApproverId { get; set; } public string? Remarks { get; set; } }
public class RejectLeaveRequestRequest { public Guid ApproverId { get; set; } public string RejectReason { get; set; } = null!; }

// ========== Invoice DTOs ==========
public class InvoiceDto { public Guid Id { get; set; } public string InvoiceNumber { get; set; } = null!; public Guid CustomerId { get; set; } public string? CustomerName { get; set; } public Guid? SalesOrderId { get; set; } public Guid? DeliveryOrderId { get; set; } public DateTime InvoiceDate { get; set; } public DateTime? DueDate { get; set; } public string Currency { get; set; } = "TWD"; public decimal ExchangeRate { get; set; } public string TaxType { get; set; } = "taxable_5pct"; public string? TaxCode { get; set; } public decimal SalesAmount { get; set; } public decimal TaxAmount { get; set; } public decimal TotalAmount { get; set; } public decimal PaidAmount { get; set; } public decimal BaseSalesAmount { get; set; } public decimal BaseTaxAmount { get; set; } public decimal BaseTotalAmount { get; set; } public string Status { get; set; } = "draft"; public string? Remarks { get; set; } public Guid? ApprovedBy { get; set; } public DateTime? ApprovedAt { get; set; } public Guid? VoidedBy { get; set; } public DateTime? VoidedAt { get; set; } public string? VoidReason { get; set; } public DateTime CreatedAt { get; set; } public List<InvoiceItemDto> Items { get; set; } = new(); }
public class InvoiceItemDto { public Guid Id { get; set; } public Guid SkuId { get; set; } public string? SkuNumber { get; set; } public string? SkuName { get; set; } public decimal Quantity { get; set; } public decimal UnitPrice { get; set; } public decimal Amount { get; set; } }
public class CreateInvoiceRequest { public Guid CustomerId { get; set; } public Guid? SalesOrderId { get; set; } public Guid? DeliveryOrderId { get; set; } public DateTime? InvoiceDate { get; set; } public DateTime? DueDate { get; set; } public string? Currency { get; set; } public decimal? ExchangeRate { get; set; } public string? TaxType { get; set; } public string? TaxCode { get; set; } public List<CreateInvoiceItemRequest>? Items { get; set; } }
public class CreateInvoiceItemRequest { public Guid SkuId { get; set; } public decimal Quantity { get; set; } public decimal UnitPrice { get; set; } }
public class ApproveInvoiceRequest { public Guid ApprovedBy { get; set; } }
public class VoidInvoiceRequest { public Guid VoidedBy { get; set; } public string VoidReason { get; set; } = null!; }
public class AgingReportDto { public Guid CustomerId { get; set; } public string CustomerName { get; set; } = null!; public decimal Current { get; set; } public decimal Days30 { get; set; } public decimal Days60 { get; set; } public decimal Days90 { get; set; } public decimal Over90 { get; set; } }

// ========== AP DTOs ==========
public class ApSummaryDto { public Guid SupplierId { get; set; } public string SupplierCode { get; set; } = null!; public string SupplierName { get; set; } = null!; public decimal TotalPayable { get; set; } public int Count { get; set; } }
public class ApAgingDto { public Guid SupplierId { get; set; } public string SupplierName { get; set; } = null!; public decimal Current { get; set; } public decimal Days30 { get; set; } public decimal Days60 { get; set; } public decimal Days90 { get; set; } public decimal Over90 { get; set; } }
public class ApDetailDto { public Guid Id { get; set; } public string PoNumber { get; set; } = null!; public DateTime PoDate { get; set; } public DateTime? DueDate { get; set; } public decimal TotalAmount { get; set; } public decimal PaidAmount { get; set; } public decimal Outstanding { get; set; } public string Status { get; set; } = "pending"; }
public class RecordApPaymentRequest { public Guid PurchaseOrderId { get; set; } public decimal PaymentAmount { get; set; } public string? Notes { get; set; } }

// ========== Payroll DTOs ==========
public class PayrollDto { public Guid Id { get; set; } public Guid EmployeeId { get; set; } public string? EmployeeName { get; set; } public DateTime PeriodStart { get; set; } public DateTime PeriodEnd { get; set; } public decimal BasicSalary { get; set; } public decimal InsuranceBase { get; set; } public decimal PensionBase { get; set; } public int ScheduledDays { get; set; } public int ActualWorkingDays { get; set; } public int TotalLateMinutes { get; set; } public int TotalEarlyMinutes { get; set; } public int AnomalyDays { get; set; } public decimal TotalOt1Hours { get; set; } public decimal TotalOt2Hours { get; set; } public decimal TotalOt3Hours { get; set; } public decimal Ot1Pay { get; set; } public decimal Ot2Pay { get; set; } public decimal Ot3Pay { get; set; } public decimal TotalLeaveHours { get; set; } public decimal AnnualLeaveHours { get; set; } public decimal PersonalLeaveHours { get; set; } public decimal SickLeaveHours { get; set; } public bool FullAttendanceFlag { get; set; } public decimal LateDeduction { get; set; } public decimal EarlyDeduction { get; set; } public decimal AnomalyPenalty { get; set; } public decimal OtherDeductions { get; set; } public decimal OvertimePay { get; set; } public decimal Bonus { get; set; } public decimal AttendanceBonus { get; set; } public decimal LaborInsuranceDeduction { get; set; } public decimal HealthInsuranceDeduction { get; set; } public decimal PensionDeduction { get; set; } public decimal WithholdingTax { get; set; } public decimal GrossPay { get; set; } public decimal TotalDeductions { get; set; } public decimal EmployerContributions { get; set; } public decimal NetPay { get; set; } public string Status { get; set; } = "draft"; public DateTime? PaymentDate { get; set; } public string? PaymentBankAccount { get; set; } public string? PayrollSlipNo { get; set; } public string? CostAllocation { get; set; } public string? Notes { get; set; } public DateTime CreatedAt { get; set; } }
public class CalculatePayrollRequest { public Guid EmployeeId { get; set; } public DateTime PeriodStart { get; set; } public DateTime PeriodEnd { get; set; } }
public class PayrollSummaryDto { public DateTime PeriodStart { get; set; } public DateTime PeriodEnd { get; set; } public int EmployeeCount { get; set; } public decimal TotalBasicSalary { get; set; } public decimal TotalGrossPay { get; set; } public decimal TotalDeductions { get; set; } public decimal TotalNetPay { get; set; } public decimal TotalEmployerContributions { get; set; } }

// ========== QA DTOs ==========
public class IqcReportDto { public Guid Id { get; set; } public string ReportNumber { get; set; } = null!; public Guid SkuId { get; set; } public string? SkuName { get; set; } public string? SupplierName { get; set; } public string? BatchNumber { get; set; } public decimal SampleQty { get; set; } public decimal DefectQty { get; set; } public DateTime ReportDate { get; set; } public string Result { get; set; } = "pending"; }
public class SupplierComplaintDto { public Guid Id { get; set; } public string ComplaintNumber { get; set; } = null!; public string? SupplierName { get; set; } public string? SkuName { get; set; } public string? DefectType { get; set; } public decimal Ppm { get; set; } public string Priority { get; set; } = "medium"; public string Status { get; set; } = "open"; public string? ResponsiblePerson { get; set; } public string? AttachmentUrl { get; set; } public string? ClosureReason { get; set; } public DateTime? ClosedAt { get; set; } public DateTime CreatedAt { get; set; } public string? CapaNumber { get; set; } public string? CapaStatus { get; set; } public string? CapaResponsiblePerson { get; set; } public string? CapaAttachmentUrl { get; set; } public string? CapaClosureReason { get; set; } public DateTime? CapaClosedAt { get; set; } public List<QaReviewLogDto> Timeline { get; set; } = new(); public List<QaReviewLogDto> CapaTimeline { get; set; } = new(); }
public class NonConformanceReportDto { public Guid Id { get; set; } public string NcrNumber { get; set; } = null!; public string SourceType { get; set; } = null!; public Guid? SourceId { get; set; } public string? SourceNumber { get; set; } public string? SupplierName { get; set; } public string? SkuName { get; set; } public string? DefectType { get; set; } public decimal DefectQty { get; set; } public string? DefectDescription { get; set; } public string? Disposition { get; set; } public decimal CostImpact { get; set; } public string? ResponsiblePerson { get; set; } public string? AttachmentUrl { get; set; } public string? ClosureReason { get; set; } public DateTime? ClosedAt { get; set; } public DateTime CreatedAt { get; set; } public List<QaReviewLogDto> Timeline { get; set; } = new(); }

// ========== Supplier Rating DTOs ==========
public class SupplierRatingDto { public Guid SupplierId { get; set; } public string SupplierCode { get; set; } = null!; public string SupplierName { get; set; } = null!; public decimal QualityScore { get; set; } public decimal DeliveryScore { get; set; } public decimal CostScore { get; set; } public decimal ServiceScore { get; set; } public decimal TotalScore { get; set; } public string Grade { get; set; } = "C"; public DateTime CalculatedAt { get; set; } public int PeriodMonths { get; set; } }
public class SupplierApprovalRequest { public Guid EmployeeId { get; set; } public string? Comments { get; set; } }
public class ConvertToPoRequest { public Guid SupplierId { get; set; } }
public class CreateWorkOrderRequest { public Guid ProductSkuId { get; set; } public decimal PlannedQty { get; set; } public string? Priority { get; set; } public DateTime? PlannedStartDate { get; set; } public DateTime? PlannedEndDate { get; set; } }
public class UpdateWorkOrderRequest { public Guid? ProductSkuId { get; set; } public decimal? PlannedQty { get; set; } public string? Priority { get; set; } public DateTime? PlannedStartDate { get; set; } public DateTime? PlannedEndDate { get; set; } }

// ========== Missing DTOs ==========
public class WarehouseTypeDto
{
    public Guid Id { get; set; }
    public string TypeCode { get; set; } = null!;
    public string TypeName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class WarehouseTypeCreateDto
{
    [Required] public string TypeCode { get; set; } = null!;
    [Required] public string TypeName { get; set; } = null!;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
}

public class SupplierPortalLoginRequest
{
    [Required] public string Username { get; set; } = null!;
    [Required] public string Password { get; set; } = null!;
}

public class SupplierPortalLoginResponse
{
    public Guid SupplierUserId { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public string Role { get; set; } = null!;
}

public class SubmitSupplierQuoteRequest
{
    [Required] public Guid RfqId { get; set; }
    [Required] public decimal QuotedPrice { get; set; }
    public int LeadTimeDays { get; set; }
    public decimal Moq { get; set; }
    public string? Notes { get; set; }
}

public class SupplierAsnDto
{
    public Guid AsnId { get; set; }
    public string AsnNumber { get; set; } = null!;
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public DateTime ShipDate { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalShippedQty { get; set; }
    public List<SupplierAsnItemDto> Items { get; set; } = new();
}

public class SupplierAsnItemDto
{
    public Guid Id { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public decimal ShippedQty { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class CreateSupplierAsnRequest
{
    [Required] public Guid PurchaseOrderId { get; set; }
    public string? AsnNumber { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public List<CreateSupplierAsnItemRequest> Items { get; set; } = new();
}

public class CreateSupplierAsnItemRequest
{
    [Required] public Guid SkuId { get; set; }
    [Required] public decimal ShippedQty { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class SupplierPerformanceReportDto
{
    public Guid SupplierId { get; set; }
    public string Period { get; set; } = null!;
    public double QualityScore { get; set; }
    public double DeliveryScore { get; set; }
    public double CostScore { get; set; }
    public double OverallScore { get; set; }
    public string Trend { get; set; } = "stable";
}

public class InventoryTransferDto
{
    public Guid Id { get; set; }
    public string TransferNumber { get; set; } = null!;
    public Guid SourceWarehouseId { get; set; }
    public string? SourceWarehouseName { get; set; }
    public Guid TargetWarehouseId { get; set; }
    public string? TargetWarehouseName { get; set; }
    public DateTime TransferDate { get; set; }
    public string Status { get; set; } = "pending";
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<InventoryTransferItemDto> Items { get; set; } = new();
}

public class InventoryTransferItemDto
{
    public Guid Id { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public decimal Qty { get; set; }
    public string? LotNumber { get; set; }
}

public class CreateInventoryTransferRequest
{
    [Required] public Guid SourceWarehouseId { get; set; }
    [Required] public Guid TargetWarehouseId { get; set; }
    public DateTime? TransferDate { get; set; }
    public List<CreateInventoryTransferItemRequest> Items { get; set; } = new();
}

public class CreateInventoryTransferItemRequest
{
    [Required] public Guid SkuId { get; set; }
    [Required] public decimal Qty { get; set; }
    public string? LotNumber { get; set; }
}
