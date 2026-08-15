import { authClient } from '../api/clients/authClient'
import type { ICustomer } from '../interfaces/ICustomer'
import type { ICustomerBooking } from '../interfaces/ICustomerBooking'
import type { ICustomerLatestNote } from '../interfaces/ICustomerLatestNote'
import type { IPagedResult, IPageParams } from '../interfaces/IPagedResult'

// Raw wire shape from ApexBooking.Core.Application.Dtos.Response.CustomerSummary
interface ICustomerSummaryWire {
  customerId: string
  name: string
  email: string | null
  phoneNumber: string | null
  createdAt: string
}

function toCustomer(wire: ICustomerSummaryWire): ICustomer {
  return {
    id: wire.customerId,
    name: wire.name,
    email: wire.email,
    phoneNumber: wire.phoneNumber,
    createdAt: wire.createdAt,
  }
}

export async function getCustomers(params: IPageParams = {}): Promise<IPagedResult<ICustomer>> {
  const response = await authClient.get<IPagedResult<ICustomerSummaryWire>>('/api/Tenant/customers', {
    params: { pageNumber: params.pageNumber ?? 1, pageSize: params.pageSize ?? 10 },
  })
  return { data: response.data.data.map(toCustomer), total: response.data.total }
}

// Backs the walk-in flow's "find an existing customer" search. Returns an empty list for terms
// under 2 characters (enforced backend-side too) rather than erroring.
export async function searchCustomers(term: string): Promise<ICustomer[]> {
  const response = await authClient.get<ICustomerSummaryWire[]>('/api/Tenant/customers/search', {
    params: { term },
  })
  return response.data.map(toCustomer)
}

// Wire shape from ApexBooking.Core.Application.Dtos.Response.CustomerBookingSummary matches
// ICustomerBooking field-for-field (camelCase JSON naming policy), so no mapper is needed.
export async function getCustomerBookings(customerId: string, params: IPageParams = {}): Promise<IPagedResult<ICustomerBooking>> {
  const response = await authClient.get<IPagedResult<ICustomerBooking>>(`/api/Tenant/customers/${customerId}/bookings`, {
    params: { pageNumber: params.pageNumber ?? 1, pageSize: params.pageSize ?? 10 },
  })
  return response.data
}

// Wire shape from ApexBooking.Core.Application.Dtos.Response.CustomerLatestNoteDto matches
// ICustomerLatestNote field-for-field — no mapper needed. 204 (no past note) maps to null.
export async function getCustomerLatestNote(customerId: string): Promise<ICustomerLatestNote | null> {
  const response = await authClient.get<ICustomerLatestNote>(`/api/Tenant/customers/${customerId}/latest-note`)
  return response.status === 204 ? null : response.data
}
