import type { ReactNode } from 'react'

interface IFormGroupProps {
  label: string
  htmlFor: string
  error?: string
  required?: boolean
  children: ReactNode
}

export function FormGroup({ label, htmlFor, error, required, children }: IFormGroupProps) {
  return (
    <div className="mb-3">
      <label htmlFor={htmlFor} className="form-label">
        {label}
        {required && (
          <span className="text-danger ms-1" aria-hidden="true">
            *
          </span>
        )}
      </label>
      {children}
      {error && (
        <div id={`${htmlFor}-error`} className="invalid-feedback d-block" role="alert">
          {error}
        </div>
      )}
    </div>
  )
}
