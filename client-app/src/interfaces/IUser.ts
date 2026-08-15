import type { Role } from '../types/Role'

export interface IUser {
  id: string
  email: string
  tenantId: string
  isPlatformAdmin: boolean
  roles: Role[]
  slug: string | null
  tenantMemberId: string | null
}
