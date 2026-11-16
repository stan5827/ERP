import React, { useState, useMemo, useCallback } from 'react';

export interface ValidationRule {
  type: 'required' | 'minLength' | 'maxLength' | 'min' | 'max' | 'email' | 'regex' | 'custom';
  value?: string | number | RegExp;
  message: string;
}

export interface FieldSchema {
  name: string;
  label: string;
  type: 'text' | 'number' | 'select' | 'date' | 'textarea' | 'checkbox' | 'email' | 'password' | 'tel' | 'url' | 'custom' | 'button' | 'time' | 'month';
  required?: boolean;
  placeholder?: string;
  description?: string;
  options?: { value: string; label: string }[];
  defaultValue?: string | number | boolean;
  min?: number;
  max?: number;
  step?: number;
  pattern?: string;
  minLength?: number;
  maxLength?: number;
  disabled?: boolean;
  readonly?: boolean;
  validation?: ValidationRule[];
  dependsOn?: { field: string; value: unknown };
  conditionalOptions?: { field: string; options: { value: string; label: string }[] }[];
}

export interface FormSchema {
  title?: string;
  description?: string;
  fields: FieldSchema[];
  tabs?: { label: string; fields: string[] }[];
  submitLabel?: string;
  cancelLabel?: string;
  showCancel?: boolean;
  width?: number;
}

interface DynamicFormProps {
  schema: FormSchema;
  initialValues?: Record<string, unknown>;
  onSubmit: (data: Record<string, unknown>) => Promise<void>;
  onCancel?: () => void;
  customRenderers?: Record<string, React.ReactNode>;
  loading?: boolean;
}

export function DynamicFormGenerator({ 
  schema, 
  initialValues = {}, 
  onSubmit, 
  onCancel,
  customRenderers,
  loading = false 
}: DynamicFormProps) {
  const [storedValues, setValues] = useState<Record<string, unknown>>(initialValues);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [touched, setTouched] = useState<Record<string, boolean>>({});

  const defaultValues = useMemo(() => {
    const defaultValues: Record<string, unknown> = {};
    schema.fields.forEach(field => {
      if (field.defaultValue !== undefined && !(field.name in initialValues)) {
        defaultValues[field.name] = field.defaultValue;
      }
    });
    return defaultValues;
  }, [schema.fields, initialValues]);
  const values = useMemo(() => ({ ...defaultValues, ...storedValues }), [defaultValues, storedValues]);

  const validateField = useCallback((field: FieldSchema, value: unknown): string | null => {
    if (field.required && (value === undefined || value === null || value === '')) {
      return `${field.label} 為必填`;
    }

    if (value === undefined || value === null || value === '') {
      return null;
    }

    if (field.type === 'email' && typeof value === 'string') {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(value)) return '請輸入有效的電子郵件';
    }

    if (field.type === 'number' || field.type === 'tel') {
      const numValue = typeof value === 'string' ? parseFloat(value) : value;
      if (typeof numValue === 'number' && !isNaN(numValue)) {
        if (field.min !== undefined && numValue < field.min) return `最小值為 ${field.min}`;
        if (field.max !== undefined && numValue > field.max) return `最大值為 ${field.max}`;
      }
    }

    if (typeof value === 'string') {
      if (field.minLength !== undefined && value.length < field.minLength) {
        return `最少 ${field.minLength} 個字元`;
      }
      if (field.maxLength !== undefined && value.length > field.maxLength) {
        return `最多 ${field.maxLength} 個字元`;
      }
      if (field.pattern) {
        const regex = new RegExp(field.pattern);
        if (!regex.test(value)) return `格式不正確`;
      }
    }

    return null;
  }, []);

  const validateForm = useCallback((): boolean => {
    const newErrors: Record<string, string> = {};
    let isValid = true;

    schema.fields.forEach(field => {
      if (field.dependsOn) {
        const dependsValue = values[field.dependsOn.field];
        if (dependsValue !== field.dependsOn.value) return;
      }

      const error = validateField(field, values[field.name]);
      if (error) {
        newErrors[field.name] = error;
        isValid = false;
      }
    });

    setErrors(newErrors);
    return isValid;
  }, [schema.fields, values, validateField]);

  const handleChange = useCallback((name: string, value: unknown) => {
    setValues(prev => ({ ...prev, [name]: value }));
    if (touched[name]) {
      const field = schema.fields.find(f => f.name === name);
      if (field) {
        const error = validateField(field, value);
        setErrors(prev => ({ ...prev, [name]: error || '' }));
      }
    }
  }, [schema.fields, touched, validateField]);

  const handleBlur = useCallback((name: string) => {
    setTouched(prev => ({ ...prev, [name]: true }));
    const field = schema.fields.find(f => f.name === name);
    if (field) {
      const error = validateField(field, values[field.name]);
      setErrors(prev => ({ ...prev, [name]: error || '' }));
    }
  }, [schema.fields, values, validateField]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const allTouched: Record<string, boolean> = {};
    schema.fields.forEach(field => {
      allTouched[field.name] = true;
    });
    setTouched(allTouched);

    if (validateForm()) {
      await onSubmit(values);
    }
  };

  const renderField = (field: FieldSchema) => {
    const value = values[field.name];
    const error = errors[field.name];
    const isDisabled = field.disabled || loading;

    if (field.type === 'custom' && customRenderers?.[field.name]) {
      return <div key={field.name}>{customRenderers[field.name]}</div>;
    }

    const baseInputClass = `form-input ${error ? 'error' : ''}`;

    switch (field.type) {
      case 'select':
        return (
          <select
            key={field.name}
            className={baseInputClass}
            value={String(value ?? '')}
            onChange={e => handleChange(field.name, e.target.value)}
            onBlur={() => handleBlur(field.name)}
            disabled={isDisabled}
          >
            <option value="">{field.placeholder || '請選擇'}</option>
            {(field.options || []).map(opt => (
              <option key={opt.value} value={opt.value}>{opt.label}</option>
            ))}
          </select>
        );

      case 'textarea':
        return (
          <textarea
            key={field.name}
            className={baseInputClass}
            value={String(value ?? '')}
            onChange={e => handleChange(field.name, e.target.value)}
            onBlur={() => handleBlur(field.name)}
            placeholder={field.placeholder}
            disabled={isDisabled}
            rows={4}
          />
        );

      case 'checkbox':
        return (
          <label key={field.name} className="checkbox-label">
            <input
              type="checkbox"
              checked={!!value}
              onChange={e => handleChange(field.name, e.target.checked)}
              disabled={isDisabled}
            />
            <span>{field.description || '啟用'}</span>
          </label>
        );

      case 'number':
        return (
          <input
            key={field.name}
            type="number"
            className={baseInputClass}
            value={value !== undefined && value !== null ? String(value) : ''}
            onChange={e => handleChange(field.name, e.target.value)}
            onBlur={() => handleBlur(field.name)}
            placeholder={field.placeholder}
            disabled={isDisabled}
            min={field.min}
            max={field.max}
            step={field.step || 1}
          />
        );

      case 'date':
        return (
          <input
            key={field.name}
            type="date"
            className={baseInputClass}
            value={value ? String(value).split('T')[0] : ''}
            onChange={e => handleChange(field.name, e.target.value)}
            onBlur={() => handleBlur(field.name)}
            disabled={isDisabled}
          />
        );

      case 'password':
      case 'email':
      case 'tel':
      case 'url':
      case 'text':
      default:
        return (
          <input
            key={field.name}
            type={field.type}
            className={baseInputClass}
            value={String(value ?? '')}
            onChange={e => handleChange(field.name, e.target.value)}
            onBlur={() => handleBlur(field.name)}
            placeholder={field.placeholder}
            disabled={isDisabled}
            readOnly={field.readonly}
          />
        );
    }
  };

  const renderFieldGroup = (field: FieldSchema) => {
    const fieldError = errors[field.name];
    return (
      <div key={field.name} className="form-field">
        {field.type !== 'checkbox' && (
          <label className="form-label">
            {field.label}
            {field.required && <span className="required-mark">*</span>}
          </label>
        )}
        {renderField(field)}
        {field.description && !fieldError && (
          <span className="field-description">{field.description}</span>
        )}
        {fieldError && <span className="field-error">{fieldError}</span>}
      </div>
    );
  };

  const visibleFields = schema.fields.filter(field => {
    if (!field.dependsOn) return true;
    return values[field.dependsOn.field] === field.dependsOn.value;
  });

  return (
    <form onSubmit={handleSubmit} className="dynamic-form" style={{ width: schema.width || '100%' }}>
      {schema.title && <h3 className="form-title">{schema.title}</h3>}
      {schema.description && <p className="form-description">{schema.description}</p>}
      
      <div className="form-fields">
        {schema.tabs ? (
          <div className="form-tabs">
            {schema.tabs.map((tab, idx) => (
              <div key={idx} className="tab-panel">
                <h4>{tab.label}</h4>
                {tab.fields.map(fieldName => {
                  const field = schema.fields.find(f => f.name === fieldName);
                  return field ? renderFieldGroup(field) : null;
                })}
              </div>
            ))}
          </div>
        ) : (
          visibleFields.map(renderFieldGroup)
        )}
      </div>

      <div className="form-actions">
        {schema.showCancel !== false && onCancel && (
          <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
            {schema.cancelLabel || '取消'}
          </button>
        )}
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? '處理中...' : (schema.submitLabel || '儲存')}
        </button>
      </div>

      <style>{`
        .dynamic-form { max-width: 800px; margin: 0 auto; }
        .form-title { margin-bottom: 16px; font-size: 18px; font-weight: 600; }
        .form-description { margin-bottom: 24px; color: var(--slate-400); }
        .form-fields { display: flex; flex-direction: column; gap: 16px; }
        .form-field { display: flex; flex-direction: column; gap: 6px; }
        .form-label { font-size: 13px; font-weight: 600; color: var(--slate-300); }
        .required-mark { color: #ef4444; margin-left: 4px; }
        .form-input { 
          padding: 8px 12px; border: 1px solid var(--slate-600); 
          border-radius: 4px; background: var(--slate-800); color: white;
          font-size: 14px;
        }
        .form-input:focus { outline: none; border-color: var(--cobalt-400); }
        .form-input.error { border-color: #ef4444; }
        .form-input:disabled { opacity: 0.6; cursor: not-allowed; }
        .field-description { font-size: 12px; color: var(--slate-500); }
        .field-error { font-size: 12px; color: #ef4444; }
        .checkbox-label { display: flex; align-items: center; gap: 8px; cursor: pointer; }
        .form-actions { display: flex; gap: 12px; justify-content: flex-end; margin-top: 24px; }
        .tab-panel { margin-bottom: 16px; }
        .tab-panel h4 { margin-bottom: 12px; font-size: 14px; color: var(--slate-400); }
      `}</style>
    </form>
  );
}

export default DynamicFormGenerator;