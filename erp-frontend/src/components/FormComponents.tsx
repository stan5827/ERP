import React, { forwardRef, useId } from 'react';
import './FormComponents.css';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  helpText?: string;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(({
  label,
  error,
  helpText,
  leftIcon,
  rightIcon,
  className = '',
  id,
  required,
  ...props
}, ref) => {
  const generatedId = useId();
  const inputId = id || generatedId;
  const errorId = `${inputId}-error`;

  return (
    <div className={`form-group ${className}`}>
      {label && (
        <label 
          htmlFor={inputId} 
          className={`form-label ${required ? 'required' : ''}`}
        >
          {label}
        </label>
      )}
      <div className="input-wrapper">
        {leftIcon && <span className="input-icon input-left-icon">{leftIcon}</span>}
        <input
          ref={ref}
          id={inputId}
          className={`form-input ${error ? 'error' : ''} ${leftIcon ? 'has-left-icon' : ''} ${rightIcon ? 'has-right-icon' : ''}`}
          aria-invalid={!!error}
          aria-describedby={error ? errorId : helpText ? `${inputId}-help` : undefined}
          required={required}
          {...props}
        />
        {rightIcon && <span className="input-icon input-right-icon">{rightIcon}</span>}
      </div>
      {error && (
        <span id={errorId} className="form-error" role="alert">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 22C6.477 22 2 17.523 2 12S6.477 2 12 2s10 4.477 10 10-4.477 10-10 10zm-1-7v2h2v-2h-2zm0-8v6h2V7h-2z"/>
          </svg>
          {error}
        </span>
      )}
      {helpText && !error && (
        <span id={`${inputId}-help`} className="form-help">{helpText}</span>
      )}
    </div>
  );
});

Input.displayName = 'Input';

interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
  helpText?: string;
}

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(({
  label,
  error,
  helpText,
  className = '',
  id,
  required,
  ...props
}, ref) => {
  const generatedId = useId();
  const inputId = id || generatedId;
  const errorId = `${inputId}-error`;

  return (
    <div className={`form-group ${className}`}>
      {label && (
        <label 
          htmlFor={inputId} 
          className={`form-label ${required ? 'required' : ''}`}
        >
          {label}
        </label>
      )}
      <textarea
        ref={ref}
        id={inputId}
        className={`form-input form-textarea ${error ? 'error' : ''}`}
        aria-invalid={!!error}
        aria-describedby={error ? errorId : helpText ? `${inputId}-help` : undefined}
        required={required}
        {...props}
      />
      {error && (
        <span id={errorId} className="form-error" role="alert">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 22C6.477 22 2 17.523 2 12S6.477 2 12 2s10 4.477 10 10-4.477 10-10 10zm-1-7v2h2v-2h-2zm0-8v6h2V7h-2z"/>
          </svg>
          {error}
        </span>
      )}
      {helpText && !error && (
        <span id={`${inputId}-help`} className="form-help">{helpText}</span>
      )}
    </div>
  );
});

Textarea.displayName = 'Textarea';

interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

interface SelectProps extends Omit<React.SelectHTMLAttributes<HTMLSelectElement>, 'children'> {
  label?: string;
  error?: string;
  helpText?: string;
  options: SelectOption[];
  placeholder?: string;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(({
  label,
  error,
  helpText,
  options,
  placeholder,
  className = '',
  id,
  required,
  ...props
}, ref) => {
  const generatedId = useId();
  const inputId = id || generatedId;
  const errorId = `${inputId}-error`;

  return (
    <div className={`form-group ${className}`}>
      {label && (
        <label 
          htmlFor={inputId} 
          className={`form-label ${required ? 'required' : ''}`}
        >
          {label}
        </label>
      )}
      <select
        ref={ref}
        id={inputId}
        className={`form-input form-select ${error ? 'error' : ''}`}
        aria-invalid={!!error}
        aria-describedby={error ? errorId : helpText ? `${inputId}-help` : undefined}
        required={required}
        {...props}
      >
        {placeholder && (
          <option value="" disabled>
            {placeholder}
          </option>
        )}
        {options.map(opt => (
          <option key={opt.value} value={opt.value} disabled={opt.disabled}>
            {opt.label}
          </option>
        ))}
      </select>
      {error && (
        <span id={errorId} className="form-error" role="alert">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 22C6.477 22 2 17.523 2 12S6.477 2 12 2s10 4.477 10 10-4.477 10-10 10zm-1-7v2h2v-2h-2zm0-8v6h2V7h-2z"/>
          </svg>
          {error}
        </span>
      )}
      {helpText && !error && (
        <span id={`${inputId}-help`} className="form-help">{helpText}</span>
      )}
    </div>
  );
});

Select.displayName = 'Select';

interface CheckboxProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: string;
  error?: string;
}

export const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(({
  label,
  error,
  className = '',
  id,
  ...props
}, ref) => {
  const generatedId = useId();
  const inputId = id || generatedId;

  return (
    <div className={`form-group checkbox-group ${className}`}>
      <label htmlFor={inputId} className="checkbox-label">
        <input
          ref={ref}
          type="checkbox"
          id={inputId}
          className={`checkbox-input ${error ? 'error' : ''}`}
          {...props}
        />
        <span className="checkbox-custom">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3">
            <polyline points="20 6 9 17 4 12" />
          </svg>
        </span>
        <span className="checkbox-text">{label}</span>
      </label>
      {error && (
        <span className="form-error" role="alert">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 22C6.477 22 2 17.523 2 12S6.477 2 12 2s10 4.477 10 10-4.477 10-10 10zm-1-7v2h2v-2h-2zm0-8v6h2V7h-2z"/>
          </svg>
          {error}
        </span>
      )}
    </div>
  );
});

Checkbox.displayName = 'Checkbox';

interface RadioOption {
  value: string;
  label: string;
  disabled?: boolean;
}

interface RadioGroupProps {
  name: string;
  options: RadioOption[];
  value?: string;
  onChange?: (value: string) => void;
  label?: string;
  error?: string;
  inline?: boolean;
}

export const RadioGroup: React.FC<RadioGroupProps> = ({
  name,
  options,
  value,
  onChange,
  label,
  error,
  inline = false,
}) => {
  return (
    <div className="form-group">
      {label && <label className="form-label">{label}</label>}
      <div className={`radio-group ${inline ? 'radio-inline' : ''}`}>
        {options.map(opt => (
          <label key={opt.value} className="radio-label">
            <input
              type="radio"
              name={name}
              value={opt.value}
              checked={value === opt.value}
              onChange={e => onChange?.(e.target.value)}
              disabled={opt.disabled}
              className="radio-input"
            />
            <span className="radio-custom" />
            <span className="radio-text">{opt.label}</span>
          </label>
        ))}
      </div>
      {error && <span className="form-error" role="alert">{error}</span>}
    </div>
  );
};

interface SwitchProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label: string;
}

export const Switch = forwardRef<HTMLInputElement, SwitchProps>(({
  label,
  className = '',
  id,
  ...props
}, ref) => {
  const generatedId = useId();
  const inputId = id || generatedId;

  return (
    <div className={`form-group switch-group ${className}`}>
      <label htmlFor={inputId} className="switch-label">
        <input
          ref={ref}
          type="checkbox"
          id={inputId}
          className="switch-input"
          {...props}
        />
        <span className="switch-track">
          <span className="switch-thumb" />
        </span>
        <span className="switch-text">{label}</span>
      </label>
    </div>
  );
});

Switch.displayName = 'Switch';