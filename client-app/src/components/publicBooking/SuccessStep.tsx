import { formatDisplayDate } from '../../utils/formatDateTime'
import { formatMoney } from '../../utils/formatMoney'
import { buildDirectionsUrl } from '../../utils/publicBookingActions'
import { Icon } from '../common/Icon'
import type { IPublicBranch } from '../../interfaces/publicBooking/IPublicBranch'
import type { IPublicService } from '../../interfaces/publicBooking/IPublicService'
import type { IPublicStaff } from '../../interfaces/publicBooking/IPublicStaff'
import type { IPublicSlot } from '../../interfaces/publicBooking/IPublicSlot'
import type { IBookingInitiationResult } from '../../interfaces/publicBooking/IBookingInitiationResult'
import type { WizardDirection } from '../../hooks/usePublicBookingWizard'

interface ISuccessStepProps {
  result: IBookingInitiationResult
  branch: IPublicBranch
  service: IPublicService
  staffMember: IPublicStaff
  date: string
  slot: IPublicSlot
  direction: WizardDirection
}

export function SuccessStep({ result, branch, service, staffMember, date, slot, direction }: ISuccessStepProps) {
  return (
    <div className={`pb-step-enter-${direction} text-center`}>
      <svg className="pb-confirm-check mb-3" width="72" height="72" viewBox="0 0 72 72" fill="none" aria-hidden="true">
        <circle className="pb-confirm-check-circle" cx="36" cy="36" r="33" />
        <path className="pb-confirm-check-mark" d="M21 37l10.5 10.5L51 26" />
      </svg>
      <p className="pb-display fs-5 mb-4">Booking confirmed</p>

      <div className="pb-ticket text-start">
        <div className="p-4">
          <div className="fw-semibold fs-3 mb-1">{service.name}</div>
          <div className="pb-muted mb-3">
            with {staffMember.fullName} · {branch.branchName}
          </div>

          <div className="d-flex justify-content-between pb-mono fs-5 fw-semibold">
            <span>{formatDisplayDate(date)}</span>
            <span>{slot.timeString}</span>
          </div>
        </div>

        <div className="pb-ticket-divider mx-4" />

        <div className="p-4">
          <div className="pb-muted small text-uppercase mb-1" style={{ letterSpacing: '0.06em' }}>
            Booking reference
          </div>
          <div className="pb-mono pb-muted fw-semibold">{result.bookingReference}</div>
        </div>

        <div className="pb-ticket-divider mx-4" />

        <div className="p-4 text-center">
          <img src={result.ticketQrCodeDataUri} alt="Boarding pass QR code" width={160} height={160} />
          <p className="pb-muted small mt-2 mb-0">
            Show this at check-in — or just give your name, our staff can look you up too.
          </p>
        </div>

        <div className="pb-ticket-divider mx-4" />

        {!result.requiresPayment && result.amountToPay > 0 && (
          <div className="p-4 pb-muted small border-top">
            Pay {formatMoney(result.amountToPay, service.currencyCode)} at your visit.
          </div>
        )}

        <div className="p-4 pb-muted small">
          You'll receive a confirmation email shortly. Please arrive a few minutes early.
        </div>
      </div>

      <div className="d-flex flex-column flex-sm-row gap-2 mt-3">
        <button
          type="button"
          className="btn pb-btn-outline flex-fill d-inline-flex align-items-center justify-content-center gap-2"
          onClick={() => window.print()}
        >
          <Icon name="download" size={16} />
          Download Receipt
        </button>
        <a
          href={buildDirectionsUrl(branch)}
          target="_blank"
          rel="noopener noreferrer"
          className="btn pb-btn-outline flex-fill d-inline-flex align-items-center justify-content-center gap-2"
        >
          <Icon name="branches" size={16} />
          Get Directions
        </a>
      </div>
    </div>
  )
}
