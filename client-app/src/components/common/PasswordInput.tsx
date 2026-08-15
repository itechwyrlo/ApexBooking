import { useState } from 'react'
import { Icon } from './Icon'

interface IPasswordInputProps {
  id: string
  value: string
  onChange: (value: string) => void
  onBlur?: () => void
  autoComplete?: string
  isInvalid?: boolean
  disabled?: boolean
  'aria-describedby'?: string
}

/**
 * Reuses the `.password-input-wrap` / `.password-toggle` styling already
 * shipped in auth.css (globally loaded) and used on the login/reset-password
 * pages, so any future secret field in a dashboard modal looks and behaves
 * the same as the rest of the app.
 */
export function PasswordInput({ id, value, onChange, onBlur, autoComplete, isInvalid, disabled, ...rest }: IPasswordInputProps) {
  const [isVisible, setIsVisible] = useState(false)

  return (
    <div className="password-input-wrap">
      <input
        type={isVisible ? 'text' : 'password'}
        id={id}
        name={id}
        autoComplete={autoComplete}
        className={`form-control ${isInvalid ? 'is-invalid' : ''}`}
        value={value}
        disabled={disabled}
        aria-invalid={isInvalid}
        onChange={(event) => onChange(event.target.value)}
        onBlur={onBlur}
        {...rest}
      />
      <button
        type="button"
        className="password-toggle"
        onClick={() => setIsVisible((visible) => !visible)}
        aria-label={isVisible ? 'Hide password' : 'Show password'}
        disabled={disabled}
      >
        <Icon name={isVisible ? 'eye-slash' : 'eye'} size={18} />
      </button>
    </div>
  )
}
