import { z } from 'zod';

export const voucherSchema = z.object({
  voucherDate: z.string(),
  description: z.string().min(2, '請輸入分錄摘要'),
  entries: z.array(z.object({
    accountId: z.string().uuid('請選擇科目'),
    debitAmount: z.number().nonnegative(),
    creditAmount: z.number().nonnegative(),
    description: z.string().optional(),
  })).min(2, '分錄至少需要兩筆（借貸平衡）'),
}).refine(data => {
  const totalDebit = data.entries.reduce((sum, e) => sum + e.debitAmount, 0);
  const totalCredit = data.entries.reduce((sum, e) => sum + e.creditAmount, 0);
  return Math.abs(totalDebit - totalCredit) < 0.01;
}, {
  message: "借貸不平衡，請檢查金額",
  path: ["entries"]
});

export const accountSchema = z.object({
  accountCode: z.string().min(3, '科目代碼至少 3 碼'),
  accountName: z.string().min(2, '科目名稱為必填'),
  accountType: z.enum(['asset', 'liability', 'equity', 'revenue', 'expense']),
  parentId: z.string().uuid().optional().nullable(),
});
