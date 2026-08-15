import type { Role } from '../types/Role'

export interface IAddTeamMemberValues {
  branchId: string
  firstName: string
  lastName: string
  email: string
  contactNumber: string
  role: Role | ''
  customJobTitle: string
}
