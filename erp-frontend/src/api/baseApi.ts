import axios, { type AxiosResponse } from 'axios';

// Custom error class for API errors with typed response data
export class ApiError extends Error {
  readonly response?: AxiosResponse<{ success: boolean; message?: string; errors?: Record<string, string[]> }>;
  readonly statusCode?: number;

  constructor(message: string, response?: AxiosResponse, statusCode?: number) {
    super(message);
    this.name = 'ApiError';
    this.response = response;
    this.statusCode = statusCode;
  }
}

// Error handling utility
const handleApiError = (error: unknown): never => {
  if (!axios.isAxiosError(error)) {
    throw new Error(error instanceof Error ? error.message : '發生未知錯誤');
  }

  if (!error.response) {
    throw new Error('網路連線錯誤，請檢查您的網路連接');
  }

  const { status, data } = error.response;

  if (status === 401) {
    localStorage.removeItem('erp_token');
    localStorage.removeItem('erp_user');
    window.location.reload();
    throw new Error('登入已過期，請重新登入');
  }

  if (status === 400) {
    const message = (data as { message?: string })?.message || '輸入資料驗證失敗';
    throw new ApiError(message, error.response, 400);
  }

  if (status === 403) {
    throw new Error('您沒有權限執行此操作');
  }

  if (status === 404) {
    throw new Error('請求的資源不存在');
  }

  if (status >= 500) {
    const serverMsg = (data as { message?: string })?.message;
    console.error('[API] Server Error:', {
      url: error.config?.url,
      method: error.config?.method,
      status,
      data,
    });
    throw new Error(serverMsg || '伺服器內部錯誤，請稍後再試');
  }

  throw new Error((data as { message?: string })?.message || '發生未知錯誤');
};

const API_BASE = 'http://localhost:5100/api/erp';

export const api = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000,
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('erp_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (err) => {
    return Promise.reject(handleApiError(err));
  }
);

export interface ApiResponse<T = unknown> {
  success: boolean;
  message?: string;
  data?: T;
  totalCount?: number;
  page?: number;
  pageSize?: number;
}
