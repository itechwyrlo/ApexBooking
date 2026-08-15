import { authClient } from '../api/clients/authClient'
import type { IPagedResult, IPageParams } from '../interfaces/IPagedResult'
import type { IBookingArrival, IScheduleBookingValues, ITenantBooking, ITenantBookingFilters } from '../interfaces/ITenantBooking'
import type { ICheckoutBookingResult, ICheckoutDetails } from '../interfaces/ICheckoutDetails'
import type { ITenantBookingCounts } from '../interfaces/ITenantBookingCounts'
import type { ITenantRevenue } from '../interfaces/ITenantRevenue'
import type { IReassignableStaffMember } from '../interfaces/IReassignableStaffMember'
import type { IWalkInStaffAvailability } from '../interfaces/IWalkInStaffAvailability'

// Wire shape from ApexBooking.Core.Application.Dtos.Response.TenantBookingSummary matches
// ITenantBooking field-for-field (camelCase JSON naming policy), so no mapper is needed.
export async function getTenantBookings(
  filters: ITenantBookingFilters,
  params: IPageParams = {},
): Promise<IPagedResult<ITenantBooking>> {
  const response = await authClient.get<IPagedResult<ITenantBooking>>('/api/Tenant/bookings', {
    params: {
      pageNumber: params.pageNumber ?? 1,
      pageSize: params.pageSize ?? 10,
      branchId: filters.branchId || undefined,
      staffId: filters.staffId || undefined,
      status: filters.status || undefined,
      fromDate: filters.fromDate || undefined,
      toDate: filters.toDate || undefined,
    },
  })
  return response.data
}

export async function getTenantBookingCounts(date: string): Promise<ITenantBookingCounts> {
  const response = await authClient.get<ITenantBookingCounts>('/api/Tenant/bookings/counts', { params: { date } })
  return response.data
}

export async function getTenantRevenue(fromDate: string, toDate: string): Promise<ITenantRevenue> {
  const response = await authClient.get<ITenantRevenue>('/api/Tenant/bookings/revenue', { params: { fromDate, toDate } })
  return response.data
}

export async function getWalkInAvailableStaff(branchId: string, serviceId: string): Promise<IWalkInStaffAvailability[]> {
  const response = await authClient.get<IWalkInStaffAvailability[]>('/api/Tenant/bookings/walk-in-staff', {
    params: { branchId, serviceId },
  })
  return response.data
}

export async function scheduleBooking(values: IScheduleBookingValues): Promise<string> {
  const response = await authClient.post<{ id: string }>('/api/Tenant/bookings', {
    branchId: values.branchId,
    staffId: values.staffId,
    serviceId: values.serviceId,
    customerFirstName: values.customerFirstName,
    customerLastName: values.customerLastName,
    customerEmail: values.customerEmail || null,
    customerPhone: values.customerPhone || null,
    scheduledDate: values.scheduledDate,
    scheduledStartTime: values.scheduledStartTime,
    customerNotes: values.customerNotes || null,
    admitImmediately: values.admitImmediately,
  })
  return response.data.id
}

export async function admitBooking(bookingId: string): Promise<IBookingArrival> {
  const response = await authClient.post<IBookingArrival>(`/api/Tenant/bookings/${bookingId}/admit`)
  return response.data
}

export async function scanArrival(token: string): Promise<IBookingArrival> {
  const response = await authClient.post<IBookingArrival>('/api/Tenant/bookings/scan-arrival', { token })
  return response.data
}

export async function getCheckoutDetails(bookingId: string): Promise<ICheckoutDetails> {
  const response = await authClient.get<ICheckoutDetails>(`/api/Tenant/bookings/${bookingId}/checkout-detail`)
  return response.data
}

export async function checkoutBooking(bookingId: string): Promise<ICheckoutBookingResult> {
  const response = await authClient.post<ICheckoutBookingResult>(`/api/Tenant/bookings/${bookingId}/checkout`)
  return response.data
}

export async function completeBooking(bookingId: string): Promise<void> {
  await authClient.post(`/api/Tenant/bookings/${bookingId}/complete`)
}

export async function setBookingStaffNotes(bookingId: string, notes: string): Promise<void> {
  await authClient.post(`/api/Tenant/bookings/${bookingId}/staff-notes`, { notes })
}

export async function getReassignableStaff(bookingId: string): Promise<IReassignableStaffMember[]> {
  const response = await authClient.get<IReassignableStaffMember[]>(`/api/Tenant/bookings/${bookingId}/reassignable-staff`)
  return response.data
}

export async function reassignBooking(bookingId: string, newStaffId: string): Promise<void> {
  await authClient.post(`/api/Tenant/bookings/${bookingId}/reassign`, { newStaffId })
}

export interface ICancelBookingEwalletDetails {
  provider: string
  number: string
  name: string
}

export async function cancelBooking(bookingId: string, reason: string, ewallet: ICancelBookingEwalletDetails | null = null): Promise<void> {
  await authClient.post(`/api/Tenant/bookings/${bookingId}/cancel`, {
    reason,
    ewalletProvider: ewallet?.provider ?? null,
    ewalletNumber: ewallet?.number ?? null,
    ewalletName: ewallet?.name ?? null,
  })
}

export async function markBookingNoShow(bookingId: string): Promise<void> {
  await authClient.post(`/api/Tenant/bookings/${bookingId}/no-show`)
}
