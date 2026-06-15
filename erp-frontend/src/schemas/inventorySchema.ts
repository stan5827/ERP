import { z } from 'zod';

export const skuSchema = z.object({
  skuNumber: z.string()
    .min(3, '品碼至少需 3 個字元')
    .max(50, '品碼長度上限為 50')
    .regex(/^[A-Z0-9-]+$/, '品碼僅限大寫字母、數字及 dash'),
  skuName: z.string().min(2, '品名至少需 2 個字元').max(100, '品名長度上限為 100'),
  itemType: z.enum(['raw', 'semi', 'finished']),
  baseUomId: z.string().uuid('請選擇有效的庫存單位'),
  safetyStock: z.number().nonnegative('安全庫存不能為負數').default(0),
  reorderPoint: z.number().nonnegative('再訂購點不能為負數').default(0),
});

export const inventoryTransferSchema = z.object({
  sourceWarehouseId: z.string().uuid('請選擇來源倉庫'),
  targetWarehouseId: z.string().uuid('請選擇目標倉庫'),
  items: z.array(z.object({
    skuId: z.string().uuid('請選擇物料'),
    transferQty: z.number().positive('調撥數量必須大於 0'),
    lotNumber: z.string().optional()
  })).min(1, '請至少新增一項調撥明細')
}).refine(data => data.sourceWarehouseId !== data.targetWarehouseId, {
  message: "來源倉庫與目標倉庫不能相同",
  path: ["targetWarehouseId"]
});
