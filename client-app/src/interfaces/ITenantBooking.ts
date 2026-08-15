import type { BookingStatus } from '../types/BookingStatus'
import type { PaymentConfirmationMethod } from '../types/PaymentConfirmationMethod'

// Mirrors ApexBooking.Core.Application.Dtos.Response.TenantBookingSummary
export interface ITenantBooking {
  bookingId: string
  customerId: string
  staffId: string
  bookingReference: string
  customerName: string
  customerPhone: string | null
  serviceName: string
  staffName: string
  branchName: string
  scheduledDate: string
  scheduledStartTime: string
  durationMinutes: number
  status: BookingStatus
  requiresUpfrontPayment: boolean
  amountDue: number
  currencyCode: string
  paymentConfirmedVia: PaymentConfirmationMethod | null
  checkedInAt: string | null
  serviceCompletedAt: string | null
  cancelledAt: string | null
  cancellationReason: string | null
  noShowAt: string | null
  amountPaidOnline: number
  remainingBalance: number
  createdAt: string
}

export interface ITenantBookingFilters {
  branchId?: string
  staffId?: string
  status?: BookingStatus
  fromDate?: string
  toDate?: string
}

// Mirrors ApexBooking.Core.Application.Dtos.Response.ScanArrivalResult
export interface IBookingArrival {
  bookingId: string
  bookingReference: string
  status: string
  checkedInAt: string
  wasFirstAdmission: boolean
}

export interface IScheduleBookingValues {
  branchId: string
  staffId: string
  serviceId: string
  customerFirstName: string
  customerLastName: string
  customerEmail: string
  customerPhone: string
  scheduledDate: string
  scheduledStartTime: string
  customerNotes: string
  admitImmediately: boolean
}
