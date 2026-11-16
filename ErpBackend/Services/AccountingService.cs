using Microsoft.EntityFrameworkCore;
using Npgsql;
using ErpBackend.Data;
using ErpBackend.Helpers;
using ErpBackend.Models.Finance;
using ErpBackend.Models.Sales;

namespace ErpBackend.Services;

public interface IAccountingService
{
    Task<AccountingVoucher> CreateVoucherFromInvoiceAsync(Guid invoiceId);
    Task<AccountingVoucher> CreateVoucherFromSalesCreditNoteAsync(Guid creditNoteId);
    Task<AccountingVoucher> CreateVoucherFromSupplierDebitNoteAsync(Guid debitNoteId, DateTime voucherDate);
    Task<AccountingVoucher> CreateVoucherFromCashReceiptAsync(Guid receiptId);
    Task<AccountingVoucher> CreateVoucherFromCashReceiptApplicationAsync(
        Guid receiptId,
        Guid allocationId,
        decimal amount);
    Task<string?> ValidateInvoiceMappingsAsync(bool requiresTax);
    Task<string?> ValidateCashReceiptMappingsAsync(bool requiresUnapplied);
    Task<AccountingVoucher> CreateVoucherFromAPAsync(Guid apId);
    Task<AccountingVoucher> CreateVoucherFromProductionReceiptAsync(Guid workOrderId, decimal quantity, decimal costingAmount);
    Task<AccountingVoucher> CreateVoucherFromManufacturingReceiptAsync(Guid receiptId, Guid workOrderId, decimal quantity, decimal costingAmount);
    Task<string?> ValidateDeliveryCogsMappingsAsync();
    Task<AccountingVoucher> CreateVoucherFromDeliveryAsync(Guid deliveryOrderId, string deliveryNumber, decimal costingAmount);
    Task<AccountingVoucher?> CreateVoucherForWorkOrderCostSettlementAsync(
        Guid workOrderId,
        decimal actualCost,
        IReadOnlyList<WorkOrderCostVarianceAllocation> abnormalCostAllocations);
    Task<(bool success, string status, AccountingVoucher? voucher)> ReverseVoucherAsync(
        Guid voucherId,
        DateOnly reversalDate,
        string reason);
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

    public async Task<(bool success, string status, AccountingVoucher? voucher)> ReverseVoucherAsync(
        Guid voucherId,
        DateOnly reversalDate,
        string reason)
    {
        var original = await _db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .FirstOrDefaultAsync(voucher => voucher.Id == voucherId);
        if (original is null)
            return (false, "not_found", null);
        if (!original.Status.Equals("Posted", StringComparison.OrdinalIgnoreCase))
            return (false, "not_posted", null);
        if (original.ReversalOfVoucherId.HasValue)
            return (false, "reversal_cannot_be_reversed", null);

        var existingReversal = await _db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .FirstOrDefaultAsync(voucher => voucher.ReversalOfVoucherId == original.Id);
        if (existingReversal is not null)
            return (false, "already_reversed", existingReversal);

        if (string.IsNullOrWhiteSpace(reason))
            return (false, "reason_required", null);
        if (reason.Trim().Length > 150)
            return (false, "reason_too_long", null);
        if (reversalDate == default)
            return (false, "reversal_date_required", null);

        var normalizedDate = reversalDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        if (await _db.AccountingPeriods.AnyAsync(period =>
                period.IsClosed &&
                period.StartDate <= reversalDate &&
                period.EndDate >= reversalDate))
            return (false, "period_closed", null);

        var reversal = new AccountingVoucher
        {
            VoucherNumber = DocumentNumberGenerator.Generate("VREV"),
            VoucherDate = normalizedDate,
            VoucherType = "Journal",
            Description = $"沖銷傳票 {original.VoucherNumber}: {reason.Trim()}",
            ReferenceType = "AccountingVoucherReversal",
            ReferenceId = original.Id,
            PostingPurpose = "reversal",
            ReversalOfVoucherId = original.Id,
            Status = "Posted",
            PostedAt = DateTime.UtcNow,
            Entries = original.Entries.Select(entry => new AccountingEntry
            {
                GLAccountId = entry.GLAccountId,
                CostCenterId = entry.CostCenterId,
                DebitAmount = entry.CreditAmount,
                CreditAmount = entry.DebitAmount,
                Memo = $"沖銷 {original.VoucherNumber}: {reason.Trim()}"
            }).ToList()
        };

        try
        {
            var savedVoucher = await SavePostedVoucherAsync(reversal);
            return (true, "ok", savedVoucher);
        }
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("會計期間已關帳", StringComparison.Ordinal))
        {
            return (false, "period_closed", null);
        }
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
            PostingPurpose = "invoice",
            Status = "Posted",
            PostedAt = DateTime.UtcNow
        };

        // 1. 借方：應收帳款 (1131)
        var arAccount = await RequireMappedAccountAsync("accounts_receivable");
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = arAccount.Id,
            DebitAmount = invoice.TotalAmount,
            CreditAmount = 0,
            Memo = $"客戶應收帳款 - {invoice.InvoiceNumber}"
        });

        // 2. 貸方：銷貨收入 (4101)
        var revenueAccount = await RequireMappedAccountAsync("sales_revenue");
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = revenueAccount.Id,
            DebitAmount = 0,
            CreditAmount = invoice.SalesAmount,
            Memo = $"銷貨收入 - {invoice.InvoiceNumber}"
        });

        // 3. 貸方：銷項稅額 (2201)
        if (invoice.TaxAmount > 0)
        {
            var taxAccount = await RequireMappedAccountAsync("output_tax");
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = taxAccount.Id,
                DebitAmount = 0,
                CreditAmount = invoice.TaxAmount,
                Memo = $"銷項稅額 - {invoice.InvoiceNumber}"
            });
        }

        return await SavePostedVoucherAsync(voucher);
    }

    public async Task<AccountingVoucher> CreateVoucherFromSalesCreditNoteAsync(Guid creditNoteId)
    {
        var existing = await _db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .FirstOrDefaultAsync(voucher =>
                voucher.ReferenceType == "SalesCreditNote" &&
                voucher.ReferenceId == creditNoteId &&
                voucher.PostingPurpose == "credit_note");
        if (existing is not null)
            return existing;

        var note = await _db.SalesCreditNotes
            .Include(item => item.SalesInvoice)
            .FirstOrDefaultAsync(item => item.Id == creditNoteId)
            ?? throw new ArgumentException("找不到銷貨折讓單", nameof(creditNoteId));
        if (note.Status != "Approved")
            throw new InvalidOperationException("銷貨折讓單尚未完成必要簽核");

        var discountAccount = await RequireMappedAccountAsync("sales_discount");
        var receivableAccount = await RequireMappedAccountAsync("accounts_receivable");
        var taxAccount = note.TaxAmount > 0
            ? await RequireMappedAccountAsync("output_tax")
            : null;
        if (note.ReceivableAmount < 0 ||
            note.CustomerAdvanceAmount < 0 ||
            note.ReceivableAmount + note.CustomerAdvanceAmount != note.TotalAmount)
            throw new InvalidOperationException("折讓單的應收帳款與客戶未沖款分配金額不平衡");

        var receivableCredit = note.ReceivableAmount;
        var advanceCredit = note.CustomerAdvanceAmount;
        var advanceAccount = advanceCredit > 0
            ? await RequireMappedAccountAsync("customer_advances")
            : null;

        var voucher = new AccountingVoucher
        {
            VoucherNumber = DocumentNumberGenerator.Generate("VCN"),
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            Description = $"銷貨折讓過帳 - {note.CreditNoteNumber}",
            ReferenceType = "SalesCreditNote",
            ReferenceId = note.Id,
            PostingPurpose = "credit_note",
            Status = "Posted",
            PostedAt = DateTime.UtcNow,
            Entries =
            {
                new AccountingEntry
                {
                    GLAccountId = discountAccount.Id,
                    DebitAmount = note.NetAmount,
                    Memo = $"銷貨折讓 - {note.CreditNoteNumber}"
                }
            }
        };

        if (note.TaxAmount > 0)
        {
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = taxAccount!.Id,
                DebitAmount = note.TaxAmount,
                Memo = $"銷項稅額折讓 - {note.CreditNoteNumber}"
            });
        }
        if (receivableCredit > 0)
        {
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = receivableAccount.Id,
                CreditAmount = receivableCredit,
                Memo = $"應收帳款折讓沖抵 - {note.CreditNoteNumber}"
            });
        }
        if (advanceCredit > 0)
        {
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = advanceAccount!.Id,
                CreditAmount = advanceCredit,
                Memo = $"客戶未沖款折讓餘額 - {note.CreditNoteNumber}"
            });
        }

        return await SavePostedVoucherAsync(voucher);
    }

    public async Task<AccountingVoucher> CreateVoucherFromSupplierDebitNoteAsync(
        Guid debitNoteId,
        DateTime voucherDate)
    {
        var existing = await _db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .FirstOrDefaultAsync(voucher =>
                voucher.ReferenceType == "SupplierDebitNote" &&
                voucher.ReferenceId == debitNoteId &&
                voucher.PostingPurpose == "supplier_debit_note");
        if (existing is not null)
            return existing;

        var note = await _db.SupplierDebitNotes
            .Include(item => item.AccountsPayable)
            .FirstOrDefaultAsync(item => item.Id == debitNoteId)
            ?? throw new ArgumentException("找不到採購借項單", nameof(debitNoteId));
        if (note.Status != "Approved")
            throw new InvalidOperationException("採購借項單尚未完成簽核");

        var payableAccount = await RequireMappedAccountAsync("accounts_payable");
        var inventoryAllowanceAccount = await RequireMappedAccountAsync("purchase_allowance_inventory");
        var cogsAllowanceAccount = await RequireMappedAccountAsync("purchase_allowance_cogs");
        var inputTaxAccount = note.BaseTaxAmount > 0
            ? await RequireMappedAccountAsync("input_tax")
            : null;
        var creditTotal = note.BaseInventoryAllocation +
                          note.BaseCogsAllocation +
                          note.BaseTaxAmount;
        if (note.BaseTotalAmount <= 0 || creditTotal != note.BaseTotalAmount ||
            note.BaseInventoryAllocation < 0 || note.BaseCogsAllocation < 0)
            throw new InvalidOperationException("採購借項本幣分攤金額不平衡");

        var voucher = new AccountingVoucher
        {
            VoucherNumber = DocumentNumberGenerator.Generate("VSDN"),
            VoucherDate = voucherDate,
            VoucherType = "Journal",
            Description = $"採購借項自動過帳 - {note.DebitNoteNumber}",
            ReferenceType = "SupplierDebitNote",
            ReferenceId = note.Id,
            PostingPurpose = "supplier_debit_note",
            Status = "Posted",
            PostedAt = DateTime.UtcNow,
            Entries =
            {
                new AccountingEntry
                {
                    GLAccountId = payableAccount.Id,
                    DebitAmount = note.BaseTotalAmount,
                    Memo = $"沖減應付帳款 - {note.DebitNoteNumber}"
                }
            }
        };

        if (note.BaseInventoryAllocation > 0)
        {
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = inventoryAllowanceAccount.Id,
                CreditAmount = note.BaseInventoryAllocation,
                Memo = $"採購折讓-存貨 - {note.DebitNoteNumber}"
            });
        }
        if (note.BaseCogsAllocation > 0)
        {
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = cogsAllowanceAccount.Id,
                CreditAmount = note.BaseCogsAllocation,
                Memo = $"採購折讓-銷貨成本 - {note.DebitNoteNumber}"
            });
        }
        if (note.BaseTaxAmount > 0)
        {
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = inputTaxAccount!.Id,
                CreditAmount = note.BaseTaxAmount,
                Memo = $"沖回進項稅額 - {note.DebitNoteNumber}"
            });
        }

        return await SavePostedVoucherAsync(voucher);
    }

    public async Task<AccountingVoucher> CreateVoucherFromCashReceiptAsync(Guid receiptId)
    {
        var receipt = await _db.CashReceipts
            .Include(item => item.Allocations)
            .Include(item => item.BankAccount)
                .ThenInclude(account => account!.GLAccount)
            .FirstOrDefaultAsync(item => item.Id == receiptId)
            ?? throw new ArgumentException("找不到收款單");

        var existing = await _db.AccountingVouchers
            .FirstOrDefaultAsync(voucher =>
                voucher.ReferenceType == "CashReceipt" && voucher.ReferenceId == receiptId);
        if (existing != null)
            return existing;

        if (receipt.BankAccount?.GLAccount is not { IsActive: true, IsCashAccount: true } cashAccount)
            throw new InvalidOperationException("收款帳戶尚未對應有效的現金／銀行總帳科目");

        var allocatedAmount = receipt.Allocations.Sum(allocation => allocation.AllocatedAmount);
        var discountAmount = receipt.Allocations.Sum(allocation => allocation.DiscountAmount);
        var unappliedAmount = receipt.ReceivedAmount - allocatedAmount;
        if (allocatedAmount < 0 || discountAmount != receipt.DiscountAmount || unappliedAmount < 0)
            throw new InvalidOperationException("收款分配金額、折讓金額與實收金額不一致");
        if (discountAmount > 0)
            throw new InvalidOperationException("銷貨折讓過帳尚待財務確認稅額處理政策");

        var arAccount = await RequireMappedAccountAsync("accounts_receivable");
        var advancesAccount = unappliedAmount > 0
            ? await GetMappedAccountAsync("customer_advances")
                ?? throw new InvalidOperationException("未設定有效的客戶預收款／未沖款總帳科目映射")
            : null;
        var voucher = new AccountingVoucher
        {
            VoucherNumber = $"V{DateTime.UtcNow:yyyyMMdd}-{receipt.ReceiptNumber}",
            VoucherDate = receipt.ReceiptDate,
            VoucherType = "CashReceipt",
            Description = $"收款沖帳自動過帳 - {receipt.ReceiptNumber}",
            ReferenceType = "CashReceipt",
            ReferenceId = receiptId,
            PostingPurpose = "receipt",
            Status = "Posted",
            PostedAt = DateTime.UtcNow,
            Entries =
            {
                new AccountingEntry
                {
                    GLAccountId = cashAccount.Id,
                    DebitAmount = receipt.ReceivedAmount,
                    Memo = $"收款入帳 - {receipt.ReceiptNumber}"
                },
            }
        };

        if (allocatedAmount > 0)
        {
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = arAccount.Id,
                CreditAmount = allocatedAmount,
                Memo = $"應收帳款沖帳 - {receipt.ReceiptNumber}"
            });
        }
        if (unappliedAmount > 0)
        {
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = advancesAccount!.Id,
                CreditAmount = unappliedAmount,
                Memo = $"客戶未沖款 - {receipt.ReceiptNumber}"
            });
        }
        return await SavePostedVoucherAsync(voucher);
    }

    public async Task<string?> ValidateCashReceiptMappingsAsync(bool requiresUnapplied)
    {
        if (await GetMappedAccountAsync("accounts_receivable") is null)
            return "accounts_receivable_mapping_required";
        if (requiresUnapplied && await GetMappedAccountAsync("customer_advances") is null)
            return "unapplied_account_mapping_required";
        return null;
    }

    public async Task<string?> ValidateInvoiceMappingsAsync(bool requiresTax)
    {
        foreach (var mappingKey in requiresTax
                     ? new[] { "accounts_receivable", "sales_revenue", "output_tax" }
                     : new[] { "accounts_receivable", "sales_revenue" })
        {
            if (await GetMappedAccountAsync(mappingKey) is null)
                return $"{mappingKey}_mapping_required";
        }

        return null;
    }

    public async Task<AccountingVoucher> CreateVoucherFromCashReceiptApplicationAsync(
        Guid receiptId,
        Guid allocationId,
        decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "沖轉金額必須大於零");

        var existing = await _db.AccountingVouchers
            .FirstOrDefaultAsync(voucher =>
                voucher.ReferenceType == "CashReceiptApplication" &&
                voucher.ReferenceId == allocationId);
        if (existing is not null)
            return existing;

        var receipt = await _db.CashReceipts.FirstOrDefaultAsync(item => item.Id == receiptId)
            ?? throw new ArgumentException("找不到收款單");
        var advancesAccount = await GetMappedAccountAsync("customer_advances")
            ?? throw new InvalidOperationException("未設定有效的客戶預收款／未沖款總帳科目映射");
        var arAccount = await RequireMappedAccountAsync("accounts_receivable");

        var voucher = new AccountingVoucher
        {
            VoucherNumber = DocumentNumberGenerator.Generate("VRC"),
            VoucherDate = receipt.ReceiptDate,
            VoucherType = "Journal",
            Description = $"客戶未沖款轉應收 - {receipt.ReceiptNumber}",
            ReferenceType = "CashReceiptApplication",
            ReferenceId = allocationId,
            PostingPurpose = "application",
            Status = "Posted",
            PostedAt = DateTime.UtcNow,
            Entries =
            {
                new AccountingEntry
                {
                    GLAccountId = advancesAccount.Id,
                    DebitAmount = amount,
                    Memo = $"沖轉客戶預收款 - {receipt.ReceiptNumber}"
                },
                new AccountingEntry
                {
                    GLAccountId = arAccount.Id,
                    CreditAmount = amount,
                    Memo = $"沖轉應收帳款 - {receipt.ReceiptNumber}"
                }
            }
        };

        return await SavePostedVoucherAsync(voucher);
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
            PostingPurpose = "invoice",
            Status = "Posted",
            PostedAt = DateTime.UtcNow
        };

        // 1. 借方：存貨/原料 (1201 - 假設代碼)
        var inventoryAccount = await RequireMappedAccountAsync("raw_material_inventory");
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
            var taxAccount = await RequireMappedAccountAsync("input_tax");
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = taxAccount.Id,
                DebitAmount = ap.TaxAmount,
                CreditAmount = 0,
                Memo = $"進項稅額 - {ap.ApNumber}"
            });
        }

        // 3. 貸方：應付帳款 (2101)
        var apAccount = await RequireMappedAccountAsync("accounts_payable");
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = apAccount.Id,
            DebitAmount = 0,
            CreditAmount = ap.TotalAmount,
            Memo = $"供應商應付帳款 - {ap.ApNumber}"
        });

        return await SavePostedVoucherAsync(voucher);
    }

    /// <summary>
    /// 從生產完工入庫產生會計傳票 (成本結轉: WIP -> Finished Goods)
    /// </summary>
    public async Task<AccountingVoucher> CreateVoucherFromProductionReceiptAsync(Guid workOrderId, decimal quantity, decimal costingAmount)
    {
        var wo = await _db.WorkOrders
            .FirstOrDefaultAsync(w => w.Id == workOrderId);

        if (wo == null) throw new ArgumentException("找不到對應工單");

        var existing = await _db.AccountingVouchers
            .FirstOrDefaultAsync(v => v.ReferenceType == "WorkOrder" && v.ReferenceId == workOrderId);
        if (existing != null) return existing;

        var voucher = new AccountingVoucher
        {
            VoucherNumber = $"V{DateTime.UtcNow:yyyyMMdd}-PR-{wo.WoNumber}",
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            Description = $"完工入庫自動過帳 - 工單: {wo.WoNumber}, 數量: {quantity}",
            ReferenceType = "WorkOrder",
            ReferenceId = workOrderId,
            PostingPurpose = "production_receipt",
            Status = "Posted",
            PostedAt = DateTime.UtcNow
        };

        // 1. 借方：存貨-成品 (1205)
        var fgAccount = await RequireMappedAccountAsync("finished_goods_inventory");
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = fgAccount.Id,
            DebitAmount = costingAmount,
            CreditAmount = 0,
            Memo = $"成品入庫 - 工單: {wo.WoNumber}"
        });

        // 2. 貸方：在製品 (1206)
        var wipAccount = await RequireMappedAccountAsync("work_in_progress");
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = wipAccount.Id,
            DebitAmount = 0,
            CreditAmount = costingAmount,
            Memo = $"WIP 結轉 - 工單: {wo.WoNumber}"
        });

        return await SavePostedVoucherAsync(voucher);
    }

    public async Task<AccountingVoucher> CreateVoucherFromManufacturingReceiptAsync(
        Guid receiptId,
        Guid workOrderId,
        decimal quantity,
        decimal costingAmount)
    {
        var existing = await _db.AccountingVouchers
            .FirstOrDefaultAsync(voucher => voucher.ReferenceType == "ProductionReceipt" &&
                voucher.ReferenceId == receiptId);
        if (existing is not null)
            return existing;

        var workOrder = await _db.WorkOrders.FirstOrDefaultAsync(order => order.Id == workOrderId)
            ?? throw new ArgumentException("找不到對應工單");
        var finishedGoodsAccount = await RequireMappedAccountAsync("finished_goods_inventory");
        var workInProgressAccount = await RequireMappedAccountAsync("work_in_progress");

        var voucher = new AccountingVoucher
        {
            VoucherNumber = $"V{DateTime.UtcNow:yyyyMMdd}-MGR-{receiptId:N}"[..30],
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            Description = $"製造完工入庫 - 工單: {workOrder.WoNumber}, 數量: {quantity}",
            ReferenceType = "ProductionReceipt",
            ReferenceId = receiptId,
            PostingPurpose = "production_receipt",
            Status = "Posted",
            PostedAt = DateTime.UtcNow
        };
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = finishedGoodsAccount.Id,
            DebitAmount = costingAmount,
            CreditAmount = 0,
            Memo = $"成品入庫 - {workOrder.WoNumber}"
        });
        voucher.Entries.Add(new AccountingEntry
        {
            GLAccountId = workInProgressAccount.Id,
            DebitAmount = 0,
            CreditAmount = costingAmount,
            Memo = $"WIP 結轉 - {workOrder.WoNumber}"
        });
        return await SavePostedVoucherAsync(voucher);
    }

    public async Task<string?> ValidateDeliveryCogsMappingsAsync()
    {
        foreach (var mappingKey in new[]
                 {
                     "sales_cogs",
                     "finished_goods_inventory"
                 })
        {
            if (await GetMappedAccountAsync(mappingKey) is null)
                return mappingKey;
        }

        return null;
    }

    public async Task<AccountingVoucher> CreateVoucherFromDeliveryAsync(
        Guid deliveryOrderId,
        string deliveryNumber,
        decimal costingAmount)
    {
        if (costingAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(costingAmount), "銷貨成本不可為負數");

        var existing = await _db.AccountingVouchers
            .FirstOrDefaultAsync(voucher => voucher.ReferenceType == "DeliveryOrder" &&
                                           voucher.ReferenceId == deliveryOrderId);
        if (existing is not null)
            return existing;

        var cogsAccount = await GetMappedAccountAsync("sales_cogs")
            ?? throw new InvalidOperationException("未設定有效的銷貨成本總帳科目映射");
        var inventoryAccount = await GetMappedAccountAsync("finished_goods_inventory")
            ?? throw new InvalidOperationException("未設定有效的成品存貨總帳科目映射");
        var roundedCost = Math.Round(costingAmount, 2);
        var voucher = new AccountingVoucher
        {
            VoucherNumber = DocumentNumberGenerator.Generate("VCOGS"),
            VoucherDate = DateTime.UtcNow,
            VoucherType = "Journal",
            Description = $"銷貨成本結轉 - {deliveryNumber}",
            ReferenceType = "DeliveryOrder",
            ReferenceId = deliveryOrderId,
            PostingPurpose = "sales_cogs",
            Status = "Posted",
            PostedAt = DateTime.UtcNow,
            Entries =
            {
                new AccountingEntry
                {
                    GLAccountId = cogsAccount.Id,
                    DebitAmount = roundedCost,
                    Memo = $"銷貨成本 - {deliveryNumber}"
                },
                new AccountingEntry
                {
                    GLAccountId = inventoryAccount.Id,
                    CreditAmount = roundedCost,
                    Memo = $"成品存貨出庫 - {deliveryNumber}"
                }
            }
        };

        return await SavePostedVoucherAsync(voucher);
    }

    private async Task<GLAccount?> GetMappedAccountAsync(string mappingKey)
    {
        var definition = AccountingMappingKeys.All
            .FirstOrDefault(item => item.Key == mappingKey);
        if (definition is null)
            throw new ArgumentException($"未知的總帳科目映射：{mappingKey}", nameof(mappingKey));

        var mapping = await _db.GLAccountMappings
            .Include(item => item.GLAccount)
            .FirstOrDefaultAsync(item => item.MappingKey == mappingKey);
        var account = mapping?.GLAccount;
        if (account is null || !account.IsActive ||
            !account.AccountType.Equals(definition.AccountType, StringComparison.OrdinalIgnoreCase))
            return null;

        return account;
    }

    private async Task<GLAccount> RequireMappedAccountAsync(string mappingKey)
    {
        return await GetMappedAccountAsync(mappingKey)
            ?? throw new InvalidOperationException($"未設定有效的總帳科目映射: {mappingKey}");
    }

    private async Task<AccountingVoucher> SavePostedVoucherAsync(AccountingVoucher voucher)
    {
        AccountingVoucherRules.ValidateForPosting(voucher);
        var finalStatus = voucher.Status;
        voucher.Status = "Draft";
        _db.AccountingVouchers.Add(voucher);
        var transaction = _db.Database.CurrentTransaction is null
            ? await _db.Database.BeginTransactionAsync()
            : null;
        try
        {
            await _db.SaveChangesAsync();
            voucher.Status = finalStatus;
            await _db.SaveChangesAsync();
            if (transaction is not null)
                await transaction.CommitAsync();
            return voucher;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgresException &&
            postgresException.SqlState == "55000" &&
            postgresException.MessageText.Contains("closed accounting period", StringComparison.OrdinalIgnoreCase))
        {
            if (transaction is not null)
                await transaction.RollbackAsync();
            throw new InvalidOperationException("傳票日期所屬會計期間已關帳，禁止過帳", exception);
        }
        catch (DbUpdateException exception) when (
            transaction is not null &&
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_AccountingVouchers_ReferenceType_ReferenceId_PostingPurpose"
                    or "IX_AccountingVouchers_ReversalOfVoucherId"
            })
        {
            if (transaction is null)
                throw;
            await transaction.RollbackAsync();
            await transaction.DisposeAsync();
            transaction = null;
            foreach (var entry in voucher.Entries)
                _db.Entry(entry).State = EntityState.Detached;
            _db.Entry(voucher).State = EntityState.Detached;

            var existing = await _db.AccountingVouchers
                .Include(item => item.Entries)
                .FirstOrDefaultAsync(item =>
                voucher.ReversalOfVoucherId.HasValue
                    ? item.ReversalOfVoucherId == voucher.ReversalOfVoucherId
                    : item.ReferenceType == voucher.ReferenceType &&
                      item.ReferenceId == voucher.ReferenceId &&
                      item.PostingPurpose == voucher.PostingPurpose);
            if (existing is null)
                throw;

            return existing;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    public async Task<AccountingVoucher?> CreateVoucherForWorkOrderCostSettlementAsync(
        Guid workOrderId,
        decimal actualCost,
        IReadOnlyList<WorkOrderCostVarianceAllocation> abnormalCostAllocations)
    {
        ArgumentNullException.ThrowIfNull(abnormalCostAllocations);
        if (abnormalCostAllocations.Any(allocation =>
                allocation.WorkOrderId != workOrderId ||
                allocation.Amount <= 0 ||
                decimal.Round(allocation.Amount, 2) != allocation.Amount ||
                !allocation.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("成本差異分攤必須是該工單已核准的正數金額", nameof(abnormalCostAllocations));
        var abnormalCost = abnormalCostAllocations.Sum(allocation => allocation.Amount);
        if (actualCost < 0 || abnormalCost < 0 || abnormalCost > actualCost)
            throw new ArgumentOutOfRangeException(nameof(abnormalCost), "異常成本必須介於零與工單實際成本之間");
        var workOrder = await _db.WorkOrders.FirstOrDefaultAsync(w => w.Id == workOrderId);
        if (workOrder == null)
            throw new ArgumentException("找不到對應工單");

        var existing = await _db.AccountingVouchers
            .Include(voucher => voucher.Entries)
            .FirstOrDefaultAsync(v => v.ReferenceType == "WorkOrderCostSettlement" && v.ReferenceId == workOrderId);
        if (existing is not null &&
            !existing.Status.Equals("Posted", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("工單成本結算傳票存在但尚未過帳，請先完成人工核對");

        var finishedGoodsAccount = await RequireMappedAccountAsync("finished_goods_inventory");
        var workInProgressAccount = await RequireMappedAccountAsync("work_in_progress");
        var varianceAccount = abnormalCost > 0
            ? await RequireMappedAccountAsync("production_variance")
            : null;
        if (finishedGoodsAccount.Id == workInProgressAccount.Id ||
            (varianceAccount is not null &&
             (varianceAccount.Id == finishedGoodsAccount.Id || varianceAccount.Id == workInProgressAccount.Id)))
            throw new InvalidOperationException("工單成本結算所需的成品、WIP 與差異科目映射必須彼此不同");

        var postedFinishedGoodsCost = await (
            from entry in _db.AccountingEntries
            join receiptVoucher in _db.AccountingVouchers on entry.VoucherId equals receiptVoucher.Id
            where ((receiptVoucher.ReferenceType == "WorkOrder"
                    && receiptVoucher.ReferenceId == workOrderId)
                || (receiptVoucher.ReferenceType == "ProductionReceipt"
                    && _db.ProductionReceipts.Any(receipt =>
                        receipt.Id == receiptVoucher.ReferenceId && receipt.WorkOrderId == workOrderId)))
                && receiptVoucher.Status == "Posted"
                && entry.GLAccountId == finishedGoodsAccount.Id
            select entry.DebitAmount - entry.CreditAmount
        ).SumAsync();

        var capitalizedCost = Math.Round(actualCost - abnormalCost, 2);
        var finishedGoodsAdjustment = Math.Round(capitalizedCost - postedFinishedGoodsCost, 2);
        var workInProgressAdjustment = Math.Round(actualCost - postedFinishedGoodsCost, 2);
        if (existing is not null)
        {
            var existingNetByAccount = existing.Entries
                .GroupBy(entry => entry.GLAccountId)
                .ToDictionary(group => group.Key, group =>
                    Math.Round(group.Sum(entry => entry.DebitAmount - entry.CreditAmount), 2));
            var expectedNetByAccount = new Dictionary<Guid, decimal>
            {
                [finishedGoodsAccount.Id] = finishedGoodsAdjustment,
                [workInProgressAccount.Id] = -workInProgressAdjustment
            };
            if (varianceAccount is not null)
                expectedNetByAccount[varianceAccount.Id] = abnormalCost;
            if (expectedNetByAccount.All(pair =>
                    existingNetByAccount.GetValueOrDefault(pair.Key) == pair.Value) &&
                existingNetByAccount.All(pair => expectedNetByAccount.ContainsKey(pair.Key)))
                return existing;
            throw new InvalidOperationException("工單已有成本結算傳票，但金額與目前成本快照不一致");
        }

        if (finishedGoodsAdjustment == 0 && workInProgressAdjustment == 0 && abnormalCost == 0)
            return null;

        var voucherDate = DateTime.UtcNow;
        var postingDate = DateOnly.FromDateTime(voucherDate);
        if (await _db.AccountingPeriods.AnyAsync(period =>
                period.IsClosed &&
                period.StartDate <= postingDate &&
                period.EndDate >= postingDate))
            throw new InvalidOperationException("傳票日期所屬會計期間已關帳，禁止過帳");

        var voucher = new AccountingVoucher
        {
            VoucherNumber = $"CS{voucherDate:yyMMdd}{Guid.NewGuid():N}"[..30],
            VoucherDate = voucherDate,
            VoucherType = "Journal",
            Description = $"工單成本結轉差額 - {workOrder.WoNumber}",
            ReferenceType = "WorkOrderCostSettlement",
            ReferenceId = workOrderId,
            PostingPurpose = "work_order_settlement",
            Status = "Posted",
            PostedAt = DateTime.UtcNow
        };

        if (finishedGoodsAdjustment != 0)
        {
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = finishedGoodsAccount.Id,
                DebitAmount = Math.Max(finishedGoodsAdjustment, 0),
                CreditAmount = Math.Max(-finishedGoodsAdjustment, 0),
                Memo = $"工單可資本化實際成本調整 - {workOrder.WoNumber}"
            });
        }

        if (varianceAccount is not null)
        {
            foreach (var allocation in abnormalCostAllocations)
            {
                var memo = $"差異 {allocation.Id:N} {allocation.ReasonCode} {allocation.SourceType}:{allocation.SourceId:N} {allocation.Reason}";
                voucher.Entries.Add(new AccountingEntry
                {
                    GLAccountId = varianceAccount.Id,
                    DebitAmount = allocation.Amount,
                    CreditAmount = 0,
                    Memo = memo.Length <= 200 ? memo : memo[..200]
                });
            }
        }

        if (workInProgressAdjustment != 0)
        {
            voucher.Entries.Add(new AccountingEntry
            {
                GLAccountId = workInProgressAccount.Id,
                DebitAmount = Math.Max(-workInProgressAdjustment, 0),
                CreditAmount = Math.Max(workInProgressAdjustment, 0),
                Memo = $"工單在製品成本結清 - {workOrder.WoNumber}"
            });
        }

        return await SavePostedVoucherAsync(voucher);
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
