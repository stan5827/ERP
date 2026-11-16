using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErpBackend.Models.Finance;

public class BankAccount
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(10)] public string? BankCode { get; set; }
    [MaxLength(100)] public string? BankName { get; set; }
    [MaxLength(50)] public string? BranchName { get; set; }
    [MaxLength(30)] public string? AccountNumber { get; set; }
    [MaxLength(100)] public string? AccountName { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(18,2)")] public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? GLAccountId { get; set; }

    [ForeignKey(nameof(GLAccountId))] public GLAccount? GLAccount { get; set; }
}

public class Loan
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string LoanNumber { get; set; } = null!;
    [MaxLength(200)] public string? BankName { get; set; }
    [MaxLength(100)] public string? BranchName { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal LoanAmount { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(8,4)")] public decimal AnnualInterestRate { get; set; }
    [MaxLength(30)] public string InterestType { get; set; } = "fixed";
    public DateTime LoanDate { get; set; }
    public DateTime MaturityDate { get; set; }
    [MaxLength(50)] public string RepaymentMethod { get; set; } = "bullet";
    [MaxLength(500)] public string? CollateralInfo { get; set; }
    [MaxLength(100)] public string? Guarantor { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "active";
    [Column(TypeName = "decimal(18,2)")] public decimal? NextInterestAmount { get; set; }
    public DateTime? NextInterestDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LoanRepaymentSchedule> RepaymentSchedules { get; set; } = new List<LoanRepaymentSchedule>();
}

public class LoanRepaymentSchedule
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LoanId { get; set; }
    public DateTime PaymentDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal PrincipalAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal InterestAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal PaidAmount { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";

    [ForeignKey(nameof(LoanId))] public Loan Loan { get; set; } = null!;
}

public class TreasuryTransfer
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string TransferNumber { get; set; } = null!;
    public DateTime TransferDate { get; set; }
    [MaxLength(200)] public string? Reason { get; set; }
    public Guid? FromBankAccountId { get; set; }
    public Guid? ToBankAccountId { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TransferAmount { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Fee { get; set; }
    [MaxLength(10)] public string Currency { get; set; } = "TWD";
    [Column(TypeName = "decimal(12,6)")] public decimal ExchangeRate { get; set; } = 1;
    [Column(TypeName = "decimal(14,2)")] public decimal ExchangeGainLoss { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Guarantee
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string GuaranteeNumber { get; set; } = null!;
    [MaxLength(50)] public string GuaranteeType { get; set; } = "bank";
    [MaxLength(200)] public string? Beneficiary { get; set; }
    public DateTime ApplicationDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal GuaranteeAmount { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal FeeRate { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal FeePaid { get; set; }
    public Guid? RelatedPoId { get; set; }
    [MaxLength(50)] public string? RelatedContractNo { get; set; }
    public DateTime? ReleaseDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ReleaseAmount { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Investment
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(200)] public string InvestmentName { get; set; } = null!;
    [MaxLength(30)] public string? InvestmentCode { get; set; }
    [MaxLength(50)] public string InvestmentCategory { get; set; } = "stock";
    public DateTime PurchaseDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Quantity { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Fees { get; set; }
    [Column(TypeName = "decimal(14,2)")] public decimal Tax { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalCost { get; set; }
    [MaxLength(50)] public string? AccountingClassification { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal CurrentMarketValue { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnrealizedGainLoss { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DividendIncome { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DisposalGainLoss { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal ShareholdingPercentage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<InvestmentValuation> Valuations { get; set; } = new List<InvestmentValuation>();
}

public class InvestmentValuation
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvestmentId { get; set; }
    public DateTime ValuationDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal MarketValue { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnrealizedGainLoss { get; set; }

    [ForeignKey(nameof(InvestmentId))] public Investment Investment { get; set; } = null!;
}
