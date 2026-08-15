import type { ISidebarNavItem } from '../../interfaces/ISidebarNavItem'
import { Role } from '../../types/Role'

// href values are relative to the current tenant's dashboard root (buildDashboardPath(slug, href))
// — resolved at render time in Sidebar.tsx, not baked in here, since slug isn't known until login.
export const BOOKING_NAV_ITEMS: ISidebarNavItem[] = [
  {
    label: 'Dashboard',
    href: '',
    icon: '/assets/icons/dashboard.svg',
    roles: [Role.Owner, Role.Admin, Role.Staff],
    section: 'scheduling',
  },
  {
    label: 'Appointments',
    href: 'appointments',
    icon: '/assets/icons/appointments.svg',
    roles: [Role.Owner, Role.Admin, Role.Staff],
    section: 'scheduling',
  },
  {
    label: 'Calendar',
    href: 'calendar',
    icon: '/assets/icons/calendar.svg',
    roles: [Role.Owner, Role.Admin, Role.Staff],
    section: 'scheduling',
  },
  {
    label: 'Time Offs',
    href: 'time-offs',
    icon: '/assets/icons/time-offs.svg',
    roles: [Role.Owner, Role.Admin, Role.Staff],
    section: 'scheduling',
  },
  {
    label: 'Clients',
    href: 'clients',
    icon: '/assets/icons/clients.svg',
    roles: [Role.Owner, Role.Admin],
    section: 'manage',
  },
  {
    label: 'Team',
    href: 'staff',
    icon: '/assets/icons/staff.svg',
    roles: [Role.Owner, Role.Admin],
    section: 'manage',
  },
  {
    label: 'Refunds',
    href: 'refunds',
    icon: '/assets/icons/refund.svg',
    roles: [Role.Owner, Role.Admin],
    section: 'manage',
  },
  {
    label: 'Services',
    href: 'services',
    icon: '/assets/icons/services.svg',
    roles: [Role.Owner, Role.Admin],
    section: 'manage',
  },
  {
    label: 'Business Profile',
    href: 'business-profile',
    icon: '/assets/icons/business-profile.svg',
    roles: [Role.Owner],
    section: 'manage',
  },
  {
    label: 'Branches',
    href: 'branches',
    icon: '/assets/icons/branches.svg',
    roles: [Role.Owner],
    section: 'manage',
  },
  {
    label: 'Settings',
    href: 'settings',
    icon: '/assets/icons/settings.svg',
    roles: [Role.Owner],
    section: 'settings',
  },
]

export const NAV_SECTION_LABELS: Record<'scheduling' | 'manage', string> = {
  scheduling: 'Scheduling',
  manage: 'Manage',
}
