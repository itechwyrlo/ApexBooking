import { Fragment } from 'react'
import { Badge } from '../common/Badge'
import { formatDisplayDateTime } from '../../utils/formatDateTime'
import { getBookingDisplayStatus, BOOKING_DISPLAY_STATUS_BADGE_TONE, BOOKING_DISPLAY_STATUS_LABEL } from '../../utils/bookingDisplayStatus'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface IStep {
  label: string
  isDone: boolean
  isCurrent: boolean
  showCheckmark: boolean
  timestamp: string | null
}

interface IBookingStatusStepperProps {
  booking: ITenantBooking
}

export function BookingStatusStepper({ booking }: IBookingStatusStepperProps) {
  const displayStatus = getBookingDisplayStatus(booking)

  // Cancelled / no-show / awaiting-payment are distinct branch or pre-states,
  // never squeezed into the linear Scheduled -> Admitted -> Completed line.
  if (displayStatus === 'cancelled') {
    return (
      <div>
        <Badge tone={BOOKING_DISPLAY_STATUS_BADGE_TONE.cancelled}>{BOOKING_DISPLAY_STATUS_LABEL.cancelled}</Badge>
        {booking.cancelledAt && <div className="text-muted small mt-2">{formatDisplayDateTime(booking.cancelledAt)}</div>}
        {booking.cancellationReason && <div className="small mt-2">Reason: {booking.cancellationReason}</div>}
      </div>
    )
  }

  if (displayStatus === 'noShow') {
    return (
      <div>
        <Badge tone={BOOKING_DISPLAY_STATUS_BADGE_TONE.noShow}>{BOOKING_DISPLAY_STATUS_LABEL.noShow}</Badge>
        {booking.noShowAt && <div className="text-muted small mt-2">{formatDisplayDateTime(booking.noShowAt)}</div>}
      </div>
    )
  }

  if (displayStatus === 'awaitingPayment') {
    return (
      <div>
        <Badge tone={BOOKING_DISPLAY_STATUS_BADGE_TONE.awaitingPayment}>{BOOKING_DISPLAY_STATUS_LABEL.awaitingPayment}</Badge>
        <div className="text-muted small mt-2">Booked {formatDisplayDateTime(booking.createdAt)}</div>
      </div>
    )
  }

  const isAdmitted = displayStatus === 'admitted' || displayStatus === 'completed'
  const isCompleted = displayStatus === 'completed'

  const steps: IStep[] = [
    {
      label: 'Scheduled',
      isDone: true,
      isCurrent: displayStatus === 'scheduled',
      showCheckmark: displayStatus !== 'scheduled',
      timestamp: booking.createdAt,
    },
    {
      label: 'Admitted',
      isDone: isAdmitted,
      isCurrent: displayStatus === 'admitted',
      showCheckmark: isCompleted,
      timestamp: booking.checkedInAt,
    },
    { label: 'Completed', isDone: isCompleted, isCurrent: isCompleted, showCheckmark: isCompleted, timestamp: booking.serviceCompletedAt },
  ]

  return (
    <div className="booking-stepper">
      {steps.map((step, index) => {
        const isLast = index === steps.length - 1

        return (
          <Fragment key={step.label}>
            <div className="booking-stepper-item">
              <span className={`booking-stepper-circle ${step.isDone ? 'is-done' : ''} ${step.isCurrent ? 'is-current' : ''}`.trim()}>
                {step.showCheckmark ? '✓' : index + 1}
              </span>
              <span className={`booking-stepper-label ${step.isCurrent ? 'is-current' : ''}`.trim()}>{step.label}</span>
              {step.timestamp && <span className="booking-stepper-timestamp">{formatDisplayDateTime(step.timestamp)}</span>}
            </div>
            {!isLast && <span className={`booking-stepper-bar ${steps[index + 1].isDone ? 'is-done' : ''}`.trim()} aria-hidden="true" />}
          </Fragment>
        )
      })}
    </div>
  )
}
