import { Badge } from '../common/Badge'
import { formatDisplayTime } from '../../utils/formatDateTime'
import { getBookingDisplayStatus, BOOKING_DISPLAY_STATUS_BADGE_TONE, BOOKING_DISPLAY_STATUS_LABEL } from '../../utils/bookingDisplayStatus'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface IBookingDayListProps {
  bookings: ITenantBooking[]
  onSelectBooking: (booking: ITenantBooking) => void
}

export function BookingDayList({ bookings, onSelectBooking }: IBookingDayListProps) {
  return (
    <div className="d-flex flex-column gap-2">
      {bookings.map((booking) => {
        const displayStatus = getBookingDisplayStatus(booking)

        return (
          <button
            key={booking.bookingId}
            type="button"
            className="btn text-start border rounded p-2 d-flex align-items-center justify-content-between gap-2"
            onClick={() => onSelectBooking(booking)}
          >
            <div style={{ minWidth: 0 }}>
              <div className="fw-semibold text-truncate">{booking.customerName}</div>
              <div className="text-muted small text-truncate">
                {formatDisplayTime(booking.scheduledStartTime)} · {booking.serviceName}
              </div>
            </div>
            <Badge tone={BOOKING_DISPLAY_STATUS_BADGE_TONE[displayStatus]} className="flex-shrink-0">
              {BOOKING_DISPLAY_STATUS_LABEL[displayStatus]}
            </Badge>
          </button>
        )
      })}
    </div>
  )
}
