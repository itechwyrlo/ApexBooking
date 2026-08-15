import type { Role } from '../types/Role'

export type NavSection = 'scheduling' | 'manage' | 'settings'

export interface ISidebarNavItem {
  label: string
  href: string
  icon: string
  roles: Role[]
  section: NavSection
  children?: ISidebarNavItem[]
}
