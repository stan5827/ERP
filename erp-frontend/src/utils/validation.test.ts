import { describe, expect, it } from 'vitest';
import { createFieldValidators, validateField } from './validation';

describe('password field validation', () => {
  it('applies the configured minimum length to password fields', () => {
    const validators = createFieldValidators({
      name: 'password',
      type: 'password',
      required: true,
      min: 8
    });

    expect(validateField('abc1234', validators)).toBe('長度至少需要 8 個字元');
    expect(validateField('abc12345', validators)).toBeNull();
    expect(validateField('', validators)).toBe('此欄位為必填');
  });
});
