import { ADMIN_NAV_ITEMS } from './admin.nav.config'
import type { IShellNavSection } from '../../interfaces/IShellNavItem'

// Super Admin's nav has no role gating and no sections today — a single, ungrouped list.
export function buildAdminNavSections(): IShellNavSection[] {
  return [
    {
      key: 'admin',
      items: ADMIN_NAV_ITEMS.map((item) => ({
        key: item.href,
        label: item.label,
        icon: item.icon,
        href: item.href,
        end: item.end,
      })),
    },
  ]
}
