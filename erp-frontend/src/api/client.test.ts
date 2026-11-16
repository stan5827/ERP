import { describe, expect, it } from 'vitest';
import { handleApiError } from './client';

describe('handleApiError', () => {
  it('preserves the network error message for login failures without a response', () => {
    expect(() => handleApiError({}))
      .toThrow('網路連線錯誤，請檢查您的網路連接');
  });

  it('uses a login-specific message for unauthorized credentials', () => {
    expect(() => handleApiError({
      config: { url: '/auth/login' },
      response: { status: 401, data: { message: 'Invalid credentials' } }
    })).toThrow('帳號或密碼錯誤，或帳號目前無法登入');
  });
});
