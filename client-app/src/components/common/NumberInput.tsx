import { useEffect, useState } from 'react'

interface INumberInputProps {
  id: string
  value: number
  onChange: (value: number) => void
  min?: number
  max?: number
  step?: number
  decimals?: number
  disabled?: boolean
  isInvalid?: boolean
}

function format(value: number, decimals: number): string {
  return value === 0 ? '' : value.toFixed(decimals)
}

export function NumberInput({ id, value, onChange, min, max, step, decimals = 0, disabled, isInvalid }: INumberInputProps) {
  const [text, setText] = useState(() => format(value, decimals))
  const [isFocused, setIsFocused] = useState(false)

  useEffect(() => {
    if (!isFocused) {
      setText(format(value, decimals))
    }
  }, [value, decimals, isFocused])

  return (
    <input
      type="number"
      id={id}
      name={id}
      className={`form-control no-spinner ${isInvalid ? 'is-invalid' : ''}`}
      value={text}
      placeholder={decimals > 0 ? (0).toFixed(decimals) : '0'}
      min={min}
      max={max}
      step={step ?? (decimals > 0 ? 1 / 10 ** decimals : 1)}
      disabled={disabled}
      aria-invalid={isInvalid}
      onFocus={() => setIsFocused(true)}
      onBlur={() => setIsFocused(false)}
      onChange={(e) => {
        const raw = e.target.value
        setText(raw)
        const parsed = raw === '' ? 0 : Number(raw)
        if (!Number.isNaN(parsed)) {
          onChange(parsed)
        }
      }}
    />
  )
}
