import { authClient } from '../api/clients/authClient'
import type { IService, IServiceFormValues } from '../interfaces/IService'
import type { IPagedResult, IPageParams } from '../interfaces/IPagedResult'
import type { IStaffAssignment } from '../interfaces/IStaffAssignment'

// Raw wire shape from ApexBooking.Core.Application.Dtos.Response.ServiceCatalogSummary
interface IServiceCatalogSummaryWire {
  serviceId: string
  name: string
  description: string | null
  durationMinutes: number
  bufferBeforeMinutes: number
  bufferAfterMinutes: number
  price: number
  currencyCode: string
  minAdvanceBookingHoursOverride: number | null
  isActive: boolean
  createdAt: string
}

function toService(wire: IServiceCatalogSummaryWire): IService {
  return {
    id: wire.serviceId,
    name: wire.name,
    description: wire.description,
    durationMinutes: wire.durationMinutes,
    bufferBeforeMinutes: wire.bufferBeforeMinutes,
    bufferAfterMinutes: wire.bufferAfterMinutes,
    price: wire.price,
    currencyCode: wire.currencyCode,
    minAdvanceBookingHoursOverride: wire.minAdvanceBookingHoursOverride,
    isActive: wire.isActive,
    createdAt: wire.createdAt,
  }
}

export async function getServices(params: IPageParams = {}): Promise<IPagedResult<IService>> {
  const response = await authClient.get<IPagedResult<IServiceCatalogSummaryWire>>('/api/Tenant/services', {
    params: { pageNumber: params.pageNumber ?? 1, pageSize: params.pageSize ?? 10 },
  })
  return { data: response.data.data.map(toService), total: response.data.total }
}

// Only services at least one active staff member at this branch can perform — backs the walk-in
// flow's service picker, same "service offered at a branch" rule the public wizard already uses.
export async function getServicesByBranch(branchId: string): Promise<IService[]> {
  const response = await authClient.get<IServiceCatalogSummaryWire[]>(`/api/Tenant/branches/${branchId}/services`)
  return response.data.map(toService)
}

export async function createService(values: IServiceFormValues): Promise<string> {
  const response = await authClient.post<{ id: string }>('/api/Tenant/services', {
    name: values.name,
    description: values.description || null,
    durationMinutes: values.durationMinutes,
    price: values.price,
    currencyCode: values.currencyCode,
    bufferBeforeMinutes: values.bufferBeforeMinutes,
    bufferAfterMinutes: values.bufferAfterMinutes,
    minAdvanceBookingHoursOverride: values.minAdvanceBookingHoursOverride,
  })
  return response.data.id
}

export async function updateService(serviceId: string, values: IServiceFormValues): Promise<void> {
  await authClient.put('/api/Tenant/services', {
    serviceId,
    name: values.name,
    description: values.description || null,
    durationMinutes: values.durationMinutes,
    price: values.price,
    currencyCode: values.currencyCode,
    bufferBeforeMinutes: values.bufferBeforeMinutes,
    bufferAfterMinutes: values.bufferAfterMinutes,
    minAdvanceBookingHoursOverride: values.minAdvanceBookingHoursOverride,
  })
}

export async function getServiceStaff(serviceId: string): Promise<IStaffAssignment[]> {
  const response = await authClient.get<IStaffAssignment[]>(`/api/Tenant/services/${serviceId}/staff`)
  return response.data
}

export async function assignStaffToService(serviceId: string, tenantMemberId: string): Promise<void> {
  await authClient.post(`/api/Tenant/services/${serviceId}/staff`, { tenantMemberId })
}

export async function unassignStaffFromService(serviceId: string, tenantMemberId: string): Promise<void> {
  await authClient.delete(`/api/Tenant/services/${serviceId}/staff/${tenantMemberId}`)
}
