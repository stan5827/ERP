import axios from 'axios';

export const API_BASE = import.meta.env.VITE_API_BASE_URL || '/api/erp';
let sessionValidation: Promise<boolean> | null = null;

const isCurrentSessionUnauthorized = (token: string): Promise<boolean> => {
  if (!sessionValidation) {
    sessionValidation = axios.get(`${API_BASE}/system-settings/permissions/me`, {
      headers: { Authorization: `Bearer ${token}` },
      timeout: 10000
    })
      .then(() => false)
      .catch(error => {
        if (axios.isAxiosError(error) && error.response?.status === 401)
          return true;

        console.error('[API] Could not validate the current login session after a 401 response.', error);
        return false;
      })
      .finally(() => {
        sessionValidation = null;
      });
  }

  return sessionValidation;
};

interface ApiErrorPayload {
  message?: string;
}

interface ApiErrorResponse {
  status: number;
  data?: ApiErrorPayload;
}

interface ApiErrorLike {
  config?: {
    url?: string;
    method?: string;
  };
  response: ApiErrorResponse;
}

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null;

const isApiErrorLike = (error: unknown): error is ApiErrorLike => {
  if (!isRecord(error) || !isRecord(error.response)
      || typeof error.response.status !== 'number') {
    return false;
  }

  const { config, response } = error;
  if (config !== undefined
      && (!isRecord(config)
        || (config.url !== undefined && typeof config.url !== 'string')
        || (config.method !== undefined && typeof config.method !== 'string'))) {
    return false;
  }

  return response.data === undefined
    || (isRecord(response.data)
      && (response.data.message === undefined
        || typeof response.data.message === 'string'));
};

// Error handling utility
export const handleApiError = (error: unknown) => {
  const apiError = isApiErrorLike(error) ? error : undefined;
  if (!apiError?.response) {
    throw new Error('網路連線錯誤，請檢查您的網路連接');
  }
  const { response } = apiError;
  if (response.status === 401) {
    if (String(apiError.config?.url ?? '').endsWith('/auth/login')) {
      throw new Error('帳號或密碼錯誤，或帳號目前無法登入');
    }
    throw new Error(response.data?.message || '登入工作階段無效或已過期，請重新登入');
  }
  if (response.status === 400) {
    const message = response.data?.message || '輸入資料驗證失敗';
    const err = Object.assign(new Error(message), {
      response,
    });
    throw err;
  }
  if (response.status === 403) {
    throw new Error('您沒有權限執行此操作');
  }
  if (response.status === 404) {
    throw new Error('請求的資源不存在');
  }
  if (response.status >= 500) {
    const serverMsg = response.data?.message;
    console.error('[API] Server Error 500:', {
      url: apiError.config?.url,
      method: apiError.config?.method,
      data: response.data
    });
    throw new Error(serverMsg || '伺服器內部錯誤，請稍後再試');
  }
  throw new Error(response.data?.message || '發生未知錯誤');
};

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
  async (err: unknown) => {
    const apiError = axios.isAxiosError<ApiErrorPayload>(err) ? err : undefined;
    const requestUrl = String(apiError?.config?.url ?? '');
    if (apiError?.response?.status === 401 && !requestUrl.endsWith('/auth/login')) {
      const token = localStorage.getItem('erp_token');
      const isSessionValidationRequest = requestUrl.endsWith('/system-settings/permissions/me');
      const sessionIsInvalid = token && (isSessionValidationRequest
        || await isCurrentSessionUnauthorized(token));
      if (sessionIsInvalid && localStorage.getItem('erp_token') === token) {
        localStorage.removeItem('erp_token');
        localStorage.removeItem('erp_user');
        window.location.reload();
      }
    }

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

export function toPascalCase(obj: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  for (const key in obj) {
    if (!Object.prototype.hasOwnProperty.call(obj, key)) continue;
    const pascal = key.charAt(0).toUpperCase() + key.slice(1);
    const value = obj[key];
    if (value !== null && value !== undefined && typeof value === 'object' && !Array.isArray(value)) {
      result[pascal] = toPascalCase(value as Record<string, unknown>);
    } else if (Array.isArray(value)) {
      result[pascal] = value.map(item =>
        (item && typeof item === 'object') ? toPascalCase(item as Record<string, unknown>) : item
      );
    } else {
      result[pascal] = value;
    }
  }
  return result;
}
