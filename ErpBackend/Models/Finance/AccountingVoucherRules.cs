namespace ErpBackend.Models.Finance;

public static class AccountingVoucherRules
{
    public static void ValidateForPosting(AccountingVoucher voucher)
    {
        ArgumentNullException.ThrowIfNull(voucher);

        if (!voucher.Status.Equals("Posted", StringComparison.OrdinalIgnoreCase))
            return;

        if (!string.IsNullOrWhiteSpace(voucher.ReferenceType) &&
            voucher.ReferenceId.HasValue &&
            string.IsNullOrWhiteSpace(voucher.PostingPurpose))
            throw new InvalidOperationException("有來源單據的過帳傳票必須指定過帳用途");

        if (voucher.Entries.Count < 2)
            throw new InvalidOperationException("過帳傳票至少需要兩筆分錄");

        foreach (var entry in voucher.Entries)
        {
            if (entry.DebitAmount < 0 || entry.CreditAmount < 0 ||
                (entry.DebitAmount > 0) == (entry.CreditAmount > 0))
                throw new InvalidOperationException("每筆過帳分錄必須且只能有一側金額大於零");
        }

        var debitTotal = Math.Round(voucher.Entries.Sum(entry => entry.DebitAmount), 2);
        var creditTotal = Math.Round(voucher.Entries.Sum(entry => entry.CreditAmount), 2);
        if (debitTotal != creditTotal)
            throw new InvalidOperationException("過帳傳票借貸不平衡");
    }
}
