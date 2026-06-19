import React, { useState } from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faEye, faEyeSlash } from '@fortawesome/free-solid-svg-icons';

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
  showToggle?: boolean;
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
  showToggle = false,
}) => {
  const [showPassword, setShowPassword] = useState(false);
  const inputId = id || label?.toLowerCase().replace(/\s+/g, '-');
  const isPasswordToggle = showToggle && type === 'password';
  const resolvedType = isPasswordToggle && showPassword ? 'text' : type;

  const inputClasses = ['form-control', error ? 'is-invalid' : '', isPasswordToggle ? 'pe-5' : '', className]
    .filter(Boolean)
    .join(' ');

  return (
    <div className="mb-3 w-100 text-start">
      {label && (
        <label htmlFor={inputId} className="form-label fw-medium mb-1">
          {label}
          {required && <span className="text-danger ms-1">*</span>}
        </label>
      )}
      <div className={isPasswordToggle ? 'position-relative' : ''}>
        <input
          type={resolvedType}
          id={inputId}
          placeholder={placeholder}
          value={value}
          onChange={(e) => onChange?.(e.target.value)}
          onBlur={onBlur}
          disabled={disabled}
          required={required}
          className={inputClasses}
          aria-invalid={!!error}
          aria-describedby={error ? `${inputId}-error` : undefined}
        />
        {isPasswordToggle && (
          <button
            type="button"
            onClick={() => setShowPassword(prev => !prev)}
            tabIndex={-1}
            aria-label={showPassword ? 'Hide password' : 'Show password'}
            className="position-absolute top-50 end-0 translate-middle-y border-0 bg-transparent text-secondary pe-3"
          >
            <FontAwesomeIcon icon={showPassword ? faEyeSlash : faEye} />
          </button>
        )}
        {error && (
          <div id={`${inputId}-error`} className="invalid-feedback">
            {error}
          </div>
        )}
      </div>
    </div>
  );
};
