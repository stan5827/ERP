import type { FormField } from '../../components/FormModal';

export const statusConfig: Record<string, { label: string; cls: string }> = {
  'pending': { label: '待處理', cls: 'neutral' },
  'confirmed': { label: '已核准', cls: 'info' },
  'partial': { label: '部分收料', cls: 'warning' },
  'completed': { label: '已結案', cls: 'success' },
  'cancelled': { label: '已取消', cls: 'neutral' }
};

export const ratingConfig: Record<string, { label: string; color: string; bg: string }> = {
  'S': { label: 'S 級 (優)', color: '#10b981', bg: 'rgba(16,185,129,0.1)' },
  'A': { label: 'A 級 (佳)', color: '#38bdf8', bg: 'rgba(56,189,248,0.1)' },
  'B': { label: 'B 級 (中)', color: '#fbbf24', bg: 'rgba(251,191,36,0.1)' },
  'C': { label: 'C 級 (弱)', color: '#f43f5e', bg: 'rgba(244,63,94,0.1)' },
};

export const getPoFields = (suppliersMapped: any[]): FormField[] => [
  { name: 'supplier_code', label: '供應商', type: 'select', required: true, options: suppliersMapped },
  { name: 'poDate', label: '下單日期', type: 'date', required: true, defaultValue: new Date().toISOString().slice(0,10) },
  { name: '_poItems', label: '', type: 'custom' }
];

export const getPrFields = (departmentsMapped: any[]): FormField[] => [
  { name: 'departmentId', label: '請購單位/部門', type: 'select', required: true, options: departmentsMapped },
  { name: 'prDate', label: '請購日期', type: 'date', required: true, defaultValue: new Date().toISOString().slice(0,10) },
  { name: 'purpose', label: '摘要 / 用途', type: 'text', required: true, placeholder: '請輸入請購目的 (如：Q3 生產補貨)' },
  { name: 'priority', label: '緊急程度', type: 'select', options: [{ value: 'normal', label: '一般' }, { value: 'urgent', label: '緊急' }], defaultValue: 'normal' },
  { name: 'budgetAccount', label: '預算科目', type: 'text', placeholder: '生產耗材費' },
  { name: '_prItems', label: '', type: 'custom' }
];

export const supplierFields: FormField[] = [
  { name: 'supplierCode', label: '供應商代號', type: 'text', required: true },
  { name: 'supplierName', label: '供應商名稱', type: 'text', required: true },
  { name: 'taxId', label: '統一編號', type: 'text' },
  { name: 'contactPerson', label: '聯絡人', type: 'text' },
  { name: 'phone', label: '聯絡電話', type: 'text' },
  { name: 'email', label: 'Email', type: 'text' },
  { name: 'address', label: '地址', type: 'text' },
  { name: 'paymentTerms', label: '付款條件', type: 'text', placeholder: '如: 月結30天' },
  { name: 'rating', label: '評等', type: 'select', options: [{ value: 'A', label: 'A級 (優良)' }, { value: 'B', label: 'B級 (合格)' }, { value: 'C', label: 'C級 (待改善)' }, { value: 'D', label: 'D級 (不合格)' }] },
  { name: 'status', label: '狀態', type: 'select', options: [{ value: 'ACTIVE', label: '啟用' }, { value: 'INACTIVE', label: '停用' }], defaultValue: 'ACTIVE' },
];
