import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { IPricingPlan } from '../../interfaces/IPricingPlan'
import { Icon } from '../common/Icon'

interface IPlanSummaryCardProps {
  plan: IPricingPlan
}

const MAX_FEATURES = 3

export function PlanSummaryCard({ plan }: IPlanSummaryCardProps) {
  const [isExpanded, setIsExpanded] = useState(false)

  return (
    <div className="plan-summary-card card border-0 shadow-sm">
      <div
        className="card-body plan-summary-card__strip"
        role="button"
        tabIndex={0}
        aria-expanded={isExpanded}
        aria-controls="plan-summary-features"
        onClick={() => setIsExpanded((expanded) => !expanded)}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault()
            setIsExpanded((expanded) => !expanded)
          }
        }}
      >
        <div className="plan-summary-card__header">
          <div>
            <p className="text-eyebrow mb-1">Selected plan</p>
            <p className="h5 mb-1">{plan.name}</p>
            <p className="text-secondary small mb-0">{plan.description}</p>
          </div>
          <Icon
            name="chevron-down"
            size={18}
            className={`plan-summary-card__chevron ${isExpanded ? 'plan-summary-card__chevron--open' : ''}`}
          />
        </div>

        <Link to="/#pricing" className="plan-summary-card__change-link" onClick={(event) => event.stopPropagation()}>
          Change plan
        </Link>

        <ul
          id="plan-summary-features"
          className={`plan-summary-card__features list-unstyled d-flex flex-column gap-2 mb-0 ${isExpanded ? 'plan-summary-card__features--expanded' : ''}`}
        >
          {plan.features.slice(0, MAX_FEATURES).map((feature) => (
            <li key={feature} className="d-flex align-items-center gap-2">
              <Icon name="check-circle" size={16} />
              {feature}
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}
