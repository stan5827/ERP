export interface WorkOrderDisplayData {
  woNumber: string;
  productSkuNumber?: string | null;
  productSkuName?: string | null;
  status?: string | null;
}

const workOrderStatusLabels: Record<string, string> = {
  pending: '待發放',
  planned: '待發放',
  released: '已發放',
  inprogress: '生產中',
  completed: '已完工',
  closed: '已結案',
  canceled: '已取消',
  cancelled: '已取消',
};

export function formatWorkOrderStatus(status?: string | null): string {
  const value = status?.trim();
  if (!value)
    return '狀態未提供';

  const normalized = value.toLowerCase().replace(/[\s_-]/g, '');
  return workOrderStatusLabels[normalized] ?? value;
}

export function formatWorkOrderOption(workOrder: WorkOrderDisplayData): string {
  return [
    workOrder.woNumber,
    workOrder.productSkuNumber?.trim() || '料號未提供',
    workOrder.productSkuName?.trim() || '品名未提供',
    formatWorkOrderStatus(workOrder.status),
  ].join(' | ');
}
