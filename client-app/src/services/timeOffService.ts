import { authClient } from '../api/clients/authClient'
import type { IPagedResult, IPageParams } from '../interfaces/IPagedResult'
import type { IRequestTimeOffValues, ITimeOffFilters, ITimeOffRequest } from '../interfaces/ITimeOffRequest'
import { TimeOffType } from '../types/TimeOffType'

// Wire shape from ApexBooking.Core.Application.Dtos.Response.TimeOffRequestSummary matches
// ITimeOffRequest field-for-field (camelCase JSON naming policy), so no mapper is needed.
export async function getTimeOffRequests(
  filters: ITimeOffFilters,
  params: IPageParams = {},
): Promise<IPagedResult<ITimeOffRequest>> {
  const response = await authClient.get<IPagedResult<ITimeOffRequest>>('/api/Tenant/team/time-off', {
    params: {
      pageNumber: params.pageNumber ?? 1,
      pageSize: params.pageSize ?? 10,
      status: filters.status || undefined,
    },
  })
  return response.data
}

export async function requestTimeOff(values: IRequestTimeOffValues): Promise<string> {
  const isPartialDay = values.type === TimeOffType.PartialDay
  const response = await authClient.post<{ id: string }>('/api/Tenant/team/time-off', {
    type: values.type,
    startDate: values.startDate,
    endDate: values.endDate,
    startTime: isPartialDay ? `${values.startTime}:00` : null,
    endTime: isPartialDay ? `${values.endTime}:00` : null,
    reason: values.reason || null,
  })
  return response.data.id
}

export async function blockMyTime(date: string, startTime: string, endTime: string, reason: string): Promise<string> {
  const response = await authClient.post<{ id: string }>('/api/Tenant/team/time-off/block', {
    date,
    startTime: `${startTime}:00`,
    endTime: `${endTime}:00`,
    reason: reason || null,
  })
  return response.data.id
}

export async function approveTimeOff(requestId: string): Promise<void> {
  await authClient.post(`/api/Tenant/team/time-off/${requestId}/approve`)
}

export async function rejectTimeOff(requestId: string): Promise<void> {
  await authClient.post(`/api/Tenant/team/time-off/${requestId}/reject`)
}
