import { z } from 'zod';

export const salesOrderSchema = z.object({
  customerId: z.string().uuid('請選擇客戶'),
  orderDate: z.string(),
  expectedDeliveryDate: z.string().optional(),
  items: z.array(z.object({
    skuId: z.string().uuid('請選擇產品'),
    quantity: z.number().positive('數量必須大於 0'),
    unitPrice: z.number().nonnegative('單價不能為負數'),
  })).min(1, '請至少新增一項銷售項目'),
  totalAmount: z.number().nonnegative(),
});

export const customerSchema = z.object({
  customerCode: z.string().min(2, '客戶代碼為必填'),
  customerName: z.string().min(2, '客戶名稱至少 2 個字元'),
  taxId: z.string().optional(),
  email: z.string().email('郵件格式不正確').optional().or(z.literal('')),
  phone: z.string().optional(),
});
