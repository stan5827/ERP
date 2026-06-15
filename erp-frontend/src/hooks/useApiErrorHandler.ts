import { useState, useCallback } from 'react';
import { useToast } from '../hooks/useToast';

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

export function useApiErrorHandler(options: UseApiErrorOptions = {}) {
  const { showToast = true, onError } = options;
  const [error, setError] = useState<ApiError | null>(null);
  const { showError } = useToast();

  const handleApiError = useCallback((err: unknown, defaultMessage = '發生錯誤'): ApiError => {
    let apiError: ApiError;

    if (err instanceof Response || (err && typeof err === 'object' && 'status' in err)) {
      // Already an API error object
      apiError = {
        status: (err as any).status || 500,
        message: (err as any).message || defaultMessage,
        code: (err as any).code,
        details: (err as any).details
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
      showError(apiError.message);
    }

    // Call custom error handler
    if (onError) {
      onError(apiError);
    }

    return apiError;
  }, [showToast, showError, onError]);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  const getErrorMessage = useCallback((err: unknown, fallback = '發生錯誤'): string => {
    if (err && typeof err === 'object') {
      return (err as any).message || (err as any).data?.message || fallback;
    }
    if (typeof err === 'string') {
      return err;
    }
    return fallback;
  }, []);

  const isAuthError = useCallback((err: ApiError | unknown): boolean => {
    const status = (err as ApiError).status || (err as any)?.status;
    return status === 401 || status === 403;
  }, []);

  const isNetworkError = useCallback((err: unknown): boolean => {
    return !err || (err as any).status === 0 || (err as any).status === undefined;
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