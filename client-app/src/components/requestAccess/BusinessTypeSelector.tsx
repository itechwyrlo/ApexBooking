import { BUSINESS_TYPE_OPTIONS } from '../../config/businessTypes'
import { BusinessType } from '../../types/BusinessType'
import { Icon } from '../common/Icon'

interface IBusinessTypeSelectorProps {
  id: string
  value: BusinessType | ''
  onChange: (value: BusinessType) => void
  onSelect: () => void
  isInvalid: boolean
}

const BUSINESS_TYPE_ICONS: Record<BusinessType, string> = {
  [BusinessType.BarberShop]: 'barbershop',
  [BusinessType.Salon]: 'salon',
  [BusinessType.Spa]: 'spa',
  [BusinessType.Clinic]: 'clinic',
  [BusinessType.FitnessStudio]: 'fitness',
  [BusinessType.AutoRepair]: 'auto-repair',
  [BusinessType.RetailService]: 'retail-service',
  [BusinessType.Other]: 'other',
}

export function BusinessTypeSelector({ id, value, onChange, onSelect, isInvalid }: IBusinessTypeSelectorProps) {
  return (
    <div id={id} className="row row-cols-2 row-cols-md-4 g-2" role="radiogroup" aria-invalid={isInvalid}>
      {BUSINESS_TYPE_OPTIONS.map((option) => {
        const isSelected = value === option.value

        return (
          <div className="col" key={option.value}>
            <button
              type="button"
              role="radio"
              aria-checked={isSelected}
              className={`card border-0 shadow-sm business-type-chip ${isSelected ? 'business-type-chip--selected' : ''}`}
              onClick={() => {
                onChange(option.value)
                onSelect()
              }}
            >
              <Icon name={BUSINESS_TYPE_ICONS[option.value]} size={22} />
              <span>{option.label}</span>
            </button>
          </div>
        )
      })}
    </div>
  )
}
