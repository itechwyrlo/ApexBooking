import { authClient } from '../api/clients/authClient'
import type { ITenantRequest } from '../interfaces/ITenantRequest'
import type { IPlatformDashboardSummary } from '../interfaces/IPlatformDashboardSummary'
import type { IPlatformBooking, IPlatformBookingFilters } from '../interfaces/IPlatformBooking'
import type { IPagedResult, IPageParams } from '../interfaces/IPagedResult'
import type { IFailedOutboxMessage } from '../interfaces/IFailedOutboxMessage'
import { fromWireStatus } from '../types/TenantRequestStatus'

// Raw wire shape from ApexBooking.Core.Application.Dtos.Request.PendingTenantRequestSummary
// (camelCase property names, ASP.NET Core's default JSON naming policy).
interface IPendingTenantRequestWire {
  tenantRegistrationRequestId: string
  businessName: string
  businessType: string
  requestedSlug: string
  requestedPlan: string
  ownerFirstName: string
  ownerLastName: string
  ownerEmail: string
  status: string
  requestedAt: string
}

function toTenantRequest(wire: IPendingTenantRequestWire): ITenantRequest {
  return {
    id: wire.tenantRegistrationRequestId,
    businessName: wire.businessName,
    businessType: wire.businessType,
    requestedSlug: wire.requestedSlug,
    requestedPlan: wire.requestedPlan,
    ownerFirstName: wire.ownerFirstName,
    ownerLastName: wire.ownerLastName,
    ownerEmail: wire.ownerEmail,
    status: fromWireStatus(wire.status),
    requestedAt: wire.requestedAt,
  }
}

export async function getTenantRequests(params: IPageParams = {}): Promise<IPagedResult<ITenantRequest>> {
  const response = await authClient.get<IPagedResult<IPendingTenantRequestWire>>('/api/TenantRequests', {
    params: { pageNumber: params.pageNumber ?? 1, pageSize: params.pageSize ?? 10 },
  })
  return { data: response.data.data.map(toTenantRequest), total: response.data.total }
}

export async function approveTenantRequest(requestId: string): Promise<void> {
  await authClient.post(`/api/TenantRequests/approved-request/${requestId}`)
}

export async function rejectTenantRequest(requestId: string, reason: string): Promise<void> {
  await authClient.post(`/api/TenantRequests/reject-request/${requestId}`, { reason })
}

export async function getDashboardSummary(): Promise<IPlatformDashboardSummary> {
  const response = await authClient.get<IPlatformDashboardSummary>('/api/superadmin/dashboard-summary')
  return response.data
}

export async function getFailedOutboxMessages(): Promise<IFailedOutboxMessage[]> {
  const response = await authClient.get<IFailedOutboxMessage[]>('/api/superadmin/outbox-messages/failed')
  return response.data
}

export async function retryOutboxMessage(id: string): Promise<void> {
  await authClient.post(`/api/superadmin/outbox-messages/${id}/retry`)
}

export async function getPlatformBookings(
  filters: IPlatformBookingFilters = {},
  params: IPageParams = {},
): Promise<IPagedResult<IPlatformBooking>> {
  const response = await authClient.get<IPagedResult<IPlatformBooking>>('/api/superadmin/bookings', {
    params: {
      pageNumber: params.pageNumber ?? 1,
      pageSize: params.pageSize ?? 10,
      tenantId: filters.tenantId || undefined,
      status: filters.status || undefined,
      fromDate: filters.fromDate || undefined,
      toDate: filters.toDate || undefined,
    },
  })
  return response.data
}
