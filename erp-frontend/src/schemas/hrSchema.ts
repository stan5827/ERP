import { z } from 'zod';

export const employeeSchema = z.object({
  employeeId: z.string().min(2, '員編為必填'),
  fullName: z.string().min(2, '姓名至少需 2 個字元'),
  email: z.string().email('請輸入有效電子郵件'),
  departmentId: z.string().uuid('請選擇部門'),
  jobTitleId: z.string().uuid('請選擇職稱'),
  salary: z.number().positive('薪資必須大於 0'),
  hireDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/, '日期格式不正確'),
});

export const leaveRequestSchema = z.object({
  employeeId: z.string().uuid('請選擇員工'),
  leaveType: z.enum(['annual', 'sick', 'personal', 'other']),
  startDate: z.string(),
  endDate: z.string(),
  reason: z.string().min(5, '請輸入請假理由（至少 5 字）'),
}).refine(data => new Date(data.endDate) >= new Date(data.startDate), {
  message: "結束日期不能早於開始日期",
  path: ["endDate"]
});
