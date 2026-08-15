import type { TimeOffType } from '../types/TimeOffType'
import type { TimeOffStatus } from '../types/TimeOffStatus'

// Mirrors ApexBooking.Core.Application.Dtos.Response.TimeOffRequestSummary
export interface ITimeOffRequest {
  requestId: string
  tenantMemberId: string
  memberName: string
  memberPhotoUrl: string | null
  type: TimeOffType
  startDate: string
  endDate: string
  startTime: string | null
  endTime: string | null
  status: TimeOffStatus
  reason: string | null
  requestedAt: string
  decidedAt: string | null
}

export interface IRequestTimeOffValues {
  type: TimeOffType
  startDate: string
  endDate: string
  startTime: string
  endTime: string
  reason: string
}

export interface ITimeOffFilters {
  status?: TimeOffStatus
}
