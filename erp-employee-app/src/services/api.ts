import AsyncStorage from '@react-native-async-storage/async-storage';
import axios from 'axios';
import { API_BASE_URL } from '../constants/config';

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use(async (config) => {
  const token = await AsyncStorage.getItem('erp_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      AsyncStorage.removeItem('erp_token');
      AsyncStorage.removeItem('erp_user');
    }
    return Promise.reject(error);
  }
);

export const authApi = {
  login: (username: string, password: string) =>
    api.post('/auth/login', { username, password }),
};

export const attendanceApi = {
  list: (params?: Record<string, string>) =>
    api.get('/ess/attendance', { params }),
  clockIn: (location: { latitude: number; longitude: number }) =>
    api.post('/ess/attendance/clock-in', location),
  clockOut: (location: { latitude: number; longitude: number }) =>
    api.post('/ess/attendance/clock-out', location),
};

export const leaveRequestsApi = {
  list: (params?: Record<string, string>) =>
    api.get('/ess/leave-requests', { params }),
  create: (data: { leaveType: string; startDate: string; endDate: string; reason?: string }) =>
    api.post('/ess/leave-requests', data),
  balances: (params?: Record<string, string>) =>
    api.get('/ess/leave-balances', { params }),
};

export const payrollApi = {
  list: (params?: Record<string, string>) =>
    api.get('/ess/payroll', { params }),
};

export const announcementApi = {
  list: (params?: Record<string, string>) =>
    api.get('/collaboration/announcements', { params }),
  markRead: (id: string) =>
    api.patch(`/collaboration/announcements/${id}/read`),
};

export const employeesApi = {
  get: (id: string) =>
    api.get(`/employees/${id}`),
};

export const operationsApi = {
  warehouses: () => api.get('/warehouses'),
  workOrders: () => api.get('/work-orders', { params: { pageSize: 500 } }),
  stocktakeBalance: (skuCode: string, warehouseCode: string, lotNumber: string) =>
    api.get('/inventory/stocktake/balance', { params: { skuCode, warehouseCode, lotNumber } }),
  postStocktake: (data: {
    skuCode: string;
    warehouseCode: string;
    lotNumber: string;
    expiryDate: string;
    countedQty: number;
    expectedSystemQty: number;
    notes: string;
  }) => api.post('/inventory/stocktake', data),
  createFqcInspection: (data: {
    workOrderId: string;
    lotNumber: string;
    expiryDate: string;
    sampledQty: number;
    defectiveQty: number;
    criticalDefectFound: boolean;
    defectCode?: string;
    inspectorName: string;
  }) => api.post('/quality/fqc-inspections', data),
};

export const getApiErrorMessage = (error: unknown, fallback: string): string => {
  if (axios.isAxiosError<{ message?: string }>(error)) {
    return error.response?.data?.message || fallback;
  }

  return error instanceof Error ? error.message : fallback;
};

export default api;