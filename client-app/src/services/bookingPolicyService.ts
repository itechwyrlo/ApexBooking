import { authClient } from '../api/clients/authClient'
import type { IBookingPolicy } from '../interfaces/IBookingPolicy'

// Wire shape from ApexBooking.Core.Application.Features.Tenancy.Queries.GetBookingPolicy.BookingPolicyDto
// matches IBookingPolicy field-for-field (camelCase JSON naming policy), so no mapper is needed.

export async function getBookingPolicy(): Promise<IBookingPolicy> {
  const response = await authClient.get<IBookingPolicy>('/api/Tenant/policy/booking')
  return response.data
}

export async function updateBookingPolicy(values: IBookingPolicy): Promise<void> {
  await authClient.put('/api/Tenant/policy/booking', values)
}
