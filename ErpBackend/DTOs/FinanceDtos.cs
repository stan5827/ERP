using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErpBackend.DTOs;

public class GLAccountDto
{
    public Guid Id { get; set; }
    public string AccountCode { get; set; } = null!;
    public string AccountName { get; set; } = null!;
    public string AccountType { get; set; } = "Asset";
    public bool IsActive { get; set; }
    public bool IsCashAccount { get; set; }
    public Guid? ParentAccountId { get; set; }
}

public class CreateGLAccountRequestDto
{
    [Required, MaxLength(20)]
    public string AccountCode { get; set; } = null!;

    [Required, MaxLength(100)]
    public string AccountName { get; set; } = null!;

    [Required, MaxLength(20)]
    public string AccountType { get; set; } = "Asset";

    public bool IsCashAccount { get; set; }
    public Guid? ParentAccountId { get; set; }
}

public class JournalEntryDto
{
    public Guid Id { get; set; }
    public DateTime EntryDate { get; set; }
    public Guid AccountId { get; set; }
    public string? AccountNo { get; set; }
    public string? AccountName { get; set; }
    public decimal AmountDr { get; set; }
    public decimal AmountCr { get; set; }
    public string SourceModule { get; set; } = "";
    public string SourceDocNo { get; set; } = "";
    public string? Description { get; set; }
}

/// <summary>會計傳票的借貸分錄與科目資料。</summary>
public class AccountingVoucherEntryDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid GLAccountId { get; set; }
    public string AccountCode { get; set; } = null!;
    public string AccountName { get; set; } = null!;
    public Guid? CostCenterId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string? Memo { get; set; }
}

/// <summary>會計傳票主檔及其分錄明細。</summary>
public class AccountingVoucherDto
{
    public Guid Id { get; set; }
    public string VoucherNumber { get; set; } = null!;
    public DateTime VoucherDate { get; set; }
    public string VoucherType { get; set; } = null!;
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public List<AccountingVoucherEntryDto> Entries { get; set; } = [];
    public decimal DebitTotal { get; set; }
    public decimal CreditTotal { get; set; }
}

/// <summary>單筆人工會計傳票分錄的輸入資料。</summary>
public class AccountingVoucherLineRequestDto
{
    [Required]
    public Guid GLAccountId { get; set; }
    [Range(typeof(decimal), "0", "999999999999999")]
    public decimal DebitAmount { get; set; }
    [Range(typeof(decimal), "0", "999999999999999")]
    public decimal CreditAmount { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? WorkOrderId { get; set; }
    [MaxLength(200)]
    public string? Memo { get; set; }
}

/// <summary>建立或更新人工傳票草稿的輸入資料。</summary>
public class SaveAccountingVoucherRequestDto
{
    [Required]
    public DateOnly VoucherDate { get; set; }
    [Required, MaxLength(20)]
    public string VoucherType { get; set; } = "Journal";
    [MaxLength(200)]
    public string? Description { get; set; }
    [Required, MinLength(2), MaxLength(100)]
    public List<AccountingVoucherLineRequestDto> Entries { get; set; } = [];
}

/// <summary>日記帳查詢條件與分頁設定。</summary>
public class JournalQueryRequestDto
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? AccountId { get; set; }
    [MaxLength(30)]
    public string? VoucherNumber { get; set; }
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [Range(1, 100)]
    public int PageSize { get; set; } = 50;
}

/// <summary>日記帳查詢結果、借貸合計與分頁資訊。</summary>
public class JournalQueryResultDto
{
    public List<JournalEntryDto> Entries { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public decimal DebitTotal { get; set; }
    public decimal CreditTotal { get; set; }
}

public class GeneralLedgerQueryRequestDto
{
    [Required]
    public Guid AccountId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    [Range(1, 200)]
    public int PageSize { get; set; } = 50;
}

public class GeneralLedgerEntryDto
{
    public Guid EntryId { get; set; }
    public Guid VoucherId { get; set; }
    public string VoucherNumber { get; set; } = null!;
    public DateTime VoucherDate { get; set; }
    public string VoucherType { get; set; } = null!;
    public string? Description { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public int LineNumber { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningDebitBalance { get; set; }
    public decimal RunningCreditBalance { get; set; }
}

public class GeneralLedgerResultDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = null!;
    public string AccountName { get; set; } = null!;
    public string AccountType { get; set; } = null!;
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public decimal OpeningDebitBalance { get; set; }
    public decimal OpeningCreditBalance { get; set; }
    public decimal PeriodDebitTotal { get; set; }
    public decimal PeriodCreditTotal { get; set; }
    public decimal ClosingDebitBalance { get; set; }
    public decimal ClosingCreditBalance { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<GeneralLedgerEntryDto> Entries { get; set; } = [];
}

public class TrialBalanceQueryRequestDto
{
    public DateOnly? FromDate { get; set; }
    [Required]
    public DateOnly? ToDate { get; set; }
}

public class AccountBalanceDto
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = null!;
    public string AccountName { get; set; } = null!;
    public string AccountType { get; set; } = null!;
    public decimal OpeningDebitBalance { get; set; }
    public decimal OpeningCreditBalance { get; set; }
    public decimal PeriodDebitTotal { get; set; }
    public decimal PeriodCreditTotal { get; set; }
    public decimal ClosingDebitBalance { get; set; }
    public decimal ClosingCreditBalance { get; set; }
}

public class TrialBalanceResultDto
{
    public DateOnly? FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public List<AccountBalanceDto> Accounts { get; set; } = [];
    public decimal OpeningDebitTotal { get; set; }
    public decimal OpeningCreditTotal { get; set; }
    public decimal PeriodDebitTotal { get; set; }
    public decimal PeriodCreditTotal { get; set; }
    public decimal ClosingDebitTotal { get; set; }
    public decimal ClosingCreditTotal { get; set; }
    public bool IsBalanced { get; set; }
}

public class FinancialStatementsResultDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public List<AccountBalanceDto> Assets { get; set; } = [];
    public List<AccountBalanceDto> Liabilities { get; set; } = [];
    public List<AccountBalanceDto> Equity { get; set; } = [];
    public List<AccountBalanceDto> Revenue { get; set; } = [];
    public List<AccountBalanceDto> Expenses { get; set; } = [];
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public decimal PeriodNetIncome { get; set; }
    public decimal CumulativeUnclosedProfit { get; set; }
    public decimal BalanceSheetDifference { get; set; }
    public bool IsBalanceSheetBalanced { get; set; }
}

public class PostingSetupDto
{
    public string TransactionType { get; set; } = null!;
    public string? DrAccountNo { get; set; }
    public string? CrAccountNo { get; set; }
}

public class BankAccountDto
{
    public Guid Id { get; set; }
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? BranchName { get; set; }
    public string? AccountName { get; set; }
    public Guid? GLAccountId { get; set; }
    public string? GLAccountCode { get; set; }
    public string? GLAccountName { get; set; }
    public bool? GLAccountIsActive { get; set; }
    public bool? GLAccountIsCashAccount { get; set; }
    public string Currency { get; set; } = "TWD";
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; }
}

public class InvestmentDto
{
    public Guid Id { get; set; }
    public string InvestmentName { get; set; } = null!;
    public string InvestmentCategory { get; set; } = "stock";
    public decimal TotalCost { get; set; }
    public decimal CurrentMarketValue { get; set; }
    public decimal UnrealizedGainLoss { get; set; }
}

public class LoanDto
{
    public Guid Id { get; set; }
    public string LoanNumber { get; set; } = null!;
    public string? BankName { get; set; }
    public decimal LoanAmount { get; set; }
    public string Currency { get; set; } = "TWD";
    public decimal AnnualInterestRate { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime MaturityDate { get; set; }
    public string RepaymentMethod { get; set; } = "bullet";
    public string Status { get; set; } = "active";
}

public class CreateBankAccountRequest
{
    [MaxLength(10)] public string? BankCode { get; set; }
    [Required, MaxLength(100)] public string BankName { get; set; } = null!;
    [Required, MaxLength(30)] public string AccountNumber { get; set; } = null!;
    [MaxLength(100)] public string? BranchName { get; set; }
    [MaxLength(100)] public string? AccountName { get; set; }
    public Guid? GLAccountId { get; set; }
    [Required, MaxLength(10)] public string Currency { get; set; } = "TWD";
    public decimal? CurrentBalance { get; set; }
}

public class UpdateBankAccountRequestDto
{
    [MaxLength(10)] public string? BankCode { get; set; }
    [Required, MaxLength(100)] public string BankName { get; set; } = null!;
    [Required, MaxLength(30)] public string AccountNumber { get; set; } = null!;
    [MaxLength(100)] public string? BranchName { get; set; }
    [MaxLength(100)] public string? AccountName { get; set; }
    public Guid? GLAccountId { get; set; }
    [Required, MaxLength(10)] public string Currency { get; set; } = "TWD";
}

public class SetBankAccountStatusRequestDto
{
    public bool IsActive { get; set; }
}

public class UpdateBankAccountGlMappingDto
{
    public Guid GLAccountId { get; set; }
}

public class GLAccountMappingDto
{
    public string MappingKey { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string RequiredAccountType { get; set; } = null!;
    public Guid? GLAccountId { get; set; }
    public string? AccountCode { get; set; }
    public string? AccountName { get; set; }
    public bool IsConfigured { get; set; }
}

public class SetGLAccountMappingRequestDto
{
    public Guid GLAccountId { get; set; }
}

public class CreateAccountingPeriodRequestDto
{
    [Required, MaxLength(30)]
    public string PeriodCode { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public class AccountingPeriodDto
{
    public Guid Id { get; set; }
    public string PeriodCode { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public class ReverseAccountingVoucherRequestDto
{
    public DateOnly ReversalDate { get; set; }
    [Required, MaxLength(150)]
    public string Reason { get; set; } = null!;
}

public class CreateInvestmentRequest
{
    public string InvestmentName { get; set; } = null!;
    public string? InvestmentCategory { get; set; }
    public decimal TotalCost { get; set; }
    public decimal? CurrentMarketValue { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? Quantity { get; set; }
}

public class CreateLoanRequest
{
    public string? BankName { get; set; }
    public decimal LoanAmount { get; set; }
    public string? Currency { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public DateTime? LoanDate { get; set; }
    public DateTime? MaturityDate { get; set; }
    public string? RepaymentMethod { get; set; }
}

public class CreateTreasuryTransferRequest
{
    public DateTime? TransferDate { get; set; }
    public string? Reason { get; set; }
    public Guid? FromBankAccountId { get; set; }
    public Guid? ToBankAccountId { get; set; }
    public decimal TransferAmount { get; set; }
    public string? Currency { get; set; }
    public decimal? ExchangeRate { get; set; }
}

public class CreateGuaranteeRequest
{
    public string? GuaranteeType { get; set; }
    public string? Beneficiary { get; set; }
    public decimal GuaranteeAmount { get; set; }
    public DateTime? ApplicationDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal? FeeRate { get; set; }
}

public class MonthlyPeriodDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal GrossProfit { get; set; }
}

public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DiscountedAmount { get; set; }
    public decimal Outstanding { get; set; }
    public string Status { get; set; } = null!;
    public string Currency { get; set; } = "TWD";
    public decimal? ExchangeRate { get; set; }
    public string? TaxType { get; set; }
    public string? TaxCode { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal BaseSalesAmount { get; set; }
    public decimal BaseTaxAmount { get; set; }
    public decimal BaseTotalAmount { get; set; }
    public string? Remarks { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? DeliveryOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? VoidedBy { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidReason { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? SkuId { get; set; }
    public string? SkuNumber { get; set; }
    public string? SkuName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Amount { get; set; }
}

public class CreateInvoiceRequest
{
    public Guid CustomerId { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Currency { get; set; }
    public decimal? ExchangeRate { get; set; }
    public string? TaxType { get; set; }
    public string? TaxCode { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? DeliveryOrderId { get; set; }
    public List<CreateInvoiceItemRequest> Items { get; set; } = new();
}

public class CreateInvoiceItemRequest
{
    public Guid SkuId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
}

public class ApproveInvoiceRequest
{
    public Guid ApproverId { get; set; }
    public Guid ApprovedBy { get; set; }
    public string? Comments { get; set; }
}

public class VoidInvoiceRequest
{
    public Guid VoiderId { get; set; }
    public Guid VoidedBy { get; set; }
    public string? VoidReason { get; set; }
}

public sealed class CreateSalesCreditNoteRequest
{
    public Guid SalesInvoiceId { get; set; }
    public Guid DeliveryOrderId { get; set; }
    public decimal NetAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed record ApproveSalesCreditNoteRequest(string ApprovalRole, string? Comments);

public sealed record SalesCreditNoteDto(
    Guid Id,
    string CreditNoteNumber,
    Guid SalesInvoiceId,
    string InvoiceNumber,
    Guid DeliveryOrderId,
    string DeliveryNumber,
    decimal NetAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal BaseTotalAmount,
    decimal ReceivableAmount,
    decimal CustomerAdvanceAmount,
    string Currency,
    decimal ExchangeRate,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<SalesCreditNoteApprovalDto> Approvals);

public sealed record SalesCreditNoteApprovalDto(
    string ApprovalRole,
    Guid ApproverId,
    string? Comments,
    DateTime ApprovedAt);

public class ArAgingDto
{
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Current { get; set; }
    public decimal Days30 { get; set; }
    public decimal Days60 { get; set; }
    public decimal Days90 { get; set; }
    public decimal Over90 { get; set; }
    public decimal TotalOutstanding { get; set; }
}

public class CreditUsageDto
{
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal UsedCredit { get; set; }
    public decimal AvailableCredit { get; set; }
    public decimal UsageRate { get; set; }
}

public class AgingReportDto
{
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Current { get; set; }
    public decimal Days30 { get; set; }
    public decimal Days60 { get; set; }
    public decimal Days90 { get; set; }
    public decimal Over90 { get; set; }
    public decimal TotalOutstanding { get; set; }
}

public class CreateCashReceiptRequest
{
    public DateTime ReceiptDate { get; set; }
    public Guid CustomerId { get; set; }
    public string PaymentMethod { get; set; } = "wire";
    public decimal ReceivedAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Currency { get; set; } = "TWD";
    public Guid? BankAccountId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public List<CreateCashReceiptAllocationRequest> Allocations { get; set; } = new();
}

public class CreateCashReceiptAllocationRequest
{
    public Guid SalesInvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal DiscountAmount { get; set; }
}

public class ApplyCashReceiptUnappliedRequestDto
{
    public Guid SalesInvoiceId { get; set; }
    public decimal Amount { get; set; }
}

public class PendingInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DiscountedAmount { get; set; }
    public decimal Outstanding { get; set; }
    public string Currency { get; set; } = "TWD";
}

public class CashReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = null!;
    public DateTime ReceiptDate { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public decimal ReceivedAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Currency { get; set; } = "TWD";
    public decimal UnappliedAmount { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CashReceiptAllocationDto>? Allocations { get; set; }
}

public class CashReceiptAllocationDto
{
    public Guid Id { get; set; }
    public Guid SalesInvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ExchangeGainLoss { get; set; }
}
