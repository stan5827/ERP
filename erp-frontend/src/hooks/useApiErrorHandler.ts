import { useState, useCallback } from 'react';
import { useToast } from './toastContext';

export interface ApiError {
  status: number;
  message: string;
  code?: string;
  details?: Record<string, unknown>;
}

export interface UseApiErrorOptions {
  showToast?: boolean;
  onError?: (error: ApiError) => void;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function getErrorStatus(value: unknown): number | undefined {
  if (!isRecord(value)) return undefined;
  return typeof value.status === 'number' ? value.status : undefined;
}

export function useApiErrorHandler(options: UseApiErrorOptions = {}) {
  const { showToast = true, onError } = options;
  const [error, setError] = useState<ApiError | null>(null);
  const { addToast } = useToast();

  const handleApiError = useCallback((err: unknown, defaultMessage = '發生錯誤'): ApiError => {
    let apiError: ApiError;

    if (err instanceof Response || (isRecord(err) && 'status' in err)) {
      // Already an API error object
      const errorRecord = isRecord(err) ? err : {};
      const details = isRecord(errorRecord.details) ? errorRecord.details : undefined;
      apiError = {
        status: typeof errorRecord.status === 'number' && errorRecord.status
          ? errorRecord.status
          : 500,
        message: typeof errorRecord.message === 'string' && errorRecord.message
          ? errorRecord.message
          : defaultMessage,
        code: typeof errorRecord.code === 'string' ? errorRecord.code : undefined,
        details
      };
    } else if (err instanceof Error) {
      // JavaScript error
      apiError = {
        status: 500,
        message: err.message,
        code: 'UNKNOWN_ERROR'
      };
    } else if (typeof err === 'string') {
      apiError = {
        status: 500,
        message: err,
        code: 'UNKNOWN_ERROR'
      };
    } else {
      apiError = {
        status: 500,
        message: defaultMessage,
        code: 'UNKNOWN_ERROR'
      };
    }

    // Set error state
    setError(apiError);

    // Show toast notification if enabled
    if (showToast) {
      addToast({ type: 'error', title: '錯誤', message: apiError.message });
    }

    // Call custom error handler
    if (onError) {
      onError(apiError);
    }

    return apiError;
  }, [showToast, addToast, onError]);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  const getErrorMessage = useCallback((err: unknown, fallback = '發生錯誤'): string => {
    if (isRecord(err)) {
      if (typeof err.message === 'string' && err.message) return err.message;
      const data = isRecord(err.data) ? err.data : undefined;
      if (typeof data?.message === 'string' && data.message) return data.message;
      return fallback;
    }
    if (typeof err === 'string') {
      return err;
    }
    return fallback;
  }, []);

  const isAuthError = useCallback((err: ApiError | unknown): boolean => {
    const status = getErrorStatus(err);
    return status === 401 || status === 403;
  }, []);

  const isNetworkError = useCallback((err: unknown): boolean => {
    const status = getErrorStatus(err);
    return !err || status === 0 || status === undefined;
  }, []);

  return {
    error,
    handleApiError,
    clearError,
    getErrorMessage,
    isAuthError,
    isNetworkError
  };
}