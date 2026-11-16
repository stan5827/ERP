import { FormField } from '../components/FormModal';

// Validation rules interface
export interface ValidationRule {
  validate: (value: unknown) => boolean;
  message: string;
}

// Common validation rules
export const validationRules = {
  required: (message = '此欄位為必填'): ValidationRule => ({
    validate: (value) => {
      if (value === undefined || value === null) return false;
      if (typeof value === 'string') return value.trim().length > 0;
      if (typeof value === 'number') return !isNaN(value);
      if (Array.isArray(value)) return value.length > 0;
      return !!value;
    },
    message
  }),

  minLength: (min: number, message?: string): ValidationRule => ({
    validate: (value) => {
      if (typeof value !== 'string') return false;
      return value.length >= min;
    },
    message: message || `長度至少需要 ${min} 個字元`
  }),

  maxLength: (max: number, message?: string): ValidationRule => ({
    validate: (value) => {
      if (typeof value !== 'string') return false;
      return value.length <= max;
    },
    message: message || `長度不能超過 ${max} 個字元`
  }),

  min: (min: number, message?: string): ValidationRule => ({
    validate: (value) => {
      if (value === undefined || value === null || value === '') return true;
      if (typeof value !== 'number') return true;
      return value >= min;
    },
    message: message || `值不能小於 ${min}`
  }),

  max: (max: number, message?: string): ValidationRule => ({
    validate: (value) => {
      if (value === undefined || value === null || value === '') return true;
      if (typeof value !== 'number') return true;
      return value <= max;
    },
    message: message || `值不能大於 ${max}`
  }),

  email: (message = '請輸入有效的電子郵件地址'): ValidationRule => ({
    validate: (value) => {
      if (typeof value !== 'string') return false;
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      return emailRegex.test(value);
    },
    message
  }),

  pattern: (regex: RegExp, message: string): ValidationRule => ({
    validate: (value) => {
      if (typeof value !== 'string') return false;
      return regex.test(value);
    },
    message
  })
};

// Validate a single field value against rules
export function validateField(
  value: unknown,
  rules: ValidationRule[]
): string | null {
  for (const rule of rules) {
    if (rule && typeof rule.validate === 'function') {
      if (!rule.validate(value)) {
        return rule.message;
      }
    }
  }
  return null;
}

// Validate all fields in a form
export function validateForm(
  values: Record<string, unknown>,
  fieldValidators: Record<string, ValidationRule[]>
): Record<string, string> {
  const errors: Record<string, string> = {};

  for (const [fieldName, validators] of Object.entries(fieldValidators)) {
    const error = validateField(values[fieldName], validators);
    if (error) {
      errors[fieldName] = error;
    }
  }

  return errors;
}

// Helper to create field validation rules from FormField definition
export function createFieldValidators(field: FormField): ValidationRule[] {
  const rules: ValidationRule[] = [];

  if (field.required) {
    rules.push(validationRules.required());
  }

  if (field.type === 'text' || field.type === 'password' || field.type === 'textarea') {
    if (field.min !== undefined) {
      rules.push(validationRules.minLength(field.min));
    }
    if (field.max !== undefined) {
      rules.push(validationRules.maxLength(field.max));
    }
  }

  if (field.type === 'number') {
    if (field.min !== undefined) {
      rules.push(validationRules.min(field.min));
    }
    if (field.max !== undefined) {
      rules.push(validationRules.max(field.max));
    }
  }

  // Add custom validation if needed
  // This could be extended to support custom validation functions

  return rules;
}