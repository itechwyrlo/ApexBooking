import { publicClient } from '../api/clients/publicClient'
import type { IRefundStatus } from '../interfaces/IRefundStatus'

// Un-prefixed by design, same as getCancellableBooking in publicBookingService.ts — the token
// resolves its own tenant, not the URL's slug.

export async function getRefundStatus(token: string): Promise<IRefundStatus> {
  const response = await publicClient.get<IRefundStatus>(`/api/public/refund-status/${token}`)
  return response.data
}
