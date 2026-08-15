import type { CSSProperties } from 'react'
import { generateTimeOptions } from '../../utils/timeOptions'

const TIME_OPTIONS = generateTimeOptions(30)

interface ITimeSelectProps {
  id: string
  value: string
  onChange: (value: string) => void
  disabled?: boolean
  isInvalid?: boolean
  className?: string
  style?: CSSProperties
}

export function TimeSelect({ id, value, onChange, disabled, isInvalid, className = '', style }: ITimeSelectProps) {
  const hasExactMatch = TIME_OPTIONS.some((option) => option.value === value)

  return (
    <select
      id={id}
      name={id}
      className={`form-select ${isInvalid ? 'is-invalid' : ''} ${className}`.trim()}
      style={style}
      value={value}
      disabled={disabled}
      aria-invalid={isInvalid}
      onChange={(e) => onChange(e.target.value)}
    >
      {value === '' ? <option value="">Select a time</option> : !hasExactMatch && <option value={value}>{value}</option>}
      {TIME_OPTIONS.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </select>
  )
}
