import { authClient } from '../api/clients/authClient'
import type { IEditTeamMemberValues, ITeamMember, ITeamMemberRemovalImpact, ITeamMemberRemovalResult } from '../interfaces/ITeamMember'
import type { IAddTeamMemberValues } from '../interfaces/IAddTeamMemberValues'
import type { IDaySchedule } from '../interfaces/IDaySchedule'
import type { IAddStaffBreakValues, IStaffBreak } from '../interfaces/IStaffBreak'
import type { IIdleStaffMember } from '../interfaces/IIdleStaffMember'
import type { IStaffPerformanceEntry } from '../interfaces/IStaffPerformanceEntry'
import type { IPagedResult, IPageParams } from '../interfaces/IPagedResult'
import type { Role } from '../types/Role'
import type { TenantMemberStatus } from '../types/TenantMemberStatus'

// Raw wire shape from ApexBooking.Core.Application.Dtos.Response.TeamMemberSummary
// (camelCase property names, ASP.NET Core's default JSON naming policy; role/status are
// enum names as strings via the global JsonStringEnumConverter).
interface ITeamMemberSummaryWire {
  tenantMemberId: string
  userId: string | null
  email: string
  firstName: string
  lastName: string
  fullName: string
  contactNumber: string
  photoUrl: string | null
  role: Role
  customJobTitle: string | null
  status: TenantMemberStatus
  createdAt: string
}

function toTeamMember(wire: ITeamMemberSummaryWire): ITeamMember {
  return {
    id: wire.tenantMemberId,
    userId: wire.userId,
    email: wire.email,
    firstName: wire.firstName,
    lastName: wire.lastName,
    fullName: wire.fullName,
    contactNumber: wire.contactNumber,
    photoUrl: wire.photoUrl,
    role: wire.role,
    customJobTitle: wire.customJobTitle,
    status: wire.status,
    createdAt: wire.createdAt,
  }
}

export async function getTeamMembers(params: IPageParams = {}): Promise<IPagedResult<ITeamMember>> {
  const response = await authClient.get<IPagedResult<ITeamMemberSummaryWire>>('/api/Tenant/team', {
    params: { pageNumber: params.pageNumber ?? 1, pageSize: params.pageSize ?? 10 },
  })
  return { data: response.data.data.map(toTeamMember), total: response.data.total }
}

export async function getIdleStaff(): Promise<IIdleStaffMember[]> {
  const response = await authClient.get<IIdleStaffMember[]>('/api/Tenant/team/idle')
  return response.data
}

export async function getStaffPerformance(fromDate: string, toDate: string): Promise<IStaffPerformanceEntry[]> {
  const response = await authClient.get<IStaffPerformanceEntry[]>('/api/Tenant/team/performance', { params: { fromDate, toDate } })
  return response.data
}

export async function addTeamMember(values: IAddTeamMemberValues): Promise<void> {
  await authClient.post('/api/Tenant/add-team', {
    request: {
      branchId: values.branchId,
      email: values.email,
      firstName: values.firstName,
      lastName: values.lastName,
      contactNumber: values.contactNumber || null,
      role: values.role,
      customJobTitle: values.customJobTitle,
    },
  })
}

export async function updateTeamMember(tenantMemberId: string, values: IEditTeamMemberValues): Promise<void> {
  await authClient.put(`/api/Tenant/team/${tenantMemberId}`, {
    firstName: values.firstName,
    lastName: values.lastName,
    contactNumber: values.contactNumber || '',
    customJobTitle: values.customJobTitle || null,
    role: values.role,
  })
}

export async function getTeamMemberRemovalImpact(tenantMemberId: string): Promise<ITeamMemberRemovalImpact> {
  const response = await authClient.get<ITeamMemberRemovalImpact>(`/api/Tenant/team/${tenantMemberId}/removal-impact`)
  return response.data
}

export async function removeTeamMember(tenantMemberId: string): Promise<ITeamMemberRemovalResult> {
  const response = await authClient.delete<ITeamMemberRemovalResult>(`/api/Tenant/team/${tenantMemberId}`)
  return response.data
}

export async function getTeamMemberSchedule(tenantMemberId: string): Promise<IDaySchedule[]> {
  const response = await authClient.get<IDaySchedule[]>(`/api/Tenant/team/${tenantMemberId}/schedule`)
  return response.data
}

export async function updateTeamMemberSchedule(tenantMemberId: string, schedules: IDaySchedule[]): Promise<void> {
  await authClient.put('/api/Tenant/team/schedule', { tenantMemberId, schedules })
}

export async function getStaffBreaks(tenantMemberId: string): Promise<IStaffBreak[]> {
  const response = await authClient.get<IStaffBreak[]>(`/api/Tenant/team/${tenantMemberId}/breaks`)
  return response.data
}

export async function addStaffBreak(tenantMemberId: string, values: IAddStaffBreakValues): Promise<string> {
  const response = await authClient.post<{ id: string }>('/api/Tenant/team/break', {
    tenantMemberId,
    name: values.name,
    startTime: values.startTime,
    endTime: values.endTime,
  })
  return response.data.id
}

export async function removeStaffBreak(tenantMemberId: string, breakId: string): Promise<void> {
  await authClient.delete(`/api/Tenant/team/${tenantMemberId}/break/${breakId}`)
}
