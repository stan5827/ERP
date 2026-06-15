import { z } from 'zod';

export const purchaseOrderSchema = z.object({
  supplierId: z.string().uuid('請選擇供應商'),
  orderDate: z.string(),
  expectedDate: z.string().optional(),
  currency: z.string().default('TWD'),
  items: z.array(z.object({
    skuId: z.string().uuid('請選擇物料'),
    quantity: z.number().positive('數量必須大於 0'),
    unitPrice: z.number().nonnegative('單價不能為負數'),
  })).min(1, '請至少新增一項採購項目'),
  totalAmount: z.number().nonnegative(),
});

export const supplierSchema = z.object({
  supplierCode: z.string().min(2, '供應商代碼為必填'),
  supplierName: z.string().min(2, '供應商名稱至少 2 個字元'),
  contactPerson: z.string().optional(),
  email: z.string().email('郵件格式不正確').optional().or(z.literal('')),
  phone: z.string().optional(),
  address: z.string().optional(),
});
