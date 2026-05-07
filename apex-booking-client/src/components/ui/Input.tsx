import React from 'react';

interface InputProps {
  label?: string;
  type?: 'text' | 'email' | 'password' | 'number';
  placeholder?: string;
  value?: string;
  onChange?: (value: string) => void;
  onBlur?: () => void;
  error?: string;
  disabled?: boolean;
  required?: boolean;
  className?: string;
  id?: string;
}

export const Input: React.FC<InputProps> = ({
  label,
  type = 'text',
  placeholder,
  value,
  onChange,
  onBlur,
  error,
  disabled = false,
  required = false,
  className = '',
  id,
}) => {
  const inputId = id || label?.toLowerCase().replace(/\s+/g, '-');

  // Switch from Tailwind classes to Bootstrap classes
  const classes = `form-control ${error ? 'is-invalid' : ''} ${className}`;

  return (
    <div className="mb-3 w-100 text-start">
      {label && (
        <label htmlFor={inputId} className="form-label fw-medium mb-1">
          {label}
          {required && <span className="text-danger ms-1">*</span>}
        </label>
      )}
      <input
        type={type}
        id={inputId}
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange?.(e.target.value)}
        onBlur={onBlur}
        disabled={disabled}
        required={required}
        className={classes}
        aria-invalid={!!error}
        aria-describedby={error ? `${inputId}-error` : undefined}
      />
      {error && (
        <div id={`${inputId}-error`} className="invalid-feedback">
          {error}
        </div>
      )}
    </div>
  );
};
