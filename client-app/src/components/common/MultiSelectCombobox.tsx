import { useEffect, useMemo, useRef, useState } from 'react'
import { Icon } from './Icon'
import { Skeleton } from './Skeleton'

export interface IMultiSelectOption {
  value: string
  label: string
  sublabel?: string | null
}

interface IMultiSelectComboboxProps {
  options: IMultiSelectOption[]
  selectedValues: string[]
  onChange: (values: string[]) => void
  placeholder?: string
  searchPlaceholder?: string
  emptyMessage?: string
  isLoading?: boolean
  disabled?: boolean
}

export function MultiSelectCombobox({
  options,
  selectedValues,
  onChange,
  placeholder = 'Select...',
  searchPlaceholder = 'Search...',
  emptyMessage = 'No options available.',
  isLoading = false,
  disabled = false,
}: IMultiSelectComboboxProps) {
  const [searchTerm, setSearchTerm] = useState('')
  const searchInputRef = useRef<HTMLInputElement>(null)
  const toggleRef = useRef<HTMLDivElement>(null)

  const selectedOptions = useMemo(
    () => options.filter((option) => selectedValues.includes(option.value)),
    [options, selectedValues],
  )

  const filteredOptions = useMemo(() => {
    const term = searchTerm.trim().toLowerCase()
    if (!term) return options
    return options.filter(
      (option) => option.label.toLowerCase().includes(term) || option.sublabel?.toLowerCase().includes(term),
    )
  }, [options, searchTerm])

  const toggleValue = (value: string) => {
    if (selectedValues.includes(value)) {
      onChange(selectedValues.filter((id) => id !== value))
    } else {
      onChange([...selectedValues, value])
    }
  }

  const removeValue = (value: string) => {
    onChange(selectedValues.filter((id) => id !== value))
  }

  useEffect(() => {
    const node = toggleRef.current
    if (!node) return

    const handleShown = () => searchInputRef.current?.focus()
    const handleHidden = () => setSearchTerm('')

    node.addEventListener('shown.bs.dropdown', handleShown)
    node.addEventListener('hidden.bs.dropdown', handleHidden)
    return () => {
      node.removeEventListener('shown.bs.dropdown', handleShown)
      node.removeEventListener('hidden.bs.dropdown', handleHidden)
    }
  }, [])

  const handleToggleKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault()
      event.currentTarget.click()
    }
  }

  return (
    <div className="dropdown">
      <div
        className={`form-control dropdown-toggle d-flex flex-wrap align-items-center gap-1 ${disabled ? 'disabled' : ''}`}
        role="button"
        tabIndex={disabled ? -1 : 0}
        aria-haspopup="listbox"
        aria-disabled={disabled}
        data-bs-toggle={disabled ? undefined : 'dropdown'}
        data-bs-auto-close="outside"
        onKeyDown={disabled ? undefined : handleToggleKeyDown}
        style={{ minHeight: 'calc(1.5em + 0.75rem + 2px)', cursor: disabled ? 'not-allowed' : 'pointer' }}
        ref={toggleRef}
      >
        {selectedOptions.length === 0 && <span className="text-muted">{placeholder}</span>}
        {selectedOptions.map((option) => (
          <span key={option.value} className="badge rounded-pill text-bg-light border d-inline-flex align-items-center gap-1 fw-medium">
            {option.label}
            <button
              type="button"
              className="btn-close"
              style={{ fontSize: '0.55rem' }}
              aria-label={`Remove ${option.label}`}
              onClick={(event) => {
                event.stopPropagation()
                removeValue(option.value)
              }}
            />
          </span>
        ))}
      </div>

      <div className="dropdown-menu w-100 p-0 shadow-sm" style={{ maxWidth: 'none' }}>
        <div className="p-2 border-bottom">
          <div className="input-group input-group-sm">
            <span className="input-group-text bg-transparent border-end-0">
              <Icon name="search" size={14} />
            </span>
            <input
              ref={searchInputRef}
              type="text"
              className="form-control border-start-0"
              placeholder={searchPlaceholder}
              value={searchTerm}
              onChange={(event) => setSearchTerm(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') event.preventDefault()
              }}
            />
          </div>
        </div>

        <div style={{ maxHeight: '260px', overflowY: 'auto' }}>
          {isLoading ? (
            <div className="p-2">
              <Skeleton height="1.5rem" className="mb-2" />
              <Skeleton height="1.5rem" className="mb-2" />
              <Skeleton height="1.5rem" />
            </div>
          ) : filteredOptions.length === 0 ? (
            <div className="px-3 py-3 text-muted small text-center">
              {options.length === 0 ? emptyMessage : 'No matches found.'}
            </div>
          ) : (
            filteredOptions.map((option) => (
              <label key={option.value} className="dropdown-item d-flex align-items-start gap-2 py-2" style={{ cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  className="form-check-input mt-1 flex-shrink-0"
                  checked={selectedValues.includes(option.value)}
                  onChange={() => toggleValue(option.value)}
                />
                <span>
                  <div className="fw-medium">{option.label}</div>
                  {option.sublabel && <div className="text-muted small">{option.sublabel}</div>}
                </span>
              </label>
            ))
          )}
        </div>
      </div>
    </div>
  )
}
