using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ErpBackend.Models.Finance;

/// <summary>
/// 會計科目表 (Chart of Accounts)
/// </summary>
public class GLAccount
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(20)] 
    public string AccountCode { get; set; } = null!; // 科目代碼 (如: 1101)
    
    [Required, MaxLength(100)] 
    public string AccountName { get; set; } = null!; // 科目名稱 (如: 現金)
    
    [MaxLength(20)] 
    public string AccountType { get; set; } = "Asset"; // 類別: Asset, Liability, Equity, Revenue, Expense
    
    public bool IsCashAccount { get; set; } // 是否為現金/銀行科目
    
    public Guid? ParentAccountId { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ParentAccountId))]
    [JsonIgnore]
    public GLAccount? ParentAccount { get; set; }
    
    public ICollection<GLAccount> ChildAccounts { get; set; } = new List<GLAccount>();
}

public static class AccountingMappingKeys
{
    public static readonly IReadOnlyList<AccountingMappingDefinition> All =
    [
        new("accounts_receivable", "應收帳款", "Asset"),
        new("accounts_payable", "應付帳款", "Liability"),
        new("sales_revenue", "銷貨收入", "Revenue"),
        new("output_tax", "銷項稅額", "Liability"),
        new("input_tax", "進項稅額", "Asset"),
        new("raw_material_inventory", "原物料存貨", "Asset"),
        new("work_in_progress", "在製品存貨", "Asset"),
        new("sales_cogs", "銷貨成本", "Expense"),
        new("finished_goods_inventory", "成品存貨", "Asset"),
        new("scrap_loss", "報廢損失", "Expense"),
        new("production_rework_loss", "生產損失-工單改製", "Expense"),
        new("production_variance", "製費差異", "Expense"),
        new("purchase_allowance_inventory", "採購折讓-存貨", "Asset"),
        new("purchase_allowance_wip", "採購折讓-在製品", "Asset"),
        new("purchase_allowance_cogs", "採購折讓-銷貨成本", "Expense"),
        new("quarantine_inventory", "隔離品存貨", "Asset"),
        new("customer_advances", "客戶預收款／未沖款", "Liability"),
        new("sales_discount", "銷貨折讓", "Revenue")
    ];
}

public sealed record AccountingMappingDefinition(string Key, string Name, string AccountType);

public class GLAccountMapping
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(50)] public string MappingKey { get; set; } = null!;
    [Required] public Guid GLAccountId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(GLAccountId))] public GLAccount GLAccount { get; set; } = null!;
}

/// <summary>
/// 會計傳票 (Accounting Voucher)
/// </summary>
public class AccountingVoucher
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required, MaxLength(30)] 
    public string VoucherNumber { get; set; } = null!; // 傳票編號 (如: V20260416001)
    
    public DateTime VoucherDate { get; set; } = DateTime.UtcNow;
    
    [MaxLength(20)] 
    public string VoucherType { get; set; } = "Journal"; // 轉帳, 現金收入, 現金支出
    
    [MaxLength(200)] 
    public string? Description { get; set; } // 摘要
    
    [MaxLength(20)] 
    public string Status { get; set; } = "Draft"; // Draft, Posted, Reversed
    
    public Guid? CreatedBy { get; set; }
    public Guid? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? PostedBy { get; set; }
    public DateTime? PostedAt { get; set; }
    
    // 關聯業務單據 (如: 銷貨發票 ID)
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    [MaxLength(50)] public string? PostingPurpose { get; set; }
    public Guid? ReversalOfVoucherId { get; set; }

    public ICollection<AccountingEntry> Entries { get; set; } = new List<AccountingEntry>();
    public AccountingVoucher? ReversalOfVoucher { get; set; }
    public AccountingVoucher? ReversedByVoucher { get; set; }
}

public class AccountingPeriod
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(30)] public string PeriodCode { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
}

/// <summary>
/// 會計分錄 (Accounting Entry/Journal Entry)
/// </summary>
public class AccountingEntry
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid VoucherId { get; set; }

    public int LineNumber { get; set; }
    
    public Guid GLAccountId { get; set; }
    
    public Guid? CostCenterId { get; set; }
    
    [Column(TypeName = "decimal(18,2)")] 
    public decimal DebitAmount { get; set; } // 借方金額
    
    [Column(TypeName = "decimal(18,2)")] 
    public decimal CreditAmount { get; set; } // 貸方金額
    
    [MaxLength(200)] 
    public string? Memo { get; set; } // 分錄摘要

    [ForeignKey(nameof(VoucherId))]
    [JsonIgnore]
    public AccountingVoucher Voucher { get; set; } = null!;
    
    [ForeignKey(nameof(GLAccountId))]
    public GLAccount GLAccount { get; set; } = null!;
    
    [ForeignKey(nameof(CostCenterId))]
    public CostCenter? CostCenter { get; set; }
}
