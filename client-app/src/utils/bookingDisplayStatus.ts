import type { BadgeTone } from '../components/common/Badge'
import { BookingStatus } from '../types/BookingStatus'
import type { ITenantBooking } from '../interfaces/ITenantBooking'

// "Admitted" isn't its own BookingStatus value on the wire — a booking stays
// Scheduled and gains a non-null checkedInAt. This derives the single display
// state the chip, legend, and stepper all key off, so they can't drift apart.
export type BookingDisplayStatus = 'awaitingPayment' | 'scheduled' | 'admitted' | 'completed' | 'noShow' | 'cancelled'

export function getBookingDisplayStatus(booking: ITenantBooking): BookingDisplayStatus {
  if (booking.status === BookingStatus.Cancelled) return 'cancelled'
  if (booking.status === BookingStatus.NoShow) return 'noShow'
  if (booking.status === BookingStatus.Completed) return 'completed'
  if (booking.status === BookingStatus.PendingPayment) return 'awaitingPayment'
  return booking.checkedInAt ? 'admitted' : 'scheduled'
}

export const BOOKING_DISPLAY_STATUS_LABEL: Record<BookingDisplayStatus, string> = {
  awaitingPayment: 'Awaiting Payment',
  scheduled: 'Scheduled',
  admitted: 'Admitted',
  completed: 'Completed',
  noShow: 'No-show',
  cancelled: 'Cancelled',
}

export const BOOKING_DISPLAY_STATUS_BADGE_TONE: Record<BookingDisplayStatus, BadgeTone> = {
  awaitingPayment: 'teal',
  scheduled: 'primary',
  admitted: 'warning',
  completed: 'success',
  noShow: 'neutral',
  cancelled: 'danger',
}

// Calendar event chips reuse the same badge-tone-* background/color pairs as
// the shared Badge component (just applied directly, since chips have their
// own compact pill layout) so the palette can't drift between the two.
export const BOOKING_DISPLAY_STATUS_CHIP_CLASS: Record<BookingDisplayStatus, string> = {
  awaitingPayment: 'badge-tone-teal',
  scheduled: 'badge-tone-primary',
  admitted: 'badge-tone-warning',
  completed: 'badge-tone-success',
  noShow: 'badge-tone-neutral',
  cancelled: 'badge-tone-danger',
}

export function isBookingPaid(booking: ITenantBooking): boolean {
  return booking.paymentConfirmedVia !== null
}
