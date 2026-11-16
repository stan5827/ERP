import React, { useState, useEffect } from 'react';
import { X, Save, Loader2, ChevronLeft, ChevronRight, Check, Search } from 'lucide-react';
import { validateField, createFieldValidators } from '../utils/validation';

export interface FormField {
  name: string;
  label?: string;
  type: 'text' | 'password' | 'number' | 'select' | 'multi-select' | 'date' | 'textarea' | 'checkbox' | 'custom' | 'button' | 'time' | 'month' | 'hidden';
  required?: boolean;
  placeholder?: string;
  description?: string;
  options?: { value: string; label: string }[];
  defaultValue?: string | number | boolean;
  value?: string | number | boolean;
  min?: number;
  max?: number;
  step?: number;
  onBlur?: (name: string, value: string) => void;
  sameAs?: string;
  searchable?: boolean;
  visibleWhen?: { field: string; equals: string };
  visibleWhenChanged?: string[];
  requiredWhenChanged?: string[];
}

export interface FormTab {
  label: string;
  fields: FormField[];
}

interface FormModalProps {
  open: boolean;
  title: string;
  tabs?: FormTab[];
  fields?: FormField[];
  initialValues?: Record<string, unknown>;
  onClose: () => void;
  onSubmit?: (data: Record<string, unknown>) => Promise<void>;
  width?: number;
  customRenderers?: Record<string, React.ReactNode>;
  onChangeData?: (
    values: Record<string, unknown>, 
    setErrors: React.Dispatch<React.SetStateAction<Record<string, string>>>, 
    setFieldOptions?: (fieldName: string, options: { value: string; label: string }[]) => void,
    setFieldValues?: (updates: Record<string, unknown>) => void
  ) => void;
  onFieldDefaultChange?: (setFieldDefaults: (fieldName: string, defaultValue: unknown) => void) => void;
  children?: React.ReactNode;
  hiddenFields?: string[];
  isWizard?: boolean;
  wizardConfig?: {
    stepTitles: string[];
    getStepFields: (stepIndex: number, values: Record<string, unknown>) => FormField[];
    getStepHiddenFields?: (stepIndex: number, values: Record<string, unknown>) => string[];
  };
}

const SearchableSelect: React.FC<{
  options: { value: string; label: string }[];
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}> = ({ options, value, onChange, placeholder }) => {
  const [isOpen, setIsOpen] = useState(false);
  const [search, setSearch] = useState('');
  
  const selectedOption = options.find(o => o.value === value);
  const filteredOptions = options.filter(o => 
    o.label.toLowerCase().includes(search.toLowerCase()) || 
    (o.value && String(o.value).toLowerCase().includes(search.toLowerCase()))
  );

  return (
    <div style={{ position: 'relative' }}>
      <div 
        className="form-input" 
        style={{ cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}
        onClick={() => setIsOpen(!isOpen)}
      >
        <span style={{ color: selectedOption ? 'var(--slate-200)' : 'var(--slate-500)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
          {selectedOption ? selectedOption.label : (placeholder || '請選擇')}
        </span>
        <ChevronRight size={14} style={{ transform: isOpen ? 'rotate(90deg)' : 'none', transition: 'transform 0.2s', color: 'var(--slate-500)', flexShrink: 0 }} />
      </div>
      
      {isOpen && (
        <>
          <div style={{ position: 'fixed', inset: 0, zIndex: 40 }} onClick={() => setIsOpen(false)} />
          <div style={{ 
            position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 100, 
            marginTop: 4, background: '#0f172a', border: '1px solid var(--slate-700)', 
            borderRadius: 6, boxShadow: '0 20px 25px -5px rgba(0,0,0,0.5), 0 8px 10px -6px rgba(0,0,0,0.5)', 
            overflow: 'hidden', backdropFilter: 'blur(12px)'
          }}>
            <div style={{ padding: 8, borderBottom: '1px solid var(--slate-700)', display: 'flex', alignItems: 'center', gap: 8 }}>
              <Search size={14} color="var(--slate-500)" />
              <input 
                autoFocus
                style={{ background: 'none', border: 'none', color: 'var(--slate-200)', fontSize: 13, outline: 'none', width: '100%' }}
                placeholder="搜尋關鍵字..."
                value={search}
                onChange={e => setSearch(e.target.value)}
                onClick={e => e.stopPropagation()}
              />
            </div>
            <div style={{ maxHeight: 200, overflowY: 'auto' }}>
              {filteredOptions.length > 0 ? (
                filteredOptions.map(o => (
                  <div 
                    key={o.value} 
                    style={{ 
                      padding: '8px 12px', fontSize: 13, cursor: 'pointer', 
                      background: value === o.value ? 'rgba(212, 175, 55, 0.15)' : 'transparent',
                      color: value === o.value ? 'var(--lux-gold)' : 'var(--slate-300)',
                      transition: 'background 0.1s'
                    }}
                    onMouseEnter={e => e.currentTarget.style.background = 'rgba(255,255,255,0.05)'}
                    onMouseLeave={e => e.currentTarget.style.background = value === o.value ? 'rgba(212, 175, 55, 0.15)' : 'transparent'}
                    onClick={(e) => { e.stopPropagation(); onChange(o.value); setIsOpen(false); setSearch(''); }}
                  >
                    {o.label}
                  </div>
                ))
              ) : (
                <div style={{ padding: '12px', fontSize: 12, color: 'var(--slate-500)', textAlign: 'center' }}>查無資料</div>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  );
};

const MultiSelect: React.FC<{
  options: { value: string; label: string }[];
  value: string[];
  onChange: (value: string[]) => void;
  placeholder?: string;
}> = ({ options, value, onChange, placeholder }) => {
  const [search, setSearch] = useState('');
  const filteredOptions = options.filter(option =>
    option.label.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div style={{ border: '1px solid var(--slate-700)', borderRadius: 6, padding: 8 }}>
      <input
        className="form-input"
        aria-label="搜尋收件人"
        placeholder={placeholder || '搜尋姓名或員工編號'}
        value={search}
        onChange={event => setSearch(event.target.value)}
        style={{ width: '100%', marginBottom: 6 }}
      />
      <div style={{ maxHeight: 180, overflowY: 'auto' }}>
        {filteredOptions.map(option => (
          <label key={option.value} style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '6px 4px', color: 'var(--slate-300)', fontSize: 13 }}>
            <input
              type="checkbox"
              checked={value.includes(option.value)}
              onChange={event => onChange(event.target.checked
                ? [...value, option.value]
                : value.filter(id => id !== option.value))}
            />
            {option.label}
          </label>
        ))}
        {filteredOptions.length === 0 && (
          <div style={{ padding: 10, color: 'var(--slate-500)', fontSize: 12, textAlign: 'center' }}>
            查無符合人員
          </div>
        )}
      </div>
      <div style={{ color: 'var(--slate-500)', fontSize: 11, marginTop: 6 }}>
        已選擇 {value.length} 位收件人
      </div>
    </div>
  );
};

function renderField(field: FormField, value: unknown, error: string | undefined, onChange: (name: string, value: unknown) => void, onBlur?: (name: string, error: string | null) => void, customRenderers?: Record<string, React.ReactNode>, getFieldOptionsFn?: (field: FormField) => { value: string; label: string }[], index?: number, sameAs?: boolean, onSameAsChange?: (checked: boolean) => void) {
  if (field.type === 'custom') {
    return <div key={field.name}>{customRenderers?.[field.name]}</div>;
  }
  const showLabel = field.type !== 'button';
  return (
    <div>
      {showLabel && <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
        {field.label}{field.required && <span style={{ color: 'var(--crimson-400)', marginLeft: 4 }}>*</span>}
      </label>}
      {field.type === 'select' ? (
        field.searchable ? (
          <SearchableSelect 
            options={getFieldOptionsFn ? getFieldOptionsFn(field) : field.options ?? []} 
            value={String(value ?? '')} 
            onChange={(val) => onChange(field.name, val)}
            placeholder={field.placeholder}
          />
        ) : (
          <select className="form-input" style={{ width: '100%' }}
            value={String(value ?? '')}
            onChange={e => onChange(field.name, e.target.value)}>
            <option value="">請選擇</option>
            {(getFieldOptionsFn ? getFieldOptionsFn(field) : field.options ?? []).map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
          </select>
        )
      ) : field.type === 'multi-select' ? (
        <MultiSelect
          options={getFieldOptionsFn ? getFieldOptionsFn(field) : field.options ?? []}
          value={Array.isArray(value) ? value.map(String) : []}
          onChange={selected => onChange(field.name, selected)}
          placeholder={field.placeholder}
        />
      ) : field.type === 'textarea' ? (
        <textarea className="form-input" style={{ width: '100%', minHeight: 80, resize: 'vertical' }}
          value={String(value ?? '')}
          onChange={e => onChange(field.name, e.target.value)}
          placeholder={field.placeholder} />
      ) : field.type === 'button' ? (
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => onChange(field.name, 'clicked')}
            style={{ padding: '8px 16px', fontSize: 13 }}
          >
            {field.label}
          </button>
          {field.description && <span style={{ fontSize: 12, color: 'var(--slate-500)' }}>{field.description}</span>}
        </div>
      ) : field.type === 'checkbox' ? (
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <input type="checkbox" checked={!!value}
            onChange={e => onChange(field.name, e.target.checked)}
            style={{ width: 16, height: 16, accentColor: 'var(--lux-gold)' }} />
          <span style={{ fontSize: 13, color: 'var(--slate-300)' }}>{field.placeholder || '啟用'}</span>
        </div>
      ) : (
        <input className="form-input" style={{ width: '100%' }}
          type={field.type}
          value={field.type === 'date' && value ? (typeof value === 'string' ? value.split('T')[0] : String(value).split('T')[0]) : String(value ?? '')}
          onChange={e => onChange(field.name, field.type === 'number' ? (e.target.value === '' ? '' : parseFloat(e.target.value)) : e.target.value)}
          onBlur={(e) => {
            const val = field.type === 'number' ? (e.target.value === '' ? '' : parseFloat(e.target.value)) : e.target.value;
            const rules = createFieldValidators(field);
            const error = validateField(val, rules);
            if (onBlur) onBlur(field.name, error);
            if (field.onBlur) field.onBlur(field.name, String(val ?? ''));
          }}
          placeholder={field.placeholder}
          min={field.min} max={field.max} step={field.step} />
      )}
      {field.description && <div style={{ fontSize: 11, color: 'var(--slate-500)', marginTop: 4, lineHeight: 1.4 }}>{field.description}</div>}
      {error && <div style={{ fontSize: 11, color: 'var(--crimson-400)', marginTop: 4 }}>{error}</div>}
      {sameAs !== undefined && (
        <div style={{ marginTop: 6, display: 'flex', alignItems: 'center', gap: 6, padding: '4px 8px', background: 'rgba(212, 175, 55, 0.05)', borderRadius: 4, width: 'fit-content', border: '1px solid rgba(212, 175, 55, 0.1)' }}>
          <input type="checkbox" checked={sameAs} onChange={e => onSameAsChange?.(e.target.checked)} style={{ width: 14, height: 14, accentColor: 'var(--lux-gold)', cursor: 'pointer' }} />
          <span style={{ fontSize: 12, color: 'var(--lux-gold)', cursor: 'pointer', fontWeight: 600 }}>同上（自動同步）</span>
        </div>
      )}
    </div>
  );
}

export function FormModal({ open, title, tabs, fields, initialValues, onClose, onSubmit, width = 600, customRenderers, onChangeData, onFieldDefaultChange, children, hiddenFields = [], isWizard = false, wizardConfig }: FormModalProps) {
  const hasTabs = tabs && tabs.length > 0;
  const isWizardMode = isWizard && !!wizardConfig;
  const wizardStepCount = isWizardMode ? wizardConfig!.stepTitles.length : 0;

  const getSafeAllFields = () => {
    if (tabs) return tabs.flatMap(t => t.fields || []);
    if (isWizardMode) {
      const all: FormField[] = [];
      const currentValues = initialValues || {};
      for (let i = 0; i < wizardStepCount; i++) {
        all.push(...wizardConfig!.getStepFields(i, currentValues));
      }
      return all;
    }
    return fields || [];
  };

  const [activeTab, setActiveTab] = useState(0);
  const [wizardStep, setWizardStep] = useState(0);
  const [values, setValues] = useState<Record<string, unknown>>({});
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [fieldOptions, setFieldOptions] = useState<Record<string, { value: string; label: string }[]>>({});
  const [fieldDefaults, setFieldDefaults] = useState<Record<string, unknown>>({});
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [initialized, setInitialized] = useState(false);
  const baseFields = getSafeAllFields();
  const hasChanged = (names: string[]) => initialized && names.some(name =>
    String(initialValues?.[name] ?? '') !== String(values[name] ?? '')
  );
  const isFieldVisible = (field: FormField) =>
    (!field.visibleWhen || String(values[field.visibleWhen.field] ?? '') === field.visibleWhen.equals)
    && (!field.visibleWhenChanged || hasChanged(field.visibleWhenChanged));
  const isFieldRequired = (field: FormField) =>
    field.required || (!!field.requiredWhenChanged && hasChanged(field.requiredWhenChanged));
  const allFields = baseFields.filter(field =>
    !hiddenFields.includes(field.name) && isFieldVisible(field)
  );

  useEffect(() => {
    if (open) {
      if (!initialized) {
        setActiveTab(0);
        setWizardStep(0);
        setErrors({});
        setFieldOptions({});
        setSubmitting(false);
        const init: Record<string, unknown> = {};
        allFields.forEach(f => { 
          const effectiveDefault = fieldDefaults[f.name] !== undefined ? fieldDefaults[f.name] : f.defaultValue;
          if (initialValues && initialValues[f.name] !== undefined) {
            init[f.name] = initialValues[f.name];
          } else {
            init[f.name] = effectiveDefault ?? (f.type === 'checkbox' ? false : f.type === 'multi-select' ? [] : '');
          }
        });
        setValues(init);
        setInitialized(true);
      }
    } else {
      setInitialized(false);
    }
  }, [open, initialValues, initialized, allFields, fieldDefaults]);

  useEffect(() => {
    if (open && onFieldDefaultChange) {
      setTimeout(() => {
        onFieldDefaultChange((fieldName: string, defaultValue: unknown) => {
          setFieldDefaults(prev => ({ ...prev, [fieldName]: defaultValue }));
        });
      }, 0);
    }
  }, [open, onFieldDefaultChange]);

  const getAllFields = () => {
    if (tabs) return tabs.flatMap(t => t.fields);
    if (wizardConfig) {
      const fields: FormField[] = [];
      for (let i = 0; i < wizardConfig.stepTitles.length; i++) {
        fields.push(...wizardConfig.getStepFields(i, values));
      }
      return fields;
    }
    return fields || [];
  };

  const handleChange = (name: string, value: unknown) => {
    const isSameAsToggle = name.startsWith('_sameAs_');
    
    setValues(prev => {
      const next = { ...prev };
      const fieldsForSameAs = tabs ? tabs.flatMap(t => t.fields) : (wizardConfig ? getAllFields() : fields || []);
      
      if (isSameAsToggle) {
        const targetField = name.replace('_sameAs_', '');
        const sourceField = fieldsForSameAs.find(f => f.name === targetField)?.sameAs;
        if (sourceField) {
          if (value === true) {
            next[targetField] = prev[sourceField] || '';
          } else {
            next[targetField] = '';
          }
        }
      } else {
        next[name] = value;
      }
      
      // 打字期間僅清除錯誤提示，不觸發新驗證，防止頻繁渲染導致失焦
      if (errors[name]) {
        setErrors(prevErr => { const n = { ...prevErr }; delete n[name]; return n; });
      }

      if (onChangeData) {
        setTimeout(() => {
          onChangeData(
            next, 
            setErrors, 
            (fieldName, options) => setFieldOptions(prev => ({ ...prev, [fieldName]: options })),
            (updates) => setValues(prevValues => ({ ...prevValues, ...updates }))
          );
        }, 0);
      }
      return next;
    });
  };

    const validate = () => {
      const errs: Record<string, string> = {};
      allFields.forEach(f => {
        if (f.type === 'custom') return;
        const fieldValidators = createFieldValidators({ ...f, required: isFieldRequired(f) });
        const error = validateField(values[f.name], fieldValidators);
        if (error) {
          errs[f.name] = error;
        }
      });
      setErrors(errs);
      
      const finalErrors = errs;
      const hasAnyError = Object.keys(finalErrors).length > 0;
      
      if (hasAnyError) {
        if (hasTabs) {
          for (let i = 0; i < tabs!.length; i++) {
            if (tabs![i].fields.some(f => finalErrors[f.name])) {
              setActiveTab(i);
              break;
            }
          }
        } else if (isWizardMode) {
          for (let i = 0; i < wizardStepCount; i++) {
            const stepFields = wizardConfig!.getStepFields(i, values).filter(f => !hiddenFields.includes(f.name));
            if (stepFields.some(f => finalErrors[f.name])) {
              setWizardStep(i);
              break;
            }
          }
        }
      }
      return !hasAnyError;
    };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError(null);
    if (!validate()) return;
    setSubmitting(true);
    const safeValues = Object.fromEntries(
      Object.entries(values).map(([name, value]) => [name, /password/i.test(name) ? '[REDACTED]' : value])
    );
    console.log('[FormModal] 提交表單:', JSON.stringify(safeValues, null, 2));
    try {
      if (onSubmit) {
        await onSubmit(values);
      }
      onClose();
    } catch (error: unknown) {
      let message = error instanceof Error ? error.message : String(error);
      const response = error && typeof error === 'object' && 'response' in error
        ? error.response
        : undefined;
      const responseData = response && typeof response === 'object' && 'data' in response
        ? response.data
        : undefined;
      const responseBody = responseData && typeof responseData === 'object'
        ? responseData as Record<string, unknown>
        : undefined;

      // 嘗試從後端回應獲取更詳細的驗證錯誤
      const errors = responseBody?.errors;
      if (errors && typeof errors === 'object' && !Array.isArray(errors)) {
        const detailErrors = Object.entries(errors as Record<string, unknown>)
          .map(([field, messages]) => `${field}: ${Array.isArray(messages) ? messages.map(String).join(', ') : String(messages)}`)
          .join(' | ');
        if (detailErrors) message = `驗證失敗 - ${detailErrors}`;
      } else if (typeof responseBody?.message === 'string') {
        message = responseBody.message;
      }
      
      setSubmitError(message);
      console.error('[FormModal] 提交錯誤:', message);
    } finally {
      setSubmitting(false);
    }
  };

  if (!open) return null;

  const currentFields = (hasTabs ? tabs![activeTab].fields : (isWizardMode ? wizardConfig.getStepFields(wizardStep, values) : allFields))
    .filter(isFieldVisible);
  const stepHiddenFields = isWizardMode && wizardConfig.getStepHiddenFields ? wizardConfig.getStepHiddenFields(wizardStep, values) : [];
  const wizardFilteredFields = currentFields.filter(f => !stepHiddenFields.includes(f.name));

  const getFieldOptionsFn = (field: FormField) => fieldOptions[field.name] ?? field.options ?? [];

  return (
    <div 
      style={{ 
        position: 'fixed', 
        inset: 0, 
        zIndex: 1000, 
        display: 'flex', 
        alignItems: 'flex-start', 
        justifyContent: 'center', 
        paddingTop: '40px' 
      }}
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      {/* 背景遮罩 */}
      <div 
        style={{ 
          position: 'fixed', 
          inset: 0, 
          background: 'rgba(4, 8, 15, 0.8)', 
          backdropFilter: 'blur(4px)',
          zIndex: 0 
        }} 
      />
      
      {/* 內容卡片 */}
      <div 
        style={{ 
          position: 'relative', 
          width, 
          maxHeight: '85vh', 
          display: 'flex', 
          flexDirection: 'column', 
          padding: 0, 
          zIndex: 1,
          overflow: 'hidden',
          background: '#040811',
          border: '1px solid var(--lux-border)',
          borderRadius: '24px',
          boxShadow: '0 40px 100px rgba(0,0,0,0.9), 0 0 1px rgba(255,255,255,0.15)'
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '16px 20px', borderBottom: '1px solid var(--glass-border)', borderTop: '0px', borderLeft: '0px', borderRight: '0px', flexShrink: 0 }}>
          <h3 style={{ fontSize: 15, fontWeight: 700, color: 'var(--slate-100)', fontFamily: 'var(--font-display)', margin: 0 }}>{title}</h3>
          <button onClick={onClose} style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--slate-500)', padding: 4, borderRadius: 6 }}>
            <X size={18} />
          </button>
        </div>

        {/* 提交錯誤提示 */}
        {submitError && (
          <div style={{
            margin: '12px 20px 0',
            padding: '10px 14px',
            background: 'rgba(232, 51, 82, 0.12)',
            border: '1px solid rgba(232, 51, 82, 0.25)',
            borderRadius: 'var(--radius-md)',
            color: 'var(--crimson-300)',
            fontSize: 13,
            display: 'flex',
            alignItems: 'center',
            gap: 8
          }}>
            <span style={{ fontWeight: 600 }}>錯誤：</span>
            {submitError}
          </div>
        )}

        {hasTabs && (
          <div style={{ display: 'flex', borderBottom: '1px solid var(--glass-border)', overflow: 'auto', flexShrink: 0 }}>
            {tabs!.map((tab, i) => {
              const hasError = tab.fields.some(f => errors[f.name]);
              return (
                <button key={i} type="button" onClick={() => setActiveTab(i)}
                  style={{
                    flex: 1, padding: '10px 12px', fontSize: 12, fontWeight: 600, whiteSpace: 'nowrap',
                    fontFamily: 'var(--font-display)', letterSpacing: '0.02em',
                    background: activeTab === i ? 'rgba(212,175,55,0.08)' : 'transparent',
                    color: activeTab === i ? 'var(--lux-gold)' : hasError ? 'var(--crimson-400)' : 'var(--slate-500)',
                    borderBottom: activeTab === i ? '2px solid var(--lux-gold)' : '2px solid transparent',
                    borderTop: '0px', borderLeft: '0px', borderRight: '0px', cursor: 'pointer', transition: 'all 0.15s',
                  }}>
                  {tab.label}
                  {hasError && <span style={{ marginLeft: 4, fontSize: 10, filter: 'drop-shadow(0 0 5px var(--lux-gold))' }}>●</span>}
                </button>
              );
            })}
          </div>
        )}

        {isWizardMode && (
          <div style={{ display: 'flex', borderBottom: '1px solid var(--glass-border)', background: 'rgba(45,110,245,0.03)', padding: '12px 20px', flexShrink: 0 }}>
            {wizardConfig.stepTitles.map((stepTitle, i) => {
              const isActive = wizardStep === i;
              const isCompleted = wizardStep > i;
              return (
                <div key={i} style={{ display: 'flex', alignItems: 'center', flex: 1 }}>
                  <div style={{
                    width: 28, height: 28, borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center',
                    background: isCompleted ? 'var(--lux-emerald)' : isActive ? 'var(--lux-gold)' : 'rgba(255,255,255,0.05)',
                    color: isCompleted || isActive ? '#000' : 'var(--slate-500)', fontSize: 12, fontWeight: 800,
                    boxShadow: (isActive || isCompleted) ? '0 0 15px rgba(212,175,55,0.3)' : 'none',
                    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)'
                  }}>
                    {isCompleted ? <Check size={14} /> : i + 1}
                  </div>
                  <span style={{
                    marginLeft: 8, fontSize: 12, fontWeight: isActive ? 600 : 400,
                    color: isActive ? 'var(--slate-200)' : 'var(--slate-500)',
                    whiteSpace: 'nowrap'
                  }}>
                    {stepTitle}
                  </span>
                  {i < wizardStepCount - 1 && (
                    <div style={{
                      flex: 1, height: 2, margin: '0 12px',
                      background: isCompleted ? 'var(--lux-emerald)' : 'rgba(255,255,255,0.05)'
                    }} />
                  )}
                </div>
              );
            })}
          </div>
        )}

        <form onSubmit={handleSubmit} style={{ flex: 1, overflow: 'auto', padding: '20px', display: 'flex', flexDirection: 'column' }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 16, flex: 1 }}>
            {children || (
              <>
              {(isWizardMode ? wizardFilteredFields : currentFields).map((field, index) => {
                const allFieldsForSameAs = hasTabs ? tabs!.flatMap(t => t.fields) : (wizardConfig ? getAllFields() : allFields);
                const sourceField = field.sameAs ? allFieldsForSameAs.find(f => f.name === field.sameAs) : null;
                const isSameAs = sourceField ? values[field.name] === values[field.sameAs] : undefined;
                return <React.Fragment key={field.name || index}>{renderField({ ...field, required: isFieldRequired(field) }, values[field.name], errors[field.name], handleChange, (name, err) => {
                  setErrors(prev => {
                    const next = { ...prev };
                    if (err) next[name] = err;
                    else delete next[name];
                    return next;
                  });
                }, customRenderers, getFieldOptionsFn, index, isSameAs, (checked) => handleChange('_sameAs_' + field.name, checked))}</React.Fragment>;
              })}
              {customRenderers && Object.keys(customRenderers).map(key => {
                if (hasTabs) {
                  const tabLabel = tabs![activeTab].label;
                  if (key === '_items' && tabLabel === '明細') return <div key={key}>{customRenderers[key]}</div>;
                  if (key === '_summary' && tabLabel === '完成') return <div key={key}>{customRenderers[key]}</div>;
                }
                return null;
              })}
              </>
            )}
          </div>

          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 24, paddingTop: 16, borderTop: '1px solid var(--glass-border)', flexShrink: 0 }}>
            {hasTabs ? (
              <div style={{ display: 'flex', gap: 8 }}>
                <button type="button" className="btn btn-ghost btn-sm" disabled={activeTab === 0} onClick={() => setActiveTab(i => i - 1)}
                  style={{ opacity: activeTab === 0 ? 0.3 : 1 }}>
                  <ChevronLeft size={14} /> 上一頁
                </button>
                {activeTab < tabs!.length - 1 ? (
                  <button type="button" className="btn btn-ghost btn-sm" onClick={() => setActiveTab(i => i + 1)}>
                    下一頁 <ChevronRight size={14} />
                  </button>
                ) : null}
              </div>
            ) : isWizardMode ? (
              <div style={{ display: 'flex', gap: 8 }}>
                <button type="button" className="btn btn-ghost btn-sm" disabled={wizardStep === 0} onClick={() => setWizardStep(s => s - 1)}
                  style={{ opacity: wizardStep === 0 ? 0.3 : 1 }}>
                  <ChevronLeft size={14} /> 上一步
                </button>
                {wizardStep < wizardStepCount - 1 ? (
                  <button type="button" className="btn btn-ghost btn-sm" onClick={() => {
                    const stepErrors: Record<string, string> = {};
                    wizardFilteredFields.forEach(f => {
                      if (f.type === 'custom') return;
                      const error = validateField(values[f.name], createFieldValidators(f));
                      if (error) stepErrors[f.name] = error;
                    });
                    if (Object.keys(stepErrors).length > 0) {
                      setErrors(prev => ({ ...prev, ...stepErrors }));
                      return;
                    }
                    setWizardStep(s => s + 1);
                  }}>
                    下一步 <ChevronRight size={14} />
                  </button>
                ) : null}
              </div>
            ) : <div />}
            <div style={{ display: 'flex', gap: 10 }}>
              <button type="button" className="btn btn-ghost btn-sm" onClick={onClose}>關閉</button>
              {onSubmit && (
                <button type="submit" className="btn btn-primary btn-sm" disabled={submitting}
                  style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                  {submitting ? <Loader2 size={14} className="spin-icon" /> : <Save size={14} />}
                  {submitting ? '儲存中...' : (isWizardMode ? '完成' : '儲存')}
                </button>
              )}
            </div>
          </div>
        </form>
      </div>
      <style>{`.spin-icon { animation: spin 0.8s linear infinite; } @keyframes spin { to { transform: rotate(360deg); } }`}</style>
    </div>
  );
}
