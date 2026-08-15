import { publicClient } from '../api/clients/publicClient'
import type { IPublicBookingStatus } from '../interfaces/IPublicBookingStatus'
import type { ICancellableBooking } from '../interfaces/publicBooking/ICancellableBooking'
import type { IPublicBranch } from '../interfaces/publicBooking/IPublicBranch'
import type { IPublicService } from '../interfaces/publicBooking/IPublicService'
import type { IPublicStaff } from '../interfaces/publicBooking/IPublicStaff'
import type { IAvailableSlotsResult } from '../interfaces/publicBooking/IPublicSlot'
import type { IInitiateBookingValues } from '../interfaces/publicBooking/IInitiateBookingValues'
import type { IBookingInitiationResult } from '../interfaces/publicBooking/IBookingInitiationResult'

// Wire shapes match their interfaces field-for-field (camelCase JSON naming policy), so no
// mapper is needed anywhere in this file.

export async function getPublicBranches(slug: string): Promise<IPublicBranch[]> {
  const response = await publicClient.get<IPublicBranch[]>(`/api/public/${slug}/bookings/branches`)
  return response.data
}

export async function getPublicServices(slug: string, branchId: string): Promise<IPublicService[]> {
  const response = await publicClient.get<IPublicService[]>(`/api/public/${slug}/bookings/branches/${branchId}/services`)
  return response.data
}

export async function getPublicStaff(slug: string, branchId: string, serviceId: string): Promise<IPublicStaff[]> {
  const response = await publicClient.get<IPublicStaff[]>(
    `/api/public/${slug}/bookings/branches/${branchId}/services/${serviceId}/staff`,
  )
  return response.data
}

export async function getPublicAvailability(
  slug: string,
  branchId: string,
  staffId: string,
  serviceId: string,
  date: string,
): Promise<IAvailableSlotsResult> {
  const response = await publicClient.get<IAvailableSlotsResult>(`/api/public/${slug}/bookings/branches/${branchId}/availability`, {
    params: { staffId, serviceId, date },
  })
  return response.data
}

export async function initiateBooking(slug: string, values: IInitiateBookingValues): Promise<IBookingInitiationResult> {
  const response = await publicClient.post<IBookingInitiationResult>(`/api/public/${slug}/bookings/initiate`, values)
  return response.data
}

// Un-prefixed by design — GetBookingStatusByTicketHandler resolves the tenant from the signed
// ticket token itself, not from the URL's slug.
export async function getBookingStatus(ticketToken: string): Promise<IPublicBookingStatus> {
  const response = await publicClient.get<IPublicBookingStatus>(`/api/public/bookings/status/${ticketToken}`)
  return response.data
}

export interface IPublicTheme {
  themePaletteId: string
  isDarkMode: boolean
}

// Deliberately separate from the branches/services/staff step calls — this exists only to
// paint the page's colors correctly, fetched once at wizard mount alongside (not blocking)
// the rest of the flow.
export async function getPublicTheme(slug: string): Promise<IPublicTheme> {
  const response = await publicClient.get<IPublicTheme>(`/api/public/${slug}/theme`)
  return response.data
}

// Un-prefixed by design, same as getBookingStatus above — the cancellation token resolves
// its own tenant, and it's a wholly separate credential from the ticket token, never
// interchangeable with it.
export async function getCancellableBooking(token: string): Promise<ICancellableBooking> {
  const response = await publicClient.get<ICancellableBooking>(`/api/public/bookings/cancel/${token}`)
  return response.data
}

export interface ICancelBookingEwalletDetails {
  provider: string
  number: string
  name: string
}

export async function cancelBookingByToken(
  token: string,
  reason: string | null,
  ewallet: ICancelBookingEwalletDetails | null,
): Promise<void> {
  await publicClient.post('/api/public/bookings/cancel', {
    token,
    reason,
    ewalletProvider: ewallet?.provider ?? null,
    ewalletNumber: ewallet?.number ?? null,
    ewalletName: ewallet?.name ?? null,
  })
}
