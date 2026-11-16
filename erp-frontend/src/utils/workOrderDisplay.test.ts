import { describe, expect, it } from 'vitest';
import { formatWorkOrderOption, formatWorkOrderStatus } from './workOrderDisplay';

describe('formatWorkOrderOption', () => {
  it('shows complete work-order, product, and status information', () => {
    expect(formatWorkOrderOption({
      woNumber: 'WO-001',
      productSkuNumber: 'SKU-001',
      productSkuName: '成品名稱',
      status: 'in_progress',
    })).toBe('WO-001 | SKU-001 | 成品名稱 | 生產中');
  });

  it('labels absent product data instead of leaving an unexplained blank', () => {
    expect(formatWorkOrderOption({
      woNumber: 'WO-002',
      status: 'Released',
    })).toBe('WO-002 | 料號未提供 | 品名未提供 | 已發放');
  });

  it('translates PascalCase work-order statuses and labels missing statuses', () => {
    expect(formatWorkOrderStatus('InProgress')).toBe('生產中');
    expect(formatWorkOrderStatus(null)).toBe('狀態未提供');
  });
});
