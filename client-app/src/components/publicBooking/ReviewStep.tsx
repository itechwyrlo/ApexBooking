import { formatMoney } from '../../utils/formatMoney'
import { formatDisplayDate } from '../../utils/formatDateTime'
import type { WizardStep, WizardDirection } from '../../hooks/usePublicBookingWizard'
import type { IPublicBranch } from '../../interfaces/publicBooking/IPublicBranch'
import type { IPublicService } from '../../interfaces/publicBooking/IPublicService'
import type { IPublicStaff } from '../../interfaces/publicBooking/IPublicStaff'
import type { IPublicSlot } from '../../interfaces/publicBooking/IPublicSlot'
import type { IBookingContactValues } from '../../interfaces/publicBooking/IInitiateBookingValues'

interface IReviewStepProps {
  branch: IPublicBranch
  service: IPublicService
  staffMember: IPublicStaff
  date: string
  slot: IPublicSlot
  contact: IBookingContactValues
  branchStepSkipped: boolean
  staffStepSkipped: boolean
  isSubmitting: boolean
  submitError: string | null
  direction: WizardDirection
  onEdit: (step: WizardStep) => void
  onSubmit: () => void
}

function EditLink({ onClick }: { onClick: () => void }) {
  return (
    <button type="button" className="pb-btn-link" onClick={onClick}>
      Edit
    </button>
  )
}

export function ReviewStep({
  branch,
  service,
  staffMember,
  date,
  slot,
  contact,
  branchStepSkipped,
  staffStepSkipped,
  isSubmitting,
  submitError,
  direction,
  onEdit,
  onSubmit,
}: IReviewStepProps) {
  return (
    <div className={`pb-step-enter-${direction}`}>
      <h1 className="pb-display fs-3 mb-1">Review your booking</h1>
      <p className="pb-muted mb-4">Double check the details below, then confirm.</p>

      <div className="pb-ticket p-4 mb-3">
        <div className="d-flex justify-content-between align-items-start mb-1">
          <div className="fw-semibold">{service.name}</div>
          <EditLink onClick={() => onEdit('service')} />
        </div>
        <div className="pb-muted small mb-3">
          with {staffMember.fullName} · {branch.branchName}
        </div>

        <div className="d-flex justify-content-between pb-mono small">
          <span>{formatDisplayDate(date)}</span>
          <span>{slot.timeString}</span>
        </div>
        <div className="d-flex justify-content-between pb-mono small mt-1 mb-2">
          <span>{service.durationMinutes} min</span>
          <span>{formatMoney(service.price, service.currencyCode)}</span>
        </div>
        <div className="d-flex justify-content-end">
          <EditLink onClick={() => onEdit('schedule')} />
        </div>

        {!branchStepSkipped && (
          <div className="d-flex justify-content-between align-items-center pt-2 mt-2 pb-ticket-divider">
            <span className="pb-muted small">Location</span>
            <EditLink onClick={() => onEdit('branch')} />
          </div>
        )}

        {!staffStepSkipped && (
          <div className="d-flex justify-content-between align-items-center pt-2 mt-2 pb-ticket-divider">
            <span className="pb-muted small">Team member</span>
            <EditLink onClick={() => onEdit('staff')} />
          </div>
        )}
      </div>

      <div className="pb-ticket p-4 mb-4">
        <div className="d-flex justify-content-between align-items-start mb-2">
          <span className="fw-semibold small text-uppercase pb-muted" style={{ letterSpacing: '0.04em' }}>
            Your Information
          </span>
          <EditLink onClick={() => onEdit('customerInfo')} />
        </div>
        <div className="fw-semibold">
          {contact.customerFirstName} {contact.customerLastName}
        </div>
        <div className="pb-muted small">{contact.customerEmail}</div>
        <div className="pb-muted small">{contact.customerPhone}</div>
        {contact.customerNotes && <div className="pb-muted small mt-2">"{contact.customerNotes}"</div>}
      </div>

      {submitError && <div className="pb-alert-danger mb-3">{submitError}</div>}

      <button type="button" className="btn pb-btn-primary w-100" disabled={isSubmitting} onClick={onSubmit}>
        {isSubmitting && <span className="spinner-border spinner-border-sm me-2" aria-hidden="true" />}
        {isSubmitting ? 'Booking...' : 'Confirm Booking'}
      </button>
    </div>
  )
}
