import { Badge, type BadgeTone } from '../common/Badge'
import { formatStatusLabel } from '../../utils/statusLabel'
import { BookingStatus } from '../../types/BookingStatus'

interface IBookingStatusBadgeProps {
  status: BookingStatus
}

// Canonical BookingStatus -> tone mapping. A second, conflicting copy of this mapping previously
// lived inline in CustomerBookingsModal.tsx, disagreeing on two of five values (NoShow and
// Cancelled were swapped) — this is the mapping kept, since it's the one two other real call
// sites already relied on (StaffLineupTimeline, BookingTable) versus the other's one.
const STATUS_TONE: Record<BookingStatus, BadgeTone> = {
  [BookingStatus.PendingPayment]: 'warning',
  [BookingStatus.Scheduled]: 'primary',
  [BookingStatus.Completed]: 'success',
  [BookingStatus.NoShow]: 'neutral',
  [BookingStatus.Cancelled]: 'danger',
}

export function BookingStatusBadge({ status }: IBookingStatusBadgeProps) {
  return <Badge tone={STATUS_TONE[status]}>{formatStatusLabel(status)}</Badge>
}
