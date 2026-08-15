import type { BookingStatus } from '../types/BookingStatus'
import type { PaymentConfirmationMethod } from '../types/PaymentConfirmationMethod'

// Mirrors ApexBooking.Core.Application.Dtos.Response.CustomerBookingSummary
export interface ICustomerBooking {
  bookingId: string
  bookingReference: string
  serviceName: string
  staffName: string
  branchName: string
  scheduledDate: string
  scheduledStartTime: string
  status: BookingStatus
  requiresUpfrontPayment: boolean
  amountDue: number
  currencyCode: string
  paymentConfirmedVia: PaymentConfirmationMethod | null
  createdAt: string
}
