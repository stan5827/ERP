import type { AxiosAdapter, AxiosRequestConfig } from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@react-native-async-storage/async-storage', () => ({
  default: {
    getItem: vi.fn().mockResolvedValue('test-token'),
    removeItem: vi.fn().mockResolvedValue(undefined),
  },
}));

import AsyncStorage from '@react-native-async-storage/async-storage';
import api, { attendanceApi, leaveRequestsApi, payrollApi } from './api';

describe('employee ESS API client', () => {
  const requests: AxiosRequestConfig[] = [];

  const adapter: AxiosAdapter = async (config) => {
    requests.push(config);
    return {
      data: { success: true, data: [] },
      status: 200,
      statusText: 'OK',
      headers: {},
      config,
    };
  };

  beforeEach(() => {
    requests.length = 0;
    vi.mocked(AsyncStorage.getItem).mockResolvedValue('test-token');
    api.defaults.adapter = adapter;
  });

  it('uses personal ESS routes and attaches the stored bearer token', async () => {
    await attendanceApi.list({ pageSize: '30' });
    await payrollApi.list({ pageSize: '12' });
    await leaveRequestsApi.list({ pageSize: '20' });

    expect(requests.map((request) => request.url)).toEqual([
      '/ess/attendance',
      '/ess/payroll',
      '/ess/leave-requests',
    ]);
    expect(requests.every((request) => request.headers?.Authorization === 'Bearer test-token')).toBe(true);
  });

  it('sends location only for an employee clock-in', async () => {
    await attendanceApi.clockIn({ latitude: 25.03, longitude: 121.56 });

    expect(requests[0].url).toBe('/ess/attendance/clock-in');
    expect(JSON.parse(String(requests[0].data))).toEqual({ latitude: 25.03, longitude: 121.56 });
  });
});
