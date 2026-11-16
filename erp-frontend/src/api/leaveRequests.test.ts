import { beforeEach, describe, expect, it, vi } from 'vitest';

const clientMocks = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  patch: vi.fn(),
}));

vi.mock('./client', () => ({
  api: clientMocks,
}));

import { hrApi } from './system';

describe('leave request API routes', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    clientMocks.get.mockResolvedValue({});
    clientMocks.post.mockResolvedValue({});
    clientMocks.put.mockResolvedValue({});
    clientMocks.patch.mockResolvedValue({});
  });

  it('uses the backend leave-requests route for all operations', async () => {
    const data = { employeeId: 'employee-1' };
    const filters = { status: 'pending' };
    await hrApi.leaveRequests.list(filters);
    await hrApi.leaveRequests.get('leave-1');
    await hrApi.leaveRequests.create(data);
    await hrApi.leaveRequests.update('leave-1', data);
    await hrApi.leaveRequests.approve('leave-1', 'approved');
    await hrApi.leaveRequests.cancel('leave-1', 'test cancellation');
    await hrApi.leaveRequests.balances({ year: '2026' });

    expect(clientMocks.get).toHaveBeenNthCalledWith(1, '/leave-requests', { params: filters });
    expect(clientMocks.get).toHaveBeenNthCalledWith(2, '/leave-requests/leave-1');
    expect(clientMocks.post).toHaveBeenCalledWith('/leave-requests', data);
    expect(clientMocks.put).toHaveBeenCalledWith('/leave-requests/leave-1', data);
    expect(clientMocks.patch).toHaveBeenNthCalledWith(
      1,
      '/leave-requests/leave-1/approve',
      { status: 'approved', approverId: undefined, rejectReason: undefined },
    );
    expect(clientMocks.patch).toHaveBeenNthCalledWith(
      2,
      '/leave-requests/leave-1/cancel',
      { reason: 'test cancellation' },
    );
    expect(clientMocks.get).toHaveBeenNthCalledWith(
      3,
      '/leave-requests/balances',
      { params: { year: '2026' } },
    );
  });
});
