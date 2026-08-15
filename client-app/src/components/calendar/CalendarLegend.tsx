import type { BookingDisplayStatus } from '../../utils/bookingDisplayStatus'
import { BOOKING_DISPLAY_STATUS_LABEL } from '../../utils/bookingDisplayStatus'

const LEGEND_STATUSES: BookingDisplayStatus[] = ['scheduled', 'admitted', 'completed', 'noShow', 'cancelled', 'awaitingPayment']

const SWATCH_COLOR: Record<BookingDisplayStatus, string> = {
  scheduled: 'var(--color-primary)',
  admitted: 'var(--color-accent)',
  completed: 'var(--color-success)',
  noShow: 'var(--color-muted)',
  cancelled: 'var(--color-danger)',
  awaitingPayment: 'var(--color-teal)',
}

export function CalendarLegend() {
  return (
    <div className="d-flex flex-wrap align-items-center gap-3 small text-muted mt-2">
      {LEGEND_STATUSES.map((status) => (
        <span key={status} className="d-inline-flex align-items-center gap-1">
          <span
            aria-hidden="true"
            style={{ width: 8, height: 8, borderRadius: '50%', backgroundColor: SWATCH_COLOR[status], display: 'inline-block' }}
          />
          {BOOKING_DISPLAY_STATUS_LABEL[status]}
        </span>
      ))}
      <span className="d-inline-flex align-items-center gap-1">
        <span aria-hidden="true" style={{ width: 6, height: 6, borderRadius: '50%', backgroundColor: 'currentColor', display: 'inline-block' }} />
        Paid
      </span>
      <span className="d-inline-flex align-items-center gap-1">
        <span
          aria-hidden="true"
          style={{ width: 6, height: 6, borderRadius: '50%', border: '1px solid currentColor', display: 'inline-block' }}
        />
        Payment pending
      </span>
    </div>
  )
}
