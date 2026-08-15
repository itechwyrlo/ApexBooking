import type { RefundRequestStatus } from '../types/RefundRequestStatus'

// Mirrors ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests.RefundRequestSummaryDto
export interface IRefundRequest {
  id: string
  bookingId: string
  bookingReference: string
  customerName: string
  requestedAmount: number
  amountPaid: number
  payMongoPaymentId: string | null
  currencyCode: string
  status: RefundRequestStatus
  rejectionReason: string | null
  customerEwalletProvider: string
  customerEwalletNumber: string
  customerEwalletName: string
  receiptUrl: string | null
  createdAt: string
  dueDate: string
}
