import type { IAdminNavItem } from '../../interfaces/IAdminNavItem'

export const ADMIN_NAV_ITEMS: IAdminNavItem[] = [
  { label: 'Dashboard', href: '/admin', icon: '/assets/icons/dashboard.svg', end: true },
  { label: 'Tenant Requests', href: '/admin/tenant-requests', icon: '/assets/icons/tenant-requests.svg' },
  { label: 'Bookings', href: '/admin/bookings', icon: '/assets/icons/appointments.svg' },
  { label: 'Failed Notifications', href: '/admin/failed-notifications', icon: '/assets/icons/alert-triangle.svg' },
]
