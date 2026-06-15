using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Sales;

namespace ErpBackend.Services;

public interface IAccountingService
{
    Task<AccountingVoucher> CreateVoucherFromInvoiceAsync(Guid invoiceId);
    Task<AccountingVoucher> CreateVoucherFromAPAsync(Guid apId);
    Task<AccountingVoucher> CreateVoucherFromProductionReceiptAsync(Guid workOrderId, decimal quantity, decimal costingAmount);
    Task<GLAccount?> GetAccountByCodeAsync(string code);
    Task SeedDefaultAccountsAsync();
}

public class AccountingService : IAccountingService
{
    private readonly ErpGeneralDbContext _db;

    public AccountingService(ErpGeneralDbContext db)
    {
        _db = db;
    }

    public async Task<GLAccount?> GetAccountByCodeAsync(string code)
    {
        return await _db.GLAccounts.FirstOrDefaultAsync(a => a.AccountCode == code);
    }

    /// <summary>
    /// 從銷貨發票產生會計傳票
    /// </summary>
    public async Task<AccountingVoucher> CreateVoucherFromInvoiceAsync(Guid invoiceId)
    {
        var invoice = await _db.SalesInvoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null) throw new ArgumentException("找不到發票");
        
        // 檢查是否已產生過傳票
        var existing = await _db.AccountingVouchers
            .FirstOrDefaultAsync(v => v.ReferenceType == "SalesInvoice" && v.ReferenceId == invoiceId);
        if (existing != null) return existing;

        var voucher = new AccountingVoucher
        {
            VoucherNumber = $"V{DateTime.UtcNow:yyyyMMdd}-{invoice.InvoiceNumber}",
            VoucherDate = invoice.InvoiceDate,
            VoucherType = "Journal",
            Description = $"銷貨發票自動過帳 - {invoice.InvoiceNumber}",
            ReferenceType = "SalesInvoice",
            ReferenceId = invoiceId,
            Status = "Posted",
            PostedAt = DateTime.UtcNow
        };

        // 1. 借方：應收帳款 (1131)
        var arAccount = await GetAccountByCodeAsync("1131") ?? throw new Exception("未設定應收帳款科目 (1131)");
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = arAccount.Id,
            DebitAmount = invoice.TotalAmount,
            CreditAmount = 0,
            Memo = $"客戶應收帳款 - {invoice.InvoiceNumber}"
        });

        // 2. 貸方：銷貨收入 (4101)
        var revenueAccount = await GetAccountByCodeAsync("4101") ?? throw new Exception("未設定銷貨收入科目 (4101)");
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = revenueAccount.Id,
            DebitAmount = 0,
            CreditAmount = invoice.SalesAmount,
            Memo = $"銷貨收入 - {invoice.InvoiceNumber}"
        });

        // 3. 貸方：銷項稅額 (2101 - 假設代碼)
        if (invoice.TaxAmount > 0)
        {
            var taxAccount = await GetAccountByCodeAsync("2101") ?? throw new Exception("未設定銷項稅額科目 (2101)");
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = taxAccount.Id,
                DebitAmount = 0,
                CreditAmount = invoice.TaxAmount,
                Memo = $"銷項稅額 - {invoice.InvoiceNumber}"
            });
        }

        _db.AccountingVouchers.Add(voucher);
        await _db.SaveChangesAsync();

        return voucher;
    }

    /// <summary>
    /// 從應付帳款產生會計傳票
    /// </summary>
    public async Task<AccountingVoucher> CreateVoucherFromAPAsync(Guid apId)
    {
        var ap = await _db.AccountsPayables
            .Include(a => a.Items)
            .FirstOrDefaultAsync(a => a.Id == apId);

        if (ap == null) throw new ArgumentException("找不到應付帳款記錄");

        var voucher = new AccountingVoucher
        {
            VoucherNumber = $"V{DateTime.UtcNow:yyyyMMdd}-{ap.ApNumber}",
            VoucherDate = ap.SupplierInvoiceDate ?? DateTime.UtcNow,
            VoucherType = "Journal",
            Description = $"採購進貨自動過帳 - {ap.ApNumber} / 發票: {ap.SupplierInvoiceNumber}",
            ReferenceType = "AccountsPayable",
            ReferenceId = apId,
            Status = "Posted",
            PostedAt = DateTime.UtcNow
        };

        // 1. 借方：存貨/原料 (1201 - 假設代碼)
        var inventoryAccount = await GetAccountByCodeAsync("1201") ?? throw new Exception("未設定存貨科目 (1201)");
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = inventoryAccount.Id,
            DebitAmount = ap.InvoiceAmount,
            CreditAmount = 0,
            Memo = $"採購商品/物料 - {ap.ApNumber}"
        });

        // 2. 借方：進項稅額 (1202 - 假設代碼)
        if (ap.TaxAmount > 0)
        {
            var taxAccount = await GetAccountByCodeAsync("1202") ?? throw new Exception("未設定進項稅額科目 (1202)");
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = taxAccount.Id,
                DebitAmount = ap.TaxAmount,
                CreditAmount = 0,
                Memo = $"進項稅額 - {ap.ApNumber}"
            });
        }

        // 3. 貸方：應付帳款 (2101)
        var apAccount = await GetAccountByCodeAsync("2101") ?? throw new Exception("未設定應付帳款科目 (2101)");
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = apAccount.Id,
            DebitAmount = 0,
            CreditAmount = ap.TotalAmount,
            Memo = $"供應商應付帳款 - {ap.ApNumber}"
        });

        _db.AccountingVouchers.Add(voucher);
        await _db.SaveChangesAsync();

        return voucher;
    }

    /// <summary>
    /// 從生產完工入庫產生會計傳票 (成本結轉: WIP -> Finished Goods)
    /// </summary>
    public async Task<AccountingVoucher> CreateVoucherFromProductionReceiptAsync(Guid workOrderId, decimal quantity, decimal costingAmount)
    {
        var wo = await _db.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == workOrderId);

        if (wo == null) throw new ArgumentException("找不到對應工單");

        var voucher = new AccountingVoucher
        {
            VoucherNumber = $"V{DateTime.UtcNow:yyyyMMdd}-PR-{wo.WoNumber}",
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            Description = $"完工入庫自動過帳 - 工單: {wo.WoNumber}, 數量: {quantity}",
            ReferenceType = "WorkOrder",
            ReferenceId = workOrderId,
            Status = "Posted",
            PostedAt = DateTime.UtcNow
        };

        // 1. 借方：存貨-成品 (1205)
        var fgAccount = await GetAccountByCodeAsync("1205") ?? throw new Exception("未設定存貨-成品科目 (1205)");
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = fgAccount.Id,
            DebitAmount = costingAmount,
            CreditAmount = 0,
            Memo = $"成品入庫 - 工單: {wo.WoNumber}"
        });

        // 2. 貸方：在製品 (1206)
        var wipAccount = await GetAccountByCodeAsync("1206") ?? throw new Exception("未設定在製品科目 (1206)");
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = wipAccount.Id,
            DebitAmount = 0,
            CreditAmount = costingAmount,
            Memo = $"WIP 結轉 - 工單: {wo.WoNumber}"
        });

        _db.AccountingVouchers.Add(voucher);
        await _db.SaveChangesAsync();

        return voucher;
    }

    /// <summary>
    /// 初始化開發用的基本會計科目
    /// </summary>
    public async Task SeedDefaultAccountsAsync()
    {
        var accounts = new List<GLAccount>
        {
            new GLAccount { AccountCode = "1131", AccountName = "應收帳款", AccountType = "Asset" },
            new GLAccount { AccountCode = "4101", AccountName = "銷貨收入", AccountType = "Revenue" },
            new GLAccount { AccountCode = "2101", AccountName = "應付帳款", AccountType = "Liability" },
            new GLAccount { AccountCode = "1201", AccountName = "存貨-原料", AccountType = "Asset" },
            new GLAccount { AccountCode = "1202", AccountName = "進項稅額", AccountType = "Asset" },
            new GLAccount { AccountCode = "2201", AccountName = "銷項稅額", AccountType = "Liability" },
            new GLAccount { AccountCode = "1205", AccountName = "存貨-成品", AccountType = "Asset" },
            new GLAccount { AccountCode = "1206", AccountName = "在製品 (WIP)", AccountType = "Asset" },
            new GLAccount { AccountCode = "1101", AccountName = "現金", AccountType = "Asset", IsCashAccount = true },
            new GLAccount { AccountCode = "1103", AccountName = "銀行存款", AccountType = "Asset", IsCashAccount = true }
        };

        foreach (var acc in accounts)
        {
            if (!await _db.GLAccounts.AnyAsync(a => a.AccountCode == acc.AccountCode))
            {
                _db.GLAccounts.Add(acc);
            }
        }
        await _db.SaveChangesAsync();
    }
}
