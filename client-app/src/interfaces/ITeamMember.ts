import type { Role } from '../types/Role'
import type { TenantMemberStatus } from '../types/TenantMemberStatus'

// Mirrors ApexBooking.Core.Application.Dtos.Response.TeamMemberSummary
export interface ITeamMember {
  id: string
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

export interface IEditTeamMemberValues {
  firstName: string
  lastName: string
  contactNumber: string
  customJobTitle: string
  role: Role | ''
}

// Mirrors ApexBooking.Core.Application.Dtos.Response.TeamMemberRemovalImpact
export interface ITeamMemberRemovalImpact {
  hasHistoricalRecords: boolean
}

// Mirrors ApexBooking.Core.Application.Dtos.Response.TeamMemberRemovalResult
export interface ITeamMemberRemovalResult {
  wasSoftDeleted: boolean
}
