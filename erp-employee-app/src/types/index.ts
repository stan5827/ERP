export interface User {
  userId: string;
  username: string;
  email?: string;
  token: string;
  expiresAt: string;
  roles: string[];
  permissions: string[];
}

export interface Employee {
  id: string;
  employeeNo: string;
  chineseName: string;
  englishName?: string;
  departmentId?: string;
  departmentName?: string;
  jobTitleId?: string;
  jobTitleName?: string;
  mobile?: string;
  email?: string;
  hireDate?: string;
  status: string;
}

export interface AttendanceRecord {
  id: string;
  employeeId: string;
  employeeNo: string;
  employeeName: string;
  date: string;
  clockIn?: string;
  clockOut?: string;
  shiftType?: string;
  status: string;
  approvalStatus: string;
  lateMinutes?: number;
  earlyLeaveMinutes?: number;
  ot1Hours?: number;
  ot2Hours?: number;
  ot3Hours?: number;
  notes?: string;
}

export interface LeaveRequest {
  id: string;
  applicationNo: string;
  employeeId: string;
  employeeNo: string;
  employeeName: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  totalHours: number;
  reason?: string;
  status: string;
  approverId?: string;
  approverName?: string;
  approvedAt?: string;
}

export interface LeaveBalance {
  leaveType: string;
  year: number;
  totalDays: number;
  usedDays: number;
  remainingDays: number;
}

export interface PayrollRecord {
  id: string;
  employeeId: string;
  employeeNo: string;
  employeeName: string;
  periodStart: string;
  periodEnd: string;
  basicSalary: number;
  foodAllowance: number;
  positionAllowance: number;
  overtimePay: number;
  bonus: number;
  attendanceBonus: number;
  grossPay: number;
  laborInsuranceDeduction: number;
  healthInsuranceDeduction: number;
  pensionDeduction: number;
  withholdingTax: number;
  totalDeductions: number;
  netPay: number;
  status: string;
  paymentDate?: string;
}

export interface Announcement {
  id: string;
  title: string;
  category: string;
  content?: string;
  isPinned: boolean;
  isUrgent: boolean;
  targetAudience: string;
  publishDate: string;
  createdAt: string;
  read?: boolean;
}

export type LeaveType = 'annual' | 'personal' | 'sick' | 'maternity' | 'marriage' | 'funeral' | 'official';

export const LEAVE_TYPES: { value: LeaveType; label: string; days: number }[] = [
  { value: 'annual', label: '特休/年假', days: 15 },
  { value: 'personal', label: '事假', days: 7 },
  { value: 'sick', label: '病假', days: 30 },
  { value: 'maternity', label: '產假', days: 60 },
  { value: 'marriage', label: '婚假', days: 8 },
  { value: 'funeral', label: '喪假', days: 10 },
  { value: 'official', label: '公假', days: 0 },
];

export const ATTENDANCE_STATUS = {
  NORMAL: 'normal',
  LATE: 'late',
  EARLY_LEAVE: 'early_leave',
  ABSENT: 'absent',
  PENDING: 'pending',
};

export const LEAVE_STATUS = {
  PENDING: 'pending',
  APPROVED: 'approved',
  REJECTED: 'rejected',
  CANCELLED: 'cancelled',
};

export const PAYROLL_STATUS = {
  DRAFT: 'draft',
  APPROVED: 'approved',
  PAID: 'paid',
  VOID: 'void',
};