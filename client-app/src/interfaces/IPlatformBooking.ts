import type { BookingStatus } from '../types/BookingStatus'

// Mirrors ApexBooking.Core.Application.Dtos.Response.PlatformBookingSummary
export interface IPlatformBooking {
  bookingId: string
  bookingReference: string
  tenantId: string
  tenantName: string
  customerName: string
  staffName: string
  serviceName: string
  scheduledDate: string
  scheduledStartTime: string
  status: BookingStatus
}

export interface IPlatformBookingFilters {
  tenantId?: string
  status?: BookingStatus
  fromDate?: string
  toDate?: string
}
