const apiHost = typeof window === 'undefined' ? '10.0.2.2' : window.location.hostname;
export const API_BASE_URL = process.env.EXPO_PUBLIC_API_BASE_URL?.trim()
  || `http://${apiHost}:5100/api/erp`;

export const APP_CONFIG = {
  TOKEN_KEY: 'erp_token',
  USER_KEY: 'erp_user',
  TOKEN_EXPIRY_DAYS: 30,
};

export const COLORS = {
  primary: '#2563EB',
  success: '#10B981',
  warning: '#F59E0B',
  danger: '#EF4444',
  gray: '#6B7280',
  lightGray: '#F3F4F6',
  dark: '#1F2937',
  white: '#FFFFFF',
  background: '#F9FAFB',
};