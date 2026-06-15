using System;
using System.Collections.Generic;

namespace ErpBackend.DTOs
{
    public class BankAccountDto
    {
        public Guid Id { get; set; }
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string Currency { get; set; } = "TWDTWD";
        public decimal AnnualInterestRate { get; set; }
        public string LoanDate { get; set; } = null!; // ISO string
        public string MaturityDate { get; set; } = null!; // ISO string
        public string RepaymentMethod { get; set; } = "bullet";
        public string Status { get; set; } = "active";
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

    public class GuaranteeDto
    {
        public Guid Id { get; set; }
        public string GuaranteeNumber { get; set; } = null!;
        public string GuaranteeType { get; set; } = "bankactive";
    }

    public class VoucherDto
    {
        public Guid Id { get; set; }
        public string VoucherNumber { get; set; } = null!;
        public string EntryDate { get; set; } = null!;
        public string SourceModule { get; set; } = "Manual";
        public string SourceDocNo { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "Posted";
        public List<VoucherEntryDto> Entries { get; set; } = new();
    }

    public class VoucherEntryDto
    {
        public string? AccountNo { get; set; }
        public string? AccountName { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public GlAccountSummaryDto? GlAccount { get; set; }
    }

    public class GlAccountSummaryDto
    {
        public string? AccountName { get; set; }
        public string? AccountCode { get; set; }
    }
}
