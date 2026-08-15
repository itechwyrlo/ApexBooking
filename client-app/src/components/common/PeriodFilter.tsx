import { Button } from './Button'
import type { RevenuePeriod } from '../../utils/dateRanges'

const OPTIONS: { value: RevenuePeriod; label: string }[] = [
  { value: 'today', label: 'Today' },
  { value: 'week', label: 'This Week' },
  { value: 'month', label: 'This Month' },
]

interface IPeriodFilterProps {
  value: RevenuePeriod
  onChange: (period: RevenuePeriod) => void
}

export function PeriodFilter({ value, onChange }: IPeriodFilterProps) {
  return (
    <div className="btn-group" role="group" aria-label="Revenue period">
      {OPTIONS.map((option) => (
        <Button
          key={option.value}
          type="button"
          size="sm"
          variant={value === option.value ? 'primary' : 'outline-secondary'}
          onClick={() => onChange(option.value)}
        >
          {option.label}
        </Button>
      ))}
    </div>
  )
}
