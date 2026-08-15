import type { BookingConfirmationMode } from '../types/BookingConfirmationMode'
import type { CancellationPolicy } from '../types/CancellationPolicy'

// Mirrors ApexBooking.Core.Application.Features.Tenancy.Queries.GetBookingPolicy.BookingPolicyDto
export interface IBookingPolicy {
  bookingConfirmationMode: BookingConfirmationMode
  minAdvanceBookingHours: number
  maxAdvanceBookingDays: number
  cancellationCutoffHours: number
  lateCancellationPolicy: CancellationPolicy
  notifyBookingConfirmed: boolean
  notifyBookingCancelled: boolean
  notifyBookingReminder: boolean
  notifyNewCustomer: boolean
  reminderHoursBefore: number
}
