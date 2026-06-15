import { useState, useCallback } from 'react';
import { validateField, validateForm, ValidationRule } from '../utils/validation';

export interface UseFormValidationOptions<T extends Record<string, unknown>> {
  initialValues: T;
  validationRules?: Record<string, ValidationRule[]>;
  onSubmit?: (values: T) => Promise<void>;
}

export interface UseFormValidationReturn<T extends Record<string, unknown>> {
  values: T;
  errors: Record<string, string>;
  touched: Record<string, boolean>;
  isSubmitting: boolean;
  isValid: boolean;
  setFieldValue: (field: string, value: unknown) => void;
  setFieldTouched: (field: string) => void;
  validateField: (field: string) => boolean;
  handleSubmit: () => Promise<boolean>;
  resetForm: () => void;
}

export function useFormValidation<T extends Record<string, unknown>>({
  initialValues,
  validationRules = {},
  onSubmit
}: UseFormValidationOptions<T>): UseFormValidationReturn<T> {
  const [values, setValues] = useState<T>(initialValues);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [touched, setTouched] = useState<Record<string, boolean>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const setFieldValue = useCallback((field: string, value: unknown) => {
    setValues(prev => ({ ...prev, [field]: value }));
    
    // Real-time validation if field has been touched
    if (touched[field] && validationRules[field]) {
      const error = validateField(value, validationRules[field]);
      setErrors(prev => ({ ...prev, [field]: error || '' }));
    }
  }, [touched, validationRules]);

  const setFieldTouched = useCallback((field: string) => {
    setTouched(prev => ({ ...prev, [field]: true }));
    
    // Validate on blur
    if (validationRules[field]) {
      const error = validateField(values[field], validationRules[field]);
      setErrors(prev => ({ ...prev, [field]: error || '' }));
    }
  }, [values, validationRules]);

  const validateSingleField = useCallback((field: string): boolean => {
    if (!validationRules[field]) return true;
    
    const error = validateField(values[field], validationRules[field]);
    setErrors(prev => ({ ...prev, [field]: error || '' }));
    return !error;
  }, [values, validationRules]);

  const validateAllFields = useCallback((): boolean => {
    if (!validationRules) return true;
    
    const formErrors = validateForm(values, validationRules);
    setErrors(formErrors);
    return Object.keys(formErrors).length === 0;
  }, [values, validationRules]);

  const handleSubmit = useCallback(async (): Promise<boolean> => {
    // Validate all fields
    const isValid = validateAllFields();
    if (!isValid) return false;

    if (onSubmit) {
      setIsSubmitting(true);
      try {
        await onSubmit(values);
        return true;
      } catch (error) {
        console.error('Form submission error:', error);
        return false;
      } finally {
        setIsSubmitting(false);
      }
    }
    return true;
  }, [validateAllFields, onSubmit, values]);

  const resetForm = useCallback(() => {
    setValues(initialValues);
    setErrors({});
    setTouched({});
  }, [initialValues]);

  const isValid = Object.keys(errors).length === 0;

  return {
    values,
    errors,
    touched,
    isSubmitting,
    isValid,
    setFieldValue,
    setFieldTouched,
    validateField: validateSingleField,
    handleSubmit,
    resetForm
  };
}