import type { RefundRequestStatus } from '../types/RefundRequestStatus'

// Mirrors ApexBooking.Core.Application.Features.PublicBookings.Queries.GetRefundStatus.RefundStatusDto
export interface IRefundStatus {
  bookingReference: string
  status: RefundRequestStatus | null
  amount: number | null
  currencyCode: string
  businessContactPhoneNumber: string | null
  receiptUrl: string | null
}
