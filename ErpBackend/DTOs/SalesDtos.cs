using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class CreateCustomerRequestDto
{
    [Required, MaxLength(20)]
    public string CustomerCode { get; set; } = null!;

    [Required, MaxLength(200)]
    public string CustomerName { get; set; } = null!;

    [MaxLength(10)]
    public string? TaxId { get; set; }

    [MaxLength(50)]
    public string? ContactPerson { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? InvoiceAddress { get; set; }

    [MaxLength(500)]
    public string? DeliveryAddress { get; set; }

    [MaxLength(50)]
    public string? PaymentTerms { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "TWD";

    [Range(typeof(decimal), "0", "999999999999.99")]
    public decimal CreditLimit { get; set; }
}

public class UpdateCustomerRequestDto : CreateCustomerRequestDto
{
}

public class QuoteDto
{
    public Guid Id { get; set; }
    public string QuoteNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime QuoteDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal FinalAmount { get; set; }
    public string Currency { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? CustomerPoNumber { get; set; }
    public DateTime? CustomerDueDate { get; set; }
    public DateTime? ConfirmedDeliveryDate { get; set; }
    public List<QuoteItemDto> Items { get; set; } = new();
}

public class CreateQuoteRequest
{
    public Guid CustomerId { get; set; }
    public DateTime? QuoteDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public decimal? DiscountRate { get; set; } = 1m;
    public string? CustomerPoNumber { get; set; }
    public DateTime? CustomerDueDate { get; set; }
    public DateTime? ConfirmedDeliveryDate { get; set; }
    public string? Currency { get; set; }
    public List<CreateQuoteItemRequest> Items { get; set; } = new();
}

public class CreateQuoteItemRequest
{
    public Guid? ProductId { get; set; }
    public Guid? SkuId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; } = 1m;
}

public class QuoteItemDto
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public string? Unit { get; set; }
    public string? UnitName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal Subtotal { get; set; }
}

public class SalesOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!;
    public bool CreditChecked { get; set; }
    public string? CustomerPoNumber { get; set; }
    public Guid? SalespersonId { get; set; }
    public string? SalespersonName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SalesOrderItemDto> Items { get; set; } = new();
}

public class SalesOrderConfirmationDto
{
    public string OrderNumber { get; set; } = null!;
    public decimal CreditLimit { get; set; }
    public decimal CreditUsageBeforeOrder { get; set; }
    public decimal CreditUsageAfterOrder { get; set; }
    public bool CreditLimitExceeded { get; set; }
    public List<SalesOrderInventoryWarningDto> InventoryWarnings { get; set; } = new();
}

public class SalesOrderInventoryWarningDto
{
    public int LineNumber { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public decimal RequiredQty { get; set; }
    public decimal AvailableQty { get; set; }
    public decimal ShortageQty { get; set; }
}

public class CreateSalesOrderRequest
{
    public Guid CustomerId { get; set; }
    public DateTime? OrderDate { get; set; }
    public string? CustomerPoNumber { get; set; }
    public string? PaymentTerms { get; set; }
    public DateTime? RequiredDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? OrderRemarks { get; set; }
    public List<CreateSalesOrderItemRequest> Items { get; set; } = new();
}

public class CreateSalesOrderItemRequest
{
    public Guid SkuId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public bool NeedManufacturing { get; set; }
    public bool NeedOutsourcing { get; set; }
}

public class UpdateStatusRequest
{
    public string NewStatus { get; set; } = null!;
}

public class UpdateManufacturingStatusRequest
{
    public string NewStatus { get; set; } = null!;
    public string? Status { get; set; }
}

public class SalesOrderItemDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public string? Unit { get; set; }
    public string? UnitName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal Subtotal { get; set; }
    public int LineNumber { get; set; }
    public string? Status { get; set; }
    public DateTime? PlannedFinishDate { get; set; }
    public bool NeedManufacturing { get; set; }
    public bool NeedOutsourcing { get; set; }
}

public class UpdateSalesOrderStatusRequest
{
    public string Status { get; set; } = null!;
}

public class UpdateSalesOrderRequest
{
    public Guid? CustomerId { get; set; }
    public string? ShippingMethod { get; set; }
    public string? ShippingAddress { get; set; }
}
