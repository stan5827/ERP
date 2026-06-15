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
  try {
    const token = await AsyncStorage.getItem('erp_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  } catch (e) {
    console.log('Token error:', e);
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
    api.get('/attendance', { params }),
  create: (data: Record<string, unknown>) =>
    api.post('/attendance', data),
  update: (id: string, data: Record<string, unknown>) =>
    api.put(`/attendance/${id}`, data),
  approve: (id: string, status: string, notes?: string) =>
    api.patch(`/attendance/${id}/approve`, { status, notes }),
};

export const leaveRequestsApi = {
  list: (params?: Record<string, string>) =>
    api.get('/leave-requests', { params }),
  get: (id: string) =>
    api.get(`/leave-requests/${id}`),
  create: (data: Record<string, unknown>) =>
    api.post('/leave-requests', data),
  update: (id: string, data: Record<string, unknown>) =>
    api.put(`/leave-requests/${id}`, data),
  approve: (id: string, status: string, approverId?: string, rejectReason?: string) =>
    api.patch(`/leave-requests/${id}/approve`, { status, approverId, rejectReason }),
  cancel: (id: string, reason?: string) =>
    api.patch(`/leave-requests/${id}/cancel`, { reason }),
  balances: (params?: Record<string, string>) =>
    api.get('/leave-requests/balances', { params }),
};

export const approvalApi = {
  listPending: () =>
    api.get('/approvals/pending'),
  approve: (id: string, data: Record<string, unknown>) =>
    api.post(`/approvals/${id}/approve`, data),
  reject: (id: string, reason?: string) =>
    api.post(`/approvals/${id}/reject`, { reason }),
};

export const payrollApi = {
  list: (params?: Record<string, string>) =>
    api.get('/payroll', { params }),
  get: (id: string) =>
    api.get(`/payroll/${id}`),
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

export default api;
