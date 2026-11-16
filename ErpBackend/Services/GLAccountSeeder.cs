using Microsoft.EntityFrameworkCore;
using ErpBackend.Data;
using ErpBackend.Models.Finance;

namespace ErpBackend.Services;

public static class GLAccountSeeder
{
    private static readonly IReadOnlyList<LegacyGLAccountDefinition> LegacyAccounts =
    [
        new("1101", "現金", "1101", "現金及約當現金"),
        new("1103", "銀行存款", "1111", "銀行存款"),
        new("1131", "應收帳款", "1136", "應收帳款"),
        new("1201", "存貨-原料", "1201", "原料"),
        new("1205", "存貨-成品", "1241", "製成品"),
        new("1206", "在製品 (WIP)", "1221", "在製品"),
        new("2101", "應付帳款", "2141", "應付帳款"),
        new("4101", "銷貨收入", "4111", "產品銷售收入")
    ];

    private static readonly IReadOnlyList<GLAccountSeedDefinition> Accounts =
    [
        new("1101", "現金及約當現金", "Asset", IsCashAccount: true),
        new("1111", "銀行存款", "Asset", "1101", IsCashAccount: true),
        new("1136", "應收帳款", "Asset"),
        new("1137", "備抵呆帳－應收帳款", "Asset"),
        new("1150", "其他應收款", "Asset"),
        new("1201", "原料", "Asset"),
        new("1202", "進項稅額", "Asset"),
        new("1211", "物料", "Asset"),
        new("1221", "在製品", "Asset"),
        new("1231", "半成品", "Asset"),
        new("1241", "製成品", "Asset"),
        new("1251", "委外加工存貨", "Asset"),
        new("1270", "預付貨款／預付款項", "Asset"),
        new("1280", "預付費用", "Asset"),
        new("1501", "不動產、廠房及設備", "Asset"),
        new("1502", "土地", "Asset", "1501"),
        new("1503", "房屋及建築", "Asset", "1501"),
        new("1504", "機器設備", "Asset", "1501"),
        new("1505", "模具設備", "Asset", "1501"),
        new("1506", "運輸設備", "Asset", "1501"),
        new("1507", "辦公設備", "Asset", "1501"),
        new("1509", "累積折舊", "Asset", "1501"),
        new("1781", "無形資產", "Asset"),
        new("1841", "存出保證金", "Asset"),
        new("2101", "短期借款", "Liability"),
        new("2141", "應付帳款", "Liability"),
        new("2145", "應付委外加工款", "Liability"),
        new("2150", "其他應付款", "Liability"),
        new("2151", "應付薪資", "Liability"),
        new("2152", "應付獎金／紅利", "Liability"),
        new("2155", "應付稅捐", "Liability"),
        new("2156", "應付退休金", "Liability"),
        new("2170", "本期所得稅負債", "Liability"),
        new("2200", "預收款項／客戶訂金", "Liability"),
        new("2201", "銷項稅額", "Liability"),
        new("2280", "一年內到期長期負債", "Liability"),
        new("2541", "長期借款", "Liability"),
        new("2571", "應付公司債／長期應付票據", "Liability"),
        new("2601", "存入保證金", "Liability"),
        new("3101", "資本額／股本", "Equity"),
        new("3201", "資本公積", "Equity"),
        new("3311", "法定盈餘公積", "Equity"),
        new("3351", "特別盈餘公積", "Equity"),
        new("3411", "未分配盈餘／累積虧損", "Equity"),
        new("3501", "本期損益", "Equity"),
        new("4111", "產品銷售收入", "Revenue"),
        new("4121", "模具／專案開發收入", "Revenue"),
        new("4131", "副產品及下腳售價收入", "Revenue"),
        new("4191", "其他營業收入", "Revenue"),
        new("5101", "直接材料", "Expense"),
        new("5111", "直接人工", "Expense"),
        new("5121", "製造費用", "Expense"),
        new("5122", "間接人工", "Expense", "5121"),
        new("5123", "廠房租金", "Expense", "5121"),
        new("5124", "廠房及設備折舊", "Expense", "5121"),
        new("5125", "水電瓦斯費－廠房", "Expense", "5121"),
        new("5126", "修繕費－機器設備", "Expense", "5121"),
        new("5127", "模具耗用費", "Expense", "5121"),
        new("5128", "委外加工費", "Expense", "5121"),
        new("5129", "製造費用－其他", "Expense", "5121"),
        new("5311", "銷貨成本", "Expense"),
        new("6111", "薪資支出－業務", "Expense"),
        new("6150", "廣告及行銷費", "Expense"),
        new("6170", "差旅費－業務", "Expense"),
        new("6180", "運費及交際費", "Expense"),
        new("6211", "薪資支出－行政與管理", "Expense"),
        new("6225", "租金支出", "Expense"),
        new("6226", "郵電費／通訊費", "Expense"),
        new("6230", "修繕費", "Expense"),
        new("6240", "保險費", "Expense"),
        new("6250", "稅捐", "Expense"),
        new("6270", "膳食費", "Expense"),
        new("6280", "折舊費用", "Expense"),
        new("6300", "專業服務費", "Expense"),
        new("6511", "研究發展人員薪資", "Expense"),
        new("6520", "研究發展費／試作測試費", "Expense"),
        new("7101", "利息收入", "Revenue"),
        new("7480", "兌換利益／損失", "Revenue"),
        new("7501", "利息費用", "Expense"),
        new("7880", "其他營業外支出", "Expense")
    ];

    private static readonly IReadOnlyDictionary<string, string> DefaultMappings =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["accounts_receivable"] = "1136",
            ["accounts_payable"] = "2141",
            ["sales_revenue"] = "4111",
            ["output_tax"] = "2201",
            ["input_tax"] = "1202",
            ["raw_material_inventory"] = "1201",
            ["work_in_progress"] = "1221",
            ["finished_goods_inventory"] = "1241",
            ["sales_cogs"] = "5311"
        };

    public static async Task SeedAsync(ErpGeneralDbContext db)
    {
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync()
            : null;
        var existingAccounts = await db.GLAccounts.ToListAsync();
        var accountsByCode = new Dictionary<string, GLAccount>(StringComparer.OrdinalIgnoreCase);
        foreach (var account in existingAccounts)
            accountsByCode.TryAdd(account.AccountCode, account);

        foreach (var legacy in LegacyAccounts)
        {
            if (!accountsByCode.TryGetValue(legacy.LegacyCode, out var account) ||
                !account.AccountName.Equals(legacy.LegacyName, StringComparison.Ordinal) ||
                (legacy.AccountCode != legacy.LegacyCode &&
                 accountsByCode.ContainsKey(legacy.AccountCode)))
                continue;

            if (legacy.AccountCode != legacy.LegacyCode)
            {
                accountsByCode.Remove(legacy.LegacyCode);
                account.AccountCode = legacy.AccountCode;
                accountsByCode.Add(account.AccountCode, account);
            }
            account.AccountName = legacy.AccountName;
            var definition = Accounts.Single(item => item.AccountCode == legacy.AccountCode);
            account.AccountType = definition.AccountType;
            account.IsCashAccount = definition.IsCashAccount;
        }

        await db.SaveChangesAsync();

        foreach (var definition in Accounts)
        {
            if (accountsByCode.ContainsKey(definition.AccountCode))
                continue;

            var account = new GLAccount
            {
                AccountCode = definition.AccountCode,
                AccountName = definition.AccountName,
                AccountType = definition.AccountType,
                IsCashAccount = definition.IsCashAccount,
                ParentAccountId = definition.ParentAccountCode is not null &&
                                  accountsByCode.TryGetValue(definition.ParentAccountCode, out var parent)
                    ? parent.Id
                    : null
            };
            db.GLAccounts.Add(account);
            accountsByCode.Add(account.AccountCode, account);
        }

        var existingMappings = await db.GLAccountMappings
            .Select(mapping => mapping.MappingKey)
            .ToListAsync();
        var mappingKeys = existingMappings.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (mappingKey, accountCode) in DefaultMappings)
        {
            if (mappingKeys.Contains(mappingKey) ||
                !accountsByCode.TryGetValue(accountCode, out var account) ||
                !account.AccountType.Equals(
                    AccountingMappingKeys.All.Single(definition => definition.Key == mappingKey).AccountType,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            db.GLAccountMappings.Add(new GLAccountMapping
            {
                MappingKey = mappingKey,
                GLAccountId = account.Id,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
    }

    private sealed record LegacyGLAccountDefinition(
        string LegacyCode,
        string LegacyName,
        string AccountCode,
        string AccountName);

    private sealed record GLAccountSeedDefinition(
        string AccountCode,
        string AccountName,
        string AccountType,
        string? ParentAccountCode = null,
        bool IsCashAccount = false);
}
