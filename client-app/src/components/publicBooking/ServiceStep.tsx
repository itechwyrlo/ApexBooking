import { formatMoney } from '../../utils/formatMoney'
import type { IPublicService } from '../../interfaces/publicBooking/IPublicService'
import type { WizardDirection } from '../../hooks/usePublicBookingWizard'
import { Icon } from '../common/Icon'

interface IServiceStepProps {
  services: IPublicService[]
  isLoading: boolean
  selectedServiceId: string | null
  direction: WizardDirection
  onSelect: (service: IPublicService) => void
}

export function ServiceStep({ services, isLoading, selectedServiceId, direction, onSelect }: IServiceStepProps) {
  return (
    <div className={`pb-step-enter-${direction}`}>
      <h1 className="pb-display fs-3 mb-1">What can we help with?</h1>
      <p className="pb-muted mb-4">Pick a service to continue.</p>

      <div aria-live="polite">
        {isLoading ? (
          <p className="pb-muted">Loading services...</p>
        ) : services.length === 0 ? (
          <p className="pb-muted">No services are currently available at this location.</p>
        ) : (
          <div className="d-flex flex-column gap-2">
            {services.map((service) => {
              const isSelected = service.serviceId === selectedServiceId
              return (
                <button
                  key={service.serviceId}
                  type="button"
                  className={`pb-option ${isSelected ? 'is-selected' : ''}`.trim()}
                  onClick={() => onSelect(service)}
                >
                  {isSelected && (
                    <span className="pb-option-check" aria-hidden="true">
                      ✓
                    </span>
                  )}
                  <div className="d-flex align-items-start gap-3">
                    <span className="pb-option-icon" aria-hidden="true">
                      <Icon name="services" size={20} />
                    </span>
                    <div className="d-flex justify-content-between align-items-start gap-3 flex-grow-1">
                      <div>
                        <div className="fw-semibold">{service.name}</div>
                        {service.description && <div className="pb-muted small">{service.description}</div>}
                        <div className="pb-muted small mt-1">{service.durationMinutes} min</div>
                      </div>
                      <div className="pb-option-price flex-shrink-0">{formatMoney(service.price, service.currencyCode)}</div>
                    </div>
                  </div>
                </button>
              )
            })}
          </div>
        )}
      </div>
    </div>
  )
}
