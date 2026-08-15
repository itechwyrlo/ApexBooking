import { EmptyState } from '../common/EmptyState'
import { BookingStatusBadge } from '../admin/BookingStatusBadge'
import { formatDisplayTime } from '../../utils/formatDateTime'
import type { ITenantBooking } from '../../interfaces/ITenantBooking'

interface IStaffLineupTimelineProps {
  bookings: ITenantBooking[]
  isLoading?: boolean
}

export function StaffLineupTimeline({ bookings, isLoading }: IStaffLineupTimelineProps) {
  if (isLoading) {
    return <p className="text-muted small mb-0">Loading your lineup…</p>
  }

  if (bookings.length === 0) {
    return (
      <EmptyState
        icon="clock"
        title="No appointments assigned to you today"
        description="A chronological list of just your appointments for today will appear here."
      />
    )
  }

  const sorted = [...bookings].sort((a, b) => a.scheduledStartTime.localeCompare(b.scheduledStartTime))

  return (
    <ul className="list-unstyled mb-0">
      {sorted.map((booking) => (
        <li key={booking.bookingId} className="d-flex gap-3 py-2 border-bottom">
          <div className="text-mono small text-muted" style={{ minWidth: 72 }}>
            {formatDisplayTime(booking.scheduledStartTime)}
          </div>
          <div className="flex-grow-1">
            <div className="fw-semibold">{booking.serviceName}</div>
            <div className="text-muted small">{booking.customerName}</div>
          </div>
          <div>
            <BookingStatusBadge status={booking.status} />
          </div>
        </li>
      ))}
    </ul>
  )
}
