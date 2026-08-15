import { authClient } from '../api/clients/authClient'
import type { IBranch } from '../interfaces/IBranch'
import type { IBranchDetail, IBranchFormValues } from '../interfaces/IBranchDetail'
import type { IOperatingHoursEntry } from '../interfaces/IOperatingHoursEntry'

// Raw wire shape from ApexBooking.Core.Application.Features.Tenancy.Queries.GetAllBranches.BranchAdminSummary
interface IBranchAdminSummaryWire {
  branchId: string
  branchName: string
  street: string
  barangay: string | null
  city: string
  province: string
  zipCode: string
  timeZoneId: string
  isActive: boolean
}

// Raw wire shape from ApexBooking.Core.Application.Features.Tenancy.Queries.GetBranch.BranchDetailDto
interface IBranchDetailWire extends IBranchAdminSummaryWire {
  operatingHours: IOperatingHoursEntry[]
}

function toBranch(wire: IBranchAdminSummaryWire): IBranch {
  return {
    id: wire.branchId,
    name: wire.branchName,
    street: wire.street,
    barangay: wire.barangay,
    city: wire.city,
    province: wire.province,
    zipCode: wire.zipCode,
    timeZoneId: wire.timeZoneId,
    isActive: wire.isActive,
  }
}

function toBranchDetail(wire: IBranchDetailWire): IBranchDetail {
  return { ...toBranch(wire), operatingHours: wire.operatingHours }
}

export async function getBranches(): Promise<IBranch[]> {
  const response = await authClient.get<IBranchAdminSummaryWire[]>('/api/Tenant/branches')
  return response.data.map(toBranch)
}

export async function getBranch(branchId: string): Promise<IBranchDetail> {
  const response = await authClient.get<IBranchDetailWire>(`/api/Tenant/branches/${branchId}`)
  return toBranchDetail(response.data)
}

export async function addBranch(values: IBranchFormValues): Promise<string> {
  const response = await authClient.post<{ id: string }>('/api/Tenant/branches', {
    branchName: values.branchName,
    timeZoneId: values.timeZoneId,
    street: values.street,
    barangay: values.barangay || null,
    city: values.city,
    province: values.province,
    zipCode: values.zipCode,
  })
  return response.data.id
}

export async function updateBranchProfile(branchId: string, values: IBranchFormValues): Promise<void> {
  await authClient.put(`/api/Tenant/branches/${branchId}`, {
    branchName: values.branchName,
    timeZoneId: values.timeZoneId,
    street: values.street,
    barangay: values.barangay || null,
    city: values.city,
    province: values.province,
    zipCode: values.zipCode,
  })
}

export async function updateBranchOperatingHours(branchId: string, operatingHours: IOperatingHoursEntry[]): Promise<void> {
  await authClient.put(`/api/Tenant/branches/${branchId}/hours`, { operatingHours })
}
