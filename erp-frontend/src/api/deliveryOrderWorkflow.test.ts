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

import { deliveryOrdersApi } from './sales';

describe('delivery order API workflow', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('creates a delivery order with the selected lot and inventory location', async () => {
    const request = {
      salesOrderId: 'sales-order-id',
      deliveryDate: '2026-10-22T00:00:00.000Z',
      items: [{
        skuId: 'sku-id',
        quantity: 3,
        warehouseId: 'warehouse-id',
        binLocationId: 'bin-location-id',
        lotNumber: 'LOT-001'
      }]
    };
    const response = { data: { success: true, data: { id: 'delivery-order-id' } } };
    clientMocks.post.mockResolvedValueOnce(response);

    await expect(deliveryOrdersApi.create(request)).resolves.toBe(response);

    expect(clientMocks.post).toHaveBeenCalledWith('/delivery-orders', request);
  });
});
