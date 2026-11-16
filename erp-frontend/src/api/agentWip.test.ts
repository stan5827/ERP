import { beforeEach, describe, expect, it, vi } from 'vitest';

const clientMocks = vi.hoisted(() => ({
  get: vi.fn(),
}));

vi.mock('./client', () => ({
  api: clientMocks,
}));

import { agentWipApi } from './agentWip';

describe('agent WIP API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('requests one work order with a per-turn session identifier', async () => {
    const response = { data: { success: true, data: { workOrderNumber: { value: 'WO-TEST-01' } } } };
    clientMocks.get.mockResolvedValueOnce(response);

    await expect(agentWipApi.getWorkOrder('WO/TEST 01', 'session-id')).resolves.toBe(response);

    expect(clientMocks.get).toHaveBeenCalledWith(
      '/agent-wip/work-orders/WO%2FTEST%2001',
      { headers: { 'X-Agent-Session-Id': 'session-id' } },
    );
  });
});
