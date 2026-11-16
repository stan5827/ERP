using ErpBackend.Models.Finance;
using Xunit;

namespace ErpBackend.Tests.Services;

public class AccountingVoucherRulesTests
{
    [Fact]
    public void ValidateForPosting_AcceptsBalancedVoucherWithOneSidedLines()
    {
        var voucher = CreatePostedVoucher(
            new AccountingEntry { DebitAmount = 125.50m },
            new AccountingEntry { CreditAmount = 125.50m });

        var exception = Record.Exception(() => AccountingVoucherRules.ValidateForPosting(voucher));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateForPosting_RejectsUnbalancedVoucher()
    {
        var voucher = CreatePostedVoucher(
            new AccountingEntry { DebitAmount = 125.50m },
            new AccountingEntry { CreditAmount = 125m });

        var exception = Assert.Throws<InvalidOperationException>(
            () => AccountingVoucherRules.ValidateForPosting(voucher));

        Assert.Contains("借貸不平衡", exception.Message);
    }

    [Fact]
    public void ValidateForPosting_RejectsLinesWithBothDebitAndCredit()
    {
        var voucher = CreatePostedVoucher(
            new AccountingEntry { DebitAmount = 100m, CreditAmount = 1m },
            new AccountingEntry { CreditAmount = 101m });

        var exception = Assert.Throws<InvalidOperationException>(
            () => AccountingVoucherRules.ValidateForPosting(voucher));

        Assert.Contains("必須且只能有一側金額大於零", exception.Message);
    }

    [Fact]
    public void ValidateForPosting_RejectsZeroOrNegativeLines()
    {
        var voucher = CreatePostedVoucher(
            new AccountingEntry { DebitAmount = 0m, CreditAmount = 0m },
            new AccountingEntry { CreditAmount = 0m });

        var exception = Assert.Throws<InvalidOperationException>(
            () => AccountingVoucherRules.ValidateForPosting(voucher));

        Assert.Contains("必須且只能有一側金額大於零", exception.Message);
    }

    [Fact]
    public void ValidateForPosting_RequiresAtLeastTwoLines()
    {
        var voucher = CreatePostedVoucher(new AccountingEntry { DebitAmount = 10m });

        var exception = Assert.Throws<InvalidOperationException>(
            () => AccountingVoucherRules.ValidateForPosting(voucher));

        Assert.Contains("至少需要兩筆分錄", exception.Message);
    }

    [Fact]
    public void ValidateForPosting_RequiresPurposeForReferencedVoucher()
    {
        var voucher = CreatePostedVoucher(
            new AccountingEntry { DebitAmount = 10m },
            new AccountingEntry { CreditAmount = 10m });
        voucher.ReferenceType = "SalesInvoice";
        voucher.ReferenceId = Guid.NewGuid();

        var exception = Assert.Throws<InvalidOperationException>(
            () => AccountingVoucherRules.ValidateForPosting(voucher));

        Assert.Contains("必須指定過帳用途", exception.Message);
    }

    [Fact]
    public void ValidateForPosting_DoesNotBlockDraftVoucher()
    {
        var voucher = CreatePostedVoucher(new AccountingEntry { DebitAmount = 10m });
        voucher.Status = "Draft";

        var exception = Record.Exception(() => AccountingVoucherRules.ValidateForPosting(voucher));

        Assert.Null(exception);
    }

    private static AccountingVoucher CreatePostedVoucher(params AccountingEntry[] entries) =>
        new()
        {
            Status = "Posted",
            Entries = entries
        };
}
