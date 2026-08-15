import { Button } from '../common/Button'
import { Badge, type BadgeTone } from '../common/Badge'
import { BookingStatusStepper } from './BookingStatusStepper'
import { PaymentConfirmationMethod } from '../../types/PaymentConfirmationMethod'
import { BookingStatus } from '../../types/BookingStatus'
import { formatDisplayDate, formatDisplayTime } from '../../utils/formatDateTime'
import { getBookingDisplayStatus, BOOKING_DISPLAY_STATUS_BADGE_TONE, BOOKING_DISPLAY_STATUS_LABEL } from '../../utils/bookingDisplayStatus'
import { useAuth } from '../../hooks/useAuth'
import { Role } from '../../types/Role'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

function formatAmount(amount: number, currencyCode: string): string {
  return new Intl.NumberFormat(undefined, { style: 'currency', currency: currencyCode }).format(amount)
}

interface IPaymentSummary {
  label: string
  tone: BadgeTone
  detail?: string
}

function getPaymentSummary(booking: ITenantBooking): IPaymentSummary {
  if (booking.paymentConfirmedVia === PaymentConfirmationMethod.Online && booking.remainingBalance > 0) {
    return { label: 'Deposit paid online', tone: 'warning', detail: `${formatAmount(booking.remainingBalance, booking.currencyCode)} left to pay` }
  }

  if (booking.paymentConfirmedVia === PaymentConfirmationMethod.Online) {
    return { label: 'Paid online', tone: 'success', detail: formatAmount(booking.amountPaidOnline, booking.currencyCode) }
  }

  if (booking.paymentConfirmedVia === PaymentConfirmationMethod.PayInVisit) {
    return { label: 'Paid in visit', tone: 'success', detail: formatAmount(booking.amountDue, booking.currencyCode) }
  }

  if (booking.status === BookingStatus.PendingPayment) {
    return { label: 'Awaiting online payment', tone: 'warning', detail: formatAmount(booking.amountDue, booking.currencyCode) }
  }

  return { label: 'Pay in visit', tone: 'primary', detail: formatAmount(booking.remainingBalance, booking.currencyCode) }
}

interface IBookingDetailPanelProps {
  booking: ITenantBooking
  onBack?: () => void
  pendingBookingId: string | null
  onAdmit: (booking: ITenantBooking) => void
  onComplete: (booking: ITenantBooking) => void
  onNoShow: (booking: ITenantBooking) => void
  onCancel: (booking: ITenantBooking) => void
}

export function BookingDetailPanel({ booking, onBack, pendingBookingId, onAdmit, onComplete, onNoShow, onCancel }: IBookingDetailPanelProps) {
  const { user } = useAuth()
  const canManage = (user?.roles ?? []).some((role) => role === Role.Owner || role === Role.Admin)
  const payment = getPaymentSummary(booking)
  const displayStatus = getBookingDisplayStatus(booking)
  const isPending = pendingBookingId === booking.bookingId
  const isScheduled = booking.status === BookingStatus.Scheduled
  const isCheckedIn = booking.checkedInAt !== null

  return (
    <div>
      {onBack && (
        <button type="button" className="btn btn-link p-0 mb-3 text-decoration-none" onClick={onBack}>
          ← Back to day
        </button>
      )}

      <div className="d-flex align-items-start justify-content-between mb-3">
        <div>
          <div className="fw-bold">{booking.serviceName}</div>
          <div className="text-muted small">{booking.bookingReference}</div>
        </div>
        <Badge tone={BOOKING_DISPLAY_STATUS_BADGE_TONE[displayStatus]}>{BOOKING_DISPLAY_STATUS_LABEL[displayStatus]}</Badge>
      </div>

      <div className="border rounded p-3 mb-3">
        <BookingStatusStepper booking={booking} />
      </div>

      <dl className="row small mb-3 gy-1">
        <dt className="col-4 text-muted fw-normal">Date</dt>
        <dd className="col-8 mb-0">{formatDisplayDate(booking.scheduledDate)}</dd>

        <dt className="col-4 text-muted fw-normal">Time</dt>
        <dd className="col-8 mb-0">
          {formatDisplayTime(booking.scheduledStartTime)} · {booking.durationMinutes} min
        </dd>

        <dt className="col-4 text-muted fw-normal">Team Member</dt>
        <dd className="col-8 mb-0">{booking.staffName}</dd>

        <dt className="col-4 text-muted fw-normal">Branch</dt>
        <dd className="col-8 mb-0">{booking.branchName}</dd>

        <dt className="col-4 text-muted fw-normal">Customer</dt>
        <dd className="col-8 mb-0">
          <div>{booking.customerName}</div>
          {booking.customerPhone && <div className="text-muted">{booking.customerPhone}</div>}
        </dd>

        <dt className="col-4 text-muted fw-normal">Payment</dt>
        <dd className="col-8 mb-0">
          <Badge tone={payment.tone}>{payment.label}</Badge>
          {payment.detail && <div className="text-muted mt-1">{payment.detail}</div>}
        </dd>
      </dl>

      {isScheduled && (
        <div className="d-flex flex-wrap gap-2 pt-2 border-top">
          {!isCheckedIn && (
            <Button size="sm" variant="outline-primary" isLoading={isPending} onClick={() => onAdmit(booking)}>
              Mark as Admitted
            </Button>
          )}
          {isCheckedIn && canManage && (
            <Button size="sm" variant="outline-primary" isLoading={isPending} onClick={() => onComplete(booking)}>
              Mark as Completed
            </Button>
          )}
          {!isCheckedIn && canManage && (
            <Button size="sm" variant="outline-secondary" disabled={isPending} onClick={() => onNoShow(booking)}>
              Mark as No-show
            </Button>
          )}
          <Button size="sm" variant="outline-secondary" disabled={isPending} onClick={() => onCancel(booking)}>
            Cancel
          </Button>
        </div>
      )}
    </div>
  )
}
