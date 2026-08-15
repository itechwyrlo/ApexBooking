import type { RefundRequestStatus } from '../types/RefundRequestStatus'

// Mirrors ApexBooking.Core.Application.Features.RefundRequests.Queries.GetRefundLog.RefundLogEntryDto
export interface IRefundLogEntry {
  id: string
  bookingReference: string
  amount: number
  currencyCode: string
  status: RefundRequestStatus
  processedAt: string
}
