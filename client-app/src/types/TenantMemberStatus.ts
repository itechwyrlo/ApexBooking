export const TenantMemberStatus = {
  Invited: 'Invited',
  Active: 'Active',
  Deactivated: 'Deactivated',
} as const

export type TenantMemberStatus = (typeof TenantMemberStatus)[keyof typeof TenantMemberStatus]
