export const Role = {
  Owner: 'Owner',
  Admin: 'Admin',
  Staff: 'Staff',
} as const

export type Role = (typeof Role)[keyof typeof Role]
