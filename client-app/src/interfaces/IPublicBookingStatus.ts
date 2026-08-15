import type { BookingStatus } from '../types/BookingStatus'

// Mirrors ApexBooking.Core.Application.Features.PublicBookings.Queries.GetBookingStatusByTicket.PublicBookingStatusDto
export interface IPublicBookingStatus {
  bookingId: string
  bookingReference: string
  status: BookingStatus
  serviceName: string
  staffName: string
  branchName: string
  scheduledDate: string
  scheduledStartTime: string
  requiresUpfrontPayment: boolean
  amountDue: number
  currencyCode: string
}
