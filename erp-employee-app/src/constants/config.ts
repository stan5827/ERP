export const API_BASE_URL = process.env.EXPO_PUBLIC_API_URL || 'http://10.0.2.2:5100/api';
export const DEFAULT_TIMEOUT = 15000;

export const COMPANY_LOCATIONS = [
  {
    id: 'main',
    name: '總公司',
    latitude: 25.0330,
    longitude: 121.5654,
    radius: 200,
  },
];

export const APP_CONFIG = {
  TOKEN_KEY: 'erp_token',
  USER_KEY: 'erp_user',
  OFFLINE_KEY: 'erp_offline',
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
