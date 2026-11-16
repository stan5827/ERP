import { createContext, useContext } from 'react';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  title: string;
  message?: string;
  duration?: number;
}

export interface ToastContextType {
  toasts: Toast[];
  addToast: (toast: Omit<Toast, 'id'>) => void;
  removeToast: (id: string) => void;
  clearAll: () => void;
}

export const ToastContext = createContext<ToastContextType | undefined>(undefined);

export const useToast = () => {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error('useToast must be used within a ToastProvider');
  }
  return context;
};

let toastFn: ((toast: Omit<Toast, 'id'>) => void) | null = null;

export const setToastFunction = (fn: (toast: Omit<Toast, 'id'>) => void) => {
  toastFn = fn;
};

export const toast = {
  success: (title: string, message?: string) => toastFn?.({ type: 'success', title, message }),
  error: (title: string, message?: string) => toastFn?.({ type: 'error', title, message }),
  warning: (title: string, message?: string) => toastFn?.({ type: 'warning', title, message }),
  info: (title: string, message?: string) => toastFn?.({ type: 'info', title, message }),
};
