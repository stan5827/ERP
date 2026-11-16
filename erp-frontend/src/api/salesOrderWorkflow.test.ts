// Runtime validation coverage for the sales-order-to-work-order API flow.
import { beforeEach, describe, expect, it, vi } from 'vitest';

const clientMocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  patch: vi.fn(),
  delete: vi.fn()
}));

vi.mock('./client', () => ({
  api: clientMocks,
  toPascalCase: (value: unknown) => value
}));

import { salesOrdersApi } from './sales';
import { workOrdersApi } from './mes';

describe('sales order production workflow API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns credit and inventory warnings when confirming a sales order', async () => {
    const confirmation = {
      orderNumber: 'SO-TEST-001',
      creditLimit: 100,
      creditUsageBeforeOrder: 0,
      creditUsageAfterOrder: 525,
      creditLimitExceeded: true,
      inventoryWarnings: [{
        lineNumber: 1,
        skuNumber: 'FG-001',
        skuName: '測試成品',
        requiredQty: 5,
        availableQty: 3,
        shortageQty: 2
      }]
    };
    const response = { data: { success: true, data: confirmation } };
    clientMocks.post.mockResolvedValueOnce(response);

    await expect(salesOrdersApi.confirm('SO-TEST-001')).resolves.toBe(response);
    expect(clientMocks.post).toHaveBeenCalledWith('/sales-orders/SO-TEST-001/confirm');
  });

  it('returns BOM shortage details when converting an order to work orders', async () => {
    const response = {
      data: {
        success: true,
        data: [{
          woNumber: 'WO-TEST-001',
          materials: [{
            skuNumber: 'RM-001',
            requiredQty: 10,
            availableQty: 3,
            shortageQty: 7,
            status: 'shortage'
          }]
        }]
      }
    };
    clientMocks.post.mockResolvedValueOnce(response);

    const result = await workOrdersApi.createFromSalesOrder('order-id');

    expect(clientMocks.post).toHaveBeenCalledWith('/work-orders/from-sales-order/order-id');
    expect(result.data.data?.[0].materials?.[0].shortageQty).toBe(7);
  });
});
