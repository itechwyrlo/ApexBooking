import React from 'react';

interface SelectOption {
  label: string;
  value: string;
}

interface SelectProps {
  label?: string;
  value?: string;
  onChange?: (value: string) => void;
  options: SelectOption[];
  placeholder?: string;
  error?: string;
  disabled?: boolean;
  required?: boolean;
  className?: string;
  id?: string;
}

export const Select: React.FC<SelectProps> = ({
  label,
  value,
  onChange,
  options,
  placeholder = 'Select option',
  error,
  disabled = false,
  required = false,
  className = '',
  id,
}) => {
  const selectId = id || label?.toLowerCase().replace(/\s+/g, '-');

  const classes = `form-select ${error ? 'is-invalid' : ''} ${className}`;

  return (
    <div className="mb-3 w-100 text-start">
      {label && (
        <label htmlFor={selectId} className="form-label fw-medium mb-1">
          {label}
          {required && <span className="text-danger ms-1">*</span>}
        </label>
      )}

      <select
        id={selectId}
        value={value}
        onChange={(e) => onChange?.(e.target.value)}
        disabled={disabled}
        required={required}
        className={classes}
        aria-invalid={!!error}
        aria-describedby={error ? `${selectId}-error` : undefined}
      >
        <option value="">{placeholder}</option>

        {options.map(option => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>

      {error && (
        <div id={`${selectId}-error`} className="invalid-feedback">
          {error}
        </div>
      )}
    </div>
  );
};