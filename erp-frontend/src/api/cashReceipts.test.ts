import { beforeEach, describe, expect, it, vi } from 'vitest';

const clientMocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn()
}));

vi.mock('./client', () => ({
  api: clientMocks
}));

import { cashReceiptsApi } from './sales';

describe('cash receipt allocation API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('creates a partially allocated receipt and preserves the remaining balance as unapplied', async () => {
    const request = {
      receiptDate: '2026-04-12T00:00:00.000Z',
      customerId: 'customer-1',
      paymentMethod: 'wire',
      receivedAmount: 1000,
      discountAmount: 0,
      currency: 'TWD',
      bankAccountId: 'bank-1',
      allocations: [{
        salesInvoiceId: 'invoice-1',
        allocatedAmount: 400,
        discountAmount: 0
      }]
    };
    const response = {
      data: {
        success: true,
        data: { id: 'receipt-1', receivedAmount: 1000, unappliedAmount: 600 }
      }
    };
    clientMocks.post.mockResolvedValueOnce(response);

    await expect(cashReceiptsApi.create(request)).resolves.toBe(response);
    expect(clientMocks.post).toHaveBeenCalledWith('/cash-receipts', request);
  });

  it('applies an unapplied balance to a selected invoice', async () => {
    const request = { salesInvoiceId: 'invoice-1', amount: 250 };
    const response = {
      data: {
        success: true,
        data: { id: 'receipt-1', receivedAmount: 1000, unappliedAmount: 350 }
      }
    };
    clientMocks.post.mockResolvedValueOnce(response);

    await expect(cashReceiptsApi.applyUnapplied('receipt-1', request)).resolves.toBe(response);
    expect(clientMocks.post).toHaveBeenCalledWith(
      '/cash-receipts/receipt-1/apply-unapplied',
      request
    );
  });

  it('loads eligible invoices for a customer', async () => {
    const response = { data: { success: true, data: [] } };
    clientMocks.get.mockResolvedValueOnce(response);

    await expect(cashReceiptsApi.pendingInvoices('customer-1')).resolves.toBe(response);
    expect(clientMocks.get).toHaveBeenCalledWith('/cash-receipts/pending-invoices/customer-1');
  });
});
