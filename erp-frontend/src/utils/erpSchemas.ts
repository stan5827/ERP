import { z } from 'zod';

/**
 * 企業 ERP 核心資料驗證 Schema (Zod)
 * 符合 user_global 規範：必填檢查、邊界檢查
 */

// 1. 幣別主檔驗證
export const currencySchema = z.object({
  currencyCode: z.string().min(2, '幣別代碼至少 2 碼').max(10, '幣別代碼太長'),
  currencyName: z.string().min(1, '請輸入幣別名稱').max(50),
  symbol: z.string().max(10).optional(),
  isBaseCurrency: z.boolean().default(false),
  isActive: z.boolean().default(true),
});

// 2. 匯率資料驗證
export const exchangeRateSchema = z.object({
  fromCurrency: z.string().min(1, '請選擇來源幣別'),
  toCurrency: z.string().min(1, '請選擇目標幣別'),
  rate: z.number().positive('匯率必須大於 0').max(1000000, '匯率數值異常'),
  effectiveDate: z.string().min(1, '請選擇生效日期'),
  rateType: z.enum(['Standard', 'Custom', 'Closing']),
  source: z.string().min(1, '請註明資料來源'),
});

// 3. RCCP 資源驗證
export const rccpResourceSchema = z.object({
  resourceCode: z.string().min(3, '資源代碼至少 3 碼').max(20),
  resourceName: z.string().min(1, '請輸入資源名稱').max(100),
  capacityPerBucket: z.number().nonnegative('產能不可為負數').max(10000, '時格產能上限異常'),
  unit: z.string().min(1, '請指定產能單位 (如 Hours)'),
  isActive: z.boolean().default(true),
});

// 4. Capacity Bill (品項資源耗用) 驗證
export const capacityBillSchema = z.object({
  resourceId: z.string().uuid('無效的資源 ID'),
  usagePerUnit: z.number().positive('單位耗用量必須大於 0').max(1000, '耗用量數值異常'),
  notes: z.string().max(200).optional(),
});

export type CurrencyForm = z.infer<typeof currencySchema>;
export type ExchangeRateForm = z.infer<typeof exchangeRateSchema>;
export type RccpResourceForm = z.infer<typeof rccpResourceSchema>;
export type CapacityBillForm = z.infer<typeof capacityBillSchema>;
